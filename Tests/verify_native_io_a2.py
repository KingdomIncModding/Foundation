#!/usr/bin/env python3
"""Static A2/native source contract tests. Requires no guessed Unity assemblies or gameplay simulation."""
from pathlib import Path
import argparse, json, re, sys

root=Path(__file__).resolve().parents[1]
project_version=re.search(r'<Version>([0-9.]+)</Version>', (root/'Foundation.csproj').read_text(encoding='utf-8-sig')).group(1)
files={
 'bridge':root/'src/Foundation/Machines/IO/MachineNativeIO.cs',
 'snapshots':root/'src/Foundation/Machines/IO/NativeIOSnapshots.cs',
 'patch':root/'src/Foundation/Machines/IO/NativeIOPatches.cs',
 'api':root/'src/Foundation/Machines/Api/FoundationMachineApi.cs',
 'public':root/'src/Foundation/Public/FoundationApi.cs',
 'runtime':root/'src/Foundation/Machines/Runtime/MachineRuntime.cs',
 'test':root/'Tests/NativeIO/NativeIOVerificationPlugin.cs',
 'install':root/'Tests/NativeIO/Invoke-NativeIOVerification.ps1',
 'csproj':root/'Tests/NativeIO/Foundation.NativeIOVerification.csproj',
 'guide':root/'Docs/GameAPI/A2-Native-Machine-IO-Throughput.md',
}
text={n:p.read_text(encoding='utf-8-sig') for n,p in files.items()}
checks=[]
def check(name,value):
 checks.append({'name':name,'status':'PASS' if value else 'FAIL'})
check('Every required native IO source included',all(p.is_file() for p in files.values()))
check('One production Foundation DLL unchanged', '<AssemblyName>Foundation</AssemblyName>' in (root/'Foundation.csproj').read_text())
check('Production version aligned with current project', '<Version>'+project_version+'</Version>' in (root/'Foundation.csproj').read_text() and 'Version = "'+project_version+'"' in (root/'src/Foundation/Bootstrap/FoundationPlugin.FoundationPlugin.cs').read_text())
check('Shared native IO public entrypoints', 'MachineNativeIO IO' in text['api'] and 'FoundationMachineApi.IO' in text['public'])
check('Mutations delegate to existing native workspace API',all('FoundationApi.IO.'+x+'(' in text['bridge'] for x in ('Connect','Disconnect','ReplaceRoute','InsertRouteNode','RemoveRouteNode')))
check('No duplicated pipe graph and no manual flow simulation', 'new FlowPipe()' not in text['bridge'] and '.Update(' not in text['bridge'] and 'InputItem(' not in text['bridge'])
check('Port validation honors native occupancy and flow identity',all(k in text['bridge'] for k in ('output.ConnectedPipe != null','input.ConnectedPipe != null','from != to','manager.CurrentFlow','MachineThread.Verify()')))
check('Native acceptance predicate not guessed', 'input.CanAcceptItem(item)' in text['bridge'] and 'input.CanBuffer' in text['bridge'])
check('Native preview truth labels incomplete custom filters','port.FilterPreview' in text['snapshots'] and 'port.Filter == null && port.Item != null' in text['snapshots'])
check('No invented physical item rate cap', not any(x in text['bridge']+text['snapshots'] for x in ('MaxItemsPerSecond','ThroughputLimit','PipeSpeedCap','MaxThroughput')))
check('Native throughput delegates to existing measured counter', 'GetMeasuredThroughput(pipe)' in text['bridge'] and 'ObservationWindowSeconds => 10' in text['snapshots'])
check('Native production distinct from observed throughput','NativeProductionOutputPerSecond' in text['snapshots'] and 'FoundationMachineApi.Production.NativeOutputRate(machine)' in text['bridge'])
check('Native 3 logistics routing classes mapped',all(k in text['bridge'] for k in ('machine is Collector','machine is Distributor','machine is Manifold')))
check('Delegates port expansion/collapse to existing native machine calls', 'Operations.ExpandPorts(machine)' in text['bridge'] and 'Operations.CollapsePorts(machine)' in text['bridge'])
check('Protects against trailing buffered-item loss','would remove' in text['bridge'] and 'HasBuffer' in text['bridge'])
check('No fictional configurable FlowIO setters',not any(x in text['bridge'] for x in ('port.BufferSize =','port.Filter =','port.Item =','port.ConnectedPipe =')))
check('Real native editor grid snap','WorldFlowPipeNode.ClampToGrid' in text['bridge'])
check('Committed topology events from native methods', 'nameof(WorldFlowManager.CreateNewPipe)' in text['patch'] and 'nameof(FlowDiagram.DisconnectPipe)' in text['patch'] and 'ghost' in text['patch'])
check('Native IO hooks enabled in unified runtime',all(x in text['runtime'] for x in ('NativeIOPipeCreatedPatch','NativeIOPipeRemovedPatch','FoundationMachineApi.IO.Shutdown()')))
check('Callback exception isolation and disposable ownership', 'catch (Exception error)' in text['bridge'] and 'ListenerToken : IDisposable' in text['bridge'])
check('Independent real-game BepInEx consumer included', 'BepInPlugin(' in text['test'] and 'Foundation.dll' in text['install'])
check('One-step full build and verification installer', (root/'Tests/NativeIO/INSTALL-NATIVE-IO-TEST.cmd').is_file() and 'Install-Foundation.ps1' in (root/'Tests/NativeIO/INSTALL-NATIVE-IO-TEST.cmd').read_text())
check('Development verification report has PASS FAIL PENDING',all(x in text['test'] for x in ('"PASS"','"FAIL"','"PENDING"','nativeio-verification.json')))
check('Manual mutations require explicit opt-in','-foundation-nativeio-mutate' in text['test'] and 'if (_mutationEnabled && !_mutationsTried)' in text['test'])
check('Windows installer checks fresh runtime report',all(x in text['install'] for x in ('LastWriteTimeUtc','Stale report','dotnet build','Get-FileHash')))
check('Roadmap boundary in game API docs', 'Gate A' in text['guide'] and 'does not provide a universal settings UI' in text['guide'])
check('No Unity JsonUtility dependency in verifier','JsonUtility' not in text['test'])

# Compare with the actual decompiled source when supplied. A missing source
# is a SKIP, never false proof of correct native semantics.
parser=argparse.ArgumentParser()
parser.add_argument('--game-source', type=Path, default=root.parent/'Source')
args=parser.parse_args()
source=args.game_source
if source.is_dir():
 def g(path):return (source/path).read_text(encoding='utf-8-sig')
 pipe=g('Assets.Source.Flow/FlowPipe.cs')
 io=g('Assets.Source.Flow/FlowIO.cs')
 inport=g('Assets.Source.Flow/FlowInput.cs')
 col=g('Assets.Source.Flow.Elements/Collector.cs')
 dist=g('Assets.Source.Flow.Elements/Distributor.cs')
 man=g('Assets.Source.Flow.Elements/Manifold.cs')
 manager=g('Assets.Behaviour.Flow/WorldFlowManager.cs')
 check('Vanilla pipe explicitly drains buffers without hard-coded speed', 'do' in pipe and 'while (flag)' in pipe and 'ConnectedInput.CanAcceptItem' in pipe and 'ConnectedInput.InputItem(availableItem)' in pipe)
 check('Vanilla IO accepts by native filter or ItemType', 'Filter(item)' in io and 'item.Type == Item' in io)
 check('Vanilla InputItem only checks CanBuffer','if (CanBuffer)' in inport and 'CanAcceptItem' not in inport)
 check('Vanilla Collector forwards max one item per native Update', 'Outputs[0].CanBuffer' in col and 'Inputs[CurrentInput].ConsumeItem()' in col and 'NextInput()' in col)
 check('Vanilla Distributor advances selected output and returns if full', 'if (!Outputs[CurrentOutput].CanBuffer)' in dist and 'return;' in dist and 'Inputs[0].ConsumeItem()' in dist)
 check('Vanilla Manifold first buffered source to first connected output', 'Outputs[j].ConnectedPipe != null && Outputs[j].CanBuffer' in man and 'return;' in man)
 check('Vanilla UI port limit 4 documented by original UI class','_element.InputCount < 4' in g('Assets.Behaviour.Flow.Elements/WorldFlowCollector.cs') and '_element.OutputCount < 4' in g('Assets.Behaviour.Flow.Elements/WorldFlowDistributor.cs'))
 check('Vanilla pipe graph uses actual manager CreateNewPipe', 'CurrentFlow.Pipes.Add(pipe)' in manager)
else:
 for label in ('pipe transfer loop','native item predicate','input buffer','collector','distributor','manifold','native port count','native graph manager'):
  checks.append({'name':'Native source comparison: '+label,'status':'SKIP'})

summary={key:sum(c['status']==key for c in checks) for key in ('PASS','FAIL','SKIP')}
print('A2 Native IO source contracts: PASS=%d FAIL=%d SKIP=%d' % (summary['PASS'],summary['FAIL'],summary['SKIP']))
for c in checks:
 if c['status']=='FAIL': print('  FAIL',c['name'])
report={'scope':'source contracts and native decompiled source comparisons only; NOT a Windows compile or Unity runtime audit','summary':summary,'checks':checks}
output=root/'Docs/GameAPI/A2-NativeIO-Static-Verification.json'
output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
sys.exit(1 if summary['FAIL'] else 0)
