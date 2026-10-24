# Bootstrap checks for a Windows development machine (M0).
# Run in a normal PowerShell window, not as admin unless a tool
# installation actually requires it. Normal app operation never
# requires admin rights.
$ErrorActionPreference = "Stop"

function Test-Command($name) {
    return $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
}

$missing = @()
if (-not (Test-Command "dotnet")) { $missing += "dotnet (.NET 10 SDK)" }
if (-not (Test-Command "cmake")) { $missing += "cmake" }
if (-not (Test-Command "git")) { $missing += "git" }

if ($missing.Count -gt 0) {
    Write-Host "Missing tools:"
    $missing | ForEach-Object { Write-Host "  - $_" }
    Write-Host ""
    Write-Host "Install: Visual Studio 2026 with desktop C++ and Windows app tooling,"
    Write-Host ".NET 10 SDK, Windows SDK, CMake, and Node.js LTS for backend work."
    Write-Host "See README.md and docs/ARCHITECTURE.md for the full list."
    exit 1
}

dotnet --version
cmake --version | Select-Object -First 1
Write-Host "Bootstrap checks OK."
