using System;
using BepInEx;
using KingdomFoundation.Api;
using LightJson;

namespace FoundationExamples
{
    /// <summary>Optional SDK sample. Never included in the Foundation player install.</summary>
    [BepInPlugin(ModId, "Foundation Services Sample", "0.10.0")]
    [BepInDependency("urakka.kingdominc.foundation", "0.10.0")]
    public sealed class ExampleServicesPlugin : BaseUnityPlugin
    {
        public const string ModId = "urakka.foundation.services.example";
        FoundationModContext scope;
        bool enabledValue = true;
        int eventsSeen;
        SignalSelection selected = new SignalSelection(null, null);
        void Awake()
        {
            scope = FoundationApi.ForMod(ModId);
            scope.Persist(1,
                () => new JsonObject { { "Enabled", enabledValue }, { "EventsSeen", eventsSeen },
                    { "SignalSource", selected.SourceId }, { "SignalProperty", selected.PropertyId } },
                (schema, saved) =>
                {
                    if (schema < 1 || !saved.IsJsonObject)
                    { enabledValue = true; eventsSeen = 0; selected = new SignalSelection(null, null); return; }
                    enabledValue = saved["Enabled"].AsBoolean;
                    eventsSeen = saved["EventsSeen"].AsInteger;
                    selected = new SignalSelection(saved["SignalSource"].AsString, saved["SignalProperty"].AsString);
                });
            scope.Expose(new SignalSourceDefinition(ModId, ModId + ".status", "Sample Status", new[]
            {
                new SignalPropertyDefinition("enabled", "Enabled", typeof(bool), () => enabledValue, value => enabledValue = (bool)value),
                new SignalPropertyDefinition("events", "Workspace events", typeof(int), () => eventsSeen)
            }));
            scope.Listen<WorkspaceChangedEvent>(e => { if (e.Kind == WorkspaceChangeKind.Created) eventsSeen++; });
            var config = scope.Settings("Foundation Services Sample", "Public signal discovery, lifecycle, save and event example.");
            config.AddText("Native-styled signal and property selection", isHeader: true);
            config.AddToggle("Enabled", enabledValue, value => enabledValue = value, () => enabledValue);
            config.AddSignalSelector("Select Source", selected, value => selected = value, () => selected);
            config.AddButton("Print status", () => Logger.LogInfo("Events: " + eventsSeen + " / binding: " + selected), "Log Status");
            FoundationApi.Diagnostics.Write(ModId, FoundationDiagnosticLevel.Info, "Example Services initialized");
        }
        void OnDestroy() { scope?.Dispose(); }
    }
}
