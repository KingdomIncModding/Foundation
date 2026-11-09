using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Registration identity is fixed. Refresh applies mutable presentation and visibility; Dispose removes only this item.</summary>
    public interface IModBarRegistration : IDisposable
    {
        string Id { get; }
        string ModId { get; }
        IModBarElement Element { get; }
        bool IsRegistered { get; }
        void Refresh();
    }
}
