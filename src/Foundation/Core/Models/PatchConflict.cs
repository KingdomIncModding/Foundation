namespace KingdomFoundation.Core
{
    public sealed class PatchConflict
    {
        public string Method { get; }
        public string Owner { get; }
        public string OtherOwner { get; }
        public string Kind { get; }
        public PatchConflict(string method, string owner, string otherOwner, string kind)
        { Method = method; Owner = owner; OtherOwner = otherOwner; Kind = kind; }
    }
}
