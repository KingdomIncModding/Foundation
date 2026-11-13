using System.Diagnostics;
using Assets.Source.Util;
using HarmonyLib;

namespace KingdomFoundation.Api
{
    public sealed class GameRuntimeInfo
    {
        public string Translate(string value)=>Translation.Translate(value);
        public GameWar FindWar(string id)=>GameWar.Wrap(Assets.Source.Adventure.WarType.Get(id));
        public string ToIdentifier(string value)=>value.ToSnakeCase();
        public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string,string>> UsableIcons=>System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(SpriteLibrary.GetUsableIconNames(),icon=>new System.Collections.Generic.KeyValuePair<string,string>(icon.icon,icon.name)));
        public UnityEngine.Sprite Icon(string id)=>SpriteLibrary.Get(id);
        public string AssemblyVersion => typeof(SaveGame).Assembly.GetName().Version.ToString();
        public string AssemblyModuleId => typeof(SaveGame).Module.ModuleVersionId.ToString();
        public string HookLibraryVersion => typeof(Harmony).Assembly.GetName().Version.ToString();
        public string SavesPath => SaveGame.SavesPath;
        /// <summary>Whether the current native autosave request came from the game's periodic UI update.</summary>
        public bool IsPeriodicAutosaveRequest()
        {
            var frames = new StackTrace(false).GetFrames();
            if (frames == null) return false;
            foreach (var frame in frames)
            {
                var method = frame.GetMethod(); var type = method?.DeclaringType;
                if (type == typeof(SaveGame) && method.Name == "StoreAutosaveState") continue;
                if (type?.Assembly == typeof(SaveGame).Assembly)
                    return type == typeof(Assets.Behaviour.UI.GameUI) && method.Name == "Update";
                if (type == null && method?.Name.StartsWith("DMD<Assets.Behaviour.UI.GameUI::Update") == true) return true;
            }
            return false;
        }
    }
}
