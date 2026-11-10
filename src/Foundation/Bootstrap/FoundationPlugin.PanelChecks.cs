using System;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    /// <summary>Provides the foundation plugin API.</summary>
    public sealed partial class FoundationPlugin
    {
        void VerifyFoundationPanels()
        {
            // Pure registry tests run before a world is created; never open a popup
            // or modify user machines in the isolated native smoke test.
            var registry = new FoundationPanelRegistry();
            Check(registry.Registrations.Count == 0, "UI panel registry starts empty");
            int notifications = 0;
            registry.Changed += () => notifications++;
            Action<IFoundationPanelBuilder> build = panel => { };
            var generic = registry.Register(new FoundationPanelDefinition("ui.test", "general", "General", build));
            var machine = registry.Register(new FoundationPanelDefinition("ui.test", "all-machines", "All Machines", build, typeof(FlowElement)));
            var specialized = registry.Register(new FoundationPanelDefinition("ui.native", "switch", "Switch", build, typeof(TrackSwitch), -100));
            Check(registry.Registrations.Count == 3, "UI panel registry tracks independent mod windows and machine definitions");
            Check(registry.TryGet("ui.test", "general", out var general) && ReferenceEquals(general, generic.Definition),
                "UI panel registry resolves mod-owned window by stable identity");
            Check(registry.TryResolveMachine(typeof(TrackSwitch), out var resolved) && ReferenceEquals(resolved, specialized.Definition),
                "UI panel registry prefers native subtype over inherited fallback");
            Check(registry.TryResolveMachine(typeof(SingleButton), out var fallback) && ReferenceEquals(fallback, machine.Definition),
                "UI machine registry falls back to FlowElement");
            var overrideSwitch = registry.Register(new FoundationPanelDefinition("ui.mod", "switch", "Override", build, typeof(TrackSwitch), 200));
            Check(registry.TryResolveMachine(typeof(TrackSwitch), out var overrideFound) && ReferenceEquals(overrideFound, overrideSwitch.Definition),
                "UI machine registry respects priority for equal machine type");
            bool rejected = false;
            try { registry.Register(new FoundationPanelDefinition("ui.mod", "switch", "Duplicate", build, typeof(TrackSwitch))); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "UI panel rejects duplicate owner/local ID");
            rejected = false;
            try { new FoundationPanelDefinition("ui.mod", "bad", "Bad", build, typeof(string)); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "UI panel rejects non-machine type binding");
            overrideSwitch.Dispose();
            Check(registry.TryResolveMachine(typeof(TrackSwitch), out var restored) && ReferenceEquals(restored, specialized.Definition),
                "UI disposal restores registered native default");
            using (var owner = registry.ForMod("ui.consumer"))
            {
                var a = owner.Register("one", "First", build);
                var b = owner.Register("two", "Second", build, typeof(SingleButton));
                Check(registry.Registrations.Count == 5 && a.IsRegistered && b.IsRegistered,
                    "UI mod context owns multiple window and machine registrations");
            }
            Check(registry.Registrations.Count == 3 && registry.RemoveOwner("ui.consumer") == 0,
                "UI owner disposal unregisters every owned window");
            Check(registry.RemoveOwner("ui.test") == 2, "UI owner removal does not remove other mods");
            Check(registry.TryResolveMachine(typeof(TrackSwitch), out var remaining) && ReferenceEquals(remaining, specialized.Definition),
                "UI foreign mod's specialized panel survives owner removal");
            specialized.Dispose();
            generic.Dispose();
            machine.Dispose();
            Check(registry.Registrations.Count == 0 && notifications >= 10,
                "UI panel registration cleanup is complete and produces notifications");
            var readOnly = new FoundationUiProperty("health", "Health", typeof(float), () => 1f);
            Check(readOnly.Write == null && readOnly.ValueType == typeof(float),
                "UI dynamic property can be read-only without reflection");
            rejected = false;
            try { new FoundationUiProperty("bad", "Bad", typeof(DateTime), () => DateTime.Now); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "UI dynamic editor rejects unsupported property types");
        }
    }
}
