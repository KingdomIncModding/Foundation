using System;
using System.Collections.Generic;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomFoundation
{
    // Links are attached to the native requirement panel. Text, layout, color and
    // translation still come from the game; destinations use model references.
    /// <summary>Provides the requirement links API.</summary>
    public sealed class RequirementLinks : MonoBehaviour, IPointerClickHandler
    {
        static readonly Func<TechNode, TechNode, string> Describe = AccessTools.MethodDelegate<Func<TechNode, TechNode, string>>(
            AccessTools.Method(typeof(UITechNode), "_getRequirementDescription"));
        readonly List<TechNode> targets = new List<TechNode>();
        UIElementTechNode owner;
        TMP_Text label;
        Image background;
        string nativeText;
        bool textRaycast, backgroundRaycast, richText;
        bool singlePanelTarget;

        internal void Bind(UIElementTechNode node, TMP_Text text)
        {
            if (!label)
            {
                label = text; textRaycast = text.raycastTarget; richText = text.richText;
                background = GetComponent<Image>();
                if (background) { backgroundRaycast = background.raycastTarget; background.raycastTarget = true; }
            }
            owner = node; nativeText = text.text;
            targets.Clear();
            var lines = new List<string>();
            foreach (var id in node.Tech.AdditionalRequirements)
            {
                var required = TechNode.Get(id);
                if (required.IsPurchased) continue;
                string description = Describe(required, node.Tech);
                if (NativeResearch.Owns(required) && !NativeResearch.Active(required)) { lines.Add(description); continue; }
                lines.Add("<link=\"foundation.requirement." + targets.Count + "\">" + description + "</link>");
                targets.Add(required);
            }
            text.richText = true; text.raycastTarget = true;
            singlePanelTarget = lines.Count == 1 && targets.Count == 1;
            text.text = string.Join("\n", lines);
        }

        /// <summary>Performs the on pointer click operation.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !owner || !label || !owner.Tech.IsLocked) return;
            var camera = eventData.pressEventCamera ?? eventData.enterEventCamera;
            if (!camera) camera = TechTreeTabs.Field<Camera>(owner.Parent, "_camera");
            label.ForceMeshUpdate();
            int link = TMP_TextUtilities.FindIntersectingLink(label, eventData.position, camera);
            // A single requirement also accepts clicks on its native panel padding.
            int index = singlePanelTarget ? 0 : -1;
            const string prefix = "foundation.requirement.";
            if (link >= 0)
            {
                string id = label.textInfo.linkInfo[link].GetLinkID();
                if (!id.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(id.Substring(prefix.Length), out index)) return;
            }
            if (index < 0 || index >= targets.Count) return;
            var target = targets[index];
            if (target.IsPurchased || (NativeResearch.Owns(target) && !NativeResearch.Active(target))) return;
            owner.Parent.FocusNode(target);
        }

        void OnDestroy()
        {
            if (label) { label.raycastTarget = textRaycast; label.richText = richText; label.text = nativeText; }
            if (background) background.raycastTarget = backgroundRaycast;
        }
    }
}
