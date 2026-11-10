using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Defines the foundation asset contract.</summary>
    public interface IFoundationAsset : IDisposable
    {
        string ModId { get; }
        string AssetId { get; }
        Sprite Sprite { get; }
        /// <summary>Exposes the bundle value.</summary>
        AssetBundle Bundle { get; }
        bool IsLoaded { get; }
    }
}
