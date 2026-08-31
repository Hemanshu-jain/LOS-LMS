# Builds the Client Shell package: a thin native window with no database and no backend. This is what
# gets installed on every user's computer OTHER than the one running the Server package.
#
#   .\publish-client.ps1
#
# Produces, under .\publish\client\ :
#   LOS-LMS.exe (+ runtime)   the window the user double-clicks; reads the server URL and connects
# and the zip:
#   los-lms-client-v<version>-win-x64.zip   send this to every other user

$ErrorActionPreference = 'Stop'
$root       = $PSScriptRoot
$clientProj = Join-Path $root 'LosLms.Client\LosLms.Client.csproj'
$outRoot    = Join-Path $root 'publish\client'

# ---- Version (kept in step with the app) ----
[xml]$xml = Get-Content $clientProj
$version = ($xml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $version) { throw "No <Version> found in $clientProj" }
Write-Host "Publishing LOS/LMS CLIENT v$version (win-x64, self-contained)..."

# ---- Clean ----
if (Test-Path $outRoot) { Remove-Item $outRoot -Recurse -Force }
New-Item -ItemType Directory -Path $outRoot -Force | Out-Null

# ---- Client -> publish\client  (self-contained folder; WebView2 native loader alongside the exe) ----
dotnet publish $clientProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false -p:PublishTrimmed=false -o $outRoot
if ($LASTEXITCODE -ne 0) { throw "Client publish failed." }

# ---- Zip ----
$clientZip = Join-Path $root "publish\los-lms-client-v$version-win-x64.zip"
if (Test-Path $clientZip) { Remove-Item $clientZip -Force }
Compress-Archive -Path (Join-Path $outRoot '*') -DestinationPath $clientZip

Write-Host ""
Write-Host "Done (v$version)."
Write-Host "  CLIENT SHELL (send to every other user): $clientZip"
Write-Host ""
Write-Host "The user unzips and double-clicks LOS-LMS.exe. It finds the server automatically —"
Write-Host "no database, no backend, no configuration."
