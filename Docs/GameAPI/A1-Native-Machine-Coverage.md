# Stage A1 — Native Machine API: Full Source Coverage

**Baseline:** user-supplied `KingdomIncSrc.zip` and latest Foundation 0.10.5 complete source.
**Target:** unified Foundation 0.10.6; one production `Foundation.dll`.
**Status:** source coverage mapped and implemented; **runtime verification still required**.

## Source inventory

- **65 native source types** declare classes in `Assets.Source.Flow.Elements/`; **61 concrete FlowElement subclasses** are individually enumerated.
- Two abstract machine bases (`RecipeElement`, `ResourceElement`) and two slot/persistence helper types (`TrainingGroundsSlot`, `InnSlot`) are excluded from the concrete machine count.
- `FlowElement.GetAllTypes()` is the actual game prefab registry, and `GetBuildableTypes()` applies the current player/unlock restrictions. The presence of a C# subclass does **not** automatically mean a build menu entry exists.
- The JSON matrix alongside this document records the SHA256 of each individual game source file and the visible public declarations for audit.

## Implementation and contract

1. `FoundationMachineApi.NativeTypes.KnownTypes` strongly references every listed concrete native class. `RegisteredTypes()` consults the **live game prefab catalog**; it never invents registrations.
2. `FoundationMachineApi.Registry.All`, `.OfType<T>`, `.Inspect`, `.SnapshotAll`, and `FoundationApi.Machines.Find<T>` return real, placed game machine objects or snapshots; modded subclasses are never silently labeled as vanilla.
3. Every `MachineSnapshot` includes `NativeType`, `NativeProperties` (read-only scalar/captured values from the native class), existing `AdditionalProperties` (owned by other mods), generic IO/upgrade/recipe/production state and native identity. **No second machine simulation or authoritative storage** is created.
4. Recipes/resources use inherited native methods (`GetAvailableRecipes`, `CanCraft`, `GetAvailableResources`, `GetDesiredOutput`, `GetCraftingCost`, `GetResourceCost`), so simple subclasses do not duplicate the engine crafting system.
5. Specialized machines use native-specific views and existing Foundation native bridges. New safe, supported operations are `SetAppraiser`, `SetAccountant`, `SetWarType`, `AdvanceCollector`, `SetOutfitterTimeout`, `SetPortalChannel`, and `SetArmoryChannel`. All require an active game machine and are reported as machine capabilities where applicable.
6. The existing **A2** physical IO/throughput API remains unchanged. Foundation signals and UI panel rewrite candidates remain frozen for Stage B after Gate A.

## Machine-by-machine native coverage

| Native class | Family | Native state exposed | Native operation / game restriction |
|---|---|---|---|
| `Accountant` | Controls | CurrType, AppraiseType, CurrAmount | SetAccountant, AppraiseItem |
| `AdventureSupplies` | Resource | ResourceElement.Resource, QueuedCount, OutputPerSecond, QualityResourceCost | SetResource, StartManualCraft when CanManualCraft; use native GetResourceCost |
| `AdventurersGuild` | Resource | ResourceElement.Resource, QueuedCount, OutputPerSecond, QualityResourceCost | SetResource, StartManualCraft when CanManualCraft; use native GetResourceCost |
| `Alchemist` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Appraiser` | Controls | StatAppraised, AppraiseType, StatAmount | SetAppraiser, AppraiseItem |
| `Arena` | Adventure | Fighting, FightToTheDeath, Slot1, Slot2, TicksRemaining, CooldownTimer, RewardMultiplier, CanOutput, BlockedByWar | StartManualCraft (native DoManualCraft/StartFight when CanManualCraft) |
| `Armory` | Storage | Channel, OutputType, InputCost, MaxItems, ArmoryType | SetArmoryChannel, SetArmoryOutput |
| `Barracks` | Resource | ResourceElement.Resource, QueuedCount, OutputPerSecond, QualityResourceCost | SetResource, StartManualCraft when CanManualCraft; use native GetResourceCost |
| `Blacksmith` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Butcher` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Campfire` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Carpenter` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Collector` | Logistics | InputCount, CurrentInput, LastItem | ExpandPorts, CollapsePorts, AdvanceCollector |
| `DemoTurtle` | Internal | No own configurable property | Internal native dev class; observe only |
| `DevSpawner` | Internal | Item | Internal native dev class; observe only |
| `Disenchanter` | Commerce | CollectedMana, RecentItems | StartManualCraft / native DoCollect when ready |
| `Distributor` | Logistics | OutputCount, CurrentOutput, LastItem | ExpandPorts, CollapsePorts |
| `Dungeon` | Adventure | CurrentDungeon, ReadyToStart, TrashLoot, LimitBreak, LimitBreak2, CoinLootMultiplier | SetDungeon, StartManualCraft when ready |
| `Enchanter` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Engraver` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Farm` | Resource | ResourceElement.Resource, QueuedCount, OutputPerSecond, QualityResourceCost | SetResource, StartManualCraft when CanManualCraft; use native GetResourceCost |
| `Filter` | Logistics | FilterType | SetFilter |
| `Forest` | Resource | SelectedResource, QueuedCount, OutputPerSecond | SetForestResource, StartManualCraft when ready |
| `Forge` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Graveyard` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Guildhouse` | Storage | Inherited Armory.Channel, InputCost, MaxItems, ArmoryType; game subclass changes restrictions | Inherited SetArmoryChannel, SetArmoryOutput; native CanAcceptItem restrictions |
| `Hoard` | Storage | Inherited Armory.Channel, InputCost, MaxItems, ArmoryType; game subclass changes restrictions | Inherited SetArmoryChannel, SetArmoryOutput; native CanAcceptItem restrictions |
| `Incinerator` | Logistics | LastItem | Read-only; native disposal in Update |
| `Inn` | Adventure | Contents, EnergyPerTick, MaxQuality, TickCount, TickDelay | StartManualCraft when ready; native slots maintained by game |
| `Inspector` | Logistics | LastItem, LiveItem | Read-only inspection; native pass-through in Update |
| `Inventor` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Jeweler` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Kitchen` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Leatherworker` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Library` | Storage | Inherited Armory.Channel, InputCost, MaxItems, ArmoryType; game subclass changes restrictions | Inherited SetArmoryChannel, SetArmoryOutput; native CanAcceptItem restrictions |
| `LuxuryShop` | Commerce | RecentItems | Native Update handles sales; read-only snapshot |
| `Manifold` | Logistics | InputCount, LastItem | ExpandPorts, CollapsePorts |
| `MercenaryGuild` | Adventure | RecentItems | Native Update; no invented recruitment trigger |
| `Mine` | Resource | ResourceElement.Resource, QueuedCount, OutputPerSecond, QualityResourceCost | SetResource, StartManualCraft when CanManualCraft; use native GetResourceCost |
| `Necromancer` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `NightmareCollector` | Internal | Progress | Game-owned progression; no invented controls |
| `Outfitter` | Controls | Queued, Automated, Timeout, CurrentHuman, CurrentEquip | SetOutfitterTimeout (native field, outfitter_timeout tech; game owns slider UI) |
| `Pantry` | Storage | Inherited Armory.Channel, InputCost, MaxItems, ArmoryType; game subclass changes restrictions | Inherited SetArmoryChannel, SetArmoryOutput; native CanAcceptItem restrictions |
| `Portal` | Logistics | Channel, ChannelIndex, IsOutput, InputCost, LastItem | SetPortalDirection, SetPortalChannel |
| `QuestBoard` | Adventure | SelectedQuest, Contents, CoinLootMultiplier | SetQuest, StartManualCraft when ready |
| `Refiner` | Refinement | UseMana, LastItem, LastQuality, LastNewQuality, LastManaCost | SetRefinerMana; failure/cost via native methods |
| `RequisitionOffice` | Adventure | SelectedContract, StoredValue, ContractedItem, MaxRequisition, CollectedRequisition | SetRequisitionContract, StartManualCraft when ready |
| `Restaurant` | Commerce | CollectedGold, CollectedCrowns, RecentItems | StartManualCraft when ready |
| `Scribe` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Shop` | Commerce | CollectedGold, MaxGold, GoldCap, RecentItems | StartManualCraft when ready |
| `Shrine` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `SingleButton` | Controls | ClickCount | PressButton |
| `Stockpile` | Logistics | Capacity, Contents, FirstItem | SetStockpileCapacity |
| `SummoningCircle` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `SupplyDepot` | Resource | ResourceElement.Resource, QueuedCount, OutputPerSecond, QualityResourceCost | SetResource, StartManualCraft when CanManualCraft; use native GetResourceCost |
| `Tailor` | Recipe | RecipeElement.Recipe, Input, Crafting, CraftableTag, native recipe & quality | SetRecipe, StartManualCraft when CanManualCraft; use native GetCraftingCost |
| `Timer` | Controls | MaxTime, TimeLeft | SetTimer |
| `TrackSwitch` | Controls | SwitchPosition | SetSwitch |
| `TrainingGrounds` | Adventure | Contents, Capacity, TrainingDummy, QualityPerTick | StartManualCraft when ready |
| `Treasury` | Storage | Inherited Armory.Channel, InputCost, MaxItems, ArmoryType; game subclass changes restrictions | Inherited SetArmoryChannel, SetArmoryOutput; native CanAcceptItem restrictions |
| `WarEffort` | Adventure | WarType, TriggerAttack, Soldiers, CurrentSoldier | SetWarType (real game WarEffort selection view) |

## Verification and required runtime work

- Run `python Tests/verify_native_machine_a1.py` for a **source contract audit** against the original class inventory; optional `--native-zip` checks every native source signature and SHA256, without redistributing game files.
- Run `Tests/NativeMachines/INSTALL-A1-TEST.cmd` after extracting the full source into `_Mods`. It builds the single production DLL from the real Windows game and installs a **separate development-only verifier**.
- Load a disposable game save with a broad range of native machines (including specialized machines) and let the verifier inspect each available native class. Run `Tests/NativeMachines/MACHINE-A1-TEST.cmd` option **3** to see dynamic `A1.Type.<NativeClass>` PASS/FAIL/PENDING results.
- Check that `FlowElement.GetAllTypes` matches expected registered game types. Types absent from the game prefab catalog do **not** create false PENDING blockers. Mod-added types are reported separately.
- Check normal native UI actions (recipe changes, filters, logistics, wars, timers, selected quests, manual craft) on a disposable kingdom separately, while verifying save/load, reload, native UI refresh, ports and upgrade effects. The independent A1 test plugin **never performs destructive mutation**.
- Ensure A2 and A3–A7 regression verifiers and previous Core/UI smoke tests remain green on the new unified Foundation binary. Gate A cannot be certified by source-only tests.

## Important exclusions and classifications

- **INTERNAL:** `DemoTurtle`, `DevSpawner`, and `NightmareCollector` are real native classes. Their presence is recorded but Foundation does not claim a buildable player-facing machine or invent UI for them.
- **INHERITED NATIVE:** Leaf `RecipeElement` and `ResourceElement` subclasses share native data and behaviors and do not need invented operations per class.
- **NATIVE-BRIDGE:** `SetAppraiser`/`SetAccountant` use fields and native `WorldFlow*` views to update icons/labels; `SetWarType` uses `WorldFlowWarEffort.SetWarType`; routing operations call the existing native Foundation machine bridge.
- **READ-ONLY WHEN UNSUPPORTED:** Outfitter/Arena/Inn and other machines with internal state cannot be assigned arbitrary new controls or invented configuration commands. Native `CanManualCraft`/`DoManualCraft` are exposed where the game permits.
- **DEFERRED B4:** Custom machine panels or a new machine UI registry. Do not construct these during A1.
- **DEFERRED B1–B3:** custom framework, events, signals and persistence rewrites.

**Acceptance distinction:** “all concrete native classes source-mapped” does not imply “all classes tested in a running kingdom.” The latter is the upcoming Windows runtime verification.
