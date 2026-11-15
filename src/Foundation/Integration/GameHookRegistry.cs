using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.HUD;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Behaviour.UI.Deco;
using Assets.Source.Player;
using Assets.Behaviour.UI.MainMenu;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameHookRegistry : IGameHookRegistry
    {
        private sealed class HookSlot
        {
            internal readonly List<GameHookSubscription> All = new List<GameHookSubscription>();
            internal GameHookSubscription[] BeforeSorted = Array.Empty<GameHookSubscription>();
            internal GameHookSubscription[] AfterSorted = Array.Empty<GameHookSubscription>();
            internal bool HasAny => BeforeSorted.Length > 0 || AfterSorted.Length > 0;

            internal void Rebuild()
            {
                BeforeSorted = All.Where(s => s.Active && s.Before != null).OrderByDescending(s => s.Priority).ThenBy(s => s.Sequence).ToArray();
                AfterSorted = All.Where(s => s.Active && s.After != null).OrderByDescending(s => s.Priority).ThenBy(s => s.Sequence).ToArray();
            }
        }

        private sealed class ActionToken : IDisposable
        {
            private readonly Action onDispose;
            private bool disposed;
            internal ActionToken(Action onDispose) => this.onDispose = onDispose;
            public void Dispose() { if (disposed) return; disposed = true; onDispose?.Invoke(); }
        }

        internal sealed class HookFaultState
        {
            internal long TotalFaults;
            internal long ConsecutiveFaults;
            internal long SuppressedSinceLastLog;
            internal DateTime LastLogUtc;
        }

        private static readonly HashSet<string> EssentialHooks = new HashSet<string>(StringComparer.Ordinal)
        {
            GameHookPoints.UiStarted,
            GameHookPoints.UiVisibility,
            GameHookPoints.WorkspaceShown,
            GameHookPoints.KingdomDeserialized,
            GameHookPoints.BuildingSerialize,
            GameHookPoints.BuildingDeserialize,
            GameHookPoints.MenuUpdate
        };

        private static readonly TimeSpan FaultLogInterval = TimeSpan.FromSeconds(5);

        private readonly object gate = new object();
        private readonly Dictionary<string, HookSlot> slots = new Dictionary<string, HookSlot>(StringComparer.Ordinal);
        private readonly HashSet<string> knownHooks = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> unavailableHooks = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<(string ModId, string HookId), HookFaultState> faultStates = new Dictionary<(string, string), HookFaultState>();
        private long nextSequence;
        private Func<DateTime> utcNow = () => DateTime.UtcNow;

        private static readonly Dictionary<MethodBase, string> Targets = new Dictionary<MethodBase, string>();
        private static readonly Dictionary<MethodBase, int[]> MutableArgsByMethod = new Dictionary<MethodBase, int[]>();

        internal static bool StrictFailFast =>
            string.Equals(Environment.GetEnvironmentVariable("FOUNDATION_STRICT_HOOKS"), "1", StringComparison.Ordinal);

        public IReadOnlyList<string> UnavailableHooks
        {
            get { lock (gate) return Array.AsReadOnly(unavailableHooks.OrderBy(id => id, StringComparer.Ordinal).ToArray()); }
        }

        public bool IsAvailable(string hookId)
        {
            if (string.IsNullOrWhiteSpace(hookId)) return false;
            lock (gate) return knownHooks.Contains(hookId) && !unavailableHooks.Contains(hookId);
        }

        internal void SetClockForTest(Func<DateTime> clock) => utcNow = clock ?? (() => DateTime.UtcNow);

        internal long GetFaultCount(string modId, string hookId)
        {
            lock (gate)
                return faultStates.TryGetValue((modId, hookId), out var state) ? state.TotalFaults : 0;
        }

        internal void ResetFaults(string modId = null, string hookId = null)
        {
            lock (gate)
            {
                if (modId == null && hookId == null) { faultStates.Clear(); return; }
                foreach (var key in faultStates.Keys.Where(k => (modId == null || k.ModId == modId) && (hookId == null || k.HookId == hookId)).ToArray())
                    faultStates.Remove(key);
            }
        }

        internal void Install(Harmony harmony, bool? failFast = null)
        {
            bool strict = failFast ?? StrictFailFast;
            var bindings = new (string Id, MethodBase Method)[]
            {
                (GameHookPoints.PortAcceptance, AccessTools.Method(typeof(FlowIO), nameof(FlowIO.CanAcceptItem))),
                (GameHookPoints.PortDelivery, AccessTools.Method(typeof(FlowInput), nameof(FlowInput.InputItem))),
                (GameHookPoints.BuildingClearPorts, AccessTools.Method(typeof(FlowElement), nameof(FlowElement.ClearIO))),
                (GameHookPoints.CurrencyAdded, AccessTools.Method(typeof(GamePlayer), nameof(GamePlayer.AddCurrency), new[] { typeof(Currency), typeof(long), typeof(bool) })),
                (GameHookPoints.CurrencySpent, AccessTools.Method(typeof(GamePlayer), nameof(GamePlayer.SpendCurrency), new[] { typeof(Currency), typeof(long) })),
                (GameHookPoints.PortConsumed, AccessTools.Method(typeof(FlowIO), nameof(FlowIO.ConsumeItem), Type.EmptyTypes)),
                (GameHookPoints.UiVisibility, AccessTools.Method(typeof(GameUI), nameof(GameUI.ToggleUI))),
                (GameHookPoints.GhostCommit, AccessTools.Method(typeof(WorldFlowGhost), nameof(WorldFlowGhost.OnDragDropEnd))),
                (GameHookPoints.GhostCost, AccessTools.Method(typeof(WorldFlowGhost), nameof(WorldFlowGhost.GetCopyCost), Type.EmptyTypes)),
                (GameHookPoints.PipeBound, AccessTools.Method(typeof(WorldFlowPipe), nameof(WorldFlowPipe.SetFlowPipe))),
                (GameHookPoints.PipeDragMove, AccessTools.Method(typeof(WorldFlowIO), nameof(WorldFlowIO.OnDragDropMove))),
                (GameHookPoints.PipeDragEnd, AccessTools.Method(typeof(WorldFlowIO), nameof(WorldFlowIO.OnDragDropEnd))),
                (GameHookPoints.PipeDragCancel, AccessTools.Method(typeof(WorldFlowIO), nameof(WorldFlowIO.OnDragDropCancel))),
                (GameHookPoints.PipeItemTransferred, AccessTools.Method(typeof(WorldFlowPipe), nameof(WorldFlowPipe.ItemTransferred))),
                (GameHookPoints.PipeItemRejected, AccessTools.Method(typeof(WorldFlowPipe), nameof(WorldFlowPipe.ItemRejected))),
                (GameHookPoints.WorkspaceShown, AccessTools.Method(typeof(WorldFlowManager), nameof(WorldFlowManager.ShowFlow))),
                (GameHookPoints.InputInitialized, AccessTools.Method(typeof(Assets.Behaviour.Input.PlayerInput), "Awake")),
                (GameHookPoints.BindingRowBound, AccessTools.Method(typeof(KeyBindingRow), nameof(KeyBindingRow.SetBinding))),
                (GameHookPoints.DeconstructionUpdate, AccessTools.Method(typeof(DeconstructOverlay), "Update")),
                (GameHookPoints.DeconstructionVisibility, AccessTools.Method(typeof(GameUI), nameof(GameUI.ToggleDeconstructMode), new[] { typeof(bool) })),
                (GameHookPoints.Cancel, AccessTools.Method(typeof(GameUI), nameof(GameUI.ProcessCancel))),
                (GameHookPoints.Escape, AccessTools.Method(typeof(GameUI), nameof(GameUI.ProcessEscape))),
                (GameHookPoints.GridSnap, AccessTools.Method(typeof(WorldFlowManager), nameof(WorldFlowManager.ClampToGrid))),
                (GameHookPoints.CameraUpdate, AccessTools.Method(typeof(WorldFlowCamera), "Update")),
                (GameHookPoints.CameraChangeZoom, AccessTools.Method(typeof(WorldFlowCamera), nameof(WorldFlowCamera.ChangeZoomLevel))),
                (GameHookPoints.CameraSetZoom, AccessTools.Method(typeof(WorldFlowCamera), nameof(WorldFlowCamera.SetZoomLevel))),
                (GameHookPoints.CameraZoomScale, AccessTools.PropertyGetter(typeof(WorldFlowCamera), nameof(WorldFlowCamera.ZoomScale))),
                (GameHookPoints.BuildingClick, AccessTools.Method(typeof(WorldFlowElement), nameof(WorldFlowElement.OnPointerClick))),
                (GameHookPoints.BuildingCanStack, AccessTools.Method(typeof(WorldFlowElement), "_canStackElement")),
                (GameHookPoints.BuildingBound, AccessTools.Method(typeof(WorldFlowElement), nameof(WorldFlowElement.SetElement))),
                (GameHookPoints.BuildingUpdatePorts, AccessTools.Method(typeof(WorldFlowElement), nameof(WorldFlowElement.UpdateIO))),
                (GameHookPoints.BuildingSize, AccessTools.PropertyGetter(typeof(WorldFlowElement), nameof(WorldFlowElement.Size))),
                (GameHookPoints.BuildingSerialize, AccessTools.Method(typeof(FlowElement), nameof(FlowElement.ToJson))),
                (GameHookPoints.BuildingDeserialize, AccessTools.Method(typeof(FlowElement), nameof(FlowElement.FromJson))),
                (GameHookPoints.BuildingCopy, AccessTools.Method(typeof(FlowElement), nameof(FlowElement.CreateCopy))),
                (GameHookPoints.PipeUpdateGeometry, AccessTools.Method(typeof(WorldFlowPipe), nameof(WorldFlowPipe.UpdateLinePositions))),
                (GameHookPoints.PortBound, AccessTools.Method(typeof(WorldFlowIO), nameof(WorldFlowIO.SetIO))),
                (GameHookPoints.GhostBound, AccessTools.Method(typeof(WorldFlowGhost), nameof(WorldFlowGhost.SetElement))),
                (GameHookPoints.DecorationSerialize, AccessTools.Method(typeof(FlowDeco), nameof(FlowDeco.ToJson))),
                (GameHookPoints.DecorationDeserialize, AccessTools.Method(typeof(FlowDeco), nameof(FlowDeco.FromJson))),
                (GameHookPoints.DecorationCopy, AccessTools.Method(typeof(FlowDeco), nameof(FlowDeco.CreateCopy))),
                (GameHookPoints.DecorationStyleUpdate, AccessTools.Method(typeof(WorldFlowDeco), nameof(WorldFlowDeco.UpdateStyle))),
                (GameHookPoints.DecorationPopupOpen, AccessTools.Method(typeof(DecoOptionsPopup), "OnEnable")),
                (GameHookPoints.DecorationStyleSelected, AccessTools.Method(typeof(DecoOptionsStyle), nameof(DecoOptionsStyle.StyleSelected))),
                (GameHookPoints.DecorationSelectionUpdate, AccessTools.Method(typeof(DecoOptionsStyle), "_updateSelection")),
                (GameHookPoints.ResearchCost, AccessTools.Method(typeof(TechNode), nameof(TechNode.GetCost))),
                (GameHookPoints.PlayerHasResearch, AccessTools.Method(typeof(GamePlayer), nameof(GamePlayer.HasTech), new[] { typeof(TechNode) })),
                (GameHookPoints.KingdomDeserialized, AccessTools.Method(typeof(GamePlayer), nameof(GamePlayer.FromJson))),
                (GameHookPoints.SpawnerConstructed, AccessTools.Constructor(typeof(Assets.Source.Flow.Elements.DevSpawner), Type.EmptyTypes)),
                (GameHookPoints.SpawnerIcon, AccessTools.PropertyGetter(typeof(Assets.Source.Flow.Elements.DevSpawner), nameof(Assets.Source.Flow.Elements.DevSpawner.IconName))),
                (GameHookPoints.MenuUpdate, AccessTools.Method(typeof(MainMenuUI), "Update")),
                (GameHookPoints.UiStarted, AccessTools.Method(typeof(GameUI), "Start")),
                (GameHookPoints.BindingRowClick, AccessTools.Method(typeof(KeyBindingRow), nameof(KeyBindingRow.OnPointerClick))),
                (GameHookPoints.BindingRowText, AccessTools.Method(typeof(KeyBindingRow), "_resetBindingText")),
                (GameHookPoints.BindingsApply, AccessTools.Method(typeof(KeyBindingsUI), nameof(KeyBindingsUI.ApplyBindings))),
                (GameHookPoints.BindingsCancel, AccessTools.Method(typeof(KeyBindingsUI), nameof(KeyBindingsUI.CancelBindings))),
                (GameHookPoints.BindingsReset, AccessTools.Method(typeof(KeyBindingsUI), nameof(KeyBindingsUI.ResetBindings))),
                (GameHookPoints.BindingsLoad, AccessTools.Method(typeof(KeyBindingsUI), nameof(KeyBindingsUI.Apply))),
                (GameHookPoints.BindingsOpened, AccessTools.Method(typeof(KeyBindingsUI), "OnEnable"))
            };
            foreach (var binding in bindings)
                Bind(harmony, binding.Id, binding.Method, strict);
        }

        internal void RegisterTestHook(string hookId, MethodBase method, bool failFast = false, Harmony harmony = null) => Bind(harmony, hookId, method, failFast);

        internal static bool IsMethodArgMutableForTest(MethodBase method, int index)
            => method != null && MutableArgsByMethod.TryGetValue(method, out var indices) && Array.IndexOf(indices, index) >= 0;

        internal IDisposable MarkUnavailableForTest(string hookId)
        {
            lock (gate)
            {
                bool existed = knownHooks.Contains(hookId);
                knownHooks.Add(hookId);
                unavailableHooks.Add(hookId);
                return new ActionToken(() =>
                {
                    lock (gate)
                    {
                        unavailableHooks.Remove(hookId);
                        if (!existed) knownHooks.Remove(hookId);
                    }
                });
            }
        }

        private void Bind(Harmony harmony, string id, MethodBase method, bool failFast)
        {
            lock (gate) knownHooks.Add(id);
            if (method == null)
            {
                if (EssentialHooks.Contains(id) || failFast)
                    throw new MissingMethodException("Foundation game hook is unavailable: " + id);
                lock (gate) unavailableHooks.Add(id);
                FoundationApi.Diagnostics.Write("Foundation", FoundationDiagnosticLevel.Warning,
                    "Optional game hook unavailable in this game build: " + id + "; dependent features will be disabled.");
                return;
            }

            Targets[method] = id;
            var returnType = (method as MethodInfo)?.ReturnType ?? typeof(void);
            string prefixName = returnType == typeof(void)
                ? (method.IsStatic ? nameof(BeforeStatic) : nameof(Before))
                : (method.IsStatic ? nameof(BeforeStaticResult) : nameof(BeforeResult));
            string postfixName = returnType == typeof(void) ? nameof(After) : nameof(AfterResult);
            var parameters = method.GetParameters();
            var mutableIndices = Array.Empty<int>();
            if (returnType == typeof(void) && !method.IsStatic && parameters.Length == 1)
            {
                if (parameters[0].ParameterType == typeof(int)) { prefixName = nameof(BeforeInt); mutableIndices = new[] { 0 }; }
                else if (parameters[0].ParameterType == typeof(bool)) { prefixName = nameof(BeforeBool); mutableIndices = new[] { 0 }; }
            }
            if (id == GameHookPoints.GridSnap) { prefixName = nameof(BeforeGrid); mutableIndices = new[] { 0, 1 }; }
            MutableArgsByMethod[method] = mutableIndices;

            if (harmony == null)
                return;

            var prefix = AccessTools.Method(typeof(GameHookRegistry), prefixName);
            var postfix = AccessTools.Method(typeof(GameHookRegistry), postfixName);
            if (returnType != typeof(void))
            {
                if (prefix.IsGenericMethodDefinition) prefix = prefix.MakeGenericMethod(returnType);
                postfix = postfix.MakeGenericMethod(returnType);
            }
            harmony.Patch(method, prefix: new HarmonyMethod(prefix) { priority = Priority.High },
                postfix: new HarmonyMethod(postfix) { priority = Priority.Last });
        }

        internal void Shutdown()
        {
            lock (gate)
            {
                slots.Clear();
                knownHooks.Clear();
                unavailableHooks.Clear();
                faultStates.Clear();
                Targets.Clear();
                MutableArgsByMethod.Clear();
            }
        }

        public IDisposable Subscribe(string modId, string hookId, Action<GameHookCall> before = null, Action<GameHookCall> after = null, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("A mod identifier is required.", nameof(modId));
            if (before == null && after == null) throw new ArgumentException("At least one handler is required.");
            lock (gate)
            {
                if (!knownHooks.Contains(hookId)) throw new ArgumentException("Unknown Foundation hook: " + hookId, nameof(hookId));
                if (unavailableHooks.Contains(hookId)) throw new MissingMethodException("Foundation game hook is unavailable in this game build: " + hookId);
                var entry = new GameHookSubscription(this) { ModId = modId, HookId = hookId, Before = before, After = after, Priority = priority, Sequence = ++nextSequence };
                if (!slots.TryGetValue(hookId, out var slot))
                {
                    slot = new HookSlot();
                    slots[hookId] = slot;
                }
                slot.All.Add(entry);
                slot.Rebuild();
                return entry;
            }
        }

        internal void Remove(GameHookSubscription entry)
        {
            lock (gate)
            {
                if (slots.TryGetValue(entry.HookId, out var slot) && slot.All.Remove(entry))
                    slot.Rebuild();
            }
        }

        public IDisposable SubscribeFiltered(string modId, string hookId, GameHookFilter filter, Action<GameHookCall> before = null, Action<GameHookCall> after = null, int priority = 0)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (hookId != GameHookPoints.PlayerHasResearch && hookId != GameHookPoints.ResearchCost)
                throw new ArgumentException("This hook does not yet expose stable research identifier filters.", nameof(hookId));
            lock (gate)
            {
                if (!knownHooks.Contains(hookId)) throw new ArgumentException("Unknown Foundation hook: " + hookId, nameof(hookId));
                if (unavailableHooks.Contains(hookId)) throw new MissingMethodException("Foundation game hook is unavailable in this game build: " + hookId);
                var entry = new GameHookSubscription(this) { ModId = modId, HookId = hookId, Before = before, After = after, Priority = priority, Filter = filter, Sequence = ++nextSequence };
                if (!slots.TryGetValue(hookId, out var slot))
                {
                    slot = new HookSlot();
                    slots[hookId] = slot;
                }
                slot.All.Add(entry);
                slot.Rebuild();
                return entry;
            }
        }

        private static bool Matches(GameHookSubscription entry, object subject, object[] arguments)
        {
            if (!entry.Active) return false;
            if (entry.Filter == null) return true;
            if (entry.Filter.SubjectId != null && (!(subject is TechNode research) || research.Identifier != entry.Filter.SubjectId)) return false;
            if (entry.Filter.ArgumentId != null && (arguments.Length == 0 || !(arguments[0] is TechNode argument) || argument.Identifier != entry.Filter.ArgumentId)) return false;
            return true;
        }

        internal void DispatchDirect(string id, GameHookCall call, bool after) => Dispatch(id, call, after);

        private void Dispatch(string id, GameHookCall call, bool after)
        {
            GameHookSubscription[] snapshot;
            lock (gate)
            {
                if (!slots.TryGetValue(id, out var slot))
                    return;
                snapshot = after ? slot.AfterSorted : slot.BeforeSorted;
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                var entry = snapshot[i];
                if (!Matches(entry, call.NativeSubject, call.NativeArguments))
                    continue;
                try
                {
                    (after ? entry.After : entry.Before).Invoke(call);
                    RecordSubscriberSuccess(entry.ModId, id);
                }
                catch (Exception error)
                {
                    RecordSubscriberFailure(entry.ModId, id, error);
                }
            }
        }

        void RecordSubscriberSuccess(string modId, string hookId)
        {
            HookFaultState recovered = null;
            long priorConsecutive = 0;
            lock (gate)
            {
                if (faultStates.TryGetValue((modId, hookId), out var state) && state.ConsecutiveFaults > 0)
                {
                    priorConsecutive = state.ConsecutiveFaults;
                    state.ConsecutiveFaults = 0;
                    state.SuppressedSinceLastLog = 0;
                    recovered = state;
                }
            }
            if (recovered != null)
            {
                FoundationApi.Diagnostics.Write(modId, FoundationDiagnosticLevel.Info,
                    $"Game hook subscriber recovered on {hookId} after {priorConsecutive} consecutive failure(s).");
            }
        }

        void RecordSubscriberFailure(string modId, string hookId, Exception error)
        {
            bool emitFullReport = false;
            bool emitRateLimitedWarning = false;
            long total = 0;
            long suppressed = 0;
            var now = utcNow();

            lock (gate)
            {
                if (!faultStates.TryGetValue((modId, hookId), out var state))
                {
                    state = new HookFaultState();
                    faultStates[(modId, hookId)] = state;
                }
                state.TotalFaults++;
                state.ConsecutiveFaults++;
                total = state.TotalFaults;

                if (state.TotalFaults == 1)
                {
                    emitFullReport = true;
                    state.LastLogUtc = now;
                    state.SuppressedSinceLastLog = 0;
                }
                else if (now - state.LastLogUtc >= FaultLogInterval)
                {
                    emitRateLimitedWarning = true;
                    suppressed = state.SuppressedSinceLastLog;
                    state.LastLogUtc = now;
                    state.SuppressedSinceLastLog = 0;
                }
                else
                {
                    state.SuppressedSinceLastLog++;
                }
            }

            if (emitFullReport)
                FoundationApi.Report(new InvalidOperationException("Game hook subscriber failed: " + modId + " / " + hookId, error));
            else if (emitRateLimitedWarning)
                FoundationApi.Diagnostics.Write(modId, FoundationDiagnosticLevel.Error,
                    $"Game hook subscriber still failing on {hookId} (total faults={total}, suppressed={suppressed}): {error.GetType().Name}: {error.Message}");
        }

        private static bool Before(object __instance, object[] __args, MethodBase __originalMethod, out GameHookCall __state)
        {
            var registry = FoundationApi.GameHooksInternal;
            var id = Targets[__originalMethod];
            bool subscribed = false;
            HookSlot slot = null;
            lock (registry.gate)
                registry.slots.TryGetValue(id, out slot);

            if (slot != null)
            {
                var before = slot.BeforeSorted;
                for (int i = 0; i < before.Length; i++)
                    if (Matches(before[i], __instance, __args)) { subscribed = true; break; }
                if (!subscribed)
                {
                    var after = slot.AfterSorted;
                    for (int i = 0; i < after.Length; i++)
                        if (Matches(after[i], __instance, __args)) { subscribed = true; break; }
                }
            }

            if (!subscribed) { __state = null; return true; }
            MutableArgsByMethod.TryGetValue(__originalMethod, out var mutable);
            __state = new GameHookCall(__instance, __args, (__originalMethod as MethodInfo)?.ReturnType ?? typeof(void), mutable);
            registry.Dispatch(id, __state, false);
            return !__state.IsCancelled;
        }

        private static void After(MethodBase __originalMethod, bool __runOriginal, GameHookCall __state)
        {
            if (__state == null) return;
            __state.OriginalRan = __runOriginal;
            FoundationApi.GameHooksInternal.Dispatch(Targets[__originalMethod], __state, true);
        }

        private static bool BeforeStatic(object[] __args, MethodBase __originalMethod, out GameHookCall __state)
            => Before(null, __args, __originalMethod, out __state);

        // HarmonyX supplies __args as an observation array in this game build.
        // Explicit ref parameters are required to propagate framework argument edits.
        private static bool BeforeInt(object __instance, ref int __0, MethodBase __originalMethod, out GameHookCall __state)
        {
            bool run = Before(__instance, new object[] { __0 }, __originalMethod, out __state);
            if (__state != null) __0 = __state.Argument<int>(0);
            return run;
        }

        private static bool BeforeBool(object __instance, ref bool __0, MethodBase __originalMethod, out GameHookCall __state)
        {
            bool run = Before(__instance, new object[] { __0 }, __originalMethod, out __state);
            if (__state != null) __0 = __state.Argument<bool>(0);
            return run;
        }

        private static bool BeforeGrid(ref Vector2Int __0, ref int __1, MethodBase __originalMethod, ref Vector2Int __result, out GameHookCall __state)
        {
            bool run = Before(null, new object[] { __0, __1 }, __originalMethod, out __state);
            if (__state != null)
            {
                __0 = __state.Argument<Vector2Int>(0);
                __1 = __state.Argument<int>(1);
                if (__state.HasResult) __result = (Vector2Int)__state.ResultValue;
            }
            return run;
        }

        private static bool BeforeResult<T>(object __instance, object[] __args, MethodBase __originalMethod, ref T __result, out GameHookCall __state)
        {
            bool run = Before(__instance, __args, __originalMethod, out __state);
            if (__state != null && __state.HasResult) __result = (T)__state.ResultValue;
            return run;
        }

        private static bool BeforeStaticResult<T>(object[] __args, MethodBase __originalMethod, ref T __result, out GameHookCall __state)
            => BeforeResult(null, __args, __originalMethod, ref __result, out __state);

        private static void AfterResult<T>(MethodBase __originalMethod, bool __runOriginal, ref T __result, GameHookCall __state)
        {
            if (__state == null) return;
            __state.CaptureResult(__result);
            After(__originalMethod, __runOriginal, __state);
            if (__state.HasResult) __result = (T)__state.ResultValue;
        }
    }
}
