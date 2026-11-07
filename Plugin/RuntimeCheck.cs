using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        readonly List<string> checks = new List<string>();
        readonly List<string> errors = new List<string>();
        void Check(bool pass, string name) { if (!pass) throw new InvalidOperationException(name); checks.Add(name); }
        void WatchErrors(string message, string stack, LogType type)
        { if (type == LogType.Exception) errors.Add(message + "\n" + stack); }
        UITechNode Node(TechTreeMenu menu, string id) => menu.Nodes.Single(n => n.Tech.Identifier == id);

        IEnumerator RuntimeCheck()
        {
            Application.logMessageReceived += WatchErrors;
            yield return new WaitForSecondsRealtime(5);
            string directory = Path.Combine(Paths.GameRootPath, "_Mods", "Foundation", "Verification");
            Directory.CreateDirectory(directory);
            string error = null;
            Mouse mouse = null;
            TechTreeMenu menu = null; TechTreeTabs tabs = null; Camera camera = null;
            Vector3 basePosition = default; float baseSize = 0;
            int initialResearchCount = 0;
            TechNode first = null, upgrade = null;
            const string FirstId = "urakka.foundation.example.first", UpgradeId = "urakka.foundation.example.upgrade";
            try
            {
                Check(GamePlayer.Current == null && !GameUI.Instance, "No user kingdom loaded");
                Check(!SaveGame.LoadLatestSave(), "Save loading blocked in diagnostic runtime");
                VerifyRegistry();
                initialResearchCount = FoundationApi.TechNodes.Nodes.Count;
                VerifyWorkspaceRegistry();
                VerifyModConfig();
                var player = GamePlayer.CreateNewPlayer();
                player.Flows[0].Label = "Foundation native research verification";
                SceneManager.LoadScene("Game");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(4);
            if (error == null) try
            {
                mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
                VerifyModBar(directory);
            }
            catch (Exception ex) { error = ex.ToString(); }
            // Native dropdowns remove their blocker after the closing fade.
            yield return new WaitForSecondsRealtime(.25f);
            if (error == null) try
            {
                FinishModBarChecks(directory);
                VerifyWorkspaceLocked();
                GameUI.Instance.ToggleTechMenu(true);
                menu = TechTreeMenu.Instance; tabs = Tabs(menu); camera = TechTreeTabs.Field<Camera>(menu, "_camera");
                Check(tabs && tabs.Bar.GetComponentsInChildren<Button>().Length == 2, "Tech Tree heading has two tabs");
                Check(tabs.Heading.GetComponentInChildren<TMPro.TMP_Text>().text == "Tech Tree" && tabs.ResearchButton.GetComponentInChildren<TMPro.TMP_Text>().text == "Base Game" && tabs.ModsButton.GetComponentInChildren<TMPro.TMP_Text>().text == "Mods", "Requested heading and tab labels retained");
                var headingCorners = new Vector3[4]; tabs.Heading.GetWorldCorners(headingCorners);
                var hudCorners = new Vector3[4]; testedBar.Bar.GetWorldCorners(hudCorners);
                Check(headingCorners[1].y < hudCorners[0].y, "Tech Tree heading stays below the shared Mod Bar");
                Check(menu.Nodes.Any() && menu.Nodes.All(n => !NativeResearch.Owns(n.Tech)), "Base Game contains only original and existing mod research");
                basePosition = camera.transform.position; baseSize = camera.orthographicSize;
                Capture(camera, Path.Combine(directory, "foundation-tech-tree.png"));
                GamePlayer.Current.AddTech("global_tech_upgrade"); menu.UpdateTechListButton();
                Click(tabs.ModsButton);
                tabs.SelectMod("urakka.foundation.example", false);
                first = TechNode.Get(FirstId); upgrade = TechNode.Get(UpgradeId);
                Check(tabs.ModsSelected && menu.Nodes.Count() == 2 && menu.Nodes.All(n => NativeResearch.ModOf(n.Tech)?.Id == "urakka.foundation.example"), "Selected mod category renders only its own two root technologies");
                Check(!tabs.ModsPage.GetComponent<ScrollRect>() && menu.Nodes.All(n => n.transform.parent == TechTreeTabs.Field<RectTransform>(menu, "_nodeParent")), "Mod research uses the native world canvas, without overlay cards or scroll containers");
                var prefab = TechTreeTabs.Field<UITechNode>(menu, "_elementNodePrefab");
                var rootView = Node(menu, FirstId);
                Check(rootView.GetType() == prefab.GetType() && ((RectTransform)rootView.transform).sizeDelta == ((RectTransform)prefab.transform).sizeDelta, "Root node uses the exact native prefab type and dimensions");
                Check(rootView.Tech == first && ((RectTransform)rootView.transform).anchoredPosition == first.Position, "Native SetTech positions node in game coordinates");
                Check(rootView.GetTooltipTitle() == first.Name, "Native tooltip reads the mod research title");
                rootView.UpdateNodeStatus();
                Check(TechTreeTabs.Field<Image>(rootView, "_glow").gameObject.activeSelf && !TechTreeTabs.Field<Image>(rootView, "_purchased").gameObject.activeSelf, "Native available glow and unpurchased state");
                rootView.OnPointerEnter(new PointerEventData(EventSystem.current));
                Check(TechTreeTabs.Field<Image>(rootView, "_highlight").gameObject.activeSelf, "Native hover highlight works");
                rootView.OnPointerExit(new PointerEventData(EventSystem.current));
                Check(!TechTreeTabs.Field<Image>(rootView, "_highlight").gameObject.activeSelf, "Native hover highlight clears");
                Check(upgrade.IsLocked, "Child research locked until its native prerequisite is purchased");
                menu.ShowTechTree(first, true);
                var locked = Node(menu, UpgradeId); locked.UpdateNodeStatus();
                Check(TechTreeTabs.Field<Image>(locked, "_icon").sprite == TechTreeTabs.Field<Sprite>(locked, "_lockedIcon"), "Unpurchased prerequisite uses native locked icon");
                Check(TechTreeTabs.Field<RectTransform>(menu, "_nodeParent").GetComponentsInChildren<LineRenderer>().Any(), "Native branch rendering draws the prerequisite connector");
                menu.ShowTechTree(null, true);
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return null;
            if (error == null)
            {
                var navigation = VerifyModCategories(menu, tabs, camera, directory);
                while (error == null)
                {
                    bool more = false; object step = null;
                    try { more = navigation.MoveNext(); if (more) step = navigation.Current; }
                    catch (Exception ex) { error = ex.ToString(); }
                    if (!more || error != null) break;
                    yield return step;
                }
                (navigation as IDisposable)?.Dispose();
                navigation = VerifyRequirementNavigation(menu, tabs, camera, directory);
                while (error == null)
                {
                    bool more = false; object step = null;
                    try { more = navigation.MoveNext(); if (more) step = navigation.Current; }
                    catch (Exception ex) { error = ex.ToString(); }
                    if (!more || error != null) break;
                    yield return step;
                }
                (navigation as IDisposable)?.Dispose();
            }
            if (error == null) try
            {
                menu.FocusNode(TechNode.Get("mine")); menu.ShowTechTree(null, true);
                camera.transform.position = basePosition;
                tabs.Select(true, false); menu.ShowTechTree(null, true);
                camera.transform.position = new Vector3(0, 0, -10);
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return null;
            if (error == null) try
            {
                var player = GamePlayer.Current;
                Capture(camera, Path.Combine(directory, "foundation-mods-available.png"));
                long gold = player.GetCurrency(Currency.Gold);
                player.AddCurrency(Currency.Gold, 50 - gold, false);
                NativeClick(Node(menu, FirstId), camera);
                Check(!first.IsPurchased && player.GetCurrency(Currency.Gold) == 50, "Native insufficient-funds path leaves research and currency unchanged");
                player.AddCurrency(Currency.Gold, 1000, false);
                NativeClick(Node(menu, FirstId), camera);
                Check(first.IsPurchased && player.GetCurrency(Currency.Gold) == 950, "Native click buys research and spends the specified gold cost (purchased=" + first.IsPurchased + ", gold=" + player.GetCurrency(Currency.Gold) + ")");
                Check(first.Name == "Example Researched", "Native unlock event reaches the separately compiled consumer plugin");
                Check(!upgrade.IsLocked && upgrade.IsAvailable, "Native prerequisite unlock makes the child available");
                menu.UpdateTechTree();
                Check(TechTreeTabs.Field<Image>(Node(menu, FirstId), "_purchased").gameObject.activeSelf, "Native purchased checkmark appears");
                NativeClick(Node(menu, FirstId), camera);
                Check(menu.RootNode == first && menu.Nodes.Any(n => n.Tech == upgrade), "Clicking researched root opens its native upgrade branch");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return null;
            if (error == null) try
            {
                NativeClick(Node(menu, UpgradeId), camera);
                Check(upgrade.IsPurchased && GamePlayer.Current.GetCurrency(Currency.Gold) == 925, "Native upgrade purchase spends cost and saves research state");
                Check(menu.ProcessEscape() && menu.RootNode == null && tabs.ModsSelected, "Native Escape returns from branch to Mods root map");
                var native = ((INativeTechNode)FoundationApi.TechNodes.Nodes.Single(n => n.Id == FirstId)).NativeNode;
                Check(native == first, "Public interface exposes the actual native research model");
                var player = GamePlayer.Current;
                var json = player.ToJson();
                Check(json["Tech"].AsJsonArray.Any(v => v.AsString == FirstId) && json["Tech"].AsJsonArray.Any(v => v.AsString == UpgradeId), "Purchased mod technologies use the game's existing saved ID list");
                Check(!json["NewTechCount"].AsJsonObject.ContainsKey(FirstId), "Optional mod branch notification counters do not create missing-key save failures");
                try { var restored = GamePlayer.FromJson(json); Check(restored.HasTech(first) && restored.HasTech(upgrade), "Native JSON round trip retains mod research purchases"); }
                finally { GamePlayer.Current = player; }
                var pos = camera.transform.position; var size = camera.orthographicSize;
                WorldFlowCamera.Instance.Move(100, 80);
                Check(camera.transform.position != pos, "Native map panning works on Mods");
                Wheel(mouse, -120);
                Check(camera.orthographicSize > size, "Real mouse wheel uses native Mods map zoom");
                Wheel(mouse, 120);
                var modPos = camera.transform.position; var modSize = camera.orthographicSize;
                Click(tabs.ResearchButton);
                Check(!tabs.ModsSelected && Vector3.Distance(camera.transform.position, basePosition) < .01f && Mathf.Approximately(camera.orthographicSize, baseSize), "Switching to Base Game restores its native camera position and zoom");
                Check(menu.Nodes.All(n => !NativeResearch.Owns(n.Tech)), "Native mod research stays out of Base Game after purchases");
                Click(tabs.ModsButton);
                Check(Vector3.Distance(camera.transform.position, modPos) < .01f && Mathf.Approximately(camera.orthographicSize, modSize), "Returning to Mods restores its independent map position and zoom");
                camera.transform.position = new Vector3(0, 0, -10);
                var icon = TechNode.Get("mine").Icon;
                diagnosticNode = FoundationApi.CreateTechNode(new TechNodeDefinition("foundation.diagnostics.visible", "Late Registered", "urakka.foundation.example", "Foundation Example", new Vector2(-300, -220), "A real game tech node registered after the kingdom was loaded.", icon,
                    new TechNodeOptions(fixedCost: new CurrencyCollection())));
                Check(!((INativeTechNode)diagnosticNode).NativeNode.IsPurchased, "Late registration safely extends the player's native research storage");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return null;
            if (error == null) try
            {
                var late = Node(menu, diagnosticNode.Id);
                Check(late.Tech.Name == "Late Registered" && late.Tech.Icon == diagnosticNode.Definition.Icon, "Live registration automatically creates native view with caller-owned icon");
                var definition = diagnosticNode.Definition;
                diagnosticNode.Update(new TechNodeDefinition(diagnosticNode.Id, "Native Technology", definition.Position, definition.Description, definition.Icon, ((INativeTechNodeDefinition)definition).Research));
                menu.ToggleListView();
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return null;
            yield return null;
            if (error == null) try
            {
                Check(Node(menu, diagnosticNode.Id).Tech.Name == "Native Technology", "Definition updates refresh the native research view automatically");
                var list = TechTreeTabs.Field<TechTreeListView>(menu, "_listView");
                Check(list.gameObject.activeSelf, "Native research list available on Mods page");
                Check(list.GetComponentsInChildren<TechTreeListItem>().All(item => NativeResearch.Active(TechTreeTabs.Field<TechNode>(item, "_node"))), "Native Mods list filters out Base Game research");
                menu.ToggleListView();
                Capture(camera, Path.Combine(directory, "foundation-mods.png"));
                diagnosticNode.Dispose(); tabs.ModTree.Refresh();
                Check(!menu.Nodes.Any(n => n.Tech.Identifier == diagnosticNode.Id), "Disposal removes native view without reindexing saved research");
                Check(GamePlayer.Current.HasTech(first) && GamePlayer.Current.HasTech(upgrade), "Late registration and disposal preserve existing purchased research");
                menu.FocusNode(first);
                Check(tabs.ModsSelected && menu.RootNode == null, "Native focus routes a mod root to the correct tab");
                menu.FocusNode(TechNode.Get("mine"));
                Check(!tabs.ModsSelected, "Native focus routes original research back to Base Game");
                for (int i = 0; i < 2; i++)
                {
                    Click(tabs.ModsButton); GameUI.Instance.ToggleTechMenu(false); GameUI.Instance.ToggleTechMenu(true);
                    Check(!tabs.ModsSelected && menu.GetComponents<TechTreeTabs>().Length == 1 && menu.Nodes.All(n => !NativeResearch.Owns(n.Tech)), "Reopening starts on Base Game without duplicate UI " + i);
                }
                Check(menu.ProcessEscape() && !TechTreeMenu.Visible, "Native Escape closes the main research map");
                PrepareWorkspacePlacement();
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return null;
            if (error == null) try { ExerciseWorkspace(); }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(.3f);
            if (error == null) try { FinishWorkspace(directory); SceneManager.LoadScene("MainMenu"); }
            catch (Exception ex) { error = ex.ToString(); }
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            yield return new WaitForSecondsRealtime(2);
            if (error == null) try
            {
                Check(!menu && !tabs, "Leaving the game scene destroys Foundation UI cleanly");
                Check(!testedBar && lifecycleElement.Disposes == 1 && lifecycleItem.IsRegistered, "Scene teardown disposes the custom Mod Bar view once and preserves its registration");
                Check(FoundationApi.TechNodes.Nodes.Count == initialResearchCount, "Native registrations survive scene teardown");
                var previewPlayer = GamePlayer.CreateNewPlayer(); previewPlayer.AddCurrency(Currency.Gold, 123456, false); SceneManager.LoadScene("Game");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(3);
            TMPro.TMP_Dropdown reopenedDropdown = null;
            if (error == null) try
            {
                var hosts = GameUI.Instance.GetComponentsInChildren<ModBarHost>(true);
                Check(hosts.Length == 1 && lifecycleElement.Creates == 2 && lifecycleElement.Disposes == 1,
                    "New game scene recreates one Mod Bar and a fresh custom view from the surviving registration");
                reopenedDropdown = hosts[0].Slots.Single(s => s.name == "urakka.foundation.example:period").GetComponentInChildren<TMPro.TMP_Dropdown>();
                Check(reopenedDropdown.value == 1, "Mod-owned dropdown state survives HUD recreation");
                reopenedDropdown.Show();
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(.25f);
            if (error == null) try
            {
                Capture(TechTreeTabs.Field<Camera>(WorldFlowManager.Instance, "_camera"), Path.Combine(directory, "foundation-mod-bar-dropdown.png"));
                GameUI.Instance.ProcessEscape();
                Check(!GameUI.MenuVisible, "Escape dismisses the Mod Bar dropdown without opening the game menu");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(.25f);
            if (error == null) try
            {
                Check(!reopenedDropdown.IsExpanded, "Mod Bar dropdown completes its native close and removes its blocker");
                reopenedDropdown.Show(); GameUI.Instance.ToggleUI(false);
                GameUI.Instance.ToggleUI(true);
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(.25f);
            if (error == null) try
            {
                Check(!reopenedDropdown.IsExpanded, "Hiding the native HUD also closes Mod Bar popups");
                SceneManager.LoadScene("MainMenu");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(2);
            if (error == null) try
            {
                Check(lifecycleElement.Disposes == 2 && lifecycleItem.IsRegistered, "Each HUD lifetime disposes its custom view exactly once");
                lifecycleItem.Dispose();
                Check(errors.Count == 0, "No Unity exceptions during research, workspace and Mod Bar diagnostics");
            }
            catch (Exception ex) { error = ex.ToString(); }
            var report = new JsonObject {
                {"passed", error == null}, {"version", Version}, {"gameVersion", Application.version},
                {"unityVersion", Application.unityVersion}, {"bepInExVersion", typeof(BaseUnityPlugin).Assembly.GetName().Version.ToString()},
                {"dllSha256", HashFile(typeof(FoundationPlugin).Assembly.Location)}, {"count", checks.Count},
                {"checks", new JsonArray(checks.Select(c => (JsonValue)c).ToArray())},
                {"plugins", new JsonArray(BepInEx.Bootstrap.Chainloader.PluginInfos.Values.Select(p => (JsonValue)(p.Metadata.Name + " " + p.Metadata.Version)).ToArray())},
                {"error", error ?? ""}, {"exceptions", new JsonArray(errors.Select(c => (JsonValue)c).ToArray())}
            };
            File.WriteAllText(Path.Combine(directory, "runtime-tests.json"), report.ToString());
            Application.logMessageReceived -= WatchErrors;
            if (error != null) Logger.LogError(error); else Logger.LogInfo("Foundation native diagnostic passed: " + checks.Count + " checks.");
            Application.Quit();
        }
        void NativeClick(UITechNode node, Camera camera)
        {
            Canvas.ForceUpdateCanvases();
            var center = node.transform.TransformPoint(((RectTransform)node.transform).rect.center);
            var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(camera, center), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Check(hits.Any(hit => hit.gameObject.GetComponentInParent<UITechNode>()?.Tech == node.Tech), "Native research node receives pointer raycasts: " + node.Tech.Identifier);
            ExecuteEvents.Execute(node.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        void Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, button.transform.TransformPoint(((RectTransform)button.transform).rect.center));
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Tab receives pointer raycasts: " + button.name);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        static void Wheel(Mouse mouse, float delta)
        {
            InputSystem.QueueDeltaStateEvent(mouse.scroll, Vector2.zero); InputSystem.Update();
            InputSystem.QueueDeltaStateEvent(mouse.scroll, new Vector2(0, delta)); InputSystem.Update();
            InputSystem.QueueDeltaStateEvent(mouse.scroll, Vector2.zero); InputSystem.Update();
        }
        static string HashFile(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        static void Capture(Camera camera, string path)
        {
            Canvas.ForceUpdateCanvases();
            var overlays = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var cameras = overlays.Select(c => c.worldCamera).ToArray();
            var distances = overlays.Select(c => c.planeDistance).ToArray();
            var orders = overlays.Select(c => c.sortingOrder).ToArray();
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var previous = RenderTexture.active; var oldTarget = camera.targetTexture; int oldMask = camera.cullingMask;
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                foreach (var canvas in overlays) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; canvas.sortingOrder = 30000; }
                camera.cullingMask = -1; camera.targetTexture = target;
                Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); texture.Apply();
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.cullingMask = oldMask;
                for (int i = 0; i < overlays.Length; i++) { overlays[i].renderMode = RenderMode.ScreenSpaceOverlay; overlays[i].worldCamera = cameras[i]; overlays[i].planeDistance = distances[i]; overlays[i].sortingOrder = orders[i]; }
                Canvas.ForceUpdateCanvases(); RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(texture);
            }
        }
        void InstallDiagnostics()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var safety = new Harmony(Id + ".diagnostics");
            foreach (var type in new[] { typeof(NoSave), typeof(NoAutosave), typeof(NoLoad), typeof(NoAchievement), typeof(Offline) }) safety.CreateClassProcessor(type).Patch();
        }
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.Store))] static class NoSave { static bool Prefix() => false; }
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.StoreAutosaveState))] static class NoAutosave { static bool Prefix() => false; }
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.LoadLatestSave))] static class NoLoad { static bool Prefix(ref bool __result) { __result = false; return false; } }
        [HarmonyPatch(typeof(SteamAchievement), nameof(SteamAchievement.Trigger))] static class NoAchievement { static bool Prefix() => false; }
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Initialized), MethodType.Getter)] static class Offline { static bool Prefix(ref bool __result) { __result = false; return false; } }
    }
}

