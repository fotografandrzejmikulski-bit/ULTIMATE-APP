@echo off
setlocal

echo === UltimateApp v5 Build ===
echo.

set SOLUTION_DIR=%~dp0UltimateApp
set CONFIG=Release
set BUILD_INSTALLER=0
set OUTPUT_DIR=

:parse_args
if "%1"=="Debug"        set CONFIG=Debug           & shift & goto parse_args
if "%1"=="debug"        set CONFIG=Debug           & shift & goto parse_args
if "%1"=="--installer"  set BUILD_INSTALLER=1      & shift & goto parse_args
if "%1"=="installer"    set BUILD_INSTALLER=1      & shift & goto parse_args
if "%1"=="--output-dir" set OUTPUT_DIR=%2          & shift & shift & goto parse_args

echo [1/4] Przywracanie pakietów NuGet...
dotnet restore "%SOLUTION_DIR%\UltimateApp.sln"
if %ERRORLEVEL% neq 0 (echo BLAD: Restore nieudany. & exit /b 1)

echo [2/4] Budowanie Application + Infrastructure + Tests...
dotnet build "%SOLUTION_DIR%\src\UltimateApp.Application\UltimateApp.Application.csproj" -c %CONFIG% --no-restore
dotnet build "%SOLUTION_DIR%\src\UltimateApp.Infrastructure\UltimateApp.Infrastructure.csproj" -c %CONFIG% --no-restore
dotnet build "%SOLUTION_DIR%\tests\UltimateApp.Tests\UltimateApp.Tests.csproj" -c %CONFIG% --no-restore
if %ERRORLEVEL% neq 0 (echo BLAD: Build nieudany. & exit /b 1)

echo [3/4] Budowanie Presentation (WinUI 3, x64)...
dotnet build "%SOLUTION_DIR%\src\UltimateApp.Presentation\UltimateApp.Presentation.csproj" -c %CONFIG% -r win-x64 --no-restore
if %ERRORLEVEL% neq 0 (echo BLAD: Build Presentation nieudany. & exit /b 1)

echo [4/4] Uruchamianie testów jednostkowych...
dotnet test "%SOLUTION_DIR%\tests\UltimateApp.Tests\UltimateApp.Tests.csproj" -c %CONFIG% --no-build --verbosity normal
if %ERRORLEVEL% neq 0 (echo BLAD: Testy nieudane. & exit /b 1)

echo.
echo === Build zakonczony sukcesem ===

if %BUILD_INSTALLER%==0 goto end

rem -------------------------------------------------------
rem Plik instalacyjny (Inno Setup)
rem -------------------------------------------------------
echo.
echo [+] Budowanie pliku instalacyjnego (Inno Setup)...

set SELFCONTAINED_DIR=%~dp0artifacts\UltimateApp-win-x64-selfcontained
if not "%OUTPUT_DIR%"=="" (
    set INSTALLER_DIR=%OUTPUT_DIR%
) else (
    set INSTALLER_DIR=%~dp0artifacts\installer
)
set ISS_FILE=%SOLUTION_DIR%\installer\UltimateApp.iss

echo     Publikowanie self-contained x64...
dotnet publish "%SOLUTION_DIR%\src\UltimateApp.Presentation\UltimateApp.Presentation.csproj" ^
    -c %CONFIG% -r win-x64 --self-contained true ^
    -p:WindowsAppSDKSelfContained=true -o "%SELFCONTAINED_DIR%"
if %ERRORLEVEL% neq 0 (echo BLAD: Publish self-contained nieudany. & exit /b 1)

if not exist "%INSTALLER_DIR%" mkdir "%INSTALLER_DIR%"

set ISCC=
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe
if exist "C:\Program Files\Inno Setup 6\ISCC.exe"       set ISCC=C:\Program Files\Inno Setup 6\ISCC.exe

if "%ISCC%"=="" (
    echo BLAD: Inno Setup 6 nie znaleziony.
    echo       Pobierz ze strony: https://jrsoftware.org/isdl.php
    exit /b 1
)

echo     Kompilowanie instalatora...
"%ISCC%" /DSourceDir="%SELFCONTAINED_DIR%" /DOutputDir="%INSTALLER_DIR%" "%ISS_FILE%"
if %ERRORLEVEL% neq 0 (echo BLAD: Budowanie instalatora nieudane. & exit /b 1)

echo.
echo Instalator gotowy: %INSTALLER_DIR%\UltimateApp-v5-Setup-x64.exe

:end
endlocal
