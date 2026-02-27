@echo off
setlocal

echo === UltimateApp v5 Build ===
echo.

set SOLUTION_DIR=%~dp0UltimateApp
set CONFIG=Release

if "%1"=="Debug" set CONFIG=Debug
if "%1"=="debug" set CONFIG=Debug

echo [1/4] Przywracanie pakietow NuGet (pakietów NuGet)...
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

echo [4/4] Uruchamianie testow jednostkowych (testów)...
dotnet test "%SOLUTION_DIR%\tests\UltimateApp.Tests\UltimateApp.Tests.csproj" -c %CONFIG% --no-build --verbosity normal
if %ERRORLEVEL% neq 0 (echo BLAD: Testy nieudane. & exit /b 1)

echo.
echo === Build zakonczony sukcesem ===
endlocal
