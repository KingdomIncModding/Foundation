using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation.Api
{
    // A config edit session is independent of how often its view is rebuilt.
    // Do not expose this internal lifecycle on the binary-compatible public contracts.
    internal interface IModConfigEditSession
    {
        void BeginEdit();
    }
}
