using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;
using Assets.Source.Items.Data;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Behaviour.Flow;

namespace KingdomFoundation
{
    /// <summary>Observe gameplay transitions via source-confirmed native methods.</summary>
    internal static class GameEventIntegration
    {
        internal static void Install(Harmony harmony)
        {
            foreach (var type in new[]
            {
                typeof(global::KingdomFoundation.AddCurrency),
                typeof(global::KingdomFoundation.SpendCurrency),
                typeof(global::KingdomFoundation.AddResource),
                typeof(global::KingdomFoundation.SetFarmResource),
                typeof(global::KingdomFoundation.UnlockTech),
                typeof(global::KingdomFoundation.QuestCompleted),
                typeof(global::KingdomFoundation.DungeonTick),
                typeof(global::KingdomFoundation.WarTick),
                typeof(global::KingdomFoundation.DungeonUnlocked),
                typeof(global::KingdomFoundation.WarUnlocked),
                typeof(global::KingdomFoundation.PrestigeSet),
                typeof(global::KingdomFoundation.NativeWorkspaceSelected),
                typeof(global::KingdomFoundation.NativePopupVisibility),
                typeof(global::KingdomFoundation.NativeEquipmentAdded),
                typeof(global::KingdomFoundation.NativeEquipmentRemoved),
                typeof(NativeDamage),
                typeof(NativeHeal),
                typeof(NativeEnergy)
            }

            )
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception error)
                {
                    FoundationApi.Report(new InvalidOperationException("Could not install " + type.Name + " native event hook.", error));
                }
            }
        }

        internal static void Publish<T>(T evt)
        {
            try
            {
                FoundationApi.Events.Publish(evt);
            }
            catch (Exception error)
            {
                FoundationApi.Report(error);
            }
        }
    }
}
