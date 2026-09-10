# Builds the ONE LOS/LMS package. First run asks host-vs-client; the same exe is both.
#
#   .\publish.ps1
#
# The extracted folder is deliberately tidy — just the exe and one folder:
#
#   LOS-LMS.exe        <- the only thing to double-click (single-file, self-contained)
#   app\               <- everything else, out of the way
#     backend\  mysql\  cloudflared.exe  server-config.example.json  READ ME FIRST.txt
#
# The named-tunnel token is baked in (from tunnel-token.txt), so the operator pastes nothing.
#
# Produces under .\publish\ :
#   LOS-LMS-v<version>-win-x64.zip        the whole app — send this to everyone
#   LOS-LMS-Update-v<version>-win-x64.zip the in-app UPDATE artifact (backend only) + .sig

# Per-client build (see deploy\NEW-CLIENT.md):
#   .\publish.ps1                                                   # default client (bhodhix)
#   .\publish.ps1 -Subdomain client1.bhodhix.com -TunnelTokenFile tunnel-tokens\client1.txt -Label client1 -SkipUpdateArtifact
param(
    [string]$Subdomain       = 'los-lms.bhodhix.com',   # this client's subdomain -> https://<subdomain>
    [string]$TunnelTokenFile = 'tunnel-token.txt',      # repo-relative file holding this client's tunnel token
    [string]$LicenseFile     = 'license.txt',           # repo-relative file holding this client's signed licence
    [string]$Label           = '',                       # names the output zip (e.g. 'client1'); blank = default
    [switch]$SkipUpdateArtifact                          # per-client builds skip the public update artifact + signing
)

$ErrorActionPreference = 'Stop'
$root      = $PSScriptRoot
$appProj   = Join-Path $root 'LosLms\LosLms.csproj'               # backend
$exeProj   = Join-Path $root 'LosLms.Server\LosLms.Server.csproj' # the unified launcher
$stage     = Join-Path $root 'publish\LOS-LMS'    # zip root: LOS-LMS.exe + app\
$appFolder = Join-Path $stage 'app'
$backend   = Join-Path $appFolder 'backend'

# ---- Version ----
[xml]$xml = Get-Content $exeProj
$version = ($xml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $version) { throw "No <Version> found in $exeProj" }
Write-Host "Publishing LOS/LMS v$version (single package, win-x64, self-contained)..."

# ---- Bundled runtime deps (cached) ----
$deps = & (Join-Path $root 'tools\fetch-runtime-deps.ps1')

# ---- Clean ----
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $backend -Force | Out-Null

# ---- Vendor break-glass master login: baked into the BACKEND from master-key.txt (gitignored) ----
$masterArgs = @()
$masterKey = Join-Path $root 'master-key.txt'
if (Test-Path $masterKey) {
    $pw = (Get-Content $masterKey -Raw).Trim()
    if ($pw) {
        $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pw))
        $masterArgs = @("-p:MasterAdminPassword=$b64")
        Write-Host "Master admin login: BAKING IN (from master-key.txt)."
    }
} else {
    Write-Host "Master admin login: off (no master-key.txt present)."
}

# ---- This client's public URL, baked into the LAUNCHER ----
$hostedUrl  = "https://$Subdomain"
$hostedB64  = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($hostedUrl))
$hostedArgs = @("-p:HostedUrl=$hostedB64")
Write-Host "Hosted URL: BAKING IN ($hostedUrl)."

# ---- This client's named-tunnel token, baked into the LAUNCHER (gitignored token file) ----
# This is what makes the fixed address work with zero setup — nobody ever pastes a token.
$tunnelArgs = @()
$tunnelKey = Join-Path $root $TunnelTokenFile
if (Test-Path $tunnelKey) {
    $tt = (Get-Content $tunnelKey -Raw).Trim()
    # Accept either the bare token or the whole "cloudflared ... service install <token>" line.
    if ($tt -match 'install\s+(\S+)\s*$') { $tt = $Matches[1] }
    if ($tt) {
        $ttB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($tt))
        $tunnelArgs = @("-p:TunnelToken=$ttB64")
        Write-Host "Tunnel token: BAKING IN (from $TunnelTokenFile)."
    }
} else {
    Write-Host "Tunnel token: none (no $TunnelTokenFile) — the host will run LAN-only."
}

# ---- This client's signed subscription licence, baked into the BACKEND ----
$licenseArgs = @()
$licenseKeyFile = Join-Path $root $LicenseFile
if (Test-Path $licenseKeyFile) {
    $lic = (Get-Content $licenseKeyFile -Raw).Trim()
    if ($lic) {
        $licB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($lic))
        $licenseArgs = @("-p:License=$licB64")
        Write-Host "Licence: BAKING IN (from $LicenseFile)."
    }
} else {
    Write-Host "Licence: none (no $LicenseFile) — unlicensed build (enforcement off). Fine for testing."
}

# ---- Backend -> app\backend  (single-file; Blazor + EF Core do not trim safely) ----
dotnet publish $appProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:PublishTrimmed=false @masterArgs @licenseArgs -o $backend
if ($LASTEXITCODE -ne 0) { throw "Backend publish failed." }

# ---- Unified launcher -> the folder root  (single clean LOS-LMS.exe, tunnel token baked) ----
dotnet publish $exeProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false @tunnelArgs @hostedArgs -o $stage
if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed." }

# Belt-and-suspenders: drop any stray debug/doc files at the root so it really is just the exe.
Get-ChildItem $stage -File | Where-Object { $_.Extension -in '.pdb', '.xml' } | Remove-Item -Force

# ---- Stage bundled MySQL + cloudflared under app\ ----
Write-Host "Staging bundled MySQL and cloudflared..."
Copy-Item -Path $deps.MysqlDir     -Destination (Join-Path $appFolder 'mysql') -Recurse -Force
Copy-Item -Path $deps.Cloudflared  -Destination (Join-Path $appFolder 'cloudflared.exe') -Force

# ---- Optional operator config template + read-me, inside app\ (top level stays just the exe + app\) ----
Copy-Item -Path (Join-Path $root 'server-config.example.json') -Destination (Join-Path $appFolder 'server-config.example.json') -Force
Copy-Item -Path (Join-Path $root 'READ-ME-FIRST.txt')          -Destination (Join-Path $appFolder 'READ ME FIRST.txt') -Force

# ZipFile, not Compress-Archive: Compress-Archive silently drops MySQL's deeply-nested files, leaving
# a package with no database engine in it.
Add-Type -AssemblyName System.IO.Compression.FileSystem

# ---- Zip 1: the whole app. includeBaseDirectory = $false, so the zip root IS { LOS-LMS.exe, app\ } —
#      extracting gives exactly those two, no extra wrapper folder. ----
$zipTag = if ($Label) { "$Label-v$version" } else { "v$version" }
$appZip = Join-Path $root "publish\LOS-LMS-$zipTag-win-x64.zip"
if (Test-Path $appZip) { Remove-Item $appZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $appZip, 'Optimal', $false)

# ---- Zip 2: the in-app UPDATE artifact (backend only). Skipped for per-client builds, which only
#      need the app zip — the update artifact is the single public release, built once from the default. ----
$updateZip = $null
$updateSig = $null
if (-not $SkipUpdateArtifact) {
    # Built WITHOUT the master password on purpose: this artifact is attached to a PUBLIC GitHub Release,
    # so it must never carry the baked master secret (the release asset is downloadable + decompilable).
    $updateBackend = Join-Path $root 'publish\_update-backend'
    if (Test-Path $updateBackend) { Remove-Item $updateBackend -Recurse -Force }
    dotnet publish $appProj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:PublishTrimmed=false -o $updateBackend
    if ($LASTEXITCODE -ne 0) { throw "Update-artifact backend publish failed." }

    $updateZip = Join-Path $root "publish\LOS-LMS-Update-v$version-win-x64.zip"
    if (Test-Path $updateZip) { Remove-Item $updateZip -Force }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($updateBackend, $updateZip, 'Optimal', $false)
    Remove-Item $updateBackend -Recurse -Force

    # ---- Sign the update artifact ----
    # Clients verify this signature against the baked public key (UpdateSigning.cs) BEFORE applying an
    # update, so an unsigned or tampered zip on the public release is refused.
    $updateSig = "$updateZip.sig"
    $signingKey = Join-Path $root 'update-signing-private.pem'
    if (Test-Path $signingKey) {
        if (Test-Path $updateSig) { Remove-Item $updateSig -Force }
        & openssl dgst -sha256 -sign $signingKey -out $updateSig $updateZip
        if ($LASTEXITCODE -ne 0) { throw "Signing the update artifact failed (is openssl on PATH?)." }
    } else {
        throw "update-signing-private.pem not found at repo root. Restore the vendor signing key before publishing — clients reject unsigned updates."
    }
}

Write-Host ""
Write-Host "Done (v$version) — $hostedUrl"
Write-Host "  GIVE TO THIS CLIENT:              $appZip"
if ($updateZip) {
    Write-Host "  UPDATE ARTIFACT (GitHub Release): $updateZip"
    Write-Host "  UPLOAD ALONGSIDE IT (signature):  $updateSig"
}
Write-Host ""
Write-Host "Extract gives just LOS-LMS.exe + app\. First run asks: host this computer, or connect to it."
