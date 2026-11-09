using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;

namespace KingdomFoundation.Machines
{
    /// <summary>A documented native FlowElement subclass from the supplied Kingdom Inc game source.
    /// The presence of a C# type does not imply it is unlocked, buildable or registered in FlowPrefabSet.
    /// These values do not allocate or simulate game machines.</summary>
    public sealed class NativeMachineTypeDescriptor
    {
        public Type NativeType { get; }
        public string Family { get; }
        public string SourcePath { get; }
        public string NativeBaseType { get; }
        internal NativeMachineTypeDescriptor(Type type, string family, string nativeBase)
        {
            NativeType = type;
            Family = family;
            SourcePath = "Assets.Source.Flow.Elements/" + type.Name + ".cs";
            NativeBaseType = nativeBase;
        }
    }

    /// <summary>Exact-type inventory of concrete native element classes, including types that may
    /// not appear in the current build menu. Prefab/unlock checks always consult the game itself.</summary>
    public sealed class NativeMachineCatalog
    {
        private static readonly IReadOnlyList<NativeMachineTypeDescriptor> Types = Array.AsReadOnly(new[]
        {
            new NativeMachineTypeDescriptor(typeof(Accountant), "Controls", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(AdventureSupplies), "Resource", "ResourceElement"),
            new NativeMachineTypeDescriptor(typeof(AdventurersGuild), "Resource", "ResourceElement"),
            new NativeMachineTypeDescriptor(typeof(Alchemist), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Appraiser), "Controls", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Arena), "Adventure", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Armory), "Storage", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Barracks), "Resource", "ResourceElement"),
            new NativeMachineTypeDescriptor(typeof(Blacksmith), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Butcher), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Campfire), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Carpenter), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Collector), "Logistics", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(DemoTurtle), "Internal", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(DevSpawner), "Internal", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Disenchanter), "Commerce", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Distributor), "Logistics", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Dungeon), "Adventure", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Enchanter), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Engraver), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Farm), "Resource", "ResourceElement"),
            new NativeMachineTypeDescriptor(typeof(Filter), "Logistics", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Forest), "Resource", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Forge), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Graveyard), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Guildhouse), "Storage", "Armory"),
            new NativeMachineTypeDescriptor(typeof(Hoard), "Storage", "Armory"),
            new NativeMachineTypeDescriptor(typeof(Incinerator), "Logistics", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Inn), "Adventure", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Inspector), "Logistics", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Inventor), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Jeweler), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Kitchen), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Leatherworker), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Library), "Storage", "Armory"),
            new NativeMachineTypeDescriptor(typeof(LuxuryShop), "Commerce", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Manifold), "Logistics", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(MercenaryGuild), "Adventure", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Mine), "Resource", "ResourceElement"),
            new NativeMachineTypeDescriptor(typeof(Necromancer), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(NightmareCollector), "Internal", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Outfitter), "Controls", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Pantry), "Storage", "Armory"),
            new NativeMachineTypeDescriptor(typeof(Portal), "Logistics", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(QuestBoard), "Adventure", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Refiner), "Refinement", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(RequisitionOffice), "Adventure", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Restaurant), "Commerce", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Scribe), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Shop), "Commerce", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Shrine), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(SingleButton), "Controls", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Stockpile), "Logistics", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(SummoningCircle), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(SupplyDepot), "Resource", "ResourceElement"),
            new NativeMachineTypeDescriptor(typeof(Tailor), "Recipe", "RecipeElement"),
            new NativeMachineTypeDescriptor(typeof(Timer), "Controls", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(TrackSwitch), "Controls", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(TrainingGrounds), "Adventure", "FlowElement"),
            new NativeMachineTypeDescriptor(typeof(Treasury), "Storage", "Armory"),
            new NativeMachineTypeDescriptor(typeof(WarEffort), "Adventure", "FlowElement"),
        });

        /// <summary>Class declarations present in the supplied native game source, not buildable previews.</summary>
        public IReadOnlyList<NativeMachineTypeDescriptor> KnownTypes => Types;

        /// <summary>Returns the exact supported native type; mod subclasses are intentionally not
        /// misidentified as vanilla. Unknown types can still be queried through generic APIs.</summary>
        public NativeMachineTypeDescriptor Describe(FlowElement machine)
        {
            if (machine == null) throw new ArgumentNullException(nameof(machine));
            var actual = machine.GetType();
            foreach (var candidate in Types) if (candidate.NativeType == actual) return candidate;
            return null;
        }

        /// <summary>Checks whether the *game's* prefab catalog includes a native type. Never
        /// assume that every concrete C# class is buildable or currently unlocked.</summary>
        public IReadOnlyList<NativeMachineTypeDescriptor> RegisteredTypes()
        {
            MachineThread.Verify();
            var discovered = new List<NativeMachineTypeDescriptor>();
            if (MachineThread.Player == null) return discovered.AsReadOnly();
            foreach (var preview in FlowElement.GetAllTypes())
            {
                var found = Describe(preview);
                if (found != null) discovered.Add(found);
            }
            return discovered.AsReadOnly();
        }
    }
}
