using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Machines
{
    // This is a source module of the ONE Foundation BepInEx plugin.
    // No second BaseUnityPlugin, plugin GUID, or Harmony owner is created.
    internal static class MachineRuntime
    {
        private static ManualLogSource _logger;
        private static float _nextScan;

        internal static void Initialize(Harmony harmony, ManualLogSource logger)
        {
            if (harmony == null) throw new ArgumentNullException(nameof(harmony));
            _logger = logger;
            MachineThread.CaptureMainThread();
            try
            {
                harmony.CreateClassProcessor(typeof(NativeTransferPatch)).Patch();
                harmony.CreateClassProcessor(typeof(NativeIOPipeCreatedPatch)).Patch();
                harmony.CreateClassProcessor(typeof(NativeIOPipeRemovedPatch)).Patch();
                _logger?.LogInfo("Machine Framework integrated: native registry, operations, IO, events, throughput and public API ready.");
            }
            catch (Exception ex)
            {
                ReportCallbackError("Machine native transfer patch failed", ex);
            }
        }

        internal static void Tick(float now)
        {
            if (now < _nextScan) return;
            _nextScan = now + 0.25f;
            try { FoundationMachineApi.Events.Scan(); }
            catch (Exception ex) { ReportCallbackError("Machine monitor scan failed", ex); }
        }

        internal static void Shutdown()
        {
            // Harmony patches belong to the Foundation plugin and are released
            // by FoundationPlugin.hooks.UnpatchSelf(), not by a second owner.
            FoundationMachineApi.Events.Clear();
            FoundationMachineApi.Telemetry.Clear();
            FoundationMachineApi.IO.Shutdown();
            FoundationMachineApi.Extensions.Clear();
            FoundationMachineApi.Commands.Clear();
            _logger = null;
            _nextScan = 0f;
        }

        internal static void ReportCallbackError(string context, Exception ex)
        {
            if (_logger != null) _logger.LogError("[Machines] " + context + ": " + ex);
            else Debug.LogError("[Foundation Machines] " + context + ": " + ex);
        }

        internal static void LogWarning(string text)
        {
            if (_logger != null) _logger.LogWarning("[Machines] " + text);
            else Debug.LogWarning("[Foundation Machines] " + text);
        }
    }
}
