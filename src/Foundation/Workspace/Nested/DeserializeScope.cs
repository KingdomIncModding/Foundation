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
        [HarmonyPatch(typeof(FlowElement), nameof(FlowElement.FromJson))]
        static class DeserializeScope
        {
            // Native FromJson eagerly constructs FlowUpgrade and InventoryItem
            // instances from stored data. If their defining mod is missing, it
            // throws before the placeholder can preserve the element's JSON.
            // Treat absent node types as inert opaque content instead.
            [HarmonyPriority(Priority.First)]
            static bool Prefix(JsonValue data, ref FlowElement __result, out bool __state)
            {
                __state = false;
                if (data.IsJsonObject && data["ElementType"].IsString)
                {
                    string id = data["ElementType"].AsString;
                    if (!string.IsNullOrWhiteSpace(id) &&
                        !WorkspaceNodeRegistry.Shared.TryResolve(id, out _) &&
                        typeof(FlowElement).Assembly.GetType("Assets.Source.Flow.Elements." + id) == null)
                    {
                        var missing = new MissingWorkspaceElement(id);
                        missing.RestoreRaw(data.AsJsonObject);
                        __result = missing;
                        FoundationApi.Diagnostics.Write("Foundation", FoundationDiagnosticLevel.Warning,
                            "Unknown workspace node '" + id + "' retained without deserializing unavailable upgrades/items.");
                        return false;
                    }
                }
                deserializeDepth++;
                __state = true;
                return true;
            }
            static Exception Finalizer(Exception __exception, bool __state)
            {
                if (__state) deserializeDepth--;
                return __exception;
            }
        }
    }

}
