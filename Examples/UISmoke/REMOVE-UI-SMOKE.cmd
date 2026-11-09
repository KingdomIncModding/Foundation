@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Remove-UISmoke.ps1"
set "code=%ERRORLEVEL%"
if not "%code%"=="0" echo [FAIL] Screenshot smoke removal failed.
pause
exit /b %code%
