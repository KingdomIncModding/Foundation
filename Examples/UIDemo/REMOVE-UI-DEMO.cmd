@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Remove-UIDemo.ps1"
set "code=%ERRORLEVEL%"
pause
exit /b %code%
