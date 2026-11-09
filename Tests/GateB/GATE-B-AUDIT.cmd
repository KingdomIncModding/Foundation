@echo off
setlocal
cd /d "%~dp0..\.."
echo Foundation Gate B read-only source and compatibility audit.
echo This does NOT build, patch, or run the game.
echo.
where python >nul 2>&1
if errorlevel 1 (
  echo [FAIL] Python 3 required for Gate B source audit.
  pause
  exit /b 1
)
python "%~dp0verify_gate_b.py" --report "%~dp0..\..\Verification\GateB\gate-b-source-audit.json"
set "rc=%errorlevel%"
if not "%rc%"=="0" echo [FAIL] Review Docs\GateB and Verification\GateB\gate-b-source-audit.json
if "%rc%"=="0" echo [PASS] Source-only Gate B checks. Runtime acceptance still separate.
pause
exit /b %rc%
