@echo off
setlocal EnableExtensions
cd /d "%~dp0"
:menu
echo =============================================================
echo FOUNDATION FULL TEST RUNNER - A1-A7 + B1-B6
 echo One development-only test DLL installed per game session.
echo F8 console temporarily enabled for Stage B, restored afterward.
echo =============================================================
echo.
echo 1 - Run ALL tests [A1-A7, B1-B6] and collect combined report
 echo 2 - Run Stage B tests only (includes HUD controls and F8)
 echo 3 - Read latest combined report
 echo 4 - Cleanup temporary tests and restore F8 setting
 echo 5 - Preflight
 echo 0 - Exit
set "opt="
set /p opt=Choose: 
if "%opt%"=="0" exit /b 0
set "args="
if "%opt%"=="1" set "args=-Action Run -Scope All -Launch Auto"
if "%opt%"=="2" set "args=-Action Run -Scope StageB -Launch Auto"
if "%opt%"=="3" set "args=-Action Report"
if "%opt%"=="4" set "args=-Action Cleanup -Scope All"
if "%opt%"=="5" set "args=-Action Preflight -Scope All"
if not defined args goto menu
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tests\FullSuite\Run-FoundationFullSuite.ps1" %args%
set "result=%errorlevel%"
echo.
if "%result%"=="0" echo [PASS] Completed with no reported failures or pending checks.
if "%result%"=="1" echo [FAIL] See combined report and log for failed checks.
if "%result%"=="2" echo [PARTIAL] Some tests still require game interactions or native fixtures.
echo.
pause
goto menu
