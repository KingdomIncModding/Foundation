using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.UI.MainMenu;
using Assets.Behaviour.UI;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace KingdomFoundation
{
    internal sealed class OptionsMenuChecks
    {
        readonly List<string> checks = new List<string>();
        readonly List<string> exceptions = new List<string>();
        string error;
        string directory;
        OptionsUI options;
        FoundationConfigMenu menu;
        IOptionsTabRegistration registration, customRegistration;
        IDisposable observer;
        IModConfigToggle toggle;
        IModConfigSlider slider;
        IModConfigDropdown dropdown;
        IVanillaSectionView audioSection;
        bool savedToggle;
        int commits;
        void Assert(bool condition, string description)
        {
            if (!condition)
                throw new InvalidOperationException(description);
            checks.Add(description);
        }

        void ObserveErrors(string message, string stack, LogType type)
        {
            if (type == LogType.Exception)
                exceptions.Add(message + "\n" + stack);
        }

        void Step(Action action)
        {
            if (error != null)
                return;
            try
            {
                action();
            }
            catch (Exception failure)
            {
                error = failure.ToString();
            }
        }

        internal IEnumerator Run()
        {
            Application.logMessageReceived += ObserveErrors;
            directory = Path.Combine(Paths.GameRootPath, "options-results");
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(5);
            Step(() =>
            {
                Assert(GamePlayer.Current == null && !SaveGame.LoadLatestSave(), "No player save loaded; native save loading blocked");
                MainMenuUI.Instance.ShowOptions();
                options = Resources.FindObjectsOfTypeAll<OptionsUI>().First(o => o.gameObject.activeInHierarchy);
                menu = options.GetComponent<FoundationConfigMenu>();
            });
            yield return null;
            Step(() =>
            {
                Assert(menu && menu.VanillaSettings.Count == 8, "Options exposes all eight native settings rows");
                Assert(menu.VanillaSettings.All(s => s.IsAlive && s.IsVisible), "Vanilla settings start visible without mod policy");
                IOptionsView initialView = menu;
                Assert(initialView.GetVanillaSections().Count == 2 && initialView.GetVanillaSections().All(s => s.IsAlive && s.IsVisible), "Both vanilla headings are available and visible by default");
                var strip = menu.GetComponent<FoundationConfigMenu>().transform.Find("OptionsTabs");
                Assert(strip.GetComponentsInChildren<Button>().Length == 3, "Default tab strip has Game, Mods and Installed only");
                foreach (var button in strip.GetComponentsInChildren<Button>())
                {
                    var label = button.GetComponentInChildren<TMP_Text>();
                    label.ForceMeshUpdate();
                    var rect = (RectTransform)button.transform;
                    var actual = rect.InverseTransformPoint(label.transform.TransformPoint(label.textBounds.center));
                    Assert(Vector2.Distance(actual, rect.rect.center + new Vector2(0, -4)) < 1, "Rendered tab glyphs are centered horizontally and lowered four pixels: " + label.text);
                }

                Capture("options-default.png");
                var tab = new OptionsTab("foundation.options.check", "tools", "Workshop");
                tab.Settings.Description = "An example of a mod-owned Options tab.";
                tab.Settings.AddText("Workshop settings", true);
                toggle = tab.Settings.AddToggle("Enabled", false, value =>
                {
                    savedToggle = value;
                    commits++;
                }, () => savedToggle);
                slider = tab.Settings.AddSlider("Intensity", 0, 100, 50, _ =>
                {
                }, formatValue: value => value.ToString("0") + "%");
                dropdown = tab.Settings.AddDropdown("Mode", new[] { "Normal", "Detailed", "Compact" }, 0, _ =>
                {
                });
                tab.Settings.AddInput("Label", "My workshop", _ =>
                {
                });
                tab.Settings.AddNumber("Limit", 0, 100, 10, 1, _ =>
                {
                });
                tab.Settings.AddButton("Action", () =>
                {
                }, "Run action");
                registration = FoundationApi.Options.Register(tab);
                var before = FoundationApi.Options.Tabs;
                try
                {
                    FoundationApi.Options.Register(tab);
                    throw new InvalidOperationException("Duplicate was accepted");
                }
                catch (ArgumentException)
                {
                    checks.Add("Duplicate tab identity is rejected");
                }

                customRegistration = FoundationApi.Options.Register(new OptionsProbeTab(tab.Settings));
                Assert(before.Count == 1 && FoundationApi.Options.Tabs.Count == 2, "Tab enumeration is a stable snapshot and accepts interface implementations");
                Assert(FoundationApi.Options.TryGet(tab.ModId, tab.Id, out var found) && found == registration, "Registry lookup returns the live tab handle");
            });
            yield return null;
            Step(() =>
            {
                Capture("options-registered.png");
                Click(TabButton("Workshop"));
            });
            yield return null;
            Step(() =>
            {
                Capture("options-custom.png");
                var page = menu.transform.Find("CustomOptions:foundation.options.check:tools");
                Assert(page.gameObject.activeSelf && page.GetComponentsInChildren<Slider>().Length == 1 && page.GetComponentsInChildren<TMP_InputField>().Length == 2, "Custom page renders native slider, text and numeric controls");
                var nativeToggle = page.GetComponentInChildren<Toggle>();
                Click(nativeToggle);
                Assert(toggle.Value && !savedToggle, "Pointer toggle edits remain pending before Apply");
                slider.Value = 75;
                dropdown.SelectedIndex = 2;
                Click(TabButton("Game"));
                Click(TabButton("Custom Tools"));
                Click(TabButton("Workshop"));
                Assert(toggle.Value && slider.Value == 75 && dropdown.SelectedIndex == 2, "Switching built-in and custom tabs preserves all pending edits");
                menu.CommitModConfigs();
                menu.CommitModConfigs();
                Assert(savedToggle && commits == 1, "Shared section across two custom tabs commits exactly once");
                options.CancelOptions();
            });
            yield return null;
            Step(() =>
            {
                MainMenuUI.Instance.ShowOptions();
                toggle.Value = false;
                slider.Value = 5;
                options.CancelOptions();
                Assert(savedToggle && commits == 1 && toggle.Value && slider.Value == 75, "Cancel discards pending values without callbacks");
                MainMenuUI.Instance.ShowOptions();
                Assert(menu.TryGetVanillaSetting("MusicVolume", out var music), "Stable native row ID resolves MusicVolume");
                float original = music.IsVisible ? options.GetComponentsInChildren<Slider>(true).First(s => s.transform.parent.name == "MusicVolume").value : 0;
                observer = FoundationApi.Options.ObserveViews(view =>
                {
                    if (view.TryGetVanillaSetting("MusicVolume", out var row))
                        row.IsVisible = false;
                });
                Assert(!music.IsVisible && menu.VanillaSettings.Count(s => s.IsVisible) == 7, "A mod can hide one vanilla row; unrelated rows stay visible");
                Assert(options.GetComponentsInChildren<Slider>(true).First(s => s.transform.parent.name == "MusicVolume").value == original, "Hiding a native row does not alter its native value");
                observer.Dispose();
                observer = null;
                options.CancelOptions();
                MainMenuUI.Instance.ShowOptions();
                Assert(menu.VanillaSettings.All(s => s.IsVisible), "Removing the observer restores default visible settings next opening");
                var registeredWithoutHide = menu.VanillaSettings.All(s => s.IsVisible);
                Assert(registeredWithoutHide, "A registered tab alone never hides vanilla settings");
            });
            yield return null;
            Step(() =>
            {
                Click(TabButton("Game"));
                IOptionsView view = menu;
                Assert(view.TryGetVanillaSection(VanillaSectionIds.AudioAndSystem, out audioSection) && audioSection.Title == "Audio & System", "Options observer interface resolves the Audio & System heading");
                Assert(!view.TryGetVanillaSection(null, out _) && !view.TryGetVanillaSection("missing", out _), "Unknown section IDs safely return false");
                audioSection.IsVisible = false;
                Assert(!audioSection.IsVisible && menu.VanillaSettings.All(s => s.IsVisible), "Hiding only a heading keeps every vanilla setting available");
                audioSection.IsVisible = true;
                observer = FoundationApi.Options.ObserveViews(opened =>
                {
                    if (opened.TryGetVanillaSection(VanillaSectionIds.AudioAndSystem, out var section))
                        section.IsVisible = false;
                    if (opened.TryGetVanillaSetting(VanillaSettingIds.MusicVolume, out var music))
                        music.IsVisible = false;
                    if (opened.TryGetVanillaSetting(VanillaSettingIds.SoundVolume, out var effects))
                        effects.IsVisible = false;
                });
                Assert(!audioSection.IsVisible && menu.VanillaSettings.Count(s => s.IsVisible) == 6 && menu.TryGetVanillaSetting(VanillaSettingIds.AutosaveFreq, out var autosave) && autosave.IsVisible && menu.TryGetVanillaSetting(VanillaSettingIds.KeyBindings, out var bindings) && bindings.IsVisible, "A mod can hide the audio heading and volume rows while keeping Autosave and Key Bindings");
                menu.TryGetVanillaSetting(VanillaSettingIds.MusicVolume, out var row);
                row.IsVisible = true;
                row.IsVisible = false;
                Assert(!audioSection.IsVisible, "Row layout changes never re-enable an explicitly hidden heading");
                Assert(view.TryGetVanillaSection(VanillaSectionIds.DisplayAndInterface, out var display) && display.IsVisible, "Hiding Audio & System leaves the Display & Interface heading visible");
            });
            yield return null;
            Step(() =>
            {
                Capture("options-hidden-heading.png");
                observer.Dispose();
                observer = null;
                audioSection.IsVisible = true;
                foreach (var row in menu.VanillaSettings)
                    row.IsVisible = true;
                Assert(audioSection.IsVisible && menu.VanillaSettings.All(s => s.IsVisible), "A mod can restore its heading and rows immediately on unload");
                foreach (var id in new[]
                {
                    VanillaSettingIds.MusicVolume,
                    VanillaSettingIds.SoundVolume,
                    VanillaSettingIds.AutosaveFreq,
                    VanillaSettingIds.KeyBindings
                }

                )
                    if (menu.TryGetVanillaSetting(id, out var row))
                        row.IsVisible = false;
                Assert(!audioSection.IsVisible, "An empty section still removes its unused heading automatically");
                if (menu.TryGetVanillaSetting(VanillaSettingIds.AutosaveFreq, out var autosave))
                    autosave.IsVisible = true;
                Assert(audioSection.IsVisible, "A default-visible heading returns when its section has a visible row");
                audioSection.IsVisible = false;
                options.CancelOptions();
                MainMenuUI.Instance.ShowOptions();
                Assert(menu.GetVanillaSections().All(s => s.IsVisible) && menu.VanillaSettings.All(s => s.IsVisible), "Reopening without the mod observer resets heading and row visibility");
            });
            yield return null;
            Step(() =>
            {
                Click(TabButton("Workshop"));
                registration.Dispose();
                registration.Dispose();
                Assert(menu.transform.Find("GameSettingsPage").gameObject.activeSelf, "Removing the active custom tab safely selects Game");
                customRegistration.Dispose();
            });
            yield return null;
            Step(() =>
            {
                var strip = menu.transform.Find("OptionsTabs");
                Assert(strip.GetComponentsInChildren<Button>().Length == 3 && menu.VanillaSettings.All(s => s.IsVisible), "Unregistered mods leave the three default tabs and visible settings");
                var shortTab = new OptionsTab("foundation.options.check", "short", "One");
                var longTab = new OptionsTab("foundation.options.check", "long", "Much longer tab title");
                using (var shortHandle = FoundationApi.Options.Register(shortTab))
                using (var longHandle = FoundationApi.Options.Register(longTab))
                {
                    var shortButton = (RectTransform)TabButton("One").transform;
                    var longButton = (RectTransform)TabButton("Much longer tab title").transform;
                    Assert(longButton.rect.width > shortButton.rect.width + 100, "Tab widths follow rendered title length instead of fixed widths");
                    var label = longButton.GetComponentInChildren<TMP_Text>();
                    Assert(Mathf.Abs(longButton.rect.width - label.GetPreferredValues(label.text).x - 56) < 1.1f, "Every tab includes consistent horizontal label padding");
                    Capture("options-widths.png");
                }

                options.CancelOptions();
                Assert(!FoundationApi.Options.OpenViews.Any(), "Closed Options view is removed from registry");
                Assert(exceptions.Count == 0, "No Unity exceptions during Options registration, navigation or disposal");
                GamePlayer.CreateNewPlayer();
                SceneManager.LoadScene("Game");
            });
            yield return new WaitForSecondsRealtime(4);
            Step(() =>
            {
                Assert(!audioSection.IsAlive, "Heading view lifetime ends when its menu scene is destroyed");
                GameUI.Instance.ProcessEscape();
                var inGame = Resources.FindObjectsOfTypeAll<InGameMenuUI>().First(m => m.gameObject.activeInHierarchy);
                inGame.ShowOptions();
                options = inGame.GetComponentInChildren<OptionsUI>(true);
                menu = options.GetComponent<FoundationConfigMenu>();
                Assert(menu && menu.IsOpen && menu.VanillaSettings.All(s => s.IsVisible), "In-game Options builds the same tabs with all vanilla settings visible");
                registration = FoundationApi.Options.Register(new OptionsTab("foundation.options.check", "ingame", "In Game Tools"));
            });
            yield return null;
            Step(() =>
            {
                Click(TabButton("In Game Tools"));
                Capture("options-ingame.png");
                options.CancelOptions();
                Assert(!menu.IsOpen, "Native in-game Cancel closes the custom-tab Options session");
                registration.Dispose();
                SceneManager.LoadScene("MainMenu");
            });
            yield return new WaitForSecondsRealtime(2);
            Step(() => Assert(exceptions.Count == 0 && !FoundationApi.Options.OpenViews.Any(), "Scene teardown releases Options views without Unity exceptions"));
            var report = new JsonObject
            {
                {
                    "passed",
                    error == null && exceptions.Count == 0
                },
                {
                    "version",
                    FoundationPlugin.Version
                },
                {
                    "count",
                    checks.Count
                },
                {
                    "checks",
                    new JsonArray(checks.Select(c => (JsonValue)c).ToArray())
                },
                {
                    "error",
                    error ?? ""
                },
                {
                    "exceptions",
                    new JsonArray(exceptions.Select(c => (JsonValue)c).ToArray())
                }
            };
            File.WriteAllText(Path.Combine(directory, "options-tests.json"), report.ToString());
            observer?.Dispose();
            registration?.Dispose();
            customRegistration?.Dispose();
            Application.logMessageReceived -= ObserveErrors;
            Application.Quit();
        }

        Button TabButton(string title) => menu.transform.Find("OptionsTabs").GetComponentsInChildren<Button>(true).First(b => b.gameObject.activeSelf && b.GetComponentInChildren<TMP_Text>().text == title);
        void Click(Selectable control)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)control.transform;
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var data = new PointerEventData(EventSystem.current)
            {
                position = point,
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Selectable>() == control, "Real UI pointer reaches " + control.name + "; hits=" + string.Join(",", hits.Select(h => h.gameObject.name)));
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            var camera = Resources.FindObjectsOfTypeAll<Camera>().First(c => c.enabled && c.gameObject.activeInHierarchy);
            var canvases = Resources.FindObjectsOfTypeAll<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var old = RenderTexture.active;
            var oldTarget = camera.targetTexture;
            var oldMask = camera.cullingMask;
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                foreach (var canvas in canvases)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 10;
                }

                camera.cullingMask = -1;
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(directory, name), ImageConversion.EncodeToPNG(texture));
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.cullingMask = oldMask;
                foreach (var canvas in canvases)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera = null;
                }

                RenderTexture.active = old;
                RenderTexture.ReleaseTemporary(target);
                global::UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
