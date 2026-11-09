using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomFoundation
{
    internal sealed class FoundationPanelBuilder : IFoundationPanelBuilder
    {
        private readonly FoundationPanelHost host;
        private readonly Transform root;
        private int ordinal;
        public FlowElement Machine => host.Machine;
        public string PanelId => host.PanelId;

        internal FoundationPanelBuilder(FoundationPanelHost host, Transform root)
        {
            this.host = host;
            this.root = root;
        }

        private string Key(string prefix, string title) => prefix + ":" + (title ?? "") + ":" + (ordinal++).ToString(CultureInfo.InvariantCulture);
        private RectTransform Row(float height, string name = "Row") => host.Row(root, height, name);
        private void Safe(Action handler) => host.Guard(handler);

        public void Text(string value)
        {
            // Native pixel font remains legible at UI scale. Reserve enough
            // height for informative copy instead of drawing the next row on
            // top of it. Short labels stay one line; long copy wraps to 3.
            string body = value ?? "";
            int lines = Mathf.Clamp(Mathf.CeilToInt(body.Length / 52f), 1, 3);
            var row = Row(32f + (lines - 1) * 23f, "Text");
            var label = host.Context(row).CreateText(body);
            if (lines > 1)
            {
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        public void Header(string value)
        {
            var row = Row(44, "Header");
            var label = host.Context(row).CreateText(value ?? "");
            label.fontSizeMax = 21;
            label.fontSize = 21;
            label.fontStyle = FontStyles.Bold;
        }

        public void Button(string title, Action clicked, string tooltip = null)
        {
            if (clicked == null) throw new ArgumentNullException(nameof(clicked));
            var row = Row(46, "Button");
            var button = host.Context(row).CreateButton(title, () => Safe(clicked));
            if (!string.IsNullOrEmpty(tooltip)) Tooltip(button.gameObject, tooltip);
        }

        public void Toggle(string title, bool value, Action<bool> changed, string tooltip = null)
        {
            if (changed == null) throw new ArgumentNullException(nameof(changed));
            var row = Row(46, "Toggle");
            var toggle = host.Context(row).CreateToggle(title, value, next => Safe(() => changed(next)));
            if (!string.IsNullOrEmpty(tooltip)) Tooltip(toggle.gameObject, tooltip);
        }

        public void Slider(string title, float min, float max, float value, float step, Action<float> changed)
        {
            if (changed == null) throw new ArgumentNullException(nameof(changed));
            if (float.IsNaN(min) || float.IsInfinity(min) || float.IsNaN(max) || float.IsInfinity(max) || min >= max)
                throw new ArgumentOutOfRangeException(nameof(min), "Slider min must be less than max.");
            if (step <= 0 || float.IsNaN(step) || float.IsInfinity(step)) throw new ArgumentOutOfRangeException(nameof(step));
            Text(title);
            var row = Row(38, "Slider");
            var sliderRoot = TechTreeTabs.Rect("Track", row);
            sliderRoot.anchorMin = new Vector2(0, 0);
            sliderRoot.anchorMax = new Vector2(.79f, 1);
            sliderRoot.offsetMin = new Vector2(8, 4);
            sliderRoot.offsetMax = new Vector2(-6, -4);
            var background = sliderRoot.gameObject.AddComponent<Image>();
            FoundationGameTheme.ApplyWell(background);
            var fill = TechTreeTabs.Rect("Fill", sliderRoot);
            fill.anchorMin = new Vector2(0, .25f);
            fill.anchorMax = new Vector2(1, .75f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = null;
            fillImage.type = Image.Type.Simple;
            fillImage.color = FoundationGameTheme.Brass;
            fillImage.raycastTarget = false;
            var handle = TechTreeTabs.Rect("Handle", sliderRoot);
            handle.anchorMin = handle.anchorMax = new Vector2(.5f, .5f);
            handle.sizeDelta = new Vector2(25, 34);
            var handleImage = handle.gameObject.AddComponent<Image>();
            FoundationGameTheme.ApplySliderHandle(handleImage, host.NativeButton);
            var slider = sliderRoot.gameObject.AddComponent<Slider>();
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.targetGraphic = handleImage;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = step >= 1 && Mathf.Approximately(step, Mathf.Round(step));
            slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
            var captionRoot = TechTreeTabs.Rect("Value", row);
            captionRoot.anchorMin = new Vector2(.8f, 0);
            captionRoot.anchorMax = Vector2.one;
            captionRoot.offsetMin = captionRoot.offsetMax = Vector2.zero;
            var caption = host.CreateNativeText(captionRoot, value.ToString("0.##", CultureInfo.InvariantCulture), 15);
            slider.onValueChanged.AddListener(raw => Safe(() =>
            {
                var quantized = Mathf.Clamp(min + Mathf.Round((raw - min) / step) * step, min, max);
                caption.text = quantized.ToString("0.##", CultureInfo.InvariantCulture);
                changed(quantized);
            }));
            host.AddDisposer(() => { if (slider) slider.onValueChanged.RemoveAllListeners(); });
        }

        public void Number(string title, double value, double min, double max, double step, Action<double> changed)
        {
            if (changed == null) throw new ArgumentNullException(nameof(changed));
            if (double.IsNaN(min) || double.IsInfinity(min) || double.IsNaN(max) || double.IsInfinity(max) || min > max)
                throw new ArgumentOutOfRangeException(nameof(min));
            if (step <= 0 || double.IsNaN(step) || double.IsInfinity(step)) throw new ArgumentOutOfRangeException(nameof(step));
            Text(title);
            AddInput(value.ToString("0.########", CultureInfo.InvariantCulture), 36, 64, final =>
            {
                if (!double.TryParse(final, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    || double.IsInfinity(parsed) || double.IsNaN(parsed))
                {
                    host.ShowMessage("Enter a valid number for " + title + ".");
                    return;
                }
                parsed = Math.Max(min, Math.Min(max, min + Math.Round((parsed - min) / step) * step));
                Safe(() => changed(parsed));
            });
        }

        public void Input(string title, string value, Action<string> changed, int maxLength = 256)
        {
            if (changed == null) throw new ArgumentNullException(nameof(changed));
            if (maxLength < 1 || maxLength > 8192) throw new ArgumentOutOfRangeException(nameof(maxLength));
            Text(title);
            AddInput(value ?? "", 42, maxLength, final => Safe(() => changed(final)));
        }

        private TMP_InputField AddInput(string value, float height, int limit, Action<string> changed)
        {
            var row = Row(height, "Input");
            var rect = TechTreeTabs.Rect("NativeInput", row);
            ModBarContext.Fill(rect, 7, 2);
            var background = rect.gameObject.AddComponent<Image>();
            FoundationGameTheme.ApplyWell(background);
            var viewport = TechTreeTabs.Rect("TextViewport", rect);
            ModBarContext.Fill(viewport, 13, 5);
            viewport.gameObject.AddComponent<RectMask2D>();
            var inputText = host.CreateNativeText(viewport, "", 16);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            inputText.overflowMode = TextOverflowModes.Overflow;
            inputText.enableAutoSizing = false;
            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textViewport = viewport;
            input.textComponent = inputText;
            input.characterLimit = limit;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            input.SetTextWithoutNotify(value);
            input.onEndEdit.AddListener(s => changed?.Invoke(s));
            host.AddDisposer(() => { if (input) input.onEndEdit.RemoveAllListeners(); });
            return input;
        }

        public void Dropdown(string title, IReadOnlyList<string> items, int selected, Action<int> changed)
        {
            if (changed == null) throw new ArgumentNullException(nameof(changed));
            if (items == null || items.Count == 0 || items.Any(item => item == null))
                throw new ArgumentException("Dropdown needs at least one non-null entry.", nameof(items));
            if (selected < 0 || selected >= items.Count) throw new ArgumentOutOfRangeException(nameof(selected));
            Text(title);
            var row = Row(48, "Dropdown");
            host.Context(row).CreateDropdown(items, selected, index => Safe(() => changed(index)));
        }

        public void Search(string title, IReadOnlyList<string> items, int selected, Action<int> changed)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (changed == null) throw new ArgumentNullException(nameof(changed));
            Text(title);
            var search = AddInput("", 38, 128, _ => { });
            const int pageSize = 24;
            var buttonRows = new GameObject[pageSize];
            var captions = new TMP_Text[pageSize];
            var indexes = new int[pageSize];
            var matches = new List<int>();
            var page = 0;
            for (int i = 0; i < pageSize; i++)
            {
                int slot = i;
                var row = Row(38, "SearchResult");
                var button = host.Context(row).CreateButton("", () =>
                {
                    int index = indexes[slot];
                    if (index >= 0) Safe(() => changed(index));
                });
                captions[i] = button.GetComponentInChildren<TMP_Text>(true);
                buttonRows[i] = row.gameObject;
                indexes[i] = -1;
            }
            var footer = Row(38, "SearchPages");
            var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            var previous = TechTreeTabs.Rect("Previous", footer);
            var next = TechTreeTabs.Rect("Next", footer);
            // The update local function below captures these controls. Initialize them
            // before creating the button callbacks so C# definite-assignment analysis
            // accepts references through the captured local function (CS0165).
            UnityEngine.UI.Button previousButton = null;
            UnityEngine.UI.Button nextButton = null;
            TMP_Text caption = null;
            previousButton = host.Context(previous).CreateButton("< Previous", () => { page = Math.Max(0, page - 1); update(); });
            nextButton = host.Context(next).CreateButton("Next >", () => { page++; update(); });
            var status = Row(26, "SearchStatus");
            caption = host.Context(status).CreateText("");

            // Reuse a small fixed set of native buttons while filtering the entire
            // catalog. No truncation at 500 items and no huge Unity hierarchy.
            void update()
            {
                var query = search.text ?? "";
                matches.Clear();
                for (int i = 0; i < items.Count; i++)
                    if (items[i] != null && items[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        matches.Add(i);
                int maxPage = Math.Max(0, (matches.Count - 1) / pageSize);
                page = Math.Min(page, maxPage);
                for (int i = 0; i < pageSize; i++)
                {
                    int m = page * pageSize + i;
                    bool visible = m < matches.Count;
                    indexes[i] = visible ? matches[m] : -1;
                    if (buttonRows[i]) buttonRows[i].SetActive(visible);
                    if (visible && captions[i])
                        captions[i].text = (indexes[i] == selected ? "[" : "") + items[indexes[i]] + (indexes[i] == selected ? "]" : "");
                }
                previousButton.interactable = page > 0;
                nextButton.interactable = page < maxPage;
                caption.text = matches.Count + " matching entries  -  page " + (page + 1) + " / " + (maxPage + 1);
            }
            search.onValueChanged.AddListener(_ => { page = 0; update(); });
            host.AddDisposer(() => { if (search) search.onValueChanged.RemoveAllListeners(); });
            update();
        }

        public void Colors(string title, IReadOnlyList<Color> colors, int selected, Action<int> changed)
        {
            if (colors == null || colors.Count == 0) throw new ArgumentException("Color choices are required.", nameof(colors));
            if (changed == null) throw new ArgumentNullException(nameof(changed));
            Text(title);
            var row = Row(52, "ColorSwatches");
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            for (int i = 0; i < colors.Count; i++)
            {
                int index = i;
                var swatch = TechTreeTabs.Rect("Color" + i.ToString(CultureInfo.InvariantCulture), row);
                swatch.gameObject.AddComponent<LayoutElement>().minWidth = 28;
                var background = swatch.gameObject.AddComponent<Image>();
                background.color = colors[i];
                FoundationGameTheme.FrameColorSwatch(swatch);
                var button = swatch.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                if (selected == i)
                {
                    var outline = swatch.gameObject.AddComponent<Outline>();
                    outline.effectColor = FoundationGameTheme.Brass;
                    outline.effectDistance = new Vector2(2, -2);
                }
                button.onClick.AddListener(() => Safe(() => changed(index)));
                host.AddDisposer(() => { if (button) button.onClick.RemoveAllListeners(); });
            }
        }

        public void Signal(string title, SignalSelection initial, Action<SignalSelection> changed)
        {
            if (changed == null) throw new ArgumentNullException(nameof(changed));
            var key = Key("signal", title);
            var sources = FoundationApi.Signals.Sources;
            var sourceIds = new List<string> { null };
            var sourceLabels = new List<string> { "(No source)" };
            // Display friendly native-style names. Long globally unique source IDs
            // belong in the selection value, not in the dropdown caption: the
            // full ID previously wrapped onto 2+ lines and obscured the next
            // property selector. Include owner only when names collide.
            var duplicateNames = sources.GroupBy(item => item.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            foreach (var source in sources)
            {
                sourceIds.Add(source.Id);
                sourceLabels.Add(duplicateNames[source.Name] > 1
                    ? source.Name + " (" + source.ModId + ")" : source.Name);
            }
            var selectedSource = host.Temp(key) ?? initial?.SourceId;
            if (!sourceIds.Contains(selectedSource)) selectedSource = null;
            var sourceIndex = sourceIds.IndexOf(selectedSource);
            Dropdown(title + " / Source", sourceLabels, sourceIndex, next =>
            {
                host.SetTemp(key, sourceIds[next]);
                changed(new SignalSelection(sourceIds[next], null));
                host.Rebuild();
            });
            var properties = new List<string> { null };
            var labels = new List<string> { "(No property)" };
            if (selectedSource != null && FoundationApi.Signals.TryGet(selectedSource, out var sourceObj))
                foreach (var property in sourceObj.Properties)
                {
                    properties.Add(property.Id);
                    labels.Add(property.Name);
                }
            if (properties.Count > 1)
            {
                var propIndex = properties.IndexOf(initial?.PropertyId);
                if (propIndex < 0) propIndex = 0;
                Dropdown(title + " / Exposed property", labels, propIndex,
                    next => changed(new SignalSelection(selectedSource, properties[next])));
            }
            else Text("No exposed properties available for this source.");
        }

        public void Tabs(string key, IReadOnlyList<string> titles, Action<int, IFoundationPanelBuilder> build)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Stable tab key required.", nameof(key));
            if (titles == null || titles.Count == 0) throw new ArgumentException("Tab names required.", nameof(titles));
            if (build == null) throw new ArgumentNullException(nameof(build));
            var selected = Mathf.Clamp(host.LoadTab("tab:" + key, 0), 0, titles.Count - 1);
            var bar = Row(46, "Tabs");
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            for (int i = 0; i < titles.Count; i++)
            {
                int index = i;
                var tabSlot = TechTreeTabs.Rect("Tab", bar);
                host.Context(tabSlot).CreateButton((i == selected ? "[" + titles[i] + "]" : titles[i]), () =>
                {
                    host.SaveTab("tab:" + key, index);
                    host.Rebuild();
                });
            }
            build(selected, this);
        }

        public void Columns(int count, Action<int, IFoundationPanelBuilder> build)
        {
            if (count < 2 || count > 4) throw new ArgumentOutOfRangeException(nameof(count), "Use 2, 3 or 4 columns.");
            if (build == null) throw new ArgumentNullException(nameof(build));
            var row = Row(50, "Columns");
            var panel = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            panel.spacing = 8;
            panel.childControlWidth = true;
            panel.childForceExpandWidth = true;
            var maxHeight = 40f;
            for (int i = 0; i < count; i++)
            {
                var column = TechTreeTabs.Rect("Column" + i, row);
                var vertical = column.gameObject.AddComponent<VerticalLayoutGroup>();
                vertical.spacing = 6;
                vertical.childControlWidth = true;
                vertical.childForceExpandWidth = true;
                vertical.childControlHeight = true;
                vertical.childForceExpandHeight = false;
                build(i, new FoundationPanelBuilder(host, column));
                float height = 0;
                foreach (Transform child in column)
                {
                    var element = child.GetComponent<LayoutElement>();
                    if (element) height += element.preferredHeight + vertical.spacing;
                }
                maxHeight = Mathf.Max(maxHeight, height + 4);
            }
            var layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = maxHeight;
            layout.minHeight = maxHeight;
        }

        public void Reorder(string title, IReadOnlyList<string> items, Action<int, int> onMove)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (onMove == null) throw new ArgumentNullException(nameof(onMove));
            Header(title);
            var group = TechTreeTabs.Rect("ReorderGroup", root);
            var groupLayout = group.gameObject.AddComponent<VerticalLayoutGroup>();
            groupLayout.spacing = 5;
            groupLayout.childControlWidth = true;
            groupLayout.childForceExpandWidth = true;
            groupLayout.childControlHeight = true;
            groupLayout.childForceExpandHeight = false;
            var size = group.gameObject.AddComponent<ContentSizeFitter>();
            size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int index = 0; index < items.Count; index++)
            {
                var from = index;
                var row = host.Row(group, 42, "ReorderRow");
                var image = row.gameObject.AddComponent<Image>();
                FoundationGameTheme.ApplyWell(image);
                var text = host.CreateNativeText(row, "Drag: " + (items[index] ?? "") + "  (" + (index + 1) + ")", 17);
                var handler = row.gameObject.AddComponent<FoundationPanelReorder>();
                handler.Index = index;
                handler.Container = group;
                handler.Move = (src, dst) => Safe(() => { onMove(src, dst); host.Rebuild(); });
            }
            if (items.Count == 0) Text("Nothing to reorder.");
        }

        public void Section(string key, string title, Action<IFoundationPanelBuilder> build, bool initiallyExpanded = true)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Stable section key required.", nameof(key));
            if (build == null) throw new ArgumentNullException(nameof(build));
            var expanded = host.LoadSection("section:" + key, initiallyExpanded);
            Button((expanded ? "v " : "> ") + title, () =>
            {
                host.SaveSection("section:" + key, !expanded);
                host.Rebuild();
            });
            if (expanded) build(this);
        }

        public void Confirm(string question, Action accepted, Action rejected = null)
        {
            if (accepted == null) throw new ArgumentNullException(nameof(accepted));
            Button("Confirm: " + question, () => host.AskConfirm(question, accepted, rejected));
        }

        public void Property(FoundationUiProperty property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            var current = host.Guard(property.Read, (object)null);
            if (property.Write == null)
            {
                Text(property.Title + ": " + (current?.ToString() ?? "(null)"));
                return;
            }
            if (property.ValueType == typeof(bool))
                Toggle(property.Title, current is bool b && b, next => host.Guard(() => property.Write(next)));
            else if (property.ValueType == typeof(string))
            {
                if (property.Options != null && property.Options.Count > 0)
                {
                    var index = -1;
                    for (int i = 0; i < property.Options.Count; i++)
                        if (string.Equals(property.Options[i], current as string, StringComparison.Ordinal)) { index = i; break; }
                    Dropdown(property.Title, property.Options, Math.Max(0, index),
                        next => host.Guard(() => property.Write(property.Options[next])));
                }
                else Input(property.Title, current as string ?? "", next => host.Guard(() => property.Write(next)));
            }
            else if (property.ValueType == typeof(SignalSelection))
                Signal(property.Title, current as SignalSelection, next => host.Guard(() => property.Write(next)));
            else
            {
                double numeric = 0;
                if (current != null) numeric = Convert.ToDouble(current, CultureInfo.InvariantCulture);
                Number(property.Title, numeric, property.Minimum, property.Maximum, property.Step, next => host.Guard(() =>
                {
                    object converted = property.ValueType == typeof(int) ? (object)Convert.ToInt32(next) :
                        property.ValueType == typeof(float) ? (object)(float)next : next;
                    property.Write(converted);
                }));
            }
        }

        public void LiveText(string title, Func<string> read, float refreshSeconds = .4f)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            if (float.IsNaN(refreshSeconds) || refreshSeconds < .1f || refreshSeconds > 60f)
                throw new ArgumentOutOfRangeException(nameof(refreshSeconds));
            var row = Row(34, "LiveValue");
            var label = host.Context(row).CreateText(title + ": " + host.Guard(read, "(unavailable)"));
            host.AddLive(label, () => title + ": " + read(), refreshSeconds);
        }

        public void Rebuild() => host.Rebuild();
        public void Message(string value) => host.ShowMessage(value);
        private void Tooltip(GameObject target, string text)
        {
            var hover = target.AddComponent<FoundationPanelTooltip>();
            hover.Host = host;
            hover.Message = text;
        }
    }

    internal sealed class FoundationPanelTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal FoundationPanelHost Host;
        internal string Message;
        public void OnPointerEnter(PointerEventData data) { if (Host) Host.Tooltip(Message); }
        public void OnPointerExit(PointerEventData data) { if (Host) Host.Tooltip(null); }
    }
}
