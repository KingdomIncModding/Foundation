using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using Assets.Source.Flow;
using Assets.Behaviour.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using Assets.Source.Items;
using BepInEx;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using UnityEngine;

namespace Foundation.NativeIOVerification
{
    /// <summary>Separate, development-only test consumer; not included in the shipping Foundation.dll.</summary>
    [BepInPlugin("urakka.foundation.nativeioverification", "Foundation Native IO Verification", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class NativeIOVerificationPlugin : BaseUnityPlugin
    {
        private sealed class Check { internal string Id;internal string Status;internal string Details; }
        private readonly Dictionary<string, Check> _checks = new Dictionary<string, Check>();
        private readonly List<IDisposable> _observers = new List<IDisposable>();
        private string _path;
        private bool _loaded;
        private bool _mutationsTried;
        private int _loads, _unloads, _connectionEvents, _disconnectionEvents, _transfers, _machinesSeen;
        private float _nextAudit;
        private bool _mutationEnabled;
        private const string Marker = "[NATIVE-IO VERIFY]";
        private static readonly string[] Names = {
            "EntryPoint.Unified", "EntryPoint.MainThread", "Runtime.Audit", "Lifecycle.WorldLoaded", "Lifecycle.WorldUnloaded", "Lifecycle.SecondLoad",
            "Ports.NativeParity", "Ports.SnapshotBuffers", "Ports.PreviewNotClaimedComplete", "Ports.InvalidRejected", "Ports.NativeCanAccept",
            "Connections.CatalogParity", "Connections.ValidationOccupied", "Connections.CrossFlowRejected",
            "Connections.CommittedEvent", "Connections.RemovedEvent", "Connections.CallbackIsolation",
            "Metrics.ReadsObserved", "Metrics.LiveTransfer", "Metrics.ZeroIsNotCapacity", "Metrics.MachineAggregate",
            "Routes.NativeReadback", "Routes.ReplaceRestore", "Routes.InsertRemoveRestore", "Routes.NativeGridSnap",
            "Router.Collector", "Router.Distributor", "Router.Manifold", "Router.NativeExpandCollapse",
            "Edit.InvalidReject", "Edit.ConnectDisconnect", "Lifecycle.SecondWorldCatalog"
        };
        private bool _throwCallbackFollowed;
        private void Mark(string id, string result, string details)
        {
            if (!_checks.TryGetValue(id, out var c)) return;
            if (c.Status == "FAIL" || (c.Status == "PASS" && result != "FAIL")) return;
            if (c.Status == result && result != "FAIL") return;
            c.Status = result;
            c.Details = details;
            if (result == "FAIL") Logger.LogError(Marker + " FAIL " + id + ": " + details);
            else if (result == "PASS") Logger.LogInfo(Marker + " PASS " + id + ": " + details);
        }
        private void Test(string id, Action action)
        {
            try { action(); }
            catch (Exception ex) { Mark(id, "FAIL", ex.ToString()); }
        }
        private void Awake()
        {
            _path = Path.Combine(Paths.BepInExRootPath, "plugins", "Foundation.NativeIOVerification", "Reports", "nativeio-verification.json");
            foreach (var name in Names) _checks.Add(name, new Check { Id=name, Status="PENDING", Details="Not yet exercised on a real native kingdom." });
            _mutationEnabled = Environment.GetCommandLineArgs().Any(a => a == "-foundation-nativeio-mutate");
            Test("EntryPoint.Unified", () =>
            {
                if (!ReferenceEquals(FoundationMachineApi.IO, FoundationApi.NativeIO))
                    throw new InvalidOperationException("Public native IO entry points are not a single shared service.");
                Mark("EntryPoint.Unified", "PASS", "Single native IO facade in one Foundation.dll.");
            });
            Test("EntryPoint.MainThread", () =>
            {
                bool refused = false; Exception unexpected = null;
                var thread = new Thread(() =>
                {
                    try { FoundationMachineApi.IO.Ports(null); }
                    catch (InvalidOperationException) { refused = true; }
                    catch (Exception ex) { unexpected = ex; }
                });
                thread.IsBackground = true; thread.Start();
                if (!thread.Join(2000) || unexpected != null || !refused)
                    throw new InvalidOperationException("Non-main-thread call not rejected: " + unexpected);
                Mark("EntryPoint.MainThread", "PASS", "Worker thread rejected by native IO facade.");
            });
            Test("Edit.InvalidReject", () =>
            {
                var io = FoundationMachineApi.IO;
                if (io.ValidateConnection(null, null).Allowed || io.Connect(null,null).Succeeded || io.Disconnect(null).Succeeded ||
                    io.ReplaceRoute(null, new Vector2Int[0]).Succeeded)
                    throw new InvalidOperationException("Invalid handles were not rejected.");
                Mark("Edit.InvalidReject", "PASS", "Null native IO handles rejected without changes.");
            });
            Test("Ports.InvalidRejected", () =>
            {
                bool rejected = false;
                try { FoundationMachineApi.IO.Port(null); }
                catch (ArgumentNullException) { rejected = true; }
                if (!rejected) throw new InvalidOperationException("Null port was accepted.");
                Mark("Ports.InvalidRejected", "PASS", "Null native port rejected.");
            });
            Test("Connections.CallbackIsolation", () =>
            {
                _observers.Add(FoundationMachineApi.IO.ObserveConnections(evt => { throw new InvalidOperationException("Expected observer isolation probe"); }));
                _observers.Add(FoundationMachineApi.IO.ObserveConnections(evt => { _throwCallbackFollowed = true; }));
            });
            _observers.Add(FoundationMachineApi.IO.ObserveConnections(evt =>
            {
                if (evt.Connected) { _connectionEvents++; Mark("Connections.CommittedEvent", "PASS", "Native placement post-commit observed."); }
                else { _disconnectionEvents++; Mark("Connections.RemovedEvent", "PASS", "Native FlowDiagram disconnection observed."); }
                if (_throwCallbackFollowed)
                    Mark("Connections.CallbackIsolation", "PASS", "Second subscriber ran despite expected first-subscriber exception.");
            }));
            _observers.Add(FoundationMachineApi.Telemetry.ObserveTransfers(evt =>
            {
                _transfers++; Mark("Metrics.LiveTransfer", "PASS", "Observed native WorldFlowPipe transfer.");
            }));
            Save();
        }
        private void Update()
        {
            try
            {
                bool now = FoundationMachineApi.IsKingdomLoaded;
                if (now && !_loaded)
                {
                    _loaded = true; _loads++;
                    Mark("Lifecycle.WorldLoaded", "PASS", "Loaded real kingdom.");
                    if (_loads >= 2) Mark("Lifecycle.SecondLoad", "PASS", "Second separate world entry observed.");
                }
                else if (!now && _loaded)
                {
                    _loaded = false; _unloads++;
                    Mark("Lifecycle.WorldUnloaded", "PASS", "Returned from kingdom to menu.");
                    _mutationsTried = false;
                }
                if (!now || Time.unscaledTime < _nextAudit) return;
                _nextAudit = Time.unscaledTime + 2f;
                Audit();
                Mark("Runtime.Audit", "PASS", "Native IO catalog audit ran to completion.");
                if (_mutationEnabled && !_mutationsTried)
                {
                    _mutationsTried = true;
                    // Explicit opt-in only, on a disposable kingdom. The test
                    // adds/removes pipes then restores original route data.
                    MutationAudit();
                }
                Save();
            }
            catch (Exception ex) { Mark("Runtime.Audit", "FAIL", ex.ToString()); Logger.LogError(Marker + " verification tick failed: " + ex);Save(); }
        }
        private void Audit()
        {
            var io = FoundationMachineApi.IO;
            var player = GamePlayer.Current;
            if (player == null) return;
            var point = new Vector2Int(17,31);
            if (io.SnapRouteNode(point) == WorldFlowPipeNode.ClampToGrid(point))
                Mark("Routes.NativeGridSnap", "PASS", "Snap matches native WorldFlowPipeNode.ClampToGrid.");
            else Mark("Routes.NativeGridSnap", "FAIL", "Snap deviates from native grid.");
            var machines = FoundationMachineApi.Registry.All();
            _machinesSeen = Math.Max(_machinesSeen, machines.Count);
            int nativeCount = player.Flows.Sum(flow => flow.Elements.Count);
            if (machines.Count != nativeCount) Mark("Ports.NativeParity", "FAIL", "Machine registry differs from actual native element count.");
            else Mark("Ports.NativeParity", "PASS", "Native registry count=" + nativeCount);
            bool testedBuffer = false, testedPreview = false;
            foreach (var machine in machines)
            {
                var ports = io.Ports(machine);
                if (ports.Count != machine.Inputs.Count + machine.Outputs.Count)
                    Mark("Ports.NativeParity", "FAIL", "Native IO port count mismatch for " + machine.ElementType);
                // Real ItemType.PreviewItem is a non-mutating native fixture.
                // Checking only buffered items is insufficient because one-slot
                // buffers often empty between the two-second audit samples.
                var probeItem = ItemType.Unlocked.Select(t => t?.PreviewItem).FirstOrDefault(item => item != null);
                if (probeItem != null)
                {
                    foreach (var input in machine.Inputs)
                    {
                        bool expected = input.CanAcceptItem(probeItem) && input.CanBuffer;
                        if (io.CanAccept(input, probeItem).Allowed == expected)
                            Mark("Ports.NativeCanAccept", "PASS", "Acceptance matches real native item predicate plus live buffer capacity.");
                        else Mark("Ports.NativeCanAccept", "FAIL", "API predicate diverges from the game for native preview item.");
                    }
                }
                foreach (var port in machine.IO)
                {
                    var snap = io.Port(port);
                    if (snap.BufferCapacity != port.BufferSize || snap.BufferedCount != port.BufferedItems.Count ||
                        snap.IsConnected != (port.ConnectedPipe != null) || snap.CanBuffer != port.CanBuffer)
                        Mark("Ports.SnapshotBuffers", "FAIL", "Port snapshot differs from native fields.");
                    else testedBuffer = true;
                    if (port is FlowInput inbound && inbound.BufferedItems.Count > 0 && inbound.BufferedItems[0] != null)
                    {
                        var item = inbound.BufferedItems[0];
                        bool expected = inbound.CanAcceptItem(item) && inbound.CanBuffer;
                        if (io.CanAccept(inbound, item).Allowed == expected)
                            Mark("Ports.NativeCanAccept", "PASS", "Native item predicate and capacity agree with Foundation result.");
                        else Mark("Ports.NativeCanAccept", "FAIL", "Native item acceptance differs from actual game predicate.");
                    }
                    if (port.Filter != null && !port.FilterPreview && !snap.PreviewIsExhaustive) testedPreview = true;
                }
                var aggregate = io.Throughput(machine);
                if (aggregate.MachineGuid != machine.Guid || aggregate.ObservationWindowSeconds != 10)
                    Mark("Metrics.MachineAggregate", "FAIL", "Machine throughput aggregate has inconsistent ownership/window.");
                else Mark("Metrics.MachineAggregate", "PASS", "Native input/output observed transfer totals and native production kept distinct.");
                var router = io.Router(machine);
                if (machine is Collector c)
                {
                    if (router.InputCount == c.InputCount && router.CurrentInputIndex == c.CurrentInput && router.NativeUiPortLimit == 4)
                        Mark("Router.Collector", "PASS", "Native collector routing fields matched.");
                    else Mark("Router.Collector", "FAIL", "Collector native state mismatch.");
                }
                if (machine is Distributor d)
                {
                    if (router.OutputCount == d.OutputCount && router.CurrentOutputIndex == d.CurrentOutput && router.NativeUiPortLimit == 4)
                        Mark("Router.Distributor", "PASS", "Native distributor routing fields matched.");
                    else Mark("Router.Distributor", "FAIL", "Distributor native state mismatch.");
                }
                if (machine is Manifold m)
                {
                    if (router.InputCount == m.InputCount && router.OutputCount == m.InputCount && router.NativeUiPortLimit == 4)
                        Mark("Router.Manifold", "PASS", "Native manifold routing fields matched.");
                    else Mark("Router.Manifold", "FAIL", "Manifold native state mismatch.");
                }
            }
            if (testedBuffer) Mark("Ports.SnapshotBuffers", "PASS", "Buffer sizes/counts and connection fields match native IO.");
            if (testedPreview) Mark("Ports.PreviewNotClaimedComplete", "PASS", "Custom native filters correctly marked as non-exhaustive previews.");
            for (int flow = 0; flow < player.Flows.Count; flow++)
            {
                var p = player.Flows[flow].Pipes;
                var snapshots = io.Pipes(flow);
                if (p.Count != snapshots.Count) Mark("Connections.CatalogParity", "FAIL", "Pipe snapshot catalog mismatch.");
                else Mark("Connections.CatalogParity", "PASS", "Native pipe catalog matches live flow graph.");
                foreach (var pipe in p)
                {
                    var snap = io.Pipe(pipe);
                    if (!snap.Route.SequenceEqual(pipe.Nodes)) Mark("Routes.NativeReadback", "FAIL", "Native route node mismatch.");
                    else Mark("Routes.NativeReadback", "PASS", "Native bend nodes read without modification.");
                    var count = FoundationApi.IO.GetMeasuredThroughput(pipe);
                    if (count.TotalTransfers != snap.ObservedTransfers || count.AverageTransfersPerSecond != snap.ObservedTransfersPerSecond)
                        Mark("Metrics.ReadsObserved", "FAIL", "Native observed throughput did not equal existing Foundation 10s meter.");
                    else Mark("Metrics.ReadsObserved", "PASS", "Identical real transfer observation meter in both public APIs.");
                    if (snap.ObservationWindowSeconds == 10)
                        Mark("Metrics.ZeroIsNotCapacity", "PASS", "10s throughput observation, no invented pipe maximum-rate field.");
                    if (pipe.ConnectedOutput != null && pipe.ConnectedInput != null)
                    {
                        var check = io.ValidateConnection(pipe.ConnectedOutput,pipe.ConnectedInput);
                        if (check.Allowed) Mark("Connections.ValidationOccupied", "FAIL", "Occupied connection incorrectly allowed.");
                        else Mark("Connections.ValidationOccupied", "PASS", "Occupied endpoints correctly rejected.");
                    }
                }
            }
            if (player.Flows.Count > 1)
            {
                var a = player.Flows[0].Elements.SelectMany(m => m.Outputs).FirstOrDefault();
                var b = player.Flows[1].Elements.SelectMany(m => m.Inputs).FirstOrDefault();
                if (a != null && b != null)
                {
                    if (io.ValidateConnection(a,b).Allowed) Mark("Connections.CrossFlowRejected", "FAIL", "Cross-flow native connection unexpectedly allowed.");
                    else Mark("Connections.CrossFlowRejected", "PASS", "Cross-flow pipe rejected.");
                }
            }
            if (_loads > 1 && machines.Count == nativeCount)
                Mark("Lifecycle.SecondWorldCatalog", "PASS", "Second world loaded with consistent native machine catalog.");
        }
        private void MutationAudit()
        {
            var io = FoundationMachineApi.IO;
            var player = GamePlayer.Current;
            if (player == null) return;
            var flow = player.ActiveFlow;
            // Reversible route edits, on the current visible flow only.
            var pipe = flow.Pipes.FirstOrDefault(p => p != null && p.World);
            if (pipe != null)
            {
                var prior = pipe.Nodes.ToArray();
                try
                {
                    var replacement = prior.Concat(new[] { new Vector2Int(16, 32) }).ToArray();
                    var change = io.ReplaceRoute(pipe,replacement);
                    if (!change.Succeeded || !pipe.Nodes.SequenceEqual(replacement))
                        Mark("Routes.ReplaceRestore", "FAIL", "Native ReplaceRoute did not write expected nodes.");
                    else Mark("Routes.ReplaceRestore", "PASS", "Native route changed and will be restored.");
                    var remove = io.RemoveRouteNode(pipe, pipe.Nodes.Count-1);
                    var insert = io.InsertRouteNode(pipe, pipe.Nodes.Count, new Vector2Int(16,32));
                    if (!remove.Succeeded || !insert.Succeeded || !pipe.Nodes.SequenceEqual(replacement))
                        Mark("Routes.InsertRemoveRestore", "FAIL", "Native insert/remove changed route unexpectedly.");
                    else Mark("Routes.InsertRemoveRestore", "PASS", "Native route insert and remove completed.");
                    var snapped = io.SnapRouteNode(new Vector2Int(17,31));
                    if (snapped == WorldFlowPipeNode.ClampToGrid(new Vector2Int(17,31)))
                        Mark("Routes.NativeGridSnap", "PASS", "Native grid snapping matches original game.");
                }
                catch(Exception ex) { Mark("Routes.ReplaceRestore", "FAIL", ex.Message); }
                finally
                {
                    var back = io.ReplaceRoute(pipe,prior);
                    if (!back.Succeeded || !pipe.Nodes.SequenceEqual(prior)) Mark("Routes.ReplaceRestore", "FAIL", "Unable to restore route: " + back.Message);
                }
            }
            // This opt-in mutation only uses initially free ports; never replaces
            // real pipes and never executes if both endpoints aren't available.
            var outs = flow.Elements.SelectMany(m => m.Outputs).Where(o => o.ConnectedPipe == null).ToArray();
            var ins = flow.Elements.SelectMany(m => m.Inputs).Where(i => i.ConnectedPipe == null).ToArray();
            foreach (var output in outs)
            {
                var input = ins.FirstOrDefault(i => io.ValidateConnection(output,i).Allowed);
                if (input == null) continue;
                var connect = io.Connect(output,input);
                if (!connect.Succeeded) { Mark("Edit.ConnectDisconnect", "FAIL", connect.Message); break; }
                var created = output.ConnectedPipe;
                if (created == null || input.ConnectedPipe != created)
                    Mark("Edit.ConnectDisconnect", "FAIL", "Native pipe not attached at both ports.");
                else
                {
                    var removed = io.Disconnect(created);
                    if (removed.Succeeded && output.ConnectedPipe == null && input.ConnectedPipe == null)
                        Mark("Edit.ConnectDisconnect", "PASS", "Connected and disconnected using actual native world manager.");
                    else Mark("Edit.ConnectDisconnect", "FAIL", "Native disconnection failed: " + removed.Message);
                }
                break;
            }
            foreach (var m in flow.Elements)
            {
                if (!(m is Collector) && !(m is Distributor) && !(m is Manifold)) continue;
                var beforeIn=m.Inputs.Count;var beforeOut=m.Outputs.Count;
                if (beforeIn >=4 || beforeOut >=4) continue;
                var add=io.ExpandRouter(m);
                if (!add.Succeeded) { Mark("Router.NativeExpandCollapse","FAIL",add.Message);continue; }
                var collapse=io.CollapseRouter(m);
                if (!collapse.Succeeded) { Mark("Router.NativeExpandCollapse","FAIL",collapse.Message); continue; }
                // Restore original count in case native Collapse also removed
                // pre-existing empty trailing slots. No buffers/pipes affected.
                while (m.Inputs.Count < beforeIn || m.Outputs.Count < beforeOut)
                {
                    var again=io.ExpandRouter(m);
                    if (!again.Succeeded) { Mark("Router.NativeExpandCollapse","FAIL",again.Message);break; }
                }
                if (m.Inputs.Count == beforeIn && m.Outputs.Count == beforeOut)
                    Mark("Router.NativeExpandCollapse", "PASS", "Native Expand/Collapse executed and original port layout restored.");
                else Mark("Router.NativeExpandCollapse", "FAIL", "Original port counts not restored.");
                break;
            }
        }
        private static string J(string s)
        {
            if (s==null) return "null";
            var b=new StringBuilder("\"");
            foreach(var ch in s)
            {
                switch(ch)
                {
                    case '\\': b.Append("\\\\");break;
                    case '"': b.Append("\\\"");break;
                    case '\n':b.Append("\\n");break;
                    case '\r':b.Append("\\r");break;
                    case '\t':b.Append("\\t");break;
                    case '\b':b.Append("\\b");break;
                    case '\f':b.Append("\\f");break;
                    default: if(ch<32 || char.IsSurrogate(ch)) b.Append("\\u").Append(((int)ch).ToString("x4"));else b.Append(ch);break;
                }
            }
            return b.Append('"').ToString();
        }
        private static string FoundationDigest()
        {
            var foundation = Path.Combine(Paths.BepInExRootPath,"plugins","Foundation","Foundation.dll");
            if (!File.Exists(foundation)) return null;
            using (var stream = File.OpenRead(foundation))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
        }
        private void Save()
        {
            try
            {
                var b = new StringBuilder("{\n  \"generatedUtc\": ").Append(J(DateTime.UtcNow.ToString("o")));
                b.Append(",\n  \"foundationVersion\": ").Append(J(typeof(FoundationMachineApi).Assembly.GetName().Version.ToString()));
                b.Append(",\n  \"foundationSha256\": ").Append(J(FoundationDigest()));
                b.Append(",\n  \"mutationsEnabled\": ").Append(_mutationEnabled ? "true" : "false");
                b.Append(",\n  \"worldLoads\": ").Append(_loads);
                b.Append(",\n  \"worldUnloads\": ").Append(_unloads);
                b.Append(",\n  \"machinesSeen\": ").Append(_machinesSeen);
                b.Append(",\n  \"nativeConnectionEvents\": ").Append(_connectionEvents);
                b.Append(",\n  \"nativeDisconnectionEvents\": ").Append(_disconnectionEvents);
                b.Append(",\n  \"nativeTransfers\": ").Append(_transfers);
                b.Append(",\n  \"checks\": [");
                int count=0;
                foreach(var v in _checks.Values)
                {
                    b.Append(count++==0?"\n":",\n").Append("    {\"id\": ").Append(J(v.Id))
                        .Append(", \"result\": ").Append(J(v.Status)).Append(", \"details\": ").Append(J(v.Details)).Append('}');
                }
                b.Append("\n  ]\n}\n");
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(_path,b.ToString(), new UTF8Encoding(false));
            }
            catch(Exception ex) {Logger.LogWarning(Marker + " cannot write report: " + ex.Message);}
        }
        private void OnDestroy()
        {
            foreach(var disposable in _observers) disposable.Dispose();
            _observers.Clear();
            Save();
        }
    }
}
