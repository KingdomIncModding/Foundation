using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
namespace KingdomFoundation.Machines
{
    /// <summary>Named facts read directly from native machine members. These are observations, not simulated values.</summary>
    public sealed class MachineSpecializations
    {
        private static int CountOccupied<T>(T[] slots) where T : class
        {
            int count = 0;
            foreach (var slot in slots) if (slot != null) count++;
            return count;
        }
        public IReadOnlyDictionary<string,object> Read(FlowElement machine)
        {
            MachineThread.Verify();
            if(!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in current kingdom.");
            var result = new Dictionary<string,object>(StringComparer.Ordinal);
            if(machine is RecipeElement recipe)
            {
                result["recipeId"] = recipe.Recipe?.Identifier;
                result["recipeCraftTime"] = recipe.Recipe?.CraftingTime;
                result["crafting"] = recipe.Crafting;
                result["productivityDisabled"] = recipe.ProductivityDisabled;
            }
            if(machine is ResourceElement resource)
            {
                result["resourceId"] = resource.Resource?.Identifier;
                result["nativeResourceOutputPerSecond"] = resource.OutputPerSecond;
                result["resourceQueued"] = resource.QueuedCount;
            }
            if(machine is Forest forest)
            {
                result["forestItemId"] = forest.SelectedResource?.Identifier;
                result["nativeForestOutputPerSecond"] = forest.OutputPerSecond;
                result["forestQueued"] = forest.QueuedCount;
            }
            if(machine is Collector collector)
            {
                result["inputCount"] = collector.InputCount;
                result["currentInput"] = collector.CurrentInput;
                result["lastItemType"] = collector.LastItem?.Type?.Identifier;
            }
            if(machine is Distributor distributor)
            {
                result["outputCount"] = distributor.OutputCount;
                result["currentOutput"] = distributor.CurrentOutput;
                result["lastItemType"] = distributor.LastItem?.Type?.Identifier;
            }
            if(machine is Manifold manifold)
            {
                result["inputCount"] = manifold.InputCount;
                result["lastItemType"] = manifold.LastItem?.Type?.Identifier;
            }
            if(machine is TrackSwitch sw) result["switchPosition"] = sw.SwitchPosition;
            if(machine is SingleButton button) result["queuedButtonPresses"] = button.ClickCount;
            if(machine is Assets.Source.Flow.Elements.Timer timer)
            {
                result["timerInterval"] = timer.MaxTime;
                result["timerRemaining"] = timer.TimeLeft;
            }
            if(machine is Filter filter) result["filterItemId"] = filter.FilterType?.Identifier;
            if(machine is Refiner refiner)
            {
                result["useMana"] = refiner.UseMana;
                result["lastRefineQuality"] = refiner.LastQuality;
                result["lastRefineNewQuality"] = refiner.LastNewQuality;
                result["lastManaCost"] = refiner.LastManaCost;
            }
            if(machine is Stockpile stockpile)
            {
                result["capacity"] = stockpile.Capacity;
                result["contentsCount"] = stockpile.Contents.Count;
            }
            if(machine is Armory armory)
            {
                result["channel"] = armory.Channel;
                result["outputItemType"] = armory.OutputType?.Identifier;
                result["armoryItemType"] = armory.ArmoryType?.Identifier;
            }
            if(machine is Portal portal)
            {
                result["channel"] = portal.Channel;
                result["channelIndex"] = portal.ChannelIndex;
                result["isOutput"] = portal.IsOutput;
            }
            if(machine is QuestBoard quest) result["selectedQuestId"] = quest.SelectedQuest?.Quest?.Identifier;
            if(machine is Dungeon dungeon) result["selectedDungeonId"] = dungeon.CurrentDungeon?.Dungeon?.Identifier;
            if(machine is RequisitionOffice req)
            {
                result["selectedContractIndex"] = req.SelectedContract;
                result["storedValue"] = req.StoredValue;
            }
            if(machine is Appraiser appraiser)
            {
                result["appraiserStat"] = appraiser.StatAppraised?.ToString();
                result["appraiserComparison"] = appraiser.AppraiseType.ToString();
                result["appraiserThreshold"] = appraiser.StatAmount;
            }
            if(machine is Accountant accountant)
            {
                result["accountantCurrency"] = accountant.CurrType?.ToString();
                result["accountantComparison"] = accountant.AppraiseType.ToString();
                result["accountantThreshold"] = accountant.CurrAmount;
            }
            if(machine is WarEffort war) result["warTypeId"] = war.WarType?.Identifier;
            if(machine is TrainingGrounds training)
            {
                result["capacity"] = training.Capacity;
                result["slotCount"] = training.Contents.Length;
                result["occupiedSlots"] = CountOccupied(training.Contents);
                result["energyPerTick"] = training.EnergyPerTick;
                result["qualityPerTick"] = training.QualityPerTick;
                result["tickCount"] = training.TickCount;
                result["tickDelay"] = training.TickDelay;
                result["trainingDummyPresent"] = training.TrainingDummy != null;
            }
            if (machine is Assets.Source.Flow.Elements.RecipeElement craft)
            {
                result["canAutoCraft"] = craft.CanAutoCraft;
                result["isEquipmentCraft"] = craft.IsEquipment;
                result["craftingInputPresent"] = craft.Input != null;
                result["craftableTag"] = craft.CraftableTag.ToString();
            }
            if (machine is ResourceElement supply)
            {
                result["desiredResourceOutput"] = supply.GetDesiredOutput();
                result["qualityResourceCost"] = supply.QualityResourceCost;
                result["resourceActionLabel"] = supply.ActionLabel;
            }
            if (machine is Forest wood)
                result["desiredForestOutput"] = wood.GetDesiredOutput();
            if (machine is Shop shop)
            {
                result["collectedGold"] = shop.CollectedGold;
                result["maxGold"] = shop.MaxGold;
                result["goldCap"] = shop.GoldCap;
                result["recentItemCount"] = shop.RecentItems.Count;
            }
            if (machine is Restaurant restaurant)
            {
                result["collectedGold"] = restaurant.CollectedGold;
                result["collectedCrowns"] = restaurant.CollectedCrowns;
                result["recentItemCount"] = restaurant.RecentItems.Count;
            }
            if (machine is LuxuryShop luxury)
                result["recentItemCount"] = luxury.RecentItems.Count;
            if (machine is MercenaryGuild mercenary)
                result["recentItemCount"] = mercenary.RecentItems.Count;
            if (machine is Disenchanter disenchanter)
            {
                result["collectedMana"] = disenchanter.CollectedMana;
                result["recentItemCount"] = disenchanter.RecentItems.Count;
            }
            if (machine is Incinerator incinerator)
                result["lastItemType"] = incinerator.LastItem?.Type?.Identifier;
            if (machine is Inspector inspector)
            {
                result["lastItemType"] = inspector.LastItem?.Type?.Identifier;
                result["liveItemType"] = inspector.LiveItem?.Type?.Identifier;
            }
            if (machine is Outfitter outfitter)
            {
                result["queued"] = outfitter.Queued;
                result["automated"] = outfitter.Automated;
                result["timeout"] = outfitter.Timeout;
                result["currentHumanType"] = outfitter.CurrentHuman?.Type?.Identifier;
                result["currentEquipmentType"] = outfitter.CurrentEquip?.Type?.Identifier;
            }
            if (machine is Arena arena)
            {
                result["fighting"] = arena.Fighting;
                result["fightToTheDeath"] = arena.FightToTheDeath;
                result["slot1Occupied"] = arena.Slot1 != null;
                result["slot2Occupied"] = arena.Slot2 != null;
                result["ticksRemaining"] = arena.TicksRemaining;
                result["tickCount"] = arena.TickCount;
                result["tickDelay"] = arena.TickDelay;
                result["cooldownTimer"] = arena.CooldownTimer;
                result["rewardMultiplier"] = arena.RewardMultiplier;
                result["outputAvailable"] = arena.CanOutput;
                result["blockedByWar"] = arena.BlockedByWar;
                result["energyPerTick"] = arena.EnergyPerTick;
                result["qualityPerTick"] = arena.QualityPerTick;
            }
            if (machine is Inn inn)
            {
                result["slotCount"] = inn.Contents.Length;
                result["occupiedSlots"] = CountOccupied(inn.Contents);
                result["energyPerTick"] = inn.EnergyPerTick;
                result["maxQuality"] = inn.MaxQuality;
                result["foodGoldMultiplier"] = inn.FoodGoldMultiplier;
                result["tickCount"] = inn.TickCount;
                result["tickDelay"] = inn.TickDelay;
            }
            if (machine is WarEffort warEffort)
            {
                result["warTypeId"] = warEffort.WarType?.Identifier;
                result["triggerAttack"] = warEffort.TriggerAttack;
                result["currentSoldierPresent"] = warEffort.CurrentSoldier != null;
                result["soldierCount"] = warEffort.Soldiers.Count;
            }
            if (machine is QuestBoard board)
            {
                result["partyCount"] = board.Contents.Count;
                result["partySlots"] = QuestBoard.SlotCount;
                result["coinLootMultiplier"] = board.CoinLootMultiplier;
            }
            if (machine is Dungeon delve)
            {
                result["readyToStart"] = delve.ReadyToStart;
                result["triggerAttack"] = delve.TriggerAttack;
                result["limitBreak"] = delve.LimitBreak;
                result["limitBreak2"] = delve.LimitBreak2;
                result["trashLoot"] = delve.TrashLoot;
                result["coinLootMultiplier"] = delve.CoinLootMultiplier;
            }
            if (machine is RequisitionOffice requisition)
            {
                result["collectedRequisition"] = requisition.CollectedRequisition;
                result["maxRequisition"] = requisition.MaxRequisition;
                result["contractedItemId"] = requisition.ContractedItem?.Identifier;
            }
            if (machine is Armory store)
            {
                // Includes each distinct native storage subclass: Guildhouse, Hoard,
                // Library, Pantry and Treasury (their MaxItems/InputCost override).
                result["maxItems"] = store.MaxItems;
                result["inputCost"] = store.InputCost;
                result["armoryGroupItemId"] = store.ArmoryType?.Identifier;
            }
            if (machine is Portal connectedPortal)
            {
                result["inputCost"] = connectedPortal.InputCost;
                result["lastItemType"] = connectedPortal.LastItem?.Type?.Identifier;
            }
            if (machine is Appraiser compare)
                result["appraiserHasSelection"] = compare.StatAppraised.HasValue;
            if (machine is Accountant currency)
            {
                result["accountantCurrency"] = currency.CurrType?.ToString();
                result["accountantComparison"] = currency.AppraiseType.ToString();
                result["accountantThreshold"] = currency.CurrAmount;
            }
            if (machine is DevSpawner dev)
                result["spawnItemId"] = dev.Item?.Identifier;
            if (machine is NightmareCollector nightmare)
                result["nativeProgress"] = nightmare.Progress;
            // The native class itself is recorded separately by MachineSnapshot.NativeType.
            // Avoid leaking references to native InventoryItem or mutable game lists.
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(result);
        }
    }
}
