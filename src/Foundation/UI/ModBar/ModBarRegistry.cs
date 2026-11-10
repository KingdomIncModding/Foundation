using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Isolated registries are useful for consumers' tests; the HUD uses FoundationApi.ModBar.</summary>
    public sealed class ModBarRegistry : IModBarRegistry
    {
        readonly Dictionary<(string, string), ModBarRegistration> items = new Dictionary<(string, string), ModBarRegistration>();
        public IReadOnlyList<IModBarRegistration> Items => Array.AsReadOnly(items.Values
            .OrderBy(r => r.Element.Alignment)
            .ThenBy(r => r.Element.Order)
            .ThenBy(r => r.ModId, StringComparer.Ordinal)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .Cast<IModBarRegistration>()
            .ToArray());
        /// <summary>Occurs when changed is raised.</summary>
        public event Action Changed;
        public IModBarRegistration Register(IModBarElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            ValidateIdentity(element.ModId, element.Id);
            ValidateWidth(element.Width);
            ValidateAlignment(element.Alignment);
            var key = (element.ModId, element.Id);
            if (items.ContainsKey(key)) throw new ArgumentException("This mod already registered a Mod Bar item with this ID.", nameof(element));
            var registration = new ModBarRegistration(this, element);
            items.Add(key, registration); Notify(); return registration;
        }
        /// <summary>Registers separator.</summary>
        public IModBarRegistration RegisterSeparator(string modId, string id, ModBarAlignment alignment = ModBarAlignment.Left, int order = 0, float width = 16)
            => Register(new ModBarSeparator(modId, id, width, order, alignment));
        public bool TryGet(string modId, string id, out IModBarRegistration registration)
        {
            registration = null;
            if (modId == null || id == null || !items.TryGetValue((modId, id), out var found)) return false;
            registration = found; return true;
        }
        internal static void ValidateIdentity(string modId, string id)
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("A stable owning mod GUID is required.", nameof(modId));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable item ID is required.", nameof(id));
        }
        internal static void ValidateWidth(float width)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width < 8 || width > 2048)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be between 8 and 2048 UI units.");
        }
        internal static void ValidateAlignment(ModBarAlignment alignment)
        {
            if (!Enum.IsDefined(typeof(ModBarAlignment), alignment))
                throw new ArgumentOutOfRangeException(nameof(alignment), "Alignment must be Left, Center or Right.");
        }
        internal void Remove(ModBarRegistration item)
        {
            var key = (item.ModId, item.Id);
            if (items.TryGetValue(key, out var current) && ReferenceEquals(current, item)) { items.Remove(key); Notify(); }
        }
        internal void Notify()
        {
            if (Changed == null) return;
            foreach (Action subscriber in Changed.GetInvocationList())
                try { subscriber(); } catch (Exception error) { FoundationApi.Report(error); }
        }
    }
}
