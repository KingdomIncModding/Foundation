using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Behaviour.UI;
using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.UI;

namespace Foundation.StageBVerification
{
    // This is part of the SAME temporary StageBVerification DLL, not a new
    // production framework and not an extra always-on verification plugin.
    // Registry entries survive the one-shot checks so the user can SEE and USE
    // the controls in a loaded kingdom. No Update, polling, Harmony or mutation.
    public sealed partial class StageBVerificationPlugin
    {
        const string VisualPanelId = "visual-panel";
        IFoundationPanelModContext visualPanels;
        readonly List<IModBarRegistration> visualItems = new List<IModBarRegistration>();
        string visualReport;
        bool barButtonClicked, barToggleChanged, barDropdownChanged;
        bool panelOpened, panelButtonClicked, panelToggleChanged, panelDropdownChanged;
        bool panelInputChanged, visualUserReviewed;
        bool barHierarchyVerified;
        int panelActions;
        bool panelToggleValue;
        int panelDropdownIndex;
        string panelInputValue = "Native input test";
        string visualError;

        private void InitializeVisualProbe()
        {
            visualReport = Path.Combine(BepInEx.Paths.GameRootPath, "_Mods", "Foundation", "Verification", "StageB", "stage-b-visual-verification.json");
            try
            {
                // Keep these owner-scoped registrations until the test DLL is
                // unloaded. Previous smoke code used a `using` scope and a
                // temporary text control; neither can ever display buttons.
                visualPanels = FoundationApi.Panels.ForMod(Owner);
                visualPanels.Register(VisualPanelId, "STAGE B - VISUAL TEST", BuildVisualPanel, allowDrag: true);
                visualItems.Add(FoundationApi.ModBar.Register(new ModBarButton(
                    Owner, "open-ui", "STAGE B UI TEST", OpenVisualPanel,
                    width: 194f, order: -500, alignment: ModBarAlignment.Left)));
                visualItems.Add(FoundationApi.ModBar.Register(new ModBarToggle(
                    Owner, "bar-toggle", "Test Toggle", false, value =>
                    {
                        barToggleChanged = true;
                        VerifyVisibleBar();
                        WriteVisualReport();
                    }, width: 166f, order: -499, alignment: ModBarAlignment.Left)));
                visualItems.Add(FoundationApi.ModBar.Register(new ModBarDropdown(
                    Owner, "bar-dropdown", new[] { "Choose option", "First", "Second" },
                    selectedIndex: 0, selected: index =>
                    {
                        if (index > 0) barDropdownChanged = true;
                        VerifyVisibleBar();
                        WriteVisualReport();
                    }, width: 165f, order: -498, alignment: ModBarAlignment.Left)));
                visualItems.Add(FoundationApi.ModBar.Register(new ModBarSeparator(
                    Owner, "bar-separator", 14f, -497, ModBarAlignment.Left)));
                Logger.LogInfo("[STAGE B VISUAL] In a loaded kingdom, look below the native currency HUD for the MOD BAR and click 'STAGE B UI TEST'. The controls remain registered until the test DLL is removed.");
            }
            catch (Exception e)
            {
                visualError = "Could not create visible UI test controls: " + e;
                Logger.LogError("[STAGE B VISUAL] " + visualError);
            }
            WriteVisualReport();
        }

        private void OpenVisualPanel()
        {
            barButtonClicked = true;
            try
            {
                panelOpened = FoundationApi.Panels.Open(Owner, VisualPanelId);
                if (!panelOpened) visualError = "Cannot open Stage B test panel: no in-game GameUI available.";
                else visualError = null;
                VerifyVisibleBar();
            }
            catch (Exception e)
            {
                visualError = "Could not open Stage B test panel: " + e;
                Logger.LogError("[STAGE B VISUAL] " + visualError);
            }
            WriteVisualReport();
        }

        private void BuildVisualPanel(IFoundationPanelBuilder ui)
        {
            ui.Header("TEST FOUNDATION CONTROLS");
            ui.Text("Click the button, change the toggle and dropdown, then edit the text field. These tests only affect temporary test data.");
            ui.Button("CLICK ME - TEST BUTTON", () =>
            {
                panelButtonClicked = true;
                panelActions++;
                ui.Message("Button clicked " + panelActions + " time(s).");
                WriteVisualReport();
            }, "Verifies the real native-themed Foundation button receives a click.");
            ui.Toggle("TEST CHECKBOX", panelToggleValue, value =>
            {
                panelToggleValue = value;
                panelToggleChanged = true;
                WriteVisualReport();
            });
            ui.Dropdown("TEST DROPDOWN", new[] { "Select a value", "First", "Second" }, panelDropdownIndex, index =>
            {
                panelDropdownIndex = index;
                if (index > 0) panelDropdownChanged = true;
                WriteVisualReport();
            });
            ui.Input("TEST TEXT FIELD", panelInputValue, text =>
            {
                panelInputValue = text;
                if (!string.Equals(text, "Native input test", StringComparison.Ordinal)) panelInputChanged = true;
                WriteVisualReport();
            });
            ui.Text("Inspect the layout at your current UI scale. Ensure buttons, text, borders and menu options are visible and correctly aligned.");
            ui.Button("I CHECKED THE VISUAL LAYOUT", () =>
            {
                visualUserReviewed = true;
                VerifyVisibleBar();
                WriteVisualReport();
                ui.Message("Visual confirmation recorded. Exit game and select option 3 to read the result.");
            }, "Manual acknowledgement: never claim a visual pass from registration alone.");
        }

        // Inspect the ACTIVE native GameUI, not merely a registration list.
        // The bar is created by Foundation when the game's HUD loads.
        private void VerifyVisibleBar()
        {
            var gameUi = GameUI.Instance;
            if (!gameUi) return;
            var controls = gameUi.GetComponentsInChildren<RectTransform>(true);
            bool HasControl(string name, Func<RectTransform, bool> valid) => controls.Any(root =>
                root && root.name == Owner + ":" + name && root.gameObject.activeInHierarchy && valid(root));
            barHierarchyVerified =
                HasControl("open-ui", r => r.GetComponentInChildren<Button>(true) != null) &&
                HasControl("bar-toggle", r => r.GetComponentInChildren<Toggle>(true) != null) &&
                HasControl("bar-dropdown", r => r.GetComponentInChildren<TMPro.TMP_Dropdown>(true) != null) &&
                HasControl("bar-separator", r => r.GetComponentInChildren<Image>(true) != null);
            if (!barHierarchyVerified) Logger.LogWarning("[STAGE B VISUAL] Some controls are registered but not active in GameUI. Check the Mod Bar chevron/HUD visibility and BepInEx log.");
        }

        private struct VisualResult { internal string Id, Status, Details; }
        private VisualResult[] VisualResults()
        {
            var gameUi = GameUI.Instance;
            bool panelVisible = gameUi && FoundationApi.Panels.IsOpen;
            bool panelInteractive = panelOpened && panelButtonClicked && panelToggleChanged && panelDropdownChanged && panelInputChanged;
            bool barInteractive = barButtonClicked && barToggleChanged && barDropdownChanged;
            string Missing(params (string Label, bool Done)[] flags) => string.Join(", ", flags.Where(f => !f.Done).Select(f => f.Label));
            VisualResult R(string id, bool pass, string details) => new VisualResult { Id = id, Status = pass ? "PASS" : "PENDING", Details = details };
            return new[]
            {
                R("B4.PanelOpened", panelOpened, panelOpened ? "Foundation native panel opened from the visible Mod Bar button." : "Load a kingdom and click STAGE B UI TEST in the Mod Bar."),
                R("B4.PanelControlsInteracted", panelInteractive,
                    panelInteractive ? "Button, toggle, dropdown and committed text input callbacks executed." : "Interact with the real panel controls: " + Missing(("open panel",panelOpened),("button",panelButtonClicked),("toggle",panelToggleChanged),("dropdown (choose First or Second)",panelDropdownChanged),("change text and leave input",panelInputChanged))),
                R("B4.RenderingVisualAudit", panelInteractive && visualUserReviewed && barHierarchyVerified,
                    panelInteractive && visualUserReviewed && barHierarchyVerified ? "Operator visually approved layout after actual UI interactions; active HUD hierarchy verified." : "Manual visual approval required after interaction. " + Missing(("all panel actions",panelInteractive),("click I CHECKED THE VISUAL LAYOUT",visualUserReviewed),("active native HUD controls",barHierarchyVerified))),
                R("B6.ModBarNativeVisual", barInteractive && barHierarchyVerified && visualUserReviewed,
                    barInteractive && barHierarchyVerified && visualUserReviewed ? "HUD button/toggle/dropdown received interaction; separator and active GameUI hierarchy verified; operator visually approved." : "Use visible HUD controls, then confirm appearance in the panel. " + Missing(("HUD button",barButtonClicked),("HUD toggle",barToggleChanged),("HUD dropdown",barDropdownChanged),("active HUD hierarchy",barHierarchyVerified),("visual approval",visualUserReviewed)))
            };
        }

        private void WriteVisualReport()
        {
            if (string.IsNullOrEmpty(visualReport)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(visualReport));
                var result = VisualResults();
                var b = new StringBuilder();
                b.Append("{\n  \"source\": \"Stage B native HUD and panel interaction (event-driven, no Update)\",\n");
                b.Append("  \"utc\": ").Append(Escape(DateTime.UtcNow.ToString("o"))).Append(",\n");
                b.Append("  \"foundationVersion\": ").Append(Escape(typeof(FoundationApi).Assembly.GetName().Version.ToString())).Append(",\n");
                b.Append("  \"foundationSha256\": ").Append(Escape(Hash(typeof(FoundationApi).Assembly.Location))).Append(",\n");
                b.Append("  \"manualVisualApproval\": ").Append(visualUserReviewed ? "true" : "false").Append(",\n");
                b.Append("  \"warning\": ").Append(Escape(visualError ?? "")).Append(",\n");
                b.Append("  \"checks\": [\n");
                for (int i = 0; i < result.Length; i++)
                {
                    if (i > 0) b.Append(",\n");
                    var c = result[i];
                    b.Append("    { \"id\": ").Append(Escape(c.Id)).Append(", \"result\": ").Append(Escape(c.Status))
                     .Append(", \"details\": ").Append(Escape(c.Details)).Append(" }");
                }
                b.Append("\n  ]\n}\n");
                // Writes only on registration and user events; never per frame.
                File.WriteAllText(visualReport, b.ToString());
            }
            catch (Exception e) { Logger.LogError("[STAGE B VISUAL] Could not write report: " + e); }
        }

        private void OnDestroy()
        {
            for (int i = visualItems.Count - 1; i >= 0; i--)
                try { visualItems[i].Dispose(); } catch (Exception e) { Logger.LogWarning(e); }
            visualItems.Clear();
            try { visualPanels?.Dispose(); } catch (Exception e) { Logger.LogWarning(e); }
            visualPanels = null;
        }
    }
}
