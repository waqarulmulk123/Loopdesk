# Windows build script. Run on Windows, not on Linux.
# Requires: .NET 10 SDK, CMake, and an MSVC developer environment.
$ErrorActionPreference = "Stop"
$cmake = (Get-Command cmake -ErrorAction SilentlyContinue).Source
if (-not $cmake) {
    $cmake = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
}
if (-not (Test-Path -LiteralPath $cmake)) { throw "CMake was not found. Install CMake or Visual Studio Desktop C++ tools." }

dotnet build src/desktop/LiveWallpaperStudio.Core/LiveWallpaperStudio.Core.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Core build failed." }
dotnet build src/desktop/LiveWallpaperStudio.App/LiveWallpaperStudio.App.csproj -c Release -p:Platform=x64 -r win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true
if ($LASTEXITCODE -ne 0) { throw "App build failed." }
dotnet run --project tests/Core.Tests/Core.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Core tests failed." }

& $cmake -S src/renderer -B build/renderer -DCMAKE_BUILD_TYPE=Release
if ($LASTEXITCODE -ne 0) { throw "Renderer configuration failed." }
& $cmake --build build/renderer --config Release
if ($LASTEXITCODE -ne 0) { throw "Renderer build failed." }

# The supervisor looks for lws-renderer.exe next to the app output. Use the
# target framework's Release output, not an arbitrary historical build folder.
$appOut = Join-Path (Get-Location) "src\desktop\LiveWallpaperStudio.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64"
if ($appOut) {
    $renderer = Get-ChildItem -Path "build\renderer" -Recurse -Filter "lws-renderer.exe" | Select-Object -First 1 -ExpandProperty FullName
    if (-not $renderer) { throw "Renderer build completed but lws-renderer.exe was not found." }
    Copy-Item $renderer -Destination $appOut -Force
    Write-Host "Copied renderer next to app output: $appOut"
}

Write-Host "Desktop build OK"
