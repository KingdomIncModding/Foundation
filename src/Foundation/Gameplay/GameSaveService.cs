using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Util;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed class GameSaveService : IGameSaveService
    {
        public IReadOnlyList<SaveGameFile> SavedGames()
            => Array.AsReadOnly(SaveGame.GetSaveGames().ToArray());
        public SaveGameFile FindSave(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return SaveGame.GetSaveGame(name);
        }
        public JsonObject ReadSave(string name)
        {
            var save = FindSave(name) ?? throw new ArgumentException("Save not found: " + name, nameof(name));
            return save.Recall();
        }
        public JsonObject CreateSnapshot()
        {
            GameWorldService.RequirePlayer();
            return SaveGame.SaveCurrentState();
        }
        static string SafeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 96 ||
                name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0 ||
                name.Contains("..") || name.Contains("/") || name.Contains("\\"))
                throw new ArgumentException("Use a safe simple save name.", nameof(name));
            return name;
        }
        public void Save(string name)
        {
            GameWorldService.RequirePlayer();
            SaveGame.Store(SaveGame.SaveCurrentState(), SafeName(name));
        }
        public void Autosave()
        {
            GameWorldService.RequirePlayer();
            SaveGame.StoreAutosaveState();
        }
    }
}
