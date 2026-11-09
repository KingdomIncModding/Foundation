#!/usr/bin/env python3
"""A1 exact native class coverage and source contract verification (NOT game runtime)."""
import argparse, hashlib, json, re, sys, zipfile
from pathlib import Path

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--native-zip', type=Path, help='Optional supplied KingdomIncSrc.zip; verifies original game source class signatures and SHA256.')
args = parser.parse_args()
manifest = json.loads((root / 'Docs/GameAPI/A1-Native-Machine-Coverage.json').read_text(encoding='utf-8-sig'))
entries = manifest['entries']

checks=[]
def check(name, ok, detail=''):
    checks.append({'name':name,'status':'PASS' if ok else 'FAIL','details':detail})

def src(path):
    p=root / path
    return p.read_text(encoding='utf-8-sig') if p.is_file() else ''

catalog=src('src/Foundation/Machines/Api/NativeMachineCatalog.cs')
snapshot=src('src/Foundation/Machines/Models/MachineSnapshot.cs')
registry=src('src/Foundation/Machines/Api/MachineRegistry.cs')
special=src('src/Foundation/Machines/Api/MachineSpecializations.cs')
operations=src('src/Foundation/Machines/Native/MachineOperations.cs')
capabilities=src('src/Foundation/Machines/Api/MachineCapabilities.cs')
production=src('src/Foundation/Machines/Api/MachineProduction.cs')
facade=src('src/Foundation/Machines/Api/FoundationMachineApi.cs')
bootstrap=src('src/Foundation/Bootstrap/FoundationPlugin.FoundationPlugin.cs')
csproj=src('Foundation.csproj')
check('Source manifest has 61 exact concrete native FlowElement classes', len(entries)==61 and
      len({e['type'] for e in entries})==61 and manifest['concreteNativeElementTypeCount']==61)
project_version=re.search(r'<Version>([0-9.]+)</Version>', csproj).group(1)
check('One Foundation production DLL and matching current plugin version',
      '<AssemblyName>Foundation</AssemblyName>' in csproj and '<Version>'+project_version+'</Version>' in csproj and
      'Version = "'+project_version+'"' in bootstrap)
check('Source inventory is strongly typed (not fabricated runtime registration)',
      all('new NativeMachineTypeDescriptor(typeof('+e['type']+'), "'+e['family']+'", "'+e['nativeBase']+'")' in catalog for e in entries))
check('Native catalog consults real FlowElement.GetAllTypes',
      'foreach (var preview in FlowElement.GetAllTypes())' in catalog and
      'if (MachineThread.Player == null)' in catalog and 'candidate.NativeType == actual' in catalog)
check('Native catalog is exposed by same FoundationMachineApi',
      'public static NativeMachineCatalog NativeTypes' in facade and 'public static MachineRegistry Registry' in facade)
check('Snapshot preserves separate read-only native and owner extension property maps',
      all(t in snapshot for t in ['public NativeMachineTypeDescriptor NativeType','public IReadOnlyDictionary<string, object> NativeProperties','public IReadOnlyDictionary<string, object> AdditionalProperties']))
check('All snapshot paths read native specializations (not generic mod properties only)',
      registry.count('FoundationMachineApi.Specializations.Read(el)')==2 and
      registry.count('new MachineSnapshot(el, i, FoundationMachineApi.Extensions.ReadProperties(el),')==2)
check('Native specializations are main-thread and active-world checked',
      'MachineThread.Verify();' in special and 'MachineThread.IsPlaced(machine)' in special and
      'new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(result)' in special)
check('Native observations do not expose mutable collections or Unity UI objects',
      all('result["'+k+'"] = ' in special for k in ('craftableTag','slotCount','occupiedSlots','canAutoCraft','nativeForestOutputPerSecond','desiredForestOutput','collectedGold','maxItems','soldierCount','inputCost','collectedMana','fighting','recentItemCount')) and
      'result["recentItems"]' not in special and 'result["soldiers"]' not in special)
check('Native family getters use original inherited game methods',
      all(x in production for x in ('machine.GetAvailableRecipes()', 'machine.GetAvailableResources()', 'machine.GetCraftingCost(recipe)', 'machine.GetResourceCost(resource)', 'machine.GetPurchaseCost()', 'machine.GetRefund()')))
check('Native operations guard kingdom placement and real Unity views',
      'MachineThread.IsPlaced(el)' in operations and 'machine.World.GetComponent<WorldFlowAppraiser>()' in operations and
      'machine.World.GetComponent<WorldFlowAccountant>()' in operations and
      'machine.World.GetComponent<WorldFlowWarEffort>()' in operations)
for method in ('SetAppraiser','SetAccountant','SetWarType','AdvanceCollector','SetOutfitterTimeout','SetPortalChannel','SetArmoryChannel'):
    check('Native operation contract: '+method,
          'public MachineOperationResult '+method+'(' in operations and 'names.Add("'+method+'")' in capabilities)
check('A2 native transport untouched by A1 scope',
      all((root / p).is_file() for p in ('src/Foundation/Machines/IO/MachineNativeIO.cs',
                                         'src/Foundation/Machines/IO/NativeIOSnapshots.cs')))
check('Read-only independent A1 verifier and Windows scripts exist',
      all((root / p).is_file() for p in ('Tests/NativeMachines/NativeMachinesVerificationPlugin.cs',
         'Tests/NativeMachines/Foundation.NativeMachinesVerification.csproj',
         'Tests/NativeMachines/Invoke-NativeMachinesVerification.ps1',
         'Tests/NativeMachines/INSTALL-A1-TEST.cmd','Tests/NativeMachines/MACHINE-A1-TEST.cmd')))
check('Verifier has no direct native mutation',
      all(token not in src('Tests/NativeMachines/NativeMachinesVerificationPlugin.cs') for token in
           ('machine.SetUpgrade(', 'machine.DoManualCraft(', 'machine.SetOutputType(', 'new FlowPipe(', 'GamePlayer.Current.AddCurrency(')))

if args.native_zip:
    zip_path=args.native_zip.resolve()
    with zipfile.ZipFile(zip_path) as z:
        names=set(z.namelist())
        expected={e['type'] for e in entries}
        actual=set()
        for name in names:
            if not name.startswith('Source/Assets.Source.Flow.Elements/') or not name.endswith('.cs'):continue
            data=z.read(name).decode('utf-8-sig')
            m=re.search(r'public\s+(?:(?:sealed|abstract)\s+)?class\s+(\w+)\s*:\s*(\w+)',data)
            if m and m.group(1) not in set(manifest['excludedNonConcreteOrNonElement']):actual.add(m.group(1))
        check('Original native class inventory matches every supported game type',actual==expected,
              'missing '+str(sorted(actual-expected))+' unexpected '+str(sorted(expected-actual)))
        check('Original game source zip unchanged (SHA256)',hashlib.sha256(zip_path.read_bytes()).hexdigest()==manifest['sourceZipSHA256'])
        for e in entries:
            name=e['gameSource']
            ok=name in names and hashlib.sha256(z.read(name)).hexdigest()==e['gameSourceSha256']
            check('Native game declaration: '+e['type'], ok, name)
else:
    checks.append({'name':'Optional native game source SHA256 verification', 'status':'SKIP',
                   'details':'Pass --native-zip KingdomIncSrc.zip to compare the real game declarations.'})

pass_count=sum(x['status']=='PASS' for x in checks)
fail_count=sum(x['status']=='FAIL' for x in checks)
skip_count=sum(x['status']=='SKIP' for x in checks)
report={'milestone':'Foundation A1 Native Machine API','version':project_version,'originalMilestone':'A1 0.10.6','sourceOnly':True,
        'compiled':False,'runtimeVerified':False,
        'counts':{'PASS':pass_count,'FAIL':fail_count,'SKIP':skip_count},'checks':checks}
out=root / 'Docs/GameAPI/A1-Native-Machine-Static-Checks.json'
out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Native A1 source checks: PASS=%d FAIL=%d SKIP=%d'%(pass_count,fail_count,skip_count))
for row in checks:
    if row['status']=='FAIL': print('FAIL:',row['name'],row['details'])
sys.exit(1 if fail_count else 0)
