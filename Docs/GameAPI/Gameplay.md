# Gameplay Services

Foundation exposes the existing Kingdom Inc rules and objects through named services.

| Service | Game functionality |
| --- | --- |
| `FoundationApi.Items` | Items, equipment and inventory |
| `FoundationApi.Production` | Crafting, production and resources |
| `FoundationApi.Economy` | Currencies and economy |
| `FoundationApi.Progression` | Research and game progression |
| `FoundationApi.Adventures` | Quests, dungeons, wars and characters |
| `FoundationApi.Combat` | Combatants and combat |
| `FoundationApi.World` | Current world and workspaces |
| `FoundationApi.Workspace` | Workspace editing |
| `FoundationApi.Saves` | Native game save integration |
| `FoundationApi.Presentation`, `View`, `Audio` | Assets, camera, input and sound |

The game keeps control of actual crafting, purchases, combat and progression state. Prefer API methods that delegate to the game's own logic. Treat snapshots as views of native state, not independent writable models.

Use [Content and Persistence](../Content-And-Persistence.md) to register mod-created definitions.

For game 1.0.14, read exchange tax with `FoundationApi.Economy.ExchangeTaxDouble(currency)` to preserve the native `double` result. The float-returning `ExchangeTax(currency)` is a deprecated compatibility adapter; see [Migration](../Migration.md).
