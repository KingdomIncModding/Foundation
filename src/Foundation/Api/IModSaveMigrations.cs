using System;
using LightJson;
namespace KingdomFoundation.Api
{
    /// <summary>Additive schema migration API; IModSaveRegistry remains binary compatible.</summary>
    public interface IModSaveMigrations : IModSaveRegistry
    {
        IDisposable RegisterMigrating(string modId, int schemaVersion, Func<JsonValue> save,
            Action<int, JsonValue> load, Func<int, JsonValue, JsonValue> migrate);
    }
}
