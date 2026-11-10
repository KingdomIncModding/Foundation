using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the mod config registry API.</summary>
    public class ModConfigRegistry : IModConfigRegistry
    {
        readonly Dictionary<string, ModConfigSection> sections = new Dictionary<string, ModConfigSection>(StringComparer.Ordinal);
        readonly List<ModConfigSection> ordered = new List<ModConfigSection>();

        public IReadOnlyList<IModConfigSection> Sections => ordered.ToArray();
        public event Action Changed;

        /// <summary>Gets or create section.</summary>
        public IModConfigSection GetOrCreateSection(string modId, string modName = null, string description = null)
        {
            if (string.IsNullOrEmpty(modId)) throw new ArgumentException("ModId cannot be empty.", nameof(modId));
            if (!sections.TryGetValue(modId, out var section))
            {
                string name = modName;
                if (string.IsNullOrEmpty(name))
                {
                    var detected = TechModIdentity.Detect();
                    name = detected.Item2;
                }
                section = new ModConfigSection(this, modId, name ?? modId, description);
                sections.Add(modId, section);
                ordered.Add(section);
                Notify();
            }
            else
            {
                if (!string.IsNullOrEmpty(modName) && string.IsNullOrEmpty(section.ModName))
                    section.ModName = modName;
                if (!string.IsNullOrEmpty(description))
                    section.Description = description;
            }
            return section;
        }

        /// <summary>Attempts to get section.</summary>
        public bool TryGetSection(string modId, out IModConfigSection section)
        {
            if (modId != null && sections.TryGetValue(modId, out var found))
            {
                section = found;
                return true;
            }
            section = null;
            return false;
        }

        /// <summary>Removes section.</summary>
        public void RemoveSection(string modId)
        {
            if (modId != null && sections.TryGetValue(modId, out var found))
            {
                sections.Remove(modId);
                ordered.Remove(found);
                Notify();
            }
        }

        internal void Notify()
        {
            if (Changed == null) return;
            foreach (Action subscriber in Changed.GetInvocationList())
                try { subscriber(); } catch (Exception error) { FoundationApi.Report(error); }
        }
    }
}
