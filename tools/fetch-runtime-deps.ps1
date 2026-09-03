# Downloads and caches the two large runtime dependencies the Server package bundles:
#   - MySQL Community Server (ZIP archive, no installer)
#   - cloudflared (Cloudflare tunnel client, single exe)
#
# They are cached under .tools\ so the download happens once, not on every publish. Both publish
# scripts call this and then copy the cached copies into the package.
#
#   .\tools\fetch-runtime-deps.ps1
#
# Prints the cache paths it produced. Safe to re-run — it skips anything already cached.

$ErrorActionPreference = 'Stop'
$root      = Split-Path $PSScriptRoot -Parent
$cache     = Join-Path $root '.tools'
$mysqlVer  = '8.0.40'
$mysqlDir  = Join-Path $cache "mysql-$mysqlVer-winx64"          # extracted base dir (contains bin\mysqld.exe)
$mysqlZip  = Join-Path $cache "mysql-$mysqlVer-winx64.zip"
$cfExe     = Join-Path $cache 'cloudflared.exe'

New-Item -ItemType Directory -Path $cache -Force | Out-Null

# ---- MySQL Community ZIP ----
# The archives CDN holds every released version permanently and is the most reliable source; the
# dev.mysql.com/get redirector occasionally returns an Oracle "technical difficulties" HTML page, so
# it is only a fallback and every download is validated to actually be a ZIP before use.
$mysqlUrls = @(
    "https://cdn.mysql.com/archives/mysql-8.0/mysql-$mysqlVer-winx64.zip",
    "https://dev.mysql.com/get/Downloads/MySQL-8.0/mysql-$mysqlVer-winx64.zip"
)
if (-not (Test-Path (Join-Path $mysqlDir 'bin\mysqld.exe'))) {
    if (-not (Test-Path $mysqlZip)) {
        $ok = $false
        foreach ($url in $mysqlUrls) {
            try {
                Write-Host "Downloading MySQL $mysqlVer (~230 MB) from $url ..."
                Invoke-WebRequest -Uri $url -OutFile $mysqlZip -UseBasicParsing
                # Validate: a real ZIP starts with the bytes 'PK'. An error page does not.
                $head = [System.IO.File]::ReadAllBytes($mysqlZip)[0..1]
                if ($head[0] -eq 0x50 -and $head[1] -eq 0x4B) { $ok = $true; break }
                Write-Warning "Downloaded file is not a ZIP (server returned an error page). Trying next source."
                Remove-Item $mysqlZip -Force
            }
            catch {
                Write-Warning "Download from $url failed: $($_.Exception.Message)"
                if (Test-Path $mysqlZip) { Remove-Item $mysqlZip -Force }
            }
        }
        if (-not $ok) { throw "Could not download a valid MySQL ZIP from any source." }
    }
    Write-Host "Extracting MySQL..."
    Expand-Archive -Path $mysqlZip -DestinationPath $cache -Force
}
if (-not (Test-Path (Join-Path $mysqlDir 'bin\mysqld.exe'))) {
    throw "MySQL extraction did not produce bin\mysqld.exe under $mysqlDir"
}

# ---- cloudflared ----
if (-not (Test-Path $cfExe)) {
    $url = 'https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe'
    Write-Host "Downloading cloudflared..."
    Invoke-WebRequest -Uri $url -OutFile $cfExe -UseBasicParsing
}

Write-Host ""
Write-Host "MySQL base dir : $mysqlDir"
Write-Host "cloudflared    : $cfExe"

# Return the paths for callers that dot-source or parse.
[PSCustomObject]@{ MysqlDir = $mysqlDir; Cloudflared = $cfExe }
