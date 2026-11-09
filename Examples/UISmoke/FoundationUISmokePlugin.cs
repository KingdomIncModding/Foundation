using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Source.Flow;
using Assets.Source.Player;
using Assets.Behaviour.UI.MainMenu;
using BepInEx;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomFoundation.Examples
{
    /// <summary>
    /// Development-only, one-click UI screenshot and callback audit.
    /// Never modifies native game machines: machine panels are opened READ-ONLY.
    /// Programmatic callback tests affect only this plugin's temporary values.
    /// </summary>
    [BepInPlugin(ModId, "Foundation UI Screenshot Smoke", "1.1.0")]
    [BepInDependency("urakka.kingdominc.foundation", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class FoundationUISmokePlugin : BaseUnityPlugin
    {
        private const string ModId = "urakka.foundation.example.ui-smoke";
        private IFoundationPanelModContext scope;
        private IModBarRegistration startButton;
        private ISignalSource syntheticSignal;
        private readonly List<AuditSpec> specs = new List<AuditSpec>();
        private readonly List<SettingsAuditSpec> settingSpecs = new List<SettingsAuditSpec>();
        private UiAuditReport report;
        private string directory;
        private bool running;
        private int actions;
        private bool toggleValue;
        private float sliderValue;
        private double numericValue;
        private string inputValue;
        private int dropdownValue;
        private int searchValue;
        private int colorValue;
        private SignalSelection signalValue;
        private int tabValue;
        private bool sectionRendered;
        private int confirms;
        private int rejected;
        private bool propertyValue;
        private int liveNumber;
        private readonly List<string> reordered = new List<string> { "North", "Center", "South" };

        private void Awake()
        {
            scope = FoundationApi.Panels.ForMod(ModId);
            syntheticSignal = FoundationApi.Signals.Register(new SignalSourceDefinition(
                ModId, ModId + ".test-source", "UI Audit Test Source",
                new[] {
                    new SignalPropertyDefinition("enabled", "Enabled", typeof(bool), () => true),
                    new SignalPropertyDefinition("pressure", "Pressure", typeof(float), () => 0.75f)
                }));
            InitializeSpecs();
            InitializeSettingsSpecs();
            startButton = FoundationApi.ModBar.Register(new ModBarButton(
                ModId, "capture", "UI Smoke + Screenshots", Begin, 245, order: 20));
            Logger.LogInfo("[FOUNDATION UI SMOKE] Ready. In a kingdom, click 'UI Smoke + Screenshots' on the Mod Bar.");
        }

        private void Add(string key, string label, string row, Action<IFoundationPanelBuilder> build, bool exercise = true)
        {
            var id = "smoke-" + key;
            scope.Register(id, "UI Screenshot / " + label, build, allowDrag: false);
            specs.Add(new AuditSpec { Id = id, Key = key, Title = label, RowName = row, Interactive = exercise });
        }

        private void InitializeSpecs()
        {
            Add("header", "Header", "Header", u => u.Header("Native heading style"), false);
            Add("text", "Text", "Text", u => u.Text("Long-form text and game font sample"), false);
            Add("button", "Button", "Button", u => u.Button("Test action", () => actions++, "Native callback and tooltip"));
            Add("toggle", "Toggle", "Toggle", u => u.Toggle("Native checkbox", toggleValue, v => toggleValue = v));
            Add("slider", "Slider", "Slider", u => u.Slider("0 to 10", 0, 10, sliderValue, .5f, v => sliderValue = v));
            Add("number", "Number Input", "Input", u => u.Number("Numeric value", numericValue, -100, 100, .5, v => numericValue = v));
            Add("input", "Text Input", "Input", u => u.Input("Custom text", inputValue ?? "Original", v => inputValue = v));
            Add("dropdown", "Dropdown", "Dropdown", u => u.Dropdown("Choose entry", new[] { "Amber", "Blue", "Crimson" }, dropdownValue, v => dropdownValue = v));
            Add("search", "Search / Lookup", "SearchResult", u => u.Search("Search color", new[] { "Amber", "Blue", "Crimson", "Dandelion", "Emerald", "Fuchsia" }, searchValue, v => searchValue = v));
            Add("colors", "Color Swatches", "ColorSwatches", u => u.Colors("Select color", new[] { Color.red, Color.green, Color.blue, Color.yellow }, colorValue, v => { colorValue = v; u.Rebuild(); }));
            Add("signal-source", "Signal Source Selector", "Dropdown", u => u.Signal("Select signal", signalValue, v => signalValue = v));
            Add("signal-property", "Signal Property Selector", "Dropdown", u => u.Signal("Select signal property", new SignalSelection(syntheticSignal.Id, signalValue?.PropertyId), v => signalValue = v));
            Add("tabs", "Tabs", "Tabs", u => u.Tabs("smoke-tabs", new[] { "General", "Advanced" }, (idx, pane) => { tabValue = idx; pane.Text("Active: " + (idx == 0 ? "General" : "Advanced")); }));
            Add("columns", "Columns", "Columns", u => u.Columns(3, (idx, pane) => pane.Text("Column " + (idx + 1))), false);
            Add("section", "Collapsible Section", "Button", u => u.Section("smoke-section", "Details", pane => { sectionRendered = true; pane.Text("Expanded contents"); }));
            Add("reorder", "Reorderable Rows", "ReorderRow", u => u.Reorder("Drag rows", reordered, (from, to) => { var value = reordered[from]; reordered.RemoveAt(from); reordered.Insert(to, value); }));
            Add("confirm", "Confirmation Accept", "Button", u => u.Confirm("Apply test action?", () => confirms++, () => rejected++));
            Add("confirm-cancel", "Confirmation Cancel", "Button", u => u.Confirm("Apply test action?", () => confirms++, () => rejected++));
            Add("property", "Dynamic Property", "Toggle", u => u.Property(new FoundationUiProperty("audit-prop", "Editable property", typeof(bool), () => propertyValue, value => propertyValue = (bool)value)));
            Add("live", "Live Telemetry", "LiveValue", u => u.LiveText("Frame counter", () => (++liveNumber).ToString(CultureInfo.InvariantCulture), .15f), false);
            Add("tooltip", "Button Tooltip", "Button", u => u.Button("Hover for tooltip", () => actions++, "Tooltip copied from native text style"));
            Add("scroll", "Scrolling / Overflow", "Text", u => { for (int i = 0; i < 32; i++) u.Text("Scrollable row " + (i + 1)); });
        }

        private void AddSettings(string key, string label, Action<IModConfigSection> build)
        {
            var id = ModId + ".settings-" + key;
            var section = FoundationApi.ModConfig.GetOrCreateSection(id, "UI Audit: " + label,
                "Temporary UI screenshot test; no gameplay properties are changed.");
            build(section);
            settingSpecs.Add(new SettingsAuditSpec { Id = id, Title = label, Label = "Audit " + label });
        }

        private void InitializeSettingsSpecs()
        {
            AddSettings("text", "Text", s => s.AddText("Audit Text"));
            AddSettings("button", "Button", s => s.AddButton("Audit Button", () => actions++, "Test button"));
            AddSettings("toggle", "Toggle", s => s.AddToggle("Audit Toggle", false, value => toggleValue = value));
            AddSettings("slider", "Slider", s => s.AddSlider("Audit Slider", 0f, 10f, 3f, value => sliderValue = value, .5f));
            AddSettings("dropdown", "Dropdown", s => s.AddDropdown("Audit Dropdown", new[] { "Amber", "Blue", "Crimson" }, 0, value => dropdownValue = value));
            AddSettings("number", "Number", s => s.AddNumber("Audit Number", 0, 100, 12, .5, value => numericValue = value));
            AddSettings("input", "Input", s => s.AddInput("Audit Input", "Test text", value => inputValue = value));
            AddSettings("search", "Search", s => s.AddSearch("Audit Search", new[] { "Amber", "Blue", "Crimson", "Emerald" }, 1, value => searchValue = value));
            AddSettings("colors", "Colors", s => s.AddElement(new ModConfigColorSwatches(
                "Audit Colors", new[] { Color.red, Color.green, Color.blue, Color.yellow }, Color.red, value => { })));
            AddSettings("signal", "Signal", s => s.AddElement(new ModConfigSignalSelector(
                "Audit Signal", new SignalSelection(syntheticSignal.Id, "enabled"), value => signalValue = value)));
        }

        private void Begin()
        {
            if (running) { Logger.LogWarning("[FOUNDATION UI SMOKE] Audit already running."); return; }
            if (GamePlayer.Current == null)
            {
                Logger.LogWarning("[FOUNDATION UI SMOKE] Load a test kingdom before capturing UI screenshots.");
                return;
            }
            StartCoroutine(RunAudit());
        }

        private IEnumerator RunAudit()
        {
            running = true;
            directory = Path.Combine(Paths.GameRootPath, "_Mods", "Foundation", "Verification", "UISmoke",
                DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            report = new UiAuditReport {
                StartedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                FoundationVersion = typeof(FoundationApi).Assembly.GetName().Version.ToString(),
                GameResolution = Screen.width + "x" + Screen.height,
                ScreenshotFolder = directory
            };
            try { Directory.CreateDirectory(directory); }
            catch (Exception ex) { Logger.LogError("[FOUNDATION UI SMOKE] Cannot create report directory: " + ex); running = false; yield break; }
            Logger.LogInfo("[FOUNDATION UI SMOKE] START " + directory);
            int sequence = 0;
            foreach (var spec in specs)
            {
                var result = new UiAuditResult { Name = spec.Title, Kind = "control", Status = "FAIL" };
                report.Cases.Add(result);
                try
                {
                    if (!scope.Open(spec.Id)) throw new InvalidOperationException("Foundation refused to open " + spec.Id);
                }
                catch (Exception ex) { result.Error = "Open: " + ex.Message; SaveReport(); continue; }
                yield return null;
                yield return new WaitForEndOfFrame();
                var root = LocatePanel();
                if (!root)
                {
                    result.Error = "FoundationModPanel not active after opening " + spec.Id;
                    SaveReport(); continue;
                }
                try
                {
                    ValidateStructure(root, spec);
                    Capture(++sequence, spec.Key, "initial", root, spec.RowName, result);
                    result.Rendered = true;
                }
                catch (Exception ex) { result.Error = "Render / capture: " + ex.Message; SaveReport(); continue; }

                if (!spec.Interactive)
                {
                    result.Status = result.VisualIssues.Count == 0 ? "PASS" : "FAIL";
                    if (result.VisualIssues.Count != 0)
                        result.Error = "Visual layout defect: " + result.VisualIssues[0];
                    result.Note = "Rendered and screenshotted; no safe callback required.";
                    SaveReport();
                    continue;
                }

                // Capture the actual expanded dropdown options, not just its closed caption.
                if (spec.Key == "dropdown")
                {
                    try
                    {
                        var dropdown = root.GetComponentInChildren<TMP_Dropdown>(true);
                        if (!dropdown) throw new InvalidOperationException("Native dropdown missing.");
                        dropdown.Show();
                    }
                    catch (Exception ex) { result.Error = "Dropdown expand: " + ex.Message; }
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    root = LocatePanel();
                    if (root)
                    {
                        try { Capture(++sequence, spec.Key, "expanded", root, spec.RowName, result); }
                        catch (Exception ex) { result.Error = "Dropdown expanded capture: " + ex.Message; }
                        var open = root.GetComponentInChildren<TMP_Dropdown>(true);
                        if (open) open.Hide();
                    }
                    yield return null;
                    yield return new WaitForEndOfFrame();
                }

                // Confirmation prompts are a distinct visual state. Capture it before
                // accepting so the audit includes the actual dialog, not only the button.
                if (spec.Key == "confirm" || spec.Key == "confirm-cancel")
                {
                    var ask = FindButton(root, "Confirm: Apply test action?");
                    if (ask) ask.onClick.Invoke();
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    root = LocatePanel();
                    if (root)
                    {
                        try { Capture(++sequence, spec.Key, "confirmation-prompt", root, "Button", result); }
                        catch (Exception ex) { result.Error = "Confirmation prompt capture: " + ex.Message; }
                    }
                }
                string issue;
                bool triggered;
                try { triggered = Exercise(root, spec.Key, out issue); }
                catch (Exception ex) { triggered = false; issue = "Callback exception: " + ex; }
                result.Interacted = triggered;
                result.Note = issue;
                // A UI event may cause Foundation to rebuild its panel. Wait for layout before the next snapshot.
                yield return null;
                yield return new WaitForEndOfFrame();
                root = LocatePanel();
                if (root)
                {
                    try { Capture(++sequence, spec.Key, "after-action", root, spec.Key == "scroll" ? null : spec.RowName, result); }
                    catch (Exception ex) { issue = (issue ?? "") + "; post-action capture failed: " + ex.Message; triggered = false; }
                }
                else { issue = (issue ?? "") + "; panel disappeared after callback"; triggered = false; }
                // Do not silently discard earlier errors (for example, dropdown expansion).
                result.Status = (triggered && string.IsNullOrEmpty(result.Error) && result.VisualIssues.Count == 0) ? "PASS" : "FAIL";
                if (!triggered) result.Error = issue;
                else if (result.VisualIssues.Count != 0 && string.IsNullOrEmpty(result.Error))
                    result.Error = "Visual layout defect: " + result.VisualIssues[0];
                SaveReport();
            }
            // Observe machine-specific panel layouts in the active flow without mutating them.
            if (GamePlayer.Current != null)
            {
                FlowElement[] native = new FlowElement[0];
                try { native = FoundationMachineApi.Registry.OnFlow(GamePlayer.Current.CurrentFlow)
                    .Where(e => e != null).GroupBy(e => e.GetType().FullName).Select(g => g.First()).ToArray(); }
                catch (Exception ex) { Logger.LogWarning("[FOUNDATION UI SMOKE] Could not enumerate placed machines: " + ex.Message); }
                int machineNumber = 0;
                foreach (var item in native)
                {
                    var result = new UiAuditResult { Name = item.GetType().Name, Kind = "machine", Status = "SKIP" };
                    report.Cases.Add(result);
                    try
                    {
                        if (!FoundationApi.Panels.OpenMachine(item)) { result.Note = "No matching machine panel."; SaveReport(); continue; }
                    }
                    catch (Exception ex) { result.Error = "Open: " + ex.Message; result.Status = "FAIL"; SaveReport(); continue; }
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    var root = LocatePanel();
                    if (root)
                    {
                        try
                        {
                            Capture(++sequence, "machine-" + (++machineNumber).ToString("00") + "-" + item.GetType().Name, "native", root, null, result);
                            result.Rendered = true;
                            result.Status = result.VisualIssues.Count == 0 ? "PASS" : "FAIL";
                            if (result.VisualIssues.Count != 0) result.Error = "Visual layout defect: " + result.VisualIssues[0];
                            result.Note = "Read-only screenshot; machine behavior is not altered.";
                        }
                        catch (Exception ex) { result.Error = "Screenshot: " + ex.Message; result.Status = "FAIL"; }
                    }
                    else { result.Error = "Native panel did not appear."; result.Status = "FAIL"; }
                    SaveReport();
                }
            }
            FoundationApi.Panels.Close();
            // The Options/Mod Settings path is a separate UI system, so capture
            // every ModConfig element using the real native in-game Options menu.
            var settingsCapture = CaptureSettings();
            while (settingsCapture.MoveNext()) yield return settingsCapture.Current;
            report.FinishedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            SaveReport();
            int passed = report.Cases.Count(x => x.Status == "PASS");
            int failed = report.Cases.Count(x => x.Status == "FAIL");
            Logger.LogInfo("[FOUNDATION UI SMOKE] FINISH pass=" + passed + " fail=" + failed + " folder=" + directory);
            running = false;
        }

        private IEnumerator CaptureSettings()
        {
            var menu = Resources.FindObjectsOfTypeAll<InGameMenuUI>().FirstOrDefault(item =>
                item && item.gameObject.scene.IsValid() && item.gameObject.scene.isLoaded);
            if (!menu)
            {
                report.Cases.Add(new UiAuditResult { Name = "Native Options / Mod Settings", Kind = "settings", Status = "SKIP",
                    Note = "In-game Options menu is not available in the active scene." });
                SaveReport();
                yield break;
            }
            bool wasActive = menu.gameObject.activeSelf;
            var startup = new UiAuditResult { Name = "Options Game Settings", Kind = "settings", Status = "FAIL" };
            report.Cases.Add(startup);
            FoundationConfigMenu config = null;
            RectTransform optionsRoot = null;
            try
            {
                menu.gameObject.SetActive(true);
                menu.ShowOptions();
            }
            catch (Exception ex) { startup.Error = "Open native Options: " + ex.Message; }
            yield return null;
            yield return new WaitForEndOfFrame();
            try
            {
                var options = menu.GetComponentInChildren<OptionsUI>(true);
                if (!options) throw new InvalidOperationException("Native OptionsUI was not found.");
                optionsRoot = options.GetComponent<RectTransform>();
                config = options.GetComponent<FoundationConfigMenu>();
                if (!config) throw new InvalidOperationException("FoundationConfigMenu was not attached to OptionsUI.");
                config.ShowGameTab();
                Capture(report.Cases.Count + 100, "options-game", "native", optionsRoot, null, startup);
                startup.Rendered = true;
                startup.Status = "PASS";
                startup.Note = "Captured native Game Settings without committing options.";
            }
            catch (Exception ex) { startup.Error = "Native Options capture: " + ex.Message; }
            SaveReport();
            if (config && optionsRoot)
            {
                foreach (var spec in settingSpecs)
                {
                    var result = new UiAuditResult { Name = "Mod Settings / " + spec.Title, Kind = "settings", Status = "FAIL" };
                    report.Cases.Add(result);
                    try { config.ShowModsTab(spec.Id); }
                    catch (Exception ex) { result.Error = "Select mod section: " + ex.Message; SaveReport(); continue; }
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    try
                    {
                        var textExists = optionsRoot.GetComponentsInChildren<TMP_Text>(true).Any(t =>
                            t.gameObject.activeInHierarchy && (t.text ?? "").Contains(spec.Label));
                        if (!textExists) throw new InvalidOperationException("Control label is not visible: " + spec.Label);
                        Capture(report.Cases.Count + 100, "settings-" + spec.Title.ToLowerInvariant(), "native", optionsRoot, null, result);
                        // A Foundation signal caption previously displayed its
                        // raw source GUID and wrapped into the next Options row.
                        // This dedicated inspection covers Foundation-injected
                        // settings, which the generic Foundation-panel visual
                        // checker deliberately excludes.
                        // Check injected native Options controls, not just
                        // Foundation popup windows. The previous 37/37 result
                        // missed four human-visible defects in these rows.
                        InspectModSettingsControlLayout(optionsRoot, spec.Title, result);
                        if (spec.Title == "Signal") InspectModSettingsSignalLayout(optionsRoot, result);
                        result.Rendered = true;
                        result.Status = result.VisualIssues.Count == 0 ? "PASS" : "FAIL";
                        result.Note = "Visual capture only; Apply/Cancel not invoked.";
                        if (result.VisualIssues.Count != 0)
                            result.Error = "Visual layout defect: " + result.VisualIssues[0];
                    }
                    catch (Exception ex) { result.Error = "Settings capture: " + ex.Message; }
                    SaveReport();
                }
                var installed = new UiAuditResult { Name = "Options Installed Mods", Kind = "settings", Status = "FAIL" };
                report.Cases.Add(installed);
                try { config.ShowInstalledTab(); }
                catch (Exception ex) { installed.Error = "Installed Mods tab: " + ex.Message; }
                yield return null;
                yield return new WaitForEndOfFrame();
                try
                {
                    if (string.IsNullOrEmpty(installed.Error))
                    {
                        Capture(report.Cases.Count + 100, "options-installed-mods", "native", optionsRoot, null, installed);
                        installed.Rendered = true;
                        installed.Status = "PASS";
                        installed.Note = "Captured installed mods tab.";
                    }
                }
                catch (Exception ex) { installed.Error = "Screenshot: " + ex.Message; }
                SaveReport();
            }
            // Close the temporarily opened menu. We do not Apply anything.
            try
            {
                if (!wasActive) menu.DoResume();
                else menu.ShowMainMenu();
            }
            catch (Exception ex) { Logger.LogWarning("[FOUNDATION UI SMOKE] Could not restore in-game menu: " + ex.Message); }
        }

        private static RectTransform LocatePanel()
        {
            var panel = GameObject.Find("FoundationModPanel");
            return panel ? panel.GetComponent<RectTransform>() : null;
        }

        private static void ValidateStructure(RectTransform root, AuditSpec spec)
        {
            var row = FindRow(root, spec.RowName);
            if (!row) throw new InvalidOperationException("Expected active element row not found: " + spec.RowName);
            if (Screen.width <= 0 || Screen.height <= 0) throw new InvalidOperationException("Screen dimensions are unavailable.");
            if (root.rect.width < 200 || root.rect.height < 100) throw new InvalidOperationException("Popup layout is too small.");
            if (!root.GetComponentsInChildren<TMP_Text>(true).Any(t => t.gameObject.activeInHierarchy))
                throw new InvalidOperationException("The popup has no active native text elements.");
        }

        private static RectTransform FindRow(RectTransform root, string rowName)
        {
            if (string.IsNullOrEmpty(rowName)) return null;
            foreach (var row in root.GetComponentsInChildren<RectTransform>(true))
                if (row.name == rowName && row.gameObject.activeInHierarchy) return row;
            return null;
        }

        private static Button FindButton(RectTransform root, string text)
        {
            return root.GetComponentsInChildren<Button>(true).FirstOrDefault(button =>
                button.gameObject.activeInHierarchy && button.GetComponentInChildren<TMP_Text>(true)?.text == text);
        }

        private bool Exercise(RectTransform root, string key, out string detail)
        {
            detail = "Callback was not exercised.";
            switch (key)
            {
                case "button":
                    var button = FindButton(root, "Test action");
                    if (!button) { detail = "Test action button missing."; return false; }
                    int before = actions;
                    button.onClick.Invoke();
                    detail = "Button callback fired " + (actions - before) + " time(s).";
                    return actions == before + 1;
                case "toggle":
                    var toggle = root.GetComponentInChildren<Toggle>(true);
                    if (!toggle) { detail = "Toggle missing."; return false; }
                    toggle.isOn = !toggle.isOn;
                    detail = "Toggle callback updated value=" + toggleValue;
                    return toggleValue == toggle.isOn;
                case "slider":
                    var slider = root.GetComponentInChildren<UnityEngine.UI.Slider>(true);
                    if (!slider) { detail = "Slider missing."; return false; }
                    slider.value = 7.5f;
                    detail = "Slider callback updated value=" + sliderValue;
                    return Mathf.Abs(sliderValue - 7.5f) < .01f;
                case "number":
                    var number = root.GetComponentInChildren<TMP_InputField>(true);
                    if (!number) { detail = "Number input missing."; return false; }
                    number.SetTextWithoutNotify("17.5");
                    number.onEndEdit.Invoke("17.5");
                    detail = "Number callback updated value=" + numericValue;
                    return Math.Abs(numericValue - 17.5) < .001;
                case "input":
                    var input = root.GetComponentInChildren<TMP_InputField>(true);
                    if (!input) { detail = "Text input missing."; return false; }
                    input.SetTextWithoutNotify("UI Audit Text");
                    input.onEndEdit.Invoke("UI Audit Text");
                    detail = "Text callback updated value=" + inputValue;
                    return inputValue == "UI Audit Text";
                case "dropdown":
                    var list = root.GetComponentInChildren<TMP_Dropdown>(true);
                    if (!list) { detail = "Dropdown missing."; return false; }
                    list.value = 2;
                    detail = "Dropdown callback selected index " + dropdownValue;
                    return dropdownValue == 2;
                case "search":
                    var search = root.GetComponentInChildren<TMP_InputField>(true);
                    if (!search) { detail = "Search text input missing."; return false; }
                    search.text = "Emerald";
                    var result = FindRow(root, "SearchResult");
                    var select = result ? result.GetComponentInChildren<Button>(true) : null;
                    if (!select) { detail = "Search result button missing after filtering."; return false; }
                    select.onClick.Invoke();
                    detail = "Search filter selected index " + searchValue;
                    return searchValue == 4;
                case "colors":
                    var colorRow = FindRow(root, "Color2");
                    var colorButton = colorRow ? colorRow.GetComponent<Button>() : null;
                    if (!colorButton) { detail = "Blue color swatch missing."; return false; }
                    colorButton.onClick.Invoke();
                    detail = "Swatch callback selected index " + colorValue;
                    return colorValue == 2;
                case "signal-source":
                    var sourceSelect = root.GetComponentInChildren<TMP_Dropdown>(true);
                    if (!sourceSelect) { detail = "Signal source dropdown missing."; return false; }
                    var sources = FoundationApi.Signals.Sources;
                    var sourceIndex = -1;
                    for (int i = 0; i < sources.Count; i++) if (sources[i].Id == syntheticSignal.Id) sourceIndex = i + 1;
                    if (sourceIndex < 0) { detail = "Test signal source missing."; return false; }
                    sourceSelect.value = sourceIndex;
                    detail = "Signal source callback selected " + signalValue?.SourceId;
                    return signalValue?.SourceId == syntheticSignal.Id;
                case "signal-property":
                    var selects = root.GetComponentsInChildren<TMP_Dropdown>(true).Where(d => d.gameObject.activeInHierarchy).ToArray();
                    if (selects.Length < 2) { detail = "Signal source and property dropdowns not both visible."; return false; }
                    selects[1].value = 1;
                    detail = "Signal property callback selected " + signalValue?.PropertyId;
                    return signalValue?.PropertyId == "enabled";
                case "tabs":
                    var advanced = FindButton(root, "Advanced");
                    if (!advanced) advanced = FindButton(root, "[Advanced]");
                    if (!advanced) { detail = "Advanced tab button missing."; return false; }
                    advanced.onClick.Invoke();
                    detail = "Tabbed callback rendered index " + tabValue;
                    return tabValue == 1;
                case "section":
                    sectionRendered = false;
                    var collapse = FindButton(root, "v Details") ?? FindButton(root, "> Details");
                    if (!collapse) { detail = "Collapsible section header missing."; return false; }
                    bool wasExpanded = collapse.GetComponentInChildren<TMP_Text>(true)?.text == "v Details";
                    collapse.onClick.Invoke();
                    detail = "Section changed expanded state " + wasExpanded + " -> " + sectionRendered;
                    return sectionRendered != wasExpanded;
                case "reorder":
                    var rows = root.GetComponentsInChildren<RectTransform>(true)
                        .Where(r => r.name == "ReorderRow" && r.gameObject.activeInHierarchy).ToArray();
                    if (rows.Length < 2) { detail = "Reorder has fewer than two rows."; return false; }
                    var drag = rows[0].GetComponents<MonoBehaviour>().OfType<IBeginDragHandler>().FirstOrDefault();
                    var drop = rows[1].GetComponents<MonoBehaviour>().OfType<IDropHandler>().FirstOrDefault();
                    if (drag == null || drop == null) { detail = "Reorder drag/drop handlers missing."; return false; }
                    if (EventSystem.current == null) { detail = "Unity EventSystem is unavailable."; return false; }
                    string first = reordered[0], second = reordered[1];
                    var eventData = new PointerEventData(EventSystem.current);
                    drag.OnBeginDrag(eventData);
                    drop.OnDrop(eventData);
                    detail = "Reordered first two rows: " + string.Join(" -> ", reordered);
                    return reordered[0] == second && reordered[1] == first;
                case "confirm-cancel":
                    var cancel = FindButton(root, "Cancel");
                    if (!cancel) { detail = "Confirmation cancel button missing."; return false; }
                    var rejectedBefore = rejected;
                    cancel.onClick.Invoke();
                    detail = "Confirmation cancel callback count: " + (rejected - rejectedBefore);
                    return rejected == rejectedBefore + 1;
                case "confirm":
                    // The runner opened and screenshotted the confirmation prompt.
                    var accept = FindButton(root, "Confirm");
                    if (!accept) { detail = "Confirmation accept button missing."; return false; }
                    var previous = confirms;
                    accept.onClick.Invoke();
                    detail = "Confirmation accept callback count: " + (confirms - previous);
                    return confirms == previous + 1;
                case "property":
                    var property = root.GetComponentInChildren<Toggle>(true);
                    if (!property) { detail = "Editable property toggle missing."; return false; }
                    property.isOn = !property.isOn;
                    detail = "Dynamic property setter value=" + propertyValue;
                    return propertyValue == property.isOn;
                case "tooltip":
                    var hoverButton = FindButton(root, "Hover for tooltip");
                    if (!hoverButton) { detail = "Tooltip target button missing."; return false; }
                    // Unity's Button/Selectable also implements IPointerEnterHandler; selecting
                    // the first handler probes the Button rather than Foundation's tooltip.
                    // Explicitly target the Foundation handler (internal to the main DLL).
                    var hover = hoverButton.GetComponents<MonoBehaviour>()
                        .OfType<IPointerEnterHandler>()
                        .FirstOrDefault(h => h.GetType().Name == "FoundationPanelTooltip");
                    if (hover == null || EventSystem.current == null) { detail = "Tooltip pointer handler or Unity EventSystem missing."; return false; }
                    hover.OnPointerEnter(new PointerEventData(EventSystem.current));
                    bool shown = root.GetComponentsInChildren<TMP_Text>(true)
                        .Any(t => t.gameObject.activeInHierarchy && t.text == "Tooltip copied from native text style");
                    detail = "Tooltip status text rendered=" + shown;
                    return shown;
                case "scroll":
                    var scroll = root.GetComponentInChildren<ScrollRect>(true);
                    if (!scroll) { detail = "ScrollRect missing."; return false; }
                    scroll.verticalNormalizedPosition = 0f;
                    detail = "Scrolling to bottom: position=" + scroll.verticalNormalizedPosition;
                    return scroll.verticalNormalizedPosition < .05f;
                default:
                    detail = "No input driver for control: " + key;
                    return false;
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static float ColorLuminance(Color c)
        {
            float r = c.r <= .04045f ? c.r / 12.92f : Mathf.Pow((c.r + .055f) / 1.055f, 2.4f);
            float g = c.g <= .04045f ? c.g / 12.92f : Mathf.Pow((c.g + .055f) / 1.055f, 2.4f);
            float b = c.b <= .04045f ? c.b / 12.92f : Mathf.Pow((c.b + .055f) / 1.055f, 2.4f);
            return .2126f * r + .7152f * g + .0722f * b;
        }

        private static float ColorContrast(Color a, Color b)
        {
            float aa = ColorLuminance(a), bb = ColorLuminance(b);
            return (Mathf.Max(aa, bb) + .05f) / (Mathf.Min(aa, bb) + .05f);
        }

        private void InspectFoundationTextLayout(RectTransform root, UiAuditResult result)
        {
            // Do not report the vanilla Options UI's geometry as a Foundation bug.
            if (!root || root.name != "FoundationModPanel") return;
            var bounds = ScreenRect(root);
            // The dark fallback can keep text readable while still looking
            // foreign next to native gold-trimmed Options. Flag missing native
            // sprite styles for a required human comparison against Options.
            if (!root.GetComponentsInChildren<Image>(true).Any(g => g && g.name == "NativeGameFrame" && g.sprite))
                AddVisualIssue(result, "Native decorative panel sprite unavailable; simple border fallback in use.");
            foreach (var nativeButton in root.GetComponentsInChildren<Button>(true))
            {
                if (!nativeButton || !nativeButton.gameObject.activeInHierarchy) continue;
                var graphic = nativeButton.targetGraphic as Image;
                if (graphic && !graphic.sprite)
                    AddVisualIssue(result, "Native button sprite unavailable: " + nativeButton.name);
            }
            var visible = new List<Tuple<TMP_Text, Rect>>();
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!label || !label.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(label.text)) continue;
                var box = ScreenRect(label.rectTransform);
                if (box.width < 1 || box.height < 1)
                {
                    AddVisualIssue(result, "Zero-size text: " + label.text);
                    continue;
                }
                if (!box.Overlaps(bounds)) continue;
                var scroll = label.GetComponentInParent<ScrollRect>();
                if (scroll && scroll.content && label.transform.IsChildOf(scroll.content) &&
                    scroll.viewport && !box.Overlaps(ScreenRect(scroll.viewport))) continue;
                if (label.fontSize < 12f || label.fontSize > 25f)
                    AddVisualIssue(result, "Font outside visual scale 12-25: " + label.text);
                // Check against both of Foundation's measured native Options
                // backgrounds: report only when text has insufficient contrast
                // with *both* dark surfaces, minimizing false positives.
                var panelInk = new Color(38f / 255f, 26f / 255f, 35f / 255f, 1f);
                var wellInk = new Color(26f / 255f, 18f / 255f, 26f / 255f, 1f);
                if (ColorContrast(label.color, panelInk) < 4.5f && ColorContrast(label.color, wellInk) < 4.5f)
                    AddVisualIssue(result, "Possible low-contrast text: " + label.text);
                if (box.xMin < bounds.xMin - 4 || box.xMax > bounds.xMax + 4 ||
                    box.yMin < bounds.yMin - 4 || box.yMax > bounds.yMax + 4)
                    AddVisualIssue(result, "Text rectangle escapes panel: " + label.text);
                visible.Add(Tuple.Create(label, box));
            }
            // Detect distinct text elements whose screen rectangles materially
            // collide. Ignore nested elements, clipping masks, and tiny contact.
            for (int i = 0; i < visible.Count; i++)
            {
                for (int j = i + 1; j < visible.Count; j++)
                {
                    var a = visible[i]; var b = visible[j];
                    if (a.Item1.transform.IsChildOf(b.Item1.transform) ||
                        b.Item1.transform.IsChildOf(a.Item1.transform)) continue;
                    float width = Mathf.Min(a.Item2.xMax, b.Item2.xMax) - Mathf.Max(a.Item2.xMin, b.Item2.xMin);
                    float height = Mathf.Min(a.Item2.yMax, b.Item2.yMax) - Mathf.Max(a.Item2.yMin, b.Item2.yMin);
                    if (width < 5f || height < 5f) continue;
                    float smallerArea = Mathf.Min(a.Item2.width * a.Item2.height, b.Item2.width * b.Item2.height);
                    if (smallerArea > 0 && width * height > smallerArea * .30f)
                        AddVisualIssue(result, "Potential text overlap: '" + a.Item1.text + "' / '" + b.Item1.text + "'");
                }
            }
        }

        private void InspectModSettingsControlLayout(RectTransform optionsRoot, string title, UiAuditResult result)
        {
            if (title == "Number" || title == "Input")
            {
                var fields = optionsRoot.GetComponentsInChildren<TMP_InputField>(true)
                    .Where(input => input && input.gameObject.activeInHierarchy && input.name == "ConfigInput")
                    .ToArray();
                if (fields.Length == 0)
                    AddVisualIssue(result, "Native Options text input is missing.");
                foreach (var input in fields)
                {
                    if (!input.textViewport || !input.textComponent)
                    {
                        AddVisualIssue(result, "Native Options text input has no clipped text viewport.");
                        continue;
                    }
                    var outer = ScreenRect(input.GetComponent<RectTransform>());
                    var inner = ScreenRect(input.textViewport);
                    if (inner.xMin - outer.xMin < 16f || outer.xMax - inner.xMax < 16f)
                        AddVisualIssue(result, "Native Options input text crowds decorative border (gutter under 16px).");
                    if (input.textComponent.textWrappingMode != TextWrappingModes.NoWrap)
                        AddVisualIssue(result, "Native Options input text is not constrained to a single line.");
                    // The input should use a dark recessed native well, not
                    // the Apply/Cancel button graphic with pointed gold sides.
                    var background = input.GetComponent<Image>();
                    if (!background || background.sprite)
                        AddVisualIssue(result, "Native Options input is using a button-like image instead of a recessed well.");
                    if (outer.width < 80f || outer.height < 30f)
                        AddVisualIssue(result, "Native Options input has insufficient room for readable text.");
                }
            }
            if (title == "Toggle")
            {
                foreach (var toggle in optionsRoot.GetComponentsInChildren<Toggle>(true))
                {
                    if (!toggle || !toggle.gameObject.activeInHierarchy || toggle.name != "ConfigToggle") continue;
                    var checkbox = toggle.targetGraphic;
                    if (!checkbox || ScreenRect(checkbox.rectTransform).width < 22f ||
                        ScreenRect(checkbox.rectTransform).height < 22f)
                        AddVisualIssue(result, "Native Options checkbox art is too small (under 22px).");
                }
            }
        }

        private void InspectFoundationToggleLayout(RectTransform root, UiAuditResult result)
        {
            foreach (var toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                if (!toggle || !toggle.gameObject.activeInHierarchy) continue;
                var checkbox = toggle.targetGraphic;
                if (!checkbox || ScreenRect(checkbox.rectTransform).width < 22f ||
                    ScreenRect(checkbox.rectTransform).height < 22f)
                    AddVisualIssue(result, "Foundation panel checkbox art is too small (under 22px).");
                var label = toggle.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(t => t && t.gameObject.activeInHierarchy && t.text == "Native checkbox");
                if (label && checkbox)
                {
                    var caption = ScreenRect(label.rectTransform);
                    var box = ScreenRect(checkbox.rectTransform);
                    if (caption.xMin < box.xMax + 6f)
                        AddVisualIssue(result, "Toggle text collides with the checkbox artwork.");
                    if (caption.height < 22f)
                        AddVisualIssue(result, "Toggle text area is too short for the native pixel font.");
                }
            }
        }

        private void InspectModSettingsSignalLayout(RectTransform optionsRoot, UiAuditResult result)
        {
            // The runtime selector is a plain GameObject, not a component;
            // inspect only its explicitly named FoundationSignalSelector host.
            foreach (var group in optionsRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (!group || !group.gameObject.activeInHierarchy || group.name != "FoundationSignalSelector") continue;
                foreach (var dropdown in group.GetComponentsInChildren<TMP_Dropdown>(true))
                {
                    if (!dropdown || !dropdown.gameObject.activeInHierarchy || !dropdown.captionText) continue;
                    var caption = dropdown.captionText;
                    caption.ForceMeshUpdate();
                    if (caption.textInfo.lineCount > 1)
                        AddVisualIssue(result, "Mod Settings signal caption wraps across multiple lines: " + caption.text);
                    if (caption.textWrappingMode != TextWrappingModes.NoWrap)
                        AddVisualIssue(result, "Mod Settings signal caption permits unwanted line wrapping.");
                    if ((caption.text ?? "").IndexOf("urakka.foundation.example.ui-smoke.test-source", StringComparison.Ordinal) >= 0)
                        AddVisualIssue(result, "Mod Settings signal caption exposes a raw internal source ID.");
                    if ((caption.text ?? "").Length > 28)
                        AddVisualIssue(result, "Mod Settings signal caption is too long for the native Options well.");
                    var row = dropdown.GetComponent<RectTransform>();
                    var outer = ScreenRect(row);
                    var textBounds = ScreenRect(caption.rectTransform);
                    if (textBounds.xMin < outer.xMin + 5f || textBounds.xMax > outer.xMax - 10f)
                        AddVisualIssue(result, "Mod Settings signal caption intrudes into the dropdown frame or arrow.");
                }
            }
        }

        private void AddVisualIssue(UiAuditResult result, string issue)
        {
            if (result.VisualIssues.Count >= 20 || result.VisualIssues.Contains(issue)) return;
            result.VisualIssues.Add(issue);
            Logger.LogWarning("[FOUNDATION UI VISUAL QA] " + result.Name + ": " + issue);
        }

        private void Capture(int number, string key, string state, RectTransform root, string rowName, UiAuditResult result)
        {
            // Report potential geometric text collisions even when callbacks pass.
            // This is a conservative machine check; a human still must inspect
            // each PNG against the native Options UI reference before approval.
            InspectFoundationTextLayout(root, result);
            if (key == "toggle") InspectFoundationToggleLayout(root, result);
            var prefix = number.ToString("000", CultureInfo.InvariantCulture) + "_" + key + "_" + state;
            var frame = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                frame.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                frame.Apply(false, false);
                var full = prefix + "_screen.png";
                File.WriteAllBytes(Path.Combine(directory, full), ImageConversion.EncodeToPNG(frame));
                result.Screenshots.Add(full);
                SaveCrop(frame, root, prefix + "_panel.png", result);
                var element = FindRow(root, rowName);
                if (element) SaveCrop(frame, element, prefix + "_element.png", result);
            }
            finally { Destroy(frame); }
        }

        private void SaveCrop(Texture2D frame, RectTransform rect, string name, UiAuditResult result)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            var p0 = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var p2 = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            int left = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p0.x, p2.x)) - 8, 0, frame.width - 1);
            int bottom = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p0.y, p2.y)) - 8, 0, frame.height - 1);
            int right = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p0.x, p2.x)) + 8, 1, frame.width);
            int top = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p0.y, p2.y)) + 8, 1, frame.height);
            if (right <= left || top <= bottom) throw new InvalidOperationException("Off-screen or empty control bounds: " + name);
            var cropped = new Texture2D(right - left, top - bottom, TextureFormat.RGB24, false);
            try
            {
                cropped.SetPixels(frame.GetPixels(left, bottom, right - left, top - bottom));
                cropped.Apply(false, false);
                File.WriteAllBytes(Path.Combine(directory, name), ImageConversion.EncodeToPNG(cropped));
                result.Screenshots.Add(name);
            }
            finally { Destroy(cropped); }
        }

        private void SaveReport()
        {
            try
            {
                var reportFile = Path.Combine(directory, "ui-smoke-results.json");
                File.WriteAllText(reportFile, SerializeReport(report), new UTF8Encoding(false));
            }
            catch (Exception ex) { Logger.LogError("[FOUNDATION UI SMOKE] Failed writing result manifest: " + ex); }
        }

        // Unity JsonUtility is part of UnityEngine.JSONSerializeModule on newer
        // Unity builds. The optional verifier intentionally uses no extra Unity
        // serialization DLL: its manifest is plain, escaped UTF-8 JSON.
        // Keep field names stable for Tests/Read-UISmokeReport.ps1.
        private static string SerializeReport(UiAuditReport value)
        {
            var b = new StringBuilder(4096);
            b.AppendLine("{");
            AppendJsonField(b, 2, "StartedUtc", value.StartedUtc);
            AppendJsonField(b, 2, "FinishedUtc", value.FinishedUtc);
            AppendJsonField(b, 2, "FoundationVersion", value.FoundationVersion);
            AppendJsonField(b, 2, "GameResolution", value.GameResolution);
            AppendJsonField(b, 2, "ScreenshotFolder", value.ScreenshotFolder);
            b.AppendLine("  \"Cases\": [");
            for (int i = 0; i < value.Cases.Count; i++)
            {
                UiAuditResult c = value.Cases[i];
                b.AppendLine("    {");
                AppendJsonField(b, 6, "Name", c.Name);
                AppendJsonField(b, 6, "Kind", c.Kind);
                AppendJsonField(b, 6, "Status", c.Status);
                AppendJsonBool(b, 6, "Rendered", c.Rendered);
                AppendJsonBool(b, 6, "Interacted", c.Interacted);
                AppendJsonField(b, 6, "Note", c.Note);
                AppendJsonField(b, 6, "Error", c.Error);
                b.Append("      \"VisualIssues\": [");
                for (int j = 0; j < c.VisualIssues.Count; j++)
                {
                    if (j != 0) b.Append(", ");
                    AppendJsonString(b, c.VisualIssues[j]);
                }
                b.AppendLine("],");
                b.Append("      \"Screenshots\": [");
                for (int j = 0; j < c.Screenshots.Count; j++)
                {
                    if (j != 0) b.Append(", ");
                    AppendJsonString(b, c.Screenshots[j]);
                }
                b.AppendLine("]");
                b.Append("    }");
                if (i < value.Cases.Count - 1) b.Append(',');
                b.AppendLine();
            }
            b.AppendLine("  ]");
            b.AppendLine("}");
            return b.ToString();
        }

        private static void AppendJsonField(StringBuilder b, int spaces, string key, string value)
        {
            b.Append(' ', spaces);
            AppendJsonString(b, key);
            b.Append(": ");
            AppendJsonString(b, value);
            b.AppendLine(",");
        }

        private static void AppendJsonBool(StringBuilder b, int spaces, string key, bool value)
        {
            b.Append(' ', spaces);
            AppendJsonString(b, key);
            b.Append(": ").Append(value ? "true" : "false").AppendLine(",");
        }

        private static void AppendJsonString(StringBuilder b, string value)
        {
            if (value == null) { b.Append("null"); return; }
            b.Append('"');
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\b': b.Append("\\b"); break;
                    case '\f': b.Append("\\f"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (ch < ' ' || ch == '\u2028' || ch == '\u2029' || char.IsSurrogate(ch))
                            b.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else b.Append(ch);
                        break;
                }
            }
            b.Append('"');
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            if (running) Logger.LogWarning("[FOUNDATION UI SMOKE] Interrupted by plugin unload.");
            startButton?.Dispose();
            scope?.Dispose();
            foreach (var section in settingSpecs) FoundationApi.ModConfig.RemoveSection(section.Id);
            syntheticSignal?.Dispose();
        }

        private sealed class AuditSpec
        {
            internal string Id;
            internal string Key;
            internal string Title;
            internal string RowName;
            internal bool Interactive;
        }

        private sealed class SettingsAuditSpec
        {
            internal string Id;
            internal string Title;
            internal string Label;
        }

        [Serializable]
        private sealed class UiAuditReport
        {
            public string StartedUtc;
            public string FinishedUtc;
            public string FoundationVersion;
            public string GameResolution;
            public string ScreenshotFolder;
            public List<UiAuditResult> Cases = new List<UiAuditResult>();
        }

        [Serializable]
        private sealed class UiAuditResult
        {
            public string Name;
            public string Kind;
            public string Status;
            public bool Rendered;
            public bool Interacted;
            public string Note;
            public string Error;
            public List<string> Screenshots = new List<string>();
            public List<string> VisualIssues = new List<string>();
        }
    }
}
