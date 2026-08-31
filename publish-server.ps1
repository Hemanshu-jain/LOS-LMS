# Builds the Server package: the backend + a bundled portable MySQL + the tunnel client + the
# orchestrator/launcher with its local WebView2 shell. This is what runs on the ONE server machine.
#
#   .\publish-server.ps1
#
# Produces, under .\publish\server\ :
#   app\                              the backend (self-contained, single-file LosLms.exe)
#   mysql\                            bundled MySQL Community (ZIP archive; no installer)
#   cloudflared.exe                   Cloudflare tunnel client
#   LOS-LMS Server.exe (+ runtime)    the launcher the operator double-clicks
#   server.example.json               config template (operator copies to server.json, adds a token)
# and the zips:
#   los-lms-server-v<version>-win-x64.zip   the whole Server install (send to the ONE server machine)
#   los-lms-v<version>-win-x64.zip          the in-app UPDATE artifact (attach to a GitHub Release)

$ErrorActionPreference = 'Stop'
$root       = $PSScriptRoot
$appProj    = Join-Path $root 'LosLms\LosLms.csproj'
$serverProj = Join-Path $root 'LosLms.Server\LosLms.Server.csproj'
$outRoot    = Join-Path $root 'publish\server'
$appOut     = Join-Path $outRoot 'app'

# ---- Version ----
[xml]$xml = Get-Content $appProj
$version = ($xml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $version) { throw "No <Version> found in $appProj" }
Write-Host "Publishing LOS/LMS SERVER v$version (win-x64, self-contained)..."

# ---- Fetch bundled runtime deps (cached) ----
$deps = & (Join-Path $root 'tools\fetch-runtime-deps.ps1')

# ---- Clean ----
if (Test-Path $outRoot) { Remove-Item $outRoot -Recurse -Force }
New-Item -ItemType Directory -Path $appOut -Force | Out-Null

# ---- Backend -> publish\server\app  (single-file; Blazor Server + EF Core do not trim safely) ----
dotnet publish $appProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:PublishTrimmed=false -o $appOut
if ($LASTEXITCODE -ne 0) { throw "Backend publish failed." }

# ---- Launcher -> publish\server  (self-contained FOLDER, not single-file: WebView2's native loader
#      is more reliable extracted alongside the exe than self-extracted from a single file) ----
dotnet publish $serverProj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false -p:PublishTrimmed=false -o $outRoot
if ($LASTEXITCODE -ne 0) { throw "Launcher publish failed." }

# ---- Stage bundled MySQL + cloudflared + config template ----
Write-Host "Staging bundled MySQL and cloudflared..."
Copy-Item -Path $deps.MysqlDir -Destination (Join-Path $outRoot 'mysql') -Recurse -Force
Copy-Item -Path $deps.Cloudflared -Destination (Join-Path $outRoot 'cloudflared.exe') -Force
Copy-Item -Path (Join-Path $root 'server.example.json') -Destination (Join-Path $outRoot 'server.example.json') -Force

# ---- Zip 1: the whole Server install ----
$serverZip = Join-Path $root "publish\los-lms-server-v$version-win-x64.zip"
if (Test-Path $serverZip) { Remove-Item $serverZip -Force }
Compress-Archive -Path (Join-Path $outRoot '*') -DestinationPath $serverZip

# ---- Zip 2: the in-app UPDATE artifact (contents of app\ only; the launcher swaps this in) ----
$updateZip = Join-Path $root "publish\los-lms-v$version-win-x64.zip"
if (Test-Path $updateZip) { Remove-Item $updateZip -Force }
Compress-Archive -Path (Join-Path $appOut '*') -DestinationPath $updateZip

Write-Host ""
Write-Host "Done (v$version)."
Write-Host "  SERVER INSTALL (send to the server machine): $serverZip"
Write-Host "  UPDATE ARTIFACT (attach to GitHub Release):  $updateZip"
Write-Host ""
Write-Host "On the server machine: unzip, copy server.example.json to server.json and add a GitHub token,"
Write-Host "then double-click 'LOS-LMS Server.exe'. No database or configuration to install."
