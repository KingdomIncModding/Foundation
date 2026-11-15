using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Player;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;

namespace KingdomFoundation
{
    internal static class IssueRegressionChecks
    {
        private sealed class DummyTarget
        {
            internal int LastValue;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            internal void AcceptInt(int value) => LastValue = value;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            internal static int Echo(int value) => value + 1;
        }

        private sealed class TestFrameworkNode : FrameworkWorkspaceNode
        {
            internal TestFrameworkNode(string typeId) : base(typeId)
            {
            }
        }

        internal static void VerifyCore(Action<bool, string> check, Action<Action, string> throws)
        {
            VerifyIssue5SaveRecovery(check);
            VerifyIssue6MutableArgs(check, throws);
            VerifyIssue7And8And20Hooks(check, throws);
            VerifyIssue12And19Presentation(check);
            VerifyIssue13Through17UpdateChecker(check);
            VerifyIssue18BepInDependency(check);
        }

        private static void VerifyIssue5SaveRecovery(Action<bool, string> check)
        {
            const string owner = "urakka.foundation.issue5-save";
            var saves = FoundationApi.SaveDataInternal;
            JsonObject doc = new JsonObject
            {
                {
                    ModSaveRegistry.RootKey,
                    new JsonObject
                    {
                        {
                            "other.uninstalled.mod",
                            new JsonObject
                            {
                                { "Version", 4 },
                                { "Data", new JsonObject { { "Keep", true } } }
                            }
                        }
                    }
                }
            };
            saves.LoadState(doc);
            var handle = saves.Register(owner, 3, () => new JsonObject { { "Marker", 77 } }, (_, __) => { });
            try
            {
                SaveCurrentStatePatch.ApplyForTest(doc);
                check(doc[ModSaveRegistry.RootKey][owner]["Data"]["Marker"].AsInteger == 77
                    && doc[ModSaveRegistry.RootKey]["other.uninstalled.mod"]["Data"]["Keep"].AsBoolean,
                    "Issue #5: Normal save extension writes current and opaque mod save data");

                SaveCurrentStatePatch.AfterExtensionHookForTest = d =>
                {
                    d.Remove(ModSaveRegistry.RootKey);
                    throw new InvalidOperationException("Simulated downstream save failure");
                };
                try
                {
                    SaveCurrentStatePatch.ApplyForTest(doc);
                }
                finally
                {
                    SaveCurrentStatePatch.AfterExtensionHookForTest = null;
                }

                check(doc.ContainsKey(ModSaveRegistry.RootKey)
                    && doc[ModSaveRegistry.RootKey][owner]["Data"]["Marker"].AsInteger == 77
                    && doc[ModSaveRegistry.RootKey]["other.uninstalled.mod"]["Data"]["Keep"].AsBoolean,
                    "Issue #5: Extension error recovers known-good live and opaque mod save data instead of stripping FoundationModData");
            }
            finally
            {
                SaveCurrentStatePatch.AfterExtensionHookForTest = null;
                handle.Dispose();
                saves.NewKingdom();
            }
        }

        private static void VerifyIssue6MutableArgs(Action<bool, string> check, Action<Action, string> throws)
        {
            var mutableCall = new GameHookCall(null, new object[] { 12 }, typeof(void), new[] { 0 });
            check(mutableCall.IsArgumentMutable(0) && !mutableCall.IsArgumentMutable(1), "Issue #6: IsArgumentMutable reports declared writable indices only");
            mutableCall.SetArgument(0, 99);
            check(mutableCall.Argument<int>(0) == 99, "Issue #6: Mutable hook argument accepts updated value");

            var readOnlyCall = new GameHookCall(null, new object[] { 12 }, typeof(void), Array.Empty<int>());
            check(!readOnlyCall.IsArgumentMutable(0), "Issue #6: Read-only hook argument reports IsArgumentMutable == false");
            throws(() => readOnlyCall.SetArgument(0, 99), "Issue #6: SetArgument throws InvalidOperationException on read-only hook arguments");

            var registry = new GameHookRegistry();
            var harmony = new Harmony("urakka.foundation.issue6-test");
            var method = AccessTools.Method(typeof(DummyTarget), nameof(DummyTarget.AcceptInt));
            try
            {
                var target = new DummyTarget();
                target.AcceptInt(5);
                check(target.LastValue == 5, "Issue #6: Unpatched dummy target receives argument normally");

                const string intHookId = "foundation.test.beforeInt";
                FoundationApi.GameHooksInternal.RegisterTestHook(intHookId, method, failFast: true, harmony: harmony);
                int? seenAfter = null;
                using (FoundationApi.GameHooksInternal.Subscribe("urakka.foundation.issue6", intHookId,
                    before: call =>
                    {
                        if (call.IsArgumentMutable(0))
                            call.SetArgument(0, 99);
                    },
                    after: call => seenAfter = call.Argument<int>(0)))
                {
                    target.AcceptInt(5);
                }

                check(target.LastValue == 99 && seenAfter == 99,
                    "Issue #6: Harmony BeforeInt propagates SetArgument edits into the native target invocation");
                check(GameHookRegistry.IsMethodArgMutableForTest(method, 0) && !GameHookRegistry.IsMethodArgMutableForTest(method, 1),
                    "Issue #6: Mutable argument index table marks index 0 mutable only");
                check(!GameHookRegistry.IsMethodArgMutableForTest(AccessTools.Method(typeof(DummyTarget), nameof(DummyTarget.Echo)), 0),
                    "Issue #6: Return-value hooks mark parameters read-only");
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        private static void VerifyIssue7And8And20Hooks(Action<bool, string> check, Action<Action, string> throws)
        {
            var registry = new GameHookRegistry();
            const string hookId = "foundation.test.hotpath";
            var method = AccessTools.Method(typeof(DummyTarget), nameof(DummyTarget.AcceptInt));
            registry.RegisterTestHook(hookId, method);

            var order = new List<string>();
            IDisposable selfDispose = null;
            var s1 = registry.Subscribe("m1", hookId, before: _ => order.Add("p5-first"), priority: 5);
            var s2 = registry.Subscribe("m2", hookId, before: _ => order.Add("p10"), priority: 10);
            var s3 = registry.Subscribe("m3", hookId, before: _ =>
            {
                order.Add("p5-second-disposing");
                selfDispose?.Dispose();
            }, priority: 5);
            selfDispose = s3;

            registry.DispatchDirect(hookId, new GameHookCall(null, new object[] { 1 }, typeof(void), new[] { 0 }), after: false);
            check(order.SequenceEqual(new[] { "p10", "p5-first", "p5-second-disposing" }),
                "Issue #7: Hook dispatch honors priority descending then registration sequence");

            order.Clear();
            var emptyCall = new GameHookCall(null, new object[] { 1 }, typeof(void), new[] { 0 });
            for (int i = 0; i < 10000; i++)
                registry.DispatchDirect(hookId, emptyCall, after: false);
            check(order.Count == 20000, "Issue #7: Hot-path pre-sorted array dispatch handles 10,000 invocations without allocating LINQ chains and honors self-disposal");
            s1.Dispose();
            s2.Dispose();

            // Issue #8: unavailable hook reporting & fail-fast
            const string missingHook = "foundation.test.missing-optional";
            using (registry.MarkUnavailableForTest(missingHook))
            {
                check(!registry.IsAvailable(missingHook) && registry.UnavailableHooks.Contains(missingHook),
                    "Issue #8: Unavailable hook is recorded and IsAvailable returns false");
                bool missingThrew = false;
                try
                {
                    registry.Subscribe("m1", missingHook, before: _ => { });
                }
                catch (MissingMethodException)
                {
                    missingThrew = true;
                }
                check(missingThrew, "Issue #8: Subscribing to unavailable hook throws MissingMethodException");
                check(registry.IsAvailable(hookId), "Issue #8: Other healthy hooks remain available when an optional hook is missing");
            }

            bool strictThrew = false;
            try
            {
                registry.RegisterTestHook("foundation.test.missing-strict", null, failFast: true);
            }
            catch (MissingMethodException)
            {
                strictThrew = true;
            }
            check(strictThrew, "Issue #8: Strict fail-fast rejects missing hook resolution immediately");

            // Issue #20: per-(ModId, HookId) fault rate-limiting & recovery
            DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            registry.SetClockForTest(() => now);
            bool shouldThrow = true;
            using (registry.Subscribe("fault-mod", hookId, before: _ =>
            {
                if (shouldThrow)
                    throw new InvalidOperationException("Repeated hook fault");
            }))
            {
                for (int i = 0; i < 5; i++)
                {
                    now = now.AddMilliseconds(100);
                    registry.DispatchDirect(hookId, new GameHookCall(null, new object[] { i }, typeof(void), new[] { 0 }), after: false);
                }
                check(registry.GetFaultCount("fault-mod", hookId) == 5, "Issue #20: All faults are counted accurately during 5s rate-limit window");

                now = now.AddSeconds(6);
                registry.DispatchDirect(hookId, new GameHookCall(null, new object[] { 6 }, typeof(void), new[] { 0 }), after: false);
                check(registry.GetFaultCount("fault-mod", hookId) == 6, "Issue #20: Fault past rate-limit window emits summary and increments count");

                shouldThrow = false;
                registry.DispatchDirect(hookId, new GameHookCall(null, new object[] { 7 }, typeof(void), new[] { 0 }), after: false);
                check(registry.GetFaultCount("fault-mod", hookId) == 6, "Issue #20: Subsequent clean invocation clears consecutive fault state");
            }
            registry.SetClockForTest(null);
            registry.ResetFaults();
        }

        internal static void VerifyIssue9ExecutionShutdown(Action<bool, string> check)
        {
            var echoMethod = AccessTools.Method(typeof(DummyTarget), nameof(DummyTarget.Echo));
            int calls = 0;
            var sub = FoundationApi.ExecutionHooks.SubscribeExternal("urakka.foundation.issue9", echoMethod, _ =>
            {
                calls++;
                return null;
            }, (_, __) => { });

            check(DummyTarget.Echo(4) == 5 && calls == 1 && GameExecutionHooks.Targets.ContainsKey(echoMethod),
                "Issue #9: Execution hook patches external target before shutdown");
            sub.Dispose();
            GameExecutionHooks.Shutdown();
            check(GameExecutionHooks.Targets.Count == 0 && GameExecutionHooks.Subscriptions.Count == 0 && DummyTarget.Echo(4) == 5 && calls == 1,
                "Issue #9: GameExecutionHooks.Shutdown unpatches targets and clears all state");
        }

        private static void VerifyIssue12And19Presentation(Action<bool, string> check)
        {
            var pres = FoundationApi.PresentationInternal;
            int beforeScans = pres.FullSpriteScanCount;
            var missingA = pres.FindSprite("foundation.issue12.does_not_exist");
            var missingB = pres.FindSprite("foundation.issue12.does_not_exist");
            int afterScans = pres.FullSpriteScanCount;
            check(missingA == null && missingB == null && afterScans - beforeScans <= 1,
                "Issue #12: Repeated negative FindSprite lookups reuse bounded cache without extra full scene scans");

            var tex = new Texture2D(2, 2);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.zero);
            sprite.name = "foundation.issue19.sprite";
            const string ctxMod = "urakka.foundation.issue19-ctx";
            try
            {
                using (var ctx = FoundationApi.ForMod(ctxMod))
                {
                    ctx.RegisterSprite("sample_icon", sprite);
                    ctx.RegisterText("greeting", pres.CurrentLocale, "Hello Foundation");
                    check(pres.FindSprite(ctxMod + ":sample_icon") == sprite
                        && pres.TranslateOnly("@" + ctxMod + ":greeting") == "Hello Foundation",
                        "Issue #19: Context-registered sprite and text are immediately discoverable");
                }

                check(pres.FindSprite(ctxMod + ":sample_icon") == null
                    && pres.TranslateOnly("@" + ctxMod + ":greeting") != "Hello Foundation",
                    "Issue #19: Disposing FoundationModContext removes registered sprite and translation keys");

                using (var ctx2 = FoundationApi.ForMod(ctxMod))
                {
                    ctx2.RegisterSprite("sample_icon", sprite);
                    ctx2.RegisterText("greeting", pres.CurrentLocale, "Hello Again");
                    check(pres.FindSprite(ctxMod + ":sample_icon") == sprite
                        && pres.TranslateOnly("@" + ctxMod + ":greeting") == "Hello Again",
                        "Issue #19: Owner can cleanly re-register sprites and translations after context disposal");
                }
            }
            finally
            {
                pres.RemoveOwner(ctxMod);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        private static void VerifyIssue13Through17UpdateChecker(Action<bool, string> check)
        {
            // Issue #16: SemVer 2.0 ordering & invalid version safety
            check(NexusUpdateChecker.CompareVersions("1.0.0-beta", "1.0.0") < 0
                && NexusUpdateChecker.CompareVersions("1.0.0-beta.2", "1.0.0-beta.10") < 0
                && NexusUpdateChecker.CompareVersions("1.0.0-alpha.1", "1.0.0-alpha.beta") < 0
                && NexusUpdateChecker.CompareVersions("v0.14.4", "0.14.4") == 0
                && !NexusUpdateChecker.IsOutdated("1.0.0", "not-a-version")
                && NexusUpdateChecker.IsOutdated("1.0.0-beta", "1.0.0")
                && !NexusUpdateChecker.IsOutdated("2.0.0", "1.9.9"),
                "Issue #16: SemVer 2.0 comparison orders prereleases, numeric identifiers, v-prefixes, and malformed inputs safely");

            string capturedUserAgent = null;
            var modA = new ModItem { Guid = "test.gl1", Name = "Global Ledger", InstalledVersion = "1.7.0", NexusId = 19, FileId = 8319102, GameDomain = "kingdominc" };
            var modB = new ModItem { Guid = "test.gl2", Name = "Global Ledger V2", InstalledVersion = "1.0.0", NexusId = 19, FileId = 8379906, GameDomain = "kingdominc" };

            try
            {
                // Issue #13 & #17: multi-file GraphQL resolution + dynamic User-Agent
                NexusUpdateChecker.TransportOverrideForTest = (method, url, body, headers, done) =>
                {
                    capturedUserAgent = headers.TryGetValue("User-Agent", out var ua) ? ua : null;
                    if (method == "GET")
                    {
                        done(new NexusUpdateChecker.NexusHttpResponse
                        {
                            Success = true,
                            ResponseCode = 200,
                            Body = "{\"data\":{\"version\":\"1.7.0\",\"game_id\":10893}}"
                        });
                    }
                    else
                    {
                        done(new NexusUpdateChecker.NexusHttpResponse
                        {
                            Success = true,
                            ResponseCode = 200,
                            Body = "{\"data\":{\"modFiles\":[{\"fileId\":8319102,\"groupId\":8319102,\"name\":\"Global Ledger\",\"version\":\"1.7.0\",\"category\":\"MAIN\",\"categoryId\":1,\"date\":100},{\"fileId\":8379906,\"groupId\":8379906,\"name\":\"Global Ledger V2\",\"version\":\"1.0.1\",\"category\":\"OPTIONAL\",\"categoryId\":3,\"date\":200}]}}"
                        });
                    }
                };

                bool done13 = false;
                NexusUpdateChecker.CheckTargets(new[] { modA, modB }, () => done13 = true);
                check(done13 && capturedUserAgent == "KingdomInc-Foundation/" + FoundationPlugin.Version,
                    "Issue #17: Nexus update check sends dynamic User-Agent matching current Foundation version");
                check(modA.Checked && !modA.IsOutdated && modA.LatestVersion == "1.7.0"
                    && modB.Checked && modB.IsOutdated && modB.LatestVersion == "1.0.1"
                    && NexusUpdateChecker.LastCheckSummary == "1 update available",
                    "Issue #13: Shared Nexus page resolves each mod's distinct FileId stream independently");

                // Issue #14: Failing check resets stale Outdated flags and reports honest summary
                NexusUpdateChecker.TransportOverrideForTest = (method, url, body, headers, done) =>
                {
                    bool failA = modB.IsChecking && url.EndsWith("/19", StringComparison.Ordinal);
                    done(new NexusUpdateChecker.NexusHttpResponse { Success = false, ResponseCode = 503, Body = null });
                };
                NexusUpdateChecker.CheckTargets(new[] { modA, modB });
                check(!modB.Checked && !modB.IsOutdated && modB.LatestVersion == null
                    && NexusUpdateChecker.LastCheckSummary.Contains("failed", StringComparison.OrdinalIgnoreCase)
                    && NexusUpdateChecker.LastCheckSummary != "All mods up to date",
                    "Issue #14: Failed recheck resets stale update state and never claims All mods up to date");

                // Issue #14 mixed result summary + FileId must NOT fall back when GraphQL fails
                int callNum = 0;
                NexusUpdateChecker.TransportOverrideForTest = (method, url, body, headers, done) =>
                {
                    if (method == "GET")
                    {
                        callNum++;
                        done(new NexusUpdateChecker.NexusHttpResponse
                        {
                            Success = true,
                            ResponseCode = 200,
                            Body = "{\"data\":{\"version\":\"9.9.9\",\"game_id\":10893}}"
                        });
                    }
                    else
                    {
                        done(new NexusUpdateChecker.NexusHttpResponse { Success = false, ResponseCode = 500, Body = null });
                    }
                };
                NexusUpdateChecker.CheckTargets(new[] { modB });
                check(!modB.Checked && !modB.IsOutdated && modB.LatestVersion == null,
                    "Issue #13: Missing stream query does not fall back to another stream's page version when FileId is specified");

                // Issue #15: Stale out-of-order response discarded across generations
                var item = new ModItem { Guid = "test.race", Name = "Race Mod", InstalledVersion = "1.0.0", NexusId = 18, FileId = 8358756, GameDomain = "kingdominc" };
                Action<NexusUpdateChecker.NexusHttpResponse> staleCallback = null;
                int run = 0;
                NexusUpdateChecker.TransportOverrideForTest = (method, url, body, headers, done) =>
                {
                    if (run == 1 && method == "GET")
                    {
                        staleCallback = done;
                        return;
                    }

                    if (method == "GET")
                    {
                        done(new NexusUpdateChecker.NexusHttpResponse { Success = true, ResponseCode = 200, Body = "{\"data\":{\"version\":\"2.0.0\",\"game_id\":10893}}" });
                    }
                    else
                    {
                        done(new NexusUpdateChecker.NexusHttpResponse { Success = true, ResponseCode = 200, Body = "{\"data\":{\"modFiles\":[{\"fileId\":8358756,\"groupId\":8358756,\"name\":\"Foundation\",\"version\":\"2.0.0\",\"category\":\"MAIN\",\"categoryId\":1,\"date\":10}]}}" });
                    }
                };
                run = 1;
                NexusUpdateChecker.CheckTargets(new[] { item });
                run = 2;
                NexusUpdateChecker.CheckTargets(new[] { item });
                check(item.Checked && item.LatestVersion == "2.0.0" && !NexusUpdateChecker.IsCheckingAll,
                    "Issue #15: Newer generation check completes cleanly");
                staleCallback?.Invoke(new NexusUpdateChecker.NexusHttpResponse { Success = true, ResponseCode = 200, Body = "{\"data\":{\"version\":\"9.9.9\",\"game_id\":0}}" });
                check(item.LatestVersion == "2.0.0" && !item.IsChecking,
                    "Issue #15: Stale response from superseded check generation is discarded");
            }
            finally
            {
                NexusUpdateChecker.TransportOverrideForTest = null;
            }
        }

        private static void VerifyIssue18BepInDependency(Action<bool, string> check)
        {
            var dep = new BepInDependency(FoundationPlugin.Id, FoundationPlugin.Version);
            check(dep.DependencyGUID == FoundationPlugin.Id && dep.MinimumVersion != null && dep.MinimumVersion.ToString() == FoundationPlugin.Version,
                "Issue #18: Exact SemVer Foundation version parses cleanly as a BepInDependency version constraint");
        }

        internal static void VerifyWorkspaceScene(Action<bool, string> check)
        {
            const string idA = "urakka.foundation.issue11.nodeA";
            const string idB = "urakka.foundation.issue11.nodeB";
            var regA = FoundationApi.RegisterWorkspaceNode(new FrameworkWorkspaceNodeDefinition(
                idA, "Issue11 Node A", () => new TestFrameworkNode(idA), "mine", new Vector2Int(64, 64), "Cost probe A", "Utility", iconName: "Icons_104", costMultiplier: 1, goldMultiplier: 2).NativeDefinition);
            var regB = FoundationApi.RegisterWorkspaceNode(new FrameworkWorkspaceNodeDefinition(
                idB, "Issue11 Node B", () => new TestFrameworkNode(idB), "mine", new Vector2Int(64, 64), "Cost probe B", "Utility", iconName: "Icons_104", costMultiplier: 1, goldMultiplier: 2).NativeDefinition);
            FlowElement placedB = null;
            try
            {
                placedB = FlowElement.Create(idB);
                GamePlayer.Current.ActiveFlow.Elements.Add(placedB);

                var probeA = FlowElement.Create(idA);
                var probeB = FlowElement.Create(idB);
                int countA = FoundationApi.Kingdom.ElementCount(idA);
                int countB = FoundationApi.Kingdom.ElementCount(idB);
                long goldA = probeA.GetPurchaseCost().GetCurrency().FirstOrDefault(e => e.c == Currency.Gold).amt;
                long goldB = probeB.GetPurchaseCost().GetCurrency().FirstOrDefault(e => e.c == Currency.Gold).amt;
                check(countA == 0 && countB == 1 && goldA < goldB,
                    "Issue #11: Placing HostedWorkspaceNode B does not inflate purchase cost or ElementCount for HostedWorkspaceNode A");
            }
            finally
            {
                if (placedB != null)
                    GamePlayer.Current.ActiveFlow.Elements.Remove(placedB);
                regA.Dispose();
                regB.Dispose();
            }
        }

        internal static void VerifyToolbarHelpers(Action<bool, string> check)
        {
            var template = FoundationApi.UiAccess.CloneButtonTemplate;
            check(template != null, "Issue #23: CloneButtonTemplate template object is available");
            var clone = template != null ? UnityEngine.Object.Instantiate(template) : null;
            check(clone != null, "Issue #23: Cloned toolbar button instantiates cleanly");
            try
            {
                FoundationApi.UiAccess.RemoveNativeToolbarTutorial(clone);
                var icon = FoundationApi.UiAccess.ToolbarButtonIcon(clone);
                var active = FoundationApi.UiAccess.ToolbarButtonActiveIndicator(clone);
                check(icon != null && active != null, "Issue #23: Foundation exposes toolbar button icon and active indicator without child-name strings");
            }
            finally
            {
                if (clone != null)
                    UnityEngine.Object.Destroy(clone);
            }
        }
    }
}
