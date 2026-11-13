using System;
using System.Collections;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    internal sealed class WorkspaceReadOnlyList<TNative, TIdentity> : IReadOnlyList<TIdentity>
    {
        private readonly List<TNative> items;
        private readonly Func<TNative, TIdentity> wrap;

        internal WorkspaceReadOnlyList(List<TNative> items, Func<TNative, TIdentity> wrap)
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
            this.wrap = wrap ?? throw new ArgumentNullException(nameof(wrap));
        }

        public int Count => items.Count;

        public TIdentity this[int index] => wrap(items[index]);

        public IEnumerator<TIdentity> GetEnumerator()
        {
            foreach (var item in items)
                yield return wrap(item);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
