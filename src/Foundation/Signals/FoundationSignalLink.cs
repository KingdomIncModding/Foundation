using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>A live Foundation signal-to-signal binding. Never a native item pipe.</summary>
    public interface IFoundationSignalLink : IDisposable
    {
        string OwnerId { get; }
        SignalSelection Source { get; }
        SignalSelection Destination { get; }
        bool IsResolved { get; }
        string UnavailableReason { get; }
    }
    /// <summary>Owner-scoped routing over registered Foundation signal properties.</summary>
    public interface IFoundationSignalLinks
    {
        IReadOnlyList<IFoundationSignalLink> Links { get; }
        IFoundationSignalLink Connect(string ownerId, SignalSelection source, SignalSelection destination);
        int RemoveOwner(string ownerId);
        /// <summary>Serializable selections; save these inside the mod's existing ModSave payload.</summary>
        JsonValue ExportOwner(string ownerId);
        /// <summary>Restores selection IDs without writing any game state.</summary>
        IReadOnlyList<IFoundationSignalLink> ImportOwner(string ownerId, JsonValue stored);
    }
    internal sealed class FoundationSignalLinks : IFoundationSignalLinks
    {
        readonly List<Link> connections = new List<Link>();
        public IReadOnlyList<IFoundationSignalLink> Links => Array.AsReadOnly(connections.Cast<IFoundationSignalLink>().ToArray());
        public IFoundationSignalLink Connect(string ownerId, SignalSelection source, SignalSelection destination)
        {
            SignalPropertyDefinition.ValidateId(ownerId, nameof(ownerId));
            if (source == null || string.IsNullOrWhiteSpace(source.SourceId) || string.IsNullOrWhiteSpace(source.PropertyId))
                throw new ArgumentException("A source and property ID are required.", nameof(source));
            if (destination == null || string.IsNullOrWhiteSpace(destination.SourceId) || string.IsNullOrWhiteSpace(destination.PropertyId))
                throw new ArgumentException("A destination and property ID are required.", nameof(destination));
            if (source.Equals(destination)) throw new ArgumentException("A signal cannot link to itself.", nameof(destination));
            if (connections.Any(l => l.OwnerId == ownerId && l.Source.Equals(source) && l.Destination.Equals(destination)))
                throw new ArgumentException("Duplicate owned signal connection.");
            var link = new Link(this, ownerId, source, destination);
            connections.Add(link);
            try { link.Refresh(); }
            catch { link.Dispose(); throw; }
            return link;
        }
        public int RemoveOwner(string ownerId)
        {
            var targets = connections.Where(l => l.OwnerId == ownerId).ToArray();
            foreach (var link in targets) link.Dispose();
            return targets.Length;
        }
        public JsonValue ExportOwner(string ownerId)
        {
            var items = new JsonArray();
            foreach (var link in connections.Where(l => l.OwnerId == ownerId))
                items.Add(new JsonObject {
                    { "source", link.Source.SourceId }, { "property", link.Source.PropertyId },
                    { "destination", link.Destination.SourceId }, { "destinationProperty", link.Destination.PropertyId }
                });
            return items;
        }
        public IReadOnlyList<IFoundationSignalLink> ImportOwner(string ownerId, JsonValue stored)
        {
            if (!stored.IsJsonArray) throw new ArgumentException("Signal bindings must be an array.", nameof(stored));
            var imported = new List<IFoundationSignalLink>();
            try
            {
                foreach (var item in stored.AsJsonArray)
                {
                    if (!item.IsJsonObject || !item["source"].IsString || !item["property"].IsString ||
                        !item["destination"].IsString || !item["destinationProperty"].IsString)
                        throw new ArgumentException("Malformed signal binding entry.", nameof(stored));
                    imported.Add(Connect(ownerId,
                        new SignalSelection(item["source"].AsString, item["property"].AsString),
                        new SignalSelection(item["destination"].AsString, item["destinationProperty"].AsString)));
                }
                return imported.AsReadOnly();
            }
            catch { foreach (var link in imported) link.Dispose(); throw; }
        }
        void Remove(Link link) => connections.Remove(link);
        sealed class Link : IFoundationSignalLink
        {
            readonly FoundationSignalLinks registry;
            ISignalProperty input;
            ISignalProperty output;
            bool disposed;
            public string OwnerId { get; }
            public SignalSelection Source { get; }
            public SignalSelection Destination { get; }
            public bool IsResolved => !disposed && input != null && output != null;
            public string UnavailableReason { get; private set; }
            internal Link(FoundationSignalLinks registry, string ownerId, SignalSelection source, SignalSelection destination)
            {
                this.registry = registry; OwnerId = ownerId; Source = source; Destination = destination;
                FoundationApi.Signals.Changed += Refresh;
            }
            internal void Refresh()
            {
                if (disposed) return;
                if (input != null) input.Changed -= Forward;
                input = null; output = null; UnavailableReason = null;
                if (!FoundationApi.SignalDirectory.TryResolve(Source, out var candidate))
                { UnavailableReason = "Signal source unavailable: " + Source; return; }
                if (!FoundationApi.SignalDirectory.TryResolve(Destination, out var destination))
                { UnavailableReason = "Signal destination unavailable: " + Destination; return; }
                if (!destination.CanWrite)
                { UnavailableReason = "Signal destination is read-only: " + Destination; return; }
                if (candidate.ValueType != destination.ValueType)
                { UnavailableReason = "Incompatible signal types: " + candidate.ValueType.Name + " / " + destination.ValueType.Name; return; }
                input = candidate; output = destination;
                input.Changed += Forward;
            }
            void Forward(SignalPropertyChanged evt)
            {
                if (!IsResolved) return;
                try
                {
                    // The source already performed the native read. Only write if
                    // a change was observed. Never fabricate a transfer/capacity.
                    output.Write(evt.NewValue);
                }
                catch (Exception error)
                {
                    FoundationApi.Diagnostics.Write(OwnerId, FoundationDiagnosticLevel.Error,
                        "Signal binding failed: " + Source + " -> " + Destination + " / " + error.Message);
                }
            }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                FoundationApi.Signals.Changed -= Refresh;
                if (input != null) input.Changed -= Forward;
                input = output = null;
                registry.Remove(this);
            }
        }
    }
}
