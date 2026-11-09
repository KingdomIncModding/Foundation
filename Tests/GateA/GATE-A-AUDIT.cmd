@echo off
setlocal
set "ROOT=%~dp0..\.."
echo === Foundation Gate A source/static checks (read-only) ===
where python >nul 2>&1
if %errorlevel%==0 (
  python "%~dp0verify_gate_a.py"
  if errorlevel 1 (echo [FAIL] Gate A source checks.& exit /b 1)
  python "%~dp0export_rewrite_inventory.py"
  if errorlevel 1 (echo [FAIL] Stage B inventory.& exit /b 1)
) else (
  echo [INFO] Python unavailable; source audit skipped. Existing Foundation build not affected.
)
echo === Installed DLL hashes and optional preserved verifier reports ===
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Collect-GateA-Evidence.ps1" %*
if errorlevel 1 exit /b 1
echo.
echo No tests were installed and no game state was changed.
endlocal
