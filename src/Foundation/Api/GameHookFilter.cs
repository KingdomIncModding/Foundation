namespace KingdomFoundation.Api
{
    /// <summary>Stable research identifiers used to avoid allocating invocation frames for unrelated hot-path queries.</summary>
    public sealed class GameHookFilter
    {
        public string SubjectId {get;}
        public string ArgumentId {get;}
        public GameHookFilter(string subjectId=null,string argumentId=null){SubjectId=subjectId;ArgumentId=argumentId;}
    }
}
