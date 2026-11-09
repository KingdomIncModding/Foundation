@echo off
setlocal
cd /d "%~dp0"
echo Foundation Stage B1-B6: One-Shot and Native UI Verification
:menu
echo.
echo 1 - Check requirements
 echo 2 - Build and install verifier + visible HUD UI test
 echo 3 - Read one-shot and on-screen UI interaction reports
 echo 4 - Remove verifier DLL and test UI
 echo 0 - Exit
set /p CHOICE=Choose: 
if "%CHOICE%"=="0" exit /b 0
if "%CHOICE%"=="1" set ACTION=Preflight
if "%CHOICE%"=="2" set ACTION=BuildInstall
if "%CHOICE%"=="3" set ACTION=Report
if "%CHOICE%"=="4" set ACTION=Remove
if not defined ACTION goto menu
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-StageBVerification.ps1" -Action %ACTION%
set "ACTION="
pause
goto menu
