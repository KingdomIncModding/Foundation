using System;
using KingdomFoundation.Api;
namespace KingdomFoundation.Api
{
    /// <summary>Convenient implementation; mods may implement IOptionsTab themselves instead.</summary>
    public sealed class OptionsTab : IOptionsTab
    {
        public string ModId { get; }
        public string Id { get; }
        public string Title { get; }
        public int Order { get; }
        public IModConfigSection Settings { get; }
        public OptionsTab(string modId, string id, string title, int order = 0)
        {
            OptionsTabRegistry.ValidateIdentity(modId, id, title);
            ModId = modId; Id = id; Title = title; Order = order;
            Settings = new ModConfigRegistry().GetOrCreateSection(modId, title);
        }
    }
}
