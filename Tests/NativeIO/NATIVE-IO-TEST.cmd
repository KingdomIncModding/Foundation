@echo off
setlocal EnableExtensions
cd /d "%~dp0"
:menu
cls
echo FOUNDATION NATIVE IO / THROUGHPUT VERIFICATION
 echo.
echo 1 Preflight    2 Build + Install test    3 Read Report    4 Remove Test    Q Quit
set /p op=Choice: 
if /I "%op%"=="Q" exit /b 0
if "%op%"=="1" set "action=Preflight"
if "%op%"=="2" set "action=BuildInstall"
if "%op%"=="3" set "action=Report"
if "%op%"=="4" set "action=Remove"
if not defined action goto menu
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-NativeIOVerification.ps1" -Action %action%
set "action="
echo.
pause
goto menu
