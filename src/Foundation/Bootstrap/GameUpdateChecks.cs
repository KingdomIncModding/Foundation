using System;
using System.Collections;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.Flow.Elements;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;
using LightJson;
using UnityEngine;
using UnityEngine.UI;
using TimerNode = Assets.Source.Flow.Elements.Timer;

namespace KingdomFoundation
{
    /// <summary>Game 1.0.14 regressions, run only inside the isolated Foundation diagnostic kingdom.</summary>
    internal static class GameUpdateChecks
    {
        internal static IEnumerator Run(Action<bool, string> check)
        {
            VerifyEconomy(check);
            VerifySaves(check);
            var manager = WorldFlowManager.Instance;
            var timer = new TimerNode { Position = new Vector2Int(-1800, -1200) };
            var track = new TrackSwitch { Position = new Vector2Int(-1400, -1200) };
            manager.CreateNewElement(timer);
            manager.CreateNewElement(track);
            try
            {
                // Native views bind their model in Start, on the following frame.
                yield return null;
                check(!GamePlayer.Current.HasTech("outfitter_timeout"), "Timer regression runs without outfitter timeout research");
                check(FoundationMachineApi.Capabilities.SupportedCommands(timer).Contains("SetTimer"),
                    "Timer command is discoverable without research in game 1.0.14");
                var result = FoundationMachineApi.Operations.SetTimer(timer, 7.5f);
                check(result.Succeeded && timer.MaxTime == 7.5f && timer.TimeLeft == 7.5f,
                    "Timer operation configures and resets the native timer without research");
                foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f, -1f })
                    check(!FoundationMachineApi.Operations.SetTimer(timer, invalid).Succeeded && timer.MaxTime == 7.5f,
                        "Invalid timer interval is rejected without mutation: " + invalid);

                var control = track.World.GetComponentInChildren<WorldFlowTrackSwitch>();
                check(control, "Switch regression uses the actual native child control");
                var background = AccessTools.Field(typeof(WorldFlowTrackSwitch), "_background").GetValue(control) as Image;
                var up = AccessTools.Field(typeof(WorldFlowTrackSwitch), "_bgUp").GetValue(control) as Sprite;
                var down = AccessTools.Field(typeof(WorldFlowTrackSwitch), "_bgDown").GetValue(control) as Sprite;
                GamePlayer.Current.AddTech("track_switch_2");
                track.UpdateTrackSwitch(newTech: true);
                track.World.UpdateIO();
                check(track.Outputs.Count == 2 && background.sprite == down,
                    "Native switch research upgrade adds the second output and refreshes its background");
                FoundationApi.Machines.SetSwitch(track, true);
                check(track.SwitchPosition && background.sprite == up,
                    "General switch API refreshes the game 1.0.14 background");
                check(FoundationMachineApi.Operations.SetSwitch(track, false).Succeeded && !track.SwitchPosition && background.sprite == down,
                    "Specialized switch operation shares the native sprite refresh path");
                FoundationApi.Machines.SetSwitch(track, false);
                check(track.Outputs.Count == 2, "Repeated switch operations do not duplicate native ports");
            }
            finally
            {
                manager.RemoveElement(timer);
                manager.RemoveElement(track);
            }
        }

        private static void VerifyEconomy(Action<bool, string> check)
        {
            var player = GamePlayer.Current;
            var node = PrestigeNode.CurrencyTaxReduce;
            int original = player.GetPrestigePoints(node);
            try
            {
                player.SetPrestigePoints(node, 3);
                double native = player.GetExchangeTax(Currency.Gold);
                check(FoundationApi.Economy.ExchangeTaxDouble(Currency.Gold) == native,
                    "ExchangeTaxDouble preserves the exact native double-precision result");
#pragma warning disable CS0618 // Intentionally verify the retained SDK compatibility signature.
                check(FoundationApi.Economy.ExchangeTax(Currency.Gold) == (float)native,
                    "Deprecated float exchange-tax API remains callable as a compatibility adapter");
#pragma warning restore CS0618
                check(native != (double)(float)native, "Exchange-tax precision fixture detects accidental float narrowing");
                player.SetPrestigePoints(node, 100);
                check(FoundationApi.Economy.ExchangeTaxDouble(Currency.Gold) == 0.05,
                    "Exchange tax keeps the native game 1.0.14 minimum of five percent");
                var legacy = typeof(IGameEconomyService).GetMethod("ExchangeTax");
                var deprecated = (ObsoleteAttribute)Attribute.GetCustomAttribute(legacy, typeof(ObsoleteAttribute));
                check(legacy.ReturnType == typeof(float) && deprecated != null && !deprecated.IsError &&
                    deprecated.Message.Contains("1.0.13") && deprecated.Message.Contains("next Foundation release"),
                    "SDK deprecation records the last native game build and next-release removal");
            }
            finally { player.SetPrestigePoints(node, original); }
        }

        private static void VerifySaves(Action<bool, string> check)
        {
            const string owner = "foundation.diagnostics.game-update";
            int value = 42, restored = 0;
            using (FoundationApi.ModSave.Register(owner, 1, () => new JsonObject { { "Value", value } },
                (version, json) => { if (version > 0) restored = json["Value"].AsInteger; }))
            {
                var originalPlayer = GamePlayer.Current;
                var document = SaveGame.SaveCurrentState();
                check(!document.ContainsKey("Secret") && document[ModSaveRegistry.RootKey][owner]["Data"]["Value"].AsInteger == 42,
                    "Native 1.0.14 saves retain mod-owned data without introducing Secret");
                try
                {
                    SaveGame.LoadState(document);
                    check(restored == 42, "Full native save/load round trip restores Foundation mod data without Secret");
                    var current = SaveGame.SaveCurrentState();
                    check(!current.ContainsKey("Secret") && current["Player"].IsJsonObject,
                        "Resaving a loaded game 1.0.14 document keeps its checksum-free native shape");
                    document["Version"] = "1.0.13";
#pragma warning disable CS0618 // Intentionally verify retained game 1.0.13 compatibility.
                    LegacySaveChecksum.Refresh(document);
#pragma warning restore CS0618
                    var digest = document["Secret"].AsString;
                    var digestInput = JsonValue.Parse(document.ToString()).AsJsonObject;
                    digestInput.Remove("Secret");
                    check(digest == new SeedGenerator().Add(digestInput).Hash.ToString(),
                        "Retained legacy checksum adapter uses the game 1.0.13 digest");
                    SaveGame.LoadState(document);
                    check(restored == 42 && !SaveGame.SaveCurrentState().ContainsKey("Secret"),
                        "Game 1.0.13 saves with Secret still load and resave in the game 1.0.14 format");
                }
                finally { GamePlayer.Current = originalPlayer; }
            }
        }
    }
}
