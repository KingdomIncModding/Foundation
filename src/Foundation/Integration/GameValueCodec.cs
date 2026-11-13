using Assets.Source.Flow;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal static class GameValueCodec
    {
        internal static object Wrap(object value)
        {
            if (value==null || value is Component) return value;
            if(value is Assets.Source.Player.Currency currency)return (GameCurrency)System.Enum.Parse(typeof(GameCurrency),currency.ToString());
            if(value is Assets.Source.Adventure.WarType war)return GameWar.Wrap(war);
            if(value is Assets.Source.Adventure.WarProgress progress)return GameWarProgress.Wrap(progress);
            if (value is FlowDiagram workspace) return GameWorkspace.Wrap(workspace);
            if (value is SeedGenerator seed) return GameSeed.Wrap(seed);
            if (value is FlowElement element) return GameWorkspaceAccess.IdentityFor(element);
            if (value is FlowDeco decoration) return GameWorkspaceAccess.IdentityFor(decoration);
            if (value is Assets.Source.Player.TechNode research) return GameResearchNode.Wrap(research);
            if (value is Assets.Source.Player.GamePlayer kingdom) return GameKingdom.Wrap(kingdom);
            if (value is Assets.Source.Items.ItemType itemType) return GameItemType.Wrap(itemType);
            if (value is Assets.Source.Items.InventoryItem inventoryItem) return GameInventoryItem.Wrap(inventoryItem);
            if (value is Assets.Source.Player.CurrencyCollection money) return new GameMoney(money);
            if (value is FlowIO port) return WorkspacePort.Wrap(port);
            if (value is FlowPipe pipe) return WorkspacePipe.Wrap(pipe);
            if (value is LightJson.JsonValue json) return new KingdomFoundation.Json.JsonValue(json);
            if (value is LightJson.JsonObject obj) return KingdomFoundation.Json.JsonObject.Wrap(obj);
            if (value is LightJson.JsonArray array) return KingdomFoundation.Json.JsonArray.Wrap(array);
            if (value.GetType().Assembly==typeof(FlowElement).Assembly) return new GameOpaqueValue(value);
            return value;
        }
        internal static object Unwrap(object value)
        {
            if(value is GameCurrency currency)return GameCurrencyMap.Resolve(currency.ToString());
            if(value is GameWar war)return war.Native;
            if(value is GameWarProgress progress)return progress.Native;
            if (value is GameWorkspace workspace) return workspace.Native;
            if (value is GameSeed seed) return seed.Native;
            if (value is WorkspaceObject item) return item.Native;
            if (value is GameResearchNode research) return research.Native;
            if (value is GameKingdom kingdom) return kingdom.Native;
            if (value is GameItemType itemType) return itemType.Native;
            if (value is GameInventoryItem inventoryItem) return inventoryItem.Native;
            if (value is GameMoney money) return money.Native;
            if (value is WorkspacePort port) return port.Native;
            if (value is WorkspacePipe pipe) return pipe.Native;
            if (value is KingdomFoundation.Json.JsonValue json) return json.Native;
            if (value is KingdomFoundation.Json.JsonObject obj) return obj.Native;
            if (value is KingdomFoundation.Json.JsonArray array) return array.Native;
            if (value is GameOpaqueValue opaque) return opaque.Native;
            return value;
        }
    }
}
