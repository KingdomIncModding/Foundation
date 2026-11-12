using System;
using System.Linq;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomFoundation
{
    internal sealed class VanillaSectionView : IVanillaSectionView
    {
        readonly GameObject heading;
        readonly Action changed;
        bool requestedVisible = true;
        internal Transform Container { get; }
        public string Id { get; }
        public string Title { get; }
        public bool IsAlive => heading && Container;
        public bool IsVisible
        {
            get => heading && heading.activeSelf;
            set
            {
                if (!IsAlive) return;
                requestedVisible = value;
                changed();
            }
        }

        internal VanillaSectionView(string id, string title, GameObject heading, Transform container, Action changed)
        {
            Id = id;
            Title = title;
            this.heading = heading;
            Container = container;
            this.changed = changed;
        }

        internal void RefreshVisibility()
        {
            if (!IsAlive) return;
            bool hasRows = Container.Cast<Transform>().Any(child => child.gameObject != heading && child.gameObject.activeSelf);
            heading.SetActive(requestedVisible && hasRows);
        }
    }
}
