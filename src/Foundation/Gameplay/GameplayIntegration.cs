using System;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>Read-only observation hooks over actual native game machine methods.</summary>
    internal static class GameplayIntegration
    {
        internal static void Install(Harmony harmony)
        {
            foreach (var type in new[]
            {
                typeof(global::KingdomFoundation.RecipeChanged),
                typeof(global::KingdomFoundation.FilterChanged),
                typeof(global::KingdomFoundation.UpgradeChanged),
                typeof(global::KingdomFoundation.QuestChanged),
                typeof(global::KingdomFoundation.DungeonChanged),
                typeof(global::KingdomFoundation.QualityChanged),
                typeof(global::KingdomFoundation.Ascended)
            }

            )
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception error)
                {
                    FoundationApi.Report(new InvalidOperationException("Could not install " + type.Name + " observation hook.", error));
                }
            }
        }

        internal static void Publish<T>(T message)
        {
            try
            {
                FoundationApi.Events.Publish(message);
            }
            catch (Exception error)
            {
                FoundationApi.Report(error);
            }
        }
    }
}
