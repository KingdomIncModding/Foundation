namespace KingdomFoundation.Api
{
    /// <summary>Opaque native identity for hook arguments that have no specialized Foundation adapter yet.</summary>
    public sealed class GameOpaqueValue
    {
        internal object Native { get; }
        internal GameOpaqueValue(object native) { Native=native; }
        public override bool Equals(object obj)=>obj is GameOpaqueValue other && ReferenceEquals(Native,other.Native);
        public override int GetHashCode()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Native);
    }
}
