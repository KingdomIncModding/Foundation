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
        public static readonly Dictionary<string, int> KnownNexusModIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "urakka.kingdominc.partynode", 5 },
            { "urakka.kingdominc.sharableblueprints", 6 },
            { "urakka.kingdominc.ledgermod", 7 },
            { "urakka.kingdominc.pathfinder", 8 },
            { "urakka.kingdominc.leylineconduits", 9 },
            { "urakka.kingdominc.nightmareprogressfix", 10 },
            { "urakka.kingdominc.containercolors", 11 },
            { "urakka.kingdominc.snappy", 12 },
            { "urakka.kingdominc.devspawner", 13 },
            { "urakka.kingdominc.betterzoom", 14 },
            { "urakka.kingdominc.portlayouts", 15 },
            { "urakka.kingdominc.hotkeycustomizer", 16 },
            { "urakka.kingdominc.guildhousecleanup", 17 },
            { "urakka.kingdominc.foundation", 18 },
            { "urakka.kingdominc.globalledger", 19 },
            { "urakka.kingdominc.hdsnapshot", 20 },
            { "urakka.kingdominc.bottleneckheatmap", 21 }
        };

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

        public static bool TryLoadModJson(PluginInfo plugin, out string gameDomain, out int? nexusId, out int? gameScopedId, out int? fileId)
        {
            gameDomain = null;
            nexusId = null;
            gameScopedId = null;
            fileId = null;

            if (plugin == null) return false;

            string candidate = null;

            // 1. Next to the plugin DLL
            if (!string.IsNullOrEmpty(plugin.Location))
            {
                string dir = Path.GetDirectoryName(plugin.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    string localJson = Path.Combine(dir, "mod.json");
                    if (File.Exists(localJson))
                    {
                        candidate = localJson;
                    }
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

                        if (nexus.ContainsKey("gameScopedId"))
                        {
                            if (nexus["gameScopedId"].IsInteger) gameScopedId = nexus["gameScopedId"].AsInteger;
                            else if (nexus["gameScopedId"].IsString && int.TryParse(nexus["gameScopedId"].AsString, out int parsedScoped))
                                gameScopedId = parsedScoped;
                        }

                        if (nexus.ContainsKey("fileId"))
                        {
                            if (nexus["fileId"].IsInteger) fileId = nexus["fileId"].AsInteger;
                            else if (nexus["fileId"].IsString && int.TryParse(nexus["fileId"].AsString, out int parsedFid))
                                fileId = parsedFid;
                        }

                        return nexusId.HasValue || gameScopedId.HasValue;
                    }
                }
            }
            catch { }

            return false;
        }

        public static void ResolveNexusInfo(string guid, PluginInfo plugin, out string gameDomain, out int? nexusId, out int? gameScopedId, out int? fileId)
        {
            gameDomain = null;
            nexusId = null;
            gameScopedId = null;
            fileId = null;

            // Priority 1: Read dynamic metadata directly from mod.json of loaded mod
            if (TryLoadModJson(plugin, out gameDomain, out nexusId, out gameScopedId, out fileId))
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

            // Priority 5: Fallback dictionary
            if (!string.IsNullOrEmpty(guid) && KnownNexusModIds.TryGetValue(guid, out int knownId))
            {
                nexusId = knownId;
                gameDomain = "kingdominc";
                return;
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

                ResolveNexusInfo(guid, info, out string domain, out int? nexusId, out int? gameScopedId, out int? fileId);

                if (map.TryGetValue(guid, out var existing))
                {
                    existing.Name = info.Metadata.Name;
                    existing.InstalledVersion = info.Metadata.Version?.ToString() ?? "1.0.0";
                    existing.Location = info.Location;
                    if (!string.IsNullOrEmpty(domain)) existing.GameDomain = domain;
                    if (nexusId.HasValue) existing.NexusId = nexusId;
                    if (gameScopedId.HasValue) existing.GameScopedId = gameScopedId;
                    if (fileId.HasValue) existing.FileId = fileId;
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
                        GameScopedId = gameScopedId,
                        FileId = fileId,
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

            // API key is strictly optional; Nexus v3 mod metadata does not require an API key
            string apiKey = FoundationPlugin.GetNexusApiKey();

            var targets = cachedMods.Where(m => m.NexusId.HasValue || m.GameScopedId.HasValue).ToList();
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
                CheckSingleMod(mod, apiKey, () =>
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

        private static void CheckSingleMod(ModItem mod, string apiKey, Action callback)
        {
            int targetId = mod.NexusId ?? mod.GameScopedId ?? 0;
            if (targetId <= 0)
            {
                mod.IsChecking = false;
                mod.StatusText = "Local mod";
                callback?.Invoke();
                return;
            }

            string domain = !string.IsNullOrEmpty(mod.GameDomain) ? mod.GameDomain : "kingdominc";

            // If a specific file-version gameScopedId was provided and differs from modId, check mod-file-versions first
            if (mod.GameScopedId.HasValue && mod.GameScopedId.Value > 0 && mod.GameScopedId.Value != mod.NexusId)
            {
                string fileVersionUrl = $"https://api.nexusmods.com/v3/games/{domain}/mod-file-versions/{mod.GameScopedId.Value}";
                TryRequest(fileVersionUrl, apiKey, (success, ver) =>
                {
                    if (success && !string.IsNullOrEmpty(ver))
                    {
                        ApplyVersionResult(mod, ver);
                        callback?.Invoke();
                    }
                    else if (mod.NexusId.HasValue)
                    {
                        // Fall back to unauthenticated mod endpoint
                        string modUrl = $"https://api.nexusmods.com/v3/games/{domain}/mods/{mod.NexusId.Value}";
                        TryRequest(modUrl, apiKey, (modSuccess, modVer) =>
                        {
                            if (modSuccess && !string.IsNullOrEmpty(modVer)) ApplyVersionResult(mod, modVer);
                            else ApplyErrorResult(mod, "Check failed");
                            callback?.Invoke();
                        });
                    }
                    else
                    {
                        ApplyErrorResult(mod, "Check failed");
                        callback?.Invoke();
                    }
                });
            }
            else
            {
                // Standard Nexus v3 endpoint: https://api.nexusmods.com/v3/games/{game_domain}/mods/{game_scoped_id}
                string modUrl = $"https://api.nexusmods.com/v3/games/{domain}/mods/{targetId}";
                TryRequest(modUrl, apiKey, (success, ver) =>
                {
                    if (success && !string.IsNullOrEmpty(ver))
                    {
                        ApplyVersionResult(mod, ver);
                    }
                    else
                    {
                        ApplyErrorResult(mod, "Check failed");
                    }
                    callback?.Invoke();
                });
            }
        }

        private static void TryRequest(string url, string apiKey, Action<bool, string> onComplete)
        {
            var req = UnityWebRequest.Get(url);
            if (!string.IsNullOrEmpty(apiKey))
            {
                req.SetRequestHeader("apikey", apiKey);
            }
            req.SetRequestHeader("User-Agent", "KingdomInc-Foundation/0.11.0");
            req.SetRequestHeader("Accept", "application/json");
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
                        onComplete?.Invoke(!string.IsNullOrEmpty(latest), latest);
                    }
                    else
                    {
                        onComplete?.Invoke(false, null);
                    }
                }
                catch
                {
                    onComplete?.Invoke(false, null);
                }
                finally
                {
                    req.Dispose();
                }
            };
        }

        private static void ApplyVersionResult(ModItem mod, string latest)
        {
            mod.LatestVersion = latest;
            mod.IsOutdated = IsOutdated(mod.InstalledVersion, latest);
            mod.Checked = true;
            mod.StatusText = mod.IsOutdated ? "Update Available" : "Up to date";
            mod.IsChecking = false;
        }

        private static void ApplyErrorResult(ModItem mod, string errorText)
        {
            mod.Checked = false;
            mod.StatusText = errorText;
            mod.IsChecking = false;
        }
    }
}
