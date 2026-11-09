using System;
using Assets.Source.Adventure;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>Observe gameplay transitions via source-confirmed native methods.</summary>
    internal static partial class GameEventIntegration
    {
        internal static void Install(Harmony harmony)
        {
            foreach (var type in new[] {
                typeof(AddCurrency), typeof(SpendCurrency), typeof(AddResource), typeof(SetFarmResource),
                typeof(UnlockTech), typeof(QuestCompleted), typeof(DungeonTick), typeof(WarTick),
                typeof(DungeonUnlocked), typeof(WarUnlocked), typeof(PrestigeSet),
                typeof(NativeWorkspaceSelected), typeof(NativePopupVisibility),
                typeof(NativeEquipmentAdded), typeof(NativeEquipmentRemoved),
                typeof(NativeDamage), typeof(NativeHeal), typeof(NativeEnergy)
            })
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception error) { FoundationApi.Report(new InvalidOperationException("Could not install " + type.Name + " native event hook.", error)); }
            }
        }
        static void Publish<T>(T evt)
        {
            try { FoundationApi.Events.Publish(evt); }
            catch (Exception error) { FoundationApi.Report(error); }
        }











    }
}
