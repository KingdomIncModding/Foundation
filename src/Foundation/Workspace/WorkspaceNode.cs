using Assets.Source.Flow;
using Assets.Source.Player;
using UnityEngine;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Base for modder-owned native FlowElement models. Implement Update and native save/copy hooks.</summary>
    public abstract class WorkspaceNode : FlowElement
    {
        readonly WorkspaceNodeType registration;
        public IWorkspaceNodeDefinition Definition => registration.Definition;
        public sealed override string ElementType => Definition.Id;
        public override string Label => Definition.Title;
        public override ElementCategory Category => Definition.Category;
        public override string IconName => Definition.IconName;
        /// <summary>Gets or sets the icon value.</summary>
        public override Sprite Icon => Definition.Icon ? Definition.Icon : base.Icon;
        public override bool HasCraftButton => false;
        public override int UpgradeSlots => 0;

        protected WorkspaceNode(string typeId)
        {
            registration = WorkspaceNodeRegistry.Shared.Resolve(typeId);
            Size = Definition.Size; Description = Definition.Description;
            RequiredTech = TechNode.Get(Definition.RequiredTechId);
            CostCurrency = CurrencyCollection.GetDefaultCostCurrency(RequiredTech.CostTier).ToArray();
            CostMultiplier = Definition.CostMultiplier; GoldMultiplier = Definition.GoldMultiplier;
        }
        /// <summary>Determines whether purchaseable.</summary>
        public override bool IsPurchaseable(GamePlayer player) => registration.IsRegistered && player != null &&
            (!NativeResearch.Owns(RequiredTech) || NativeResearch.Active(RequiredTech)) && base.IsPurchaseable(player);
        public override CurrencyCollection GetPurchaseCost(int numElements) => Definition.FixedCost ?? base.GetPurchaseCost(numElements);
    }
}
