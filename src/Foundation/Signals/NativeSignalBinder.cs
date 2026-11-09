using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KingdomFoundation.Api
{
    internal sealed class NativeSignalBinder : INativeSignalBinder
    {
        public ISignalSource Expose(string modId, string id, string name, Type nativeType,
            Func<object> resolveTarget, IEnumerable<string> publicMembers, bool writable = false)
        {
            if (nativeType == null) throw new ArgumentNullException(nameof(nativeType));
            if (resolveTarget == null) throw new ArgumentNullException(nameof(resolveTarget));
            if (publicMembers == null) throw new ArgumentNullException(nameof(publicMembers));
            if (nativeType.Assembly != typeof(Assets.Source.Player.GamePlayer).Assembly)
                throw new ArgumentException("This binder is reserved for native game types.", nameof(nativeType));
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var properties = new List<SignalPropertyDefinition>();
            foreach (var memberName in publicMembers.Distinct(StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(memberName)) throw new ArgumentException("Invalid member name.");
                var prop = nativeType.GetProperty(memberName, flags);
                if (prop != null && prop.GetIndexParameters().Length == 0 && prop.GetGetMethod() != null)
                {
                    if (!SignalPropertyDefinition.IsSupportedType(prop.PropertyType)) continue;
                    Func<object> getter = () => prop.GetValue(prop.GetGetMethod().IsStatic ? null : RequireTarget(resolveTarget, nativeType), null);
                    Action<object> setter = writable && prop.GetSetMethod() != null
                        ? new Action<object>(value => prop.SetValue(prop.GetSetMethod().IsStatic ? null : RequireTarget(resolveTarget, nativeType), value, null))
                        : null;
                    properties.Add(new SignalPropertyDefinition(memberName, memberName, prop.PropertyType, getter, setter));
                    continue;
                }
                var field = nativeType.GetField(memberName, flags);
                if (field == null) throw new ArgumentException("No public field or readable property: " + memberName);
                if (!SignalPropertyDefinition.IsSupportedType(field.FieldType)) continue;
                Func<object> reader = () => field.GetValue(field.IsStatic ? null : RequireTarget(resolveTarget, nativeType));
                Action<object> writer = writable && !field.IsInitOnly && !field.IsLiteral
                    ? new Action<object>(value => field.SetValue(field.IsStatic ? null : RequireTarget(resolveTarget, nativeType), value))
                    : null;
                properties.Add(new SignalPropertyDefinition(memberName, memberName, field.FieldType, reader, writer));
            }
            if (properties.Count == 0) throw new ArgumentException("No supported native properties were selected.");
            return FoundationApi.Signals.Register(new SignalSourceDefinition(modId, id, name, properties));
        }
        static object RequireTarget(Func<object> resolve, Type type)
        {
            var target = resolve();
            if (target == null || !type.IsInstanceOfType(target))
                throw new InvalidOperationException("The native source is unavailable: " + type.FullName);
            return target;
        }
    }
}
