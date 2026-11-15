using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Bootstrap;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine.Networking;

namespace KingdomFoundation
{
    public static class NexusUpdateChecker
    {
        internal sealed class NexusFileEntry
        {
            internal int FileId;
            internal int GroupId;
            internal string Name;
            internal string Version;
            internal string Category;
            internal int CategoryId;
            internal long Date;
        }

        internal sealed class NexusHttpResponse
        {
            internal bool Success;
            internal long ResponseCode;
            internal string Body;
        }

        internal delegate void HttpFetchHandler(string method, string url, string body, IDictionary<string, string> headers, Action<NexusHttpResponse> done);

        private readonly struct SemVerParts
        {
            internal readonly int[] Core;
            internal readonly string[] Prerelease;
            internal readonly bool Valid;

            internal SemVerParts(int[] core, string[] prerelease, bool valid)
            {
                Core = core;
                Prerelease = prerelease;
                Valid = valid;
            }
        }

        private static readonly Dictionary<string, (int NexusId, int? FileId)> customMappings = new Dictionary<string, (int, int?)>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ModItem> cachedMods = new List<ModItem>();
        private static int checkGeneration;
        internal static HttpFetchHandler TransportOverrideForTest;

        public static IReadOnlyList<ModItem> CachedMods => cachedMods;
        public static bool IsCheckingAll { get; private set; }
        public static string LastCheckSummary { get; private set; } = "Not checked";
        internal static string UserAgent => "KingdomInc-Foundation/" + FoundationPlugin.Version;

        public static event Action Changed;

        public static void RegisterModMapping(string guid, int nexusId) => RegisterModMapping(guid, nexusId, null);

        public static void RegisterModMapping(string guid, int nexusId, int? fileId)
        {
            if (string.IsNullOrEmpty(guid) || nexusId <= 0)
                return;
            customMappings[guid] = (nexusId, fileId);
            var existing = cachedMods.FirstOrDefault(m => string.Equals(m.Guid, guid, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.NexusId = nexusId;
                if (fileId.HasValue)
                    existing.FileId = fileId;
                Changed?.Invoke();
            }
        }

        public static bool TryLoadModJson(PluginInfo plugin, out string gameDomain, out int? nexusId)
            => TryLoadModJson(plugin, out gameDomain, out nexusId, out _);

        public static bool TryLoadModJson(PluginInfo plugin, out string gameDomain, out int? nexusId, out int? fileId)
        {
            gameDomain = null;
            nexusId = null;
            fileId = null;
            if (plugin == null)
                return false;
            string candidate = null;
            if (!string.IsNullOrEmpty(plugin.Location))
            {
                string dir = Path.GetDirectoryName(plugin.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    string localJson = Path.Combine(dir, "mod.json");
                    if (File.Exists(localJson))
                        candidate = localJson;
                    else
                    {
                        var parent = Directory.GetParent(dir);
                        if (parent != null)
                        {
                            string parentJson = Path.Combine(parent.FullName, "mod.json");
                            if (File.Exists(parentJson))
                                candidate = parentJson;
                        }
                    }
                }
            }

            if (candidate == null && !string.IsNullOrEmpty(Paths.GameRootPath))
            {
                string modDirName = plugin.Metadata?.Name ?? Path.GetFileNameWithoutExtension(plugin.Location);
                if (!string.IsNullOrEmpty(modDirName))
                {
                    string devJson = Path.Combine(Paths.GameRootPath, "_Mods", modDirName, "mod.json");
                    if (File.Exists(devJson))
                        candidate = devJson;
                }
            }

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
                            if (!File.Exists(subJson))
                                continue;
                            try
                            {
                                var testJson = JsonValue.Parse(File.ReadAllText(subJson));
                                if (testJson.IsJsonObject)
                                {
                                    var obj = testJson.AsJsonObject;
                                    string id = obj.ContainsKey("id") ? obj["id"].AsString : null;
                                    string name = obj.ContainsKey("name") ? obj["name"].AsString : null;
                                    if (string.Equals(id, targetName, StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase)
                                        || (targetGuid != null && targetGuid.IndexOf(id ?? "---", StringComparison.OrdinalIgnoreCase) >= 0))
                                    {
                                        candidate = subJson;
                                        break;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }

            if (candidate == null || !File.Exists(candidate))
                return false;

            try
            {
                var json = JsonValue.Parse(File.ReadAllText(candidate));
                if (!json.IsJsonObject)
                    return false;
                var root = json.AsJsonObject;
                if (root.ContainsKey("distribution") && root["distribution"].IsJsonObject)
                {
                    var dist = root["distribution"].AsJsonObject;
                    if (dist.ContainsKey("nexus") && dist["nexus"].IsJsonObject)
                    {
                        var nexus = dist["nexus"].AsJsonObject;
                        if (nexus.ContainsKey("game") && nexus["game"].IsString)
                            gameDomain = nexus["game"].AsString;
                        nexusId = ReadIntOrString(nexus, "modId");
                        fileId = ReadIntOrString(nexus, "fileId");
                        return nexusId.HasValue;
                    }
                }
            }
            catch { }

            return false;
        }

        private static int? ReadIntOrString(JsonObject obj, string key)
        {
            if (obj == null || !obj.ContainsKey(key))
                return null;
            var val = obj[key];
            if (val.IsInteger)
                return val.AsInteger;
            if (val.IsString && int.TryParse(val.AsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                return parsed;
            return null;
        }

        public static void ResolveNexusInfo(string guid, PluginInfo plugin, out string gameDomain, out int? nexusId)
            => ResolveNexusInfo(guid, plugin, out gameDomain, out nexusId, out _);

        public static void ResolveNexusInfo(string guid, PluginInfo plugin, out string gameDomain, out int? nexusId, out int? fileId)
        {
            gameDomain = null;
            nexusId = null;
            fileId = null;

            if (TryLoadModJson(plugin, out gameDomain, out nexusId, out fileId))
            {
                if (string.IsNullOrEmpty(gameDomain))
                    gameDomain = "kingdominc";
                return;
            }

            if (!string.IsNullOrEmpty(guid) && customMappings.TryGetValue(guid, out var custom))
            {
                nexusId = custom.NexusId;
                fileId = custom.FileId;
                gameDomain = "kingdominc";
                return;
            }

            if (plugin?.Instance != null)
            {
                try
                {
                    object attr = plugin.Instance.GetType().GetCustomAttributes(false).FirstOrDefault(a =>
                        a is NexusModAttribute || a.GetType().Name == "NexusModAttribute" || a.GetType().Name == "NexusModIdAttribute");
                    if (attr != null)
                    {
                        var prop = attr.GetType().GetProperty("ModId") ?? attr.GetType().GetProperty("Id");
                        if (prop != null)
                        {
                            object val = prop.GetValue(attr);
                            if (val is int intId && intId > 0)
                                nexusId = intId;
                            else if (val is string strId && int.TryParse(strId, out int parsed) && parsed > 0)
                                nexusId = parsed;
                        }
                        var fileProp = attr.GetType().GetProperty("FileId");
                        if (fileProp != null)
                        {
                            object fval = fileProp.GetValue(attr);
                            if (fval is int fInt && fInt > 0)
                                fileId = fInt;
                            else if (fval is string fStr && int.TryParse(fStr, out int fParsed) && fParsed > 0)
                                fileId = fParsed;
                        }
                        if (nexusId.HasValue)
                        {
                            gameDomain = "kingdominc";
                            return;
                        }
                    }
                }
                catch { }
            }

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
                                nexusId = ReadIntOrString(obj, "nexus_id") ?? ReadIntOrString(obj, "id");
                                fileId = ReadIntOrString(obj, "file_id") ?? ReadIntOrString(obj, "fileId");
                                if (nexusId.HasValue)
                                {
                                    gameDomain = "kingdominc";
                                    return;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private static bool TryParseSemVer(string raw, out SemVerParts parts)
        {
            parts = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string s = raw.Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(1).TrimStart();
            if (string.IsNullOrEmpty(s))
                return false;

            int plus = s.IndexOf('+');
            if (plus >= 0)
                s = s.Substring(0, plus);

            string coreStr = s;
            string[] prerelease = null;
            int dash = s.IndexOf('-');
            if (dash >= 0)
            {
                coreStr = s.Substring(0, dash);
                string preStr = s.Substring(dash + 1);
                if (string.IsNullOrEmpty(preStr))
                    return false;
                prerelease = preStr.Split('.');
                for (int i = 0; i < prerelease.Length; i++)
                {
                    if (string.IsNullOrEmpty(prerelease[i]))
                        return false;
                }
            }

            if (string.IsNullOrEmpty(coreStr))
                return false;

            string[] rawNums = coreStr.Split('.');
            if (rawNums.Length == 0 || rawNums.Length > 4)
                return false;

            var core = new int[rawNums.Length];
            for (int i = 0; i < rawNums.Length; i++)
            {
                if (string.IsNullOrEmpty(rawNums[i]) || !int.TryParse(rawNums[i], NumberStyles.None, CultureInfo.InvariantCulture, out core[i]))
                    return false;
            }

            parts = new SemVerParts(core, prerelease, true);
            return true;
        }

        public static int CompareVersions(string leftStr, string rightStr)
        {
            if (!TryParseSemVer(leftStr, out var left) || !TryParseSemVer(rightStr, out var right))
                return 0;

            int maxCore = Math.Max(left.Core.Length, right.Core.Length);
            for (int i = 0; i < maxCore; i++)
            {
                int l = i < left.Core.Length ? left.Core[i] : 0;
                int r = i < right.Core.Length ? right.Core[i] : 0;
                if (l != r) return l.CompareTo(r);
            }

            bool leftPre = left.Prerelease != null && left.Prerelease.Length > 0;
            bool rightPre = right.Prerelease != null && right.Prerelease.Length > 0;
            if (!leftPre && !rightPre) return 0;
            if (!leftPre) return 1;  // stable > prerelease
            if (!rightPre) return -1;

            int maxPre = Math.Max(left.Prerelease.Length, right.Prerelease.Length);
            for (int i = 0; i < maxPre; i++)
            {
                if (i >= left.Prerelease.Length) return -1;
                if (i >= right.Prerelease.Length) return 1;
                string lp = left.Prerelease[i];
                string rp = right.Prerelease[i];
                bool lNum = int.TryParse(lp, NumberStyles.None, CultureInfo.InvariantCulture, out int li);
                bool rNum = int.TryParse(rp, NumberStyles.None, CultureInfo.InvariantCulture, out int ri);
                if (lNum && rNum)
                {
                    if (li != ri) return li.CompareTo(ri);
                }
                else if (lNum != rNum)
                {
                    return lNum ? -1 : 1; // SemVer: numeric identifiers have lower precedence than non-numeric
                }
                else
                {
                    int cmp = string.Compare(lp, rp, StringComparison.OrdinalIgnoreCase);
                    if (cmp != 0) return cmp;
                }
            }
            return 0;
        }

        public static bool IsOutdated(string currentStr, string latestStr)
        {
            if (!TryParseSemVer(currentStr, out _) || !TryParseSemVer(latestStr, out _))
                return false;
            return CompareVersions(currentStr, latestStr) < 0;
        }

        public static void RefreshInstalledList()
        {
            var map = cachedMods.ToDictionary(m => m.Guid, StringComparer.OrdinalIgnoreCase);
            var list = new List<ModItem>();
            foreach (var pair in Chainloader.PluginInfos)
            {
                var info = pair.Value;
                if (info?.Metadata == null)
                    continue;
                string guid = info.Metadata.GUID;
                if (string.Equals(guid, Core.FoundationCorePlugin.Id, StringComparison.OrdinalIgnoreCase))
                    continue;
                ResolveNexusInfo(guid, info, out string domain, out int? nexusId, out int? fileId);
                if (map.TryGetValue(guid, out var existing))
                {
                    existing.Name = info.Metadata.Name;
                    existing.InstalledVersion = info.Metadata.Version?.ToString() ?? "1.0.0";
                    existing.Location = info.Location;
                    if (!string.IsNullOrEmpty(domain))
                        existing.GameDomain = domain;
                    if (nexusId.HasValue)
                        existing.NexusId = nexusId;
                    if (fileId.HasValue)
                        existing.FileId = fileId;
                    list.Add(existing);
                }
                else
                {
                    list.Add(new ModItem
                    {
                        Guid = guid,
                        Name = info.Metadata.Name,
                        InstalledVersion = info.Metadata.Version?.ToString() ?? "1.0.0",
                        Location = info.Location,
                        GameDomain = domain ?? "kingdominc",
                        NexusId = nexusId,
                        FileId = fileId,
                        StatusText = "Not checked"
                    });
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
            CheckTargets(cachedMods, onDone);
        }

        internal static void CheckTargets(IList<ModItem> allMods, Action onDone = null)
        {
            int gen = Interlocked.Increment(ref checkGeneration);
            var mods = allMods ?? cachedMods;
            var targets = mods.Where(m => m.NexusId.HasValue).ToList();

            foreach (var m in mods)
            {
                if (!m.NexusId.HasValue)
                {
                    m.Checked = false;
                    m.IsOutdated = false;
                    m.LatestVersion = null;
                    m.IsChecking = false;
                    m.StatusText = "Local mod";
                }
            }

            if (targets.Count == 0)
            {
                IsCheckingAll = false;
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
                // Always clear prior check results at start so a failing recheck never keeps stale state.
                mod.Checked = false;
                mod.IsOutdated = false;
                mod.LatestVersion = null;
                mod.IsChecking = true;
                mod.StatusText = "Checking...";
            }

            Changed?.Invoke();
            foreach (var mod in targets)
            {
                CheckSingleMod(mod, gen, () =>
                {
                    if (gen != checkGeneration)
                        return;
                    pending--;
                    if (pending <= 0)
                    {
                        IsCheckingAll = false;
                        LastCheckSummary = BuildSummary(targets);
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

        internal static string BuildSummary(IReadOnlyList<ModItem> targets)
        {
            int total = targets.Count;
            if (total == 0) return "No Nexus mods detected";
            int succeeded = targets.Count(m => m.Checked);
            int outdated = targets.Count(m => m.Checked && m.IsOutdated);
            int failed = total - succeeded;

            if (outdated > 0 && failed == 0)
                return $"{outdated} update{(outdated == 1 ? "" : "s")} available";
            if (outdated > 0 && failed > 0)
                return $"{outdated} update{(outdated == 1 ? "" : "s")} available ({failed} unavailable)";
            if (failed > 0 && succeeded == 0)
                return $"Update check failed ({failed} unavailable)";
            if (failed > 0)
                return $"Up to date ({succeeded} checked, {failed} unavailable)";
            return "All mods up to date";
        }

        private static void CheckSingleMod(ModItem mod, int gen, Action callback)
        {
            if (!mod.NexusId.HasValue)
            {
                if (gen == checkGeneration)
                {
                    mod.IsChecking = false;
                    mod.Checked = false;
                    mod.IsOutdated = false;
                    mod.StatusText = "Local mod";
                }
                callback?.Invoke();
                return;
            }

            string domain = !string.IsNullOrEmpty(mod.GameDomain) ? mod.GameDomain : "kingdominc";
            string modUrl = $"https://api.nexusmods.com/v3/games/{domain}/mods/{mod.NexusId.Value}";
            SendHttp("GET", modUrl, null, response =>
            {
                if (gen != checkGeneration)
                    return;

                try
                {
                    if (!response.Success)
                    {
                        mod.Checked = false;
                        mod.IsOutdated = false;
                        mod.LatestVersion = null;
                        mod.StatusText = response.ResponseCode > 0 ? $"Check failed ({response.ResponseCode})" : "Check failed";
                        FinishMod();
                        return;
                    }

                    var json = JsonValue.Parse(response.Body);
                    string pageVersion = null;
                    int gameId = 0;
                    if (json.IsJsonObject && json["data"].IsJsonObject)
                    {
                        var data = json["data"].AsJsonObject;
                        if (data.ContainsKey("version") && data["version"].IsString)
                            pageVersion = data["version"].AsString;
                        gameId = ReadIntOrString(data, "game_id") ?? 0;
                    }

                    if (gameId > 0)
                    {
                        string gqlBody = "{\"query\":\"query { modFiles(modId: " + mod.NexusId.Value + ", gameId: " + gameId + ") { fileId groupId name version category categoryId date } }\"}";
                        SendHttp("POST", "https://api.nexusmods.com/v2/graphql", gqlBody, gqlResponse =>
                        {
                            if (gen != checkGeneration)
                                return;

                            try
                            {
                                string resolved = pageVersion;
                                bool streamLookupFailed = false;
                                if (gqlResponse.Success && TryParseGraphQlFiles(gqlResponse.Body, out var files))
                                {
                                    resolved = ResolveStreamVersion(mod, pageVersion, files);
                                }
                                else if (mod.FileId.HasValue)
                                {
                                    // If a specific file stream is configured and stream query failed, do not fall back to another stream's page version.
                                    streamLookupFailed = true;
                                }

                                ApplyResolvedVersion(mod, streamLookupFailed ? null : resolved);
                            }
                            catch
                            {
                                mod.Checked = false;
                                mod.IsOutdated = false;
                                mod.LatestVersion = null;
                                mod.StatusText = "Error";
                            }
                            finally
                            {
                                FinishMod();
                            }
                        });
                        return;
                    }

                    ApplyResolvedVersion(mod, mod.FileId.HasValue ? null : pageVersion);
                }
                catch
                {
                    mod.Checked = false;
                    mod.IsOutdated = false;
                    mod.LatestVersion = null;
                    mod.StatusText = "Error";
                }

                FinishMod();
            });

            void FinishMod()
            {
                if (gen == checkGeneration)
                    mod.IsChecking = false;
                callback?.Invoke();
            }
        }

        private static void ApplyResolvedVersion(ModItem mod, string latest)
        {
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
                mod.IsOutdated = false;
                mod.LatestVersion = null;
                mod.StatusText = "Check failed";
            }
        }

        internal static bool TryParseGraphQlFiles(string body, out List<NexusFileEntry> files)
        {
            files = new List<NexusFileEntry>();
            if (string.IsNullOrEmpty(body))
                return false;
            var json = JsonValue.Parse(body);
            if (!json.IsJsonObject || !json["data"].IsJsonObject || !json["data"]["modFiles"].IsJsonArray)
                return false;

            foreach (var item in json["data"]["modFiles"].AsJsonArray)
            {
                if (!item.IsJsonObject) continue;
                var obj = item.AsJsonObject;
                files.Add(new NexusFileEntry
                {
                    FileId = ReadIntOrString(obj, "fileId") ?? 0,
                    GroupId = ReadIntOrString(obj, "groupId") ?? 0,
                    Name = obj.ContainsKey("name") && obj["name"].IsString ? obj["name"].AsString : "",
                    Version = obj.ContainsKey("version") && obj["version"].IsString ? obj["version"].AsString : null,
                    Category = obj.ContainsKey("category") && obj["category"].IsString ? obj["category"].AsString : "",
                    CategoryId = ReadIntOrString(obj, "categoryId") ?? 0,
                    Date = obj.ContainsKey("date") && obj["date"].IsNumber ? (long)obj["date"].AsNumber : 0
                });
            }
            return true;
        }

        internal static string ResolveStreamVersion(ModItem mod, string pageVersion, IReadOnlyList<NexusFileEntry> files)
        {
            if (files == null || files.Count == 0)
                return mod.FileId.HasValue ? null : pageVersion;

            bool IsActive(NexusFileEntry f) =>
                !string.Equals(f.Category, "OLD_VERSION", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(f.Category, "ARCHIVED", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(f.Category, "REMOVED", StringComparison.OrdinalIgnoreCase)
                && f.CategoryId != 4 && f.CategoryId != 6 && f.CategoryId != 7;

            if (mod.FileId.HasValue)
            {
                int target = mod.FileId.Value;
                var groupFiles = files.Where(f => (f.GroupId == target || f.FileId == target) && !string.IsNullOrEmpty(f.Version)).ToList();
                if (groupFiles.Count == 0)
                    return null;
                var activeGroup = groupFiles.Where(IsActive).ToList();
                var pool = activeGroup.Count > 0 ? activeGroup : groupFiles;
                return PickLatestVersion(pool);
            }

            var active = files.Where(f => IsActive(f) && !string.IsNullOrEmpty(f.Version)).ToList();
            if (active.Count == 0)
                return pageVersion;

            var runtimeCandidates = active.Where(f => f.Name.IndexOf("SDK", StringComparison.OrdinalIgnoreCase) < 0).ToList();
            if (runtimeCandidates.Count == 0)
                runtimeCandidates = active;

            var distinctStreams = runtimeCandidates.GroupBy(f => f.GroupId > 0 ? f.GroupId : f.FileId).ToList();
            if (distinctStreams.Count == 1)
                return PickLatestVersion(runtimeCandidates);

            var mainOnly = runtimeCandidates.Where(f => string.Equals(f.Category, "MAIN", StringComparison.OrdinalIgnoreCase) || f.CategoryId == 1).ToList();
            if (mainOnly.Count > 0 && mainOnly.Select(f => f.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
                return mainOnly[0].Version;

            return pageVersion;
        }

        private static string PickLatestVersion(IReadOnlyList<NexusFileEntry> entries)
        {
            NexusFileEntry best = entries[0];
            for (int i = 1; i < entries.Count; i++)
            {
                var candidate = entries[i];
                int cmp = CompareVersions(best.Version, candidate.Version);
                if (cmp < 0 || (cmp == 0 && candidate.Date > best.Date))
                    best = candidate;
            }
            return best.Version;
        }

        internal static void SendHttp(string method, string url, string body, Action<NexusHttpResponse> done)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Accept"] = "application/json",
                ["User-Agent"] = UserAgent
            };

            if (TransportOverrideForTest != null)
            {
                TransportOverrideForTest(method, url, body, headers, done);
                return;
            }

            UnityWebRequest req;
            if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body ?? ""));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
            }
            else
            {
                req = UnityWebRequest.Get(url);
            }

            foreach (var kv in headers)
                req.SetRequestHeader(kv.Key, kv.Value);
            req.timeout = 10;

            var op = req.SendWebRequest();
            op.completed += _ =>
            {
                var result = new NexusHttpResponse
                {
                    Success = req.result == UnityWebRequest.Result.Success,
                    ResponseCode = req.responseCode,
                    Body = req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null
                };
                req.Dispose();
                done?.Invoke(result);
            };
        }
    }
}
