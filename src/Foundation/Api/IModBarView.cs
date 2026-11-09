using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Owns one scene's presentation, never the registration. Dispose releases mod-owned subscriptions/resources.</summary>
    public interface IModBarView : IDisposable
    {
        void Refresh();
    }
}
