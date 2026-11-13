using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI.MainMenu;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    internal sealed class GameBindingAccess : IGameBindingAccess
    {
        private static readonly AccessTools.FieldRef<KeyBindingRow,InputAction> ActionField=AccessTools.FieldRefAccess<KeyBindingRow,InputAction>("_bindingRef");
        private static readonly AccessTools.FieldRef<KeyBindingRow,TMP_Text> TextField=AccessTools.FieldRefAccess<KeyBindingRow,TMP_Text>("_binding");
        private static readonly AccessTools.FieldRef<KeyBindingRow,Image> ButtonField=AccessTools.FieldRefAccess<KeyBindingRow,Image>("_button");
        private static readonly AccessTools.FieldRef<KeyBindingsUI,string> SnapshotField=AccessTools.FieldRefAccess<KeyBindingsUI,string>("_currentSettings");
        private static T Require<T>(Component view) where T:Component
        {var result=view ? view.GetComponent<T>() : null;return result ? result : throw new ArgumentException("Unexpected Foundation binding view.",nameof(view));}
        public InputActionAsset Actions=>Assets.Behaviour.Input.PlayerInput.Asset;
        public IReadOnlyList<InputAction> Rebindable=>Assets.Behaviour.Input.PlayerInput.RebindableKeys;
        public IReadOnlyList<Component> Rows=>Array.AsReadOnly(Resources.FindObjectsOfTypeAll<KeyBindingRow>().Cast<Component>().ToArray());
        public InputAction Action(Component row)=>ActionField(Require<KeyBindingRow>(row));
        public TMP_Text Text(Component row)=>TextField(Require<KeyBindingRow>(row));
        public TMP_Text Label(Component row)=>(TMP_Text)AccessTools.Field(typeof(KeyBindingRow),"_label").GetValue(Require<KeyBindingRow>(row));
        public void AddRebindable(InputAction action)
        {if(action==null)throw new ArgumentNullException(nameof(action));var list=Assets.Behaviour.Input.PlayerInput.RebindableKeys as List<InputAction>;if(list!=null && !list.Contains(action))list.Add(action);}
        public Image Button(Component row)=>ButtonField(Require<KeyBindingRow>(row));
        public string Snapshot(Component menu)=>SnapshotField(Require<KeyBindingsUI>(menu));
        public void SetSnapshot(Component menu,string value){SnapshotField(Require<KeyBindingsUI>(menu))=value;}
    }
}
