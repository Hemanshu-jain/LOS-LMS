# Builds the ONE LOS/LMS package. First run asks host-vs-client; the same exe is both.
#
#   .\publish.ps1
#
# The extracted folder is deliberately tidy:
#
#   LOS-LMS\
#     LOS-LMS.exe                 <- the only thing to double-click (single-file)
#     READ ME FIRST.txt
#     server-config.example.json  <- only the host operator ever touches this
#     server\                     <- host bits (ignored on staff PCs)
#       backend\  mysql\  cloudflared.exe
#
# Produces under .\publish\ :
#   LOS-LMS-v<version>-win-x64.zip   the whole app — send this to everyone
#   los-lms-v<version>-win-x64.zip   the in-app UPDATE artifact (backend only)

$ErrorActionPreference = 'Stop'
$root    = $PSScriptRoot
$appProj = Join-Path $root 'LosLms\LosLms.csproj'               # backend
$exeProj = Join-Path $root 'LosLms.Server\LosLms.Server.csproj' # the unified launcher
$stage   = Join-Path $root 'publish\LOS-LMS'
$serverD = Join-Path $stage 'server'
$backend = Join-Path $serverD 'backend'

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

# ---- Backend -> server\backend  (single-file; Blazor + EF Core do not trim safely) ----
dotnet publish $appProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:PublishTrimmed=false -o $backend
if ($LASTEXITCODE -ne 0) { throw "Backend publish failed." }

# ---- Unified launcher -> the folder root  (single-file: one clean LOS-LMS.exe) ----
dotnet publish $exeProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o $stage
if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed." }

# Belt-and-suspenders: drop any stray debug/doc files so the root really is just the exe.
Get-ChildItem $stage -File | Where-Object { $_.Extension -in '.pdb', '.xml' } | Remove-Item -Force

# ---- Stage bundled MySQL + cloudflared under server\ ----
Write-Host "Staging bundled MySQL and cloudflared..."
Copy-Item -Path $deps.MysqlDir     -Destination (Join-Path $serverD 'mysql') -Recurse -Force
Copy-Item -Path $deps.Cloudflared  -Destination (Join-Path $serverD 'cloudflared.exe') -Force

# ---- Top-level operator config template + read-me ----
Copy-Item -Path (Join-Path $root 'server-config.example.json') -Destination (Join-Path $stage 'server-config.example.json') -Force
Copy-Item -Path (Join-Path $root 'READ-ME-FIRST.txt')          -Destination (Join-Path $stage 'READ ME FIRST.txt') -Force

# ZipFile, not Compress-Archive: Compress-Archive silently drops MySQL's deeply-nested files, leaving
# a package with no database engine in it.
Add-Type -AssemblyName System.IO.Compression.FileSystem

# ---- Zip 1: the whole app (top-level LOS-LMS\ folder inside the zip; includeBaseDirectory = $true) ----
$appZip = Join-Path $root "publish\LOS-LMS-v$version-win-x64.zip"
if (Test-Path $appZip) { Remove-Item $appZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $appZip, 'Optimal', $true)

# ---- Zip 2: the in-app UPDATE artifact (backend contents only; the host swaps this in) ----
$updateZip = Join-Path $root "publish\los-lms-v$version-win-x64.zip"
if (Test-Path $updateZip) { Remove-Item $updateZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($backend, $updateZip, 'Optimal', $false)

Write-Host ""
Write-Host "Done (v$version)."
Write-Host "  SEND TO EVERYONE:                 $appZip"
Write-Host "  UPDATE ARTIFACT (GitHub Release): $updateZip"
Write-Host ""
Write-Host "Everyone gets the same zip. First run asks: host this computer, or connect to it."
