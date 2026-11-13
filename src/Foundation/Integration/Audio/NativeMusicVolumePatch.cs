using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Behaviour.Util;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    [HarmonyPatch]
    internal static class NativeMusicVolumePatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MusicManager), "_updateVolume");
            yield return AccessTools.Method(typeof(MusicManager), "PreviewVolume");
            int found = 0;
            foreach (var type in typeof(MusicManager).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!type.Name.Contains("_playTrack") && !type.Name.Contains("_fadeOut")) continue;
                var moveNext = AccessTools.Method(type, "MoveNext");
                if (moveNext != null) { found++; yield return moveNext; }
            }
            if (found != 2) throw new NotSupportedException("Expected the native music playback and fade coroutine bodies.");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var setVolume = AccessTools.PropertySetter(typeof(AudioSource), "volume");
            var getVolume = AccessTools.PropertyGetter(typeof(AudioSource), "volume");
            int writes = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(setVolume))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(GameAudioInterceptionService), nameof(GameAudioInterceptionService.WriteMusic));
                    writes++;
                }
                else if (instruction.Calls(getVolume))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(GameAudioInterceptionService), nameof(GameAudioInterceptionService.ReadMusic));
                }
                yield return instruction;
            }
            if (writes == 0) throw new NotSupportedException("No native music volume write in " + original.Name);
        }
    }
}
