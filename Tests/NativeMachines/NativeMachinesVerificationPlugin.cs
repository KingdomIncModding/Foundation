using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using KingdomFoundation.Machines;
using UnityEngine;

namespace Foundation.NativeMachinesVerification
{
    /// <summary>Independent, read-only native machine coverage verification.
    /// Runs only with the installed game and unified production Foundation.dll.
    /// It never constructs or mutates machines, upgrades, pipes, inventory or worlds.</summary>
    [BepInPlugin("urakka.foundation.nativea1.verification", "Foundation Native A1 Machine Verification", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class NativeMachinesVerificationPlugin : BaseUnityPlugin
    {
        private sealed class Check { internal string Id, Status, Details; }
        private readonly Dictionary<string, Check> checks = new Dictionary<string, Check>(StringComparer.Ordinal);
        private float nextAudit;
        private int worldLoads, nativeRegistered, nativeObserved, unknownPreviewTypes;
        private bool wasLoaded;
        private string reportPath;
        private const string Tag = "[NATIVE A1 VERIFY]";

        private void Set(string name, string status, string details)
        {
            if (!checks.TryGetValue(name, out var row))
            {
                row = new Check { Id = name, Status = "PENDING", Details = "Not exercised on a live native machine." };
                checks.Add(name, row);
            }
            if (row.Status == "FAIL" || row.Status == "PASS" && status != "FAIL") return;
            row.Status = status; row.Details = details;
            if (status == "FAIL") Logger.LogError(Tag + " FAIL " + name + ": " + details);
            else if (status == "PASS") Logger.LogInfo(Tag + " PASS " + name + ": " + details);
        }

        private void Verify(string id, Action work)
        {
            try { work(); }
            catch (Exception error) { Set(id, "FAIL", error.ToString()); }
        }

        private void Assert(string id, bool correct, string details)
        { Set(id, correct ? "PASS" : "FAIL", details); }

        private void Awake()
        {
            reportPath = Path.Combine(Paths.BepInExRootPath, "plugins", "Foundation.NativeMachinesVerification", "Reports", "native-a1-machines.json");
            foreach (var id in new[] {"A1.SingleProductionAssembly", "A1.NativeCatalog", "A1.GamePrefabMapping", "A1.RegistryParity", "A1.NativeOperationNullGuard", "A1.NativeCostParity", "A1.NativeStateRead"})
                Set(id, "PENDING", "Requires actual installed game/Foundation assemblies or a loaded kingdom.");
            Verify("A1.SingleProductionAssembly", () => Assert("A1.SingleProductionAssembly",
                typeof(FoundationMachineApi).Assembly == typeof(NativeMachineCatalog).Assembly &&
                typeof(FoundationMachineApi).Assembly == typeof(MachineSnapshot).Assembly,
                "Catalog and machine snapshots are in the unified Foundation.dll."));
            Verify("A1.NativeCatalog", () => {
                var native = FoundationMachineApi.NativeTypes.KnownTypes;
                Assert("A1.NativeCatalog", native.Count == 61 && native.Select(x => x.NativeType).Distinct().Count() == native.Count &&
                    native.All(x => x.NativeType != null && typeof(FlowElement).IsAssignableFrom(x.NativeType)),
                    "Native class declarations recognized: " + native.Count + "; expected source inventory=61.");
            });
            Verify("A1.NativeOperationNullGuard", () => {
                var ops = FoundationMachineApi.Operations;
                Assert("A1.NativeOperationNullGuard",
                    !ops.SetAccountant(null, null, default(AppraiserType), 0).Succeeded &&
                    !ops.SetWarType(null, null).Succeeded && !ops.AdvanceCollector(null).Succeeded &&
                    !ops.SetOutfitterTimeout(null, 0f).Succeeded,
                    "Native-only mutators reject missing/foreign handles without touching game state.");
            });
            Save();
        }

        private void Update()
        {
            bool loaded = GamePlayer.Current != null && GamePlayer.Current.Flows.Count > 0;
            if (loaded && !wasLoaded) { wasLoaded = true; worldLoads++; }
            if (!loaded && wasLoaded) wasLoaded = false;
            if (!loaded || Time.unscaledTime < nextAudit) return;
            nextAudit = Time.unscaledTime + 3f;
            Verify("A1.GamePrefabMapping", AuditPrefabs);
            Verify("A1.RegistryParity", AuditKingdom);
            Save();
        }

        private void AuditPrefabs()
        {
            var catalog = FoundationMachineApi.NativeTypes;
            var registered = catalog.RegisteredTypes();
            nativeRegistered = registered.Count;
            int matched = 0, unrecognized = 0;
            foreach (var preview in FlowElement.GetAllTypes())
            {
                if (catalog.Describe(preview) != null) matched++;
                else unrecognized++; // May be a mod-supplied native subclass; report, do not misclassify it.
            }
            unknownPreviewTypes = unrecognized;
            Assert("A1.GamePrefabMapping", registered.Count > 0 && matched == registered.Count,
                "Actual registered native prefabs: " + registered.Count + "; other/unknown prefabs: " + unrecognized +
                ". Registered status comes from FlowElement.GetAllTypes, not source assumptions.");
            foreach (var descriptor in registered)
                if (!checks.ContainsKey("A1.Type." + descriptor.NativeType.Name))
                    Set("A1.Type." + descriptor.NativeType.Name, "PENDING",
                        "Native registered prefab discovered. Place and exercise this type in a disposable kingdom to verify its live state.");
        }

        private void AuditKingdom()
        {
            var machineApi = FoundationMachineApi.Registry;
            var all = machineApi.All();
            var snapshots = machineApi.SnapshotAll();
            Assert("A1.RegistryParity", all.Count == snapshots.Count,
                "Live FlowDiagram element count=" + all.Count + ", Foundation snapshot count=" + snapshots.Count);
            nativeObserved = all.Count;
            foreach (var machine in all)
            {
                var descriptor = FoundationMachineApi.NativeTypes.Describe(machine);
                if (descriptor == null) continue; // mod-created subclass, not a native game API type.
                string id = "A1.Type." + descriptor.NativeType.Name;
                Verify(id, () =>
                {
                    var snapshot = machineApi.Inspect(machine);
                    var direct = FoundationMachineApi.Specializations.Read(machine);
                    bool valid = snapshot.NativeType != null && snapshot.NativeType.NativeType == machine.GetType() &&
                        snapshot.Identity.Guid == machine.Guid &&
                        snapshot.Inputs.Count == machine.Inputs.Count && snapshot.Outputs.Count == machine.Outputs.Count &&
                        snapshot.NativeProperties != null && snapshot.NativeProperties.Count == direct.Count &&
                        snapshot.NativeProperties.All(entry => direct.ContainsKey(entry.Key) && Equals(entry.Value, direct[entry.Key])) &&
                        snapshot.NativeProperties.All(entry => entry.Value == null || entry.Value is string || entry.Value is bool ||
                            entry.Value is int || entry.Value is long || entry.Value is float || entry.Value is double || entry.Value is Enum);
                    if (machine is RecipeElement) valid &= snapshot.NativeProperties.ContainsKey("recipeId") &&
                        snapshot.NativeProperties.ContainsKey("craftableTag");
                    if (machine is ResourceElement) valid &= snapshot.NativeProperties.ContainsKey("resourceId") &&
                        snapshot.NativeProperties.ContainsKey("desiredResourceOutput");
                    if (machine is Armory) valid &= snapshot.NativeProperties.ContainsKey("maxItems");
                    Assert(id, valid, "Native " + machine.ElementType + " on a real workspace: exact type, GUID, IO and scalar specialty facts match.");
                });
                Verify("A1.NativeCostParity", () => {
                    var cost = FoundationMachineApi.Production.NativePurchaseCost(machine);
                    var native = machine.GetPurchaseCost();
                    Assert("A1.NativeCostParity", cost.GetCurrency().SequenceEqual(native.GetCurrency()),
                        "Native purchase-cost currencies match current game player count and tech.");
                });
                Verify("A1.NativeStateRead", () => {
                    var names = FoundationMachineApi.Capabilities.SupportedCommands(machine);
                    Assert("A1.NativeStateRead", names.Contains("InspectState") &&
                        machineApi.Inspect(machine).NativeProperties != null,
                        "Read-only specialized native facts available to consumer DLL.");
                });
            }
        }

        private static string Quote(string s)
        {
            if (s == null) return "null";
            var b = new StringBuilder("\"");
            foreach (char c in s)
            {
                if (c == '"') b.Append("\\\"");
                else if (c == '\\') b.Append("\\\\");
                else if (c == '\n') b.Append("\\n");
                else if (c == '\r') b.Append("\\r");
                else if (c == '\t') b.Append("\\t");
                else if (c < ' ') b.Append("\\u").Append(((int)c).ToString("x4"));
                else b.Append(c);
            }
            return b.Append('"').ToString();
        }

        private void Save()
        {
            try
            {
                var source = typeof(FoundationMachineApi).Assembly.Location;
                var bytes = File.ReadAllBytes(source);
                string hash;
                using (var sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
                var sb = new StringBuilder(6144);
                sb.Append("{\n\"generatedUtc\":").Append(Quote(DateTime.UtcNow.ToString("o")));
                sb.Append(",\n\"foundationVersion\":").Append(Quote(typeof(FoundationMachineApi).Assembly.GetName().Version.ToString()));
                sb.Append(",\n\"foundationSha256\":").Append(Quote(hash));
                sb.Append(",\n\"worldLoads\":").Append(worldLoads);
                sb.Append(",\n\"registeredNativeTypes\":").Append(nativeRegistered);
                sb.Append(",\n\"nativeMachinesObserved\":").Append(nativeObserved);
                sb.Append(",\n\"otherPreviewTypes\":").Append(unknownPreviewTypes);
                sb.Append(",\n\"checks\":[");
                bool first = true;
                foreach (var check in checks.Values.OrderBy(x => x.Id))
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append("\n{\"id\":").Append(Quote(check.Id)).Append(",\"result\":").Append(Quote(check.Status));
                    sb.Append(",\"details\":").Append(Quote(check.Details)).Append('}');
                }
                sb.Append("\n]}\n");
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, sb.ToString());
            }
            catch (Exception ex) { Logger.LogError(Tag + " Cannot write report: " + ex); }
        }
        private void OnDestroy() { Save(); }
    }
}
