using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Source.Flow.Elements;
using HarmonyLib;

namespace KingdomFoundation.Api
{
    [HarmonyPatch]
    internal static class CombatContextPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Arena), "DoManualCraft");
            yield return AccessTools.DeclaredMethod(typeof(Arena), "Update", new[] { typeof(float) });
            yield return AccessTools.Method(typeof(Dungeon), "CheckForParty");
        }

        static void Prefix(out IDisposable __state) => __state = FoundationApi.AudioInterceptionInternal.Enter(new GameAudioActivity(GameAudioActivityKind.CombatAction));
        static Exception Finalizer(Exception __exception, IDisposable __state) { GameAudioInterceptionService.Exit(__state); return __exception; }
    }
}
