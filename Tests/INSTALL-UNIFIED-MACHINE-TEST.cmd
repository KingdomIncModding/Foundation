@echo off
setlocal
cd /d "%~dp0.."
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-UnifiedMachineVerification.ps1" %*
set "code=%errorlevel%"
if not "%code%"=="0" echo Verification test installation FAILED.
pause
exit /b %code%
