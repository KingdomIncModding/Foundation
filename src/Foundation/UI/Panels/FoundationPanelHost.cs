using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Flow;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomFoundation
{
    /// <summary>One native popup host shared by all mods. GameUI owns popup input and escape.</summary>
    internal sealed class FoundationPanelHost : UIPopupMenu
    {
        private static FoundationPanelHost current;
        private GameUI ui;
        private FoundationPanelDefinition definition;
        private FlowElement machine;
        private RectTransform panelRect;
        private RectTransform rows;
        private ScrollRect contentScroll;
        private TMP_Text title;
        private TMP_Text status;
        private TMP_Text nativeText;
        private Image nativeButton;
        private TMP_Dropdown nativeDropdown;
        private Toggle nativeToggle;
        private readonly List<ModBarContext> contexts = new List<ModBarContext>();
        private readonly List<Action> disposers = new List<Action>();
        private readonly List<LiveLabel> live = new List<LiveLabel>();
        private float nextLive;
        private float nextMachineValidation;
        private bool rendering;
        private bool confirmations;
        private bool active;
        private string transientMessage;
        private string defaultMessage;
        private readonly Dictionary<string, string> temporaryValues = new Dictionary<string, string>(StringComparer.Ordinal);

        internal static bool IsShowing => current && current.active && current.gameObject.activeInHierarchy;
        internal FlowElement Machine => machine;
        internal FoundationPanelDefinition Definition => definition;
        internal string PanelId => definition?.Id;
        internal RectTransform Rows => rows;
        internal TMP_Text NativeFont => nativeText;
        internal Image NativeButton => nativeButton;
        internal TMP_Dropdown NativeDropdown => nativeDropdown;
        internal Toggle NativeToggle => nativeToggle;

        internal static bool Open(FoundationPanelDefinition panel, FlowElement nativeMachine)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            var ui = GameUI.Instance;
            if (!ui) return false;
            var host = Attach(ui);
            host.OpenInternal(panel, nativeMachine);
            return true;
        }

        private static FoundationPanelHost Attach(GameUI gameUi)
        {
            if (current && current.ui == gameUi) return current;
            FoundationGameTheme.InvalidateRuntimeReferences();
            // The root is placed alongside a native popup so all camera,
            // canvas scaling, focus and UI close behavior is controlled by GameUI.
            var template = (UIPopupMenu)gameUi.AccountantPopup;
            if (!template) throw new InvalidOperationException("Kingdom Inc's Accountant popup is not ready.");
            var rect = TechTreeTabs.Rect("FoundationModPanel", template.transform.parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(560, 490);
            var background = rect.gameObject.AddComponent<Image>();
            var referenceImage = template.GetComponent<Image>() ?? template.GetComponentInChildren<Image>(true);
            if (!referenceImage) { UnityEngine.Object.Destroy(rect.gameObject); throw new InvalidOperationException("Native popup background has no Image."); }
            // Kingdom Inc's own decorative frame sprite and its native
            // Options palette are the authoritative theme. No generic neon UI.
            FoundationGameTheme.ApplyPanel(background);
            bool hasNativeFrame = FoundationGameTheme.AddPanelBorder(rect);

            var host = rect.gameObject.AddComponent<FoundationPanelHost>();
            host.ui = gameUi;
            host.panelRect = rect;
            host.nativeText = template.GetComponentInChildren<TMP_Text>(true);
            host.nativeButton = template.GetComponentInChildren<Button>(true)?.GetComponent<Image>();
            host.nativeDropdown = template.GetComponentInChildren<TMP_Dropdown>(true);
            host.nativeToggle = gameUi.GetComponentsInChildren<Toggle>(true)
                .FirstOrDefault(t => t.GetComponentInChildren<TMP_Text>(true));
            if (!host.nativeText || !host.nativeButton) {
                UnityEngine.Object.Destroy(rect.gameObject);
                throw new InvalidOperationException("Native UI text and button templates are required.");
            }
            host.CreateChrome(rect, hasNativeFrame);
            rect.gameObject.SetActive(false);
            current = host;
            FoundationApi.Signals.Changed += host.OnSignalDefinitionsChanged;
            return host;
        }

        private static void AddFrameEdge(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
            Vector2 offsetMax, Color color)
        {
            var edge = TechTreeTabs.Rect(name, parent);
            edge.anchorMin = anchorMin;
            edge.anchorMax = anchorMax;
            edge.offsetMin = offsetMin;
            edge.offsetMax = offsetMax;
            var graphic = edge.gameObject.AddComponent<Image>();
            graphic.sprite = null;
            graphic.color = color;
            graphic.raycastTarget = false;
        }

        private void CreateChrome(RectTransform root, bool hasNativeFrame)
        {
            // The extracted game's DecoRectBorder sprite is a native nine-slice.
            // Only draw a simple brass edge if that exact game sprite is absent.
            var frameColor = FoundationGameTheme.Brass;
            if (!hasNativeFrame)
            {
                AddFrameEdge(root, "TopBorder", new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(0, -3), Vector2.zero, frameColor);
                AddFrameEdge(root, "BottomBorder", Vector2.zero, new Vector2(1, 0),
                    Vector2.zero, new Vector2(0, 3), frameColor);
                AddFrameEdge(root, "LeftBorder", Vector2.zero, new Vector2(0, 1),
                    Vector2.zero, new Vector2(3, 0), frameColor);
                AddFrameEdge(root, "RightBorder", new Vector2(1, 0), Vector2.one,
                    new Vector2(-3, 0), Vector2.zero, frameColor);
            }
            AddFrameEdge(root, "HeaderDivider", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(16, -66), new Vector2(-16, -64), frameColor);

            var heading = TechTreeTabs.Rect("Heading", root);
            heading.anchorMin = new Vector2(0, 1);
            heading.anchorMax = new Vector2(1, 1);
            heading.pivot = new Vector2(.5f, 1);
            heading.offsetMin = new Vector2(20, -56);
            heading.offsetMax = new Vector2(-62, -12);
            var headingHit = heading.gameObject.AddComponent<Image>();
            headingHit.color = Color.clear;
            headingHit.raycastTarget = true;
            title = CreateNativeText(heading, "Panel", 21, TextAlignmentOptions.MidlineLeft);
            var drag = heading.gameObject.AddComponent<FoundationPanelDrag>();
            drag.Host = this;

            var closeRect = TechTreeTabs.Rect("Close", root);
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-12, -10);
            closeRect.sizeDelta = new Vector2(44, 42);
            var closeImage = closeRect.gameObject.AddComponent<Image>();
            FoundationGameTheme.ApplyButton(closeImage, nativeButton);
            var button = closeRect.gameObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.targetGraphic = closeImage;
            FoundationGameTheme.StyleButton(button);
            button.onClick.AddListener(CloseCurrent);
            CreateNativeText(closeRect, "X", 20, TextAlignmentOptions.Center);

            var viewport = TechTreeTabs.Rect("Viewport", root);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20, 58);
            viewport.offsetMax = new Vector2(-20, -68);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25;
            scroll.viewport = viewport;
            contentScroll = scroll;

            rows = TechTreeTabs.Rect("PanelContent", viewport);
            rows.anchorMin = new Vector2(0, 1);
            rows.anchorMax = new Vector2(1, 1);
            rows.pivot = new Vector2(.5f, 1);
            rows.anchoredPosition = Vector2.zero;
            rows.sizeDelta = Vector2.zero;
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var size = rows.gameObject.AddComponent<ContentSizeFitter>();
            size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rows;

            var footer = TechTreeTabs.Rect("Footer", root);
            footer.anchorMin = new Vector2(0, 0);
            footer.anchorMax = new Vector2(1, 0);
            footer.pivot = new Vector2(.5f, 0);
            footer.offsetMin = new Vector2(20, 10);
            footer.offsetMax = new Vector2(-20, 49);
            status = CreateNativeText(footer, "", 15, TextAlignmentOptions.MidlineLeft);
            status.color = FoundationGameTheme.SecondaryInk;
        }

        private void FitPanelWithinGameCanvas()
        {
            // A modded window must never run off-screen at smaller resolutions
            // or when the player changes the game's UI scale.
            var parent = panelRect ? panelRect.parent as RectTransform : null;
            if (!parent) return;
            float width = parent.rect.width;
            float height = parent.rect.height;
            if (width > 180f && height > 180f)
            {
                panelRect.sizeDelta = new Vector2(Mathf.Min(560f, Mathf.Max(180f, width - 40f)),
                    Mathf.Min(490f, Mathf.Max(180f, height - 40f)));
            }
        }

        private void OpenInternal(FoundationPanelDefinition panel, FlowElement nativeMachine)
        {
            if (gameObject.activeSelf) ui.TogglePopupMenu(this, false, false);
            definition = panel;
            machine = nativeMachine;
            temporaryValues.Clear();
            transientMessage = null;
            defaultMessage = "";
            active = true;
            FitPanelWithinGameCanvas();
            title.text = panel.Title;
            try
            {
                Rebuild();
                panelRect.SetAsLastSibling();
                LoadPosition();
                ui.TogglePopupMenu(this, true);
                // The same host is shared by all panel definitions. Resetting
                // avoids a long search/scroll gallery causing the next machine
                // panel to open scrolled down with its heading clipped.
                ResetScrollToTop();
            }
            catch
            {
                active = false;
                ClearContent();
                definition = null;
                machine = null;
                throw;
            }
        }

        internal void Rebuild()
        {
            if (rendering || !active || definition == null) return;
            if (machine != null && !MachineThread.IsPlaced(machine)) { CloseCurrent(); return; }
            rendering = true;
            try
            {
                ClearContent();
                status.text = transientMessage ?? defaultMessage ?? "";
                transientMessage = null;
                confirmations = false;
                definition.Build(new FoundationPanelBuilder(this, rows));
            }
            catch (Exception ex)
            {
                FoundationApi.Report(new InvalidOperationException("Mod panel failed: " + definition.ModId + "/" + definition.Id, ex));
                ShowMessage("The panel failed to render. See the BepInEx log.");
            }
            finally { rendering = false; }
        }

        private void ResetScrollToTop()
        {
            if (!contentScroll || !rows) return;
            contentScroll.StopMovement();
            rows.anchoredPosition = Vector2.zero;
            // Opening a new panel is infrequent; update layout before setting
            // normalized position so oversized content starts at the top.
            Canvas.ForceUpdateCanvases();
            contentScroll.verticalNormalizedPosition = 1f;
        }

        private void ClearContent()
        {
            foreach (var action in disposers) try { action(); } catch (Exception ex) { FoundationApi.Report(ex); }
            disposers.Clear();
            foreach (var context in contexts) context.Dispose();
            contexts.Clear();
            live.Clear();
            if (rows) for (int i = rows.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(rows.GetChild(i).gameObject);
        }

        internal RectTransform Row(Transform parent, float height, string name = "Control")
        {
            var rect = TechTreeTabs.Rect(name, parent);
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleWidth = 1f;
            return rect;
        }

        internal ModBarContext Context(RectTransform rect)
        {
            var context = new ModBarContext(rect, nativeText, nativeButton, nativeDropdown, nativeToggle, panelTheme: true);
            contexts.Add(context);
            return context;
        }

        internal TMP_Text CreateNativeText(RectTransform parent, string value, float fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var textRect = TechTreeTabs.Rect("Text", parent);
            ModBarContext.Fill(textRect, 4, 2);
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = nativeText.font;
            if (nativeText)
            {
                text.fontSharedMaterial = nativeText.fontSharedMaterial;
                text.color = nativeText.color;
            }
            FoundationGameTheme.ApplyText(text, fontSize);
            text.alignment = alignment;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.text = value ?? "";
            return text;
        }

        internal void ShowMessage(string message)
        {
            transientMessage = message ?? "";
            if (status) status.text = transientMessage;
        }

        internal void Tooltip(string value) { if (status) status.text = value ?? transientMessage ?? ""; }
        internal void AddDisposer(Action dispose) { if (dispose != null) disposers.Add(dispose); }
        internal void AddLive(TMP_Text text, Func<string> read, float interval)
        { live.Add(new LiveLabel(text, read, interval)); }

        internal T Guard<T>(Func<T> call, T fallback)
        {
            try
            {
                if (!active || (machine != null && !MachineThread.IsPlaced(machine))) return fallback;
                return call();
            }
            catch (Exception ex) { FoundationApi.Report(ex); ShowMessage(ex.Message); return fallback; }
        }
        internal void Guard(Action call)
        {
            Guard(() => { call(); return true; }, false);
        }

        internal void AskConfirm(string question, Action accept, Action reject)
        {
            if (confirmations) return;
            ClearContent();
            confirmations = true;
            var view = new FoundationPanelBuilder(this, rows);
            view.Header("Confirm");
            view.Text(question);
            view.Button("Confirm", () => { confirmations = false; Guard(accept); Rebuild(); });
            view.Button("Cancel", () => { confirmations = false; Guard(reject); Rebuild(); });
        }

        private void OnSignalDefinitionsChanged()
        {
            if (active && gameObject.activeInHierarchy && !confirmations) Rebuild();
        }

        private void Update()
        {
            if (!active || machine == null || Time.unscaledTime < nextMachineValidation) return;
            nextMachineValidation = Time.unscaledTime + .25f;
            if (!MachineThread.IsPlaced(machine)) { CloseCurrent(); return; }
        }

        private void LateUpdate()
        {
            if (!active || confirmations || live.Count == 0 || Time.unscaledTime < nextLive) return;
            nextLive = Time.unscaledTime + 0.15f;
            foreach (var label in live)
            {
                if (!label.Label || Time.unscaledTime < label.Next) continue;
                label.Next = Time.unscaledTime + label.Interval;
                label.Label.text = Guard(label.Reader, "(unavailable)");
            }
        }

        /// <summary>Performs the popup closed operation.</summary>
        public override void PopupClosed()
        {
            active = false;
            ClearContent();
            machine = null;
            definition = null;
            temporaryValues.Clear();
            base.PopupClosed();
        }

        internal static void CloseCurrent()
        {
            if (current && current.active && current.ui)
                current.ui.TogglePopupMenu(current, false);
        }

        internal static void CloseIfShowing(FoundationPanelDefinition panel)
        {
            if (current && ReferenceEquals(current.definition, panel)) CloseCurrent();
        }

        internal static void ResetAll()
        {
            if (!current) return;
            CloseCurrent();
            UnityEngine.Object.Destroy(current.gameObject);
            current = null;
        }

        private void OnDestroy()
        {
            FoundationApi.Signals.Changed -= OnSignalDefinitionsChanged;
            ClearContent();
            if (current == this) current = null;
        }

        internal string Temp(string key) => temporaryValues.TryGetValue(key, out var value) ? value : null;
        internal void SetTemp(string key, string value)
        {
            if (value == null) temporaryValues.Remove(key);
            else temporaryValues[key] = value;
        }

        internal int LoadTab(string key, int fallback) => UiPanelState.ReadInt(definition.ModId, definition.Id, key, fallback);
        internal void SaveTab(string key, int tab) => UiPanelState.WriteInt(definition.ModId, definition.Id, key, tab);
        internal bool LoadSection(string key, bool fallback) => UiPanelState.ReadInt(definition.ModId, definition.Id, key, fallback ? 1 : 0) != 0;
        internal void SaveSection(string key, bool expanded) => UiPanelState.WriteInt(definition.ModId, definition.Id, key, expanded ? 1 : 0);
        internal bool CanDrag => definition != null && definition.AllowDrag;
        internal void MoveBy(PointerEventData data)
        {
            if (!CanDrag || !panelRect || !panelRect.parent) return;
            var parent = (RectTransform)panelRect.parent;
            Vector2 delta = data.delta;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas && canvas.scaleFactor > 0.001f) delta /= canvas.scaleFactor;
            SetClampedPosition(panelRect.anchoredPosition + delta);
        }
        internal void SavePosition()
        {
            if (!CanDrag || definition == null) return;
            UiPanelState.WriteFloat(definition.ModId, definition.Id, "positionX", panelRect.anchoredPosition.x);
            UiPanelState.WriteFloat(definition.ModId, definition.Id, "positionY", panelRect.anchoredPosition.y);
        }
        private void LoadPosition()
        {
            if (!CanDrag || definition == null) { panelRect.anchoredPosition = Vector2.zero; return; }
            var requested = new Vector2(UiPanelState.ReadFloat(definition.ModId, definition.Id, "positionX", 0),
                UiPanelState.ReadFloat(definition.ModId, definition.Id, "positionY", 0));
            SetClampedPosition(requested);
        }

        private sealed class LiveLabel
        {
            internal readonly TMP_Text Label;
            internal readonly Func<string> Reader;
            internal readonly float Interval;
            internal float Next;
            internal LiveLabel(TMP_Text label, Func<string> reader, float interval)
            { Label = label; Reader = reader; Interval = interval; }
        }

        private void SetClampedPosition(Vector2 requested)
        {
            var parent = panelRect.parent as RectTransform;
            if (!parent) return;
            var maxX = Mathf.Max(0, (parent.rect.width - panelRect.rect.width) * .5f);
            var maxY = Mathf.Max(0, (parent.rect.height - panelRect.rect.height) * .5f);
            panelRect.anchoredPosition = new Vector2(Mathf.Clamp(requested.x, -maxX, maxX),
                Mathf.Clamp(requested.y, -maxY, maxY));
        }

    }

    internal sealed class FoundationPanelDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal FoundationPanelHost Host;
        /// <summary>Performs the on begin drag operation.</summary>
        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data) { if (Host) Host.MoveBy(data); }
        public void OnEndDrag(PointerEventData data) { if (Host) Host.SavePosition(); }
    }
}
