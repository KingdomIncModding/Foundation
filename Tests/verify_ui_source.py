#!/usr/bin/env python3
"""Read-only source and game-contract checks for the unified UI. NOT a C# build or Unity runtime test."""
from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
GAME = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else None
results = []
def check(label, value, details=""):
    results.append({'name':label,'result':'PASS' if value else 'FAIL','details':details})
def read(path):
    p=ROOT/path
    return p.read_text(encoding='utf-8-sig') if p.is_file() else ''

proj=read('Foundation.csproj')
try:
    ET.fromstring(proj)
    check('Unity netstandard2.1 project parses', True)
except Exception as e:check('Unity netstandard2.1 project parses', False,repr(e))
check('Unity UI assembly references retained', all(x in proj for x in ['UnityEngine.UIModule.dll','UnityEngine.UI.dll','Unity.TextMeshPro.dll']))
project_version=re.search(r'<Version>([0-9.]+)</Version>',proj).group(1)
check('One production AssemblyName', '<AssemblyName>Foundation</AssemblyName>' in proj and '<Version>'+project_version+'</Version>' in proj)

api=read('src/Foundation/Public/FoundationApi.cs')
registry=read('src/Foundation/UI/Panels/FoundationPanelRegistry.cs')
host=read('src/Foundation/UI/Panels/FoundationPanelHost.cs')
builder=read('src/Foundation/UI/Panels/FoundationPanelBuilder.cs')
natives=read('src/Foundation/UI/Panels/FoundationNativeMachinePanels.cs')
bootstrap=read('src/Foundation/Bootstrap/FoundationPlugin.FoundationPlugin.cs')
check('Public Foundation Panels API', 'public static IFoundationPanelRegistry Panels' in api)
check('Owner-scoped registry and disposal', all(x in registry for x in ['RemoveOwner','ForMod','Dispose','TryResolveMachine','TryGet']))
check('Duplicate owner ID rejected', 'Duplicate panel owner + ID' in registry)
check('Specificity before priority', registry.index('OrderBy(item => Distance(') < registry.index('ThenByDescending(item => item.Priority)'))
check('Machine open rejects unplaced native handles', 'MachineThread.IsPlaced(machine)' in registry)
check('Generic open cannot open machine-only panel', 'definition.MachineType != null' in registry)
check('Native GameUI popup owner', 'ui.TogglePopupMenu(this, true)' in host and 'UIPopupMenu' in host)
check('Native template styling', 'FoundationGameTheme.ApplyPanel(background)' in host and 'nativeText.font' in host and 'panelTheme: true' in host)
check('Solid game-themed popup backing', 'FoundationGameTheme.ApplyPanel(background)' in host and
      'FoundationGameTheme.AddPanelBorder(rect)' in host and 'AddFrameEdge(' in host)
check('No scroll carryover between panels', 'ResetScrollToTop();' in host and
      'contentScroll.verticalNormalizedPosition = 1f;' in host)
check('Panel closes for unplaced machine', 'if (!MachineThread.IsPlaced(machine)) { CloseCurrent();' in host)
check('Signal definition hot refresh', 'FoundationApi.Signals.Changed += host.OnSignalDefinitionsChanged' in host)
check('Popup cleanup', 'PopupClosed()' in host and 'ClearContent()' in host and 'OnDestroy()' in host)
check('Panel drag and persistence', 'MoveBy(PointerEventData' in host and 'SavePosition()' in host)
check('Tooltip footer native style', 'FoundationPanelTooltip' in builder and 'Tooltip(' in host)
check('Action exception isolation', 'Safe(Action handler)' in builder and 'FoundationApi.Report' in host)
check('Full catalog searchable and paged', 'pageSize = 24' in builder and 'items.Count; i++' in builder)
check('Slider input validation', 'float.IsNaN(min)' in builder and 'TMP_InputField' in builder)
# Compile regression checks for the 2026-11-09 build (CS0119/CS0165).
# These are source assertions, NOT a replacement for dotnet build on Windows.
check('No Slider method/type collision (CS0119)', 'slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;' in builder)
check('Search pager controls definitely initialized (CS0165)', all(token in builder for token in (
    'UnityEngine.UI.Button previousButton = null;',
    'UnityEngine.UI.Button nextButton = null;',
    'TMP_Text caption = null;')))
check('No Stockpile method/type collision (CS0119)', 'using StockpileNode = Assets.Source.Flow.Elements.Stockpile;' in natives and
      'StockpileNode.BaseCapacity' in natives and '(StockpileNode)ui.Machine' in natives)

check('Stageable panel controls', all(('public void '+x+'(') in builder for x in ['Text','Header','Button','Toggle','Slider','Number','Input','Dropdown','Search','Colors','Signal','Tabs','Columns','Reorder','Section','Confirm','Property','LiveText']))
check('Native signal source + property selector', 'Exposed property' in builder and 'FoundationApi.Signals.TryGet' in builder)
check('Color swatches no wheel', 'ColorSwatches' in builder and 'outline.effectColor' in builder and 'ColorWheel' not in builder)
check('Signal IDs not wrapped inside dropdown captions', 'sourceLabels.Add(source.Name + " [" + source.Id + "]")' not in builder and 'sourceLabels.Add(duplicateNames[source.Name] > 1' in builder)
check('Dynamic typed property editors', 'FoundationUiProperty' in builder and 'typeof(SignalSelection)' in builder)
check('Live telemetry throttled', 'Time.unscaledTime < nextLive' in host and 'label.Interval' in host)
check('ModConfig input, number and search', all(read('src/Foundation/UI/Settings/'+name+'.cs') for name in ('ModConfigInput','ModConfigNumber','ModConfigSearch')))
check('ModConfig staged Apply/Cancel', all('Commit()' in read('src/Foundation/UI/Settings/'+name+'.cs') and 'Cancel()' in read('src/Foundation/UI/Settings/'+name+'.cs') for name in ('ModConfigInput','ModConfigNumber','ModConfigSearch')))
check('Native machine defaults count >= 18', natives.count('Add<')>=18, str(natives.count('Add<')))
check('Native operations reused', 'FoundationMachineApi.Operations' in natives and 'operations.SetTimer' in natives)
check('Foundation initializes and uninstalls UI', all(x in bootstrap for x in ('FoundationNativeMachinePanels.Install()','FoundationNativeMachinePanels.Uninstall()','FoundationPanelHost.ResetAll()')))
check('Independent UI demo exists', bool(read('Examples/UIDemo/FoundationUIDemoPlugin.cs')) and bool(read('Examples/UIDemo/Foundation.UIDemo.csproj')))
check('UI API smoke tests present', 'VerifyFoundationPanels();' in read('src/Foundation/Bootstrap/FoundationPlugin.RuntimeCheck.cs'))
check('No new production plugin', sum('[BepInPlugin(' in p.read_text(encoding='utf-8-sig') for p in (ROOT/'src/Foundation').rglob('*.cs')) == 2)
check('Source project has no standalone machine reference', 'Foundation.Machines.dll' not in proj)

if GAME:
    game_contracts = {
        'Assets.Behaviour.UI/GameUI.cs': ('TogglePopupMenu(', 'AccountantPopup'),
        'Assets.Behaviour.UI.Shared/UIPopupMenu.cs': ('class UIPopupMenu', 'PopupClosed()'),
        'Assets.Behaviour.Flow.Elements/WorldFlowTimer.cs': ('OpenTimerSettingsPopup(',),
        'Assets.Behaviour.Flow.Elements/WorldFlowRecipe.cs': ('ShowRecipePopup(',),
        'Assets.Behaviour.Flow.Elements/WorldFlowResource.cs': ('ShowResourcePopup(',),
        'Assets.Behaviour.Flow.Elements/WorldFlowForest.cs': ('OpenResourcePopup(',),
        'Assets.Behaviour.Flow.Elements/WorldFlowFilter.cs': ('ToggleFilterSelect(',),
        'Assets.Behaviour.Flow.Elements/WorldFlowQuestBoard.cs': ('ToggleQuestPopup(',),
        'Assets.Behaviour.Flow.Elements/WorldFlowDungeon.cs': ('OpenDungeonPopup(',),
        'Assets.Source.Flow.Elements/TrackSwitch.cs': ('SwitchPosition',),
        'Assets.Source.Flow.Elements/SingleButton.cs': ('ClickCount',),
        'Assets.Source.Flow.Elements/Stockpile.cs': ('Capacity',),
    }
    for path,signatures in game_contracts.items():
        source=GAME/path
        content=source.read_text(encoding='utf-8-sig') if source.exists() else ''
        check('Native source signature: '+path, bool(content) and all(s in content for s in signatures))
else:
    results.append({'name':'Native signature comparison','result':'SKIP','details':'Pass extracted KingdomIncSrc/Source as positional argument.'})

summary={key:sum(x['result']==key for x in results) for key in ('PASS','FAIL','SKIP')}
report={'frameworkVersion':project_version,'scope':'static source checks, not compilation or in-game runtime','checks':results,'count':summary}
reportPath=ROOT/'Docs/UI/ui-static-checks.json'
reportPath.parent.mkdir(parents=True,exist_ok=True)
reportPath.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(summary))
for item in results:
    if item['result']=='FAIL':print('FAIL:',item['name'],item['details'])
sys.exit(1 if summary['FAIL'] else 0)
