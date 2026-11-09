#!/usr/bin/env python3
"""Gate A: reproducible source inventory and freeze audit. NEVER certifies Unity/runtime tests.

Usage: python Tests/GateA/verify_gate_a.py [--native-zip /path/KingdomIncSrc.zip]

The checks intentionally fail when FoundationApi's public entrypoint changes without
an explicit NATIVE/BRIDGE/REWRITE/REVIEW classification. Game-owned method calls
remain traceable to the supplied decompiled game source; this is not a replacement
for compiling against a real game installation or running it.
"""
import argparse, csv, hashlib, json, re, sys, zipfile
from collections import Counter
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/GameAPI/GateA'
DOC.mkdir(parents=True,exist_ok=True)

def read(p):return (ROOT/p).read_text(encoding='utf-8-sig')
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def check(ok, name, info=''):
    CHECKS.append({'name':name,'result':'PASS' if ok else 'FAIL','detail':str(info)})
    return ok

CHECKS=[]
parser=argparse.ArgumentParser()
parser.add_argument('--native-zip',type=Path,help='Original KingdomIncSrc.zip for full native-source provenance')
parser.add_argument('--baseline-zip',type=Path,help='Current user-exported source ZIP for baseline SHA provenance')
parser.add_argument('--write-freeze',action='store_true',help='Initialize the one-time source freeze candidate hash manifest; never overwrite silently')
args=parser.parse_args()

native_meta=json.loads(read('Docs/GameAPI/A1-Native-Machine-Coverage.json'))
native_entries=native_meta['entries']
manifest=read('Foundation_1.0_Game_API_First_Rewrite_Roadmap.md')
api=read('src/Foundation/Public/FoundationApi.cs')
csproj=read('Foundation.csproj')
check('Game API First' in manifest and 'GATE A' in manifest,'Controlling Gate A roadmap retained')
check(len(native_entries)==61 and len(set(x['type'] for x in native_entries))==61,'Complete 61-type native catalog manifest')
check('<AssemblyName>Foundation</AssemblyName>' in csproj and '<Version>0.10.6</Version>' in csproj,'One production assembly and current version')
check(all((ROOT/'Tests'/name).exists() for name in ('verify_native_machine_a1.py','verify_native_io_a2.py','verify_game_api_a3_a7.py','verify_source.py')),'A1-A7 independent source-verification scripts present')
check(all((ROOT/'Tests'/p).is_file() for p in ('NativeMachines/NativeMachinesVerificationPlugin.cs','NativeIO/NativeIOVerificationPlugin.cs','NativeGameApi/NativeGameApiVerificationPlugin.cs')),'All three runtime verifier projects retained')

# These are the public entrypoints, not necessarily all public types in the SDK.
# Explicitly audit custom Foundation services so the subsequent rewrite cannot
# be conflated with Stage A's native Game API.
PUBLIC={
 'TechNodes':('B2','REWRITE'), 'TechMods':('B2','REWRITE'), 'WorkspaceNodes':('B6','REVIEW'),
 'ModBar':('B6','REVIEW'), 'ModConfig':('B4','REWRITE'), 'Panels':('B4','REWRITE'),
 'Assets':('B2','REWRITE'), 'Events':('B1','REWRITE'), 'Signals':('B3','REWRITE'),
 'ModSave':('B2','REWRITE'), 'Lifecycle':('B1','REWRITE'), 'Diagnostics':('B5','REWRITE'),
 'IO':('A2','BRIDGE'), 'NativeIO':('A2','BRIDGE'), 'View':('A6','BRIDGE'),
 'Audio':('A6','BRIDGE'), 'World':('A5','BRIDGE'), 'Workspace':('A5','BRIDGE'),
 'Economy':('A3','BRIDGE'), 'Progression':('A4','BRIDGE'), 'Adventures':('A4','BRIDGE'),
 'Items':('A3','BRIDGE'), 'Production':('A3','BRIDGE'), 'Machines':('A1','BRIDGE'),
 'Presentation':('A6+B2','BRIDGE+REWRITE'), 'PropertyControls':('A1','BRIDGE'),
 'Logistics':('A2','BRIDGE'), 'Saves':('A6+B2','BRIDGE+REWRITE'), 'Combat':('A4','BRIDGE'),
 'Content':('B2','REWRITE'), 'Upgrades':('B2','REWRITE'),
 'UI':('A6','BRIDGE'), 'Flags':('B2','REWRITE'), 'Runtime':('B1','REWRITE'),
 'Native':('A6','BRIDGE'), 'NativeSignals':('B3','REWRITE'),
 'Flow':('A2','BRIDGE')
}
# Skip internal entrypoints; only public properties are in the freeze classification.
found=dict(re.findall(r'^\s*public\s+static\s+[^\n]*?\s+(\w+)\s*=>',api,re.M)) if False else None
actual=set()
for line in api.splitlines():
    line=line.strip()
    if line.startswith('public static ') and ('=>' in line or '{ get;' in line) and '(' not in line.split('=>')[0].split('{ get;')[0]:
        p=line.split('=>')[0].split('{ get;')[0].strip().split()
        if p: actual.add(p[-1])
# Ignore entrypoint methods (those have a '('), class declaration.
actual={p for p in actual if '(' not in p and p not in {'class','FoundationApi'}}
check(actual==set(PUBLIC),'Every public FoundationApi service property classified',
      {'unclassified':sorted(actual-set(PUBLIC)),'stale':sorted(set(PUBLIC)-actual)})

with (DOC/'Public-Entrypoint-Boundaries.csv').open('w',encoding='utf-8',newline='') as f:
    writer=csv.writer(f);writer.writerow(['Public entrypoint','Stage','Classification','Source path','Gate A handling'])
    for name,(stage,category) in sorted(PUBLIC.items()):
        status=('Native API bridge; inspect native behavior, NOT a new simulation' if stage.startswith('A') else
                'Mixed native reads and custom registration; native access freezes, registration stays deferred' if '+' in stage else
                'Existing custom compatibility only; freeze until Stage B')
        writer.writerow(['FoundationApi.'+name,stage,category,'src/Foundation/Public/FoundationApi.cs',status])

# Cover all direct Foundation Game API interfaces, including public surface members.
# Type names are mapped through their actual implementations (where present).
INTERFACES={
 'IGameItemsService':('A3','Gameplay/GameItemsService.cs'),
 'IGameEconomyService':('A3','Gameplay/GameEconomyService.cs'),
 'IGameProductionService':('A3','Gameplay/GameProductionService.cs'),
 'IGameAdventureService':('A4','Gameplay/GameAdventureService.cs'),
 'IGameCombatService':('A4','Gameplay/GameCombatService.cs'),
 'IGameProgressService':('A4','Gameplay/GameProgressService.cs'),
 'IGameWorldService':('A5','Gameplay/GameWorldService.cs'),
 'IWorkspaceEditorService':('A5','Workspace/WorkspaceEditorService.cs'),
 'IGameUiService':('A6','Gameplay/GameUiService.cs'),
 'IGameAudioService':('A6','Gameplay/GameAudioService.cs'),
 'IGameViewService':('A6','Gameplay/GameViewService.cs'),
 'IGamePresentationService':('A6+B2','Gameplay/GamePresentationService.cs'),
 'IGameSaveService':('A6+B2','Gameplay/GameSaveService.cs'),
 'IGameLogisticsService':('A2','Gameplay/GameLogisticsService.cs'),
 'IGameMachinesService':('A1','Gameplay/GameMachinesService.cs'),
 'IGamePropertyControls':('A1','Gameplay/GamePropertyControls.cs'),
 'IGameFlagsService':('B2','Gameplay/GameFlagsService.cs'),
 'IGameContentRegistry':('B2','Gameplay/GameContentRegistry.cs'),
 'IGameUpgradeRegistry':('B2','Gameplay/GameUpgradeRegistry.cs'),
}
# Support public C# declarations as single-line signatures/properties. Non-contract
# XML comments, using directives, attributes and pure braces are discarded.
MEMBERS=[]
for iface,(stage,implementation) in INTERFACES.items():
    path='src/Foundation/Api/'+iface+'.cs'; impl='src/Foundation/'+implementation
    check((ROOT/path).is_file() and (ROOT/impl).is_file(),'API implementation exists: '+iface)
    src=read(path);body=read(impl)
    # Headers & return types are kept verbatim; methods may be overloaded.
    for line in src.splitlines():
        st=line.strip()
        if st.startswith(('//','///','#','using','namespace','public interface','{','}','[','*')):continue
        if not st or (';' not in st): continue
        if '(' in st and not st.endswith(';'):continue # reject multiline pieces
        if '(' in st and st.endswith(';'):
            left=st.split('(',1)[0].strip()
            name=left.split()[-1] if left else ''
            kind='method'
        elif '{ get;' in st:
            left=st.split('{',1)[0].strip()
            name=left.split()[-1] if left else ''
            kind='property'
        else:continue
        if not re.match(r'^[A-Za-z_]\w*$',name):continue
        # Implementation name check is a source audit, not a compiler check.
        pattern=r'\bpublic\s+(?:(?:static|override|virtual|new)\s+)*[^;\n{}]*\b'+re.escape(name)+r'\s*(?:\(|\{|=>)'
        present=bool(re.search(pattern,body))
        MEMBERS.append({'stage':stage,'interface':iface,'member':name,'kind':kind,
            'signature':st,'implementation':impl,'implementedInSource':present})
        check(present,'Interface source member implemented: '+iface+'.'+name,st if not present else '')

with (DOC/'Native-API-Member-Matrix.csv').open('w',encoding='utf-8',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=['stage','interface','member','kind','signature','implementation','implementedInSource'])
    writer.writeheader();writer.writerows(MEMBERS)
check(len(MEMBERS)>=190,'Substantive typed native/bridge interface inventory generated',len(MEMBERS))

# Native-source provenance at the *service* level, not an unsupported claim
# that every public member is verified in-game. All the listed exact game
# source files are required when --native-zip is supplied.
SOURCE_PROVENANCE={
 'IGameItemsService':['Assets.Source.Items/ItemType.cs','Assets.Source.Items/InventoryItem.cs','Assets.Source.Items/CraftingRecipe.cs','Assets.Source.Items/ItemEnhancement.cs'],
 'IGameEconomyService':['Assets.Source.Player/GamePlayer.cs','Assets.Source.Player/CurrencyCollection.cs'],
 'IGameProductionService':['Assets.Source.Player/ForestSettings.cs','Assets.Source.Player/FarmSettings.cs'],
 'IGameAdventureService':['Assets.Source.Adventure/Quest.cs','Assets.Source.Adventure/DungeonType.cs','Assets.Source.Adventure/WarType.cs'],
 'IGameCombatService':['Assets.Source.Items.Data/CombatantItemData.cs','Assets.Source.Items.Data/HumanItemData.cs'],
 'IGameProgressService':['Assets.Source.Player/TechNode.cs','Assets.Source.Player/PrestigeNode.cs','Assets.Source.Player/GamePlayer.cs'],
 'IGameWorldService':['Assets.Source.Player/GamePlayer.cs','Assets.Source.Flow/FlowDiagram.cs','Assets.Behaviour.Flow/WorldFlowManager.cs'],
 'IWorkspaceEditorService':['Assets.Behaviour.Flow/WorldFlowManager.cs','Assets.Source.Flow/FlowElement.cs'],
 'IGameUiService':['Assets.Behaviour.UI/GameUI.cs','Assets.Behaviour.UI/UIStatusMessage.cs'],
 'IGameAudioService':['Assets.Behaviour.Util/MusicManager.cs'],
 'IGameViewService':['Assets.Behaviour.Flow/WorldFlowCamera.cs','Assets.Behaviour.Input/PlayerInput.cs'],
 'IGamePresentationService':['Assets.Source.Util/Translation.cs','SpriteLibrary.cs'],
 'IGameSaveService':['Assets.Source.Util/SaveGame.cs','Assets.Source.Util/SaveGameFile.cs'],
 'IGameLogisticsService':['Assets.Source.Flow.Elements/Armory.cs','Assets.Source.Flow.Elements/Portal.cs'],
 'IGameMachinesService':['Assets.Source.Flow/FlowElement.cs','Assets.Source.Flow.Elements/Collector.cs'],
 'IGamePropertyControls':['Assets.Source.Flow/FlowElement.cs'],
}
assert set(SOURCE_PROVENANCE).issubset(INTERFACES)
provenance=[]
for interface,sources in SOURCE_PROVENANCE.items():
    for native_path in sources:
        provenance.append({'stage':INTERFACES[interface][0],'interface':interface,
            'foundationImplementation':'src/Foundation/'+INTERFACES[interface][1],
            'nativeGameSource':'Source/'+native_path,'verification':'native-source SHA when supplied'})
with (DOC/'Native-Service-Source-Provenance.csv').open('w',encoding='utf-8',newline='') as out:
    writer=csv.DictWriter(out,fieldnames=list(provenance[0]));writer.writeheader();writer.writerows(provenance)

# Exact original game source provenance for each native machine.
zip_ok=False
if args.native_zip:
    with zipfile.ZipFile(args.native_zip) as z:
        check(z.testzip() is None,'Original KingdomIncSrc ZIP integrity')
        check(digest(args.native_zip)==native_meta['sourceZipSHA256'],'Native decompiled source SHA256 agrees with A1 manifest')
        zset=set(z.namelist())
        for mapping in provenance:
            check(mapping['nativeGameSource'] in zset,
                'Native service declaration exists: '+mapping['interface']+' -> '+mapping['nativeGameSource'])
        for entry in native_entries:
            ok=entry['gameSource'] in zset and hashlib.sha256(z.read(entry['gameSource'])).hexdigest()==entry['gameSourceSha256']
            check(ok,'Native concrete machine declaration SHA: '+entry['type'],entry['gameSource'])
        zip_ok=True
else:
    CHECKS.append({'name':'Native game source SHA256 verification','result':'UNVERIFIED','detail':'Pass --native-zip /path/to/KingdomIncSrc.zip'})

with (DOC/'Native-Machine-Coverage-Matrix.csv').open('w',encoding='utf-8',newline='') as f:
    writer=csv.writer(f);writer.writerow(['Type','Native base','Family','Native file','Native source SHA256','Source mapped','Runtime observation','Machine operations note'])
    for e in sorted(native_entries,key=lambda d:d['type']):
        writer.writerow([e['type'],e['nativeBase'],e['family'],e['gameSource'],e['gameSourceSha256'],bool(e['sourceMapped']),
            'USER-REPORTED-PASS: registered native machine inspection (individual actions not established)',e['supportedNativeOperationsOrRestrictions']])

# A1 mutation-success verification differs from A1 native type inspection.
# The optional A1 independent verifier only calls four mutators with invalid
# handles (null-guard checks); it does not demonstrate their successful paths.
# Two pipe mutations are covered by the separate native IO verifier. Do not
# convert all-green discovery results into fictional operation coverage.
operations_src=read('src/Foundation/Machines/Native/MachineOperations.cs')
operation_names=re.findall(r'public\s+MachineOperationResult\s+(\w+)\s*\(',operations_src)
check(len(operation_names)==26 and len(set(operation_names))==26,
      'All 26 native machine command entrypoints inventoried',len(operation_names))
a1_test=read('Tests/NativeMachines/NativeMachinesVerificationPlugin.cs')
with (DOC/'Machine-Operation-Runtime-Gaps.csv').open('w',encoding='utf-8',newline='') as out:
    writer=csv.writer(out);writer.writerow(['Native machine API operation','Implementation source','A1 verifier evidence','Other suite evidence','Gate A operation outcome'])
    for name in operation_names:
        direct=bool(re.search(r'\b'+re.escape(name)+r'\s*\(',a1_test))
        other=('A2 native pipe edit suite reported PASS; check the exact case report' if name in ('Connect','Disconnect') else '')
        writer.writerow([name,'src/Foundation/Machines/Native/MachineOperations.cs',
            'INVALID-HANDLE GUARD CHECK ONLY' if direct else 'NO SUCCESSFUL INVOCATION IN A1 VERIFIER SOURCE',
            other, 'SUCCESS-PATH PROOF NOT IN SUPPLIED GATE A SOURCE'])

# Freeze contract protection: record all production C# hashes (not just DLL version)
# with a standalone recheck that catches unintended edits during later rewrites.
production_files=sorted((ROOT/'src/Foundation').rglob('*.cs'))
source_hashes={p.relative_to(ROOT).as_posix():digest(p) for p in production_files}
freeze_path=DOC/'Game-API-Freeze-Candidate-Hashes.json'
if args.write_freeze:
    if freeze_path.exists():
        raise SystemExit('Refusing to overwrite an existing freeze candidate. Review the current baseline before changing it.')
    freeze_path.write_text(json.dumps({'version':'0.10.6','status':'SOURCE_CANDIDATE_NOT_GATE_APPROVED',
        'nativeSourceSha256':native_meta['sourceZipSHA256'], 'sourceFileCount':len(production_files),
        'files':source_hashes},indent=2)+'\n',encoding='utf-8')
if freeze_path.is_file():
    freeze=json.loads(freeze_path.read_text(encoding='utf-8-sig'))
    diffs=[name for name,sha in source_hashes.items() if freeze.get('files',{}).get(name)!=sha]
    missing=set(freeze.get('files',{}))-set(source_hashes)
    check(not diffs and not missing,'Source freeze candidate hashes are unchanged',
          {'modified':diffs[:15],'removed':sorted(missing)[:15]})
else:
    check(False,'Source freeze candidate manifest exists','Run once with --write-freeze to record the current user baseline')
check(len(production_files)>350,'Current full Foundation production source inventory',len(production_files))

# Preserve exactly nineteen rewrites and five conditional reviews. This is a
# source-evidence and *future action* inventory, not a rewrite implementation.
roadmap_ids=re.findall(r'^\|\s*(R\d\d)\s*\|[^\n]*',manifest,re.M)
unique_ids=set(roadmap_ids)
check(len(unique_ids)==24 and all('R%02d'%i in unique_ids for i in range(1,25)),
      'All 24 Stage B candidates retained for later decision')
review_ids={'R05','R14','R19','R23','R24'}
check(all(re.search(r'^\|\s*'+q+r'\s*\|[^\n]*\|\s*Review\s*\|',manifest,re.M|re.I) for q in review_ids),
      'All five Stage B review decisions remain undecided')

# Existing runtime verification files are *tools*, not evidence their full
# scenarios were exercised. User reported tests passed, logs not supplied.
for stage, testname in [('A1','Tests/NativeMachines/NativeMachinesVerificationPlugin.cs'),
                       ('A2','Tests/NativeIO/NativeIOVerificationPlugin.cs'),
                       ('A3-A7','Tests/NativeGameApi/NativeGameApiVerificationPlugin.cs')]:
    t=read(testname)
    check('PENDING' in t and 'PASS' in t and 'FAIL' in t,'Runtime verifier reports all three result states: '+stage)
    check('foundationSha256' in t,'Runtime verifier binds to Foundation DLL SHA256: '+stage)

if args.baseline_zip:
    with zipfile.ZipFile(args.baseline_zip) as z:
        check(z.testzip() is None,'Current user-source ZIP integrity')
        expected_source={k for k in z.namelist() if k.startswith('Foundation/src/Foundation/') and k.endswith('.cs')}
        all_source={'Foundation/'+s for s in source_hashes}
        check(expected_source==all_source,'Current source production file inventory identical to user ZIP')
        diffs=[k for k in sorted(expected_source&all_source) if hashlib.sha256(z.read(k)).hexdigest()!=source_hashes[k[len('Foundation/'):]]]
        check(not diffs,'Stage B production source code unchanged during Gate A audit',diffs[:12])
        user_sha=digest(args.baseline_zip)
else:user_sha=None

counts=dict(Counter(x['result'] for x in CHECKS))
report={'milestone':'Gate A — Native Game API audit','version':'0.10.6','nativeSourceVerified':zip_ok,
    'baselineSourceSha256':user_sha,'baselinePreserved':not any(x['name']=='Stage B production source code unchanged during Gate A audit' and x['result']=='FAIL' for x in CHECKS),
    'productionFiles':len(production_files),'nativeMachineTypes':len(native_entries),
    'auditedTypedApiMembers':len(MEMBERS),'publicEntrypoints':len(PUBLIC),'nativeServiceSourceReferences':len(provenance),'stageBCandidates':24,
    'counts':counts,'checks':CHECKS,
    'runtimeStatus':'USER-REPORTED A1-A7 PASS; reports + game assembly SHA not present in this input',
    'gateA':'AWAITING_EVIDENCE_AND_GAP_REVIEW',
    'interpretation':'Source checks do NOT prove a full per-operation runtime audit or compatibility of external mods.'}
(DOC/'Gate-A-Static-Audit.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Gate A source audit',counts,'types',len(native_entries),'members',len(MEMBERS),'entrypoints',len(PUBLIC))
for result in CHECKS:
    if result['result']!='PASS':print(result['result'],result['name'],result['detail'])
sys.exit(1 if counts.get('FAIL',0) else 0)
