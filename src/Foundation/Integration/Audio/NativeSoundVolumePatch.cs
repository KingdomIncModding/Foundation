using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Assets.Behaviour.UI;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    [HarmonyPatch(typeof(UISounds), nameof(UISounds.PlaySound), new[] { typeof(AudioClip), typeof(float), typeof(float) })]
    internal static class NativeSoundVolumePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var setter = AccessTools.PropertySetter(typeof(AudioSource), "volume");
            int writes = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(setter))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(GameAudioInterceptionService), nameof(GameAudioInterceptionService.WriteEffect));
                    writes++;
                }
                yield return instruction;
            }
            if (writes != 1) throw new NotSupportedException("The native UISounds volume assignment has changed.");
        }
    }
}
