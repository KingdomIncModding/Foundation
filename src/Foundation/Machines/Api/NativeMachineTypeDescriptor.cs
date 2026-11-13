using System;
using System.Collections.Generic;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;

namespace KingdomFoundation.Machines
{
    /// <summary>A documented native FlowElement subclass from the supplied Kingdom Inc game source.
    /// The presence of a C# type does not imply it is unlocked, buildable or registered in FlowPrefabSet.
    /// These values do not allocate or simulate game machines.</summary>
    public sealed class NativeMachineTypeDescriptor
    {
        public Type NativeType { get; }
        public string Family { get; }
        public string SourcePath { get; }
        public string NativeBaseType { get; }

        internal NativeMachineTypeDescriptor(Type type, string family, string nativeBase)
        {
            NativeType = type;
            Family = family;
            SourcePath = "Assets.Source.Flow.Elements/" + type.Name + ".cs";
            NativeBaseType = nativeBase;
        }
    }
}
