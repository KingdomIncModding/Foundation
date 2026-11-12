using KingdomFoundation.Api;
namespace KingdomFoundation
{
    internal sealed class OptionsProbeTab : IOptionsTab
    {
        public string ModId => "foundation.options.check";
        public string Id => "polymorphic";
        public string Title => "Custom Tools";
        public int Order => 1;
        public IModConfigSection Settings { get; }
        internal OptionsProbeTab(IModConfigSection settings) { Settings = settings; }
    }
}
