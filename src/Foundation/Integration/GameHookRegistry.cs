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
        private readonly List<GameHookSubscription> subscriptions = new List<GameHookSubscription>();
        private static readonly Dictionary<MethodBase, string> Targets = new Dictionary<MethodBase, string>();
        internal void Install(Harmony harmony)
        {
            Bind(harmony,GameHookPoints.PortAcceptance,AccessTools.Method(typeof(FlowIO),nameof(FlowIO.CanAcceptItem)));
            Bind(harmony,GameHookPoints.PortDelivery,AccessTools.Method(typeof(FlowInput),nameof(FlowInput.InputItem)));
            Bind(harmony,GameHookPoints.BuildingClearPorts,AccessTools.Method(typeof(FlowElement),nameof(FlowElement.ClearIO)));
            Bind(harmony,GameHookPoints.CurrencyAdded,AccessTools.Method(typeof(GamePlayer),nameof(GamePlayer.AddCurrency),new[]{typeof(Currency),typeof(long),typeof(bool)}));
            Bind(harmony,GameHookPoints.CurrencySpent,AccessTools.Method(typeof(GamePlayer),nameof(GamePlayer.SpendCurrency),new[]{typeof(Currency),typeof(long)}));
            Bind(harmony,GameHookPoints.PortConsumed,AccessTools.Method(typeof(FlowIO),nameof(FlowIO.ConsumeItem),Type.EmptyTypes));
            Bind(harmony,GameHookPoints.UiVisibility,AccessTools.Method(typeof(GameUI),nameof(GameUI.ToggleUI)));
            Bind(harmony,GameHookPoints.GhostCommit,AccessTools.Method(typeof(WorldFlowGhost),nameof(WorldFlowGhost.OnDragDropEnd)));
            Bind(harmony,GameHookPoints.GhostCost,AccessTools.Method(typeof(WorldFlowGhost),nameof(WorldFlowGhost.GetCopyCost),Type.EmptyTypes));
            Bind(harmony,GameHookPoints.PipeBound,AccessTools.Method(typeof(WorldFlowPipe),nameof(WorldFlowPipe.SetFlowPipe)));
            Bind(harmony,GameHookPoints.PipeItemTransferred,AccessTools.Method(typeof(WorldFlowPipe),nameof(WorldFlowPipe.ItemTransferred)));
            Bind(harmony,GameHookPoints.PipeItemRejected,AccessTools.Method(typeof(WorldFlowPipe),nameof(WorldFlowPipe.ItemRejected)));
            Bind(harmony,GameHookPoints.WorkspaceShown,AccessTools.Method(typeof(WorldFlowManager),nameof(WorldFlowManager.ShowFlow)));
            Bind(harmony,GameHookPoints.InputInitialized,AccessTools.Method(typeof(Assets.Behaviour.Input.PlayerInput),"Awake"));
            Bind(harmony,GameHookPoints.BindingRowBound,AccessTools.Method(typeof(KeyBindingRow),nameof(KeyBindingRow.SetBinding)));
            Bind(harmony, GameHookPoints.DeconstructionUpdate, AccessTools.Method(typeof(DeconstructOverlay), "Update"));
            Bind(harmony, GameHookPoints.DeconstructionVisibility, AccessTools.Method(typeof(GameUI), nameof(GameUI.ToggleDeconstructMode), new[] { typeof(bool) }));
            Bind(harmony, GameHookPoints.Cancel, AccessTools.Method(typeof(GameUI), nameof(GameUI.ProcessCancel)));
            Bind(harmony, GameHookPoints.Escape, AccessTools.Method(typeof(GameUI), nameof(GameUI.ProcessEscape)));
            Bind(harmony, GameHookPoints.GridSnap, AccessTools.Method(typeof(WorldFlowManager), nameof(WorldFlowManager.ClampToGrid)));
            Bind(harmony, GameHookPoints.CameraUpdate, AccessTools.Method(typeof(WorldFlowCamera), "Update"));
            Bind(harmony, GameHookPoints.CameraChangeZoom, AccessTools.Method(typeof(WorldFlowCamera), nameof(WorldFlowCamera.ChangeZoomLevel)));
            Bind(harmony, GameHookPoints.CameraSetZoom, AccessTools.Method(typeof(WorldFlowCamera), nameof(WorldFlowCamera.SetZoomLevel)));
            Bind(harmony, GameHookPoints.CameraZoomScale, AccessTools.PropertyGetter(typeof(WorldFlowCamera), nameof(WorldFlowCamera.ZoomScale)));
            Bind(harmony, GameHookPoints.BuildingClick, AccessTools.Method(typeof(WorldFlowElement), nameof(WorldFlowElement.OnPointerClick)));
            Bind(harmony, GameHookPoints.BuildingCanStack, AccessTools.Method(typeof(WorldFlowElement), "_canStackElement"));
            Bind(harmony, GameHookPoints.BuildingBound, AccessTools.Method(typeof(WorldFlowElement), nameof(WorldFlowElement.SetElement)));
            Bind(harmony, GameHookPoints.BuildingUpdatePorts, AccessTools.Method(typeof(WorldFlowElement), nameof(WorldFlowElement.UpdateIO)));
            Bind(harmony, GameHookPoints.BuildingSize, AccessTools.PropertyGetter(typeof(WorldFlowElement), nameof(WorldFlowElement.Size)));
            Bind(harmony, GameHookPoints.BuildingSerialize, AccessTools.Method(typeof(FlowElement), nameof(FlowElement.ToJson)));
            Bind(harmony, GameHookPoints.BuildingDeserialize, AccessTools.Method(typeof(FlowElement), nameof(FlowElement.FromJson)));
            Bind(harmony, GameHookPoints.BuildingCopy, AccessTools.Method(typeof(FlowElement), nameof(FlowElement.CreateCopy)));
            Bind(harmony, GameHookPoints.PipeUpdateGeometry, AccessTools.Method(typeof(WorldFlowPipe), nameof(WorldFlowPipe.UpdateLinePositions)));
            Bind(harmony, GameHookPoints.PortBound, AccessTools.Method(typeof(WorldFlowIO), nameof(WorldFlowIO.SetIO)));
            Bind(harmony, GameHookPoints.GhostBound, AccessTools.Method(typeof(WorldFlowGhost), nameof(WorldFlowGhost.SetElement)));
            Bind(harmony, GameHookPoints.DecorationSerialize, AccessTools.Method(typeof(FlowDeco), nameof(FlowDeco.ToJson)));
            Bind(harmony, GameHookPoints.DecorationDeserialize, AccessTools.Method(typeof(FlowDeco), nameof(FlowDeco.FromJson)));
            Bind(harmony, GameHookPoints.DecorationCopy, AccessTools.Method(typeof(FlowDeco), nameof(FlowDeco.CreateCopy)));
            Bind(harmony, GameHookPoints.DecorationStyleUpdate, AccessTools.Method(typeof(WorldFlowDeco), nameof(WorldFlowDeco.UpdateStyle)));
            Bind(harmony, GameHookPoints.DecorationPopupOpen, AccessTools.Method(typeof(DecoOptionsPopup), "OnEnable"));
            Bind(harmony, GameHookPoints.DecorationStyleSelected, AccessTools.Method(typeof(DecoOptionsStyle), nameof(DecoOptionsStyle.StyleSelected)));
            Bind(harmony, GameHookPoints.DecorationSelectionUpdate, AccessTools.Method(typeof(DecoOptionsStyle), "_updateSelection"));
            Bind(harmony, GameHookPoints.ResearchCost, AccessTools.Method(typeof(TechNode), nameof(TechNode.GetCost)));
            Bind(harmony, GameHookPoints.PlayerHasResearch, AccessTools.Method(typeof(GamePlayer), nameof(GamePlayer.HasTech),new[]{typeof(TechNode)}));
            Bind(harmony, GameHookPoints.KingdomDeserialized, AccessTools.Method(typeof(GamePlayer), nameof(GamePlayer.FromJson)));
            Bind(harmony, GameHookPoints.SpawnerConstructed, AccessTools.Constructor(typeof(Assets.Source.Flow.Elements.DevSpawner),Type.EmptyTypes));
            Bind(harmony, GameHookPoints.SpawnerIcon, AccessTools.PropertyGetter(typeof(Assets.Source.Flow.Elements.DevSpawner),nameof(Assets.Source.Flow.Elements.DevSpawner.IconName)));
            Bind(harmony,GameHookPoints.MenuUpdate,AccessTools.Method(typeof(MainMenuUI),"Update"));
            Bind(harmony,GameHookPoints.UiStarted,AccessTools.Method(typeof(GameUI),"Start"));
            Bind(harmony,GameHookPoints.BindingRowClick,AccessTools.Method(typeof(KeyBindingRow),nameof(KeyBindingRow.OnPointerClick)));
            Bind(harmony,GameHookPoints.BindingRowText,AccessTools.Method(typeof(KeyBindingRow),"_resetBindingText"));
            Bind(harmony,GameHookPoints.BindingsApply,AccessTools.Method(typeof(KeyBindingsUI),nameof(KeyBindingsUI.ApplyBindings)));
            Bind(harmony,GameHookPoints.BindingsCancel,AccessTools.Method(typeof(KeyBindingsUI),nameof(KeyBindingsUI.CancelBindings)));
            Bind(harmony,GameHookPoints.BindingsReset,AccessTools.Method(typeof(KeyBindingsUI),nameof(KeyBindingsUI.ResetBindings)));
            Bind(harmony,GameHookPoints.BindingsLoad,AccessTools.Method(typeof(KeyBindingsUI),nameof(KeyBindingsUI.Apply)));
            Bind(harmony,GameHookPoints.BindingsOpened,AccessTools.Method(typeof(KeyBindingsUI),"OnEnable"));
        }
        private static void Bind(Harmony harmony, string id, MethodBase method)
        {
            if (method == null) throw new MissingMethodException("Foundation game hook is unavailable: " + id);
            Targets[method] = id;
            var returnType=(method as MethodInfo)?.ReturnType ?? typeof(void);
            string prefixName = returnType == typeof(void) ? (method.IsStatic ? nameof(BeforeStatic) : nameof(Before)) : (method.IsStatic ? nameof(BeforeStaticResult) : nameof(BeforeResult));
            string postfixName = returnType == typeof(void) ? nameof(After) : nameof(AfterResult);
            var parameters = method.GetParameters();
            if (returnType == typeof(void) && !method.IsStatic && parameters.Length == 1)
            {
                if (parameters[0].ParameterType == typeof(int)) prefixName = nameof(BeforeInt);
                else if (parameters[0].ParameterType == typeof(bool)) prefixName = nameof(BeforeBool);
            }
            if (id == GameHookPoints.GridSnap) prefixName = nameof(BeforeGrid);
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
        public IDisposable Subscribe(string modId, string hookId, Action<GameHookCall> before = null, Action<GameHookCall> after = null, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("A mod identifier is required.", nameof(modId));
            if (!Targets.Values.Contains(hookId)) throw new ArgumentException("Unknown Foundation hook: " + hookId, nameof(hookId));
            if (before == null && after == null) throw new ArgumentException("At least one handler is required.");
            var entry = new GameHookSubscription(this) { ModId = modId, HookId = hookId, Before = before, After = after, Priority = priority };
            subscriptions.Add(entry);
            return entry;
        }
        internal void Remove(GameHookSubscription entry) { subscriptions.Remove(entry); }
        public IDisposable SubscribeFiltered(string modId,string hookId,GameHookFilter filter,Action<GameHookCall> before=null,Action<GameHookCall> after=null,int priority=0)
        {
            if(filter==null)throw new ArgumentNullException(nameof(filter));
            if(hookId!=GameHookPoints.PlayerHasResearch && hookId!=GameHookPoints.ResearchCost)
                throw new ArgumentException("This hook does not yet expose stable research identifier filters.",nameof(hookId));
            var entry=(GameHookSubscription)Subscribe(modId,hookId,before,after,priority);entry.Filter=filter;return entry;
        }
        private static bool Matches(GameHookSubscription entry,string id,object subject,object[] arguments)
        {
            if(!entry.Active || entry.HookId!=id)return false;
            if(entry.Filter==null)return true;
            if(entry.Filter.SubjectId!=null && (!(subject is TechNode research) || research.Identifier!=entry.Filter.SubjectId))return false;
            if(entry.Filter.ArgumentId!=null && (arguments.Length==0 || !(arguments[0] is TechNode argument) || argument.Identifier!=entry.Filter.ArgumentId))return false;
            return true;
        }
        private void Dispatch(string id, GameHookCall call, bool after)
        {
            foreach (var entry in subscriptions.Where(s => Matches(s,id,call.NativeSubject,call.NativeArguments)).OrderByDescending(s => s.Priority).ToArray())
            {
                if (!entry.Active) continue;
                try { (after ? entry.After : entry.Before)?.Invoke(call); }
                catch (Exception error) { FoundationApi.Report(new InvalidOperationException("Game hook subscriber failed: " + entry.ModId + " / " + id, error)); }
            }
        }
        private static bool Before(object __instance, object[] __args, MethodBase __originalMethod, out GameHookCall __state)
        {
            bool subscribed=false;
            foreach(var entry in FoundationApi.GameHooksInternal.subscriptions)
                if(Matches(entry,Targets[__originalMethod],__instance,__args)){subscribed=true;break;}
            if(!subscribed){__state=null;return true;}
            __state = new GameHookCall(__instance, __args,(__originalMethod as MethodInfo)?.ReturnType ?? typeof(void));
            FoundationApi.GameHooksInternal.Dispatch(Targets[__originalMethod], __state, false);
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
            if (__state!=null) __0 = __state.Argument<int>(0);
            return run;
        }
        private static bool BeforeBool(object __instance, ref bool __0, MethodBase __originalMethod, out GameHookCall __state)
        {
            bool run = Before(__instance, new object[] { __0 }, __originalMethod, out __state);
            if (__state!=null) __0 = __state.Argument<bool>(0);
            return run;
        }
        private static bool BeforeGrid(ref Vector2Int __0, ref int __1, MethodBase __originalMethod, ref Vector2Int __result, out GameHookCall __state)
        {
            bool run = Before(null, new object[] { __0, __1 }, __originalMethod, out __state);
            if (__state!=null)
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
            if (__state!=null && __state.HasResult) __result = (T)__state.ResultValue;
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
