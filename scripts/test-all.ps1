# Run all tests that can run on the current machine (M0/M1).
# On Windows CI this is the same sequence as desktop-build.yml.
$ErrorActionPreference = "Stop"

dotnet build src/desktop/LiveWallpaperStudio.Core/LiveWallpaperStudio.Core.csproj -c Release
dotnet run --project tests/Core.Tests/Core.Tests.csproj -c Release

cmake -S src/renderer -B build/renderer -DCMAKE_BUILD_TYPE=Release
cmake --build build/renderer --config Release
.\build\renderer\Release\lws-renderer.exe

Write-Host "All local tests passed."
