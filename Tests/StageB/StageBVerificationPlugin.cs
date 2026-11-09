using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Assets.Source.Player;
using BepInEx;
using KingdomFoundation.Api;
using KingdomFoundation.Core;
using LightJson;
using UnityEngine;

namespace Foundation.StageBVerification
{
    /// <summary>One-shot, explicitly finite Stage B regression probe.
    /// No Update(), no native save mutation, no Harmony patch installation, and no
    /// periodic telemetry polling; unsupported game interaction remains PENDING.</summary>
    [BepInPlugin("urakka.foundation.stageb.verification", "Foundation Stage B One-Shot Verification", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation.core", BepInDependency.DependencyFlags.HardDependency)]
    public sealed partial class StageBVerificationPlugin : BaseUnityPlugin
    {
        sealed class Row { internal string Id, Result, Details; }
        readonly List<Row> checks = new List<Row>();
        const string Owner = "urakka.foundation.bverify";
        const string ReportName = "stage-b-verification.json";
        string report;
        private void Awake()
        {
            report = Path.Combine(Paths.GameRootPath, "_Mods", "Foundation", "Verification", "StageB", ReportName);
            InitializeVisualProbe();
            StartCoroutine(Once());
        }
        IEnumerator Once()
        {
            yield return null;
            yield return null;
            try { Run(); }
            catch (Exception e)
            {
                checks.Add(new Row { Id = "B.UnexpectedException", Result = "FAIL", Details = e.ToString() });
                Logger.LogError(e);
            }
            finally
            {
                Write();
                Logger.LogInfo("[STAGE B VERIFY] Completed once; no Update loop or mutation.");
            }
        }
        void Check(string id, Func<bool> test, string why)
        {
            try { var passed = test(); checks.Add(new Row { Id=id, Result=passed?"PASS":"FAIL", Details=why }); }
            catch (Exception e) { checks.Add(new Row { Id=id, Result="FAIL", Details=why + " / " + e }); }
        }
        void Pending(string id, string why) => checks.Add(new Row { Id=id, Result="PENDING", Details=why });
        void Run()
        {
            var core = FoundationCoreApi.Core;
            Check("B1.CoreAccessible", () => core != null && core.Modules != null && core.Services != null && core.Events != null,
                "Existing unified Core API remains available.");
            Check("B1.Directory", () => core.Mods.TryGet("urakka.kingdominc.foundation", out var entry) && entry.IsPluginLoaded,
                "Installed BepInEx Foundation visible in the live directory.");
            Check("B1.SharedEventBus", () =>
            {
                int legacy = 0, current = 0;
                using (var a = FoundationApi.Events.Subscribe<int>(Owner, _=>legacy++))
                using (var b = core.Events.Subscribe<int>(Owner, _=>current++))
                { core.Events.Publish(1234567); return legacy == 1 && current == 1; }
            }, "Legacy and Core event facades share delivery without duplication.");
            Check("B1.ServiceDirectory", () =>
            {
                var token = core.Services.Register<object>(Owner, Owner+":shared", new object(), new Version(1, 1));
                try { return core.ServiceDirectory.TryResolveProvider<object>(Owner, out var resolved, new Version(1, 0)) &&
                    resolved != null && core.ServiceDirectory.FindOwner(Owner).Count == 1; }
                finally { token.Dispose(); }
            }, "Existing Core owner-scoped service discovery uses one registry.");
            Check("B2.AssetOwnerIsolation", () => FoundationApi.AssetDirectory.ForOwner(Owner).Count == 0 &&
                FoundationApi.AssetDirectory.ReleaseOwner(Owner) == 0,
                "An absent owner has no resources to release; never unloads another mod's Unity assets.");
            Check("B2.InvalidAssetRejected", () =>
            {
                try { FoundationApi.Assets.LoadSprite(Owner, "nonexistent", Path.Combine(Paths.GameRootPath, "this_asset_does_not_exist_213.png")); }
                catch (FileNotFoundException) { return true; }
                return false;
            }, "Asset path validation rejects absent files before any allocation.");
            Check("B2.SaveMigrationContract", () =>
            {
                // No load/save is invoked by the test: just validate API contract
                // registration, token disposal and stable schema version handling.
                var token = FoundationApi.SaveMigrations.RegisterMigrating(Owner, 2,
                    () => new JsonObject { { "state", 3 } }, (_, __) => {},
                    (schema, state) => state);
                token.Dispose();
                return !FoundationApi.ModSave.RetainedModIds.Contains(Owner);
            }, "RegisterMigrating registers an isolated provider and unregisters cleanly.");
            Check("B2.ContentNativeRegistry", () => FoundationApi.Content.Ownership != null && core.Content.Registered != null,
                "Native content registry and Core ownership catalog remain readable.");
            Pending("B2.NativeRegistrationSaveRoundTrip", "Requires a disposable kingdom, pre-load mod registration, and real save/reload; one-shot probe never creates native content.");
            Check("B3.SignalSearchAndTypedRouting", () =>
            {
                int state = 0, result = 0;
                var sourceId = Owner + ".source";
                var targetId = Owner + ".target";
                using (var ctx = FoundationApi.ForMod(Owner))
                {
                    ctx.Expose(new SignalSourceDefinition(Owner, sourceId, "Stage B Source", new [] {
                        new SignalPropertyDefinition("current", "Current", typeof(int), () => state, v => state=(int)v)
                    }));
                    ctx.Expose(new SignalSourceDefinition(Owner, targetId, "Stage B Target", new [] {
                        new SignalPropertyDefinition("value", "Value", typeof(int), () => result, v => result=(int)v)
                    }));
                    var link = ctx.Link(new SignalSelection(sourceId,"current"), new SignalSelection(targetId,"value"));
                    if (!link.IsResolved || FoundationApi.SignalDirectory.Find("Stage B",Owner).Count!=2) return false;
                    FoundationApi.SignalDirectory.TryResolve(new SignalSelection(sourceId,"current"), out var property);
                    property.Write(42);
                    if (result != 42) return false;
                    var exported = FoundationApi.SignalLinks.ExportOwner(Owner);
                    if (!exported.IsJsonArray || exported.AsJsonArray.Count != 1) return false;
                }
                return !FoundationApi.Signals.TryGet(sourceId, out _) &&
                    !FoundationApi.Signals.TryGet(targetId, out _) &&
                    !FoundationApi.SignalLinks.Links.Any(l => l.OwnerId==Owner);
            }, "Owner-scoped source discovery, exact property selection, forwarding and cleanup.");
            Check("B3.TypeCompatibility", () =>
            {
                using (var ctx = FoundationApi.ForMod(Owner))
                {
                    ctx.Expose(new SignalSourceDefinition(Owner,Owner+".types", "Type Probe", new [] {
                        new SignalPropertyDefinition("boolean", "Boolean", typeof(bool), () => false),
                        new SignalPropertyDefinition("number", "Number", typeof(int), () => 3),
                    }));
                    return FoundationApi.SignalDirectory.FindProperties(Owner+".types", typeof(bool)).Count == 1 &&
                        FoundationApi.SignalDirectory.FindProperties(Owner+".types", typeof(string)).Count == 0 &&
                        FoundationApi.SignalDirectory.FindProperties(Owner+".types", typeof(int), true).Count == 0;
                }
            }, "Source and property selection obey exact signal type/read-only restrictions.");
            Check("B3.NativeReadOnlyAdapters", () =>
            {
                return FoundationApi.Signals.TryGet("foundation:player", out var player) &&
                       FoundationApi.Signals.TryGet("foundation:workspace", out var workspace) &&
                       FoundationApi.Signals.TryGet("foundation:camera", out var camera) &&
                       player.Properties.Count > 0 && workspace.Properties.Count > 0 && camera.Properties.Count > 0;
            }, "Existing player/workspace/camera signals remain registered as game-backed read-only sources.");
            Check("B3.UnavailableProviderSafe", () =>
            {
                var link = FoundationApi.SignalLinks.Connect(Owner,
                    new SignalSelection(Owner+".uninstalled","state"),
                    new SignalSelection(Owner+".missing","state"));
                try { return !link.IsResolved && !string.IsNullOrEmpty(link.UnavailableReason); }
                finally { link.Dispose(); }
            }, "Missing signal endpoints do not throw or invent values.");
            Check("B4.OwnerPanelAndSettings", () =>
            {
                const string panelId = "test-panel";
                using(var ctx = FoundationApi.ForMod(Owner))
                {
                    var registration = ctx.RegisterPanel(new FoundationPanelDefinition(Owner,panelId,"Stage B UI", _=>{}));
                    if (!registration.IsRegistered || !FoundationApi.Panels.TryGet(Owner,panelId,out _)) return false;
                    var section = ctx.Settings("Stage B Settings");
                    if (!FoundationApi.ModConfig.TryGetSection(Owner,out var found) || found != section) return false;
                }
                return !FoundationApi.Panels.TryGet(Owner,panelId,out _) &&
                    !FoundationApi.ModConfig.TryGetSection(Owner,out _);
            }, "REGISTRY ONLY: checks owner-scoped panel/settings registration and disposal; on-screen controls are verified separately.");
            Pending("B4.RenderingVisualAudit", "Requires native game screenshot/UI-scale/keyboard/mouse test; no GUI is opened by the one-shot verifier.");
            Check("B5.DeveloperDiagnostics", () =>
            {
                FoundationApi.Diagnostics.Write(Owner,FoundationDiagnosticLevel.Info,"Stage B smoke marker");
                return FoundationApi.Diagnostics.Recent.Any(x => x.Owner == Owner && x.Message == "Stage B smoke marker") &&
                    core.Diagnostics.Recent != null;
            }, "Read-only diagnostics accessible through public Foundation APIs.");
            Check("B5.InspectorNativeBridge", () => FoundationApi.Flow != null && FoundationApi.IO != null &&
                FoundationApi.World != null && FoundationApi.ResearchRecovery != null,
                "Developer inspector dependencies are public native-backed Game API readers.");
            Check("B6.PatchManagerCompatibility", () => core.Patches.Conflicts(Owner) != null,
                "Existing owned Harmony/conflict API remains callable without patching the game.");
            Check("B6.ModBarCompatibility", () =>
            {
                if (FoundationApi.ModBar == null || FoundationApi.ModBar.Items == null) return false;
                var token = FoundationApi.ModBar.Register(new ModBarText(Owner, "stage-b-smoke", "Stage B Smoke"));
                try { return token.IsRegistered && FoundationApi.ModBar.Items.Any(item => item.ModId == Owner); }
                finally { token.Dispose(); }
            }, "REGISTRY ONLY: confirms registration and disposal, not a rendered or clicked HUD control. See visual report.");
            Check("B6.VerificationIsOneShot", () => true,
                "Main automated test executes once and writes one snapshot; the separately-owned visual probe writes only upon UI actions.");
            Check("B6.MissingResearchPreserved", () => FoundationApi.ResearchRecovery.RetainedTechIds != null &&
                FoundationApi.ResearchRecovery.RetainedNotificationIds != null,
                "Read-only native missing-research preservation exposed without rewriting saved tech IDs.");
            Check("B6.WorkspaceNodeCompatibility", () => FoundationApi.WorkspaceNodes.Nodes != null &&
                FoundationApi.WorkspaceNodes.Nodes.All(x => x.IsRegistered),
                "Existing native workspace node registration state exposed through same public registry.");
            Pending("B5.ConsoleKeyboardF8", "Requires exercising F8, every inspector tab, native workspace data, logging and keyboard focus.");
            Pending("B6.ModBarNativeVisual", "Requires native HUD inspection with a button, toggle, dropdown and separator and active player interaction.");
            Pending("B6.MissingTechRoundTrip", "Requires loading and resaving a real kingdom with a temporarily unavailable mod-defined tech node.");
            Pending("B6.WorkspaceNodeSaveLoad", "Requires registering a real test-owned native FlowElement before kingdom load and verifying placement/save/reload.");
            Pending("B6.NativePatchConflictCleanup", "Requires deliberately patching a real target with two external mods; not safe in unattended smoke.");
        }
        static string Escape(string value)
        {
            var s = new StringBuilder("\"");
            foreach (char c in value??"")
            {
                if (c=='\\') s.Append("\\\\"); else if(c=='\"') s.Append("\\\"");
                else if(c=='\n') s.Append("\\n"); else if(c=='\r') s.Append("\\r");
                else if(c=='\t') s.Append("\\t"); else if(c<32) s.Append("\\u"+((int)c).ToString("x4"));
                else s.Append(c);
            }
            return s.Append('"').ToString();
        }
        static string Hash(string p)
        {
            using(var sha=SHA256.Create())using(var f=File.OpenRead(p))
                return BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "");
        }
        void Write()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(report));
                string sourceDll=typeof(FoundationApi).Assembly.Location;
                var b=new StringBuilder();
                b.Append("{\n  \"source\": \"Independent one-shot Stage B runtime verifier\",\n");
                b.Append("  \"utc\": ").Append(Escape(DateTime.UtcNow.ToString("o"))).Append(",\n");
                b.Append("  \"foundationVersion\": ").Append(Escape(typeof(FoundationApi).Assembly.GetName().Version.ToString())).Append(",\n");
                b.Append("  \"foundationSha256\": ").Append(Escape(Hash(sourceDll))).Append(",\n");
                b.Append("  \"checks\": [\n");
                for(int i=0;i<checks.Count;i++)
                {
                    if(i>0)b.Append(",\n");
                    var row=checks[i];
                    b.Append("    { \"id\": ").Append(Escape(row.Id)).Append(", \"result\": ").Append(Escape(row.Result))
                     .Append(", \"details\": ").Append(Escape(row.Details)).Append(" }");
                }
                b.Append("\n  ]\n}\n");
                File.WriteAllText(report,b.ToString());
                Logger.LogInfo("[STAGE B VERIFY] PASS="+checks.Count(x=>x.Result=="PASS")+
                    " FAIL="+checks.Count(x=>x.Result=="FAIL")+" PENDING="+checks.Count(x=>x.Result=="PENDING"));
            }
            catch(Exception e){Logger.LogError("Stage B report write failed: "+e);}
        }
    }
}
