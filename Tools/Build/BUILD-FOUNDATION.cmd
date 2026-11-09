@echo off
setlocal EnableExtensions
title Foundation - Build

rem This launcher lives beside Build-Foundation.ps1.
rem Determine the KingdomInc root from Foundation\Tools\Build.
for %%I in ("%~dp0..\..\..\..") do set "GAME_ROOT=%%~fI"

if exist "%GAME_ROOT%\KingdomInc_Data\Managed\Assembly-CSharp.dll" goto build

rem Fallback for a source repository cloned away from the game folder.
set "GAME_ROOT=G:\Program Files (x86)\Steam\steamapps\common\KingdomInc"
if exist "%GAME_ROOT%\KingdomInc_Data\Managed\Assembly-CSharp.dll" goto build

echo Kingdom Inc installation not found automatically.
set /p "GAME_ROOT=Enter Kingdom Inc game folder: "
if exist "%GAME_ROOT%\KingdomInc_Data\Managed\Assembly-CSharp.dll" goto build

echo.
echo [ERROR] Kingdom Inc managed assemblies not found.
echo.
pause
exit /b 1

:build
echo.
echo Building Foundation against:
echo "%GAME_ROOT%"
echo.
if not exist "%~dp0Build-Foundation.ps1" goto missing

rem -NoExit ensures that even a PowerShell error cannot flash away.
powershell.exe -NoExit -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Foundation.ps1" -GameRoot "%GAME_ROOT%" %*
set "RC=%ERRORLEVEL%"
echo.
echo Build launcher exited with code %RC%.
pause
exit /b %RC%

:missing
echo [ERROR] Build-Foundation.ps1 not found beside this CMD file.
pause
exit /b 1
