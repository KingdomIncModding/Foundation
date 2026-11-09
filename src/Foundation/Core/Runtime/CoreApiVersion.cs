using System;

namespace KingdomFoundation.Core
{
    public static class CoreApiVersion
    {
        public static Version Current => new Version(1, 0, 0);
        /// <summary>Same API major, and requested version no newer than this implementation.</summary>
        public static bool Supports(Version minimum)
            => minimum == null || (minimum.Major == Current.Major && minimum <= Current);
    }
}
