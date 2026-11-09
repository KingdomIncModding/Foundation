@echo off
setlocal EnableExtensions
cd /d "%~dp0"

rem Build Foundation from this repository without changing the game by default.
rem Usage: BUILD-FOUNDATION.cmd [-Install] [-SkipPackaging]

set "GAME_ROOT="

rem Detect an installation when the repository is in KingdomInc\_Mods\Foundation.
for %%I in ("%~dp0..\..") do set "CANDIDATE=%%~fI"
if exist "%CANDIDATE%\KingdomInc_Data\Managed\Assembly-CSharp.dll" set "GAME_ROOT=%CANDIDATE%"

rem Known Steam installation; also works when the repository is cloned elsewhere.
if not defined GAME_ROOT if exist "G:\Program Files (x86)\Steam\steamapps\common\KingdomInc\KingdomInc_Data\Managed\Assembly-CSharp.dll" set "GAME_ROOT=G:\Program Files (x86)\Steam\steamapps\common\KingdomInc"

if not defined GAME_ROOT (
    echo Enter the full Kingdom Inc game directory.
    set /p "GAME_ROOT=Game directory: "
)

if not exist "%GAME_ROOT%\KingdomInc_Data\Managed\Assembly-CSharp.dll" (
    echo [ERROR] Could not find KingdomInc_Data\Managed\Assembly-CSharp.dll in:
    echo %GAME_ROOT%
    set "EXIT_CODE=1"
    goto finish
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] The .NET SDK is not available on PATH.
    set "EXIT_CODE=1"
    goto finish
)

echo.
echo Foundation source: %~dp0
echo Kingdom Inc:      %GAME_ROOT%
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Build\Build-Foundation.ps1" -GameRoot "%GAME_ROOT%" %*
set "EXIT_CODE=%ERRORLEVEL%"

:finish
echo.
if "%EXIT_CODE%"=="0" (
    echo [SUCCESS] Foundation build completed.
) else (
    echo [FAILED] Foundation build exited with code %EXIT_CODE%.
)
pause
exit /b %EXIT_CODE%
