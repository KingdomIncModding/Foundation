using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.MainMenu;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    public sealed class ModConfigTemplates : IModConfigTemplates
    {
        readonly TMP_Dropdown dropdownTemplate;
        readonly Toggle toggleTemplate;
        readonly Slider sliderTemplate;
        readonly Button buttonTemplate;
        readonly TMP_Text labelTemplate;
        readonly TMP_Text valueTemplate;
        public ModConfigTemplates(OptionsUI source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            dropdownTemplate = (TMP_Dropdown)AccessTools.Field(typeof(OptionsUI), "_resolutionsDropdown").GetValue(source);
            toggleTemplate = (Toggle)AccessTools.Field(typeof(OptionsUI), "_windowModeToggle").GetValue(source);
            sliderTemplate = (Slider)AccessTools.Field(typeof(OptionsUI), "_soundSlider").GetValue(source) ?? (Slider)AccessTools.Field(typeof(OptionsUI), "_musicSlider").GetValue(source);
            valueTemplate = (TMP_Text)AccessTools.Field(typeof(OptionsUI), "_fpsDisplay").GetValue(source) ?? (TMP_Text)AccessTools.Field(typeof(OptionsUI), "_autosaveDisplay").GetValue(source);
            // Find an Apply or Cancel button from Options children
            buttonTemplate = source.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "Apply" || b.name == "Cancel" || b.name == "KeyBindings") ?? source.GetComponentInChildren<Button>(true);
            // Find a row label
            labelTemplate = source.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Text" || t.name == "Label") ?? valueTemplate;
        }

        public GameObject CreateEmptyRow(Transform parent, float height = 40f)
        {
            var row = new GameObject("ModConfigRow", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, height);
            var layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            layout.flexibleWidth = 1f;
            return row;
        }

        TMP_Text CreateLabel(Transform row, string text)
        {
            var labelObj = new GameObject("RowLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(row, false);
            var rect = (RectTransform)labelObj.transform;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.42f, 1);
            rect.offsetMin = new Vector2(8, 0);
            rect.offsetMax = new Vector2(-8, 0);
            var tmp = labelObj.GetComponent<TextMeshProUGUI>();
            if (labelTemplate != null)
            {
                tmp.font = labelTemplate.font;
                tmp.fontSharedMaterial = labelTemplate.fontSharedMaterial;
                tmp.color = labelTemplate.color;
            }

            tmp.fontSize = 17;
            tmp.fontSizeMin = 12;
            tmp.fontSizeMax = 18;
            tmp.enableAutoSizing = true;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.text = text ?? "";
            return tmp;
        }

        public GameObject CreateButtonRow(Transform parent, string label, string buttonText, Action onClick, out Button button, out TMP_Text buttonLabel)
        {
            var row = CreateEmptyRow(parent, 52f);
            if (!string.IsNullOrEmpty(label))
                CreateLabel(row.transform, label);
            var buttonObj = global::UnityEngine.Object.Instantiate(buttonTemplate.gameObject, row.transform);
            buttonObj.name = "ConfigButton";
            button = buttonObj.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                UISounds.TurnPage();
                onClick?.Invoke();
            });
            var rect = (RectTransform)buttonObj.transform;
            if (string.IsNullOrEmpty(label))
            {
                rect.anchorMin = new Vector2(0.10f, 0.04f);
                rect.anchorMax = new Vector2(0.90f, 0.96f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.45f, 0.04f);
                rect.anchorMax = new Vector2(0.98f, 0.96f);
            }

            rect.offsetMin = rect.offsetMax = Vector2.zero;
            buttonLabel = buttonObj.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.text = buttonText ?? label ?? "Click";
                buttonLabel.alignment = TextAlignmentOptions.Center; // Centered vertically & horizontally
                buttonLabel.fontSizeMin = 13;
                buttonLabel.fontSizeMax = 18;
                buttonLabel.enableAutoSizing = true;
                var blRect = buttonLabel.rectTransform;
                blRect.anchorMin = Vector2.zero;
                blRect.anchorMax = Vector2.one;
                blRect.offsetMin = blRect.offsetMax = Vector2.zero;
            }

            return row;
        }

        public GameObject CreateToggleRow(Transform parent, string label, bool initialValue, Action<bool> onChanged, out Toggle toggle)
        {
            var row = CreateEmptyRow(parent, 46f);
            CreateLabel(row.transform, label);
            var toggleObj = global::UnityEngine.Object.Instantiate(toggleTemplate.gameObject, row.transform);
            toggleObj.name = "ConfigToggle";
            toggle = toggleObj.GetComponent<Toggle>();
            toggle.group = null;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            // Remove any labels attached directly inside the cloned toggle
            foreach (var t in toggleObj.GetComponentsInChildren<TMP_Text>(true))
                global::UnityEngine.Object.Destroy(t.gameObject);
            var rect = (RectTransform)toggleObj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.48f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(32, 32);
            rect.anchoredPosition = Vector2.zero;
            // The vanilla Options checkbox's child graphic has a fixed tiny
            // prefab size. Enlarging only the Toggle root leaves it 10px wide.
            // Scale the native checked/unchecked art itself, keeping its sprite.
            var checkbox = toggle.targetGraphic ? toggle.targetGraphic.rectTransform : null;
            if (checkbox != null && checkbox != rect)
            {
                checkbox.anchorMin = checkbox.anchorMax = new Vector2(.5f, .5f);
                checkbox.pivot = new Vector2(.5f, .5f);
                checkbox.anchoredPosition = Vector2.zero;
                checkbox.sizeDelta = new Vector2(26f, 26f);
            }

            var checkmark = toggle.graphic ? toggle.graphic.rectTransform : null;
            if (checkmark != null && checkmark != rect && checkmark != checkbox)
            {
                checkmark.anchorMin = checkmark.anchorMax = new Vector2(.5f, .5f);
                checkmark.pivot = new Vector2(.5f, .5f);
                checkmark.anchoredPosition = Vector2.zero;
                checkmark.sizeDelta = new Vector2(20f, 20f);
            }

            toggle.SetIsOnWithoutNotify(initialValue);
            toggle.onValueChanged.AddListener(val =>
            {
                UISounds.CraftStep();
                onChanged?.Invoke(val);
            });
            return row;
        }

        public GameObject CreateSliderRow(Transform parent, string label, float min, float max, float initialValue, float step, Action<float> onChanged, Func<float, string> formatValue, out Slider slider, out TMP_Text valueText)
        {
            var row = CreateEmptyRow(parent, 48f);
            CreateLabel(row.transform, label);
            var sliderObj = global::UnityEngine.Object.Instantiate(sliderTemplate.gameObject, row.transform);
            sliderObj.name = "ConfigSlider";
            slider = sliderObj.GetComponent<Slider>();
            slider.onValueChanged = new Slider.SliderEvent();
            slider.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = step >= 1f && Mathf.Approximately(step, Mathf.Round(step));
            // Remove any attached sound listeners from vanilla
            var soundSlider = sliderObj.GetComponent<OptionsSoundSlider>();
            if (soundSlider != null)
                global::UnityEngine.Object.Destroy(soundSlider);
            var sRect = (RectTransform)sliderObj.transform;
            sRect.anchorMin = new Vector2(0.45f, 0);
            sRect.anchorMax = new Vector2(0.82f, 1);
            sRect.offsetMin = new Vector2(0, 8);
            sRect.offsetMax = new Vector2(0, -8);
            // Value label
            var vObj = new GameObject("SliderValue", typeof(RectTransform), typeof(TextMeshProUGUI));
            vObj.transform.SetParent(row.transform, false);
            var vRect = (RectTransform)vObj.transform;
            vRect.anchorMin = new Vector2(0.84f, 0);
            vRect.anchorMax = new Vector2(0.99f, 1);
            vRect.offsetMin = vRect.offsetMax = Vector2.zero;
            valueText = vObj.GetComponent<TextMeshProUGUI>();
            if (valueTemplate != null)
            {
                valueText.font = valueTemplate.font;
                valueText.fontSharedMaterial = valueTemplate.fontSharedMaterial;
                valueText.color = valueTemplate.color;
            }

            valueText.fontSize = 16;
            valueText.alignment = TextAlignmentOptions.MidlineLeft;
            valueText.raycastTarget = false;
            bool isWhole = slider.wholeNumbers;
            Func<float, string> formatter = formatValue != null ? formatValue : new Func<float, string>(v => isWhole ? Mathf.RoundToInt(v).ToString() : v.ToString("0.##"));
            float currentVal = initialValue;
            slider.SetValueWithoutNotify(currentVal);
            valueText.text = formatter(currentVal);
            TMP_Text vText = valueText;
            slider.onValueChanged.AddListener(raw =>
            {
                float finalVal = raw;
                if (step > 0f)
                {
                    finalVal = Mathf.Round(raw / step) * step;
                    finalVal = Mathf.Clamp(finalVal, min, max);
                }

                vText.text = formatter(finalVal);
                onChanged?.Invoke(finalVal);
            });
            return row;
        }

        public GameObject CreateDropdownRow(Transform parent, string label, IReadOnlyList<string> options, int initialIndex, Action<int> onChanged, out TMP_Dropdown dropdown)
        {
            var row = CreateEmptyRow(parent, 52f);
            CreateLabel(row.transform, label);
            var dropObj = global::UnityEngine.Object.Instantiate(dropdownTemplate.gameObject, row.transform);
            dropObj.name = "ConfigDropdown";
            dropdown = dropObj.GetComponent<TMP_Dropdown>();
            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            dropdown.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            var rect = (RectTransform)dropObj.transform;
            rect.anchorMin = new Vector2(0.45f, 0.04f);
            rect.anchorMax = new Vector2(0.98f, 0.96f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            dropdown.ClearOptions();
            var list = new List<string>();
            if (options != null)
                list.AddRange(options);
            dropdown.AddOptions(list);
            if (dropdown.template != null)
            {
                dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, Mathf.Min(300f, Mathf.Max(120f, list.Count * 38f + 16f)));
            }

            dropdown.SetValueWithoutNotify(Mathf.Clamp(initialIndex, 0, Math.Max(0, list.Count - 1)));
            dropdown.RefreshShownValue();
            if (dropdown.captionText)
            {
                // Mod settings use a 52-pixel-tall native Options row. The
                // caption must stay on one line: wrapping overlaps the row
                // below and the native dropdown arrow (seen in the UI smoke).
                dropdown.captionText.alignment = TextAlignmentOptions.MidlineLeft;
                dropdown.captionText.textWrappingMode = TextWrappingModes.NoWrap;
                dropdown.captionText.overflowMode = TextOverflowModes.Ellipsis;
                dropdown.captionText.enableAutoSizing = true;
                dropdown.captionText.fontSizeMin = 12f;
                dropdown.captionText.fontSizeMax = 17f;
            }

            if (dropdown.itemText)
            {
                // Keep expanded choices readable without disturbing their
                // native background, pixel font or selected-state graphics.
                dropdown.itemText.textWrappingMode = TextWrappingModes.NoWrap;
                dropdown.itemText.overflowMode = TextOverflowModes.Ellipsis;
            }

            dropdown.onValueChanged.AddListener(idx =>
            {
                UISounds.TurnPage();
                onChanged?.Invoke(idx);
            });
            return row;
        }

        public GameObject CreateInputRow(Transform parent, string label, string currentValue, int maxLength, Action<string> onEdited, out TMP_InputField input)
        {
            if (maxLength < 1 || maxLength > 8192)
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            var row = CreateEmptyRow(parent, 54f);
            CreateLabel(row.transform, label);
            // Text inputs are recessed native Options wells, NOT bevelled action
            // buttons. Reusing Apply/Cancel button art made the value appear
            // inside a pointed button with the first glyph hitting its gold cap.
            var field = new GameObject("ConfigInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            field.transform.SetParent(row.transform, false);
            var rect = (RectTransform)field.transform;
            rect.anchorMin = new Vector2(.45f, .06f);
            rect.anchorMax = new Vector2(.98f, .94f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = field.GetComponent<Image>();
            FoundationGameTheme.ApplyWell(image);
            var viewport = new GameObject("TextViewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(rect, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            // Keep the pixel-font glyphs away from the native inset frame.
            // This margin is deliberately larger than the border's corner art.
            viewportRect.offsetMin = new Vector2(20, 5);
            viewportRect.offsetMax = new Vector2(-20, -5);
            var value = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            value.transform.SetParent(viewport.transform, false);
            var valueRect = (RectTransform)value.transform;
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = valueRect.offsetMax = Vector2.zero;
            var text = value.GetComponent<TextMeshProUGUI>();
            if (labelTemplate != null)
            {
                text.font = labelTemplate.font;
                text.fontSharedMaterial = labelTemplate.fontSharedMaterial;
                text.color = labelTemplate.color;
            }

            text.fontSize = 16;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            // TMP_InputField owns horizontal scrolling; RectMask2D clips the
            // text viewport so typing never draws over the framed side caps.
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            input = field.GetComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.textViewport = viewportRect;
            input.textComponent = text;
            input.characterLimit = maxLength;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            input.caretWidth = 2;
            input.SetTextWithoutNotify(currentValue ?? "");
            input.onValueChanged.AddListener(next => onEdited?.Invoke(next));
            return row;
        }

        public GameObject CreateTextRow(Transform parent, string text, bool isHeader, out TMP_Text textComponent)
        {
            float height = isHeader ? 44f : 34f;
            var row = CreateEmptyRow(parent, height);
            var textObj = new GameObject("ConfigText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(row.transform, false);
            var rect = (RectTransform)textObj.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 0);
            rect.offsetMax = new Vector2(-8, 0);
            textComponent = textObj.GetComponent<TextMeshProUGUI>();
            if (labelTemplate != null)
            {
                textComponent.font = labelTemplate.font;
                textComponent.fontSharedMaterial = labelTemplate.fontSharedMaterial;
                textComponent.color = labelTemplate.color;
            }

            if (isHeader)
            {
                textComponent.fontStyle = FontStyles.Bold;
                textComponent.fontSize = 20;
                textComponent.fontSizeMin = 16;
                textComponent.fontSizeMax = 22;
                textComponent.enableAutoSizing = true;
                textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            }
            else
            {
                textComponent.fontSize = 15;
                textComponent.alignment = TextAlignmentOptions.MidlineLeft;
                textComponent.textWrappingMode = TextWrappingModes.Normal;
            }

            textComponent.raycastTarget = false;
            textComponent.text = text ?? "";
            return row;
        }
    }
}
