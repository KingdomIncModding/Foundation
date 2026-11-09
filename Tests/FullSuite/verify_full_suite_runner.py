#!/usr/bin/env python3
"""Offline source guards for full-suite runner; not a Windows/Unity runtime test."""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[2]
runner=(root/'Tests/FullSuite/Run-FoundationFullSuite.ps1').read_text()
cmd=(root/'RUN-ALL-TESTS.cmd').read_text()
console=(root/'src/Foundation/Bootstrap/FoundationPlugin.FoundationDeveloperConsole.cs').read_text(encoding='utf-8-sig')
visual=(root/'Tests/StageB/StageBVerificationPlugin.Visual.cs').read_text()
checks={
 'all-five-verifiers':all(x in runner for x in ['Invoke-NativeMachinesVerification.ps1','Invoke-NativeIOVerification.ps1','Invoke-NativeGameApiVerification.ps1','Invoke-StageB1Verification.ps1','Invoke-StageBVerification.ps1']),
 'installs-one-at-time':'Assert-NoConcurrentVerifiers' in runner and "-Action','Remove'" in runner,
 'no-fake-pass':'PENDING' in runner and "status='FAIL'" in runner and 'Read-PhaseChecks' in runner,
 'real-runtime-checks':'foundationSha256' in runner and 'foundationVersion' in runner and "-Action','Report'" in runner,
 'single-report':'summary.json' in runner and 'summary.md' in runner and 'latest.txt' in runner,
 'source-checks':'Run-StaticChecks' in runner and 'verify_stage_b_all.py' in runner,
 'gate-b-source-audit':'GateB/verify_gate_b.py' in runner,
 'developer-console-disabled-by-default':'Config.Bind("Developer", "EnableConsole", false' in console,
 'f8-log-only-when-toggled':'[FOUNDATION CONSOLE] F8 toggled' in console and 'Keyboard.current.f8Key.wasPressedThisFrame' in console,
 'temporary-enable':'EnableConsole = true' in runner and 'Prepare-ConsoleConfig' in runner,
 'exact-config-restore':'Copy-Item -LiteralPath $script:configBackup -Destination $configPath -Force' in runner and 'Restore-ConsoleConfig' in runner,
 'recovery':'Recover-ConsoleConfig' in runner and '-Action Cleanup' in cmd,
 'f8-evidence':'Get-F8LogEvidence' in runner and 'ConsoleKeyboardF8.Manual' in runner and 'Did F8 OPEN' in runner,
 'real-hud-ui':'STAGE B UI TEST' in visual and 'new ModBarButton(' in visual,
 'compile-fix-path':'BepInEx.Paths.GameRootPath' in visual,
 'compile-fix-overload':'Owner, "bar-separator", 14f, -497, ModBarAlignment.Left' in visual,
 'launch-steam-or-manual':'Get-SteamAppId' in runner and 'steam://rungameid/' in runner and 'Launch Kingdom Inc normally through Steam' in runner,
 'no-save-mutations':'-foundation-gameapi-mutate' not in runner and '-foundation-nativeio-mutate' not in runner,
 'one-root-launcher':r'Tests\FullSuite\Run-FoundationFullSuite.ps1' in cmd,
 'powershell-boolean-literal':'$script:abort=$false' in runner and '$script:abort=false' not in runner,
 'nested-static-log-sanitized':"$safeName=($test -replace" in runner and "'static-'+$safeName+'.log'" in runner,
 'fresh-verifier-report':'StaleReport' in runner and '$script:sessionStartedUtc' in runner and '$installTime' in runner,
 'recovery-before-latest-overwrite':runner.index('Recover-ConsoleConfig\nRequire-Game') < runner.index("'run-'+(Get-Date"),
 'fresh-visual-report':'B4.StaleInteractiveReport' in runner,
 'visual-status-whitelist':"if ($visualStatus -notin @('PASS','FAIL','PENDING'))" in runner,

}
for k,v in checks.items(): print(('PASS' if v else 'FAIL'),k)
print('FullSuite source checks',sum(checks.values()),'/',len(checks))
sys.exit(0 if all(checks.values()) else 1)
