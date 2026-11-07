using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
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
            var toggleEl = section.AddToggle("Test Toggle", false, val => toggleVal = val, () => toggleVal);
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
            section.Commit();
            Check(toggleVal, "Commit propagates toggle value to callback");

            sliderEl.Value = 50f;
            section.Commit();
            Check(sliderVal == 50f, "Commit propagates slider value to callback");

            // Cancel
            sliderEl.Value = 99f;
            section.Cancel();
            Check(sliderEl.Value == 50f, "Cancel reverts pending slider edit");

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
                var boundDrop = section.AddDropdown(strEntry, options);

                Check(boundToggle.Value == true, "Bound toggle reads initial ConfigEntry value");
                Check(boundSlider.Value == 42f, "Bound slider reads initial ConfigEntry value");
                Check(boundFloatSlider.Value == 1.5f, "Bound float slider reads initial ConfigEntry value");
                Check(boundDrop.SelectedIndex == 1, "Bound dropdown selects matching string option index");

                boundToggle.Value = false;
                section.Commit();
                Check(!boolEntry.Value, "Commit updates bound ConfigEntry value");
            }
            finally
            {
                if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
            }

            // 5. Remove section
            registry.RemoveSection("test.mod");
            Check(registry.Sections.Count == 0 && !registry.TryGetSection("test.mod", out _), "RemoveSection successfully cleans up");
        }
    }
}
