using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
namespace KingdomFoundation.Machines
{
    /// <summary>Discoverable command names for each native machine type; not synthetic behavior.</summary>
    public sealed class MachineCapabilities
    {
        public IReadOnlyList<string> SupportedCommands(FlowElement machine)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(machine)) throw new InvalidOperationException("Machine not in the active kingdom.");
            var names = new List<string>();
            if(machine.Inputs.Count>0 || machine.Outputs.Count>0) names.Add("InspectIO");
            names.Add("InspectState");
            if(machine is RecipeElement) names.Add("SetRecipe");
            if(machine is ResourceElement) names.Add("SetResource");
            if(machine is Forest) names.Add("SetForestResource");
            if(machine is Appraiser) names.Add("SetAppraiser");
            if(machine is Accountant) names.Add("SetAccountant");
            if(machine is WarEffort) names.Add("SetWarType");
            if(machine is Collector) names.Add("AdvanceCollector");
            if(machine is Portal) names.Add("SetPortalChannel");
            if(machine is Armory) names.Add("SetArmoryChannel");
            if(machine is Filter) names.Add("SetFilter");
            if(machine is TrackSwitch) names.Add("SetSwitch");
            if(machine is SingleButton) names.Add("PressButton");
            if(machine is Assets.Source.Flow.Elements.Timer) names.Add("SetTimer");
            if(machine is Outfitter && GamePlayer.Current.HasTech("outfitter_timeout")) names.Add("SetOutfitterTimeout");
            if(machine is Refiner && GamePlayer.Current.HasTech("refiner_mana")) names.Add("SetRefinerMana");
            if(machine is Stockpile && GamePlayer.Current.HasTech("stockpile_capacity")) names.Add("SetStockpileCapacity");
            if(machine is Collector || machine is Distributor || machine is Manifold)
            {names.Add("ExpandPorts");names.Add("CollapsePorts");}
            if(machine is Portal) names.Add("SetPortalDirection");
            if(machine is Armory) names.Add("SetArmoryOutput");
            if(machine is QuestBoard) names.Add("SetQuest");
            if(machine is Dungeon) names.Add("SetDungeon");
            if(machine is RequisitionOffice) names.Add("SetRequisitionContract");
            if(machine.CanManualCraft()) names.Add("StartManualCraft");
            foreach(var extended in FoundationMachineApi.Commands.ForMachine(machine)) names.Add(extended);
            return names.AsReadOnly();
        }
    }
}
