using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Util;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>Native saves, including the Foundation ModSave extension in SaveCurrentState.</summary>
    public interface IGameSaveService
    {
        IReadOnlyList<SaveGameFile> SavedGames();
        SaveGameFile FindSave(string name);
        JsonObject ReadSave(string name);
        JsonObject CreateSnapshot();
        void Save(string name);
        void Autosave();
    }
}
