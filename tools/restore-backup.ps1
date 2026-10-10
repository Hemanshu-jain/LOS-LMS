<#
.SYNOPSIS
  Downloads a nightly DB backup from the private Supabase bucket, decrypts it and restores it into a TARGET database.

.DESCRIPTION
  Restores into a SCRATCH database by default ("los_lms_restore_scratch" on the local MySQL). It refuses, unless
  you pass -Force: a target that looks like production (los_lms, los_lms_web, defaultdb, anything containing
  "prod"), a target on a non-local host, or a target database that already contains tables.
  Needs gpg (Gpg4win) and mysql.exe on PATH. Nothing is written to disk unencrypted except the temporary
  decrypted .sql.gz, which is deleted when the script ends.

  Supabase address and service key come from the environment (SUPABASE_URL, SUPABASE_SERVICE_KEY) or a prompt;
  the backup passphrase is always prompted. None of them is echoed.

.EXAMPLE
  .\restore-backup.ps1 -List
.EXAMPLE
  .\restore-backup.ps1                      # latest backup -> los_lms_restore_scratch on 127.0.0.1
.EXAMPLE
  .\restore-backup.ps1 -Name los_lms-20261011T213000Z.sql.gz.gpg -Target los_lms_check
#>
[CmdletBinding()]
param(
    [string]$Name,                                  # backup file name; default = the latest
    [string]$Target = 'los_lms_restore_scratch',    # database to restore INTO
    [string]$TargetHost = '127.0.0.1',
    [int]$TargetPort = 3306,
    [string]$TargetUser = 'root',
    [securestring]$TargetPassword,                  # omit for a passwordless local MySQL
    [switch]$List,                                  # just list the available backups
    [switch]$Force                                  # allow a production-looking / remote / non-empty target
)

$ErrorActionPreference = 'Stop'
$bucket = 'db-backups'
$namePattern = '^los_lms-\d{8}T\d{6}Z\.sql\.gz\.gpg$'

function Read-Plain([securestring]$secure) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

# ---- Safety checks first: nothing is downloaded for a target we would refuse ----
if (-not $List) {
    $looksProduction = $Target -match '^(los_lms|los_lms_web|defaultdb)$' -or $Target -match 'prod'
    $isLocal = $TargetHost -in @('127.0.0.1', 'localhost', '::1')
    if (($looksProduction -or -not $isLocal) -and -not $Force) {
        throw "Refusing: target '$Target' on '$TargetHost' looks like production or is not local. Use a scratch database name, or pass -Force if you really mean it."
    }
    if ($Target -notmatch '^[A-Za-z0-9_]+$') { throw "Target database name may only contain letters, digits and underscores." }
}

foreach ($tool in 'gpg', 'mysql') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "$tool was not found on PATH." }
}

# ---- Supabase access ----
$url = $env:SUPABASE_URL
if (-not $url) { $url = Read-Host 'Supabase project URL' }
$key = $env:SUPABASE_SERVICE_KEY
if (-not $key) { $key = Read-Plain (Read-Host 'Supabase service key' -AsSecureString) }
$base = $url.TrimEnd('/') + '/storage/v1'
$headers = @{ Authorization = "Bearer $key"; apikey = $key }

$listing = Invoke-RestMethod -Method Post -Uri "$base/object/list/$bucket" -Headers $headers -ContentType 'application/json' `
    -Body (@{ prefix = ''; limit = 1000; sortBy = @{ column = 'name'; order = 'desc' } } | ConvertTo-Json -Depth 4)
$backups = @($listing | Where-Object { $_.name -match $namePattern } | Sort-Object name -Descending)

if ($List) { $backups | ForEach-Object { '{0}  {1:N1} MB' -f $_.name, ($_.metadata.size / 1MB) }; return }
if ($backups.Count -eq 0) { throw 'No backups found in the bucket.' }

if (-not $Name) { $Name = $backups[0].name; Write-Host "Latest backup: $Name" }
if ($Name -notmatch $namePattern) { throw "Not a backup file name: $Name" }
if ($Name -notin $backups.name) { throw "No such backup in the bucket: $Name" }

# ---- Download + decrypt ----
$work = Join-Path ([IO.Path]::GetTempPath()) ("lms-restore-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
$encrypted = Join-Path $work $Name
$plain = Join-Path $work 'dump.sql.gz'
$passFile = Join-Path $work 'pp'
try {
    Write-Host 'Downloading...'
    Invoke-WebRequest -Uri "$base/object/authenticated/$bucket/$Name" -Headers $headers -OutFile $encrypted

    $passphrase = Read-Plain (Read-Host 'Backup passphrase' -AsSecureString)
    Write-Host 'Decrypting...'
    # gpg reads the passphrase from a file that exists only for the duration of the decrypt (stdin proved unreliable
    # across gpg builds: a PowerShell pipe adds a CR that becomes part of the passphrase). It is overwritten and
    # deleted in the finally block below, and lives in the private temp folder created above.
    [IO.File]::WriteAllBytes($passFile, [Text.UTF8Encoding]::new($false).GetBytes($passphrase))   # no BOM, no line ending
    $passphrase = $null
    & gpg --batch --yes --quiet --pinentry-mode loopback --passphrase-file $passFile -o $plain --decrypt $encrypted
    [IO.File]::WriteAllBytes($passFile, [byte[]]::new(64)); Remove-Item $passFile -Force   # gone as soon as gpg is done
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $plain)) { throw 'Decryption failed (wrong passphrase?).' }

    # ---- Restore ----
    if ($TargetPassword) { $env:MYSQL_PWD = Read-Plain $TargetPassword }
    $conn = @("--host=$TargetHost", "--port=$TargetPort", "--user=$TargetUser", '--default-character-set=utf8mb4')

    & mysql @conn -e "CREATE DATABASE IF NOT EXISTS ``$Target`` CHARACTER SET utf8mb4"
    if ($LASTEXITCODE -ne 0) { throw 'Could not reach the target MySQL server.' }
    $tables = & mysql @conn -N -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$Target'"
    if ([int]$tables -gt 0 -and -not $Force) {
        throw "Refusing: '$Target' already contains $tables table(s). Restore into an empty/scratch database, or pass -Force to overwrite."
    }

    Write-Host "Restoring into '$Target' on ${TargetHost}:$TargetPort ..."
    $psi = [Diagnostics.ProcessStartInfo]::new('mysql', (($conn + $Target) -join ' '))
    $psi.RedirectStandardInput = $true
    $psi.UseShellExecute = $false
    $proc = [Diagnostics.Process]::Start($psi)
    $in = [IO.File]::OpenRead($plain)
    try {
        $gz = [IO.Compression.GZipStream]::new($in, [IO.Compression.CompressionMode]::Decompress)
        $gz.CopyTo($proc.StandardInput.BaseStream)
    }
    finally { $proc.StandardInput.Close(); $in.Dispose() }
    $proc.WaitForExit()
    if ($proc.ExitCode -ne 0) { throw "mysql exited with code $($proc.ExitCode); the restore did not complete." }

    $count = & mysql @conn -N -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$Target'"
    Write-Host "Done: $count tables restored into '$Target'."
}
finally {
    $env:MYSQL_PWD = $null
    if (Test-Path $passFile) { [IO.File]::WriteAllBytes($passFile, [byte[]]::new(64)) }   # overwrite before delete
    if (Test-Path $work) { Remove-Item -Recurse -Force $work }
}
