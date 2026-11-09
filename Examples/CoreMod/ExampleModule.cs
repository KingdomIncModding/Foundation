using System;
using Assets.Source.Player;
using KingdomFoundation.Core;

namespace FoundationExamples.CoreMod
{
    internal sealed class ExampleModule : IFoundationModule, IFoundationWorldModule
    {
        public const string ModId = "example.foundation.core";
        public ModDescriptor Descriptor { get; } = new ModDescriptor(
            ModId, "Example Core Module", new Version(1, 0, 0),
            author: "Foundation SDK", description: "A small example with an owned cross-mod service.",
            dependencies: new [] { new ModDependency("urakka.kingdominc.foundation", DependencyKind.BepInExPlugin) },
            minimumCoreApiVersion: new Version(1, 0));
        IModContext context;
        public void Initialize(IModContext scope)
        {
            context = scope;
            // Register under the SERVICE INTERFACE type, so other mods can resolve it.
            scope.Services.Register<IExampleCounter>(ModId + ":counter", new ExampleCounter(),
                new Version(1, 0));
            scope.Events.Subscribe<WorldChangedEvent>(evt =>
                scope.Log(CoreSeverity.Info, "world", evt.Current == null ? "World exited" : "World loaded"));
            scope.Log(CoreSeverity.Info, "initialize", "Example module initialized.");
        }
        public void WorldLoaded(GamePlayer world)
        {
            context.Services.Resolve<IExampleCounter>(ModId + ":counter").Increment();
        }
        public void WorldUnloading(GamePlayer world)
        {
            context.Log(CoreSeverity.Info, "world-unload", "Kingdom unloaded.");
        }
        public void Shutdown()
        {
            context?.Log(CoreSeverity.Info, "shutdown", "Example module shutting down.");
            context = null;
        }
    }
}
