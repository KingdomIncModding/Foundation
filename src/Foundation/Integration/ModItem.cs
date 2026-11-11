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
    public sealed class ModItem
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        public string InstalledVersion { get; set; }
        public string Location { get; set; }
        public string GameDomain { get; set; } = "kingdominc";
        public int? NexusId { get; set; }
        public int? GameScopedId { get; set; }
        public int? FileId { get; set; }
        public string LatestVersion { get; set; }
        public bool IsOutdated { get; set; }
        public bool IsChecking { get; set; }
        public bool Checked { get; set; }
        public string StatusText { get; set; }

        public string NexusUrl
        {
            get
            {
                if (!NexusId.HasValue) return null;
                string domain = !string.IsNullOrEmpty(GameDomain) ? GameDomain : "kingdominc";
                return $"https://www.nexusmods.com/{domain}/mods/{NexusId.Value}";
            }
        }
    }
}
