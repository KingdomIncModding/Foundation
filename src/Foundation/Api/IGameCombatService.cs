using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Items;
using Assets.Source.Items.Data;

namespace KingdomFoundation.Api
{
    /// <summary>Native combatant, adventurer and equipment operations.</summary>
    public interface IGameCombatService
    {
        CombatantItemData GetCombatant(InventoryItem item);
        HumanItemData GetHuman(InventoryItem item);
        int Stat(CombatantItemData combatant, EquipStat stat);
        int CombatPower(CombatantItemData combatant);
        int BaseDamage(CombatantItemData combatant, object target);
        float DamageReduction(CombatantItemData combatant, Assets.Source.Adventure.AdventurerTrait resistance);
        bool HasTrait(CombatantItemData combatant, Assets.Source.Adventure.AdventurerTrait trait);
        IReadOnlyList<Assets.Source.Adventure.ActiveTrait> Traits(CombatantItemData combatant);
        bool CanEquip(HumanItemData human, InventoryItem item, bool replace = true);
        IReadOnlyList<InventoryItem> ConflictingEquipment(HumanItemData human, InventoryItem item);
        InventoryItem Equipped(HumanItemData human, EquipmentSlot slot);

        IReadOnlyList<InventoryItem> Equipment(HumanItemData human);
        bool TryEquip(HumanItemData human, InventoryItem item, bool replace = true);
        void RemoveEquipment(HumanItemData human, EquipmentSlot slot);
        void Heal(CombatantItemData combatant, float amount, object context = null);
        bool Damage(CombatantItemData combatant, float damage, object context = null, bool ignoreArmor = false);
        bool SpendEnergy(CombatantItemData combatant, float amount, object context = null);
        void Train(CombatantItemData combatant, float quality, object context = null);
    }
}
