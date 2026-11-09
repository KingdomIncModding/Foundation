@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-UISmoke.ps1"
set "code=%ERRORLEVEL%"
if not "%code%"=="0" echo [FAIL] Screenshot smoke install failed.
pause
exit /b %code%
