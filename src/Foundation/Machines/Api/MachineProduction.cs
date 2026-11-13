using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
namespace KingdomFoundation.Machines
{
    /// <summary>Game-provided production catalogs and native rates; deliberately no synthetic production simulation.</summary>
    public sealed class MachineProduction
    {
        public IReadOnlyList<CraftingRecipe> AvailableRecipes(RecipeElement machine)
        {
            MachineThread.Verify();
            if(!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            return new List<CraftingRecipe>(machine.GetAvailableRecipes()).AsReadOnly();
        }
        public IReadOnlyList<ResourceItemType> AvailableResources(ResourceElement machine)
        {
            MachineThread.Verify();
            if(!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            return new List<ResourceItemType>(machine.GetAvailableResources()).AsReadOnly();
        }
        public float? NativeOutputRate(FlowElement machine)
        {
            MachineThread.Verify();
            if(!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            if(machine is ResourceElement resource) return resource.OutputPerSecond;
            if(machine is Forest forest) return forest.OutputPerSecond;
            return null; // Not asserted for types where vanilla has no single comparable rate.
        }
        public float? SelectedCraftingTime(FlowElement machine)
        {
            MachineThread.Verify();
            if(!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            return machine is RecipeElement recipe && recipe.Recipe != null ? (float?)recipe.Recipe.CraftingTime : null;
        }
        /// <summary>The native per-machine purchase cost; reflects current player
        /// counts, cost multiplier, tech and currency. Never cached as a fixed cap.</summary>
        public Assets.Source.Player.CurrencyCollection NativePurchaseCost(FlowElement machine)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            return machine.GetPurchaseCost();
        }

        /// <summary>The native refund. Calling does not remove a machine.</summary>
        public Assets.Source.Player.CurrencyCollection NativeRefund(FlowElement machine)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            return machine.GetRefund();
        }

        /// <summary>The native recipe-specific crafting cost, not a Foundation
        /// recipe calculation. Use only a real native RecipeElement.</summary>
        public Assets.Source.Player.CurrencyCollection NativeCraftingCost(RecipeElement machine, CraftingRecipe recipe = null)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            if (recipe != null && !machine.CanCraft(recipe)) throw new ArgumentException("Recipe is not craftable by this machine.", nameof(recipe));
            return machine.GetCraftingCost(recipe);
        }

        /// <summary>Native resource cost for resource subclasses.</summary>
        public Assets.Source.Player.CurrencyCollection NativeResourceCost(ResourceElement machine, ResourceItemType resource = null)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            if (resource != null && !machine.CanSupplyResource(resource)) throw new ArgumentException("Resource unsupported by this machine.", nameof(resource));
            return machine.GetResourceCost(resource);
        }

        /// <summary>Native forest selection cost for the currently unlocked resource.
        /// No simulation of resource pricing.</summary>
        public Assets.Source.Player.CurrencyCollection NativeForestCost(Forest machine, ForestItemType resource = null)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            return machine.GetResourceCost(resource);
        }

        public IReadOnlyList<Assets.Source.Flow.FlowUpgrade> GetUpgrades(FlowElement machine)
        {
            MachineThread.Verify();
            if(!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in active kingdom.");
            var result = new List<Assets.Source.Flow.FlowUpgrade>();
            for(int i=0;i<machine.UpgradeSlots;i++)
            {
                var upgrade=machine.GetUpgrade(i);
                if(upgrade!=null) result.Add(upgrade);
            }
            return result.AsReadOnly();
        }
    }
}
