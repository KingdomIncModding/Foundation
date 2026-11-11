using System;
using System.IO;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Metadata loaded from the mod's standardized mod.json file.
    /// </summary>
    public sealed class ModMetadata
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string Author { get; }
        public string Description { get; }
        public string RepositoryUrl { get; }
        public string NexusGame { get; }
        public int? NexusModId { get; }
        public string ModDirectory { get; }
        public string AssetsDirectory { get; }

        public ModMetadata(string id, string name, string version, string author, string description, string repositoryUrl, string nexusGame, int? nexusModId, string modDirectory)
        {
            Id = id;
            Name = name;
            Version = version;
            Author = author;
            Description = description;
            RepositoryUrl = repositoryUrl;
            NexusGame = !string.IsNullOrEmpty(nexusGame) ? nexusGame : "kingdominc";
            NexusModId = nexusModId;
            ModDirectory = modDirectory;
            AssetsDirectory = !string.IsNullOrEmpty(modDirectory) ? Path.Combine(modDirectory, "Assets") : null;
        }

        public static ModMetadata LoadFromDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return null;
            string path = Path.Combine(directory, "mod.json");
            if (!File.Exists(path)) return null;

            try
            {
                var json = JsonValue.Parse(File.ReadAllText(path));
                if (!json.IsJsonObject) return null;
                var root = json.AsJsonObject;

                string id = root.ContainsKey("id") ? root["id"].AsString : null;
                string name = root.ContainsKey("name") ? root["name"].AsString : null;
                string version = root.ContainsKey("version") ? root["version"].AsString : null;
                string author = root.ContainsKey("author") ? root["author"].AsString : null;
                string description = root.ContainsKey("description") ? root["description"].AsString : null;

                string repoUrl = null;
                if (root.ContainsKey("repository") && root["repository"].IsJsonObject)
                {
                    var repo = root["repository"].AsJsonObject;
                    if (repo.ContainsKey("url")) repoUrl = repo["url"].AsString;
                }

                string nexusGame = "kingdominc";
                int? nexusModId = null;
                if (root.ContainsKey("distribution") && root["distribution"].IsJsonObject)
                {
                    var dist = root["distribution"].AsJsonObject;
                    if (dist.ContainsKey("nexus") && dist["nexus"].IsJsonObject)
                    {
                        var nexus = dist["nexus"].AsJsonObject;
                        if (nexus.ContainsKey("game") && nexus["game"].IsString) nexusGame = nexus["game"].AsString;
                        if (nexus.ContainsKey("modId"))
                        {
                            if (nexus["modId"].IsInteger) nexusModId = nexus["modId"].AsInteger;
                            else if (nexus["modId"].IsString && int.TryParse(nexus["modId"].AsString, out int parsed))
                                nexusModId = parsed;
                        }
                    }
                }

                return new ModMetadata(id, name, version, author, description, repoUrl, nexusGame, nexusModId, directory);
            }
            catch
            {
                return null;
            }
        }
    }
}

