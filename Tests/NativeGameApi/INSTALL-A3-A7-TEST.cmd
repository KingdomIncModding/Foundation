@echo off
setlocal EnableExtensions
cd /d "%~dp0"
echo FOUNDATION 0.10.5 GAME API A3-A7 SOURCE + VERIFIER
echo.
echo Building installing unified Foundation against your INSTALLED Kingdom Inc...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\..\Tools\Install\Install-Foundation.ps1" -UseInstalledBepInExOnly
if errorlevel 1 goto fail
echo.
echo Compiling separate DEVELOPMENT ONLY verifier...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-NativeGameApiVerification.ps1" -Action BuildInstall
if errorlevel 1 goto fail
echo.
echo [PASS] Foundation and the independent test installed. Launch Kingdom Inc.
echo Use a disposable kingdom and exercise crafting, quests, research, world switching, menus, equipment, etc.
echo Then run GAME-API-TEST.cmd option 3 to inspect the result.
pause
exit /b 0
:fail
echo.
echo [FAIL] Installation failed. Do NOT assume verification passed.
pause
exit /b 1
