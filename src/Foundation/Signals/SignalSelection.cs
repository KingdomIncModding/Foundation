using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the signal selection API.</summary>
    public sealed class SignalSelection : IEquatable<SignalSelection>
    {
        public string SourceId { get; }
        public string PropertyId { get; }
        public SignalSelection(string sourceId, string propertyId) { SourceId = sourceId; PropertyId = propertyId; }
        public bool Equals(SignalSelection other) => other != null && SourceId == other.SourceId && PropertyId == other.PropertyId;
        public override bool Equals(object obj) => Equals(obj as SignalSelection);
        public override int GetHashCode() => (SourceId ?? "").GetHashCode() ^ (PropertyId ?? "").GetHashCode();
        /// <summary>Performs the to string operation.</summary>
        public override string ToString() => (SourceId ?? "(none)") + "/" + (PropertyId ?? "(none)");
    }
}
