using System;
using HarmonyLib;
using KingdomFoundation.Json;

namespace KingdomFoundation.Api
{
    internal sealed class GameSeedInterception : IGameSeedInterception
    {
        private static volatile GameSeedSubscription[] subscriptions = Array.Empty<GameSeedSubscription>();
        private static readonly object Gate = new object();
        private static Harmony harmony;
        public IDisposable Subscribe(string modId, Func<GameSeed, string, bool> addString, Func<GameSeed, JsonObject, bool> addObject)
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("A mod identifier is required.", nameof(modId));
            if (addString == null && addObject == null) throw new ArgumentException("An interceptor is required.");
            lock (Gate)
            {
                if (harmony == null)
                {
                    var owner = new Harmony("urakka.kingdominc.foundation.seed-hooks");
                    try
                    {
                        owner.Patch(AccessTools.Method(typeof(SeedGenerator), "Add", new[] { typeof(string) }), prefix: new HarmonyMethod(typeof(GameSeedInterception), nameof(AddString)));
                        owner.Patch(AccessTools.Method(typeof(SeedGenerator), "Add", new[] { typeof(LightJson.JsonObject) }), prefix: new HarmonyMethod(typeof(GameSeedInterception), nameof(AddObject)));
                        harmony = owner;
                    }
                    catch { owner.UnpatchSelf(); throw; }
                }
                var subscription = new GameSeedSubscription(modId, addString, addObject, Remove);
                var next = new GameSeedSubscription[subscriptions.Length + 1];
                Array.Copy(subscriptions, next, subscriptions.Length); next[next.Length - 1] = subscription;
                subscriptions = next;
                return subscription;
            }
        }
        private static void Remove(GameSeedSubscription subscription)
        {
            lock (Gate) { subscriptions = Array.FindAll(subscriptions, item => item != subscription); }
        }
        private static bool AddString(SeedGenerator __instance, string data, ref SeedGenerator __result)
        {
            foreach (var entry in subscriptions)
            {
                if (!entry.Active || entry.AddString == null) continue;
                var seed = GameSeed.Wrap(__instance); ulong previous = seed.Hash;
                try { if (!entry.AddString(seed, data)) continue; }
                catch (Exception error) { seed.Hash = previous; FoundationApi.Report(error); continue; }
                __result = __instance; return false;
            }
            return true;
        }
        private static bool AddObject(SeedGenerator __instance, LightJson.JsonObject data, ref SeedGenerator __result)
        {
            foreach (var entry in subscriptions)
            {
                if (!entry.Active || entry.AddObject == null) continue;
                var seed = GameSeed.Wrap(__instance); ulong previous = seed.Hash;
                try { if (!entry.AddObject(seed, JsonObject.Wrap(data))) continue; }
                catch (Exception error) { seed.Hash = previous; FoundationApi.Report(error); continue; }
                __result = __instance; return false;
            }
            return true;
        }
    }
}
