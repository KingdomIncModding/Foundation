@echo off
setlocal EnableExtensions
set "GAME_ROOT=G:\Program Files (x86)\Steam\steamapps\common\KingdomInc"
set "BUILD_SCRIPT=%~dp0Build-Foundation.ps1"
title Kingdom Inc Foundation - Build

echo.
echo ========================================
echo          FOUNDATION BUILD
echo ========================================
echo.

if not exist "%BUILD_SCRIPT%" (
    echo ERROR: Build-Foundation.ps1 is missing.
    echo Expected: "%BUILD_SCRIPT%"
    set "BUILD_RESULT=1"
    goto end
)

if not exist "%GAME_ROOT%\KingdomInc_Data\Managed\Assembly-CSharp.dll" (
    echo ERROR: Kingdom Inc game assemblies not found.
    echo Checked: "%GAME_ROOT%"
    set "BUILD_RESULT=1"
    goto end
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found. Install the .NET SDK and restart this window.
    set "BUILD_RESULT=1"
    goto end
)

echo Source: "%BUILD_SCRIPT%"
echo Game:   "%GAME_ROOT%"
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%" -GameRoot "%GAME_ROOT%" %*
set "BUILD_RESULT=%ERRORLEVEL%"

:end
echo.
if "%BUILD_RESULT%"=="0" (echo BUILD COMPLETED) else echo BUILD FAILED - exit code %BUILD_RESULT%
echo.
pause
exit /b %BUILD_RESULT%
