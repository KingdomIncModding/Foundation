using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Optional section-heading capability, keeping the existing IOptionsView contract unchanged.</summary>
    public interface IVanillaSectionProvider
    {
        IReadOnlyList<IVanillaSectionView> VanillaSections { get; }
        bool TryGetVanillaSection(string id, out IVanillaSectionView section);
    }
}
