using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.MainMenu;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class ConfigContext : IModConfigContext
        {
            public RectTransform Container { get; }
            public IModConfigTemplates Templates { get; }

            public ConfigContext(RectTransform container, IModConfigTemplates templates)
            {
                Container = container;
                Templates = templates;
            }
    }
}

