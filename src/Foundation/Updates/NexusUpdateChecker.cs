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
    /// <summary>Provides the nexus update checker API.</summary>
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

        /// <summary>Gets or sets the cached mods value.</summary>
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

        /// <summary>Performs the resolve nexus id operation.</summary>
        public static int? ResolveNexusId(string guid, PluginInfo plugin)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            if (customMappings.TryGetValue(guid, out int customId))
                return customId;

            if (KnownNexusModIds.TryGetValue(guid, out int knownId))
                return knownId;

            // Check custom attribute on plugin instance / type
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
                            if (val is int intId && intId > 0) return intId;
                            if (val is string strId && int.TryParse(strId, out int parsed) && parsed > 0) return parsed;
                        }
                    }
                }
                catch { }
            }

            // Check nexus.json in DLL folder
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
                                if (obj.ContainsKey("nexus_id")) return obj["nexus_id"].AsInteger;
                                if (obj.ContainsKey("id")) return obj["id"].AsInteger;
                            }
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        /// <summary>Determines whether outdated.</summary>
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

        /// <summary>Performs the refresh installed list operation.</summary>
        public static void RefreshInstalledList()
        {
            var map = cachedMods.ToDictionary(m => m.Guid, StringComparer.OrdinalIgnoreCase);
            var list = new List<ModItem>();

            foreach (var pair in Chainloader.PluginInfos)
            {
                var info = pair.Value;
                if (info?.Metadata == null) continue;
                string guid = info.Metadata.GUID;
                if (map.TryGetValue(guid, out var existing))
                {
                    existing.Name = info.Metadata.Name;
                    existing.InstalledVersion = info.Metadata.Version?.ToString() ?? "1.0.0";
                    existing.Location = info.Location;
                    if (!existing.NexusId.HasValue) existing.NexusId = ResolveNexusId(guid, info);
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
                        NexusId = ResolveNexusId(guid, info),
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

        /// <summary>Performs the check all operation.</summary>
        public static void CheckAll(Action onDone = null)
        {
            RefreshInstalledList();

            string apiKey = FoundationPlugin.GetNexusApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                foreach (var mod in cachedMods)
                {
                    if (mod.NexusId.HasValue) mod.StatusText = "Not checked";
                    else mod.StatusText = "Local mod";
                }
                LastCheckSummary = "Nexus API key not configured";
                Changed?.Invoke();
                onDone?.Invoke();
                return;
            }

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
            if (!mod.NexusId.HasValue)
            {
                mod.IsChecking = false;
                mod.StatusText = "Local mod";
                callback?.Invoke();
                return;
            }

            string url = $"https://api.nexusmods.com/v3/games/kingdominc/mods/{mod.NexusId.Value}";
            var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("apikey", apiKey);
            req.SetRequestHeader("User-Agent", "KingdomInc-Foundation/0.8.0");
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
                        string latest = json["data"]["version"].AsString;
                        mod.LatestVersion = latest;
                        mod.IsOutdated = IsOutdated(mod.InstalledVersion, latest);
                        mod.Checked = true;
                        mod.StatusText = mod.IsOutdated ? "Update Available" : "Up to date";
                    }
                    else
                    {
                        mod.Checked = false;
                        if (req.responseCode == 401 || req.responseCode == 403)
                            mod.StatusText = "Invalid API Key";
                        else
                            mod.StatusText = "Check failed (" + req.responseCode + ")";
                    }
                }
                catch (Exception ex)
                {
                    mod.StatusText = "Error: " + ex.Message;
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
