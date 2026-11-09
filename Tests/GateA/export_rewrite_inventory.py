#!/usr/bin/env python3
"""List, but do not modify, the 24 Stage B candidates from the controlling roadmap.
This is Gate A boundary evidence only; no rewrite is authorized before signoff.
"""
import csv, re, sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]
roadmap=(root/'Foundation_1.0_Game_API_First_Rewrite_Roadmap.md').read_text(encoding='utf-8-sig')
doc=root/'Docs/GameAPI/GateA';doc.mkdir(parents=True,exist_ok=True)
# Source evidence paths are explicitly verified against this installed source,
# not assumptions from the old 0.10.1 roadmap.
examples={
'R01':['Core/Modules/CoreModuleRegistry.cs','Api/FoundationRuntimeService.cs'],
'R02':['Api/FoundationLifecycleService.cs','Core/Runtime/CoreLifecycleInstaller.cs'],
'R03':['Core/Modules/ModuleDependencyResolver.cs','Core/Models/ModDependency.cs'],
'R04':['Core/Services/CoreServiceRegistry.cs','Core/Modules/OwnedServices.cs'],
'R05':['Core/Patches/CorePatchManager.cs','Core/Patches/CorePatchSession.cs'],
'R06':['Api/FoundationEventBus.cs','Api/IFoundationEventBus.cs'],
'R07':['Core/Services/CoreServiceRegistry.cs','Public/FoundationModContext.cs'],
'R08':['Signals/SignalRegistry.cs'],
'R09':['UI/Settings/ModConfigSignalSelector.cs','Signals/SignalSelection.cs'],
'R10':['UI/Settings/ModConfigSignalSelector.cs','Signals/SignalPropertyDefinition.cs'],
'R11':['Api/FoundationNativeSignals.cs','Signals/NativeSignalBinder.cs'],
'R12':['UI/Settings/ModConfigRegistry.cs'],
'R13':['UI/Settings/ModConfigTemplates.cs'],
'R14':['UI/ModBar/ModBarContext.cs'],
'R15':['UI/Panels/FoundationPanelBuilder.cs','UI/ModBar/ModBarButton.cs'],
'R16':['Bootstrap/FoundationPlugin.FoundationDeveloperConsole.cs'],
'R17':['Bootstrap/FoundationPlugin.FoundationDeveloperConsole.cs','Machines/Api/MachineRegistry.cs'],
'R18':['Api/FoundationDiagnostics.cs','Core/Diagnostics/CoreDiagnostics.cs'],
'R19':['Core/Runtime/CoreLifecycleInstaller.cs'],
'R20':['Api/FoundationAssets.cs'],
'R21':['Gameplay/GameContentRegistry.cs','Workspace/WorkspaceNodeRegistry.cs'],
'R22':['Persistence/ModSaveRegistry.cs'],
'R23':['Persistence/MissingResearchRecovery.cs'],
'R24':['Workspace/WorkspaceNodeRegistry.cs'],
}
review={'R05','R14','R19','R23','R24'}
rows=[]
for i in range(1,25):
    key='R%02d'%i
    # roadmap contains table with ID | item | Rewrite/Review | stage | status
    hits=re.findall(r'^\|\s*'+key+r'\s*\|\s*([^|]+?)\s*\|\s*(Rewrite|Review)\s*\|\s*([^|]+?)\s*\|',roadmap,re.M|re.I)
    if not hits:raise SystemExit('Not found in controlling plan: '+key)
    name,choice,stage=hits[-1]
    paths=['src/Foundation/'+x for x in examples[key]]
    found=[x for x in paths if (root/x).is_file()]
    if len(found)!=len(paths):raise SystemExit('Missing evidence for '+key+': '+str(set(paths)-set(found)))
    rows.append({'ID':key,'Candidate':name.strip(),'Decision from roadmap':choice.strip(),
       'Stage':stage.strip(),'Existing source examples':'; '.join(paths),
       'Current source state':'PRESENT: audit depth/coverage TBD',
       'Work authorized at Gate A':'SOURCE INVENTORY ONLY',
       'Later review action':'KEEP / REWRITE / MERGE / REMOVE decision required' if key in review else 'Rewrite only after Gate A accepted'})
assert len(rows)==24
assert sum(r['Decision from roadmap'].lower()=='review' for r in rows)==5
with (doc/'Stage-B-Frozen-Inventory.csv').open('w',encoding='utf-8',newline='') as out:
    writer=csv.DictWriter(out,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
print('Stage B frozen inventory: 24 candidates, 19 rewrites and 5 reviews. No custom source edited.')
