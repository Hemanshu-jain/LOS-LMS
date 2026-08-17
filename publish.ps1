# Builds the self-contained Windows release of LOS/LMS and packages the update artifact.
#
#   .\publish.ps1
#
# Produces, under .\publish\ :
#   app\                                 the main server (self-contained, single-file LosLms.exe)
#   LOS-LMS.exe                          the launcher the operator double-clicks (starts the server,
#                                        opens the browser, applies updates) — never self-updated
#   los-lms-v<version>-SETUP-win-x64.zip first-install bundle: LOS-LMS.exe + app\ (send to the client)
#   los-lms-v<version>-win-x64.zip       the update artifact: attach this to a GitHub Release so the
#                                        in-app System Updates page can download and apply it
#
# The update zip contains ONLY the app folder's contents (that is what an update swaps in).
# No database to install and no connection string to set — the app uses an embedded SQLite file.

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$appProj   = Join-Path $root 'LosLms\LosLms.csproj'
$watchProj = Join-Path $root 'LosLms.Watchdog\LosLms.Watchdog.csproj'
$outRoot   = Join-Path $root 'publish'
$appOut    = Join-Path $outRoot 'app'

# ---- Version (drives the artifact name; must match the GitHub Release tag) ----
[xml]$xml = Get-Content $appProj
$version = ($xml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $version) { throw "No <Version> found in $appProj" }
Write-Host "Publishing LOS/LMS v$version (win-x64, self-contained)..."

# ---- Clean ----
if (Test-Path $outRoot) { Remove-Item $outRoot -Recurse -Force }
New-Item -ItemType Directory -Path $appOut -Force | Out-Null

# ---- Main app -> publish\app  (single-file, NOT trimmed: Blazor Server + EF Core do not trim safely) ----
dotnet publish $appProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:PublishTrimmed=false -o $appOut
if ($LASTEXITCODE -ne 0) { throw "Main app publish failed." }

# ---- Watchdog -> publish\  (sits next to app\; this is the launcher) ----
dotnet publish $watchProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:PublishTrimmed=false -o $outRoot
if ($LASTEXITCODE -ne 0) { throw "Watchdog publish failed." }

# ---- Two artifacts, for two different jobs ----

# 1) FIRST-INSTALL bundle: the whole runnable install = launcher + app\ folder. This is what you send
#    the client. They unzip it and double-click LOS-LMS.exe — no database to install, no config to
#    edit. Unzips to a folder containing LOS-LMS.exe and app\.
$setupZip = Join-Path $outRoot "los-lms-v$version-SETUP-win-x64.zip"
if (Test-Path $setupZip) { Remove-Item $setupZip -Force }
Compress-Archive -Path $appOut, (Join-Path $outRoot 'LOS-LMS.exe') -DestinationPath $setupZip

# 2) UPDATE artifact: the CONTENTS of app\ only (no watchdog — a running watchdog can't replace
#    itself). This is what you attach to a GitHub Release; the in-app updater downloads and the
#    watchdog extracts it over the existing app\ folder.
$updateZip = Join-Path $outRoot "los-lms-v$version-win-x64.zip"
if (Test-Path $updateZip) { Remove-Item $updateZip -Force }
Compress-Archive -Path (Join-Path $appOut '*') -DestinationPath $updateZip

Write-Host ""
Write-Host "Done (v$version)."
Write-Host "  SEND TO CLIENT (first install):   $setupZip"
Write-Host "  ATTACH TO GITHUB RELEASE (update): $updateZip"
Write-Host ""
Write-Host "First install: client unzips the SETUP zip and double-clicks LOS-LMS.exe."
Write-Host "               No database to install, no configuration to edit."
