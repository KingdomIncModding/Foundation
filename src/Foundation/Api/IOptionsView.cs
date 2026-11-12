using System.Collections.Generic;
namespace KingdomFoundation.Api
{
    public interface IOptionsView
    {
        bool IsOpen { get; }
        IReadOnlyList<IVanillaSettingView> VanillaSettings { get; }
        bool TryGetVanillaSetting(string id, out IVanillaSettingView setting);
    }
}
