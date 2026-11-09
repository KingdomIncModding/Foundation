@echo off
setlocal EnableExtensions
cd /d "%~dp0"
echo FOUNDATION 0.10.4 SOURCE + NATIVE IO VERIFIER
echo.
echo Building/installing Foundation against your INSTALLED Kingdom Inc and BepInEx...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\..\Tools\Install\Install-Foundation.ps1" -UseInstalledBepInExOnly
if errorlevel 1 goto fail
echo.
echo Compiling separate DEVELOPMENT-ONLY verifier against freshly installed Foundation...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-NativeIOVerification.ps1" -Action BuildInstall
if errorlevel 1 goto fail
echo.
echo [PASS] Installation completed. Open Kingdom Inc through Steam.
echo Load a disposable kingdom, exercise Collector/Distributor/Manifold, connect/remove pipes, transfer items, and return to menu.
echo Then run NATIVE-IO-TEST.cmd, option 3 to read the report.
pause
exit /b 0
:fail
echo.
echo [FAIL] One-step install failed. Do NOT assume the verifier or Foundation was installed.
pause
exit /b 1
