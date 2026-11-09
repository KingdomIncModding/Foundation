@echo off
setlocal
cd /d "%~dp0"
echo.
echo FOUNDATION UI 0.10.4 - UNIFIED CORE + MACHINES + UI
:menu
echo 1 Build only  ^| 2 Build and install Foundation  ^| 3 Install UI demo  ^| 4 Inspect UI logs  ^| 5 Remove demo  ^| 6 Isolated smoke  ^| 7 Install screenshot smoke  ^| 8 Report + ZIP screenshots  ^| 9 Remove screenshot smoke  ^| Q Quit
set /p choice=Choice: 
if /i "%choice%"=="Q" exit /b 0
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tests\Invoke-UIVerification.ps1" -Task "%choice%"
if errorlevel 1 echo [FAIL] Task returned an error.
pause
goto menu
