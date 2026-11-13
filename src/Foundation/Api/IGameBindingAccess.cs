using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    public interface IGameBindingAccess
    {
        InputActionAsset Actions {get;}
        IReadOnlyList<InputAction> Rebindable {get;}
        IReadOnlyList<Component> Rows {get;}
        InputAction Action(Component row);
        TMP_Text Text(Component row);
        TMP_Text Label(Component row);
        void AddRebindable(InputAction action);
        Image Button(Component row);
        string Snapshot(Component menu);
        void SetSnapshot(Component menu,string value);
    }
}
