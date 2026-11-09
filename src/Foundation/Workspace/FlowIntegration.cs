using System;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Behaviour.Flow;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>Observe verified native pipe operations without changing native flow mechanics.</summary>
    internal static partial class FlowIntegration
    {
        internal static void Install(Harmony hooks)
        {
            hooks.CreateClassProcessor(typeof(PipeConnect)).Patch();
            hooks.CreateClassProcessor(typeof(PipeDisconnect)).Patch();
            hooks.CreateClassProcessor(typeof(ItemTransferred)).Patch();
            hooks.CreateClassProcessor(typeof(PauseMissingPipe)).Patch();
        }

        static void Safely(Action action)
        {
            try { action(); }
            catch (Exception error) { FoundationApi.Report(error); }
        }






    }
}
