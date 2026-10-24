# Complete first-time setup for a Windows development machine.
# Creates the local app folders the app expects, and checks the
# toolchain via bootstrap-windows.ps1. Run in a Developer PowerShell.
$ErrorActionPreference = "Stop"

.\scripts\bootstrap-windows.ps1

$base = Join-Path $env:LOCALAPPDATA "LiveWallpaperStudio"
foreach ($folder in @("media", "downloads", "cache", "logs")) {
    $path = Join-Path $base $folder
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        Write-Host "Created: $path"
    } else {
        Write-Host "Exists: $path"
    }
}

$catalog = Join-Path $base "catalog.json"
if (-not (Test-Path $catalog) -and (Test-Path "assets\catalog.json")) {
    Copy-Item "assets\catalog.json" -Destination $catalog
    Write-Host "Copied bundled catalog to: $catalog"
}

Write-Host "Setup complete. Next: .\scripts\build-desktop.ps1"
