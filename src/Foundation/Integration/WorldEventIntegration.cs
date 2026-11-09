using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static partial class WorldEventIntegration
    {
        internal static void Install(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(Created)).Patch();
            harmony.CreateClassProcessor(typeof(Removed)).Patch();
            harmony.CreateClassProcessor(typeof(Switched)).Patch();
            harmony.CreateClassProcessor(typeof(Moved)).Patch();
        }
        // Observation callbacks may fail (e.g. a mod supplies an invalid port).
        // Never turn a diagnostic failure into a vanilla game operation failure.
        private static void Safely(Action publish)
        {
            try { publish(); }
            catch (Exception exception) { FoundationApi.Report(exception); }
        }




    }
}
