using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Section-heading access for Options view observers without changing the IOptionsView contract.</summary>
    public static class OptionsViewExtensions
    {
        public static IReadOnlyList<IVanillaSectionView> GetVanillaSections(this IOptionsView view)
            => view is IVanillaSectionProvider provider ? provider.VanillaSections : Array.Empty<IVanillaSectionView>();

        public static bool TryGetVanillaSection(this IOptionsView view, string id, out IVanillaSectionView section)
        {
            section = null;
            return view is IVanillaSectionProvider provider && provider.TryGetVanillaSection(id, out section);
        }
    }
}
