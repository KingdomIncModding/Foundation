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
        /// <summary>Invokes the stat operation.</summary>
        /// <param name="combatant">The combatant argument.</param>
        /// <param name="stat">The stat argument.</param>
        /// <returns>The operation result.</returns>
        int Stat(CombatantItemData combatant, EquipStat stat);
        int CombatPower(CombatantItemData combatant);
        int BaseDamage(CombatantItemData combatant, object target);
        float DamageReduction(CombatantItemData combatant, Assets.Source.Adventure.AdventurerTrait resistance);
        /// <summary>Performs the has trait operation.</summary>
        bool HasTrait(CombatantItemData combatant, Assets.Source.Adventure.AdventurerTrait trait);
        IReadOnlyList<Assets.Source.Adventure.ActiveTrait> Traits(CombatantItemData combatant);
        bool CanEquip(HumanItemData human, InventoryItem item, bool replace = true);
        IReadOnlyList<InventoryItem> ConflictingEquipment(HumanItemData human, InventoryItem item);
        /// <summary>Invokes the equipped operation.</summary>
        /// <param name="human">The human argument.</param>
        /// <param name="slot">The slot argument.</param>
        /// <returns>The operation result.</returns>
        InventoryItem Equipped(HumanItemData human, EquipmentSlot slot);

        IReadOnlyList<InventoryItem> Equipment(HumanItemData human);
        bool TryEquip(HumanItemData human, InventoryItem item, bool replace = true);
        /// <summary>Removes equipment.</summary>
        void RemoveEquipment(HumanItemData human, EquipmentSlot slot);
        void Heal(CombatantItemData combatant, float amount, object context = null);
        bool Damage(CombatantItemData combatant, float damage, object context = null, bool ignoreArmor = false);
        bool SpendEnergy(CombatantItemData combatant, float amount, object context = null);
        /// <summary>Invokes the train operation.</summary>
        /// <param name="combatant">The combatant argument.</param>
        /// <param name="quality">The quality argument.</param>
        /// <param name="context">The context argument.</param>
        void Train(CombatantItemData combatant, float quality, object context = null);
    }
}
