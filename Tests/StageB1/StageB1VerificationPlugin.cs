using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using KingdomFoundation.Api;
using KingdomFoundation.Core;
using UnityEngine;

namespace Foundation.StageB1Verification
{
    /// <summary>
    /// One-shot Stage B1 integration test. No Update hook, native-world mutations,
    /// gameplay patches, or repeated disk writes. Safe to remove after reporting.
    /// </summary>
    [BepInPlugin("urakka.foundation.stageb1.verification", "Foundation Core Stage B1 Verification", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation.core", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class StageB1VerificationPlugin : BaseUnityPlugin
    {
        private sealed class Row { internal string Id, Status, Details; }
        private readonly List<Row> rows = new List<Row>();
        private readonly List<IDisposable> cleanup = new List<IDisposable>();
        private const string ProducerId = "urakka.foundation.b1verify.producer";
        private const string ConsumerId = "urakka.foundation.b1verify.consumer";
        private const string ObserverId = "urakka.foundation.b1verify.observer";
        private const string TransitiveId = "urakka.foundation.b1verify.transitive";
        private const string Tag = "[STAGE B1 VERIFY] ";
        private string reportPath;

        private interface ICounterService { int Value { get; } }
        private sealed class CounterService : ICounterService { public int Value => 17; }
        private sealed class ProbeEvent { }
        private sealed class TestModule : IFoundationModule
        {
            internal int Starts, Stops;
            internal readonly bool IsProducer;
            public ModDescriptor Descriptor { get; }
            internal TestModule(string id, bool producer, ModDependency[] dependencies = null)
            {
                IsProducer = producer;
                Descriptor = new ModDescriptor(id, "B1 temporary test " + id, new Version(1, 0),
                    dependencies: dependencies);
            }
            public void Initialize(IModContext ctx)
            {
                Starts++;
                if (IsProducer)
                    ctx.Services.Register<ICounterService>(Descriptor.Id + ":counter", new CounterService(), new Version(1, 2));
            }
            public void Shutdown() { Stops++; }
        }
        private void Awake()
        {
            reportPath = Path.Combine(Paths.GameRootPath, "_Mods", "Foundation", "Verification", "StageB1", "stage-b1-verification.json");
            StartCoroutine(RunOnce());
        }
        private IEnumerator RunOnce()
        {
            // Let BepInEx initialize Foundation Core before reading live state.
            yield return null;
            yield return null;
            try
            {
                // Catch unexpected iterator failures as explicit FAIL results,
                // instead of accidentally leaving a short all-PASS report.
                var work = Verify().GetEnumerator();
                while (true)
                {
                    bool more;
                    try { more = work.MoveNext(); }
                    catch (Exception ex)
                    {
                        rows.Add(new Row { Id = "B1.UnexpectedException", Status = "FAIL", Details = ex.ToString() });
                        Logger.LogError(Tag + ex);
                        break;
                    }
                    if (!more) break;
                    yield return work.Current;
                }
                (work as IDisposable)?.Dispose();
            }
            finally
            {
                for (int i = cleanup.Count - 1; i >= 0; i--)
                    try { cleanup[i].Dispose(); } catch (Exception ex) { Logger.LogError(ex); }
                WriteReport();
                Logger.LogInfo(Tag + "Single-shot checks finished; plugin performs no ongoing work.");
            }
        }
        private IEnumerable Verify()
        {
            var core = FoundationCoreApi.Core;
            Check("B1.Core", () => core != null && core.ApiVersion.Major == 1, "Core API entry point and version");
            Check("B1.LiveModDirectory", () => core.Mods.TryGet("urakka.kingdominc.foundation", out var mod) &&
                mod.IsPluginLoaded && mod.Version != null, "BepInEx registration visible through unified directory");
            Check("B1.ServiceDirectoryEmpty", () => !core.ServiceDirectory.Find(typeof(ICounterService)).Any(x => x.OwnerId == ProducerId),
                "No stale provider from prior test session");

            int legacyEvents = 0, coreEvents = 0;
            var old = FoundationApi.Events.Subscribe<ProbeEvent>(ObserverId, x => legacyEvents++);
            var current = core.Events.Subscribe<ProbeEvent>(ObserverId, x => coreEvents++);
            cleanup.Add(old); cleanup.Add(current);
            core.Events.Publish(new ProbeEvent());
            FoundationApi.Events.Publish(new ProbeEvent());
            Check("B1.OneEventBus", () => legacyEvents == 2 && coreEvents == 2,
                "Legacy and Core subscribers each receive exactly two shared publications");
            old.Dispose(); current.Dispose();
            core.Events.Publish(new ProbeEvent());
            Check("B1.EventTokenCleanup", () => legacyEvents == 2 && coreEvents == 2,
                "Disposing both subscription tokens prevents further notifications");
            int healthyCalls = 0;
            var throwing = core.Events.Subscribe<ProbeEvent>(ObserverId, _ =>
                { throw new InvalidOperationException("Expected Stage B1 subscriber isolation probe"); });
            var healthy = core.Events.Subscribe<ProbeEvent>(ObserverId, _ => healthyCalls++);
            core.Events.Publish(new ProbeEvent());
            Check("B1.SubscriberIsolation", () => healthyCalls == 1,
                "A failing event listener does not prevent subsequent listeners");
            throwing.Dispose(); healthy.Dispose();

            var consumer = new TestModule(ConsumerId, false, new[] {
                new ModDependency(ProducerId, DependencyKind.FoundationModule, new Version(1, 0)) });
            var consumerHandle = core.Modules.Register(consumer);
            cleanup.Add(consumerHandle);
            var transitive = new TestModule(TransitiveId, false, new[] {
                new ModDependency(ConsumerId, DependencyKind.FoundationModule) });
            var transitiveHandle = core.Modules.Register(transitive);
            cleanup.Add(transitiveHandle);
            yield return null; yield return null;
            Check("B1.DependencyWait", () => core.Modules.TryGet(ConsumerId, out var snapshot) &&
                snapshot.State == ModState.WaitingForDependencies && consumer.Starts == 0,
                "Dependent remains dormant when required provider is absent");

            var producer = new TestModule(ProducerId, true);
            var producerHandle = core.Modules.Register(producer);
            cleanup.Add(producerHandle);
            yield return null; yield return null; yield return null;
            Check("B1.DependencyActivation", () => consumer.Starts == 1 && producer.Starts == 1 &&
                core.Modules.TryGet(ConsumerId, out var active) && active.State == ModState.Active &&
                transitive.Starts == 1,
                "Required provider starts before consumer and transitive dependent");
            Check("B1.ModDirectoryMerged", () => core.Mods.TryGet(ProducerId, out var entry) &&
                entry.IsModuleRegistered && entry.Module.State == ModState.Active,
                "Module registered in unified live directory");
            Check("B1.CrossModResolve", () => core.ServiceDirectory.TryResolveProvider<ICounterService>(ProducerId, out var service, new Version(1, 0)) &&
                service.Value == 17 && core.ServiceDirectory.Find(typeof(ICounterService)).Any(x => x.OwnerId == ProducerId),
                "Discover typed service from different module without hardcoding service ID");
            Check("B1.VersionRejection", () => !core.ServiceDirectory.TryResolveProvider<ICounterService>(ProducerId, out _, new Version(9, 0)),
                "Minimum version higher than provider is rejected");
            var extraService = core.Services.Register<ICounterService>(ProducerId, ProducerId + ":secondary", new CounterService());
            Check("B1.AmbiguousProvider", () => !core.ServiceDirectory.TryResolveProvider<ICounterService>(ProducerId, out _) &&
                core.ServiceDirectory.Find(typeof(ICounterService)).Count(x => x.OwnerId == ProducerId) == 2,
                "Multiple services from same provider need explicit service ID");
            extraService.Dispose();

            producerHandle.Dispose();
            yield return null; yield return null;
            Check("B1.ProviderRemoval", () => consumer.Stops == 1 && producer.Stops == 1 &&
                core.Modules.TryGet(ConsumerId, out var waiting) && waiting.State == ModState.WaitingForDependencies &&
                !core.ServiceDirectory.TryResolveProvider<ICounterService>(ProducerId, out _),
                "Dependent stopped safely; stays registered; provider service disposed");
            Check("B1.TransitiveDependencyCleanup", () => transitive.Stops == 1 &&
                core.Modules.TryGet(TransitiveId, out var nested) && nested.State == ModState.WaitingForDependencies,
                "Transitive dependents are stopped before upstream service removal");
            var replacement = new TestModule(ProducerId, true);
            var replacementHandle = core.Modules.Register(replacement);
            cleanup.Add(replacementHandle);
            yield return null; yield return null; yield return null;
            Check("B1.ProviderRecovery", () => replacement.Starts == 1 && consumer.Starts == 2 && transitive.Starts == 2 &&
                core.Modules.TryGet(ConsumerId, out var recovered) && recovered.State == ModState.Active,
                "Dependent reinitializes when provider returns");

            var aId = "urakka.foundation.b1verify.cyclea";
            var bId = "urakka.foundation.b1verify.cycleb";
            var cycleA = core.Modules.Register(new TestModule(aId, false, new[] { new ModDependency(bId) }));
            var cycleB = core.Modules.Register(new TestModule(bId, false, new[] { new ModDependency(aId) }));
            cleanup.Add(cycleA); cleanup.Add(cycleB);
            yield return null; yield return null;
            Check("B1.DependencyCycle", () => core.Modules.TryGet(aId, out var cycle) &&
                cycle.State == ModState.WaitingForDependencies && cycle.Message.IndexOf("cycle", StringComparison.OrdinalIgnoreCase) >= 0,
                "Cyclic dependencies remain inactive with explanatory diagnostic");

            transitiveHandle.Dispose(); consumerHandle.Dispose(); replacementHandle.Dispose(); cycleA.Dispose(); cycleB.Dispose();
            yield return null;
            Check("B1.ModuleCleanup", () => !core.Mods.TryGet(ProducerId, out _) && !core.Mods.TryGet(ConsumerId, out _) &&
                !core.ServiceDirectory.Find(typeof(ICounterService)).Any(x => x.OwnerId == ProducerId),
                "Explicit unload removes module IDs and service registrations");
        }
        private void Check(string id, Func<bool> condition, string details)
        {
            string status = "PASS", reason = details;
            try { if (!condition()) { status = "FAIL"; reason += " (condition false)"; } }
            catch (Exception ex) { status = "FAIL"; reason += " (" + ex.GetType().Name + ": " + ex.Message + ")"; }
            rows.Add(new Row { Id = id, Status = status, Details = reason });
            Logger.LogInfo(Tag + "[" + status + "] " + id + ": " + reason);
        }
        private static string Json(string value)
        {
            var s = new StringBuilder("\"");
            foreach (char c in value ?? "")
            {
                if (c == '\\') s.Append("\\\\");
                else if (c == '"') s.Append("\\\"");
                else if (c == '\n') s.Append("\\n");
                else if (c == '\r') s.Append("\\r");
                else if (c == '\t') s.Append("\\t");
                else if (c < ' ') s.Append("\\u" + ((int)c).ToString("x4"));
                else s.Append(c);
            }
            return s.Append('"').ToString();
        }
        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
        }
        private void WriteReport()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                string dll = typeof(FoundationCoreApi).Assembly.Location;
                var b = new StringBuilder();
                b.Append("{\n  \"source\": \"One-shot independent B1 runtime verifier\",\n");
                b.Append("  \"utc\": ").Append(Json(DateTime.UtcNow.ToString("o"))).Append(",\n");
                b.Append("  \"foundationVersion\": ").Append(Json(typeof(FoundationCoreApi).Assembly.GetName().Version.ToString())).Append(",\n");
                b.Append("  \"foundationSha256\": ").Append(Json(Hash(dll))).Append(",\n");
                b.Append("  \"checks\": [\n");
                for (int i = 0; i < rows.Count; i++)
                {
                    if (i > 0) b.Append(",\n");
                    var row = rows[i];
                    b.Append("    { \"id\": ").Append(Json(row.Id)).Append(", \"result\": ")
                        .Append(Json(row.Status)).Append(", \"details\": ").Append(Json(row.Details)).Append(" }");
                }
                b.Append("\n  ]\n}\n");
                File.WriteAllText(reportPath, b.ToString());
            }
            catch (Exception ex) { Logger.LogError(Tag + "Could not write one-shot report: " + ex); }
        }
    }
}
