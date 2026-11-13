using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Dungeon;
using Assets.Behaviour.UI.TechTree;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Behaviour.Flow;
using Assets.Source.Player;
using System.Collections.Generic;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    public sealed class TechTreeTabs : MonoBehaviour
    {
        internal string SelectedModId { get; private set; }

        internal ModResearchSidebar Sidebar;
        readonly Dictionary<string, global::KingdomFoundation.TechTreeTabsModState> modStates = new Dictionary<string, global::KingdomFoundation.TechTreeTabsModState>();
        void StoreModCategory()
        {
            if (SelectedModId != null)
                modStates[SelectedModId] = new global::KingdomFoundation.TechTreeTabsModState
                {
                    Root = modsRoot,
                    Camera = modsCamera
                };
        }

        void LoadModCategory()
        {
            modStates.TryGetValue(SelectedModId ?? "", out global::KingdomFoundation.TechTreeTabsModState saved);
            modsRoot = saved?.Root;
            modsCamera = saved?.Camera;
            if (modsRoot != null && (!NativeResearch.Active(modsRoot) || NativeResearch.ModOf(modsRoot)?.Id != SelectedModId))
                modsRoot = null;
        }

        global::KingdomFoundation.TechTreeTabsCameraState DefaultModCamera()
        {
            global::KingdomFoundation.TechTreeTabsCameraState state = Capture();
            var parent = Field<UnityEngine.RectTransform>(menu, "_nodeParent");
            var bounds = menu.Nodes.Select(n => ModResearchLayout.Bounds(n, parent)).ToArray();
            if (bounds.Length == 0)
            {
                state.Position = new UnityEngine.Vector3(0, 0, -10);
                return state;
            }

            var center = new UnityEngine.Vector2((bounds.Min(r => r.xMin) + bounds.Max(r => r.xMax)) / 2, (bounds.Min(r => r.yMin) + bounds.Max(r => r.yMax)) / 2);
            var corners = new UnityEngine.Vector3[4];
            Sidebar.Panel.GetWorldCorners(corners);
            float reserved = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, corners[2]).x;
            state.Position = new UnityEngine.Vector3(center.x - reserved * state.Size / UnityEngine.Screen.height, center.y, -10);
            return state;
        }

        internal bool EnsureModCategory()
        {
            var mods = FoundationApi.TechMods;
            bool changed = !mods.Any(m => m.Id == SelectedModId);
            if (changed)
            {
                SelectedModId = mods.FirstOrDefault()?.Id;
                LoadModCategory();
            }

            Sidebar?.Refresh();
            return changed;
        }

        internal void SelectMod(string id, bool sound = true)
        {
            if (!FoundationApi.TechMods.Any(m => m.Id == id))
                return;
            if (SelectedModId == id)
            {
                Select(true, sound);
                return;
            }

            if (ModsSelected)
            {
                modsRoot = menu.RootNode;
                modsCamera = Capture();
                StoreModCategory();
            }

            SelectedModId = id;
            LoadModCategory();
            if (!ModsSelected)
            {
                Select(true, sound);
                return;
            }

            ShowSelectedMod();
            if (sound)
                UISounds.TurnPage();
        }

        internal void ShowSelectedMod()
        {
            list.gameObject.SetActive(false);
            Switching = true;
            try
            {
                menu.ShowTechTree(modsRoot, true);
                if (modsCamera == null)
                    modsCamera = DefaultModCamera();
                Restore(modsCamera);
            }
            finally
            {
                Switching = false;
            }

            Sidebar.Refresh();
            menu.UpdateTechListButton();
        }

        internal void SelectFor(TechNode node)
        {
            if (NativeResearch.Active(node))
                SelectMod(NativeResearch.ModOf(node).Id, false);
            else
                Select(false, false);
        }

        internal bool ModsSelected { get; private set; }

        internal RectTransform Bar;
        internal RectTransform Heading;
        internal Button ResearchButton, ModsButton;
        internal GameObject ModsPage;
        internal ModTechTreeView ModTree;
        TechTreeMenu menu;
        RectTransform nodes;
        GameObject nativeTitle, listButton, listHint;
        TechTreeListView list;
        Image researchImage, modsImage;
        Sprite activeSprite, defaultSprite;
        bool titleWasActive;
        Vector2 originalListButtonPosition, originalListOffset;
        internal bool Switching;
        TechNode baseRoot, modsRoot;
        global::KingdomFoundation.TechTreeTabsCameraState baseCamera, modsCamera;
        global::KingdomFoundation.TechTreeTabsCameraState Capture()
        {
            var camera = Field<Camera>(menu, "_camera");
            if (!camera)
                return null;
            return new global::KingdomFoundation.TechTreeTabsCameraState
            {
                Position = camera.transform.position,
                Zoom = WorldFlowCamera.Instance ? WorldFlowCamera.Instance.ZoomLevel : 3,
                Size = camera.orthographicSize
            };
        }

        void Restore(global::KingdomFoundation.TechTreeTabsCameraState state)
        {
            if (state == null)
                return;
            var camera = Field<Camera>(menu, "_camera");
            if (!camera)
                return;
            if (GamePlayer.Current != null && WorldFlowCamera.Instance)
            {
                WorldFlowCamera.Instance.SetZoomLevel(state.Zoom);
                // Use the existing zoom handler to retain extended Better Zoom levels.
                for (int i = 0; i < 20 && !Mathf.Approximately(camera.orthographicSize, state.Size); i++)
                {
                    float before = camera.orthographicSize;
                    WorldFlowCamera.Instance.ChangeZoomLevel(camera.orthographicSize > state.Size ? 1 : -1);
                    if (Mathf.Approximately(before, camera.orthographicSize))
                        break;
                }
            }

            camera.transform.position = state.Position;
        }

        internal static T Field<T>(object owner, string name) => (T)AccessTools.Field(owner.GetType(), name).GetValue(owner);
        internal void Open(TechTreeMenu owner)
        {
            if (!Bar)
                Build(owner);
            ModsSelected = false;
            baseRoot = null;
            nodes.gameObject.SetActive(true);
            ModsPage.SetActive(false);
            EnsureModCategory();
            Sidebar.Panel.gameObject.SetActive(false);
            titleWasActive = nativeTitle && nativeTitle.activeSelf;
            if (nativeTitle)
                nativeTitle.SetActive(false);
            Bar.gameObject.SetActive(true);
            Heading.gameObject.SetActive(true);
            Paint();
        }

        void Build(TechTreeMenu owner)
        {
            menu = owner;
            nodes = Field<RectTransform>(menu, "_nodeParent");
            listButton = Field<GameObject>(menu, "_listViewButton");
            listHint = Field<GameObject>(menu, "_listViewHint");
            list = Field<TechTreeListView>(menu, "_listView");
            originalListButtonPosition = ((RectTransform)listButton.transform).anchoredPosition;
            originalListOffset = ((RectTransform)list.transform).offsetMax;
            ((RectTransform)listButton.transform).anchoredPosition -= new Vector2(0, ModBarHost.ReservedHeight);
            ((RectTransform)list.transform).offsetMax -= new Vector2(0, ModBarHost.ReservedHeight);
            var ui = Field<RectTransform>(menu, "_uiLabel");
            var title = ui.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "TechTreeLabel");
            nativeTitle = title ? title.gameObject : null;
            var font = (title ? title.GetComponentInChildren<TMP_Text>(true) : null) ?? ui.GetComponentInChildren<TMP_Text>(true);
            var dungeon = Resources.FindObjectsOfTypeAll<DungeonMenu>().First();
            var tab = Field<DungeonMenuTab>(dungeon, "_tabPrefab");
            var image = Field<Image>(tab, "_background");
            defaultSprite = image.sprite;
            activeSprite = Field<Sprite>(tab, "_activeSprite");
            if (!font)
                font = Field<TMP_Text>(tab, "_label");
            Heading = Rect("FoundationTechTreeHeading", ui);
            Heading.anchorMin = Heading.anchorMax = new Vector2(.5f, 1);
            Heading.pivot = new Vector2(.5f, 1);
            Heading.anchoredPosition = new Vector2(0, -52 - ModBarHost.ReservedHeight);
            Heading.sizeDelta = new Vector2(240, 44);
            var headingBackground = Heading.gameObject.AddComponent<Image>();
            var titleImage = nativeTitle ? nativeTitle.GetComponent<Image>() : null;
            headingBackground.sprite = titleImage ? titleImage.sprite : activeSprite;
            headingBackground.type = Image.Type.Sliced;
            headingBackground.pixelsPerUnitMultiplier = titleImage ? titleImage.pixelsPerUnitMultiplier : image.pixelsPerUnitMultiplier;
            headingBackground.raycastTarget = false;
            MakeLabel(Heading, "Tech Tree", font);
            Bar = Rect("FoundationTabs", ui);
            Bar.anchorMin = Bar.anchorMax = new Vector2(.5f, 1);
            Bar.pivot = new Vector2(.5f, 1);
            Bar.anchoredPosition = new Vector2(0, -104 - ModBarHost.ReservedHeight);
            Bar.sizeDelta = new Vector2(460, 54);
            ResearchButton = MakeTab("Base Game", 0, font, image, out researchImage);
            ModsButton = MakeTab("Mods", 240, font, image, out modsImage);
            var divider = Rect("Divider", Bar);
            divider.anchorMin = divider.anchorMax = new Vector2(0, 1);
            divider.pivot = new Vector2(0, 1);
            divider.anchoredPosition = new Vector2(220, 0);
            divider.sizeDelta = new Vector2(20, 54);
            MakeLabel(divider, "|", font, 0);
            ResearchButton.onClick.AddListener(() => Select(false));
            ModsButton.onClick.AddListener(() => Select(true));
            // Marker only: actual nodes are rendered by TechTreeMenu on its world canvas.
            var page = Rect("FoundationModsPage", ui);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = Vector2.zero;
            page.offsetMax = new Vector2(0, -168 - ModBarHost.ReservedHeight);
            ModsPage = page.gameObject;
            ModsPage.SetActive(false);
            ModTree = menu.gameObject.AddComponent<ModTechTreeView>();
            ModTree.Initialize(menu);
            Sidebar = gameObject.AddComponent<ModResearchSidebar>();
            Sidebar.Initialize(this, ui, font, image, activeSprite, Field<Image>(Field<UITechNode>(menu, "_elementNodePrefab"), "_background"));
            Bar.SetAsLastSibling();
        }

        Button MakeTab(string label, float x, TMP_Text font, Image source, out Image background)
        {
            var rect = Rect(label + "Tab", Bar);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, 0);
            rect.sizeDelta = new Vector2(220, 54);
            background = rect.gameObject.AddComponent<Image>();
            background.sprite = defaultSprite;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(.85f, .85f, .85f);
            button.colors = colors;
            MakeLabel(rect, label, font);
            return button;
        }

        void MakeLabel(RectTransform parent, string label, TMP_Text font, float inset = 12)
        {
            // Copy only text rendering properties; no localization or native click handlers.
            var textRect = Rect("Label", parent);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(inset, 6);
            textRect.offsetMax = new Vector2(-inset, -6);
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font.font;
            text.fontSharedMaterial = font.fontSharedMaterial;
            text.fontSize = 24;
            text.color = font.color;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;
            text.raycastTarget = false;
        }

        internal static RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal void Select(bool mods, bool sound = true)
        {
            if (!Bar || ModsSelected == mods)
                return;
            if (mods)
                EnsureModCategory();
            if (mods)
            {
                baseRoot = menu.RootNode;
                baseCamera = Capture();
            }
            else
            {
                modsRoot = menu.RootNode;
                modsCamera = Capture();
                StoreModCategory();
            }

            list.gameObject.SetActive(false);
            ModsSelected = mods;
            ModsPage.SetActive(mods);
            Sidebar.Panel.gameObject.SetActive(mods);
            Switching = true;
            try
            {
                if (modsRoot != null && !Api.NativeResearch.Active(modsRoot))
                    modsRoot = null;
                menu.ShowTechTree(mods ? modsRoot : baseRoot, true);
                if (mods && modsCamera == null)
                {
                    modsCamera = DefaultModCamera();
                }

                Restore(mods ? modsCamera : baseCamera);
            }
            finally
            {
                Switching = false;
            }

            menu.UpdateTechListButton();
            Paint();
            Sidebar.Refresh();
            if (sound)
                UISounds.TurnPage();
        }

        void Paint()
        {
            researchImage.sprite = ModsSelected ? defaultSprite : activeSprite;
            modsImage.sprite = ModsSelected ? activeSprite : defaultSprite;
        }

        internal void Close()
        {
            // Scene teardown can clear GamePlayer.Current before OnDisable. Restore
            // owned visibility directly instead of calling back into research logic.
            if (ModsSelected)
            {
                modsRoot = menu.RootNode;
                modsCamera = Capture();
                StoreModCategory();
                ModsSelected = false;
                Restore(baseCamera);
            }

            if (Bar)
                Bar.gameObject.SetActive(false);
            if (Heading)
                Heading.gameObject.SetActive(false);
            if (ModsPage)
                ModsPage.SetActive(false);
            if (Sidebar && Sidebar.Panel)
                Sidebar.Panel.gameObject.SetActive(false);
            if (nativeTitle)
                nativeTitle.SetActive(titleWasActive);
        }

        void OnDestroy()
        {
            Close();
            if (listButton)
                ((RectTransform)listButton.transform).anchoredPosition = originalListButtonPosition;
            if (list)
                ((RectTransform)list.transform).offsetMax = originalListOffset;
            if (Bar)
                Destroy(Bar.gameObject);
            if (Heading)
                Destroy(Heading.gameObject);
            if (ModsPage)
                Destroy(ModsPage);
            if (ModTree)
                Destroy(ModTree);
            if (Sidebar)
                Destroy(Sidebar);
        }
    }
}
