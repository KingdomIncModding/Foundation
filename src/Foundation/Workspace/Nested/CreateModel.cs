using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LightJson;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.BuyMenu;
using Assets.Source.Flow;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomFoundation
{
    internal static partial class WorkspaceIntegration
    {
        [HarmonyPatch(typeof(FlowElement), nameof(FlowElement.Create))]
        static class CreateModel
        {
            [HarmonyPriority(Priority.Last)]
            static bool Prefix(string name, ref FlowElement __result)
            {
                if (WorkspaceNodeRegistry.Shared.TryResolve(name, out var registration))
                {
                    // Inactive registrations remain loadable for placed nodes during this session.
                    __result = registration.CreateModel(); return false;
                }
                // Type.GetType in the native method can resolve against Harmony's
                // generated assembly. Resolve built-in types in the game assembly.
                var native = typeof(FlowElement).Assembly.GetType("Assets.Source.Flow.Elements." + name);
                if (native == null && deserializeDepth > 0)
                {
                    // Preserve unknown saved node identity and ports, rather than
                    // aborting an entire kingdom load because one mod is missing.
                    __result = new MissingWorkspaceElement(name);
                    FoundationApi.Diagnostics.Write("Foundation", FoundationDiagnosticLevel.Warning,
                        "Missing workspace type '" + name + "' restored as inert placeholder; its JSON is preserved.");
                    return false;
                }
                if (native == null) return true; // Another mod may resolve non-native types.
                __result = (FlowElement)Activator.CreateInstance(native); return false;
            }
        }
    }

}
