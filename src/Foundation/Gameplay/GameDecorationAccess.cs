using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Deco;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    internal sealed class GameDecorationAccess : IGameDecorationAccess
    {
        public Sprite ContainerIcon
        {
            get
            {
                if(GameUI.Instance)
                {
                    var menu=AccessTools.Field(typeof(GameUI),"_decoMenu").GetValue(GameUI.Instance) as Component;
                    if(menu)
                        foreach(var row in menu.GetComponentsInChildren<DecoBuyMenuRow>(true))
                        {
                            if((bool)AccessTools.Field(typeof(DecoBuyMenuRow),"_isLabel").GetValue(row) || (bool)AccessTools.Field(typeof(DecoBuyMenuRow),"_isSticker").GetValue(row))continue;
                            var cost=AccessTools.Field(typeof(DecoBuyMenuRow),"_cost").GetValue(row) as Component;
                            var icon=row.GetComponentsInChildren<Image>(true).FirstOrDefault(image=>image.gameObject!=row.gameObject && image.sprite && image.type!=Image.Type.Sliced && (!cost || !image.transform.IsChildOf(cost.transform)));
                            if(icon)return icon.sprite;
                        }
                }
                return SpriteLibrary.Get("Icons_31");
            }
        }
        private static readonly AccessTools.FieldRef<DecoOptionsPopup,RectTransform> WindowField=AccessTools.FieldRefAccess<DecoOptionsPopup,RectTransform>("_window");
        private static readonly AccessTools.FieldRef<DecoOptionsPopup,TMP_InputField> LabelField=AccessTools.FieldRefAccess<DecoOptionsPopup,TMP_InputField>("_labelText");
        private static readonly AccessTools.FieldRef<DecoOptionsStyle,int> IndexField=AccessTools.FieldRefAccess<DecoOptionsStyle,int>("_styleIndex");
        private static readonly AccessTools.FieldRef<DecoOptionsStyle,GameObject> SelectedField=AccessTools.FieldRefAccess<DecoOptionsStyle,GameObject>("_selected");
        private static readonly AccessTools.FieldRef<WorldFlowDeco,Image> BackgroundField=AccessTools.FieldRefAccess<WorldFlowDeco,Image>("_background");
        private static readonly AccessTools.FieldRef<WorldFlowDeco,Sprite[]> PaletteField=AccessTools.FieldRefAccess<WorldFlowDeco,Sprite[]>("_backgroundColors");
        private static T Require<T>(Component view) where T:Component
        {var result=view ? view.GetComponent<T>() : null;return result ? result : throw new ArgumentException("Unexpected Foundation decoration view.",nameof(view));}
        public Component Popup=>GameUI.Instance ? GameUI.Instance.DecoPopup : null;
        public Component PopupFor(Component view)=>view ? view.GetComponentInParent<DecoOptionsPopup>() : null;
        public Component View(GameObject source)=>source ? source.GetComponent<WorldFlowDeco>() : null;
        public Component CurrentView(Component popup)=>popup ? Require<DecoOptionsPopup>(popup).Decoration : null;
        public WorkspaceObject Current(Component popup)
        {var view=CurrentView(popup);return view ? FoundationApi.WorkspaceAccess.FromView(view) : null;}
        public RectTransform Window(Component popup)=>WindowField(Require<DecoOptionsPopup>(popup));
        public TMP_FontAsset Font(Component popup)=>LabelField(Require<DecoOptionsPopup>(popup)).textComponent.font;
        public IReadOnlyList<Component> Choices(Component popup)=>Array.AsReadOnly(Require<DecoOptionsPopup>(popup).GetComponentsInChildren<DecoOptionsStyle>().Cast<Component>().ToArray());
        public int ChoiceIndex(Component choice)=>IndexField(Require<DecoOptionsStyle>(choice));
        public void SetChoiceSelected(Component choice,bool selected)=>SelectedField(Require<DecoOptionsStyle>(choice)).SetActive(selected);
        public Image Background(Component view)=>BackgroundField(Require<WorldFlowDeco>(view));
        public Sprite[] BackgroundPalette(Component view)
        {var palette=PaletteField(Require<WorldFlowDeco>(view));return palette==null ? null : (Sprite[])palette.Clone();}
        public void RefreshStyle(Component view)=>Require<WorldFlowDeco>(view).UpdateStyle();
    }
}
