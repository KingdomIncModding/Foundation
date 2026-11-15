using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using KingdomPipeWrench.Plugin;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KingdomFoundation.Verification
{
    /// <summary>Temporary opt-in integration runner. Never included in Foundation's production compilation.</summary>
    [BepInPlugin("urakka.kingdominc.pipewrench.runtimeverification", "Pipe Wrench Runtime Verification", "1.0.0")]
    [BepInDependency(PipeWrenchPlugin.Id)]
    public sealed class PipeToolbarRuntimePlugin : BaseUnityPlugin
    {
        private readonly StringBuilder report = new StringBuilder();
        private string output;
        private Keyboard keys;
        private Mouse mouse;
        private PipeWrenchPlugin plugin;
        private object toolbar;
        private int checks;
        private float started;
        private MouseState mouseState;
        private KeyboardState keyboardState;
        private static GameObject staleHover;

        private void Awake()
        {
            if (!Environment.GetCommandLineArgs().Contains("--pipewrench-verify-runtime")) { enabled = false; return; }
            output = Path.GetFullPath(Path.Combine(Paths.PluginPath, "../../_Mods/PipeWrench/_AI/Notes/Runtime-" + PipeWrenchPlugin.Version));
            Directory.CreateDirectory(output);
            var saves = Directory.CreateDirectory(Path.Combine(output, "TestSaves"));
            SaveGame.SavesPath = saves.FullName;
            AccessTools.Field(typeof(SaveGame), "SavesDir").SetValue(null, saves);
            AccessTools.Field(typeof(SaveGame), "_saves").SetValue(null, null);
            SaveGame.AutosaveFrequency = 0;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            EnsureInput();
            Application.runInBackground = true; started = Time.realtimeSinceStartup;
            Logger.LogInfo("Runtime verification uses isolated saves: " + saves.FullName);
        }

        private IEnumerator Start()
        {
            if (!enabled) yield break;
            var run = Run(); bool failed = false;
            while (true)
            {
                object next = null; bool active = false;
                try { active = run.MoveNext(); if (active) next = run.Current; }
                catch (Exception error) { report.AppendLine("FAIL: " + error); Logger.LogError(error); failed = true; }
                if (failed || !active) break;
                yield return next;
            }
            report.AppendLine((failed ? "FAIL" : "PASS") + ": " + checks + " runtime checks.");
            File.WriteAllText(Path.Combine(output, "result.txt"), report.ToString());
            Logger.LogInfo(report.ToString());
            Application.Quit(failed ? 1 : 0);
        }

        private IEnumerator Run()
        {
            for (int i = 0; i < 80; i++) yield return null;
            plugin = Resources.FindObjectsOfTypeAll<PipeWrenchPlugin>().Single();
            Check(plugin.Info.Metadata.Version.ToString() == PipeWrenchPlugin.Version, "The installed " + PipeWrenchPlugin.Version + " player DLL is loaded");
            toolbar = Field(plugin, "toolbar");
            Check(toolbar != null, "Production plugin completes Awake and loads the real nested toolbar JSON");
            GamePlayer.CreateNewPlayer();
            var second = new Forge(); second.SetupRecipe("iron_ingot"); second.Position = new Vector2Int(512, -64);
            GamePlayer.Current.ActiveFlow.Elements.Add(second);
            SceneManager.LoadScene("Game");
            for (int i = 0; i < 100; i++) yield return null;
            Check(GameUI.Instance && FoundationApi.UiAccess.IsWorkspaceVisible, "Native game workspace is visible");
            Check(FoundationApi.UiAccess.ModBarBounds, "Native Foundation mod-bar anchor exists");
            if (TutorialUI.Instance)
            {
                TutorialUI.Instance.NewGameContent.transform.parent.gameObject.SetActive(false);
                TutorialUI.Instance.enabled = false;
            }
            Assets.Behaviour.Input.PlayerInput.Enable();
            GameUI.Instance.ToggleUI(true);
            for (int i = 0; i < 5; i++) yield return null;
            var root = Root();
            Check(root && !root.gameObject.activeSelf, "Pipe tools are hidden before a connection starts");
            CheckBuildingLabelAvoidance();

            var source = GamePlayer.Current.ActiveFlow.Elements[0].Outputs[0].World;
            source.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            Check(FoundationApi.PipeDrag.Active != null, "Native port click starts a pipe connection");
            yield return null;
            Check(root.gameObject.activeInHierarchy, "Tools appear on the first frame after starting a connection without pointer movement");
            CheckDestroyedHover();
            var buttons = root.GetComponentsInChildren<Button>(true);
            Check(buttons.Length == 4 && buttons.All(button => button.gameObject.activeInHierarchy), "All four rule buttons are visible in the native HUD");
            Check(buttons.All(button => ((Image)button.targetGraphic).sprite), "Native UI button artwork resolves");
            Check(root.GetComponentsInChildren<TMP_Text>(true).Count(label => label.font && label.text.Length > 0) == 6, "All toolbar labels have loaded font assets");
            Check(buttons.All(button => button.GetComponentInChildren<TMP_Text>(true).font.name.Contains("LiberationSans")), "User-selected sans button font resolves inside Unity");
            var corners = new Vector3[4]; root.GetWorldCorners(corners);
            var canvas = root.GetComponentInParent<Canvas>();
            var screen = corners.Select(point => RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, point)).ToArray();
            Check(screen.All(point => point.x >= 0 && point.x <= Screen.width && point.y >= 0 && point.y <= Screen.height), "Toolbar lies inside the actual game viewport");
            yield return Screenshot("tools-started.png");

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.Invoke(); yield return null;
                var mode = plugin.Config[new BepInEx.Configuration.ConfigDefinition("Routing", "Avoidance")].BoxedValue;
                Check(Convert.ToInt32(mode) == i, "Toolbar button selects rule " + i);
                Check(root.gameObject.activeInHierarchy && FoundationApi.PipeDrag.Active != null, "Changing a rule preserves the active connection and tools");
            }
            buttons[0].onClick.Invoke();
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var buttonRect = (RectTransform)buttons[1].transform;
            var buttonPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, buttonRect.TransformPoint(buttonRect.rect.center));
            Position(buttonPoint); yield return null; yield return null; yield return null;
            report.AppendLine("Pointer: " + Mouse.current.name + " " + Mouse.current.position.ReadValue() + "; button center " + buttonPoint + "; canvas " + canvas.renderMode);
            Check((bool)AccessTools.Property(toolbar.GetType(), "IsPointerOver").GetValue(toolbar), "Toolbar hit testing recognizes its visible button center");
            AccessTools.Method(typeof(DragDropManager), "_dropTarget").Invoke(DragDropManager.Instance, null);
            yield return null;
            Check(FoundationApi.PipeDrag.Active != null && root.gameObject.activeInHierarchy, "Dropping over the toolbar keeps pipe placement alive");

            foreach (string mode in new[] { "NearStart", "NearEnd", "Freeform", "Center" })
            {
                Press(Key.P); yield return null; yield return null;
                Check(Field(Field(plugin, "routing"), "shape").ToString() == mode, "P cycles route mode " + mode);
                Check(root.gameObject.activeInHierarchy, "Tools remain visible in route mode " + mode);
                Press(); yield return null; yield return null;
            }

            var target = GamePlayer.Current.ActiveFlow.Elements[1].Inputs[0].World;
            Position(Camera.main.WorldToScreenPoint(target.transform.position));
            for (int i = 0; i < 8; i++) yield return null;
            Check(FoundationApi.PipeDrag.Active?.HoveredPort != null, "Native UI detects the target port");
            Check(!FoundationApi.Progression.HasTech("urakka.kingdominc.snappy.research"), "Isolated kingdom starts with Snappy research locked");
            Check(FoundationApi.NativeIO.SnapRouteNode(new Vector2Int(17, 0)).x == 32, "Before Snappy research the actual game uses its vanilla pipe grid");
            CheckPreviewSnap("vanilla");
            Check(FoundationApi.Progression.UnlockTech("urakka.kingdominc.snappy.research", false), "Unlock the installed Snappy research in the isolated test kingdom");
            for (int i = 0; i < 3; i++) yield return null;
            Check(FoundationApi.NativeIO.SnapRouteNode(new Vector2Int(17, 0)).x == 16, "Installed Snappy changes the actual native snap result after research");
            CheckPreviewSnap("Snappy");
            Press(Key.P); yield return null; yield return null; Press(); yield return null;
            var near = FoundationApi.PipeDrag.Active;
            int originX = FoundationApi.PipeDrag.ConnectionPoint(near.Start).x;
            int direction = FoundationApi.PipeDrag.OutwardDirection(near.Start);
            Check(near.Pipe.Nodes.Any(n => n.x == FoundationApi.NativeIO.SnapRouteNode(new Vector2Int(originX + direction * 16, 0)).x), "Near Start uses the live Snappy grid for its first routing lane");
            AccessTools.Method(typeof(DragDropManager), "_dropTarget").Invoke(DragDropManager.Instance, null);
            yield return null; yield return null;
            Check(FoundationApi.PipeDrag.Active == null, "Native pipe connection completes");
            Check(Field(Field(plugin, "editor"), "pipe") != null && root.gameObject.activeInHierarchy, "Tools remain after completing the connection");
            yield return Screenshot("tools-connected.png");
            CheckSliderSnap();
            yield return null; yield return null;

            SetMouse(new MouseState { position = new Vector2(Screen.width - 80, 160), buttons = 1 });
            yield return null; yield return null;
            SetMouse(new MouseState { position = new Vector2(Screen.width - 80, 160) });
            yield return null;
            Check(!root.gameObject.activeSelf, "Clicking away ends editing and hides tools");
            source.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            yield return null;
            Check(root.gameObject.activeInHierarchy, "Starting another connection shows tools again");
            DragDropManager.Instance.CancelDragDrop(); yield return null;
            Check(!root.gameObject.activeSelf, "Cancelling placement hides tools");
            Check(SaveGame.SavesPath == Path.Combine(output, "TestSaves"), "Verification never changes the user's save destination");
        }

        private void CheckPreviewSnap(string name)
        {
            var drag = FoundationApi.PipeDrag.Active;
            var start = FoundationApi.PipeDrag.ConnectionPoint(drag.Start);
            var end = FoundationApi.PipeDrag.ConnectionPoint(drag.HoveredPort);
            report.AppendLine(name + " terminals: " + start + " to " + end + "; preview: " + string.Join("; ", drag.Pipe.Nodes));
            Check(drag.Pipe.Nodes.Count > 0 && drag.Pipe.Nodes.All(node => {
                var snapped = FoundationApi.NativeIO.SnapRouteNode(node);
                return node.x == snapped.x && (node.y == snapped.y || node.y == start.y || node.y == end.y);
            }), "Production preview corners follow the live " + name + " grid and exact port rows");
            report.AppendLine(name + " preview: " + string.Join("; ", drag.Pipe.Nodes));
        }

        private void CheckBuildingLabelAvoidance()
        {
            var building = new Forge(); building.SetupRecipe("iron_ingot"); building.Position = new Vector2Int(256, -384);
            WorldFlowManager.Instance.CreateNewElement(building);
            try
            {
                building.World.UpdateLabel("Wide building label regression: keep pipes clear of this entire caption");
                Canvas.ForceUpdateCanvases();
                var label = FoundationApi.WorkspaceVisuals.BuildingLabel(building.World);
                var corners = new Vector3[4]; label.GetWorldCorners(corners);
                float left = corners.Min(p => p.x), right = corners.Max(p => p.x);
                float bodyLeft = building.Position.x - 8, bodyRight = building.Position.x + building.World.Size.x + 8;
                int x = right > bodyRight + 16 ? Mathf.RoundToInt((right + bodyRight + 16) / 2) : Mathf.RoundToInt((left + bodyLeft - 16) / 2);
                Check(label.gameObject.activeInHierarchy && right - left > building.World.Size.x, "Native label fixture extends beyond the building body");
                Check((x > bodyRight || x < bodyLeft) && x > left && x < right, "Test pipe crosses the label overhang separately from the building body");
                var assembly = typeof(PipeWrenchPlugin).Assembly;
                var pointType = assembly.GetType("KingdomPipeWrench.Routing.RoutePoint");
                var modeType = assembly.GetType("KingdomPipeWrench.Routing.AvoidanceMode");
                var shapeType = assembly.GetType("KingdomPipeWrench.Routing.RouteShape");
                var geometry = assembly.GetType("KingdomPipeWrench.Interaction.WorkspaceGeometry");
                var start = Activator.CreateInstance(pointType, x, Mathf.CeilToInt(corners.Max(p => p.y)) + 64);
                var end = Activator.CreateInstance(pointType, x, Mathf.FloorToInt(corners.Min(p => p.y)) - 64);
                var direct = Array.CreateInstance(pointType, 2); direct.SetValue(start, 0); direct.SetValue(end, 1);
                foreach (string mode in new[] { "Buildings", "Overlap", "BuildingsAndOverlap", "Strict" })
                {
                    var constraints = AccessTools.Method(geometry, "Constraints").Invoke(null, new object[] { null, Enum.Parse(modeType, mode), 8, null, null });
                    var clearMethod = AccessTools.Method(constraints.GetType(), "IsClear");
                    bool directClear = (bool)clearMethod.Invoke(constraints, new object[] { direct });
                    Check(directClear == (mode == "Overlap"), "Visible building label follows the " + mode + " avoidance policy");
                    var router = assembly.GetType("KingdomPipeWrench.Routing.ObstacleRouter");
                    var find = router.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "Find" && m.GetParameters().Last().ParameterType == constraints.GetType());
                    var route = find.Invoke(null, new[] { start, end, 0, 0, Enum.Parse(shapeType, "Center"), constraints });
                    Check(route != null && (bool)clearMethod.Invoke(constraints, new[] { route }), "Actual router finds a clear path for native label policy " + mode);
                }
                label.gameObject.SetActive(false);
                var hidden = AccessTools.Method(geometry, "Constraints").Invoke(null, new object[] { null, Enum.Parse(modeType, "Buildings"), 8, null, null });
                Check((bool)AccessTools.Method(hidden.GetType(), "IsClear").Invoke(hidden, new object[] { direct }), "Hidden building labels do not block routing");
            }
            finally
            {
                WorldFlowManager.Instance.RemoveElement(building);
            }
        }

        private void CheckDestroyedHover()
        {
            var drag = FoundationApi.PipeDrag.Active;
            staleHover = new GameObject("DestroyedPipeHoverRegression");
            DestroyImmediate(staleHover);
            Check(!ReferenceEquals(staleHover, null) && !staleHover, "Regression fixture retains Unity's destroyed-object managed reference");
            var method = typeof(UIHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(UIHelper.GetMouseOverUI) && !m.IsGenericMethod && m.GetParameters().Length == 0);
            var harmony = new Harmony("urakka.kingdominc.pipewrench.hoververification");
            try
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(PipeToolbarRuntimePlugin), nameof(ReturnDestroyedHover)));
                Check(ReferenceEquals(UIHelper.GetMouseOverUI(), staleHover), "Native raycast helper returns the destroyed hover regression fixture");
                Check(drag.HoveredPort == null, "Foundation ignores destroyed hover objects without calling GetComponentInParent");
                var routing = Field(plugin, "routing");
                AccessTools.Method(routing.GetType(), "Tick").Invoke(routing, null);
                Check(drag.IsActive && Root().gameObject.activeInHierarchy, "Production routing tick survives a destroyed hover and keeps the connection active");
            }
            finally
            {
                harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id);
                staleHover = null;
            }
        }
        private static bool ReturnDestroyedHover(ref GameObject __result) { __result = staleHover; return false; }

        private void CheckSliderSnap()
        {
            var marker = Resources.FindObjectsOfTypeAll<KingdomPipeWrench.Handles.PipeGrabMarker>().FirstOrDefault(m => m.gameObject.activeInHierarchy);
            Check(marker, "Completed routed pipe exposes a real session slider");
            var pipe = (WorkspacePipe)Field(Field(plugin, "editor"), "pipe");
            var original = pipe.Nodes.ToArray();
            marker.OnBeginDrag(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            var plan = Field(marker, "plan");
            bool vertical = (bool)AccessTools.Property(plan.GetType(), "Vertical").GetValue(plan);
            int coordinate = (int)AccessTools.Property(plan.GetType(), "Coordinate").GetValue(plan);
            var raw = vertical ? new Vector2Int(coordinate + 23, 0) : new Vector2Int(0, coordinate + 23);
            var snapped = FoundationApi.NativeIO.SnapRouteNode(raw);
            marker.OnDragDropMove(raw, Vector2Int.zero);
            Check(!pipe.Nodes.SequenceEqual(original) && pipe.Nodes.Any(n => vertical ? n.x == snapped.x : n.y == snapped.y), "Production slider movement follows installed Snappy snapping");
            marker.OnDragDropCancel();
            Check(pipe.Nodes.SequenceEqual(original), "Cancelling a snapped slider move restores the native corners");
            marker.OnBeginDrag(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            Press(Key.LeftCtrl);
            marker.OnDragDropMove(raw, Vector2Int.zero);
            Check(!pipe.Nodes.SequenceEqual(original) && pipe.Nodes.Any(n => vertical ? n.x == raw.x : n.y == raw.y), "Ctrl bypasses snapping during a production slider move");
            marker.OnDragDropCancel(); Press();
            CheckUnrestrictedSlide(marker, pipe);
        }

        private void CheckUnrestrictedSlide(KingdomPipeWrench.Handles.PipeGrabMarker marker, WorkspacePipe pipe)
        {
            var setting = plugin.Config[new BepInEx.Configuration.ConfigDefinition("Routing", "Avoidance")];
            var previous = setting.BoxedValue;
            setting.BoxedValue = Enum.ToObject(previous.GetType(), 3);
            try
            {
                foreach (string kind in new[] { "building", "overlap", "crossing" })
                {
                    var original = pipe.Nodes.ToArray();
                    marker.OnBeginDrag(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
                    var plan = Field(marker, "plan");
                    bool vertical = (bool)AccessTools.Property(plan.GetType(), "Vertical").GetValue(plan);
                    int coordinate = (int)AccessTools.Property(plan.GetType(), "Coordinate").GetValue(plan) + 64;
                    var raw = vertical ? new Vector2Int(coordinate, 0) : new Vector2Int(0, coordinate);
                    var full = AccessTools.Method(plan.GetType(), "Move").Invoke(plan, new object[] { coordinate });
                    var points = ((IEnumerable)full).Cast<object>().Select(p => new Vector2Int((int)Field(p, "X"), (int)Field(p, "Y"))).ToArray();
                    var expected = points.Skip(1).Take(points.Length - 2).ToArray();
                    Forge building = null; Assets.Source.Flow.FlowPipe other = null;
                    try
                    {
                        if (kind == "building")
                        {
                            building = new Forge(); building.SetupRecipe("iron_ingot");
                            building.Position = new Vector2Int(points[1].x - 16, points[1].y + 64);
                            WorldFlowManager.Instance.CreateNewElement(building);
                        }
                        else
                        {
                            other = new Assets.Source.Flow.FlowPipe();
                            other.Connect(GamePlayer.Current.ActiveFlow.Elements[0].Outputs[0], GamePlayer.Current.ActiveFlow.Elements[1].Inputs[0], true);
                            if (kind == "overlap") other.Nodes.AddRange(expected);
                            else
                            {
                                var middle = (points[1] + points[2]) / 2;
                                var offset = vertical ? new Vector2Int(64, 0) : new Vector2Int(0, 64);
                                other.Nodes.Add(middle - offset); other.Nodes.Add(middle + offset);
                            }
                            GamePlayer.Current.ActiveFlow.Pipes.Add(other);
                        }
                        var geometry = typeof(PipeWrenchPlugin).Assembly.GetType("KingdomPipeWrench.Interaction.WorkspaceGeometry");
                        var constraints = AccessTools.Method(geometry, "Constraints").Invoke(null, new object[] { pipe, setting.BoxedValue, 8, null, null });
                        var affected = AccessTools.Method(plan.GetType(), "Affected").Invoke(plan, new[] { full });
                        bool clear = (bool)AccessTools.Method(constraints.GetType(), "IsClear").Invoke(constraints, new[] { affected });
                        Check(!clear, "Regression fixture would be blocked by Strict " + kind + " avoidance");
                        marker.OnDragDropMove(raw, Vector2Int.zero);
                        Check(pipe.Nodes.SequenceEqual(expected), "Manual slider ignores " + kind + " restrictions while Strict is selected");
                    }
                    finally
                    {
                        marker.OnDragDropCancel();
                        if (other != null) GamePlayer.Current.ActiveFlow.Pipes.Remove(other);
                        if (building != null)
                        {
                            WorldFlowManager.Instance.RemoveElement(building);
                        }
                    }
                    Check(pipe.Nodes.SequenceEqual(original), "Cancellation restores the pipe after unrestricted " + kind + " sliding");
                }
            }
            finally
            {
                setting.BoxedValue = previous;
                var editor = Field(plugin, "editor"); AccessTools.Method(editor.GetType(), "Touch").Invoke(editor, null);
            }
        }

        private IEnumerator Screenshot(string name)
        {
            for (int i = 0; i < 4; i++) yield return null;
            var path = Path.Combine(output, name);
            RenderToolbar(path);
            Check(File.Exists(path), "Captured the live toolbar with Unity's UI renderer: " + name);
        }
        private void RenderToolbar(string path)
        {
            var cameraObject = new GameObject("VerificationCaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .09f, .08f);
            camera.cullingMask = 1 << 31;
            var target = new RenderTexture(620, 100, 24); camera.targetTexture = target;
            var canvasObject = new GameObject("VerificationCaptureCanvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1; canvasObject.layer = 31;
            var clone = Instantiate(Root().gameObject, canvasObject.transform, false);
            foreach (var item in clone.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 31;
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(24, -24); rect.localScale = Vector3.one;
            foreach (var label in clone.GetComponentsInChildren<TMP_Text>(true)) label.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(620, 100, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, 620, 100), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG()); RenderTexture.active = previous;
            camera.targetTexture = null; target.Release();
            Destroy(canvasObject); Destroy(cameraObject); Destroy(target); Destroy(texture);
        }
        private void Position(Vector2 point) => SetMouse(new MouseState { position = point });
        private void SetMouse(MouseState state) { mouseState = state; InputState.Change(mouse, state); }
        private void Press(params Key[] down) { keyboardState = new KeyboardState(down); InputState.Change(keys, keyboardState); }
        private void EnsureInput()
        {
            if (keys == null || !keys.added) keys = InputSystem.AddDevice<Keyboard>("PipeWrenchVerificationKeyboard");
            if (mouse == null || !mouse.added) mouse = InputSystem.AddDevice<Mouse>("PipeWrenchVerificationMouse");
            foreach (var device in InputSystem.devices.ToArray())
                if (device.enabled && (device is Keyboard || device is Mouse) && device != keys && device != mouse) InputSystem.DisableDevice(device);
            keys.MakeCurrent(); mouse.MakeCurrent();
        }
        private RectTransform Root() => (RectTransform)Field(toolbar, "root");
        private static object Field(object instance, string name) => AccessTools.Field(instance.GetType(), name).GetValue(instance);
        private void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++; report.AppendLine("PASS: " + message);
        }
        private void Update()
        {
            if (output != null) { EnsureInput(); InputState.Change(mouse, mouseState); InputState.Change(keys, keyboardState); }
            if (output != null && Time.realtimeSinceStartup - started > 90)
            { File.WriteAllText(Path.Combine(output, "result.txt"), report + "\nFAIL: Runtime verification timed out."); Application.Quit(2); }
        }
    }
}
