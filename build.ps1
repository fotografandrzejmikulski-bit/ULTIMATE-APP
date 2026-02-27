<#
.SYNOPSIS
    Lokalny skrypt budowania UltimateApp v5 (Windows PowerShell).
.DESCRIPTION
    Buduje i testuje wszystkie projekty w UltimateApp/.
    Opcjonalnie publikuje aplikację do folderu artifacts/ lub tworzy instalator.
.PARAMETER Configuration
    Debug lub Release (domyślnie Release).
.PARAMETER Publish
    Jeśli podany, publikuje aplikację (framework-dependent) do artifacts/.
.PARAMETER Installer
    Jeśli podany, tworzy plik instalacyjny Setup.exe przez Inno Setup.
    Wymaga zainstalowanego Inno Setup 6: https://jrsoftware.org/isdl.php
.PARAMETER OutputDir
    Ścieżka do folderu, w którym pojawi się plik Setup.exe (tylko z -Installer).
    Domyślnie: artifacts\installer\ w katalogu repozytorium.
    Przykład: -OutputDir "C:\Moje\Pliki" → C:\Moje\Pliki\UltimateApp-v5-Setup-x64.exe
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Configuration Debug
    .\build.ps1 -Publish
    .\build.ps1 -Installer
    .\build.ps1 -Installer -OutputDir "C:\Users\Andrzej\Desktop"
    .\build.ps1 -Installer -Configuration Release
#>
param(
    [ValidateSet("Debug","Release")]
    [string]$Configuration = "Release",
    [switch]$Publish,
    [switch]$Installer,
    [string]$OutputDir = ""
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
    $PublishOutputDir = Join-Path $PSScriptRoot "artifacts\UltimateApp-win-x64"
    dotnet publish "$SolutionDir\src\UltimateApp.Presentation\UltimateApp.Presentation.csproj" `
        -c $Configuration -r win-x64 --self-contained false -o $PublishOutputDir
    if ($LASTEXITCODE -ne 0) { throw "Publish nieudany." }
    Write-Host "Opublikowano do: $PublishOutputDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Build zakończony sukcesem ===" -ForegroundColor Green

# -------------------------------------------------------
# Plik instalacyjny (Inno Setup)
# -------------------------------------------------------
if ($Installer) {
    Write-Host ""
    Write-Host "[+] Budowanie pliku instalacyjnego (Inno Setup)..." -ForegroundColor Yellow

    $SelfContainedDir = Join-Path $PSScriptRoot "artifacts\UltimateApp-win-x64-selfcontained"
    # Użyj -OutputDir jeśli podany, w przeciwnym razie domyślny artifacts\installer
    if ($OutputDir -ne "") {
        $InstallerDir = $OutputDir
    } else {
        $InstallerDir = Join-Path $PSScriptRoot "artifacts\installer"
    }
    $IssFile = Join-Path $SolutionDir "installer\UltimateApp.iss"

    # Publikowanie self-contained (włącznie z .NET runtime i Windows App SDK)
    Write-Host "    Publikowanie self-contained x64..." -ForegroundColor Gray
    dotnet publish "$SolutionDir\src\UltimateApp.Presentation\UltimateApp.Presentation.csproj" `
        -c $Configuration -r win-x64 --self-contained true `
        -p:WindowsAppSDKSelfContained=true -o $SelfContainedDir
    if ($LASTEXITCODE -ne 0) { throw "Publish self-contained nieudany." }

    New-Item -ItemType Directory -Force -Path $InstallerDir | Out-Null

    # Znajdź ISCC.exe
    $IsccCandidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    $Iscc = $IsccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $Iscc) {
        Write-Warning "Inno Setup 6 nie znaleziony. Pobierz ze strony: https://jrsoftware.org/isdl.php"
        throw "ISCC.exe nie znaleziony."
    }

    Write-Host "    Kompilowanie instalatora: $Iscc" -ForegroundColor Gray
    & $Iscc "/DSourceDir=$SelfContainedDir" "/DOutputDir=$InstallerDir" "$IssFile"
    if ($LASTEXITCODE -ne 0) { throw "Budowanie instalatora nieudane." }

    $SetupExe = Join-Path $InstallerDir "UltimateApp-v5-Setup-x64.exe"
    Write-Host ""
    Write-Host "Instalator gotowy: $SetupExe" -ForegroundColor Green
}
