namespace KingdomFoundation.Api
{
    public interface IGameResearchAccess
    {
        GameResearchNode Find(string id);
        System.Collections.Generic.IReadOnlyList<GameResearchNode> Nodes {get;}
    }
}
