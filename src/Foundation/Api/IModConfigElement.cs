using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Polymorphic configuration element rendered inside a mod's settings page.</summary>
    public interface IModConfigElement
    {
        string Id { get; }
        bool IsVisible { get; set; }
        GameObject CreateView(IModConfigContext context);
        void Commit();
        void Cancel();
    }
}
