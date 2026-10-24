# Package the desktop app for QA (unsigned, M5 notes only).
# Production MSIX / signed installer work is in packaging/msix/README.md
# and must not be claimed as complete until the release checklist passes.
$ErrorActionPreference = "Stop"

.\scripts\build-desktop.ps1
$publish = Join-Path (Get-Location) "artifacts\desktop\win-x64"
dotnet publish src\desktop\LiveWallpaperStudio.App\LiveWallpaperStudio.App.csproj -c Release -r win-x64 --self-contained false -o $publish
$renderer = Get-ChildItem -Path "build\renderer" -Recurse -Filter "lws-renderer.exe" | Select-Object -First 1 -ExpandProperty FullName
if (-not $renderer) { throw "Renderer executable was not found after build." }
Copy-Item $renderer -Destination $publish -Force
Write-Host "QA package created: $publish"
Write-Host "Includes the offline assets catalog and renderer executable."
Write-Host "Signing is NOT performed by this script."
