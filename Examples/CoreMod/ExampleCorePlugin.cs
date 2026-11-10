using BepInEx;
using KingdomFoundation.Core;

namespace FoundationExamples.CoreMod
{
    [BepInPlugin("example.foundation.core", "Foundation Core SDK example", "1.0.0")]
    [BepInDependency("urakka.kingdominc.foundation.core")]
    /// <summary>Provides the example core plugin API.</summary>
    public sealed class ExampleCorePlugin : BaseUnityPlugin
    {
        IModRegistration registration;
        void Awake() => registration = FoundationCoreApi.Core.Modules.Register(new ExampleModule());
        void OnDestroy() => registration?.Dispose();
    }
}
