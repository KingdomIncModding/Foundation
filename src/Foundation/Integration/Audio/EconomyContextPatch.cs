using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.CurrencyExchange;
using Assets.Source.Flow.Elements;
using HarmonyLib;

namespace KingdomFoundation.Api
{
    [HarmonyPatch]
    internal static class EconomyContextPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(UISounds), "Coins");
            yield return AccessTools.Method(typeof(CurrencyExchangePopup), "ButtonExchange");
            yield return AccessTools.Method(typeof(Shop), "DoCollect");
            foreach (var type in new[] { typeof(Shop), typeof(Restaurant), typeof(RequisitionOffice) })
                yield return AccessTools.DeclaredMethod(type, "DoManualCraft");
            foreach (var type in new[] { typeof(Restaurant), typeof(LuxuryShop), typeof(MercenaryGuild), typeof(RequisitionOffice), typeof(WarEffort) })
                yield return AccessTools.DeclaredMethod(type, "Update", new[] { typeof(float) });
        }

        static void Prefix(out IDisposable __state) => __state = FoundationApi.AudioInterceptionInternal.Enter(new GameAudioActivity(GameAudioActivityKind.EconomyAction));
        static Exception Finalizer(Exception __exception, IDisposable __state) { GameAudioInterceptionService.Exit(__state); return __exception; }
    }
}
