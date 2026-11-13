using System;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    internal static class WorldEventIntegration
    {
        internal static void Install(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(global::KingdomFoundation.Created)).Patch();
            harmony.CreateClassProcessor(typeof(global::KingdomFoundation.Removed)).Patch();
            harmony.CreateClassProcessor(typeof(global::KingdomFoundation.Switched)).Patch();
            harmony.CreateClassProcessor(typeof(global::KingdomFoundation.Moved)).Patch();
        }

        internal static void Safely(Action publish)
        {
            try
            {
                publish();
            }
            catch (Exception exception)
            {
                FoundationApi.Report(exception);
            }
        }
    }
}
