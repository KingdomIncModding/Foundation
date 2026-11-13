using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Source.Flow;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using LightJson;
using LightJson.Serialization;

namespace KingdomFoundation.Api
{
    internal sealed class GameExecutionHooks : IGameExecutionHooks
    {
        private static readonly object Gate = new object();
        private static readonly Harmony Harmony = new Harmony("urakka.kingdominc.foundation.execution-hooks");
        private static readonly ConcurrentDictionary<MethodBase, string> Targets = new ConcurrentDictionary<MethodBase, string>();
        private static readonly ConcurrentDictionary<string, GameExecutionSubscription[]> Subscriptions = new ConcurrentDictionary<string, GameExecutionSubscription[]>();
        public IDisposable Subscribe(string modId, string hookId, Func<GameExecutionCall, object> begin, Action<GameExecutionCall, object> end)
            => SubscribeCore(modId, hookId, () => Resolve(hookId).ToArray(), begin, end);
        public IDisposable SubscribeExternal(string modId, MethodBase method, Func<GameExecutionCall, object> begin, Action<GameExecutionCall, object> end)
        {
            if(method==null)throw new ArgumentNullException(nameof(method));
            if(method.DeclaringType?.Assembly==typeof(GamePlayer).Assembly)throw new ArgumentException("Native targets require a stable Foundation hook identifier.",nameof(method));
            return SubscribeCore(modId,"external:"+method.Module.ModuleVersionId+":"+method.MetadataToken,()=>new[]{method},begin,end);
        }
        private IDisposable SubscribeCore(string modId,string hookId,Func<MethodBase[]> resolve,Func<GameExecutionCall,object> begin,Action<GameExecutionCall,object> end)
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("A mod identifier is required.", nameof(modId));
            if (begin == null || end == null) throw new ArgumentNullException(begin == null ? nameof(begin) : nameof(end));
            lock (Gate)
            {
                if (!Subscriptions.ContainsKey(hookId))
                {
                    var methods = resolve();
                    if (methods.Length == 0 || methods.Any(method => method == null || method.IsAbstract))
                        throw new MissingMethodException("Foundation execution hook is unavailable: " + hookId);
                    Subscriptions[hookId] = Array.Empty<GameExecutionSubscription>();
                    try
                    {
                        foreach (var method in methods)
                        {
                            Targets[method] = hookId;
                            Harmony.Patch(method,
                                prefix: new HarmonyMethod(typeof(GameExecutionHooks), method.IsStatic ? nameof(BeginStatic) : nameof(Begin)) { priority = Priority.First },
                                postfix: new HarmonyMethod(typeof(GameExecutionHooks), nameof(Returned)) { priority = Priority.Last },
                                finalizer: new HarmonyMethod(typeof(GameExecutionHooks), nameof(Ended)) { priority = Priority.Last });
                        }
                    }
                    catch
                    {
                        foreach (var method in methods) { Harmony.Unpatch(method, HarmonyPatchType.All, Harmony.Id); Targets.TryRemove(method, out _); }
                        Subscriptions.TryRemove(hookId, out _); throw;
                    }
                }
                var entry = new GameExecutionSubscription(modId, begin, end, item => Remove(hookId, item));
                Subscriptions[hookId] = Subscriptions[hookId].Concat(new[] { entry }).ToArray();
                return entry;
            }
        }
        private static void Remove(string id, GameExecutionSubscription entry)
        { lock (Gate) { Subscriptions[id] = Subscriptions[id].Where(item => item != entry).ToArray(); } }
        private static IEnumerable<MethodBase> Resolve(string id)
        {
            switch (id)
            {
                case GameExecutionPoints.Autosave: return One(typeof(SaveGame), "StoreAutosaveState", typeof(string));
                case GameExecutionPoints.Save: return One(typeof(SaveGame), "DoSave", typeof(string));
                case GameExecutionPoints.Store: return One(typeof(SaveGame), "Store", typeof(JsonObject), typeof(string), typeof(SaveGameFormat));
                case GameExecutionPoints.Snapshot: return One(typeof(SaveGame), "SaveCurrentState");
                case GameExecutionPoints.KingdomSerialize: return One(typeof(GamePlayer), "ToJson");
                case GameExecutionPoints.SeedObject: return One(typeof(SeedGenerator), "Add", typeof(JsonObject));
                case GameExecutionPoints.JsonRender: return One(typeof(JsonWriter), "Serialize", typeof(JsonValue));
                case GameExecutionPoints.WorkspaceSerialize: return One(typeof(FlowDiagram), "ToJson");
                case GameExecutionPoints.BuildingSerialize: return One(typeof(FlowElement), "ToJson");
                case GameExecutionPoints.PipeSerialize: return One(typeof(FlowPipe), "ToJson");
                case GameExecutionPoints.PortSerialize: return One(typeof(FlowIO), "ToJson");
                case GameExecutionPoints.DecorationSerialize:
                    return typeof(FlowDeco).Assembly.GetTypes().Where(type => typeof(FlowDeco).IsAssignableFrom(type))
                        .Select(type => type.GetMethod("ToJson", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null))
                        .Where(method => method != null && !method.IsAbstract);
                case GameExecutionPoints.Recall: return One(typeof(SaveGameFile), "Recall");
                case GameExecutionPoints.Load: return One(typeof(SaveGame), "LoadState", typeof(JsonObject));
                case GameExecutionPoints.WorkspaceEdit: return EditMethods();
                case GameExecutionPoints.DeconstructionUpdate: return One(typeof(Assets.Behaviour.UI.HUD.DeconstructOverlay),"Update");
                case GameExecutionPoints.WarUpdate:return One(typeof(Assets.Source.Adventure.WarProgress),"UpdateWar");
                case GameExecutionPoints.BuildingActivity:
                    var names=new HashSet<string>{"Update","DoManualCraft","DoCollect","QueueCraft","SoldierDied","UpdateQuestProgress"};
                    return typeof(FlowElement).Assembly.GetTypes().Where(type=>typeof(FlowElement).IsAssignableFrom(type)).SelectMany(type=>type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly))
                        .Where(method=>names.Contains(method.Name) && !method.IsAbstract && !method.ContainsGenericParameters);
                default: throw new ArgumentException("Unknown Foundation execution hook: " + id, nameof(id));
            }
        }
        private static MethodBase[] One(Type type, string name, params Type[] args) => new MethodBase[] { AccessTools.Method(type, name, args) };
        private static IEnumerable<MethodBase> EditMethods()
        {
            var methods=new HashSet<MethodBase>();
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowElement),"OnPointerClick","AttemptStackElement");
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowGhost),"OnDragDropEnd");
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowIO),"OnPointerClick","OnBeginDrag","_startDragDrop","OnDragDropEnd","OnDragDropCancel");
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowLabel),"OnPointerClick","OnBeginDrag","OnDragDropEnd","OnDragDropCancel");
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowDecoMover),"StartDragDrop","OnDragDropEnd","OnDragDropCancel");
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowDecoResizer),"StartDragDrop","OnDragDropEnd","OnDragDropCancel");
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowPipeNode),"StartDragDrop","OnDragDropEnd","OnDragDropCancel");
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowPipe),"AddNewNode","RemoveNode");
            AddEdit(methods,typeof(Assets.Behaviour.Flow.WorldFlowUpgrade),"SelectNewUpgrade");
            AddEdit(methods,typeof(Assets.Behaviour.UI.DragDropManager),"SetDragDropTarget","_dropTarget","CancelDragDrop");
            AddEdit(methods,typeof(Assets.Behaviour.UI.GameUI),"ToggleDeconstructMode");
            AddEdit(methods,typeof(Assets.Behaviour.UI.Deco.DecoOptionsPopup),"UpdateLabel","UpdateLock","UpdateAnchor");
            AddEdit(methods,typeof(Assets.Behaviour.UI.Deco.DecoOptionsStyle),"StyleSelected");
            return methods.OrderBy(method=>method.DeclaringType.FullName).ThenBy(method=>method.Name);
        }
        private static void AddEdit(HashSet<MethodBase> result,Type type,params string[] names)
        {
            foreach(var name in names)
            {
                var matches=type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(method=>method.Name==name).ToArray();
                if(matches.Length==0)throw new MissingMethodException(type.FullName,name);
                foreach(var method in matches)result.Add(method);
            }
        }
        private static void Begin(object __instance, object[] __args, MethodBase __originalMethod, out GameExecutionCall __state)
        {
            var id = Targets[__originalMethod];
            var entries = Subscriptions[id];
            if (entries.Length == 0) { __state = null; return; }
            __state = new GameExecutionCall(id,Operation(__originalMethod), __instance, __args, entries);
            for (int i = 0; i < entries.Length; i++)
            {
                __state.States[i] = GameExecutionCall.BeginFailed;
                if (!entries[i].Active) continue;
                try { __state.States[i] = entries[i].Begin(__state); }
                catch (Exception error) { FoundationApi.Report(error); }
            }
        }
        private static void BeginStatic(object[] __args, MethodBase __originalMethod, out GameExecutionCall __state) => Begin(null, __args, __originalMethod, out __state);
        private static string Operation(MethodBase method)
        {
            switch(method.Name)
            {
                case "Update":return "Update";
                case "DoManualCraft":return "DoManualCraft";
                case "DoCollect":return "DoCollect";
                case "QueueCraft":return "QueueCraft";
                case "SoldierDied":return "SoldierDied";
                case "UpdateQuestProgress":return "UpdateQuestProgress";
                default:return "";
            }
        }
        private static void Returned(bool __runOriginal, GameExecutionCall __state) { if (__state != null) __state.OriginalRan = __runOriginal; }
        private static Exception Ended(Exception __exception, bool __runOriginal, GameExecutionCall __state)
        {
            if (__state == null) return __exception;
            __state.Exception = __exception; __state.OriginalRan = __runOriginal;
            for (int i = __state.Observers.Length - 1; i >= 0; i--)
            {
                if (ReferenceEquals(__state.States[i], GameExecutionCall.BeginFailed)) continue;
                // Finish a successfully started scope even if its subscription was disposed during this call.
                try { __state.Observers[i].End(__state, __state.States[i]); }
                catch (Exception error) { FoundationApi.Report(error); }
            }
            return __exception;
        }
    }
}
