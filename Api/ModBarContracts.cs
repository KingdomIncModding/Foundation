using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Horizontal region within the shared Mod Bar.</summary>
    public enum ModBarAlignment
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    /// <summary>A session-wide registry. Register on Unity's main thread, including before the game scene loads.</summary>
    public interface IModBarRegistry
    {
        IReadOnlyList<IModBarRegistration> Items { get; }
        event Action Changed;
        IModBarRegistration Register(IModBarElement element);
        IModBarRegistration RegisterSeparator(string modId, string id, ModBarAlignment alignment = ModBarAlignment.Left, int order = 0, float width = 16);
        bool TryGet(string modId, string id, out IModBarRegistration registration);
    }

    /// <summary>A polymorphic control definition. CreateView is called once for each HUD lifetime.</summary>
    public interface IModBarElement
    {
        string Id { get; }
        string ModId { get; }
        float Width { get; }
        int Order { get; }
        ModBarAlignment Alignment { get; }
        bool IsVisible { get; }
        IModBarView CreateView(IModBarContext context);
    }

    /// <summary>Owns one scene's presentation, never the registration. Dispose releases mod-owned subscriptions/resources.</summary>
    public interface IModBarView : IDisposable
    {
        void Refresh();
    }

    /// <summary>Framework-owned slot and native-style factories. Custom controls must remain children of Root.</summary>
    public interface IModBarContext
    {
        RectTransform Root { get; }
        TMP_Text CreateText(string text);
        Button CreateButton(string text, Action clicked);
        TMP_Dropdown CreateDropdown(IReadOnlyList<string> options, int selectedIndex, Action<int> selected);
        Toggle CreateToggle(string text, bool value, Action<bool> changed);
        Image CreateSeparator();
    }

    /// <summary>Registration identity is fixed. Refresh applies mutable presentation and visibility; Dispose removes only this item.</summary>
    public interface IModBarRegistration : IDisposable
    {
        string Id { get; }
        string ModId { get; }
        IModBarElement Element { get; }
        bool IsRegistered { get; }
        void Refresh();
    }

    /// <summary>Optional base class; implementing IModBarElement directly is equally supported.</summary>
    public abstract class ModBarElement : IModBarElement
    {
        public string Id { get; }
        public string ModId { get; }
        public float Width { get; }
        public int Order { get; set; }
        public ModBarAlignment Alignment { get; set; }
        public bool IsVisible { get; set; } = true;
        protected ModBarElement(string modId, string id, float width = 180, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
        {
            ModBarRegistry.ValidateIdentity(modId, id);
            ModBarRegistry.ValidateWidth(width);
            ModBarRegistry.ValidateAlignment(alignment);
            ModId = modId; Id = id; Width = width; Order = order; Alignment = alignment;
        }
        public abstract IModBarView CreateView(IModBarContext context);
    }
}
