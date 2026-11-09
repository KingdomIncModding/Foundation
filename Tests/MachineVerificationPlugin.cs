using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;
using System.Globalization;
using BepInEx;
using Assets.Source.Flow;
using KingdomFoundation.Machines;
using UnityEngine;
namespace Foundation.MachineVerification
{
    [BepInPlugin("urakka.foundation.machineverification", "Foundation Machine Verification", "1.0.6")]
    [BepInDependency("urakka.kingdominc.foundation", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class MachineVerificationPlugin : BaseUnityPlugin
    {
        [Serializable] private sealed class Check { public string id;public string result;public string details; }
        [Serializable] private sealed class Report
        {
            public string generatedUtc;
            public string foundationMachinesVersion="integrated-Foundation-0.10.2";
            public string note="PASS means exercised in-game. PENDING means scenario not observed; does not imply success.";
            public int worldLoads;public int worldUnloads;public int machinesSeen;public int transfers;
            public int addedEvents;public int removedEvents;public int settingsChangedEvents;
            public int portsChangedEvents;public int movedFlowEvents;public int maxConnectedPipes;public int catalogAudits;
            public List<Check> checks=new List<Check>();
        }
        private readonly Dictionary<string,Check> _checks=new Dictionary<string,Check>();
        private readonly Report _report=new Report();
        private MachineModContext _owner;
        private bool _wasWorldLoaded;
        private float _nextWrite;
        private float _nextCatalogAudit;
        private string _reportPath;
        private void Set(string id,string status,string detail)
        {
            if(_checks.TryGetValue(id,out Check row))
            {
                // Preserve the first evidence of PASS and make FAIL sticky. Varying
                // machine identifiers/counts must not write hundreds of thousands
                // of otherwise identical PASS lines to the BepInEx log.
                if(row.result == "FAIL" || row.result == status) return;
                row.result=status;
                row.details=detail;
            }
            else {row=new Check{id=id,result=status,details=detail};_checks.Add(id,row);_report.checks.Add(row);}
            if(status=="FAIL") Logger.LogError("[MACHINE VERIFY FAIL] "+id+": "+detail);
            else if(status=="PASS") Logger.LogInfo("[MACHINE VERIFY PASS] "+id+": "+detail);
        }
        private void Awake()
        {
            _reportPath=Path.Combine(Paths.BepInExRootPath,"plugins","Foundation.MachineVerification","Reports","machine-verification.json");
            foreach(var id in new[]{"Api.EntryPoint","Api.MainThread","Registry.WorldCatalog","Registry.NativeIdentity","Registry.PortConsistency","Registry.RecipeFacts","Registry.SpecializedProperties","Production.ResourceAvailability","Production.NativeOutputRate","IO.NativeConnections","Operations.InvalidMachineRejected","Capabilities.NativeCommands","Extensions.OwnedRegistration","Extensions.DuplicateRejected","Extensions.Disposal","Extensions.ReadOnlyRejected","Commands.OwnedRegistration","Commands.DuplicateRejected","Commands.Disposal","Events.Added","Events.Removed","Events.SettingsChanged","IO.LiveTransfer","Lifecycle.Load","Lifecycle.Unload","Lifecycle.SecondLoad"})
                Set(id,"PENDING","Scenario not yet exercised.");
            try
            {
                _owner=FoundationMachineApi.ForMod("urakka.foundation.machineverification");
                Set("Api.EntryPoint","PASS","Machine API available from unified Foundation assembly to independent consumer plugin.");
                if(!FoundationMachineApi.Operations.SetSwitch(null,true).Succeeded && !FoundationMachineApi.Operations.Connect(null,null).Succeeded)
                    Set("Operations.InvalidMachineRejected","PASS","Mutations reject null/unplaced native handles without modifying game state.");
                else Set("Operations.InvalidMachineRejected","FAIL","Native operation incorrectly accepted null handle.");
                _owner.Listen(OnMachineChange);
                _owner.ListenTransfers(e => { _report.transfers++;Set("IO.LiveTransfer","PASS","Observed native item transfer; source="+e.SourceGuid+" dest="+e.DestinationGuid); });
                try
                {
                    // Make the assertion only after the worker has finished. A successful
                    // call on the worker is a FAIL, never an unconditional PASS.
                    bool rejected = false;
                    Exception unexpectedError = null;
                    var worker = new Thread(() =>
                    {
                        try { FoundationMachineApi.Registry.All(); }
                        catch (InvalidOperationException) { rejected = true; }
                        catch (Exception ex) { unexpectedError = ex; }
                    });
                    worker.IsBackground = true;
                    worker.Start();
                    if (!worker.Join(1000)) Set("Api.MainThread","FAIL","Worker-thread access probe timed out.");
                    else if (unexpectedError != null) Set("Api.MainThread","FAIL","Unexpected worker exception: "+unexpectedError);
                    else if (rejected) Set("Api.MainThread","PASS","Worker-thread native access rejected.");
                    else Set("Api.MainThread","FAIL","Registry.All() incorrectly accepted worker-thread access.");
                }
                catch(Exception ex) {Set("Api.MainThread","FAIL",ex.ToString());}
                Save();
            }
            catch(Exception ex) {Set("Api.EntryPoint","FAIL",ex.ToString());Save();}
        }
        private void OnMachineChange(MachineChangeEvent evt)
        {
            // These callbacks are emitted by the framework's native-world polling.
            // Do not manufacture events: the user must change a placed machine.
            switch(evt.Kind)
            {
                case MachineChangeKind.Added:
                    _report.addedEvents++;
                    Set("Events.Added","PASS","Native machine added: "+evt.Identity);
                    break;
                case MachineChangeKind.Removed:
                    _report.removedEvents++;
                    Set("Events.Removed","PASS","Native machine removed: "+evt.Identity);
                    break;
                case MachineChangeKind.SettingsChanged:
                    _report.settingsChangedEvents++;
                    Set("Events.SettingsChanged","PASS","Native machine settings changed: "+evt.Identity);
                    break;
                case MachineChangeKind.PortsChanged:
                    _report.portsChangedEvents++;
                    break;
                case MachineChangeKind.MovedFlow:
                    _report.movedFlowEvents++;
                    break;
            }
        }
        private bool _runPropertyTest;
        private void Update()
        {
            try
            {
                bool loaded=FoundationMachineApi.IsKingdomLoaded;
                if(loaded && !_wasWorldLoaded)
                {
                    _report.worldLoads++;Set("Lifecycle.Load","PASS","Kingdom was entered.");
                    if(_report.worldLoads>=2) Set("Lifecycle.SecondLoad","PASS","Two separate world entries observed.");
                    _wasWorldLoaded=true;
                }
                else if(!loaded && _wasWorldLoaded)
                {
                    _report.worldUnloads++;Set("Lifecycle.Unload","PASS","Kingdom exited to menu.");
                    _wasWorldLoaded=false;
                }
                // Audit the 500+ machine catalog at most once every two seconds.
                // Lifecycle checks above and event subscriptions remain active on
                // every frame; we only throttle the expensive read-only audit.
                if(loaded && Time.unscaledTime >= _nextCatalogAudit)
                {
                    _nextCatalogAudit=Time.unscaledTime+2f;
                    _report.catalogAudits++;
                    var all=FoundationMachineApi.Registry.All();
                    Set("Registry.WorldCatalog","PASS","Native flow-element registry can enumerate active kingdom (count="+all.Count+").");
                    _report.machinesSeen=Math.Max(_report.machinesSeen,all.Count);
                    var snapshots=FoundationMachineApi.Registry.SnapshotAll();
                    var nativePlayer=Assets.Source.Player.GamePlayer.Current;
                    int actualPipeCount=0;int observedConnections=0;
                    for(int i=0;i<nativePlayer.Flows.Count;i++)
                    {
                        actualPipeCount+=nativePlayer.Flows[i].Pipes.Count;
                        observedConnections+=FoundationMachineApi.Registry.Connections(i).Count;
                    }
                    _report.maxConnectedPipes=Math.Max(_report.maxConnectedPipes,observedConnections);
                    // An empty flow graph (0 == 0) is not evidence that native
                    // connection traversal works. Keep PENDING until real pipes exist.
                    if(actualPipeCount > 0 && actualPipeCount == observedConnections)
                        Set("IO.NativeConnections","PASS","Catalog enumerated "+observedConnections+" native connected pipes.");
                    else if(actualPipeCount != observedConnections)
                        Set("IO.NativeConnections","FAIL","Native pipe count differs from machine connection inventory (possibly a ghost pipe).");
                    if(snapshots.Count!=all.Count) Set("Registry.NativeIdentity","FAIL","Snapshot count differs from native count.");
                    else Set("Registry.NativeIdentity","PASS","Snapshots match native flow-element count.");
                    foreach(var native in all)
                    {
                        var snapshot=FoundationMachineApi.Registry.Inspect(native);
                        if(snapshot.Identity.Guid!=native.Guid || snapshot.Inputs.Count!=native.Inputs.Count || snapshot.Outputs.Count!=native.Outputs.Count)
                            Set("Registry.PortConsistency","FAIL","GUID/port mismatch for "+native.ElementType);
                        else if(!_checks.TryGetValue("Registry.PortConsistency",out Check portCheck) || portCheck.result!="FAIL")
                            Set("Registry.PortConsistency","PASS","Native GUID and IO counts verified.");
                        if(snapshot.AdditionalProperties!=null)
                            Set("Registry.SpecializedProperties","PASS","Native-backed specialized properties captured.");
                        var commands=FoundationMachineApi.Capabilities.SupportedCommands(native);
                        bool inspectAvailable = false;
                        foreach (string command in commands)
                            if (string.Equals(command,"InspectState",StringComparison.Ordinal)) { inspectAvailable = true; break; }
                        if(inspectAvailable) Set("Capabilities.NativeCommands","PASS","Available commands reflect native machine types.");
                        else Set("Capabilities.NativeCommands","FAIL","InspectState capability absent.");
                        if(native is Assets.Source.Flow.Elements.ResourceElement resource)
                        {
                            var choices=FoundationMachineApi.Production.AvailableResources(resource);
                            if(choices!=null) Set("Production.ResourceAvailability","PASS","Native resource availability queried.");
                            var rate=FoundationMachineApi.Production.NativeOutputRate(resource);
                            if(rate.HasValue) Set("Production.NativeOutputRate","PASS","Native ResourceElement.OutputPerSecond read.");
                        }
                        if(native is Assets.Source.Flow.Elements.Forest forest)
                        {
                            var rate=FoundationMachineApi.Production.NativeOutputRate(forest);
                            if(rate.HasValue) Set("Production.NativeOutputRate","PASS","Native Forest.OutputPerSecond read.");
                        }
                        if(native is Assets.Source.Flow.Elements.RecipeElement r)
                        {
                            var avail=FoundationMachineApi.Production.AvailableRecipes(r);
                            if(avail!=null) Set("Registry.RecipeFacts","PASS","Native recipe availability queried for "+r.ElementType);
                        }
                        if(!_runPropertyTest)
                        {
                            _runPropertyTest=true;
                            TestProperties(native);
                        }
                    }
                }
            }
            catch(Exception ex) {Set("Registry.WorldCatalog","FAIL",ex.ToString());}
            if(Time.unscaledTime >= _nextWrite) { _nextWrite=Time.unscaledTime+2f;Save(); }
        }
        private void TestProperties(FlowElement native)
        {
            IDisposable registration=null;
            try
            {
                registration=_owner.Expose(new MachinePropertyDefinition("stackSizeTest",typeof(FlowElement),e=>e.StackSize));
                var value=FoundationMachineApi.Registry.Inspect(native).AdditionalProperties;
                string key="urakka.foundation.machineverification:stackSizeTest";
                if(value.ContainsKey(key) && (int)value[key]==native.StackSize) Set("Extensions.OwnedRegistration","PASS","Native-backed property resolves under owner namespace.");
                else Set("Extensions.OwnedRegistration","FAIL","Registered native property missing or incorrect.");
                try
                {
                    _owner.Expose(new MachinePropertyDefinition("stackSizeTest",typeof(FlowElement),e=>e.StackSize));
                    Set("Extensions.DuplicateRejected","FAIL","Duplicate property registration was accepted.");
                }
                catch(InvalidOperationException) {Set("Extensions.DuplicateRejected","PASS","Duplicate native property registration rejected.");}
                var writeRejected=FoundationMachineApi.Extensions.TryWrite("urakka.foundation.machineverification","stackSizeTest",native,9);
                if(!writeRejected.Succeeded) Set("Extensions.ReadOnlyRejected","PASS","Read-only native property cannot be written.");
                else Set("Extensions.ReadOnlyRejected","FAIL","Read-only native property accepted write.");
                registration.Dispose();
                if(!FoundationMachineApi.Registry.Inspect(native).AdditionalProperties.ContainsKey(key)) Set("Extensions.Disposal","PASS","Owned native property removed without changing game.");
                else Set("Extensions.Disposal","FAIL","Disposed property remains registered.");
                TestCommands(native);
            }
            catch(Exception ex) {Set("Extensions.OwnedRegistration","FAIL",ex.ToString());}
            finally {registration?.Dispose();}
        }
        private void TestCommands(FlowElement machine)
        {
            IDisposable registration=null;
            try
            {
                registration=_owner.RegisterCommand(new MachineCommandDefinition("inspectTest",typeof(FlowElement),(e,arg)=>MachineOperationResult.Ok(e.ElementType)));
                var result=_owner.ExecuteCommand("inspectTest",machine,null);
                if(result.Succeeded && result.Message==machine.ElementType) Set("Commands.OwnedRegistration","PASS","Owner-qualified command executed against actual native machine.");
                else Set("Commands.OwnedRegistration","FAIL","Owned command returned unexpected result.");
                try
                {
                    _owner.RegisterCommand(new MachineCommandDefinition("inspectTest",typeof(FlowElement),(e,arg)=>MachineOperationResult.Ok()));
                    Set("Commands.DuplicateRejected","FAIL","Duplicate owner command accepted.");
                }
                catch(InvalidOperationException) {Set("Commands.DuplicateRejected","PASS","Duplicate owner command correctly rejected.");}
                registration.Dispose();
                if(!_owner.ExecuteCommand("inspectTest",machine,null).Succeeded) Set("Commands.Disposal","PASS","Disposed command is not executable.");
                else Set("Commands.Disposal","FAIL","Disposed command remains executable.");
            }
            catch(Exception ex) {Set("Commands.OwnedRegistration","FAIL",ex.ToString());}
            finally {registration?.Dispose();}
        }
        // The verification mod intentionally has no dependency on the optional Unity
        // JSON serialization module. Escape all control characters and strings as JSON.
        private static string JsonString(string value)
        {
            if (value == null) return "null";
            var result = new StringBuilder(value.Length + 2);
            result.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    default:
                        if (c < 0x20) result.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else result.Append(c);
                        break;
                }
            }
            return result.Append('"').ToString();
        }
        private string SerializeReport()
        {
            var json = new StringBuilder(2048);
            json.Append("{\n  \"generatedUtc\": ").Append(JsonString(_report.generatedUtc));
            json.Append(",\n  \"foundationMachinesVersion\": ").Append(JsonString(_report.foundationMachinesVersion));
            json.Append(",\n  \"note\": ").Append(JsonString(_report.note));
            json.Append(",\n  \"worldLoads\": ").Append(_report.worldLoads.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"worldUnloads\": ").Append(_report.worldUnloads.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"machinesSeen\": ").Append(_report.machinesSeen.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"transfers\": ").Append(_report.transfers.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"addedEvents\": ").Append(_report.addedEvents.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"removedEvents\": ").Append(_report.removedEvents.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"settingsChangedEvents\": ").Append(_report.settingsChangedEvents.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"portsChangedEvents\": ").Append(_report.portsChangedEvents.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"movedFlowEvents\": ").Append(_report.movedFlowEvents.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"maxConnectedPipes\": ").Append(_report.maxConnectedPipes.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"catalogAudits\": ").Append(_report.catalogAudits.ToString(CultureInfo.InvariantCulture));
            json.Append(",\n  \"checks\": [");
            for (int index = 0; index < _report.checks.Count; index++)
            {
                Check check = _report.checks[index];
                if (index > 0) json.Append(',');
                json.Append("\n    {\"id\": ").Append(JsonString(check.id));
                json.Append(", \"result\": ").Append(JsonString(check.result));
                json.Append(", \"details\": ").Append(JsonString(check.details)).Append('}');
            }
            json.Append("\n  ]\n}\n");
            return json.ToString();
        }
        private void Save()
        {
            try
            {
                _report.generatedUtc=DateTime.UtcNow.ToString("o");
                Directory.CreateDirectory(Path.GetDirectoryName(_reportPath));
                File.WriteAllText(_reportPath,SerializeReport());
            }
            catch(Exception ex) {Logger.LogWarning("Verification report could not be saved: "+ex.Message);}
        }
        private void OnDestroy()
        {
            // Destroying the plugin at application exit is NOT proof of a world-unload callback.
            _owner?.Dispose();Save();
        }
    }
}
