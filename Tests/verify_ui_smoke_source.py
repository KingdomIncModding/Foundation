#!/usr/bin/env python3
"""Static contract checks for optional UI smoke+screenshots. Not a compile or runtime pass."""
from pathlib import Path
import xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[1]
p = root/'Examples/UISmoke/FoundationUISmokePlugin.cs'
s = p.read_text(encoding='utf-8-sig')
csproj = (root/'Examples/UISmoke/Foundation.UISmoke.csproj').read_text(encoding='utf-8-sig')
ET.fromstring(csproj)
assert 'JsonUtility.ToJson' not in s, 'UI smoke manifest cannot depend on an unreferenced Unity JSON module'
assert 'SerializeReport(report)' in s and 'AppendJsonString' in s and 'new UTF8Encoding(false)' in s
assert 'UnityEngine.JSONSerializeModule' not in csproj, 'Keep the smoke exporter independent of Unity JSONSerializeModule'
assert 'InspectFoundationTextLayout(root, result);' in s and 'Potential text overlap:' in s and 'VisualIssues' in s
assert all(field in s for field in ('"StartedUtc"', '"FinishedUtc"', '"ScreenshotFolder"', '"Rendered"', '"Interacted"'))
assert '\\"Cases\\"' in s and '\\"Screenshots\\"' in s
required = ('header','text','button','toggle','slider','number','input','dropdown','search',
            'colors','signal-source','signal-property','tabs','columns','section','reorder','confirm','confirm-cancel',
            'property','live','tooltip','scroll')
assert all(f'Add("{key}"' in s for key in required)
settings = ('text','button','toggle','slider','dropdown','number','input','search','colors','signal')
assert all(f'AddSettings("{key}"' in s for key in settings)
assert 'ReadPixels(' in s and 'ImageConversion.EncodeToPNG(' in s and 'SaveCrop(' in s
assert 'WaitForEndOfFrame' in s and 'ui-smoke-results.json' in s
assert 'FoundationApi.Panels.OpenMachine(item)' in s and 'FoundationMachineApi.Registry.OnFlow(' in s
assert 'config.ShowModsTab(spec.Id)' in s and 'config.ShowInstalledTab()' in s and 'menu.DoResume()' in s
assert 'FoundationApi.ModConfig.RemoveSection(section.Id)' in s
assert '[FOUNDATION UI SMOKE] FINISH' in s
# The Unity Button's Selectable component also implements IPointerEnterHandler.
# Probe FoundationPanelTooltip specifically, not the first unrelated handler.
assert 'h.GetType().Name == "FoundationPanelTooltip"' in s, 'Tooltip probe must select Foundation hover behavior'
assert 'FirstOrDefault();' not in s[s.index('case "tooltip":'):s.index('case "scroll":')], 'Tooltip probe must not select first arbitrary handler'
assert all(t in csproj for t in ('<Reference Include="Foundation"','Unity.TextMeshPro','UnityEngine.UI','UnityEngine.UIModule','UnityEngine.ImageConversionModule','<AssemblyName>Foundation.UISmoke</AssemblyName>'))
runner = (root/'Tests/Invoke-UIVerification.ps1').read_text(encoding='utf-8-sig')
assert all(f"  '{i}'" in runner for i in ('7','8','9'))
reader = (root/'Tests/Read-UISmokeReport.ps1').read_text(encoding='utf-8-sig')
assert 'CreateFromDirectory' in reader and 'ui-smoke-results.json' in reader and '.zip' in reader
print(f'[PASS] UI screenshot harness contract checks: {len(required)} panel controls, {len(settings)} settings elements, native machine snapshots, PNG readback, JSON manifest, ZIP report, safe cleanup.')
print('[INFO] Source assertions only; Windows C# compilation and Unity screenshot test are still required.')

# Regression for native Mod Settings signal selector caption overflow,
# and for missing native art silently falling back to flat controls.
assert 'InspectModSettingsSignalLayout(optionsRoot, result)' in s
assert 'Native decorative panel sprite unavailable' in s
assert 'Native button sprite unavailable' in s
assert 'Visual layout defect:' in s and 'result.VisualIssues.Count == 0' in s
selectors = (root/'src/Foundation/UI/Settings/ModConfigSignalSelector.cs').read_text(encoding='utf-8-sig')
assert 'sourceLabels.Add(FriendlyCaption(name, "Signal Source", 28 - suffix.Length) + suffix);' in selectors
assert 'labels.Add(FriendlyCaption(name, "Property", 28 - suffix.Length) + suffix);' in selectors
assert 'sourceLabels.Add(source.Name + " [" + source.Id + "]")' not in selectors
templates = (root/'src/Foundation/UI/Settings/ModConfigTemplates.cs').read_text(encoding='utf-8-sig')
assert 'dropdown.captionText.textWrappingMode = TextWrappingModes.NoWrap' in templates
assert 'dropdown.captionText.overflowMode = TextOverflowModes.Ellipsis' in templates

# Four screenshot defects that escaped 2026-11-09 09:07 smoke report.
assert 'InspectModSettingsControlLayout(optionsRoot, spec.Title, result);' in s
assert 'InspectFoundationToggleLayout(root, result);' in s
assert 'Native Options input text crowds decorative border' in s
assert 'Foundation panel checkbox art is too small' in s
assert 'Mod Settings signal caption is too long' in s
assert 'result.VisualIssues.Count == 0 ? "PASS" : "FAIL"' in s
templates = (root/'src/Foundation/UI/Settings/ModConfigTemplates.cs').read_text(encoding='utf-8-sig')
assert 'FoundationGameTheme.ApplyWell(image);' in templates
assert 'viewportRect.offsetMin = new Vector2(20, 5)' in templates
assert 'text.textWrappingMode = TextWrappingModes.NoWrap;' in templates
context = (root/'src/Foundation/UI/ModBar/ModBarContext.cs').read_text(encoding='utf-8-sig')
assert 'nativeCheckbox.sizeDelta = new Vector2(26f, 26f)' in context
assert 'labelRect.offsetMin = new Vector2(56f, 2f)' in context
assert 'Native Options checkbox' in s
print('[PASS] Four user-identified visual regressions have source-level guards.')
