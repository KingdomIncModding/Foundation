using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the mod identity API.</summary>
    public static class ModIdentity
    {
        public static void Validate(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 160)
                throw new ArgumentException("A stable, nonempty mod identifier of at most 160 characters is required.", nameof(id));
            foreach (char c in id)
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_')
                    throw new ArgumentException("Mod IDs may contain only letters, numbers, '.', '-' and '_'.", nameof(id));
        }
    }
}
