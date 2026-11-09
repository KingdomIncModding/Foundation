using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    public interface IFoundationAsset : IDisposable
    {
        string ModId { get; }
        string AssetId { get; }
        Sprite Sprite { get; }
        AssetBundle Bundle { get; }
        bool IsLoaded { get; }
    }
}
