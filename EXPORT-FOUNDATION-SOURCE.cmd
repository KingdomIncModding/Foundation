@echo off
setlocal
rem Run from this script's own folder. Do NOT pass %%~dp0 as a quoted argument:
rem the trailing backslash can escape a closing quote in Windows native argv.
pushd "%~dp0"
if errorlevel 1 (
    echo [FAIL] Cannot enter exporter folder.
    pause
    exit /b 1
)
if not exist "Foundation.csproj" (
    echo [FAIL] Place both exporter files beside Foundation.csproj in the Foundation source root.
    echo [INFO] Current folder: "%CD%"
    popd
    pause
    exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0EXPORT-FOUNDATION-SOURCE.ps1"
set "rc=%errorlevel%"
popd
if not "%rc%"=="0" (
    echo.
    echo [FAIL] Source export did not complete. Exit code: %rc%
    pause
    exit /b %rc%
)
echo.
echo [PASS] Source export complete.
pause
