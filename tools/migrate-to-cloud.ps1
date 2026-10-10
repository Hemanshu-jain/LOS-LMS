<#
.SYNOPSIS
  One-time move of the PC host's data to the cloud: MySQL -> Aiven, uploaded files -> Supabase Storage, saved
  passwords -> the cloud database's vault. The OWNER runs this; it is never run against the real cloud by anyone else.

.DESCRIPTION
  Steps (each prints what it does; no secret is ever printed or logged):
    1  Preflight: shows the plan and asks for the Aiven password, the Supabase service key and Vault__Key.
    2  Stop writes: you switch maintenance mode ON on the PC, then type YES.
    3  Dump the PC database (mysqldump, consistent snapshot) into a private temp folder.
    4  Load it into Aiven over TLS (VERIFY_CA). The Aiven database is WIPED first and then loaded: the PC schema
       is older than the cloud's, and the cloud app brings it up to date by itself on its first boot (step 6 does
       exactly that), so a half-old/half-new schema must not be left behind.
    5  Upload every file under App_Data\uploads and App_Data\welcome-letters to the Supabase bucket, key =
       "App_Data/<relative path>" (what the database already stores).
    6  Import the saved-password vault with the cloud build of LosLms (--import-vault), which also migrates the
       Aiven schema to the current version.
    7  Gate checks: row counts per table (PC vs Aiven, taken right after the load), triggers, routines, files, sample rows.
    8  Shred every temp file, whatever happened.

  -WhatIf does steps 1, 3 and the local half of 7 only: nothing is asked of you, nothing leaves this PC.

  Needs: Windows PowerShell 5.1+, the PC host running (its MySQL on -PcPort), the bundled mysql.exe/mysqldump.exe
  under <HostRoot>\app\mysql\bin, and the .NET SDK (to build the cloud LosLms, unless you pass -PublishDir).

.EXAMPLE
  .\tools\migrate-to-cloud.ps1 -WhatIf
.EXAMPLE
  .\tools\migrate-to-cloud.ps1 -AivenHost mysql-xxxx.aivencloud.com -AivenPort 12345 -AivenUser avnadmin `
      -AivenDatabase defaultdb -CaFile C:\secure\aiven-ca.pem -SupabaseUrl https://xxxx.supabase.co
#>
[CmdletBinding()]
param(
    [string]$AivenHost,
    [int]$AivenPort,
    [string]$AivenUser,
    [string]$AivenDatabase,
    [string]$CaFile,                       # Aiven's CA certificate (PEM file)
    [string]$SupabaseUrl,                  # https://<project>.supabase.co
    [string]$Bucket = 'uploads',           # private bucket
    [string]$HostRoot = 'D:\Hemanshu\PERSNOAL\LMS\publish\test-v1.1.9',
    [int]$PcPort = 3307,
    [string]$PcUser = 'los',
    [string]$PcDatabase = 'los_lms',
    [string]$PublishDir,                   # a `dotnet publish` output of the CURRENT LosLms; built from this repo if omitted
    [switch]$WhatIf,                       # steps 1, 3 and the local half of 7 only
    [switch]$AllowLocalTarget              # rehearsal only: allow a loopback target (never the PC's own database)
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.Net.Http

$mysqlBin = Join-Path $HostRoot 'app\mysql\bin'
$mysqlExe = Join-Path $mysqlBin 'mysql.exe'
$dumpExe = Join-Path $mysqlBin 'mysqldump.exe'
$appData = Join-Path $HostRoot 'app\backend\App_Data'
$credFile = Join-Path $HostRoot 'app\.db-credentials'
$repoRoot = Split-Path -Parent $PSScriptRoot

function Say([string]$text) { Write-Host $text }
function Step([string]$text) { Write-Host ''; Write-Host "== $text" -ForegroundColor Cyan }
function Fail([string]$text) { throw $text }

# ---------------------------------------------------------------- helpers

function Read-Secret([string]$prompt) {
    $secure = Read-Host $prompt -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
    if ([string]::IsNullOrWhiteSpace($plain)) { Fail "$prompt must not be empty." }
    $plain.Trim()
}

function Quote([string]$arg) { '"' + $arg.Replace('"', '\"') + '"' }

# Runs a program. Secrets travel only in $Env (this child's environment), never in the arguments.
# -StdinFile is streamed in; -StdoutFile receives raw stdout; otherwise stdout is returned as text.
function Invoke-Native([string]$Exe, [string[]]$Arguments, [hashtable]$Env = @{}, [string]$StdinFile, [string]$StdoutFile, [string]$WorkDir) {
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $Exe
    $psi.Arguments = ($Arguments | ForEach-Object { Quote $_ }) -join ' '
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.RedirectStandardInput = [bool]$StdinFile
    if ($WorkDir) { $psi.WorkingDirectory = $WorkDir }
    foreach ($k in $Env.Keys) { $psi.EnvironmentVariables[$k] = [string]$Env[$k] }
    $proc = [Diagnostics.Process]::Start($psi)
    $errTask = $proc.StandardError.ReadToEndAsync()
    if ($StdinFile) {
        $in = [IO.File]::OpenRead($StdinFile)
        try { $in.CopyTo($proc.StandardInput.BaseStream) } finally { $in.Dispose(); $proc.StandardInput.Close() }
    }
    $out = $null
    if ($StdoutFile) {
        $fs = [IO.File]::Create($StdoutFile)
        try { $proc.StandardOutput.BaseStream.CopyTo($fs) } finally { $fs.Dispose() }
    }
    else { $out = $proc.StandardOutput.ReadToEnd() }
    $proc.WaitForExit()
    [pscustomobject]@{ ExitCode = $proc.ExitCode; Out = $out; Err = $errTask.Result }
}

# One side of the comparison (PC or Aiven): a function that runs the mysql client against it.
function New-Side([string]$Name, [string]$HostName, [int]$Port, [string]$User, [string]$Database, [string]$Password, [string]$Ca) {
    [pscustomobject]@{ Name = $Name; Host = $HostName; Port = $Port; User = $User; Database = $Database; Password = $Password; Ca = $Ca }
}

function Invoke-Mysql($Side, [string]$Sql, [string]$StdinFile) {
    $args = @("--host=$($Side.Host)", "--port=$($Side.Port)", "--user=$($Side.User)", '--default-character-set=utf8mb4', '--batch', '--skip-column-names')
    if ($Side.Ca) { $args += @('--ssl-mode=VERIFY_CA', "--ssl-ca=$($Side.Ca)") }
    $args += $Side.Database
    if ($Sql) { $args += @('-e', $Sql) }
    $r = Invoke-Native -Exe $mysqlExe -Arguments $args -Env @{ MYSQL_PWD = $Side.Password } -StdinFile $StdinFile
    if ($r.ExitCode -ne 0) { Fail "mysql ($($Side.Name)) failed: $($r.Err.Trim())" }
    $r.Out
}

function Get-Tables($Side) {
    $sql = "SELECT table_name FROM information_schema.tables WHERE table_schema='$($Side.Database)' AND table_type='BASE TABLE' ORDER BY table_name"
    @((Invoke-Mysql $Side $sql) -split "`r?`n" | Where-Object { $_ })
}

function Get-RowCounts($Side, [string[]]$Tables) {
    $counts = @{}
    foreach ($chunk in 0..([math]::Floor(($Tables.Count - 1) / 40))) {
        $part = $Tables | Select-Object -Skip ($chunk * 40) -First 40
        if (-not $part) { continue }
        $sql = ($part | ForEach-Object { "SELECT '$_', COUNT(*) FROM ``$_``" }) -join ' UNION ALL '
        foreach ($line in ((Invoke-Mysql $Side $sql) -split "`r?`n" | Where-Object { $_ })) {
            $f = $line -split "`t"
            $counts[$f[0]] = [long]$f[1]
        }
    }
    $counts
}

function Get-Scalar($Side, [string]$Sql) { ((Invoke-Mysql $Side $Sql) -split "`r?`n" | Select-Object -First 1).Trim() }

# Latest 3 applications by Id: the Ids (shown) and a hash of the full rows (compared, never printed: they are personal data).
function Get-SampleRows($Side) {
    $table = $Side.AppTable
    $ids = (Invoke-Mysql $Side "SELECT Id FROM ``$table`` ORDER BY Id DESC LIMIT 3") -split "`r?`n" | Where-Object { $_ }
    $rows = Invoke-Mysql $Side "SELECT * FROM ``$table`` ORDER BY Id DESC LIMIT 3"
    $sha = [Security.Cryptography.SHA256]::Create()
    $hash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes([string]$rows))).Replace('-', '')
    [pscustomobject]@{ Ids = @($ids); Hash = $hash }
}

function Remove-Secure([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    foreach ($f in Get-ChildItem -LiteralPath $Path -Recurse -File -Force -ErrorAction SilentlyContinue) {
        try {
            $len = $f.Length
            $fs = [IO.File]::Open($f.FullName, 'Open', 'Write')
            try { $zeros = New-Object byte[] 1048576; for ($left = $len; $left -gt 0; $left -= $zeros.Length) { $fs.Write($zeros, 0, [int][math]::Min($zeros.Length, $left)) } } finally { $fs.Dispose() }
        }
        catch { }
    }
    Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
}

# The PC runs MySQL on Windows, which stores every table name in lower case; Aiven runs on Linux, where `Applications`
# and `applications` are different tables and the app asks for the EF names (`Applications`, `__EFMigrationsHistory`).
# So the dump is rewritten to the cloud database's own spelling of each table name (taken from its existing schema).
# Safe for columns and keys: MySQL compares those case-insensitively anyway.
function Get-NameMap([string[]]$CloudTables) {
    $map = @{}
    foreach ($t in $CloudTables) { $map[$t.ToLowerInvariant()] = $t }
    $map
}

function ConvertTo-CloudCase([string]$Line, [hashtable]$Map) {
    if ($Line.StartsWith('INSERT INTO')) {
        # Only the table name at the front; the (possibly huge) values are left alone.
        $m = [regex]::Match($Line, '^INSERT INTO `([^`]+)`')
        if (-not $m.Success) { return $Line }
        $canonical = $Map[$m.Groups[1].Value.ToLowerInvariant()]
        if (-not $canonical) { return $Line }
        return 'INSERT INTO `' + $canonical + '`' + $Line.Substring($m.Length)
    }
    [regex]::Replace($Line, '`([^`]+)`', {
            param($m)
            $canonical = $Map[$m.Groups[1].Value.ToLowerInvariant()]
            if ($canonical) { '`' + $canonical + '`' } else { $m.Value }
        })
}

function Get-LocalFiles([string]$Folder) {
    if (-not (Test-Path -LiteralPath $Folder)) { return @() }
    @(Get-ChildItem -LiteralPath $Folder -Recurse -File -Force)
}

# Files in the bucket under a prefix, walking folders (Supabase lists one level at a time; a folder has no id).
function Get-BucketFileCount([string]$Prefix) {
    $count = 0
    $offset = 0
    $headers = @{ Authorization = "Bearer $supabaseKey"; apikey = $supabaseKey }
    do {
        $body = @{ prefix = $Prefix; limit = 100; offset = $offset; sortBy = @{ column = 'name'; order = 'asc' } } | ConvertTo-Json -Depth 4
        # Windows PowerShell hands a JSON array back as ONE object; piping it unrolls it into its items.
        $reply = Invoke-RestMethod -Method Post -Uri "$base/storage/v1/object/list/$([Uri]::EscapeDataString($Bucket))" -Headers $headers -ContentType 'application/json' -Body $body
        $page = @($reply | ForEach-Object { $_ })
        foreach ($item in $page) {
            if ($null -eq $item.id) { $count += Get-BucketFileCount "$Prefix/$($item.name)" } else { $count++ }
        }
        $offset += $page.Count
    } while ($page.Count -eq 100)
    $count
}

$script:Results = New-Object System.Collections.ArrayList
function Add-Result([string]$Check, [string]$Detail, [bool]$Ok) {
    [void]$script:Results.Add([pscustomobject]@{ Check = $Check; Detail = $Detail; Ok = $Ok })
}

# ---------------------------------------------------------------- Step 1: preflight

Step '1/8 Preflight'
foreach ($tool in @($mysqlExe, $dumpExe)) { if (-not (Test-Path -LiteralPath $tool)) { Fail "Not found: $tool (check -HostRoot)." } }
if (-not (Test-Path -LiteralPath $credFile)) { Fail "Not found: $credFile (the PC's database credentials)." }

$ownPublish = $false
$localHosts = @('127.0.0.1', 'localhost', '::1', $env:COMPUTERNAME, 'localhost.localdomain')
if (-not $WhatIf) {
    foreach ($p in 'AivenHost', 'AivenPort', 'AivenUser', 'AivenDatabase', 'CaFile', 'SupabaseUrl') {
        if (-not (Get-Variable $p -ValueOnly)) { Fail "-$p is required (or use -WhatIf for a local-only check)." }
    }
    if (-not (Test-Path -LiteralPath $CaFile)) { Fail "CA file not found: $CaFile" }
    # Never aim the load at this PC's own database: it WIPES the target first.
    $isLoopback = $localHosts -contains $AivenHost
    if ($isLoopback -and $AivenPort -eq $PcPort) { Fail "Refusing: the target ($AivenHost`:$AivenPort) is the PC's own MySQL." }
    if ($isLoopback -and -not $AllowLocalTarget) { Fail "Refusing: '$AivenHost' is this PC. The target must be the Aiven server." }
}

# PC database credentials: DPAPI (machine scope), exactly as the host's launcher stores them.
$raw = [IO.File]::ReadAllBytes($credFile)
try { $pcPassword = [Text.Encoding]::UTF8.GetString([Security.Cryptography.ProtectedData]::Unprotect($raw, $null, 'LocalMachine')).Trim() }
catch [Security.Cryptography.CryptographicException] { $pcPassword = [Text.Encoding]::UTF8.GetString($raw).Trim() }

$pc = New-Side 'PC' '127.0.0.1' $PcPort $PcUser $PcDatabase $pcPassword $null
try { $pcTables = Get-Tables $pc } catch { Fail "Cannot read the PC database on port $PcPort. Is the host running? $($_.Exception.Message)" }
if ($pcTables.Count -eq 0) { Fail "The PC database '$PcDatabase' has no tables." }

$uploads = Join-Path $appData 'uploads'
$letters = Join-Path $appData 'welcome-letters'
$files = @(Get-LocalFiles $uploads) + @(Get-LocalFiles $letters)
$megabytes = [math]::Round((($files | Measure-Object Length -Sum).Sum) / 1MB, 1)
$vaultFiles = @('vault.key', 'credential-vault.dat') | ForEach-Object { Join-Path $appData $_ }
foreach ($v in $vaultFiles) { if (-not (Test-Path -LiteralPath $v)) { Fail "Not found: $v" } }

Say "  Mode        : $(if ($WhatIf) { 'WHAT-IF (local only; nothing leaves this PC)' } else { 'LIVE MIGRATION' })"
Say "  Source (PC) : 127.0.0.1:$PcPort / $PcDatabase  ($($pcTables.Count) tables)"
Say "  Files       : $($files.Count) files, $megabytes MB under App_Data\uploads and \welcome-letters"
Say "  Vault       : $(Split-Path -Leaf $vaultFiles[0]) + $(Split-Path -Leaf $vaultFiles[1])"
if (-not $WhatIf) {
    $base = $SupabaseUrl.Trim().TrimEnd('/')
    Say "  Target DB   : $AivenHost`:$AivenPort / $AivenDatabase  as $AivenUser  (TLS, certificate checked against $CaFile)"
    Say "                !! this database will be WIPED and replaced by the PC's data !!"
    Say "  Target files: $base  bucket '$Bucket'  (key = App_Data/<path>)"
    Say '  Secrets     : you will be asked for the Aiven password, the Supabase service key and Vault__Key. Nothing is echoed or logged.'

    if (-not $PublishDir) {
        $PublishDir = Join-Path ([IO.Path]::GetTempPath()) ('lms-publish-' + [guid]::NewGuid().ToString('N'))
        $ownPublish = $true
    }
    Say "  Cloud build : $PublishDir  $(if ($ownPublish) { '(built from this repo now)' } else { '(given)' })"
    Say ''
    $aivenPassword = Read-Secret 'Aiven database password'
    $supabaseKey = Read-Secret 'Supabase service key'
    $vaultKey = Read-Secret 'Vault__Key (the same value as on Render)'
}

# Everything sensitive lives only in this private temp folder, shredded at the end whatever happens.
$work = Join-Path ([IO.Path]::GetTempPath()) ('lms-migrate-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$failed = $false
try {
    # ------------------------------------------------------------ Step 2: stop writes
    if (-not $WhatIf) {
        Step '2/8 Stop writes on the PC'
        Say '  Open the PC app as SuperAdmin -> System Updates and switch MAINTENANCE MODE ON, so nobody can save'
        Say '  anything while the data is copied. (There is no command-line switch for it; keep it ON afterwards.)'
        if ((Read-Host '  Type YES once maintenance mode is on') -cne 'YES') { Fail 'Cancelled: maintenance mode was not confirmed.' }
    }
    else { Step '2/8 Stop writes: skipped (what-if)' }

    # ------------------------------------------------------------ Step 3: dump
    Step '3/8 Dump the PC database'
    $dumpFile = Join-Path $work 'dump.sql'
    $r = Invoke-Native -Exe $dumpExe -StdoutFile $dumpFile -Env @{ MYSQL_PWD = $pcPassword } -Arguments @(
        '--host=127.0.0.1', "--port=$PcPort", "--user=$PcUser", '--single-transaction', '--routines', '--triggers',
        '--set-gtid-purged=OFF', '--hex-blob', '--no-tablespaces', '--default-character-set=utf8mb4', $PcDatabase)
    if ($r.ExitCode -ne 0) { Fail "mysqldump failed: $($r.Err.Trim())" }
    $dumpSize = (Get-Item $dumpFile).Length
    if ($dumpSize -lt 1024) { Fail "The dump is only $dumpSize bytes." }
    $createCount = 0
    $tail = ''
    $reader = [IO.StreamReader]::new($dumpFile, [Text.Encoding]::UTF8)
    try {
        while ($null -ne ($line = $reader.ReadLine())) {
            if ($line.StartsWith('CREATE TABLE')) { $createCount++ }
            if ($line.StartsWith('-- Dump completed')) { $tail = $line }
        }
    }
    finally { $reader.Dispose() }
    if ($createCount -ne $pcTables.Count) { Fail "The dump has $createCount tables but the PC database has $($pcTables.Count)." }
    if (-not $tail) { Fail 'The dump has no completion line (truncated?).' }
    Say "  Dumped $([math]::Round($dumpSize / 1MB, 1)) MB, $createCount tables, complete."
    Add-Result 'Dump complete' "$createCount tables, $([math]::Round($dumpSize / 1MB, 1)) MB" $true

    $pcCounts = Get-RowCounts $pc $pcTables
    $pcTriggers = [int](Get-Scalar $pc "SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema='$PcDatabase'")
    $pcRoutines = [int](Get-Scalar $pc "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema='$PcDatabase'")
    $pc | Add-Member -NotePropertyName AppTable -NotePropertyValue ($pcTables | Where-Object { $_ -ieq 'Applications' } | Select-Object -First 1) -Force
    $pcSample = if ($pc.AppTable) { Get-SampleRows $pc } else { $null }

    if ($WhatIf) {
        Step '7/8 Gate checks (local half)'
        Add-Result 'PC rows' "$($pcTables.Count) tables, $(($pcCounts.Values | Measure-Object -Sum).Sum) rows" $true
        Add-Result 'PC triggers / routines' "$pcTriggers / $pcRoutines" $true
        Add-Result 'Local files' "$($files.Count) files, $megabytes MB" $true
        Add-Result 'Sample rows' $(if ($pcSample) { 'latest 3 applications: ' + ($pcSample.Ids -join ', ') } else { 'no Applications table' }) $true
        $pcCounts.GetEnumerator() | Sort-Object Name | ForEach-Object { Say ('  {0,-40} {1,10}' -f $_.Name, $_.Value) }
    }
    else {

    # ------------------------------------------------------------ Step 4: load into Aiven
    Step '4/8 Load into Aiven (TLS, VERIFY_CA)'
    $aiven = New-Side 'Aiven' $AivenHost $AivenPort $AivenUser $AivenDatabase $aivenPassword $CaFile
    [void](Get-Scalar $aiven 'SELECT 1')   # reachable, TLS certificate accepted, credentials right: before anything is wiped

    $existing = Get-Tables $aiven
    if ($existing.Count -eq 0) { Fail "The Aiven database '$AivenDatabase' has no tables. Start the cloud app once so it creates its schema (that gives the table names their right spelling), then run this again." }
    $nameMap = Get-NameMap $existing
    $unknown = @($pcTables | Where-Object { -not $nameMap.ContainsKey($_.ToLowerInvariant()) })
    if ($unknown.Count -gt 0) { Say "  Note: $($unknown.Count) PC table(s) are not in the cloud schema and keep their names: $($unknown -join ', ')" }

    # Managed MySQL has no `los` definer account: drop the DEFINER clauses (triggers/routines/views are then owned by the loading user).
    $loadFile = Join-Path $work 'load.sql'
    $definerLines = 0
    $renamedLines = 0
    $rd = [IO.StreamReader]::new($dumpFile, [Text.Encoding]::UTF8)
    $wr = [IO.StreamWriter]::new($loadFile, $false, (New-Object Text.UTF8Encoding($false)))
    try {
        $pattern = '/\*!\d+ DEFINER=`[^`]*`@`[^`]*`\*/|DEFINER=`[^`]*`@`[^`]*`'
        while ($null -ne ($line = $rd.ReadLine())) {
            if (-not $line.StartsWith('INSERT INTO') -and $line.Contains('DEFINER=')) { $line = [regex]::Replace($line, $pattern, ''); $definerLines++ }
            $converted = ConvertTo-CloudCase $line $nameMap
            if ($converted -cne $line) { $renamedLines++ }
            $wr.Write($converted); $wr.Write("`n")
        }
    }
    finally { $rd.Dispose(); $wr.Dispose() }

    Say "  Wiping $($existing.Count) existing table(s) in '$AivenDatabase' ..."
    if ($existing.Count -gt 0) {
        $drop = 'SET FOREIGN_KEY_CHECKS=0; DROP TABLE IF EXISTS ' + (($existing | ForEach-Object { "``$_``" }) -join ', ') + '; SET FOREIGN_KEY_CHECKS=1;'
        [void](Invoke-Mysql $aiven $drop)
    }
    $oldRoutines = (Invoke-Mysql $aiven "SELECT routine_type, routine_name FROM information_schema.routines WHERE routine_schema='$AivenDatabase'") -split "`r?`n" | Where-Object { $_ }
    foreach ($ro in $oldRoutines) { $f = $ro -split "`t"; [void](Invoke-Mysql $aiven "DROP $($f[0]) IF EXISTS ``$($f[1])``") }

    Say '  Loading the dump ...'
    [void](Invoke-Mysql $aiven $null $loadFile)
    Say "  Loaded ($definerLines definer clause line(s) removed, $renamedLines line(s) renamed to the cloud's table spelling)."

    # Snapshot right after the load, before step 6 migrates the schema (a migration may add tables/seed rows).
    $aivenTables = Get-Tables $aiven
    $aivenCounts = Get-RowCounts $aiven $aivenTables
    $aivenTriggers = [int](Get-Scalar $aiven "SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema='$AivenDatabase'")
    $aivenRoutines = [int](Get-Scalar $aiven "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema='$AivenDatabase'")
    $aiven | Add-Member -NotePropertyName AppTable -NotePropertyValue ($aivenTables | Where-Object { $_ -ieq 'Applications' } | Select-Object -First 1) -Force
    $aivenSample = if ($aiven.AppTable) { Get-SampleRows $aiven } else { $null }

    # ------------------------------------------------------------ Step 5: upload files
    Step '5/8 Upload files to Supabase Storage'
    $http = New-Object System.Net.Http.HttpClient
    $http.Timeout = [TimeSpan]::FromMinutes(15)
    $http.DefaultRequestHeaders.Add('apikey', $supabaseKey)
    $http.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $supabaseKey)
    $bucketUrl = "$base/storage/v1/object/" + [Uri]::EscapeDataString($Bucket) + '/'

    $uploaded = 0
    $failures = New-Object System.Collections.ArrayList
    $i = 0
    foreach ($f in $files) {
        $i++
        $rel = $f.FullName.Substring($appData.TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
        $key = 'App_Data/' + $rel
        $url = $bucketUrl + (($key -split '/' | ForEach-Object { [Uri]::EscapeDataString($_) }) -join '/')
        $ok = $false
        $why = ''
        foreach ($attempt in 1..3) {
            $fs = [IO.File]::OpenRead($f.FullName)
            try {
                $req = New-Object System.Net.Http.HttpRequestMessage([Net.Http.HttpMethod]::Post, $url)
                $req.Content = New-Object System.Net.Http.StreamContent($fs)
                $req.Content.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue('application/octet-stream')
                $req.Headers.Add('x-upsert', 'true')
                $resp = $http.SendAsync($req).GetAwaiter().GetResult()
                $code = [int]$resp.StatusCode
                if ($code -ge 200 -and $code -lt 300) { $ok = $true; $resp.Dispose(); break }
                $why = "HTTP $code $($resp.Content.ReadAsStringAsync().GetAwaiter().GetResult())"
                $resp.Dispose()
                if ($code -lt 500 -and $code -ne 429) { break }
            }
            catch { $why = $_.Exception.Message }
            finally { $fs.Dispose() }
            Start-Sleep -Seconds (2 * $attempt)
        }
        if ($ok) { $uploaded++ } else { [void]$failures.Add("$key  ($why)") }
        if ($i % 25 -eq 0) { Say "  $i / $($files.Count) ..." }
    }
    Say "  Uploaded $uploaded of $($files.Count) files."
    if ($failures.Count -gt 0) {
        Say '  FAILED uploads:'
        $failures | ForEach-Object { Say "    $_" }
        Fail "$($failures.Count) file(s) failed to upload. The run stops here; fix and run it again (uploads are idempotent)."
    }

    # ------------------------------------------------------------ Step 6: vault (also migrates the schema)
    Step '6/8 Import the saved-password vault (and bring the Aiven schema up to date)'
    if ($ownPublish) {
        Say '  Building the cloud LosLms (dotnet publish) ...'
        $pub = Invoke-Native -Exe 'dotnet' -Arguments @('publish', (Join-Path $repoRoot 'LosLms\LosLms.csproj'), '-c', 'Release', '-o', $PublishDir, '--nologo', '-v', 'q')
        if ($pub.ExitCode -ne 0) { Fail "dotnet publish failed: $($pub.Out) $($pub.Err)" }
    }
    $dll = Join-Path $PublishDir 'LosLms.dll'
    if (-not (Test-Path -LiteralPath $dll)) { Fail "Not found: $dll" }
    $vaultDir = Join-Path $work 'vault'
    New-Item -ItemType Directory -Path $vaultDir | Out-Null
    foreach ($v in $vaultFiles) { Copy-Item -LiteralPath $v -Destination $vaultDir }

    $cs = "Server=$AivenHost;Port=$AivenPort;Database=$AivenDatabase;User Id=$AivenUser;Password=""$($aivenPassword.Replace('"', '""'))"";"
    $appEnv = @{
        'ConnectionStrings__LosDb' = $cs
        'Hosting__Cloud'           = 'true'
        'Database__SslCaPem'       = [IO.File]::ReadAllText($CaFile)
        'Vault__Key'               = $vaultKey
        'Seed__DemoTenant'         = 'false'   # this run must not plant the demo company in the real data
        'ASPNETCORE_ENVIRONMENT'   = 'Production'
    }
    $app = Invoke-Native -Exe 'dotnet' -Arguments @($dll, '--import-vault', $vaultDir) -Env $appEnv -WorkDir $PublishDir
    $m = [regex]::Match(($app.Out + "`n" + $app.Err), 'Imported (\d+) credential vault entries')
    if ($app.ExitCode -ne 0 -or -not $m.Success) {
        $tailText = (($app.Out + "`n" + $app.Err) -split "`r?`n" | Where-Object { $_ -match 'fail|error|exception' } | Select-Object -First 5) -join "`n"
        Fail "The vault import did not complete (exit $($app.ExitCode)).`n$tailText"
    }
    $imported = [int]$m.Groups[1].Value
    Say "  Imported $imported saved-password entries."
    $vaultEntries = [IO.File]::ReadAllText((Join-Path $vaultDir 'credential-vault.dat')) | ConvertFrom-Json
    $total = @($vaultEntries.PSObject.Properties).Count
    Add-Result 'Vault import' "$imported of $total entries imported" ($imported -eq $total)

    # ------------------------------------------------------------ Step 7: gate checks
    Step '7/8 Gate checks'
    $aivenByLower = @{}
    foreach ($k in $aivenCounts.Keys) { $aivenByLower[$k.ToLowerInvariant()] = $aivenCounts[$k] }
    $pcLower = @($pcTables | ForEach-Object { $_.ToLowerInvariant() })
    $mismatch = @()
    foreach ($t in $pcTables) {
        $a = $aivenByLower[$t.ToLowerInvariant()]
        if ($null -eq $a -or $a -ne $pcCounts[$t]) { $mismatch += "$t (PC $($pcCounts[$t]) vs Aiven $a)" }
    }
    $extra = @($aivenTables | Where-Object { $pcLower -notcontains $_.ToLowerInvariant() })
    Say ('  {0,-40} {1,10} {2,10}  {3}' -f 'table', 'PC', 'Aiven', '')
    foreach ($t in $pcTables) {
        $a = $aivenByLower[$t.ToLowerInvariant()]
        Say ('  {0,-40} {1,10} {2,10}  {3}' -f $t, $pcCounts[$t], $a, $(if ($a -eq $pcCounts[$t]) { 'ok' } else { 'MISMATCH' }))
    }
    Add-Result 'Row counts' "$($pcTables.Count) tables compared$(if ($mismatch) { ': ' + ($mismatch -join '; ') })" ($mismatch.Count -eq 0 -and $extra.Count -eq 0)
    Add-Result 'Triggers' "PC $pcTriggers / Aiven $aivenTriggers" ($pcTriggers -eq $aivenTriggers)
    Add-Result 'Routines' "PC $pcRoutines / Aiven $aivenRoutines" ($pcRoutines -eq $aivenRoutines)

    $remote = 0
    foreach ($prefix in 'App_Data/uploads', 'App_Data/welcome-letters') {
        $remote += Get-BucketFileCount $prefix
    }
    Add-Result 'Files' "local $($files.Count) / bucket $remote" ($remote -eq $files.Count)

    if ($pcSample) {
        $same = $aivenSample -and ($aivenSample.Hash -eq $pcSample.Hash) -and (($aivenSample.Ids -join ',') -eq ($pcSample.Ids -join ','))
        Add-Result 'Sample rows' "latest 3 applications ($($pcSample.Ids -join ', ')) $(if ($same) { 'identical' } else { 'DIFFER' })" ([bool]$same)
    }
    else { Add-Result 'Sample rows' 'no Applications table' $true }
    }   # end of the live-migration steps (4-7)
}
catch {
    $failed = $true
    Write-Host ''
    Write-Host "STOPPED: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    Step '8/8 Shredding temp files'
    Remove-Secure $work
    if ($ownPublish) { Remove-Item -LiteralPath $PublishDir -Recurse -Force -ErrorAction SilentlyContinue }
    $aivenPassword = $supabaseKey = $vaultKey = $pcPassword = $null
    $env:MYSQL_PWD = $null
    Say '  Done.'
}

# ---------------------------------------------------------------- summary
if ($script:Results.Count -gt 0) {
    Write-Host ''
    Write-Host '== Result' -ForegroundColor Cyan
    foreach ($row in $script:Results) {
        $mark = if ($row.Ok) { 'PASS' } else { 'FAIL' }
        Write-Host ('  [{0}] {1,-18} {2}' -f $mark, $row.Check, $row.Detail) -ForegroundColor $(if ($row.Ok) { 'Green' } else { 'Red' })
        if (-not $row.Ok) { $failed = $true }
    }
}
if ($failed) { Write-Host "`nMIGRATION NOT VERIFIED. Do not switch over." -ForegroundColor Red; exit 1 }
if ($WhatIf) { Write-Host "`nWhat-if finished: the local checks passed." -ForegroundColor Green; exit 0 }
Write-Host @'

Migration verified. Next:
  - Keep the PC in maintenance mode (its data is now stale); switch DNS / users to the cloud app.
  - On Render set Seed__DemoTenant=false BEFORE the next restart. appsettings.Web.json turns the demo tenant on, and it
    would create the demo company and its published login inside the real database.
  - Users sign in again once (new session keys); saved passwords were re-encrypted under Vault__Key.
'@ -ForegroundColor Green
exit 0
