using System.Runtime.CompilerServices;
using HarmonyLib;
using KingdomFoundation.Json;

namespace KingdomFoundation.Api
{
    /// <summary>Native seed state and entry points. Hashing policy belongs to the consumer.</summary>
    public sealed class GameSeed
    {
        private static readonly ConditionalWeakTable<SeedGenerator, GameSeed> Cache = new ConditionalWeakTable<SeedGenerator, GameSeed>();
        private static readonly AccessTools.FieldRef<SeedGenerator, ulong> HashField = AccessTools.FieldRefAccess<SeedGenerator, ulong>("<Hash>k__BackingField");
        internal SeedGenerator Native { get; }
        private GameSeed(SeedGenerator native) { Native = native; }
        public GameSeed() { Native = new SeedGenerator(); Cache.Add(Native, this); }
        internal static GameSeed Wrap(SeedGenerator native) => native == null ? null : Cache.GetValue(native, value => new GameSeed(value));
        public static ulong InitialHash => SeedGenerator.FnvOffset64;
        public static ulong Prime => SeedGenerator.FnvPrime64;
        public ulong Hash { get => Native.Hash; set => HashField(Native) = value; }
        public GameSeed Add(string value) { Native.Add(value); return this; }
        public GameSeed Add(JsonObject value) { Native.Add(value?.Native); return this; }
        public GameSeed Add(JsonArray value) { Native.Add(value?.Native); return this; }
    }
}
