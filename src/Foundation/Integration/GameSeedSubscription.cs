using System;
using KingdomFoundation.Json;

namespace KingdomFoundation.Api
{
    internal sealed class GameSeedSubscription : IDisposable
    {
        internal readonly string ModId;
        internal readonly Func<GameSeed, string, bool> AddString;
        internal readonly Func<GameSeed, JsonObject, bool> AddObject;
        private readonly Action<GameSeedSubscription> remove;
        internal volatile bool Active = true;
        internal GameSeedSubscription(string modId, Func<GameSeed, string, bool> addString, Func<GameSeed, JsonObject, bool> addObject, Action<GameSeedSubscription> remove)
        { ModId = modId; AddString = addString; AddObject = addObject; this.remove = remove; }
        public void Dispose() { if (!Active) return; Active = false; remove(this); }
    }
}
