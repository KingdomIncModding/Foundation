using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        [HarmonyPatch(typeof(UIElementTechNode), nameof(UIElementTechNode.UpdateNodeStatus))]
        static class RequirementNavigation
        {
            static void Postfix(UIElementTechNode __instance, TMPro.TMP_Text ____requirementsText, RectTransform ____requirementsContainer)
            {
                if (!____requirementsText || !____requirementsContainer || !____requirementsContainer.gameObject.activeSelf) return;
                var links = ____requirementsContainer.GetComponent<RequirementLinks>() ?? ____requirementsContainer.gameObject.AddComponent<RequirementLinks>();
                links.Bind(__instance, ____requirementsText);
            }
        }
    }

}
