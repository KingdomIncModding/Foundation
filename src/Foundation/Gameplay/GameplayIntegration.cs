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
    internal static partial class GameplayIntegration
    {
        internal static void Install(Harmony harmony)
        {
            foreach (var type in new[] { typeof(RecipeChanged), typeof(FilterChanged), typeof(UpgradeChanged),
                typeof(QuestChanged), typeof(DungeonChanged), typeof(QualityChanged), typeof(Ascended) })
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception error) { FoundationApi.Report(new InvalidOperationException("Could not install " + type.Name + " observation hook.", error)); }
            }
        }
        static void Publish<T>(T message)
        { try { FoundationApi.Events.Publish(message); } catch (Exception error) { FoundationApi.Report(error); } }







    }
}
