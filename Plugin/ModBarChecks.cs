using System;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.HUD;
using Assets.Behaviour.UI.Shared;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        IModBarRegistration lifecycleItem;
        ProbeBarElement lifecycleElement;
        ModBarHost testedBar;
        sealed class ProbeBarElement : IModBarElement
        {
            public string Id => "lifetime";
            public string ModId => "foundation.diagnostics";
            public float Width => 180;
            public int Order => 10;
            public ModBarAlignment Alignment => ModBarAlignment.Right;
            public bool IsVisible => true;
            internal int Creates, Refreshes, Disposes;
            internal Vector2 InitialSize;
            public IModBarView CreateView(IModBarContext context) { Creates++; InitialSize = context.Root.rect.size; return new ProbeView(this, context); }
            sealed class ProbeView : IModBarView
            {
                readonly ProbeBarElement element;
                readonly TMP_Text text;
                internal ProbeView(ProbeBarElement element, IModBarContext context) { this.element = element; text = context.CreateText("Custom contract"); }
                public void Refresh() { element.Refreshes++; text.text = "Custom contract"; }
                public void Dispose() { element.Disposes++; }
            }
        }
        void VerifyModBar(string directory)
        {
            var currentPlayer = Assets.Source.Player.GamePlayer.Current;
            Assets.Source.Player.GamePlayer snapshotPlayer;
            try { snapshotPlayer = Assets.Source.Player.GamePlayer.FromJson(currentPlayer.ToJson()); }
            finally { Assets.Source.Player.GamePlayer.Current = currentPlayer; }
            using (var lateResearch = FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.diagnostics.cached-player", "Snapshot storage")))
                Check(!snapshotPlayer.HasTech(((INativeTechNode)lateResearch).NativeNode), "Player snapshot created before late research safely reports its unpurchased state");
            var registry = new ModBarRegistry();
            var a = registry.Register(new ModBarText("mod.a", "same", "A", order: 2, alignment: ModBarAlignment.Right));
            var snapshot = registry.Items;
            var b = registry.Register(new ModBarText("mod.b", "same", "B", order: -1, alignment: ModBarAlignment.Left));
            var c = registry.RegisterSeparator("mod.c", "sep", ModBarAlignment.Center, order: 5);
            Check(registry.Items[0] == b && registry.Items[1] == c && registry.Items[2] == a && snapshot.Count == 1,
                "Mod Bar registry sorts controls by Alignment then Order and returns stable snapshots");
            Throws<ArgumentException>(() => registry.Register(new ModBarText("mod.a", "same", "Duplicate")), "Mod Bar duplicate identity rejected within a mod");
            Check(registry.TryGet("mod.b", "same", out var found) && found == b, "Different mods may use the same local Mod Bar item ID");
            Throws<ArgumentOutOfRangeException>(() => new ModBarText("mod.a", "bad", "", float.NaN), "Invalid Mod Bar width rejected");
            Throws<ArgumentOutOfRangeException>(() => new ModBarText("mod.a", "bad", "", 180, 0, (ModBarAlignment)99), "Invalid Mod Bar alignment rejected");
            Throws<ArgumentException>(() => new ModBarButton("", "button", ""), "Mod Bar requires an owning mod GUID");
            Throws<ArgumentException>(() => new ModBarDropdown("mod.a", "dropdown", Array.Empty<string>()), "Empty dropdown rejected");
            Throws<ArgumentOutOfRangeException>(() => new ModBarDropdown("mod.a", "dropdown", new[] { "One" }, 1), "Out of range dropdown selection rejected");
            a.Dispose(); a.Dispose(); var replacement = registry.Register(new ModBarText("mod.a", "same", "Replacement")); a.Dispose();
            Check(replacement.IsRegistered && registry.Items.Count == 3, "Stale Mod Bar disposal cannot remove a replacement registration");
            Throws<ObjectDisposedException>(a.Refresh, "Disposed Mod Bar handle rejects refresh"); b.Dispose(); c.Dispose(); replacement.Dispose();

            var pane = GameUI.Instance.GetComponentInChildren<CurrencyPane>(true);
            testedBar = pane.GetComponent<ModBarHost>();
            Check(testedBar && testedBar.Clip && testedBar.Bar && testedBar.Clip.parent == pane.transform.parent && testedBar.Bar.parent == testedBar.Clip,
                "Shared Mod Bar is created beneath the native currency HUD inside its slide clip container");
            Check(!testedBar.Bar.Find("Heading") && !testedBar.Bar.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "Mod Bar"),
                "Mod Bar omits the static Mod Bar heading label");
            var currency = (RectTransform)pane.transform;
            var parent = (RectTransform)currency.parent;
            var originalCurrency = currency.anchoredPosition;
            var corners = new Vector3[4]; currency.GetWorldCorners(corners);
            float bottom = parent.InverseTransformPoint(corners[0]).y;
            Check(Mathf.Approximately(testedBar.Clip.anchoredPosition.y, bottom - parent.rect.yMax - ModBarHost.Gap)
                && Mathf.Approximately(ModBarHost.Gap, 2f) && currency.anchoredPosition == originalCurrency,
                "Mod Bar leaves a 2-pixel gap beneath the vanilla currency bar without moving native currency UI");
            Check(testedBar.ToggleButton && testedBar.ToggleButton.GetComponentInChildren<TMP_Text>().text == "^"
                && ((RectTransform)testedBar.ToggleButton.transform.parent).anchorMin == new Vector2(.5f, 1),
                "Mod Bar includes a top-center chevron visibility toggle");
            ModBarHost.Attach(GameUI.Instance);
            Check(pane.GetComponents<ModBarHost>().Length == 1, "Repeated HUD attachment does not duplicate the Mod Bar");
            Check(testedBar.Bar.GetComponent<Image>().sprite == pane.GetComponent<Image>().sprite, "Mod Bar reuses the native currency frame sprite");
            Check(FoundationApi.ModBar.Items.Count >= 6 && testedBar.Slots.Count >= 6,
                "Separately compiled consumer registers text, button, separator, dropdown, toggle and custom interface implementation before scene load");
            var sepSlot = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:separator");
            Check(sepSlot.Find("Separator") && sepSlot.Find("Separator").GetComponent<Image>() && !sepSlot.Find("Separator").GetComponent<Image>().raycastTarget,
                "ModBarSeparator creates a non-raycasting vertical divider inside its slot");
            var leftSlot = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:status");
            var centerSlot = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:enabled");
            var rightSlot = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:custom");
            Check(leftSlot.anchoredPosition.x < centerSlot.anchoredPosition.x && centerSlot.anchoredPosition.x < rightSlot.anchoredPosition.x,
                "Mod Bar places Left, Center and Right aligned controls across their respective regions");
            var exampleButton = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:button").GetComponentInChildren<Button>();
            var status = leftSlot.GetComponentInChildren<TMP_Text>();
            var gold = Assets.Source.Player.GamePlayer.Current.GetCurrency(Assets.Source.Player.Currency.Gold);
            Click(exampleButton); testedBar.Refresh();
            Check(status.text == "Clicks: 1" && Assets.Source.Player.GamePlayer.Current.GetCurrency(Assets.Source.Player.Currency.Gold) == gold, "Mod Bar button invokes external callback without changing currency");
            var definition = (ModBarButton)FoundationApi.ModBar.Items.Single(r => r.Id == "button" && r.ModId == "urakka.foundation.example").Element;
            definition.Interactable = false; FoundationApi.ModBar.Items.Single(r => ReferenceEquals(r.Element, definition)).Refresh(); testedBar.Refresh();
            ExecuteEvents.Execute(exampleButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            Check(status.text == "Clicks: 1" && !exampleButton.interactable, "Disabled Mod Bar button ignores pointer clicks");
            definition.Interactable = true; testedBar.Refresh();
            var dropdown = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:period").GetComponentInChildren<TMP_Dropdown>();
            Check(dropdown.GetComponent<Image>().sprite == GameUI.Instance.AccountantPopup.GetComponentInChildren<TMP_Dropdown>(true).GetComponent<Image>().sprite, "Mod Bar dropdown uses the game's native control prefab");
            dropdown.Show(); Canvas.ForceUpdateCanvases();
            var list = dropdown.transform.Find("Dropdown List");
            Check(list && list.gameObject.activeInHierarchy, "Native Mod Bar dropdown opens its options");
            Capture(TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera"), Path.Combine(directory, "foundation-mod-bar-dropdown.png"));
            File.WriteAllLines(Path.Combine(directory, "mod-bar-dropdown-layout.txt"), list.GetComponentsInChildren<RectTransform>(true).Select(r => r.name + " rect=" + r.rect + " world=" + r.position));
            var option = list.GetComponentsInChildren<Toggle>().Last(); PointerClick(option);
            testedBar.Refresh();
            var dropdownElement = (ModBarDropdown)FoundationApi.ModBar.Items.Single(r => r.Id == "period" && r.ModId == "urakka.foundation.example").Element;
            Check(dropdownElement.SelectedIndex == 2 && status.text == "Period: Hour", "Native option selection updates typed dropdown state and external callback");
            dropdownElement.SetSelectedIndex(1); testedBar.Refresh();
            Check(dropdown.value == 1 && status.text == "Period: Hour", "Programmatic dropdown refresh does not re-fire selection callbacks");
            dropdown.Hide();
            PointerClick(testedBar.ToggleButton);
            testedBar.StepSlide(0.05f);
            Check(!testedBar.IsExpanded && testedBar.ToggleButton.GetComponentInChildren<TMP_Text>().text == "v"
                && testedBar.SlideProgress > 0f && testedBar.SlideProgress < 1f
                && testedBar.Bar.anchoredPosition.y > 0f && testedBar.Bar.anchoredPosition.y < ModBarHost.Height,
                "Clicking the top-center chevron starts a smooth non-instant upward slide transition");
        }
        void FinishModBarChecks(string directory)
        {
            testedBar.StepSlide(ModBarHost.SlideDuration);
            Check(Mathf.Approximately(testedBar.SlideProgress, 0f) && Mathf.Approximately(testedBar.Bar.anchoredPosition.y, ModBarHost.Height) && testedBar.ToggleButton.gameObject.activeInHierarchy,
                "Collapsed Mod Bar finishes sliding out of view while keeping the top-center chevron button accessible");
            PointerClick(testedBar.ToggleButton);
            testedBar.StepSlide(0.05f);
            Check(testedBar.IsExpanded && testedBar.ToggleButton.GetComponentInChildren<TMP_Text>().text == "^"
                && testedBar.SlideProgress > 0f && testedBar.SlideProgress < 1f,
                "Clicking the chevron again slides the Mod Bar smoothly back into view");
            testedBar.StepSlide(ModBarHost.SlideDuration);
            Check(Mathf.Approximately(testedBar.SlideProgress, 1f) && Mathf.Approximately(testedBar.Bar.anchoredPosition.y, 0f),
                "Expanded Mod Bar settles at its full open position");

            var pane = GameUI.Instance.GetComponentInChildren<CurrencyPane>(true);
            var status = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:status").GetComponentInChildren<TMP_Text>();
            var toggle = testedBar.Slots.Single(s => s.name == "urakka.foundation.example:enabled").GetComponentInChildren<Toggle>(); PointerClick(toggle); testedBar.Refresh();
            Check(toggle.isOn && status.text == "Enabled", "Native toggle invokes the mod-owned bool callback");
            var hostCanvas = pane.GetComponentInParent<Canvas>(); GameUI.Instance.ToggleUI(false);
            Check(!hostCanvas.enabled && testedBar.Bar.GetComponentInParent<Canvas>() == hostCanvas, "Mod Bar obeys native HUD visibility and UI scale"); GameUI.Instance.ToggleUI(true);
            pane.gameObject.SetActive(false);
            Check(!testedBar.Bar.gameObject.activeSelf && !testedBar.Clip.gameObject.activeSelf, "Disabling the currency HUD also disables its sibling Mod Bar");
            pane.gameObject.SetActive(true); testedBar.Refresh();
            Check(testedBar.Bar.gameObject.activeInHierarchy, "Re-enabling the currency HUD restores one Mod Bar and its controls");
            var blocker = testedBar.Bar.GetComponent<UIBlocksZoom>(); blocker.OnPointerEnter(new PointerEventData(EventSystem.current));
            Check(UIBlocksZoom.Blocked, "Mod Bar controls block workspace mouse-wheel zoom while hovered"); blocker.OnPointerExit(new PointerEventData(EventSystem.current));

            var textDefinition = (ModBarText)FoundationApi.ModBar.Items.Single(r => r.Id == "status" && r.ModId == "urakka.foundation.example").Element;
            var textHandle = FoundationApi.ModBar.Items.Single(r => ReferenceEquals(r.Element, textDefinition));
            textDefinition.IsVisible = false; textHandle.Refresh(); testedBar.Refresh();
            Check(!status.gameObject.activeInHierarchy, "Polymorphic visibility removes hidden controls from Mod Bar layout");
            textDefinition.IsVisible = true; textDefinition.Text = "Example ready"; textHandle.Refresh(); testedBar.Refresh();
            lifecycleElement = new ProbeBarElement(); lifecycleItem = FoundationApi.ModBar.Register(lifecycleElement); testedBar.Refresh();
            Check(lifecycleElement.Creates == 1 && lifecycleElement.Refreshes > 0, "Late custom IModBarElement creates and refreshes its own IModBarView");
            Check(lifecycleElement.InitialSize == new Vector2(180, 40), "Custom view receives its allocated slot dimensions during construction");
            var temporary = FoundationApi.ModBar.Register(new ModBarText("foundation.diagnostics", "remove", "Temporary")); testedBar.Refresh();
            temporary.Dispose(); testedBar.Refresh();
            Check(!testedBar.Slots.Any(s => s.name.EndsWith(":remove")), "Disposing a registration removes its live control");
            var parent = (RectTransform)testedBar.Clip.parent; var oldSize = parent.sizeDelta;
            try
            {
                parent.sizeDelta = new Vector2(oldSize.x - 1000, oldSize.y); testedBar.Refresh();
                Check(testedBar.Scroll.content.rect.width > testedBar.Scroll.viewport.rect.width && testedBar.Scroll.horizontal && !testedBar.Scroll.vertical, "Narrow HUD uses horizontal overflow instead of overlapping controls");
                Capture(TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera"), Path.Combine(directory, "foundation-mod-bar-overflow.png"));
                PointerClick(testedBar.Bar.Find("Next").GetComponentInChildren<Button>());
                Check(testedBar.Scroll.horizontalNormalizedPosition > 0, "Overflow arrow reaches controls beyond the visible viewport");
            }
            finally { parent.sizeDelta = oldSize; testedBar.Refresh(); }
            testedBar.Scroll.horizontalNormalizedPosition = 0;
            var visibleSlots = testedBar.Slots.Where(s => s.gameObject.activeSelf).OrderBy(s => s.anchoredPosition.x).ToArray();
            Check(visibleSlots.Zip(visibleSlots.Skip(1), (left, right) => left.anchoredPosition.x + left.rect.width <= right.anchoredPosition.x).All(v => v), "Visible controls occupy separate slots in deterministic order");
            Capture(TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera"), Path.Combine(directory, "foundation-mod-bar.png"));
        }
        void PointerClick(Selectable control)
        {
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, control.transform.TransformPoint(((RectTransform)control.transform).rect.center));
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Selectable>() == control, "Mod Bar control receives actual UI raycasts: " + control.name + " hits=" + string.Join(",", hits.Select(h => h.gameObject.name)));
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
    }
}
