@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Install\Install-Foundation.ps1" %*
set "rc=%errorlevel%"
if not "%rc%"=="0" echo Foundation install FAILED. Review the log under Verification\InstallLogs.
pause
exit /b %rc%
