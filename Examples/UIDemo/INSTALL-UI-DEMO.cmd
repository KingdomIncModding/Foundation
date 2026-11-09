@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-UIDemo.ps1"
set "code=%ERRORLEVEL%"
if not "%code%"=="0" echo [FAIL] UI demo build/installation failed.
pause
exit /b %code%
