@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\..\Tests\Read-UISmokeReport.ps1"
set "code=%ERRORLEVEL%"
if not "%code%"=="0" echo [FAIL] Report inspection failed.
pause
exit /b %code%
