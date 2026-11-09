using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Source.Player;
using LightJson;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Escape hatch for game features without a dedicated Foundation service yet.
    /// Binds ONLY to the installed Assembly-CSharp game's real types, not guessed
    /// DTOs. Mod authors can use this to integrate an existing public native method.
    /// Reflection is synchronous; calls must be made on the Unity thread.
    /// </summary>
    public interface INativeGameBridge
    {
        Assembly GameAssembly { get; }
        Type FindType(string fullName);
        IReadOnlyList<Type> FindTypes(string namespacePrefix);
        IReadOnlyList<MethodInfo> Methods(Type type, bool includeStatic = true);
        MethodInfo FindMethod(Type type, string name, params Type[] parameters);
        object Invoke(object target, MethodInfo method, params object[] arguments);
        /// <summary>Create any game type exposing a public constructor.</summary>
        object Construct(Type nativeType, params object[] arguments);
        /// <summary>Read any public instance or static field/property on a native game type.</summary>
        object Read(object target, Type nativeType, string member);
        /// <summary>Write any public writable instance or static field/property on a native game type.</summary>
        void Write(object target, Type nativeType, string member, object value);
        IReadOnlyList<MemberInfo> Members(Type nativeType);
        JsonValue SavePlayerSnapshot();
    }
}
