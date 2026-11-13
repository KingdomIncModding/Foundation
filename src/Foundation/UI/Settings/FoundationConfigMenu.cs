using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.MainMenu;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    public sealed class FoundationConfigMenu : MonoBehaviour, IOptionsView, IVanillaSectionProvider
    {
        OptionsUI optionsUI;
        RectTransform rootRect;
        Image rootImage;
        // Header and buttons
        RectTransform header;
        Button applyButton, cancelButton, closeButton;
        // Tabs
        RectTransform tabContainer;
        Button gameTabButton, modsTabButton, installedTabButton;
        TMP_Text gameTabText;
        bool isModsTabActive;
        bool isInstalledTabActive;
        // Pages
        GameObject gamePage;
        GameObject modsPage;
        GameObject installedPage;
        // Installed Page UI
        RectTransform installedContent;
        ScrollRect installedScroll;
        TMP_Text installedSummaryText;
        Button checkUpdatesButton;
        Button openFolderButton;
        readonly List<GameObject> installedModRowObjects = new List<GameObject>();
        // Mods Page UI
        RectTransform modsSidebarContent;
        RectTransform modsSettingsContent;
        ScrollRect modsSidebarScroll;
        ScrollRect modsSettingsScroll;
        TMP_Text modsHeaderTitle;
        TMP_Text modsHeaderDesc;
        TMP_Text noModsText;
        readonly Dictionary<string, Button> modButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        string selectedModId;
        // Templates
        ModConfigTemplates templates;
        public IModConfigTemplates Templates => templates;

        OptionsTabStrip tabStrip;
        OptionsMenuBrand brand;
        readonly Dictionary<IOptionsTabRegistration, OptionsTabPage> customPages = new Dictionary<IOptionsTabRegistration, OptionsTabPage>();
        readonly Dictionary<string, IVanillaSettingView> vanillaRows = new Dictionary<string, IVanillaSettingView>(StringComparer.Ordinal);
        readonly Dictionary<string, VanillaSectionView> vanillaSections = new Dictionary<string, VanillaSectionView>(StringComparer.Ordinal);
        readonly List<IModConfigSection> editSections = new List<IModConfigSection>();
        IOptionsTabRegistration selectedTab;
        public bool IsOpen => built && isActiveAndEnabled;
        public IReadOnlyList<IVanillaSettingView> VanillaSettings => vanillaRows.Values.ToArray();

        public bool TryGetVanillaSetting(string id, out IVanillaSettingView setting)
        {
            setting = null;
            return id != null && vanillaRows.TryGetValue(id, out setting);
        }

        public IReadOnlyList<IVanillaSectionView> VanillaSections => vanillaSections.Values.Cast<IVanillaSectionView>().ToArray();

        public bool TryGetVanillaSection(string id, out IVanillaSectionView section)
        {
            section = null;
            if (id == null || !vanillaSections.TryGetValue(id, out var found))
                return false;
            section = found;
            return true;
        }

        bool built;
        bool modEditSessionOpen;
        public static FoundationConfigMenu Attach(OptionsUI ui)
        {
            if (ui == null)
                return null;
            var menu = ui.GetComponent<FoundationConfigMenu>() ?? ui.gameObject.AddComponent<FoundationConfigMenu>();
            menu.Build(ui);
            return menu;
        }

        public void Build(OptionsUI source)
        {
            if (built)
                return;
            optionsUI = source;
            rootRect = (RectTransform)transform;
            rootImage = GetComponent<Image>();
            // Ensure OptionsUI._parent and _ingameParent are populated even if Start() hasn't executed yet
            var parentField = AccessTools.Field(typeof(OptionsUI), "_parent");
            var ingameField = AccessTools.Field(typeof(OptionsUI), "_ingameParent");
            if (parentField != null && parentField.GetValue(source) == null)
                parentField.SetValue(source, source.GetComponentInParent<MainMenuUI>() ?? MainMenuUI.Instance);
            if (ingameField != null && ingameField.GetValue(source) == null)
                ingameField.SetValue(source, source.GetComponentInParent<InGameMenuUI>() ?? global::UnityEngine.Object.FindAnyObjectByType<InGameMenuUI>());
            templates = new ModConfigTemplates(source);
            // 1. Sizing and centering: 3x bigger area (~1100 x 700)
            AdjustWindowTransform();
            // 2. Identify and reposition vanilla buttons & header
            FindAndRepositionNativeControls();
            // 3. Create top tabs: [ Game Settings ]  [ Mod Settings ]
            CreateTabs();
            // 4. Rearrange Game Page into 2 columns
            BuildGamePage();
            // 5. Build Mods Page (Sidebar + Scrollable settings panel)
            BuildModsPage();
            // 6. Build Installed Mods Page
            BuildInstalledPage();
            // Hook API change events
            FoundationApi.ModConfig.Changed += OnRegistryChanged;
            FoundationApi.Options.Changed += OnOptionsChanged;
            NexusUpdateChecker.Changed += OnInstalledModsChanged;
            built = true;
            brand = new OptionsMenuBrand(source);
            if (isActiveAndEnabled)
                brand.Show();
            OnOptionsChanged();
            if (isActiveAndEnabled)
                BeginModConfigSession();
            ShowGameTab();
        }

        void OnDestroy()
        {
            CancelModConfigs();
            FoundationApi.ModConfig.Changed -= OnRegistryChanged;
            FoundationApi.Options.Changed -= OnOptionsChanged;
            ((OptionsTabRegistry)FoundationApi.Options).Close(this);
            foreach (var page in customPages.Values)
                page.Dispose();
            customPages.Clear();
            tabStrip?.Dispose();
            brand?.Dispose();
            NexusUpdateChecker.Changed -= OnInstalledModsChanged;
        }

        void OnEnable()
        {
            if (!built && optionsUI != null)
                Build(optionsUI);
            if (built)
                BeginModConfigSession();
            AdjustWindowTransform();
            brand?.Show();
            if (selectedTab != null && selectedTab.IsRegistered)
                ShowCustomTab(selectedTab);
            else if (isInstalledTabActive)
                ShowInstalledTab();
            else if (isModsTabActive)
                ShowModsTab();
            else
                ShowGameTab();
        }

        void OnDisable()
        {
            // The native Options panel can also be hidden by navigation/close.
            // Discard any staged values if neither Apply nor Cancel was invoked.
            CancelModConfigs();
            ((OptionsTabRegistry)FoundationApi.Options).Close(this);
            ResetVanillaPresentation();
            brand?.Hide();
        }

        void BeginModConfigSession()
        {
            if (modEditSessionOpen)
                return;
            modEditSessionOpen = true;
            SynchronizeEditSections();
            ResetVanillaPresentation();
            ((OptionsTabRegistry)FoundationApi.Options).Open(this);
        }

        void SynchronizeEditSections()
        {
            if (!modEditSessionOpen)
                return;
            var sections = FoundationApi.ModConfig.Sections.Concat(FoundationApi.Options.Tabs.Select(t => t.Tab.Settings)).ToArray();
            foreach (var old in editSections.Where(s => !sections.Any(current => ReferenceEquals(current, s))).ToArray())
            {
                editSections.Remove(old);
                try
                {
                    old.Cancel();
                }
                catch (Exception error)
                {
                    FoundationApi.Report(error);
                }
            }

            foreach (var section in sections)
            {
                if (editSections.Any(current => ReferenceEquals(current, section)))
                    continue;
                editSections.Add(section);
                if (section is ModConfigSection concrete)
                    concrete.BeginEdit();
            }
        }

        void ResetVanillaPresentation()
        {
            foreach (var section in vanillaSections.Values)
                section.IsVisible = true;
            foreach (var row in vanillaRows.Values)
                row.IsVisible = true;
            UpdateVanillaLayout();
        }

        void UpdateVanillaLayout()
        {
            foreach (var section in vanillaSections.Values)
            {
                if (!section.IsAlive)
                    continue;
                section.RefreshVisibility();
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)section.Container);
            }
        }

        void OnOptionsChanged()
        {
            if (!built)
                return;
            SynchronizeEditSections();
            var tabs = FoundationApi.Options.Tabs;
            foreach (var removed in customPages.Keys.Where(t => !tabs.Contains(t)).ToArray())
            {
                customPages[removed].Dispose();
                customPages.Remove(removed);
            }

            foreach (var tab in tabs)
                if (!customPages.ContainsKey(tab))
                    customPages.Add(tab, new OptionsTabPage(transform, tab.Tab, templates));
            tabStrip.Rebuild(tabs, SelectBuiltinTab, ShowCustomTab);
            gameTabButton = tabStrip.Buttons["game"];
            modsTabButton = tabStrip.Buttons["mods"];
            installedTabButton = tabStrip.Buttons["installed"];
            gameTabText = gameTabButton.GetComponentInChildren<TMP_Text>();
            if (selectedTab != null)
            {
                if (selectedTab.IsRegistered)
                    ShowCustomTab(selectedTab);
                else
                    ShowGameTab();
            }
        }

        void SelectBuiltinTab(string id)
        {
            if (id == "mods")
                ShowModsTab();
            else if (id == "installed")
                ShowInstalledTab();
            else
                ShowGameTab();
        }

        public void ShowCustomTab(IOptionsTabRegistration tab)
        {
            if (tab == null || !tab.IsRegistered || !customPages.TryGetValue(tab, out var page))
                return;
            selectedTab = tab;
            isModsTabActive = isInstalledTabActive = false;
            gamePage.SetActive(false);
            modsPage.SetActive(false);
            installedPage.SetActive(false);
            foreach (var pair in customPages)
                pair.Value.SetVisible(pair.Key == tab);
            tabStrip.Select(OptionsTabStrip.Key(tab));
        }

        void HideCustomPages()
        {
            selectedTab = null;
            foreach (var page in customPages.Values)
                page.SetVisible(false);
        }

        void LateUpdate()
        {
            tabStrip?.Update();
            if (selectedTab != null && customPages.TryGetValue(selectedTab, out var page))
                page.Refresh();
        }

        void AdjustWindowTransform()
        {
            if (!rootRect)
                return;
            var canvas = GetComponentInParent<Canvas>();
            float canvasW = canvas ? canvas.pixelRect.width / canvas.scaleFactor : Screen.width;
            float canvasH = canvas ? canvas.pixelRect.height / canvas.scaleFactor : Screen.height;
            float targetW = Mathf.Min(1140f, canvasW - 40f);
            float targetH = Mathf.Min(720f, canvasH - 40f);
            rootRect.sizeDelta = new Vector2(targetW, targetH);
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
            // If father has constrained anchorMax.y (like MainMenu spanning 0..0.6), offset to center on screen
            var parentRect = transform.parent as RectTransform;
            if (parentRect != null && parentRect.anchorMax.y < 0.9f)
            {
                float yOffset = (0.5f - parentRect.anchorMax.y * 0.5f) * canvasH;
                rootRect.anchoredPosition = new Vector2(0, yOffset);
            }
            else
            {
                rootRect.anchoredPosition = Vector2.zero;
            }
        }

        void FindAndRepositionNativeControls()
        {
            // Header (Title Bar) - centered badge matching Image 2
            var headerTrans = transform.Find("Header") as RectTransform;
            if (headerTrans)
            {
                header = headerTrans;
                header.anchorMin = new Vector2(0.5f, 1f);
                header.anchorMax = new Vector2(0.5f, 1f);
                header.pivot = new Vector2(0.5f, 0.5f);
                header.anchoredPosition = new Vector2(0, 0); // Perched centered on top border line
                var headerText = header.GetComponentInChildren<TMP_Text>(true);
                float textWidth = headerText != null ? headerText.preferredWidth : 100f;
                float badgeWidth = Mathf.Max(170f, textWidth + 48f);
                header.sizeDelta = new Vector2(badgeWidth, 42f);
                if (headerText != null)
                {
                    headerText.alignment = TextAlignmentOptions.Center;
                    headerText.fontSize = 20;
                    headerText.enableAutoSizing = true;
                    headerText.fontSizeMin = 14;
                    headerText.fontSizeMax = 22;
                    var htRect = headerText.rectTransform;
                    htRect.anchorMin = Vector2.zero;
                    htRect.anchorMax = Vector2.one;
                    htRect.offsetMin = new Vector2(16, 0);
                    htRect.offsetMax = new Vector2(-16, 0);
                }
            }

            // Close Button
            var closeTrans = transform.Find("CloseButton");
            if (closeTrans)
            {
                closeButton = closeTrans.GetComponent<Button>();
                var r = (RectTransform)closeTrans;
                r.anchorMin = r.anchorMax = new Vector2(1, 1);
                r.pivot = new Vector2(1, 1);
                r.anchoredPosition = new Vector2(-16, -12);
            }

            // Apply and Cancel
            var applyTrans = transform.Find("Apply");
            if (applyTrans)
            {
                applyButton = applyTrans.GetComponent<Button>();
                var r = (RectTransform)applyTrans;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(1, 0);
                r.anchoredPosition = new Vector2(-40, 20);
                r.sizeDelta = new Vector2(180, 44); // Taller: 52px
                var text = applyButton.GetComponentInChildren<TMP_Text>(true);
                if (text)
                {
                    text.alignment = TextAlignmentOptions.Center;
                    text.rectTransform.anchorMin = Vector2.zero;
                    text.rectTransform.anchorMax = Vector2.one;
                    text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
                }
            // Apply/Cancel are handled by OptionsUI Harmony prefixes only.
            // Adding listeners here invoked commits twice for the same click.
            }

            var cancelTrans = transform.Find("Cancel");
            if (cancelTrans)
            {
                cancelButton = cancelTrans.GetComponent<Button>();
                var r = (RectTransform)cancelTrans;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(1, 0);
                r.anchoredPosition = new Vector2(-236, 20);
                r.sizeDelta = new Vector2(180, 44); // Taller: 52px
                var text = cancelButton.GetComponentInChildren<TMP_Text>(true);
                if (text)
                {
                    text.alignment = TextAlignmentOptions.Center;
                    text.rectTransform.anchorMin = Vector2.zero;
                    text.rectTransform.anchorMax = Vector2.one;
                    text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
                }
            // The OptionsUI.CancelOptions prefix discards staged values.
            }
        }

        void CreateTabs()
        {
            var font = header.GetComponentInChildren<TMP_Text>(true);
            tabStrip = new OptionsTabStrip(transform, font);
            tabStrip.Rebuild(FoundationApi.Options.Tabs, SelectBuiltinTab, ShowCustomTab);
            tabContainer = tabStrip.Root;
            gameTabButton = tabStrip.Buttons["game"];
            modsTabButton = tabStrip.Buttons["mods"];
            installedTabButton = tabStrip.Buttons["installed"];
            gameTabText = gameTabButton.GetComponentInChildren<TMP_Text>();
        }

        void BuildGamePage()
        {
            gamePage = new GameObject("GameSettingsPage", typeof(RectTransform));
            gamePage.transform.SetParent(transform, false);
            var pageRect = (RectTransform)gamePage.transform;
            pageRect.anchorMin = new Vector2(0, 0);
            pageRect.anchorMax = new Vector2(1, 1);
            pageRect.offsetMin = new Vector2(30, 84);
            pageRect.offsetMax = new Vector2(-30, -OptionsTabStrip.PageTopInset);
            // Locate the native OptionsContent containing vanilla rows
            var nativeContent = transform.Find("OptionsContent") as RectTransform;
            if (nativeContent)
            {
                // Disable its vertical layout group so we can arrange cleanly into two columns
                var oldVlg = nativeContent.GetComponent<VerticalLayoutGroup>();
                if (oldVlg)
                    oldVlg.enabled = false;
                var oldContentSize = nativeContent.GetComponent<ContentSizeFitter>();
                if (oldContentSize)
                    oldContentSize.enabled = false;
                // Reparent native content to gamePage
                nativeContent.SetParent(gamePage.transform, false);
                nativeContent.anchorMin = Vector2.zero;
                nativeContent.anchorMax = Vector2.one;
                nativeContent.offsetMin = nativeContent.offsetMax = Vector2.zero;
                // Create Left Column: Display & Interface
                var leftCol = CreateColumn(nativeContent, "DisplayColumn", new Vector2(0, 0), new Vector2(0.485f, 1));
                CreateSectionHeader(leftCol, VanillaSectionIds.DisplayAndInterface, "Display & Interface");
                // Move Resolution, WindowMode, Framerate, UIScale into Left Column
                MoveRow(nativeContent, leftCol, "Resolution");
                MoveRow(nativeContent, leftCol, "WindowMode");
                MoveRow(nativeContent, leftCol, "Framerate");
                MoveRow(nativeContent, leftCol, "UIScale");
                // Create Right Column: Audio & Controls
                var rightCol = CreateColumn(nativeContent, "AudioColumn", new Vector2(0.515f, 0), new Vector2(1, 1));
                CreateSectionHeader(rightCol, VanillaSectionIds.AudioAndSystem, "Audio & System");
                // Move MusicVolume, SoundVolume, AutosaveFreq, KeyBindings into Right Column
                MoveRow(nativeContent, rightCol, "MusicVolume");
                MoveRow(nativeContent, rightCol, "SoundVolume");
                MoveRow(nativeContent, rightCol, "AutosaveFreq");
                MoveRow(nativeContent, rightCol, "KeyBindings");
            }
        }

        RectTransform CreateColumn(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var col = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            col.transform.SetParent(parent, false);
            var rect = (RectTransform)col.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var vlg = col.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return rect;
        }

        void CreateSectionHeader(Transform parent, string id, string title)
        {
            var heading = templates.CreateTextRow(parent, title, true, out _);
            vanillaSections.Add(id, new VanillaSectionView(id, title, heading, parent, UpdateVanillaLayout));
        }

        void MoveRow(Transform nativeParent, Transform newParent, string childName)
        {
            var child = nativeParent.Find(childName);
            if (child)
            {
                child.gameObject.SetActive(true);
                vanillaRows[childName] = new VanillaSettingView(childName, child.gameObject, UpdateVanillaLayout);
                child.SetParent(newParent, false);
                var rect = (RectTransform)child;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(0, 42);
                var le = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 42;
                le.minHeight = 42;
                le.flexibleWidth = 1;
            }
        }

        void BuildModsPage()
        {
            modsPage = new GameObject("ModSettingsPage", typeof(RectTransform));
            modsPage.transform.SetParent(transform, false);
            var pageRect = (RectTransform)modsPage.transform;
            pageRect.anchorMin = new Vector2(0, 0);
            pageRect.anchorMax = new Vector2(1, 1);
            pageRect.offsetMin = new Vector2(30, 84);
            pageRect.offsetMax = new Vector2(-30, -OptionsTabStrip.PageTopInset);
            // 1. Sidebar on the left (width ~320px)
            var sidebar = new GameObject("Sidebar", typeof(RectTransform), typeof(Image));
            sidebar.transform.SetParent(modsPage.transform, false);
            var sRect = (RectTransform)sidebar.transform;
            sRect.anchorMin = new Vector2(0, 0);
            sRect.anchorMax = new Vector2(0, 1);
            sRect.pivot = new Vector2(0, 0.5f);
            sRect.anchoredPosition = Vector2.zero;
            sRect.sizeDelta = new Vector2(320, 0);
            var sImg = sidebar.GetComponent<Image>();
            if (rootImage)
            {
                sImg.sprite = rootImage.sprite;
                sImg.type = Image.Type.Sliced;
                sImg.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);
                sImg.pixelsPerUnitMultiplier = rootImage.pixelsPerUnitMultiplier;
            }

            // Sidebar Title
            var sTitleObj = new GameObject("SidebarTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            sTitleObj.transform.SetParent(sidebar.transform, false);
            var stRect = (RectTransform)sTitleObj.transform;
            stRect.anchorMin = new Vector2(0, 1);
            stRect.anchorMax = new Vector2(1, 1);
            stRect.pivot = new Vector2(0.5f, 1);
            stRect.anchoredPosition = new Vector2(0, -10);
            stRect.sizeDelta = new Vector2(-20, 36);
            var stText = sTitleObj.GetComponent<TextMeshProUGUI>();
            if (gameTabText)
            {
                stText.font = gameTabText.font;
                stText.fontSharedMaterial = gameTabText.fontSharedMaterial;
            }

            stText.text = "Installed Mods";
            stText.fontStyle = FontStyles.Bold;
            stText.fontSize = 20;
            stText.alignment = TextAlignmentOptions.Center;
            // Sidebar Scroll Viewport
            var sViewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            sViewport.transform.SetParent(sidebar.transform, false);
            var svRect = (RectTransform)sViewport.transform;
            svRect.anchorMin = Vector2.zero;
            svRect.anchorMax = Vector2.one;
            svRect.offsetMin = new Vector2(8, 12);
            svRect.offsetMax = new Vector2(-8, -48);
            sViewport.GetComponent<Image>().color = Color.clear;
            var sContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            sContent.transform.SetParent(sViewport.transform, false);
            modsSidebarContent = (RectTransform)sContent.transform;
            modsSidebarContent.anchorMin = new Vector2(0, 1);
            modsSidebarContent.anchorMax = new Vector2(1, 1);
            modsSidebarContent.pivot = new Vector2(0.5f, 1);
            modsSidebarContent.sizeDelta = new Vector2(0, 0);
            var sVlg = sContent.GetComponent<VerticalLayoutGroup>();
            sVlg.padding = new RectOffset(4, 4, 4, 4);
            sVlg.spacing = 6;
            sVlg.childControlWidth = true;
            sVlg.childControlHeight = false;
            sVlg.childForceExpandWidth = true;
            sVlg.childForceExpandHeight = false;
            sVlg.childAlignment = TextAnchor.UpperLeft;
            var sCsf = sContent.GetComponent<ContentSizeFitter>();
            sCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            modsSidebarScroll = sViewport.AddComponent<ScrollRect>();
            modsSidebarScroll.content = modsSidebarContent;
            modsSidebarScroll.viewport = svRect;
            modsSidebarScroll.horizontal = false;
            modsSidebarScroll.vertical = true;
            modsSidebarScroll.movementType = ScrollRect.MovementType.Clamped;
            modsSidebarScroll.scrollSensitivity = 25f;
            // Empty notice
            var emptyObj = new GameObject("EmptyNotice", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyObj.transform.SetParent(sViewport.transform, false);
            var eRect = (RectTransform)emptyObj.transform;
            eRect.anchorMin = Vector2.zero;
            eRect.anchorMax = Vector2.one;
            eRect.offsetMin = eRect.offsetMax = Vector2.zero;
            noModsText = emptyObj.GetComponent<TextMeshProUGUI>();
            if (gameTabText)
            {
                noModsText.font = gameTabText.font;
                noModsText.fontSharedMaterial = gameTabText.fontSharedMaterial;
            }

            noModsText.text = "No mods registered with config";
            noModsText.fontSize = 15;
            noModsText.alignment = TextAlignmentOptions.Center;
            noModsText.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            // 2. Settings Panel on the right (width ~750px)
            var panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(modsPage.transform, false);
            var pRect = (RectTransform)panel.transform;
            pRect.anchorMin = new Vector2(0, 0);
            pRect.anchorMax = new Vector2(1, 1);
            pRect.offsetMin = new Vector2(335, 0);
            pRect.offsetMax = Vector2.zero;
            var pImg = panel.GetComponent<Image>();
            if (rootImage)
            {
                pImg.sprite = rootImage.sprite;
                pImg.type = Image.Type.Sliced;
                pImg.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);
                pImg.pixelsPerUnitMultiplier = rootImage.pixelsPerUnitMultiplier;
            }

            // Selected Mod Header (Title + Description)
            var mHeaderObj = new GameObject("ModHeader", typeof(RectTransform), typeof(VerticalLayoutGroup));
            mHeaderObj.transform.SetParent(panel.transform, false);
            var mhRect = (RectTransform)mHeaderObj.transform;
            mhRect.anchorMin = new Vector2(0, 1);
            mhRect.anchorMax = new Vector2(1, 1);
            mhRect.pivot = new Vector2(0.5f, 1);
            mhRect.anchoredPosition = new Vector2(0, -10);
            mhRect.sizeDelta = new Vector2(-40, 58);
            var mhVlg = mHeaderObj.GetComponent<VerticalLayoutGroup>();
            mhVlg.spacing = 2;
            mhVlg.childControlWidth = true;
            mhVlg.childControlHeight = true;
            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(mHeaderObj.transform, false);
            modsHeaderTitle = titleObj.GetComponent<TextMeshProUGUI>();
            if (gameTabText)
            {
                modsHeaderTitle.font = gameTabText.font;
                modsHeaderTitle.fontSharedMaterial = gameTabText.fontSharedMaterial;
            }

            modsHeaderTitle.fontStyle = FontStyles.Bold;
            modsHeaderTitle.fontSize = 22;
            modsHeaderTitle.alignment = TextAlignmentOptions.MidlineLeft;
            var descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(mHeaderObj.transform, false);
            modsHeaderDesc = descObj.GetComponent<TextMeshProUGUI>();
            if (gameTabText)
            {
                modsHeaderDesc.font = gameTabText.font;
                modsHeaderDesc.fontSharedMaterial = gameTabText.fontSharedMaterial;
            }

            modsHeaderDesc.fontSize = 14;
            modsHeaderDesc.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
            modsHeaderDesc.alignment = TextAlignmentOptions.MidlineLeft;
            // Settings Viewport (with right margin for vertical scrollbar)
            var pViewport = new GameObject("SettingsViewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            pViewport.transform.SetParent(panel.transform, false);
            var pvRect = (RectTransform)pViewport.transform;
            pvRect.anchorMin = Vector2.zero;
            pvRect.anchorMax = Vector2.one;
            pvRect.offsetMin = new Vector2(12, 12);
            pvRect.offsetMax = new Vector2(-38, -72);
            pViewport.GetComponent<Image>().color = Color.clear;
            var pContent = new GameObject("SettingsContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            pContent.transform.SetParent(pViewport.transform, false);
            modsSettingsContent = (RectTransform)pContent.transform;
            modsSettingsContent.anchorMin = new Vector2(0, 1);
            modsSettingsContent.anchorMax = new Vector2(1, 1);
            modsSettingsContent.pivot = new Vector2(0.5f, 1);
            modsSettingsContent.sizeDelta = new Vector2(0, 0);
            var pVlg = pContent.GetComponent<VerticalLayoutGroup>();
            pVlg.padding = new RectOffset(8, 8, 8, 8);
            pVlg.spacing = 8;
            pVlg.childControlWidth = true;
            pVlg.childControlHeight = false;
            pVlg.childForceExpandWidth = true;
            pVlg.childForceExpandHeight = false;
            var pCsf = pContent.GetComponent<ContentSizeFitter>();
            pCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            modsSettingsScroll = pViewport.AddComponent<ScrollRect>();
            modsSettingsScroll.content = modsSettingsContent;
            modsSettingsScroll.viewport = pvRect;
            modsSettingsScroll.horizontal = false;
            modsSettingsScroll.vertical = true;
            modsSettingsScroll.movementType = ScrollRect.MovementType.Clamped;
            modsSettingsScroll.scrollSensitivity = 30f;
            // Vertical Scrollbar for Mod Settings Viewport
            var sbarObj = new GameObject("ScrollbarVertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbarObj.transform.SetParent(panel.transform, false);
            var sbRect = (RectTransform)sbarObj.transform;
            sbRect.anchorMin = new Vector2(1, 0);
            sbRect.anchorMax = new Vector2(1, 1);
            sbRect.pivot = new Vector2(1, 0.5f);
            sbRect.offsetMin = new Vector2(-28, 14);
            sbRect.offsetMax = new Vector2(-10, -74);
            var bgImg = sbarObj.GetComponent<Image>();
            if (rootImage)
            {
                bgImg.sprite = rootImage.sprite;
                bgImg.type = Image.Type.Sliced;
            }

            bgImg.color = new Color(0.12f, 0.12f, 0.15f, 0.90f);
            var slideArea = new GameObject("SlidingArea", typeof(RectTransform));
            slideArea.transform.SetParent(sbarObj.transform, false);
            var saRect = (RectTransform)slideArea.transform;
            saRect.anchorMin = Vector2.zero;
            saRect.anchorMax = Vector2.one;
            saRect.offsetMin = new Vector2(2, 2);
            saRect.offsetMax = new Vector2(-2, -2);
            var handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(slideArea.transform, false);
            var hRect = (RectTransform)handleObj.transform;
            hRect.anchorMin = Vector2.zero;
            hRect.anchorMax = Vector2.one;
            hRect.offsetMin = hRect.offsetMax = Vector2.zero;
            var hImg = handleObj.GetComponent<Image>();
            if (applyButton)
            {
                var btnImg = applyButton.GetComponent<Image>();
                if (btnImg)
                {
                    hImg.sprite = btnImg.sprite;
                    hImg.type = Image.Type.Sliced;
                }
            }

            hImg.color = new Color(0.78f, 0.68f, 0.45f, 1f);
            var scrollbar = sbarObj.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = hImg;
            scrollbar.handleRect = hRect;
            var colors = scrollbar.colors;
            colors.normalColor = new Color(0.78f, 0.68f, 0.45f, 1f);
            colors.highlightedColor = new Color(0.95f, 0.85f, 0.60f, 1f);
            colors.pressedColor = new Color(1f, 0.95f, 0.75f, 1f);
            scrollbar.colors = colors;
            modsSettingsScroll.verticalScrollbar = scrollbar;
            modsSettingsScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        public void ShowGameTab()
        {
            HideCustomPages();
            isModsTabActive = isInstalledTabActive = false;
            if (gamePage)
                gamePage.SetActive(true);
            if (modsPage)
                modsPage.SetActive(false);
            if (installedPage)
                installedPage.SetActive(false);
            tabStrip?.Select("game");
        }

        public void ShowModsTab(string modId = null)
        {
            HideCustomPages();
            isModsTabActive = true;
            isInstalledTabActive = false;
            if (gamePage)
                gamePage.SetActive(false);
            if (modsPage)
                modsPage.SetActive(true);
            if (installedPage)
                installedPage.SetActive(false);
            tabStrip?.Select("mods");
            if (!string.IsNullOrEmpty(modId))
                selectedModId = modId;
            RefreshModsList();
        }

        public void ShowInstalledTab()
        {
            HideCustomPages();
            isModsTabActive = false;
            isInstalledTabActive = true;
            if (gamePage)
                gamePage.SetActive(false);
            if (modsPage)
                modsPage.SetActive(false);
            if (installedPage)
                installedPage.SetActive(true);
            tabStrip?.Select("installed");
            RefreshInstalledModsUI();
            if (!NexusUpdateChecker.CachedMods.Any(m => m.Checked) && !NexusUpdateChecker.IsCheckingAll)
                NexusUpdateChecker.CheckAll();
        }

        void BuildInstalledPage()
        {
            installedPage = new GameObject("InstalledModsPage", typeof(RectTransform));
            installedPage.transform.SetParent(transform, false);
            var pageRect = (RectTransform)installedPage.transform;
            pageRect.anchorMin = new Vector2(0, 0);
            pageRect.anchorMax = new Vector2(1, 1);
            pageRect.offsetMin = new Vector2(30, 84);
            pageRect.offsetMax = new Vector2(-30, -OptionsTabStrip.PageTopInset);
            // 1. Top Control Bar (Title + Subtitle + Action buttons)
            var topBar = new GameObject("TopBar", typeof(RectTransform));
            topBar.transform.SetParent(installedPage.transform, false);
            var tbRect = (RectTransform)topBar.transform;
            tbRect.anchorMin = new Vector2(0, 1);
            tbRect.anchorMax = new Vector2(1, 1);
            tbRect.pivot = new Vector2(0.5f, 1);
            tbRect.anchoredPosition = new Vector2(0, 0);
            tbRect.sizeDelta = new Vector2(0, 44);
            // Summary Text
            var sumObj = new GameObject("SummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
            sumObj.transform.SetParent(topBar.transform, false);
            var stRect = (RectTransform)sumObj.transform;
            stRect.anchorMin = new Vector2(0, 0);
            stRect.anchorMax = new Vector2(1, 1);
            stRect.pivot = new Vector2(0, 0.5f);
            stRect.offsetMin = new Vector2(10, 0);
            stRect.offsetMax = new Vector2(-375, 0);
            installedSummaryText = sumObj.GetComponent<TextMeshProUGUI>();
            if (gameTabText)
            {
                installedSummaryText.font = gameTabText.font;
                installedSummaryText.fontSharedMaterial = gameTabText.fontSharedMaterial;
            }

            installedSummaryText.fontSize = 17;
            installedSummaryText.enableAutoSizing = true;
            installedSummaryText.fontSizeMin = 13;
            installedSummaryText.fontSizeMax = 17;
            installedSummaryText.alignment = TextAlignmentOptions.MidlineLeft;
            installedSummaryText.text = "<b>Installed Mods</b>  |  Loading...";
            // Action Buttons Template from Apply button
            var template = applyButton != null ? applyButton.gameObject : gameTabButton.gameObject;
            // Check Updates Button (generous width and inner padding)
            var checkBtnObj = Instantiate(template, topBar.transform);
            checkBtnObj.name = "CheckUpdatesButton";
            checkUpdatesButton = checkBtnObj.GetComponent<Button>();
            checkUpdatesButton.onClick = new Button.ButtonClickedEvent();
            checkUpdatesButton.onClick.AddListener(() =>
            {
                UISounds.Button();
                NexusUpdateChecker.CheckAll();
            });
            var cbRect = (RectTransform)checkBtnObj.transform;
            cbRect.anchorMin = new Vector2(1, 0.5f);
            cbRect.anchorMax = new Vector2(1, 0.5f);
            cbRect.pivot = new Vector2(1, 0.5f);
            cbRect.anchoredPosition = new Vector2(-181, 0);
            cbRect.sizeDelta = new Vector2(185, 38);
            var cbText = checkBtnObj.GetComponentInChildren<TMP_Text>(true);
            if (cbText)
            {
                cbText.text = "Check Updates";
                cbText.fontSize = 14;
                cbText.enableAutoSizing = true;
                cbText.fontSizeMin = 11;
                cbText.fontSizeMax = 14;
                cbText.alignment = TextAlignmentOptions.Center;
                var cbtr = cbText.rectTransform;
                cbtr.anchorMin = Vector2.zero;
                cbtr.anchorMax = Vector2.one;
                cbtr.offsetMin = new Vector2(22, 0);
                cbtr.offsetMax = new Vector2(-22, 0);
            }

            // Open Plugins Folder Button (aligned with list content edge)
            var folderBtnObj = Instantiate(template, topBar.transform);
            folderBtnObj.name = "OpenFolderButton";
            openFolderButton = folderBtnObj.GetComponent<Button>();
            openFolderButton.onClick = new Button.ButtonClickedEvent();
            openFolderButton.onClick.AddListener(() =>
            {
                UISounds.Button();
                Application.OpenURL("file://" + BepInEx.Paths.PluginPath);
            });
            var fbRect = (RectTransform)folderBtnObj.transform;
            fbRect.anchorMin = new Vector2(1, 0.5f);
            fbRect.anchorMax = new Vector2(1, 0.5f);
            fbRect.pivot = new Vector2(1, 0.5f);
            fbRect.anchoredPosition = new Vector2(-8, 0);
            fbRect.sizeDelta = new Vector2(165, 38);
            var fbText = folderBtnObj.GetComponentInChildren<TMP_Text>(true);
            if (fbText)
            {
                fbText.text = "Open Folder";
                fbText.fontSize = 14;
                fbText.enableAutoSizing = true;
                fbText.fontSizeMin = 11;
                fbText.fontSizeMax = 14;
                fbText.alignment = TextAlignmentOptions.Center;
                var fbtr = fbText.rectTransform;
                fbtr.anchorMin = Vector2.zero;
                fbtr.anchorMax = Vector2.one;
                fbtr.offsetMin = new Vector2(22, 0);
                fbtr.offsetMax = new Vector2(-22, 0);
            }

            // 2. Main Mod List Panel (Scrollable area)
            var panel = new GameObject("InstalledPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(installedPage.transform, false);
            var pRect = (RectTransform)panel.transform;
            pRect.anchorMin = new Vector2(0, 0);
            pRect.anchorMax = new Vector2(1, 1);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = new Vector2(0, -48);
            var pImg = panel.GetComponent<Image>();
            if (rootImage)
            {
                pImg.sprite = rootImage.sprite;
                pImg.type = Image.Type.Sliced;
                pImg.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);
                pImg.pixelsPerUnitMultiplier = rootImage.pixelsPerUnitMultiplier;
            }

            // Viewport
            var pViewport = new GameObject("InstalledViewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            pViewport.transform.SetParent(panel.transform, false);
            var pvRect = (RectTransform)pViewport.transform;
            pvRect.anchorMin = Vector2.zero;
            pvRect.anchorMax = Vector2.one;
            pvRect.offsetMin = new Vector2(12, 12);
            pvRect.offsetMax = new Vector2(-38, -12);
            pViewport.GetComponent<Image>().color = Color.clear;
            // Content
            var pContent = new GameObject("InstalledContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            pContent.transform.SetParent(pViewport.transform, false);
            installedContent = (RectTransform)pContent.transform;
            installedContent.anchorMin = new Vector2(0, 1);
            installedContent.anchorMax = new Vector2(1, 1);
            installedContent.pivot = new Vector2(0.5f, 1);
            installedContent.sizeDelta = new Vector2(0, 0);
            var pVlg = pContent.GetComponent<VerticalLayoutGroup>();
            pVlg.padding = new RectOffset(8, 8, 8, 8);
            pVlg.spacing = 8;
            pVlg.childControlWidth = true;
            pVlg.childControlHeight = false;
            pVlg.childForceExpandWidth = true;
            pVlg.childForceExpandHeight = false;
            var pCsf = pContent.GetComponent<ContentSizeFitter>();
            pCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            installedScroll = pViewport.AddComponent<ScrollRect>();
            installedScroll.content = installedContent;
            installedScroll.viewport = pvRect;
            installedScroll.horizontal = false;
            installedScroll.vertical = true;
            installedScroll.movementType = ScrollRect.MovementType.Clamped;
            installedScroll.scrollSensitivity = 30f;
            // Scrollbar Vertical
            var sbarObj = new GameObject("ScrollbarVertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbarObj.transform.SetParent(panel.transform, false);
            var sbRect = (RectTransform)sbarObj.transform;
            sbRect.anchorMin = new Vector2(1, 0);
            sbRect.anchorMax = new Vector2(1, 1);
            sbRect.pivot = new Vector2(1, 0.5f);
            sbRect.offsetMin = new Vector2(-28, 14);
            sbRect.offsetMax = new Vector2(-10, -14);
            var bgImg = sbarObj.GetComponent<Image>();
            if (rootImage)
            {
                bgImg.sprite = rootImage.sprite;
                bgImg.type = Image.Type.Sliced;
            }

            bgImg.color = new Color(0.12f, 0.12f, 0.15f, 0.90f);
            var slideArea = new GameObject("SlidingArea", typeof(RectTransform));
            slideArea.transform.SetParent(sbarObj.transform, false);
            var saRect = (RectTransform)slideArea.transform;
            saRect.anchorMin = Vector2.zero;
            saRect.anchorMax = Vector2.one;
            saRect.offsetMin = new Vector2(2, 2);
            saRect.offsetMax = new Vector2(-2, -2);
            var handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(slideArea.transform, false);
            var hRect = (RectTransform)handleObj.transform;
            hRect.anchorMin = Vector2.zero;
            hRect.anchorMax = Vector2.one;
            hRect.offsetMin = hRect.offsetMax = Vector2.zero;
            var hImg = handleObj.GetComponent<Image>();
            if (applyButton)
            {
                var btnImg = applyButton.GetComponent<Image>();
                if (btnImg)
                {
                    hImg.sprite = btnImg.sprite;
                    hImg.type = Image.Type.Sliced;
                }
            }

            hImg.color = new Color(0.78f, 0.68f, 0.45f, 1f);
            var scrollbar = sbarObj.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = hImg;
            scrollbar.handleRect = hRect;
            var colors = scrollbar.colors;
            colors.normalColor = new Color(0.78f, 0.68f, 0.45f, 1f);
            colors.highlightedColor = new Color(0.95f, 0.85f, 0.60f, 1f);
            colors.pressedColor = new Color(1f, 0.95f, 0.75f, 1f);
            scrollbar.colors = colors;
            installedScroll.verticalScrollbar = scrollbar;
            installedScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        void OnInstalledModsChanged()
        {
            if (isInstalledTabActive && gameObject.activeInHierarchy)
            {
                RefreshInstalledModsUI();
            }
        }

        public void RefreshInstalledModsUI()
        {
            if (!installedContent)
                return;
            var mods = NexusUpdateChecker.CachedMods;
            if (mods.Count == 0)
            {
                NexusUpdateChecker.RefreshInstalledList();
                mods = NexusUpdateChecker.CachedMods;
            }

            int total = mods.Count;
            int outdated = mods.Count(m => m.IsOutdated);
            if (installedSummaryText)
            {
                if (NexusUpdateChecker.IsCheckingAll)
                {
                    installedSummaryText.text = $"<b>Installed Mods</b>  |  <size=15>{total} Mods Loaded  |  <color=#D4AF37>Checking Nexus for updates...</color></size>";
                }
                else if (outdated > 0)
                {
                    installedSummaryText.text = $"<b>Installed Mods</b>  |  <size=15>{total} Mods Loaded  |  <color=#FF7777><b>{outdated} Update{(outdated == 1 ? "" : "s")} Available</b></color></size>";
                }
                else
                {
                    installedSummaryText.text = $"<b>Installed Mods</b>  |  <size=15>{total} Mods Loaded  |  <color=#77DD77>{NexusUpdateChecker.LastCheckSummary}</color></size>";
                }
            }

            foreach (var obj in installedModRowObjects)
            {
                if (obj)
                    Destroy(obj);
            }

            installedModRowObjects.Clear();
            var template = applyButton != null ? applyButton.gameObject : gameTabButton.gameObject;
            for (int i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                var rowObj = new GameObject("ModRow_" + mod.Guid, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                rowObj.transform.SetParent(installedContent, false);
                installedModRowObjects.Add(rowObj);
                var rRect = (RectTransform)rowObj.transform;
                rRect.sizeDelta = new Vector2(0, 58);
                var le = rowObj.GetComponent<LayoutElement>();
                le.preferredHeight = 58;
                le.minHeight = 58;
                le.flexibleWidth = 1;
                var rImg = rowObj.GetComponent<Image>();
                if (rootImage)
                {
                    rImg.sprite = rootImage.sprite;
                    rImg.type = Image.Type.Sliced;
                }

                rImg.color = mod.IsOutdated ? new Color(0.24f, 0.16f, 0.14f, 0.88f) : new Color(0.14f, 0.14f, 0.18f, 0.85f);
                // Left: Name & GUID
                var nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameObj.transform.SetParent(rowObj.transform, false);
                var nr = (RectTransform)nameObj.transform;
                nr.anchorMin = new Vector2(0, 0.46f);
                nr.anchorMax = new Vector2(0.50f, 1f);
                nr.offsetMin = new Vector2(16, 0);
                nr.offsetMax = new Vector2(0, -6);
                var nText = nameObj.GetComponent<TextMeshProUGUI>();
                if (gameTabText)
                {
                    nText.font = gameTabText.font;
                    nText.fontSharedMaterial = gameTabText.fontSharedMaterial;
                }

                nText.text = mod.Name;
                nText.fontStyle = FontStyles.Bold;
                nText.fontSize = 17;
                nText.alignment = TextAlignmentOptions.MidlineLeft;
                var guidObj = new GameObject("Guid", typeof(RectTransform), typeof(TextMeshProUGUI));
                guidObj.transform.SetParent(rowObj.transform, false);
                var gr = (RectTransform)guidObj.transform;
                gr.anchorMin = new Vector2(0, 0f);
                gr.anchorMax = new Vector2(0.50f, 0.46f);
                gr.offsetMin = new Vector2(16, 6);
                gr.offsetMax = new Vector2(0, 0);
                var gText = guidObj.GetComponent<TextMeshProUGUI>();
                if (gameTabText)
                {
                    gText.font = gameTabText.font;
                    gText.fontSharedMaterial = gameTabText.fontSharedMaterial;
                }

                string fileName = !string.IsNullOrEmpty(mod.Location) ? Path.GetFileName(mod.Location) : "";
                gText.text = $"{mod.Guid}{(string.IsNullOrEmpty(fileName) ? "" : "  |  " + fileName)}";
                gText.fontSize = 12;
                gText.color = new Color(0.65f, 0.65f, 0.70f, 0.9f);
                gText.alignment = TextAlignmentOptions.MidlineLeft;
                // Center: Version Info
                var verObj = new GameObject("Versions", typeof(RectTransform), typeof(TextMeshProUGUI));
                verObj.transform.SetParent(rowObj.transform, false);
                var vr = (RectTransform)verObj.transform;
                vr.anchorMin = new Vector2(0.50f, 0f);
                vr.anchorMax = new Vector2(0.74f, 1f);
                vr.offsetMin = new Vector2(10, 6);
                vr.offsetMax = new Vector2(-10, -6);
                var vText = verObj.GetComponent<TextMeshProUGUI>();
                if (gameTabText)
                {
                    vText.font = gameTabText.font;
                    vText.fontSharedMaterial = gameTabText.fontSharedMaterial;
                }

                vText.fontSize = 13;
                vText.alignment = TextAlignmentOptions.MidlineLeft;
                string nexusStatus;
                if (mod.IsChecking)
                    nexusStatus = "<color=#D4AF37>Nexus: Checking...</color>";
                else if (mod.IsOutdated)
                    nexusStatus = $"<color=#FF6666><b>Nexus: v{mod.LatestVersion}</b> (Newer)</color>";
                else if (mod.Checked && !string.IsNullOrEmpty(mod.LatestVersion))
                    nexusStatus = $"<color=#66DD66>Nexus: v{mod.LatestVersion} (Latest)</color>";
                else if (mod.NexusId.HasValue)
                    nexusStatus = $"<color=#999999>Nexus: #{mod.NexusId}</color>";
                else
                    nexusStatus = "<color=#777777>Local Mod (No Nexus Page)</color>";
                vText.text = $"Installed: <b>v{mod.InstalledVersion}</b>\n{nexusStatus}";
                // Right: Action button
                if (mod.NexusId.HasValue)
                {
                    var btnObj = Instantiate(template, rowObj.transform);
                    btnObj.name = "NexusAction_" + mod.Guid;
                    var btn = btnObj.GetComponent<Button>();
                    btn.onClick = new Button.ButtonClickedEvent();
                    string targetUrl = mod.NexusUrl;
                    btn.onClick.AddListener(() =>
                    {
                        UISounds.TurnPage();
                        Application.OpenURL(targetUrl);
                    });
                    var br = (RectTransform)btnObj.transform;
                    br.anchorMin = new Vector2(1f, 0.5f);
                    br.anchorMax = new Vector2(1f, 0.5f);
                    br.pivot = new Vector2(1f, 0.5f);
                    br.anchoredPosition = new Vector2(-14, 0);
                    br.sizeDelta = new Vector2(185, 38);
                    var bImg = btnObj.GetComponent<Image>();
                    var bText = btnObj.GetComponentInChildren<TMP_Text>(true);
                    if (mod.IsOutdated)
                    {
                        if (bImg)
                            bImg.color = new Color(0.95f, 0.72f, 0.28f, 1f); // Highlight amber/gold
                        if (bText)
                        {
                            bText.text = "Update Available";
                            bText.fontStyle = FontStyles.Bold;
                            bText.fontSize = 14;
                            bText.color = new Color(0.12f, 0.08f, 0.02f, 1f);
                        }
                    }
                    else
                    {
                        if (bImg)
                            bImg.color = new Color(0.85f, 0.85f, 0.85f, 0.90f);
                        if (bText)
                        {
                            bText.text = "Nexus Page";
                            bText.fontStyle = FontStyles.Normal;
                            bText.fontSize = 14;
                            bText.color = Color.white;
                        }
                    }

                    if (bText)
                    {
                        bText.alignment = TextAlignmentOptions.Center;
                        bText.enableAutoSizing = true;
                        bText.fontSizeMin = 11;
                        bText.fontSizeMax = 14;
                        var btr = bText.rectTransform;
                        btr.anchorMin = Vector2.zero;
                        btr.anchorMax = Vector2.one;
                        btr.offsetMin = new Vector2(20, 0);
                        btr.offsetMax = new Vector2(-20, 0);
                    }
                }
                else
                {
                    // Local tag
                    var localTagObj = new GameObject("LocalBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
                    localTagObj.transform.SetParent(rowObj.transform, false);
                    var ltr = (RectTransform)localTagObj.transform;
                    ltr.anchorMin = new Vector2(1f, 0.5f);
                    ltr.anchorMax = new Vector2(1f, 0.5f);
                    ltr.pivot = new Vector2(1f, 0.5f);
                    ltr.anchoredPosition = new Vector2(-20, 0);
                    ltr.sizeDelta = new Vector2(160, 32);
                    var lt = localTagObj.GetComponent<TextMeshProUGUI>();
                    if (gameTabText)
                    {
                        lt.font = gameTabText.font;
                        lt.fontSharedMaterial = gameTabText.fontSharedMaterial;
                    }

                    lt.text = "Local Mod";
                    lt.fontSize = 14;
                    lt.alignment = TextAlignmentOptions.Center;
                    lt.color = new Color(0.55f, 0.55f, 0.55f, 0.75f);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(installedContent);
            Canvas.ForceUpdateCanvases();
        }

        void OnRegistryChanged()
        {
            SynchronizeEditSections();
            if (isModsTabActive && gameObject.activeInHierarchy)
                RefreshModsList();
        }

        public void RefreshModsList()
        {
            if (!modsSidebarContent)
                return;
            var sections = FoundationApi.ModConfig.Sections;
            noModsText.gameObject.SetActive(sections.Count == 0);
            // Rebuild buttons if count or IDs changed
            foreach (var btn in modButtons.Values)
            {
                if (!btn)
                    continue;
                // Deferred destruction must not leave obsolete rows in this frame's layout.
                btn.gameObject.SetActive(false);
                btn.transform.SetParent(null, false);
                Destroy(btn.gameObject);
            }

            modButtons.Clear();
            if (string.IsNullOrEmpty(selectedModId) && sections.Count > 0)
                selectedModId = sections[0].ModId;
            // If selectedModId is no longer present, select first
            if (!sections.Any(s => s.ModId == selectedModId) && sections.Count > 0)
                selectedModId = sections[0].ModId;
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                var btnObj = Instantiate(applyButton != null ? applyButton.gameObject : gameTabButton.gameObject, modsSidebarContent);
                btnObj.name = "ModItem_" + section.ModId;
                var btn = btnObj.GetComponent<Button>();
                btn.onClick = new Button.ButtonClickedEvent();
                string id = section.ModId;
                btn.onClick.AddListener(() =>
                {
                    UISounds.TurnPage();
                    SelectMod(id);
                });
                var label = btnObj.GetComponentInChildren<TMP_Text>(true);
                if (label)
                {
                    label.text = section.ModName ?? section.ModId;
                    label.alignment = TextAlignmentOptions.Center; // Centered vertically and horizontally
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 12;
                    label.fontSizeMax = 18;
                    var lr = label.rectTransform;
                    lr.anchorMin = Vector2.zero;
                    lr.anchorMax = Vector2.one;
                    lr.offsetMin = new Vector2(24, 0);
                    lr.offsetMax = new Vector2(-24, 0);
                }

                var r = (RectTransform)btnObj.transform;
                r.sizeDelta = new Vector2(0, 50); // Taller: 50px
                var le = btnObj.GetComponent<LayoutElement>() ?? btnObj.AddComponent<LayoutElement>();
                le.preferredHeight = 50;
                le.minHeight = 50;
                le.flexibleWidth = 1;
                modButtons[section.ModId] = btn;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(modsSidebarContent);
            if (modsSidebarScroll)
            {
                modsSidebarScroll.StopMovement();
                modsSidebarScroll.verticalNormalizedPosition = 1f;
            }

            SelectMod(selectedModId);
        }

        public void SelectMod(string modId)
        {
            selectedModId = modId;
            // Highlight selected button
            foreach (var pair in modButtons)
            {
                if (pair.Value)
                {
                    var img = pair.Value.GetComponent<Image>();
                    if (img)
                        img.color = pair.Key == selectedModId ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.75f);
                }
            }

            RenderSelectedModSettings();
        }

        void RenderSelectedModSettings()
        {
            if (!modsSettingsContent)
                return;
            // Clear old elements immediately
            for (int i = modsSettingsContent.childCount - 1; i >= 0; i--)
            {
                var child = modsSettingsContent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            if (string.IsNullOrEmpty(selectedModId) || !FoundationApi.ModConfig.TryGetSection(selectedModId, out var section))
            {
                if (modsHeaderTitle)
                    modsHeaderTitle.text = "Select a Mod";
                if (modsHeaderDesc)
                    modsHeaderDesc.text = "";
                return;
            }

            if (modsHeaderTitle)
                modsHeaderTitle.text = section.ModName;
            if (modsHeaderDesc)
                modsHeaderDesc.text = section.Description;
            var context = new ConfigContext(modsSettingsContent, templates);
            foreach (var el in section.Elements)
            {
                if (el != null)
                {
                    try
                    {
                        el.CreateView(context);
                    }
                    catch (Exception ex)
                    {
                        FoundationApi.Report(ex);
                    }
                }
            }

            // Immediately rebuild layout so content height is exact and scroll position starts at the top
            LayoutRebuilder.ForceRebuildLayoutImmediate(modsSettingsContent);
            Canvas.ForceUpdateCanvases();
            modsSettingsContent.anchoredPosition = new Vector2(modsSettingsContent.anchoredPosition.x, 0f);
            if (modsSettingsScroll)
            {
                modsSettingsScroll.StopMovement();
                modsSettingsScroll.verticalNormalizedPosition = 1f;
            }
        }

        internal void CommitModConfigs()
        {
            if (!modEditSessionOpen)
                return;
            modEditSessionOpen = false;
            var sections = editSections.ToArray();
            editSections.Clear();
            foreach (var section in sections)
            {
                try
                {
                    section.Commit();
                }
                catch (Exception ex)
                {
                    FoundationApi.Report(ex);
                }
            }
        }

        internal void CancelModConfigs()
        {
            if (!modEditSessionOpen)
                return;
            modEditSessionOpen = false;
            var sections = editSections.ToArray();
            editSections.Clear();
            foreach (var section in sections)
            {
                try
                {
                    section.Cancel();
                }
                catch (Exception ex)
                {
                    FoundationApi.Report(ex);
                }
            }
        }
    }
}
