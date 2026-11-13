using Assets.Behaviour.Flow;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(WorldFlowManager), nameof(WorldFlowManager.ShowFlow), new[] { typeof(int) })]
    internal static class NativeWorkspaceSelected
    {
        static void Prefix(out int __state) => __state = GamePlayer.Current == null ? -1 : GamePlayer.Current.CurrentFlow;
        static void Postfix(int __state)
        {
            var player = GamePlayer.Current;
            if (player == null || player.CurrentFlow == __state || player.CurrentFlow < 0 || player.CurrentFlow >= player.Flows.Count)
                return;
            global::KingdomFoundation.GameEventIntegration.Publish(new NativeWorkspaceSelectedEvent(player, __state, player.CurrentFlow, player.ActiveFlow));
        }
    }
}
