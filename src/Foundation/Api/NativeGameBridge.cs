using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Source.Player;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed class NativeGameBridge : INativeGameBridge
    {
        /// <summary>Gets or sets the game assembly value.</summary>
        public Assembly GameAssembly => typeof(GamePlayer).Assembly;
        public Type FindType(string fullName) => string.IsNullOrEmpty(fullName) ? null : GameAssembly.GetType(fullName, false);
        public IReadOnlyList<Type> FindTypes(string namespacePrefix)
        {
            var all = GameAssembly.GetTypes();
            if (namespacePrefix == null) return Array.AsReadOnly(all);
            return Array.AsReadOnly(all.Where(t => (t.Namespace ?? "").StartsWith(namespacePrefix, StringComparison.Ordinal)).ToArray());
        }
        /// <summary>Performs the methods operation.</summary>
        public IReadOnlyList<MethodInfo> Methods(Type type, bool includeStatic = true)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var flags = BindingFlags.Public | BindingFlags.Instance | (includeStatic ? BindingFlags.Static : (BindingFlags)0);
            return Array.AsReadOnly(type.GetMethods(flags));
        }
        public MethodInfo FindMethod(Type type, string name, params Type[] parameters)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (name == null) throw new ArgumentNullException(nameof(name));
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            return type.GetMethod(name, flags, null, parameters ?? Type.EmptyTypes, null);
        }
        /// <summary>Performs the invoke operation.</summary>
        public object Invoke(object target, MethodInfo method, params object[] arguments)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (method.DeclaringType?.Assembly != GameAssembly)
                throw new ArgumentException("Only the game's own managed methods may be invoked by this bridge.", nameof(method));
            if (!method.IsPublic) throw new ArgumentException("Only public native methods are available.", nameof(method));
            return method.Invoke(target, arguments ?? Array.Empty<object>());
        }
        static Type RequireNative(Type nativeType)
        {
            if (nativeType == null) throw new ArgumentNullException(nameof(nativeType));
            if (nativeType.Assembly != typeof(GamePlayer).Assembly)
                throw new ArgumentException("Only types from the installed game's assembly are supported.", nameof(nativeType));
            return nativeType;
        }
        const BindingFlags NativePublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        /// <summary>Performs the members operation.</summary>
        public IReadOnlyList<MemberInfo> Members(Type nativeType)
        {
            return Array.AsReadOnly(RequireNative(nativeType).GetMembers(NativePublic));
        }
        public object Construct(Type nativeType, params object[] arguments)
        {
            return Activator.CreateInstance(RequireNative(nativeType), arguments ?? Array.Empty<object>());
        }
        /// <summary>Performs the read operation.</summary>
        public object Read(object target, Type nativeType, string member)
        {
            RequireNative(nativeType);
            if (string.IsNullOrEmpty(member)) throw new ArgumentException("Member name required.", nameof(member));
            var property = nativeType.GetProperty(member, NativePublic);
            if (property != null && property.GetGetMethod() != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(property.GetGetMethod().IsStatic ? null : target, null);
            var field = nativeType.GetField(member, NativePublic);
            if (field != null) return field.GetValue(field.IsStatic ? null : target);
            throw new MissingMemberException(nativeType.FullName, member);
        }
        /// <summary>Performs the write operation.</summary>
        public void Write(object target, Type nativeType, string member, object value)
        {
            RequireNative(nativeType);
            if (string.IsNullOrEmpty(member)) throw new ArgumentException("Member name required.", nameof(member));
            var property = nativeType.GetProperty(member, NativePublic);
            if (property != null && property.GetSetMethod() != null && property.GetIndexParameters().Length == 0)
            {
                property.SetValue(property.GetSetMethod().IsStatic ? null : target, value, null);
                return;
            }
            var field = nativeType.GetField(member, NativePublic);
            if (field != null && !field.IsInitOnly && !field.IsLiteral)
            {
                field.SetValue(field.IsStatic ? null : target, value);
                return;
            }
            throw new MissingMemberException("No writable public member: " + nativeType.FullName + "." + member);
        }
        /// <summary>Saves player snapshot.</summary>
        public JsonValue SavePlayerSnapshot() => GameWorldService.RequirePlayer().ToJson();
    }
}
