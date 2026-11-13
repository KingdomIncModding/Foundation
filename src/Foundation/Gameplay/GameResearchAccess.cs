namespace KingdomFoundation.Api
{
    internal sealed class GameResearchAccess : IGameResearchAccess
    {
        public GameResearchNode Find(string id)=>GameResearchNode.Wrap(FoundationApi.Progression.FindTech(id));
        public System.Collections.Generic.IReadOnlyList<GameResearchNode> Nodes=>System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(Assets.Source.Player.TechNode.Nodes,GameResearchNode.Wrap));
    }
}
