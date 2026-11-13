using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Source.Flow.Elements;
using HarmonyLib;

namespace KingdomFoundation.Api
{
    [HarmonyPatch]
    internal static class ManualCraftContextPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var type in new[] { typeof(RecipeElement), typeof(ResourceElement), typeof(Forest), typeof(Inn), typeof(TrainingGrounds), typeof(Disenchanter) })
                yield return AccessTools.DeclaredMethod(type, "DoManualCraft");
        }

        static void Prefix(out IDisposable __state) => __state = FoundationApi.AudioInterceptionInternal.Enter(new GameAudioActivity(GameAudioActivityKind.ManualCraft));
        static Exception Finalizer(Exception __exception, IDisposable __state) { GameAudioInterceptionService.Exit(__state); return __exception; }
    }
}
