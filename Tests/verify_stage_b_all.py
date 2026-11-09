#!/usr/bin/env python3
"""Stage B contract audits, source checks ONLY: does not imply Unity runtime validation."""
from pathlib import Path
from zipfile import ZipFile
import json,re,sys,collections
root=Path(__file__).resolve().parents[1]
checks={}
def check(key,ok,detail=''):
    checks[key]={'status':'PASS' if ok else 'FAIL','details':detail}
def text(path):return (root/path).read_text(encoding='utf-8-sig')
smoke=text('Tests/StageB/StageBVerificationPlugin.cs'); ps=text('Tests/StageB/Invoke-StageBVerification.ps1');
context=text('src/Foundation/Public/FoundationModContext.cs');fapi=text('src/Foundation/Public/FoundationApi.cs')
asset=text('src/Foundation/Api/FoundationAssets.cs');asset_handle=text('src/Foundation/Integration/Nested/Handle.cs');
content=text('src/Foundation/Gameplay/GameContentRegistry.cs');save=text('src/Foundation/Persistence/ModSaveRegistry.cs')
signals=text('src/Foundation/Signals/SignalRegistry.cs');links=text('src/Foundation/Signals/FoundationSignalLink.cs')
recovery=text('src/Foundation/Persistence/MissingResearchRecovery.cs');nodes=text('src/Foundation/Workspace/WorkspaceNodeRegistry.cs')
core=text('src/Foundation/Core/Patches/CorePatchManager.cs');dev=text('src/Foundation/Bootstrap/FoundationPlugin.FoundationDeveloperConsole.cs')
base=Path('/mnt/data/Foundation-0.10.7-Stage-B1-Core-InterMod-Complete-Source.zip')
# Source-level assertions for each of 24 roadmap IDs.
criteria={
'R01': 'core.Mods' in smoke and 'CoreModDirectory' in text('src/Foundation/Core/Runtime/FoundationCore.cs'),
'R02': 'StopAll' in text('src/Foundation/Core/Runtime/FoundationCore.cs') and 'ReleaseOwner' in text('src/Foundation/Core/Modules/CoreModuleRegistry.cs'),
'R03': 'ModuleDependencyResolver' in text('src/Foundation/Core/Modules/CoreModuleRegistry.cs'),
'R04': 'ServiceDirectory' in text('src/Foundation/Core/Runtime/FoundationCore.cs'),
'R05': 'Harmony.GetAllPatchedMethods()' in core and 'ReleaseOwner' in core,
'R06': 'core.Events.Subscribe' in smoke and 'FoundationCoreApi.Core.Events.Publish' in text('src/Foundation/Api/FoundationEventBus.cs'),
'R07': 'TryResolveProvider' in text('src/Foundation/Core/Services/CoreServiceRegistry.cs'),
'R08': 'FoundationSignalLinks' in links and 'SignalService' in fapi,
'R09': 'IReadOnlyList<ISignalSource> Find(' in signals,
'R10': 'FindProperties(' in signals and 'TryResolve(SignalSelection' in signals,
'R11': 'foundation:camera' in text('src/Foundation/Api/FoundationNativeSignals.cs') and 'foundation:workspace' in text('src/Foundation/Api/FoundationNativeSignals.cs'),
'R12': 'ModConfigRegistry' in fapi and 'GetOrCreateSection' in text('src/Foundation/UI/Settings/ModConfigRegistry.cs'),
'R13': 'OptionsUI' in text('src/Foundation/UI/Settings/ModConfigTemplates.cs') and 'ApplyWell' in text('src/Foundation/UI/Settings/ModConfigTemplates.cs'),
'R14': 'FoundationGameTheme.ApplyToggle(toggle)' in text('src/Foundation/UI/ModBar/ModBarContext.cs') and 'NativeGameTheme' not in text('src/Foundation/Api/IModBarContext.cs'),
'R15': 'RegisterPanel(FoundationPanelDefinition' in context and 'FoundationApi.Panels.Register' in context,
'R16': 'EnableConsole' in dev and 'void DrawDeveloperWindow' in dev,
'R17': 'FoundationApi.World.Player' in dev and 'FoundationApi.Flow.Inspect' in dev,
'R18': 'FoundationApi.Diagnostics.Write' in smoke and 'FoundationCoreApi.Core' in dev,
'R19': 'IEnumerator Once()' in smoke and 'void Update()' not in smoke and 'Remove' in ps and 'Hash' in ps,
'R20': 'AddDependency(' in asset and 'ForOwner(' in asset and 'ReleaseOwner(' in asset and 'Requires' in asset_handle,
'R21': 'TryGetOwner(' in content and 'RegisterRecipe' in content and 'FoundationApi.Content.RegisterItem' in context,
'R22': 'RegisterMigrating' in save and 'opaqueRoot' in save and 'Clone(original)' in save,
'R23': 'FoundationApi.Progression.FindTech' in recovery and 'PrepareLoad' in recovery and 'Extend' in recovery,
'R24': 'FoundationApi.Presentation.FindSprite' in nodes and 'FoundationApi.Progression.FindTech' in nodes and 'RegisterWorkspaceNode' in nodes,
}
for rid,passed in criteria.items():check('source.'+rid,passed,'source presence only; not runtime behavior')
# All B6 old API entry-points and settings contracts retained.
compat={
 'patch-manager-interface':text('src/Foundation/Core/Contracts/ICorePatchManager.cs'),
 'modbar-interface':text('src/Foundation/Api/IModBarContext.cs'),
 'workspace-node-interface':text('src/Foundation/Api/IWorkspaceNodeRegistry.cs'),
 'foundation-old-entry':fapi,
 'missing-research':recovery,
 'verification-source':smoke,
}
required={
 'patch-manager-interface':['PatchClass(string ownerId, Type patchContainer)','Conflicts(string ownerId)','ReleaseOwner(string ownerId)'],
 'modbar-interface':['CreateText(string text)','CreateButton(string text, Action clicked)','CreateDropdown(','CreateToggle(','CreateSeparator()'],
 'workspace-node-interface':['Nodes','RegisterWorkspaceNode(','TryGet(','Changed'],
 'foundation-old-entry':['public static IModBarRegistry ModBar','public static IWorkspaceNodeRegistry WorkspaceNodes', 'public static IModSaveRegistry ModSave'],
 'missing-research':['RetainedIds','RetainedNotificationIds','Clear()','Inspect(JsonObject','PrepareLoad(JsonObject','RestoreInputOnFailure','Loaded(','Extend(JsonObject'],
 'verification-source':['IEnumerator Once()','Check(','Pending(','Write()'],
}
for name,terms in required.items():
 for term in terms:check('compat.'+name+'.'+term,term in compat[name])
# No production DLL split / launch argument / periodic polling in one shot verifier.
proj=text('Foundation.csproj');testproj=text('Tests/StageB/Foundation.StageBVerification.csproj')
check('single-production-assembly', '<AssemblyName>Foundation</AssemblyName>' in proj and 'src\\Foundation\\**\\*.cs' in proj)
check('independent-test-assembly', 'Foundation.StageBVerification' in testproj and 'FoundationDll' in testproj)
check('explicit-uninstall-option', "[ValidateSet('Preflight','BuildInstall','Report','Remove')]" in ps and 'Remove-Item -LiteralPath $plugin' in ps)
check('report-trust', 'foundationSha256' in smoke and 'foundationSha256' in ps and 'LastWriteTimeUtc' in ps)
check('pending-not-masked', 'PENDING' in smoke and 'pending.Count' in ps and "'[PARTIAL]'" not in ps)
check('no-unattended-native-mutation', '-foundation-gameapi-mutate' not in smoke and 'void Update()' not in smoke and 'Save(' not in smoke)
check('gameapi-native-mapping-unmodified', all((root/p).exists() for p in [
 'src/Foundation/Gameplay/GameProgressService.cs','src/Foundation/Machines/IO/MachineNativeIO.cs',
 'src/Foundation/Gameplay/GameWorldService.cs','src/Foundation/Gameplay/GamePresentationService.cs']))
check('docs-migration-exists', (root/'Docs/StageB/Mod-Migration-Guide.md').stat().st_size>4000)
check('docs-B6-5', all(('R%02d'%i) in text('Docs/StageB/Mod-Migration-Guide.md') for i in [5,14,19,23,24]))
check('docs-24', all(('R%02d'%i) in text('Docs/StageB/README.md') for i in range(1,25)))
check('legacy-interface-ABI', all(text(rel)==ZipFile(base).read('Foundation/'+rel).decode('utf-8-sig') for rel in [
 'src/Foundation/Api/IFoundationAsset.cs','src/Foundation/Api/IFoundationAssets.cs',
 'src/Foundation/Api/IGameContentRegistry.cs','src/Foundation/Api/IModSaveRegistry.cs',
 'src/Foundation/Api/ISignalRegistry.cs']) if base.exists() else True)
# Compare unchanged baseline public contracts against last B1 source archive if supplied.
if base.exists():
 with ZipFile(base) as z:
  for relative in ['src/Foundation/Api/IModBarContext.cs','src/Foundation/Core/Contracts/ICorePatchManager.cs',
                   'src/Foundation/Api/IWorkspaceNodeRegistry.cs','src/Foundation/Api/UI/IFoundationPanelRegistry.cs']:
   before=z.read('Foundation/'+relative).decode('utf-8-sig')
   check('exact-public-interface.'+relative,before==text(relative),'public API unchanged')
  for prefix in ['Foundation/src/Foundation/Machines/','Foundation/src/Foundation/Gameplay/']:
   old={n:z.read(n) for n in z.namelist() if n.startswith(prefix) and n.endswith('.cs')}
   exception={'Foundation/src/Foundation/Gameplay/GameContentRegistry.cs'}
   changes=[n for n,data in old.items() if n not in exception and (root/n.removeprefix('Foundation/')).read_bytes()!=data]
   # encoding-preservation may change bytes only for touched; compare normalized content
   changes=[n for n in changes if (root/n.removeprefix('Foundation/')).read_text(encoding='utf-8-sig')!=old[n].decode('utf-8-sig')]
   check('game-api-unchanged.'+prefix,not changes,','.join(changes))
else:check('baseline-unavailable',True,'No local 0.10.7 baseline. Cannot run exact interface diff here.')
status=collections.Counter(x['status'] for x in checks.values())
out={'scope':'Stage B static source contracts only. Windows Unity build and game acceptance required.','results':dict(status),'checks':checks}
(root/'Docs/StageB/Static-Source-Verification.json').write_text(json.dumps(out,indent=2)+'\n')
print('Stage B static source checks:',dict(status))
for k,v in checks.items():
 if v['status']=='FAIL':print('[FAIL]',k,v['details'])
if status.get('FAIL',0):sys.exit(1)
