using System;
using System.Threading;
using Assets.Source.Player;
namespace KingdomFoundation.Machines
{
    internal static class MachineThread
    {
        private static int _unityThreadId;
        internal static void CaptureMainThread() => _unityThreadId = Thread.CurrentThread.ManagedThreadId;
        internal static void Verify()
        {
            if (_unityThreadId == 0) throw new InvalidOperationException("Foundation Machines has not initialized; check BepInEx dependency loading.");
            if (Thread.CurrentThread.ManagedThreadId != _unityThreadId)
                throw new InvalidOperationException("Machine APIs must be called on Unity's main thread.");
        }
        internal static GamePlayer Player => Assets.Behaviour.Flow.WorldFlowManager.Instance ? GamePlayer.Current : null;
        internal static bool IsPlaced(Assets.Source.Flow.FlowElement el)
        {
            if (el == null || Player == null) return false;
            foreach (var flow in Player.Flows) if (flow.Elements.Contains(el)) return true;
            return false;
        }
    }
}
