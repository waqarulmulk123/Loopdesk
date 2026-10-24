# Windows build script. Run on Windows, not on Linux.
# Requires: .NET 10 SDK, CMake, and an MSVC developer environment.
$ErrorActionPreference = "Stop"

dotnet build src/desktop/LiveWallpaperStudio.Core/LiveWallpaperStudio.Core.csproj -c Release
dotnet build src/desktop/LiveWallpaperStudio.App/LiveWallpaperStudio.App.csproj -c Release -p:Platform=x64
dotnet run --project tests/Core.Tests/Core.Tests.csproj -c Release

cmake -S src/renderer -B build/renderer -DCMAKE_BUILD_TYPE=Release
cmake --build build/renderer --config Release

# The supervisor looks for lws-renderer.exe next to the app output. Use the
# target framework's Release output, not an arbitrary historical build folder.
$appOut = Join-Path (Get-Location) "src\desktop\LiveWallpaperStudio.App\bin\x64\Release\net10.0-windows10.0.26100.0"
if ($appOut) {
    $renderer = Get-ChildItem -Path "build\renderer" -Recurse -Filter "lws-renderer.exe" | Select-Object -First 1 -ExpandProperty FullName
    if (-not $renderer) { throw "Renderer build completed but lws-renderer.exe was not found." }
    Copy-Item $renderer -Destination $appOut -Force
    Write-Host "Copied renderer next to app output: $appOut"
}

Write-Host "Desktop build OK"
