using System;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Behaviour.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>Observe verified native pipe operations without changing native flow mechanics.</summary>
    internal static class FlowIntegration
    {
        internal static void Install(Harmony hooks)
        {
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.PipeConnect)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.PipeDisconnect)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.ItemTransferred)).Patch();
            hooks.CreateClassProcessor(typeof(global::KingdomFoundation.PauseMissingPipe)).Patch();
        }

        internal static void Safely(Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                FoundationApi.Report(error);
            }
        }
    }
}
