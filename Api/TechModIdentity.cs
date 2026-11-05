using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace KingdomFoundation.Api
{
    internal static class TechModIdentity
    {
        internal static (string id, string name) Detect()
        {
            var foundation = typeof(FoundationApi).Assembly;
            foreach (var frame in new StackTrace().GetFrames() ?? Array.Empty<StackFrame>())
            {
                var type = frame.GetMethod()?.DeclaringType;
                if (type == null || type.Assembly == foundation || type.Assembly == typeof(object).Assembly) continue;
                for (var context = type; context != null; context = context.DeclaringType)
                {
                    var plugin = context.GetCustomAttribute<BepInPlugin>();
                    if (plugin != null) return (plugin.GUID, plugin.Name);
                }
                var loaded = Chainloader.PluginInfos.Values.Where(p => p.Instance && p.Instance.GetType().Assembly == type.Assembly).ToArray();
                if (loaded.Length == 1) return (loaded[0].Metadata.GUID, loaded[0].Metadata.Name);
                if (!type.Assembly.GetReferencedAssemblies().Any(a => a.Name == "Foundation" || a.Name == "BepInEx")) continue;
                // Awake can run before the plugin is inserted into PluginInfos.
                Type[] types;
                try { types = type.Assembly.GetTypes(); }
                catch (ReflectionTypeLoadException error) { types = error.Types.Where(t => t != null).ToArray(); }
                var declared = types.Select(t => t.GetCustomAttribute<BepInPlugin>()).Where(p => p != null).ToArray();
                if (declared.Length == 1) return (declared[0].GUID, declared[0].Name);
            }
            return (FoundationPlugin.Id, "Foundation");
        }
    }
}
