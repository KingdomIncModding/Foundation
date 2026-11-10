using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the signal property definition API.</summary>
    public sealed class SignalPropertyDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public Type ValueType { get; }
        public Func<object> Getter { get; }
        public Action<object> Setter { get; }
        public SignalPropertyDefinition(string id, string name, Type valueType, Func<object> getter, Action<object> setter = null)
        {
            ValidateId(id, nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A property needs a display name.", nameof(name));
            if (valueType == null) throw new ArgumentNullException(nameof(valueType));
            if (getter == null) throw new ArgumentNullException(nameof(getter));
            if (!IsSupportedType(valueType)) throw new ArgumentException("Unsupported property type: " + valueType.FullName, nameof(valueType));
            Id = id; Name = name; ValueType = valueType; Getter = getter; Setter = setter;
        }
        /// <summary>Determines whether supported type.</summary>
        public static bool IsSupportedType(Type type) => type == typeof(bool) || type == typeof(string) ||
            type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) ||
            type == typeof(UnityEngine.Color) || type == typeof(UnityEngine.Vector2) ||
            type == typeof(UnityEngine.Vector2Int) || type.IsEnum;
        internal static void ValidateId(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || id.Length > 192 || id.Any(char.IsControl))
                throw new ArgumentException("A stable nonempty ID up to 192 characters is required.", name);
        }
    }
}
