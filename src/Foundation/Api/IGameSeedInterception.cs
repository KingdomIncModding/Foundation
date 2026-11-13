using System;
using KingdomFoundation.Json;

namespace KingdomFoundation.Api
{
    public interface IGameSeedInterception
    {
        /// <summary>Return true after handling an operation to suppress native handling. Dispose to restore it.</summary>
        IDisposable Subscribe(string modId, Func<GameSeed, string, bool> addString, Func<GameSeed, JsonObject, bool> addObject);
    }
}
