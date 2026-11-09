using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Adventure;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    public sealed class MachineUpgradeChangedEvent
    {
        public FlowElement Machine { get; }
        public int Slot { get; }
        public FlowUpgrade Previous { get; }
        public FlowUpgrade Current { get; }
        public MachineUpgradeChangedEvent(FlowElement machine, int slot, FlowUpgrade previous, FlowUpgrade current)
        { Machine = machine; Slot = slot; Previous = previous; Current = current; }
    }
}
