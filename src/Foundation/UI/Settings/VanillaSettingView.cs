using System;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.UI;
namespace KingdomFoundation
{
    internal sealed class VanillaSettingView : IVanillaSettingView
    {
        readonly GameObject row;
        readonly Action changed;
        public string Id { get; }
        public bool IsAlive => row;
        public bool IsVisible
        {
            get => row && row.activeSelf;
            set { if (!row) return; row.SetActive(value); changed(); }
        }
        internal VanillaSettingView(string id, GameObject row, Action changed) { Id = id; this.row = row; this.changed = changed; }
    }
}
