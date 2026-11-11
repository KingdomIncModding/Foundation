using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine.Networking;

namespace KingdomFoundation
{
    public static class NexusUpdateChecker
    {
        private static readonly Dictionary<string, int> customMappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ModItem> cachedMods = new List<ModItem>();

        public static IReadOnlyList<ModItem> CachedMods => cachedMods;
        public static bool IsCheckingAll { get; private set; }
        public static string LastCheckSummary { get; private set; } = "Not checked";
        public static event Action Changed;

        public static void RegisterModMapping(string guid, int nexusId)
        {
            if (string.IsNullOrEmpty(guid) || nexusId <= 0) return;
            customMappings[guid] = nexusId;
            var existing = cachedMods.FirstOrDefault(m => string.Equals(m.Guid, guid, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.NexusId = nexusId;
                Changed?.Invoke();
            }
        }

        public static bool TryLoadModJson(PluginInfo plugin, out string gameDomain, out int? nexusId)
        {
            gameDomain = null;
            nexusId = null;

            if (plugin == null) return false;

            string candidate = null;

            // 1. Next to the plugin DLL
            if (!string.IsNullOrEmpty(plugin.Location))
            {
                string dir = Path.GetDirectoryName(plugin.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    string localJson = Path.Combine(dir, "mod.json");
                    if (File.Exists(localJson)) candidate = localJson;
                    else
                    {
                        var parent = Directory.GetParent(dir);
                        if (parent != null)
                        {
                            string parentJson = Path.Combine(parent.FullName, "mod.json");
                            if (File.Exists(parentJson)) candidate = parentJson;
                        }
                    }
                }
            }

            // 2. Check Paths.GameRootPath / _Mods / {PluginName} / mod.json (development workspace)
            if (candidate == null && !string.IsNullOrEmpty(Paths.GameRootPath))
            {
                string modDirName = plugin.Metadata?.Name ?? Path.GetFileNameWithoutExtension(plugin.Location);
                if (!string.IsNullOrEmpty(modDirName))
                {
                    string devJson = Path.Combine(Paths.GameRootPath, "_Mods", modDirName, "mod.json");
                    if (File.Exists(devJson)) candidate = devJson;
                }
            }

            // 3. Fallback: Search _Mods subdirectories for a matching mod.json
            if (candidate == null && !string.IsNullOrEmpty(Paths.GameRootPath))
            {
                string modsRoot = Path.Combine(Paths.GameRootPath, "_Mods");
                if (Directory.Exists(modsRoot))
                {
                    string targetName = plugin.Metadata?.Name;
                    string targetGuid = plugin.Metadata?.GUID;
                    try
                    {
                        foreach (string subDir in Directory.GetDirectories(modsRoot))
                        {
                            string subJson = Path.Combine(subDir, "mod.json");
                            if (File.Exists(subJson))
                            {
                                try
                                {
                                    var testJson = JsonValue.Parse(File.ReadAllText(subJson));
                                    if (testJson.IsJsonObject)
                                    {
                                        var obj = testJson.AsJsonObject;
                                        string id = obj.ContainsKey("id") ? obj["id"].AsString : null;
                                        string name = obj.ContainsKey("name") ? obj["name"].AsString : null;
                                        if (string.Equals(id, targetName, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase) ||
                                            (targetGuid != null && targetGuid.IndexOf(id ?? "---", StringComparison.OrdinalIgnoreCase) >= 0))
                                        {
                                            candidate = subJson;
                                            break;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }

            if (candidate == null || !File.Exists(candidate)) return false;

            try
            {
                var json = JsonValue.Parse(File.ReadAllText(candidate));
                if (!json.IsJsonObject) return false;

                var root = json.AsJsonObject;
                if (root.ContainsKey("distribution") && root["distribution"].IsJsonObject)
                {
                    var dist = root["distribution"].AsJsonObject;
                    if (dist.ContainsKey("nexus") && dist["nexus"].IsJsonObject)
                    {
                        var nexus = dist["nexus"].AsJsonObject;

                        if (nexus.ContainsKey("game") && nexus["game"].IsString)
                            gameDomain = nexus["game"].AsString;

                        if (nexus.ContainsKey("modId"))
                        {
                            if (nexus["modId"].IsInteger) nexusId = nexus["modId"].AsInteger;
                            else if (nexus["modId"].IsString && int.TryParse(nexus["modId"].AsString, out int parsedModId))
                                nexusId = parsedModId;
                        }

                        return nexusId.HasValue;
                    }
                }
            }
            catch { }

            return false;
        }

        public static void ResolveNexusInfo(string guid, PluginInfo plugin, out string gameDomain, out int? nexusId)
        {
            gameDomain = null;
            nexusId = null;

            // Priority 1: Read from mod.json of the loaded mod
            if (TryLoadModJson(plugin, out gameDomain, out nexusId))
            {
                if (string.IsNullOrEmpty(gameDomain)) gameDomain = "kingdominc";
                return;
            }

            // Priority 2: Custom registered mapping
            if (!string.IsNullOrEmpty(guid) && customMappings.TryGetValue(guid, out int customId))
            {
                nexusId = customId;
                gameDomain = "kingdominc";
                return;
            }

            // Priority 3: Custom attribute on plugin instance / type
            if (plugin?.Instance != null)
            {
                try
                {
                    var attr = plugin.Instance.GetType().GetCustomAttributes(false)
                        .FirstOrDefault(a => a is NexusModAttribute || a.GetType().Name == "NexusModAttribute" || a.GetType().Name == "NexusModIdAttribute");
                    if (attr != null)
                    {
                        var prop = attr.GetType().GetProperty("ModId") ?? attr.GetType().GetProperty("Id");
                        if (prop != null)
                        {
                            object val = prop.GetValue(attr);
                            if (val is int intId && intId > 0) { nexusId = intId; gameDomain = "kingdominc"; return; }
                            if (val is string strId && int.TryParse(strId, out int parsed) && parsed > 0) { nexusId = parsed; gameDomain = "kingdominc"; return; }
                        }
                    }
                }
                catch { }
            }

            // Priority 4: Legacy nexus.json in DLL folder
            if (!string.IsNullOrEmpty(plugin?.Location))
            {
                try
                {
                    string dir = Path.GetDirectoryName(plugin.Location);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        string nexusJson = Path.Combine(dir, "nexus.json");
                        if (File.Exists(nexusJson))
                        {
                            var json = JsonValue.Parse(File.ReadAllText(nexusJson));
                            if (json.IsJsonObject)
                            {
                                var obj = json.AsJsonObject;
                                if (obj.ContainsKey("nexus_id")) { nexusId = obj["nexus_id"].AsInteger; gameDomain = "kingdominc"; return; }
                                if (obj.ContainsKey("id")) { nexusId = obj["id"].AsInteger; gameDomain = "kingdominc"; return; }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public static bool IsOutdated(string currentStr, string latestStr)
        {
            if (string.IsNullOrEmpty(currentStr) || string.IsNullOrEmpty(latestStr)) return false;

            currentStr = currentStr.Trim().TrimStart('v', 'V');
            latestStr = latestStr.Trim().TrimStart('v', 'V');

            int curDash = currentStr.IndexOf('-');
            if (curDash >= 0) currentStr = currentStr.Substring(0, curDash);
            int latDash = latestStr.IndexOf('-');
            if (latDash >= 0) latestStr = latestStr.Substring(0, latDash);

            if (Version.TryParse(currentStr, out var curVer) && Version.TryParse(latestStr, out var latVer))
                return latVer > curVer;

            var curParts = currentStr.Split('.');
            var latParts = latestStr.Split('.');
            int max = Math.Max(curParts.Length, latParts.Length);
            for (int i = 0; i < max; i++)
            {
                int c = i < curParts.Length && int.TryParse(curParts[i], out var cv) ? cv : 0;
                int l = i < latParts.Length && int.TryParse(latParts[i], out var lv) ? lv : 0;
                if (l > c) return true;
                if (l < c) return false;
            }
            return false;
        }

        public static void RefreshInstalledList()
        {
            var map = cachedMods.ToDictionary(m => m.Guid, StringComparer.OrdinalIgnoreCase);
            var list = new List<ModItem>();

            foreach (var pair in Chainloader.PluginInfos)
            {
                var info = pair.Value;
                if (info?.Metadata == null) continue;
                string guid = info.Metadata.GUID;

                ResolveNexusInfo(guid, info, out string domain, out int? nexusId);

                if (map.TryGetValue(guid, out var existing))
                {
                    existing.Name = info.Metadata.Name;
                    existing.InstalledVersion = info.Metadata.Version?.ToString() ?? "1.0.0";
                    existing.Location = info.Location;
                    if (!string.IsNullOrEmpty(domain)) existing.GameDomain = domain;
                    if (nexusId.HasValue) existing.NexusId = nexusId;
                    list.Add(existing);
                }
                else
                {
                    var item = new ModItem
                    {
                        Guid = guid,
                        Name = info.Metadata.Name,
                        InstalledVersion = info.Metadata.Version?.ToString() ?? "1.0.0",
                        Location = info.Location,
                        GameDomain = domain ?? "kingdominc",
                        NexusId = nexusId,
                        StatusText = "Not checked"
                    };
                    list.Add(item);
                }
            }

            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            cachedMods.Clear();
            cachedMods.AddRange(list);
            Changed?.Invoke();
        }

        public static void CheckAll(Action onDone = null)
        {
            RefreshInstalledList();

            var targets = cachedMods.Where(m => m.NexusId.HasValue).ToList();
            if (targets.Count == 0)
            {
                LastCheckSummary = "No Nexus mods detected";
                Changed?.Invoke();
                onDone?.Invoke();
                return;
            }

            IsCheckingAll = true;
            LastCheckSummary = "Checking Nexus for updates...";
            int pending = targets.Count;

            foreach (var mod in targets)
            {
                mod.IsChecking = true;
                mod.StatusText = "Checking...";
            }
            Changed?.Invoke();

            foreach (var mod in targets)
            {
                CheckSingleMod(mod, () =>
                {
                    pending--;
                    if (pending <= 0)
                    {
                        IsCheckingAll = false;
                        int outdatedCount = cachedMods.Count(m => m.IsOutdated);
                        LastCheckSummary = outdatedCount > 0
                            ? $"{outdatedCount} update{(outdatedCount == 1 ? "" : "s")} available"
                            : "All mods up to date";
                        Changed?.Invoke();
                        onDone?.Invoke();
                    }
                    else
                    {
                        Changed?.Invoke();
                    }
                });
            }
        }

        private static void CheckSingleMod(ModItem mod, Action callback)
        {
            if (!mod.NexusId.HasValue)
            {
                mod.IsChecking = false;
                mod.StatusText = "Local mod";
                callback?.Invoke();
                return;
            }

            string domain = !string.IsNullOrEmpty(mod.GameDomain) ? mod.GameDomain : "kingdominc";
            string url = $"https://api.nexusmods.com/v3/games/{domain}/mods/{mod.NexusId.Value}";

            var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("User-Agent", "KingdomInc-Foundation/0.11.0");
            req.timeout = 10;

            var op = req.SendWebRequest();
            op.completed += _ =>
            {
                try
                {
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        string text = req.downloadHandler.text;
                        var json = JsonValue.Parse(text);
                        string latest = null;
                        if (json.IsJsonObject && json["data"].IsJsonObject)
                        {
                            var data = json["data"].AsJsonObject;
                            if (data.ContainsKey("version") && data["version"].IsString)
                            {
                                latest = data["version"].AsString;
                            }
                        }

                        if (!string.IsNullOrEmpty(latest))
                        {
                            mod.LatestVersion = latest;
                            mod.IsOutdated = IsOutdated(mod.InstalledVersion, latest);
                            mod.Checked = true;
                            mod.StatusText = mod.IsOutdated ? "Update Available" : "Up to date";
                        }
                        else
                        {
                            mod.Checked = false;
                            mod.StatusText = "Check failed";
                        }
                    }
                    else
                    {
                        mod.Checked = false;
                        mod.StatusText = "Check failed (" + req.responseCode + ")";
                    }
                }
                catch
                {
                    mod.Checked = false;
                    mod.StatusText = "Error";
                }
                finally
                {
                    mod.IsChecking = false;
                    req.Dispose();
                    callback?.Invoke();
                }
            };
        }
    }
}
