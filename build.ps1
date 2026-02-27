<#
.SYNOPSIS
    Lokalny skrypt budowania UltimateApp v5 (Windows PowerShell).
.DESCRIPTION
    Buduje i testuje wszystkie projekty w UltimateApp/.
    Opcjonalnie publikuje aplikację do folderu artifacts/.
.PARAMETER Configuration
    Debug lub Release (domyślnie Release).
.PARAMETER Publish
    Jeśli podany, publikuje aplikację do artifacts/.
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Configuration Debug
    .\build.ps1 -Publish
#>
param(
    [ValidateSet("Debug","Release")]
    [string]$Configuration = "Release",
    [switch]$Publish
)

$ErrorActionPreference = "Stop"
$SolutionDir = Join-Path $PSScriptRoot "UltimateApp"

Write-Host "=== UltimateApp v5 Build ===" -ForegroundColor Cyan
Write-Host "Konfiguracja: $Configuration"
Write-Host "Katalog solutionu: $SolutionDir"
Write-Host ""

# Restore
Write-Host "[1/4] Przywracanie pakietów NuGet..." -ForegroundColor Yellow
dotnet restore "$SolutionDir\UltimateApp.sln"
if ($LASTEXITCODE -ne 0) { throw "Restore nieudany." }

# Build cross-platform
Write-Host "[2/4] Budowanie Application + Infrastructure + Tests..." -ForegroundColor Yellow
dotnet build "$SolutionDir\src\UltimateApp.Application\UltimateApp.Application.csproj" -c $Configuration --no-restore
dotnet build "$SolutionDir\src\UltimateApp.Infrastructure\UltimateApp.Infrastructure.csproj" -c $Configuration --no-restore
dotnet build "$SolutionDir\tests\UltimateApp.Tests\UltimateApp.Tests.csproj" -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build nieudany." }

# Build Presentation
Write-Host "[3/4] Budowanie Presentation (WinUI 3, x64)..." -ForegroundColor Yellow
dotnet build "$SolutionDir\src\UltimateApp.Presentation\UltimateApp.Presentation.csproj" -c $Configuration -r win-x64 --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build Presentation nieudany." }

# Tests
Write-Host "[4/4] Uruchamianie testów jednostkowych..." -ForegroundColor Yellow
dotnet test "$SolutionDir\tests\UltimateApp.Tests\UltimateApp.Tests.csproj" -c $Configuration --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) { throw "Testy nieudane." }

# Publish
if ($Publish) {
    Write-Host "" 
    Write-Host "[+] Publikowanie aplikacji..." -ForegroundColor Yellow
    $OutputDir = Join-Path $PSScriptRoot "artifacts\UltimateApp-win-x64"
    dotnet publish "$SolutionDir\src\UltimateApp.Presentation\UltimateApp.Presentation.csproj" `
        -c $Configuration -r win-x64 --self-contained false -o $OutputDir
    if ($LASTEXITCODE -ne 0) { throw "Publish nieudany." }
    Write-Host "Opublikowano do: $OutputDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Build zakończony sukcesem ===" -ForegroundColor Green
