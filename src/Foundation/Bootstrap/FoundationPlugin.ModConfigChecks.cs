using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>Provides the foundation plugin API.</summary>
    public sealed partial class FoundationPlugin
    {
        void VerifyModConfig()
        {
            var registry = new ModConfigRegistry();
            Check(registry.Sections.Count == 0, "ModConfigRegistry starts empty");

            // 1. Get or create section
            var section = registry.GetOrCreateSection("test.mod", "Test Mod", "A mod for testing config");
            Check(registry.Sections.Count == 1, "Section successfully added");
            Check(section.ModId == "test.mod" && section.ModName == "Test Mod", "Section properties match");
            Check(registry.TryGetSection("test.mod", out var found) && ReferenceEquals(found, section), "TryGetSection retrieves registered section");

            // 2. Add standard elements
            int clickCount = 0;
            var btn = section.AddButton("Test Button", () => clickCount++, "Click Me");
            Check(btn.Label == "Test Button" && btn.ButtonText == "Click Me", "Button properties correct");
            btn.OnClick();
            Check(clickCount == 1, "Button click executes callback");

            string currentText = "Initial Text";
            var textEl = section.AddText(currentText, isHeader: true);
            Check(textEl.Text == currentText && textEl.IsHeader, "Text element properties correct");

            bool toggleVal = false;
            int toggleChangeCount = 0;
            var toggleEl = section.AddToggle("Test Toggle", false, val => { toggleVal = val; toggleChangeCount++; }, () => toggleVal);
            Check(!toggleEl.Value, "Toggle starts false");

            float sliderVal = 10f;
            var sliderEl = section.AddSlider("Test Slider", 0f, 100f, 10f, val => sliderVal = val, step: 5f, getter: () => sliderVal, formatValue: v => $"{v:0}%");
            Check(sliderEl.MinValue == 0f && sliderEl.MaxValue == 100f && sliderEl.Step == 5f, "Slider range and step correct");

            int dropIdx = 0;
            var options = new[] { "Option A", "Option B", "Option C" };
            var dropEl = section.AddDropdown("Test Dropdown", options, 0, idx => dropIdx = idx, () => dropIdx);
            Check(dropEl.Options.Count == 3 && dropEl.SelectedIndex == 0, "Dropdown options and initial index correct");

            Check(section.Elements.Count == 5, "Section contains all 5 template elements");

            // 3. Commit and Cancel lifecycle
            toggleEl.Value = true;
            Check(!toggleVal && toggleChangeCount == 0, "Editing a toggle stages the value without firing its callback");
            section.Commit();
            Check(toggleVal && toggleChangeCount == 1, "Commit propagates staged toggle value once");
            section.Commit();
            Check(toggleChangeCount == 1, "Repeated Apply without changes does not repeat callbacks");

            sliderEl.Value = 50f;
            section.Commit();
            Check(sliderVal == 50f, "Commit propagates slider value to callback");

            // Cancel
            sliderEl.Value = 99f;
            toggleEl.Value = false;
            dropEl.SelectedIndex = 2;
            section.Cancel();
            Check(sliderEl.Value == 50f && sliderVal == 50f, "Cancel discards pending slider value without applying it");
            Check(toggleEl.Value && toggleVal && toggleChangeCount == 1, "Cancel discards toggle edits without a callback");
            Check(dropEl.SelectedIndex == 0 && dropIdx == 0, "Cancel discards dropdown edits without a callback");
            sliderEl.Value = 90f;
            ((ModConfigSection)section).BeginEdit();
            Check(sliderEl.Value == 50f, "Reopening Options refreshes values from the committed source");

            // 4. BepInEx ConfigEntry integration
            string tempConfigPath = Path.Combine(Paths.ConfigPath ?? ".", "test_foundation_config.cfg");
            try
            {
                var configFile = new ConfigFile(tempConfigPath, true);
                var boolEntry = configFile.Bind("General", "TestBool", true);
                var intEntry = configFile.Bind("General", "TestInt", 42);
                var floatEntry = configFile.Bind("General", "TestFloat", 1.5f);
                var strEntry = configFile.Bind("General", "TestStr", "Option B");

                var boundToggle = section.AddToggle(boolEntry);
                var boundSlider = section.AddSlider(intEntry, 0, 100);
                var boundFloatSlider = section.AddSlider(floatEntry, 0f, 5f, step: 0.1f);
                var boundDrop = section.AddDropdown(strEntry, Array.AsReadOnly(options));

                Check(boundToggle.Value == true, "Bound toggle reads initial ConfigEntry value");
                Check(boundSlider.Value == 42f, "Bound slider reads initial ConfigEntry value");
                Check(boundFloatSlider.Value == 1.5f, "Bound float slider reads initial ConfigEntry value");
                Check(boundDrop.SelectedIndex == 1, "Bound dropdown selects matching string option index");

                boundToggle.Value = false;
                boundSlider.Value = 65f;
                boundFloatSlider.Value = 2.5f;
                boundDrop.SelectedIndex = 2;
                Check(boolEntry.Value && intEntry.Value == 42 && floatEntry.Value == 1.5f && strEntry.Value == "Option B",
                    "Editing bound controls never changes ConfigEntry values before Apply");
                section.Cancel();
                Check(boolEntry.Value && intEntry.Value == 42 && floatEntry.Value == 1.5f && strEntry.Value == "Option B" &&
                    boundToggle.Value && boundSlider.Value == 42f && boundDrop.SelectedIndex == 1,
                    "Cancel restores all BepInEx bound controls and values");
                boundToggle.Value = false;
                boundSlider.Value = 65f;
                boundFloatSlider.Value = 2.5f;
                boundDrop.SelectedIndex = 2;
                section.Commit();
                Check(!boolEntry.Value && intEntry.Value == 65 && floatEntry.Value == 2.5f && strEntry.Value == "Option C",
                    "Apply writes all changed BepInEx ConfigEntry types");
            }
            finally
            {
                if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
            }

            // 5. New staged text, numeric and search controls for Foundation UI.
            string persistedText = "Original";
            double persistedNumber = 10;
            int persistedSearch = 0;
            var input = section.AddInput("Text Entry", persistedText, val => persistedText = val,
                () => persistedText, maxLength: 64);
            var number = section.AddNumber("Numeric Entry", 0, 100, persistedNumber, 0.5,
                val => persistedNumber = val, () => persistedNumber);
            var search = section.AddSearch("Search Entry", new[] { "Alpha", "Beta", "Gamma" },
                0, idx => persistedSearch = idx, () => persistedSearch);
            input.Value = "Changed";
            number.Value = 25;
            search.SelectedIndex = 2;
            Check(persistedText == "Original" && persistedNumber == 10 && persistedSearch == 0,
                "Text, number and searchable selection stage without mutating committed values");
            section.Cancel();
            Check(input.Value == "Original" && number.Value == 10 && search.SelectedIndex == 0,
                "Cancel restores staged text/numeric/search values");
            input.Value = "Changed";
            number.Value = 25;
            search.SelectedIndex = 2;
            section.Commit();
            Check(persistedText == "Changed" && persistedNumber == 25 && persistedSearch == 2,
                "Apply writes staged text, numeric and searchable values");
            section.Commit();
            Check(persistedText == "Changed" && persistedNumber == 25 && persistedSearch == 2,
                "Repeated apply is idempotent for new controls");

            // 6. Remove section
            registry.RemoveSection("test.mod");
            Check(registry.Sections.Count == 0 && !registry.TryGetSection("test.mod", out _), "RemoveSection successfully cleans up");
        }
    }
}
