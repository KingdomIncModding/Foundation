using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items;
using Assets.Source.Items.Data;

namespace KingdomFoundation.Api
{
    internal sealed class GameCombatService : IGameCombatService
    {
        /// <summary>Gets combatant.</summary>
        public CombatantItemData GetCombatant(InventoryItem item)
            => (item ?? throw new ArgumentNullException(nameof(item))).Data as CombatantItemData;
        public HumanItemData GetHuman(InventoryItem item)
            => (item ?? throw new ArgumentNullException(nameof(item))).Data as HumanItemData;
        public int Stat(CombatantItemData combatant, EquipStat stat)
            => (combatant ?? throw new ArgumentNullException(nameof(combatant))).GetStat(stat);
        public int CombatPower(CombatantItemData combatant)
            => (combatant ?? throw new ArgumentNullException(nameof(combatant))).GetCombatPower();
        /// <summary>Performs the base damage operation.</summary>
        public int BaseDamage(CombatantItemData combatant, object target)
            => (combatant ?? throw new ArgumentNullException(nameof(combatant))).GetBaseDamage(target);
        public float DamageReduction(CombatantItemData combatant, Assets.Source.Adventure.AdventurerTrait resistance)
            => (combatant ?? throw new ArgumentNullException(nameof(combatant))).GetDamageReduction(resistance);
        public bool HasTrait(CombatantItemData combatant, Assets.Source.Adventure.AdventurerTrait trait)
            => (combatant ?? throw new ArgumentNullException(nameof(combatant))).HasTrait(trait);
        public IReadOnlyList<Assets.Source.Adventure.ActiveTrait> Traits(CombatantItemData combatant)
            => Array.AsReadOnly((combatant ?? throw new ArgumentNullException(nameof(combatant))).GetTraits(false).ToArray());
        /// <summary>Determines whether equip.</summary>
        public bool CanEquip(HumanItemData human, InventoryItem item, bool replace = true)
            => (human ?? throw new ArgumentNullException(nameof(human))).CanEquipItem(item ?? throw new ArgumentNullException(nameof(item)), replace);
        public IReadOnlyList<InventoryItem> ConflictingEquipment(HumanItemData human, InventoryItem item)
            => Array.AsReadOnly((human ?? throw new ArgumentNullException(nameof(human))).GetConflictingItems(item ?? throw new ArgumentNullException(nameof(item))).ToArray());
        public InventoryItem Equipped(HumanItemData human, EquipmentSlot slot)
            => (human ?? throw new ArgumentNullException(nameof(human))).GetEquippedItem(slot);

        public IReadOnlyList<InventoryItem> Equipment(HumanItemData human)
            => Array.AsReadOnly((human ?? throw new ArgumentNullException(nameof(human))).Equipment.ToArray());
        /// <summary>Attempts to equip.</summary>
        public bool TryEquip(HumanItemData human, InventoryItem item, bool replace = true)
        {
            if (human == null) throw new ArgumentNullException(nameof(human));
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!human.CanEquipItem(item, replace)) return false;
            human.AddEquipment(item);
            return true;
        }
        /// <summary>Removes equipment.</summary>
        public void RemoveEquipment(HumanItemData human, EquipmentSlot slot)
            => (human ?? throw new ArgumentNullException(nameof(human))).RemoveEquipment(slot);
        public void Heal(CombatantItemData combatant, float amount, object context = null)
        {
            if (float.IsNaN(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            (combatant ?? throw new ArgumentNullException(nameof(combatant))).Heal(amount, context);
        }
        public bool Damage(CombatantItemData combatant, float damage, object context = null, bool ignoreArmor = false)
        {
            if (float.IsNaN(damage) || damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            return (combatant ?? throw new ArgumentNullException(nameof(combatant))).TakeDamage(damage, context, ignoreArmor);
        }
        /// <summary>Performs the spend energy operation.</summary>
        public bool SpendEnergy(CombatantItemData combatant, float amount, object context = null)
        {
            if (float.IsNaN(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return (combatant ?? throw new ArgumentNullException(nameof(combatant))).LoseEnergy(amount, context);
        }
        public void Train(CombatantItemData combatant, float quality, object context = null)
        {
            if (float.IsNaN(quality) || quality < 0) throw new ArgumentOutOfRangeException(nameof(quality));
            (combatant ?? throw new ArgumentNullException(nameof(combatant))).AddTraining(quality, context);
        }
    }
}
