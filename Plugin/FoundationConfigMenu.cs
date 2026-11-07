using System;
using System.Collections.Generic;
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
    public sealed class FoundationConfigMenu : MonoBehaviour
    {
        OptionsUI optionsUI;
        RectTransform rootRect;
        Image rootImage;

        // Header and buttons
        RectTransform header;
        Button applyButton, cancelButton, closeButton;

        // Tabs
        RectTransform tabContainer;
        Button gameTabButton, modsTabButton;
        Image gameTabImage, modsTabImage;
        TMP_Text gameTabText, modsTabText;
        Sprite activeTabSprite;
        Color activeTabColor = Color.white;
        Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        bool isModsTabActive;

        // Pages
        GameObject gamePage;
        GameObject modsPage;

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

        bool built;

        public static FoundationConfigMenu Attach(OptionsUI ui)
        {
            if (ui == null) return null;
            var menu = ui.GetComponent<FoundationConfigMenu>() ?? ui.gameObject.AddComponent<FoundationConfigMenu>();
            menu.Build(ui);
            return menu;
        }

        public void Build(OptionsUI source)
        {
            if (built) return;
            optionsUI = source;
            rootRect = (RectTransform)transform;
            rootImage = GetComponent<Image>();

            // Ensure OptionsUI._parent and _ingameParent are populated even if Start() hasn't executed yet
            var parentField = AccessTools.Field(typeof(OptionsUI), "_parent");
            var ingameField = AccessTools.Field(typeof(OptionsUI), "_ingameParent");
            if (parentField != null && parentField.GetValue(source) == null)
                parentField.SetValue(source, source.GetComponentInParent<MainMenuUI>() ?? MainMenuUI.Instance);
            if (ingameField != null && ingameField.GetValue(source) == null)
                ingameField.SetValue(source, source.GetComponentInParent<InGameMenuUI>() ?? UnityEngine.Object.FindAnyObjectByType<InGameMenuUI>());

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

            // Hook API change events
            FoundationApi.ModConfig.Changed += OnRegistryChanged;

            built = true;
            ShowGameTab();
        }

        void OnDestroy()
        {
            FoundationApi.ModConfig.Changed -= OnRegistryChanged;
        }

        void OnEnable()
        {
            if (!built && optionsUI != null) Build(optionsUI);
            AdjustWindowTransform();
            if (isModsTabActive) ShowModsTab();
            else ShowGameTab();
        }

        void AdjustWindowTransform()
        {
            if (!rootRect) return;
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
                float badgeWidth = Mathf.Max(240f, textWidth + 80f);
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
                r.sizeDelta = new Vector2(180, 52); // Taller: 52px

                var text = applyButton.GetComponentInChildren<TMP_Text>(true);
                if (text)
                {
                    text.alignment = TextAlignmentOptions.Center;
                    text.rectTransform.anchorMin = Vector2.zero;
                    text.rectTransform.anchorMax = Vector2.one;
                    text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
                }

                // Wrap Apply to also commit ModConfig
                applyButton.onClick.AddListener(CommitModConfigs);
            }

            var cancelTrans = transform.Find("Cancel");
            if (cancelTrans)
            {
                cancelButton = cancelTrans.GetComponent<Button>();
                var r = (RectTransform)cancelTrans;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(1, 0);
                r.anchoredPosition = new Vector2(-235, 20);
                r.sizeDelta = new Vector2(180, 52); // Taller: 52px

                var text = cancelButton.GetComponentInChildren<TMP_Text>(true);
                if (text)
                {
                    text.alignment = TextAlignmentOptions.Center;
                    text.rectTransform.anchorMin = Vector2.zero;
                    text.rectTransform.anchorMax = Vector2.one;
                    text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
                }

                cancelButton.onClick.AddListener(CancelModConfigs);
            }
        }

        void CreateTabs()
        {
            tabContainer = new GameObject("Tabs", typeof(RectTransform)).GetComponent<RectTransform>();
            tabContainer.SetParent(transform, false);
            tabContainer.anchorMin = new Vector2(0, 1);
            tabContainer.anchorMax = new Vector2(1, 1);
            tabContainer.pivot = new Vector2(0.5f, 1);
            tabContainer.anchoredPosition = new Vector2(0, -40);
            tabContainer.sizeDelta = new Vector2(0, 36); // Reduced height: 36px

            // Tab button template from Apply button
            var template = applyButton != null ? applyButton.gameObject : null;

            // Game Settings Tab
            var gameBtnObj = template != null ? Instantiate(template, tabContainer) : new GameObject("GameTab", typeof(RectTransform), typeof(Image), typeof(Button));
            gameBtnObj.name = "GameTabButton";
            gameTabButton = gameBtnObj.GetComponent<Button>();
            gameTabButton.onClick = new Button.ButtonClickedEvent();
            gameTabButton.onClick.AddListener(() => { UISounds.TurnPage(); ShowGameTab(); });

            var gRect = (RectTransform)gameBtnObj.transform;
            gRect.anchorMin = new Vector2(0, 0);
            gRect.anchorMax = new Vector2(0, 1);
            gRect.pivot = new Vector2(0, 0.5f);
            gRect.anchoredPosition = new Vector2(35, 0);
            gRect.sizeDelta = new Vector2(210, 36); // Sleek 36px height

            gameTabImage = gameBtnObj.GetComponent<Image>();
            activeTabSprite = gameTabImage ? gameTabImage.sprite : null;
            gameTabText = gameBtnObj.GetComponentInChildren<TMP_Text>(true);
            if (gameTabText)
            {
                gameTabText.text = "Game Settings";
                gameTabText.fontSize = 17;
                gameTabText.alignment = TextAlignmentOptions.Center;
                var gtr = gameTabText.rectTransform;
                gtr.anchorMin = Vector2.zero;
                gtr.anchorMax = Vector2.one;
                gtr.offsetMin = gtr.offsetMax = Vector2.zero;
            }

            // Mods Settings Tab
            var modsBtnObj = template != null ? Instantiate(template, tabContainer) : new GameObject("ModsTab", typeof(RectTransform), typeof(Image), typeof(Button));
            modsBtnObj.name = "ModsTabButton";
            modsTabButton = modsBtnObj.GetComponent<Button>();
            modsTabButton.onClick = new Button.ButtonClickedEvent();
            modsTabButton.onClick.AddListener(() => { UISounds.TurnPage(); ShowModsTab(); });

            var mRect = (RectTransform)modsBtnObj.transform;
            mRect.anchorMin = new Vector2(0, 0);
            mRect.anchorMax = new Vector2(0, 1);
            mRect.pivot = new Vector2(0, 0.5f);
            mRect.anchoredPosition = new Vector2(265, 0);
            mRect.sizeDelta = new Vector2(210, 36); // Sleek 36px height

            modsTabImage = modsBtnObj.GetComponent<Image>();
            modsTabText = modsBtnObj.GetComponentInChildren<TMP_Text>(true);
            if (modsTabText)
            {
                modsTabText.text = "Mod Settings";
                modsTabText.fontSize = 17;
                modsTabText.alignment = TextAlignmentOptions.Center;
                var mtr = modsTabText.rectTransform;
                mtr.anchorMin = Vector2.zero;
                mtr.anchorMax = Vector2.one;
                mtr.offsetMin = mtr.offsetMax = Vector2.zero;
            }

            // Horizontal divider line under tabs
            var lineObj = new GameObject("TabDivider", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(tabContainer, false);
            var lRect = (RectTransform)lineObj.transform;
            lRect.anchorMin = new Vector2(0, 0);
            lRect.anchorMax = new Vector2(1, 0);
            lRect.pivot = new Vector2(0.5f, 0);
            lRect.anchoredPosition = new Vector2(0, -6);
            lRect.sizeDelta = new Vector2(-60, 2);
            var lineImg = lineObj.GetComponent<Image>();
            lineImg.color = new Color(0.55f, 0.45f, 0.28f, 0.85f);
        }

        void BuildGamePage()
        {
            gamePage = new GameObject("GameSettingsPage", typeof(RectTransform));
            gamePage.transform.SetParent(transform, false);
            var pageRect = (RectTransform)gamePage.transform;
            pageRect.anchorMin = new Vector2(0, 0);
            pageRect.anchorMax = new Vector2(1, 1);
            pageRect.offsetMin = new Vector2(30, 84);
            pageRect.offsetMax = new Vector2(-30, -96);

            // Locate the native OptionsContent containing vanilla rows
            var nativeContent = transform.Find("OptionsContent") as RectTransform;
            if (nativeContent)
            {
                // Disable its vertical layout group so we can arrange cleanly into two columns
                var oldVlg = nativeContent.GetComponent<VerticalLayoutGroup>();
                if (oldVlg) oldVlg.enabled = false;
                var oldContentSize = nativeContent.GetComponent<ContentSizeFitter>();
                if (oldContentSize) oldContentSize.enabled = false;

                // Reparent native content to gamePage
                nativeContent.SetParent(gamePage.transform, false);
                nativeContent.anchorMin = Vector2.zero;
                nativeContent.anchorMax = Vector2.one;
                nativeContent.offsetMin = nativeContent.offsetMax = Vector2.zero;

                // Create Left Column: Display & Interface
                var leftCol = CreateColumn(nativeContent, "DisplayColumn", new Vector2(0, 0), new Vector2(0.485f, 1));
                CreateSectionHeader(leftCol, "Display & Interface");

                // Move Resolution, WindowMode, Framerate, UIScale into Left Column
                MoveRow(nativeContent, leftCol, "Resolution");
                MoveRow(nativeContent, leftCol, "WindowMode");
                MoveRow(nativeContent, leftCol, "Framerate");
                MoveRow(nativeContent, leftCol, "UIScale");

                // Create Right Column: Audio & Controls
                var rightCol = CreateColumn(nativeContent, "AudioColumn", new Vector2(0.515f, 0), new Vector2(1, 1));
                CreateSectionHeader(rightCol, "Audio & System");

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

        void CreateSectionHeader(Transform parent, string title)
        {
            templates.CreateTextRow(parent, title, true, out _);
        }

        void MoveRow(Transform nativeParent, Transform newParent, string childName)
        {
            var child = nativeParent.Find(childName);
            if (child)
            {
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
            pageRect.offsetMax = new Vector2(-30, -96);

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
            if (gameTabText) { stText.font = gameTabText.font; stText.fontSharedMaterial = gameTabText.fontSharedMaterial; }
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
            if (gameTabText) { noModsText.font = gameTabText.font; noModsText.fontSharedMaterial = gameTabText.fontSharedMaterial; }
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
            if (gameTabText) { modsHeaderTitle.font = gameTabText.font; modsHeaderTitle.fontSharedMaterial = gameTabText.fontSharedMaterial; }
            modsHeaderTitle.fontStyle = FontStyles.Bold;
            modsHeaderTitle.fontSize = 22;
            modsHeaderTitle.alignment = TextAlignmentOptions.MidlineLeft;

            var descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(mHeaderObj.transform, false);
            modsHeaderDesc = descObj.GetComponent<TextMeshProUGUI>();
            if (gameTabText) { modsHeaderDesc.font = gameTabText.font; modsHeaderDesc.fontSharedMaterial = gameTabText.fontSharedMaterial; }
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
            if (rootImage) { bgImg.sprite = rootImage.sprite; bgImg.type = Image.Type.Sliced; }
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
                if (btnImg) { hImg.sprite = btnImg.sprite; hImg.type = Image.Type.Sliced; }
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
            isModsTabActive = false;
            if (gamePage) gamePage.SetActive(true);
            if (modsPage) modsPage.SetActive(false);

            if (gameTabImage) gameTabImage.color = activeTabColor;
            if (gameTabText) gameTabText.fontStyle = FontStyles.Bold;
            if (modsTabImage) modsTabImage.color = inactiveTabColor;
            if (modsTabText) modsTabText.fontStyle = FontStyles.Normal;
        }

        public void ShowModsTab(string modId = null)
        {
            isModsTabActive = true;
            if (gamePage) gamePage.SetActive(false);
            if (modsPage) modsPage.SetActive(true);

            if (modsTabImage) modsTabImage.color = activeTabColor;
            if (modsTabText) modsTabText.fontStyle = FontStyles.Bold;
            if (gameTabImage) gameTabImage.color = inactiveTabColor;
            if (gameTabText) gameTabText.fontStyle = FontStyles.Normal;

            if (!string.IsNullOrEmpty(modId))
                selectedModId = modId;

            RefreshModsList();
        }

        void OnRegistryChanged()
        {
            if (isModsTabActive && gameObject.activeInHierarchy)
                RefreshModsList();
        }

        public void RefreshModsList()
        {
            if (!modsSidebarContent) return;

            var sections = FoundationApi.ModConfig.Sections;
            noModsText.gameObject.SetActive(sections.Count == 0);

            // Rebuild buttons if count or IDs changed
            foreach (var btn in modButtons.Values)
                if (btn) Destroy(btn.gameObject);
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
                btn.onClick.AddListener(() => {
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
                    if (img) img.color = pair.Key == selectedModId ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.75f);
                }
            }

            RenderSelectedModSettings();
        }

        void RenderSelectedModSettings()
        {
            if (!modsSettingsContent) return;

            // Clear old elements immediately
            for (int i = modsSettingsContent.childCount - 1; i >= 0; i--)
            {
                var child = modsSettingsContent.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            if (string.IsNullOrEmpty(selectedModId) || !FoundationApi.ModConfig.TryGetSection(selectedModId, out var section))
            {
                if (modsHeaderTitle) modsHeaderTitle.text = "Select a Mod";
                if (modsHeaderDesc) modsHeaderDesc.text = "";
                return;
            }

            if (modsHeaderTitle) modsHeaderTitle.text = section.ModName;
            if (modsHeaderDesc) modsHeaderDesc.text = section.Description;

            var context = new ConfigContext(modsSettingsContent, templates);
            foreach (var el in section.Elements)
            {
                if (el != null)
                {
                    try { el.CreateView(context); }
                    catch (Exception ex) { FoundationApi.Report(ex); }
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
            foreach (var section in FoundationApi.ModConfig.Sections)
            {
                try { section.Commit(); }
                catch (Exception ex) { FoundationApi.Report(ex); }
            }
        }

        internal void CancelModConfigs()
        {
            foreach (var section in FoundationApi.ModConfig.Sections)
            {
                try { section.Cancel(); }
                catch (Exception ex) { FoundationApi.Report(ex); }
            }
        }

        sealed class ConfigContext : IModConfigContext
        {
            public RectTransform Container { get; }
            public IModConfigTemplates Templates { get; }

            public ConfigContext(RectTransform container, IModConfigTemplates templates)
            {
                Container = container;
                Templates = templates;
            }
        }
    }
}
