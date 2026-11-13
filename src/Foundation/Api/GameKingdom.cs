using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    /// <summary>Opaque kingdom identity usable while a native load is still returning its player object.</summary>
    public sealed class GameKingdom
    {
        private static readonly ConditionalWeakTable<GamePlayer,GameKingdom> Cache=new ConditionalWeakTable<GamePlayer,GameKingdom>();
        internal GamePlayer Native {get;}
        private GameKingdom(GamePlayer native){Native=native;}
        internal static GameKingdom Wrap(GamePlayer native)=>native==null ? null : Cache.GetValue(native,value=>new GameKingdom(value));
        public IReadOnlyList<WorkspaceObject> Buildings=>Array.AsReadOnly(Native.Flows.SelectMany(flow=>flow.Elements).Select(GameWorkspaceAccess.IdentityFor).ToArray());
        public IReadOnlyList<GameWorkspace> Workspaces=>Array.AsReadOnly(Native.Flows.Select(GameWorkspace.Wrap).ToArray());
        public static int CurrencyCount=>Enum.GetValues(typeof(GameCurrency)).Length;
        public int CurrentWorkspace=>Native.CurrentFlow;
        public GameWorkspace ActiveWorkspace=>GameWorkspace.Wrap(Native.ActiveFlow);
        public System.Collections.Generic.IReadOnlyList<string> AvailableUpgrades(string buildingType)=>Native.GetAvailableFlowUpgrades(buildingType).ToArray();
        public int UpgradeSlotCount(string buildingType)=>Native.GetUpgradeSlotCount(buildingType);
        public long GetCurrency(int index) => Native.GetCurrency(GameCurrencyMap.Resolve(((GameCurrency)index).ToString()));
        public void AddCurrency(int index,long delta,bool countAsIncome=false) => Native.AddCurrency(GameCurrencyMap.Resolve(((GameCurrency)index).ToString()),delta,countAsIncome);
        public bool HasResearch(GameResearchNode research)=>research!=null && Native.HasTech(research.Native);
        public bool CanAfford(GameMoney price)=>price!=null && Native.CanAfford(price.Native);
        public void UnlockResearch(GameResearchNode research,bool notify=true)
        {if(research==null)throw new ArgumentNullException(nameof(research));Native.AddTech(research.Native,notify);}
    }
}
