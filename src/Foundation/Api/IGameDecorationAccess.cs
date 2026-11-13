using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    /// <summary>Native decoration and popup primitives. Color policy and custom UI belong to mods.</summary>
    public interface IGameDecorationAccess
    {
        UnityEngine.Sprite ContainerIcon {get;}
        Component Popup { get; }
        Component PopupFor(Component view);
        Component View(GameObject source);
        Component CurrentView(Component popup);
        WorkspaceObject Current(Component popup);
        RectTransform Window(Component popup);
        TMP_FontAsset Font(Component popup);
        IReadOnlyList<Component> Choices(Component popup);
        int ChoiceIndex(Component choice);
        void SetChoiceSelected(Component choice,bool selected);
        Image Background(Component view);
        Sprite[] BackgroundPalette(Component view);
        void RefreshStyle(Component view);
    }
}
