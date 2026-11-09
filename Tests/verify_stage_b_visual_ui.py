#!/usr/bin/env python3
"""Static guards for the Stage B test-button regression, not a Unity visual test."""
from pathlib import Path
import xml.etree.ElementTree as ET
import sys,zipfile
root=Path(__file__).resolve().parents[1]
visual=(root/'Tests/StageB/StageBVerificationPlugin.Visual.cs').read_text()
smoke=(root/'Tests/StageB/StageBVerificationPlugin.cs').read_text()
ps=(root/'Tests/StageB/Invoke-StageBVerification.ps1').read_text()
csproj=(root/'Tests/StageB/Foundation.StageBVerification.csproj').read_text()
doc=(root/'Docs/StageB/StageB-Visual-UI-Test-20261109.md').read_text()
checks={
    'single-test-dll': 'Foundation.StageBVerification' in csproj and 'class StageBVerificationPlugin' in smoke,
    'partial-type': 'partial class StageBVerificationPlugin : BaseUnityPlugin' in smoke,
    'visual-probe-awake': 'InitializeVisualProbe();' in smoke and 'StartCoroutine(Once());' in smoke,
    'real-native-bar-button': 'new ModBarButton(' in visual and 'STAGE B UI TEST' in visual,
    'real-native-bar-toggle': 'new ModBarToggle(' in visual and 'Test Toggle' in visual,
    'real-native-bar-dropdown': 'new ModBarDropdown(' in visual and 'Choose option' in visual,
    'real-native-separator': 'new ModBarSeparator(' in visual,
    'real-native-panel': 'FoundationApi.Panels.Open(Owner, VisualPanelId)' in visual and 'BuildVisualPanel' in visual,
    'user-panel-button': 'ui.Button("CLICK ME - TEST BUTTON"' in visual,
    'user-panel-checkbox': 'ui.Toggle("TEST CHECKBOX"' in visual,
    'user-panel-dropdown': 'ui.Dropdown("TEST DROPDOWN"' in visual,
    'user-panel-input': 'ui.Input("TEST TEXT FIELD"' in visual,
    'manual-signoff': 'ui.Button("I CHECKED THE VISUAL LAYOUT"' in visual,
    'input-commit-required': 'panelInputChanged' in visual,
    'callback-driven': 'WriteVisualReport();' in visual and 'void Update()' not in visual,
    'no-fake-pass': 'visualUserReviewed && barHierarchyVerified' in visual,
    'check-active-hud': 'root.gameObject.activeInHierarchy' in visual,
    'keep-registrations-alive': 'visualItems.Add(FoundationApi.ModBar.Register(' in visual and 'visualItems[i].Dispose();' in visual and 'private void OnDestroy()' in visual,
    'one-shot-preserved': 'IEnumerator Once()' in smoke and 'void Update()' not in smoke,
    'separate-reports': 'stage-b-visual-verification.json' in visual and 'stage-b-visual-verification.json' in ps,
    'report-version-sha': 'foundationSha256' in visual and 'foundationVersion' in visual and '$visual.foundationSha256 -eq $hash' in ps,
    'stale-data-guard': '(Get-Item $visualReport).LastWriteTimeUtc -gt (Get-Item $plugin).LastWriteTimeUtc' in ps,
    'pending-not-overridden': "if ($check.id -eq 'B4.RenderingVisualAudit' -or $check.id -eq 'B6.ModBarNativeVisual')" in ps,
    'other-pending-preserved': all(x in smoke for x in ['B2.NativeRegistrationSaveRoundTrip','B5.ConsoleKeyboardF8','B6.MissingTechRoundTrip','B6.WorkspaceNodeSaveLoad','B6.NativePatchConflictCleanup']),
    'installer-no-old-verifiers': 'Other verification DLLs are installed' in ps,
    'manual-guidance': 'STAGE B UI TEST' in doc and 'five' in doc,
    'compile-required-refs': all(n in csproj for n in ['UnityEngine.UI.dll','UnityEngine.UIModule.dll','Unity.TextMeshPro.dll']),
    'proj-parses-xml': ET.fromstring(csproj) is not None,
    'no-prod-modification-claimed': 'The production DLL is unchanged' in doc,
}
for key,ok in checks.items(): print(('PASS' if ok else 'FAIL'),key)
print('Stage B visual smoke source checks:',len(checks),'total,',sum(checks.values()),'passed')
if not all(checks.values()): sys.exit(1)
