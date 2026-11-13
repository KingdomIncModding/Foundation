using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed class SignalRegistry : ISignalDirectory
    {
        readonly Dictionary<string, global::KingdomFoundation.Api.SignalRegistrySource> registered = new Dictionary<string, global::KingdomFoundation.Api.SignalRegistrySource>(StringComparer.Ordinal);
        public IReadOnlyList<ISignalSource> Sources => Array.AsReadOnly(registered.Values.Cast<ISignalSource>().OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ThenBy(v => v.Id, StringComparer.Ordinal).ToArray());

        public event Action Changed;
        public ISignalSource Register(SignalSourceDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (registered.ContainsKey(definition.Id))
                throw new ArgumentException("Signal source already registered: " + definition.Id, nameof(definition));
            global::KingdomFoundation.Api.SignalRegistrySource source = new global::KingdomFoundation.Api.SignalRegistrySource(this, definition);
            registered.Add(source.Id, source);
            Notify();
            return source;
        }

        public bool TryGet(string id, out ISignalSource source)
        {
            if (id != null && registered.TryGetValue(id, out global::KingdomFoundation.Api.SignalRegistrySource found))
            {
                source = found;
                return true;
            }

            source = null;
            return false;
        }

        public IReadOnlyList<ISignalSource> Find(string query = null, string ownerId = null)
        {
            return Array.AsReadOnly(Sources.Where(x => (ownerId == null || x.ModId == ownerId) && (string.IsNullOrWhiteSpace(query) || x.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray());
        }

        public bool TryResolve(SignalSelection selection, out ISignalProperty property)
        {
            property = null;
            return selection != null && TryGet(selection.SourceId, out var source) && source.TryGetProperty(selection.PropertyId, out property);
        }

        public IReadOnlyList<ISignalProperty> FindProperties(string sourceId, Type valueType = null, bool writable = false)
        {
            if (!TryGet(sourceId, out var source))
                return Array.Empty<ISignalProperty>();
            return Array.AsReadOnly(source.Properties.Where(property => (valueType == null || valueType == property.ValueType) && (!writable || property.CanWrite)).ToArray());
        }

        public int RemoveOwner(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                return 0;
            var targets = registered.Values.Where(source => source.ModId == modId).ToArray();
            foreach (global::KingdomFoundation.Api.SignalRegistrySource target in targets)
                target.Dispose();
            return targets.Length;
        }

        internal void Remove(global::KingdomFoundation.Api.SignalRegistrySource source)
        {
            if (registered.TryGetValue(source.Id, out global::KingdomFoundation.Api.SignalRegistrySource current) && ReferenceEquals(current, source))
            {
                registered.Remove(source.Id);
                Notify();
            }
        }

        void Notify()
        {
            if (Changed == null)
                return;
            foreach (Action subscriber in Changed.GetInvocationList())
                try
                {
                    subscriber();
                }
                catch (Exception e)
                {
                    FoundationApi.Report(e);
                }
        }

        internal void Poll()
        {
            // A sampled event for dynamic getters. Explicit Write raises a change immediately.
            // The host calls this no more than five times per second; no polling threads.
            foreach (global::KingdomFoundation.Api.SignalRegistrySource source in registered.Values.ToArray())
                if (source.IsRegistered)
                    foreach (var property in source.Properties)
                        ((global::KingdomFoundation.Api.SignalRegistryProperty)property).Poll();
        }
    }
}
