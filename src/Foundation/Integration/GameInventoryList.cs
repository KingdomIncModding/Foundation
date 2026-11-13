using System.Collections;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Api
{
    internal sealed class GameInventoryList : IReadOnlyList<GameInventoryItem>
    {
        private readonly FlowIO port;
        internal GameInventoryList(FlowIO port){this.port=port;}
        public int Count=>port.BufferedItems.Count;
        public GameInventoryItem this[int index]=>GameInventoryItem.Wrap(port.BufferedItems[index]);
        public IEnumerator<GameInventoryItem> GetEnumerator(){foreach(var item in port.BufferedItems)yield return GameInventoryItem.Wrap(item);}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
}
