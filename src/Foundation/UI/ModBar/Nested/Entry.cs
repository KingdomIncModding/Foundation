using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.HUD;
using Assets.Behaviour.UI.Shared;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed partial class ModBarHost
    {
        sealed class Entry
        {
            internal RectTransform Root;
            internal IModBarView View;
            internal ModBarContext Context;
        }
    }

}
