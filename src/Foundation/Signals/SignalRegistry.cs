using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed partial class SignalRegistry : ISignalDirectory
    {
        readonly Dictionary<string, Source> registered = new Dictionary<string, Source>(StringComparer.Ordinal);
        /// <summary>Gets or sets the sources value.</summary>
        public IReadOnlyList<ISignalSource> Sources => Array.AsReadOnly(registered.Values.Cast<ISignalSource>().OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ThenBy(v => v.Id, StringComparer.Ordinal).ToArray());
        public event Action Changed;
        public ISignalSource Register(SignalSourceDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (registered.ContainsKey(definition.Id)) throw new ArgumentException("Signal source already registered: " + definition.Id, nameof(definition));
            var source = new Source(this, definition);
            registered.Add(source.Id, source);
            Notify();
            return source;
        }
        /// <summary>Attempts to get.</summary>
        public bool TryGet(string id, out ISignalSource source)
        {
            if (id != null && registered.TryGetValue(id, out var found)) { source = found; return true; }
            source = null; return false;
        }
        public IReadOnlyList<ISignalSource> Find(string query = null, string ownerId = null)
        {
            return Array.AsReadOnly(Sources.Where(x =>
                (ownerId == null || x.ModId == ownerId) &&
                (string.IsNullOrWhiteSpace(query) ||
                 x.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 x.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray());
        }
        /// <summary>Attempts to resolve.</summary>
        public bool TryResolve(SignalSelection selection, out ISignalProperty property)
        {
            property = null;
            return selection != null && TryGet(selection.SourceId, out var source) &&
                source.TryGetProperty(selection.PropertyId, out property);
        }
        public IReadOnlyList<ISignalProperty> FindProperties(string sourceId, Type valueType = null, bool writable = false)
        {
            if (!TryGet(sourceId, out var source)) return Array.Empty<ISignalProperty>();
            return Array.AsReadOnly(source.Properties.Where(property =>
                (valueType == null || valueType == property.ValueType) &&
                (!writable || property.CanWrite)).ToArray());
        }
        /// <summary>Removes owner.</summary>
        public int RemoveOwner(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) return 0;
            var targets = registered.Values.Where(source => source.ModId == modId).ToArray();
            foreach (var target in targets) target.Dispose();
            return targets.Length;
        }
        void Remove(Source source)
        {
            if (registered.TryGetValue(source.Id, out var current) && ReferenceEquals(current, source))
            { registered.Remove(source.Id); Notify(); }
        }
        void Notify()
        {
            if (Changed == null) return;
            foreach (Action subscriber in Changed.GetInvocationList())
                try { subscriber(); } catch (Exception e) { FoundationApi.Report(e); }
        }
        internal void Poll()
        {
            // A sampled event for dynamic getters. Explicit Write raises a change immediately.
            // The host calls this no more than five times per second; no polling threads.
            foreach (var source in registered.Values.ToArray())
                if (source.IsRegistered) foreach (var property in source.Properties) ((Property)property).Poll();
        }


    }
}
