# Foundation-owned game integration

Foundation supplies game access, hooks, events and compatibility adapters. A mod supplies its feature behavior. For example, a box-deconstruction mod keeps drag handling, category filtering, confirmation UI and batch orchestration; Foundation supplies the native primitives it uses.

## Native-free consumer contracts

The following additive APIs use Foundation-owned or Unity/framework types:

| API | Purpose |
| --- | --- |
| `FoundationApi.GameHooks` | Disposable handlers for Foundation-owned game interception points. |
| `GameHookPoints` | Stable hook identifiers; native method names and signatures stay in Foundation. |
| `GameHookCall` | Unity view, primitive arguments, monotonic cancellation and whether native handling ran. |
| `FoundationApi.WorkspaceAccess` | Spatial queries, opaque workspace identities, native refunds, individual removal and currency grants. |
| `WorkspaceObject`, `WorkspacePort`, `WorkspacePipe` | Stable weakly cached model identities, geometry and native connection topology. |
| `FoundationApi.WorkspaceVisuals` | Native view/template access for mod-owned layouts and rendering. |
| `FoundationApi.Decorations` | Native decoration popup, style and background access. |
| `FoundationApi.ResearchAccess`, `ItemAccess`, `Kingdom` | Native-free research, item and kingdom identities. |
| `KingdomFoundation.Json` | Native-compatible JSON values, objects and arrays without a game-assembly dependency. |
| `FoundationPopup`, `FoundationTooltipSource` | Mod-owned popup/tooltip behaviour through internal native adapters. |
| `FoundationApi.DragDrop` | Adapts a mod-owned `IGameDragDropTarget` Unity behaviour to the native drag manager. |
| `FoundationApi.UiAccess` | Native UI/input state, overlay presentation, sounds, currency icons and number formatting. |
| `FoundationApi.CameraAccess` | Native camera components, zoom state, background and font-filter refresh primitives. |
| `FoundationApi.AudioInterception` | Native volume transforms and exception-safe activity scopes; channel policy and mixing remain in the mod. |
| `TechNodeOptions.Create` / `GameMoney` | Research options and currency amounts without native enum or collection dependencies. |
| `FoundationUiInputBlocker` | Native zoom blocking for mod-owned Unity UI without inheriting a game component. |

Handlers execute synchronously on the game's calling thread. Use Unity game access on the main thread. Higher subscriber priority runs first; cancellation cannot be undone by another subscriber. Dispose subscriptions when the mod unloads. Foundation reports subscriber exceptions and continues dispatching other subscribers. An after-handler may run even when native handling was suppressed; inspect `OriginalRan` when needed.

Workspace identities are opaque and weakly cached: repeated queries return the same identity, including for weak-table mod state. Keep selections local to the current workspace and recheck membership before mutation. Descriptive values are snapshots; query again to refresh them. Foundation does not choose which objects a feature should select, delete or confirm. Hook model arguments and results use these identities and Foundation JSON values; native game class names remain in the framework.

`GameWorkspace.Buildings`, `Pipes` and `Decorations` return cached live read-only views. Reading a view, its count, or an indexed identity does not copy the whole workspace. Existing views reflect additions, removals and reordering. Their enumerators follow the native list's mutation rules: call `ToArray()` explicitly before iterating when the operation will edit that same collection. The `SetBuildings`, `SetPipes` and `SetDecorations` methods materialize their input before changing the native list, so passing a view of that same workspace remains supported.

## Existing SDK compatibility

Existing native-facing services and `FoundationApi.Native` remain available. Those legacy contracts can expose native game types, so using them does not by itself isolate a consumer from game updates. Do not add native calls or Harmony targets to a migrated mod to fill a missing framework capability; add the primitive adapter or hook in Foundation instead.

The deprecated float `ExchangeTax` adapter remains available, marked with game build **1.0.13** as its last native signature. No existing public API is removed by this framework migration. See [Migration](Migration.md) for the retained deprecation register.

## Checking a migrated consumer

Build the consumer without an `Assembly-CSharp` reference, then run `Tools/Architecture/Audit-GameBoundary.ps1 -Mod <folder> -Enforce` from a source checkout. The report inspects compiled native type/member references and separately lists reflection sites requiring review. It also inventories the complete native assembly to make remaining coverage visible. Passing the dependency check establishes the assembly boundary; runtime scenarios are still required to verify behavior.

These contracts are being expanded as existing mods migrate. The legacy SDK is not yet a complete native-free abstraction of every game feature.

`ExecutionHooks` observes native calls with per-invocation state and an end callback that runs on normal return, cancellation and exceptions. Observers cannot replace native exceptions. Disposing a subscription stops new scopes; an already-started scope still receives its end callback. Native targets use stable `GameExecutionPoints` identifiers. `SubscribeExternal` supports optional integrations with other mods and rejects direct game-assembly targets.

`GameSeed` and `SeedInterception` expose native seed state and interception entry points. Hash algorithms remain in consumers. `JsonObject.CopyEntriesTo` fills a reusable consumer buffer without exposing the game's private dictionary. `GameInfo` supplies assembly metadata and native save paths.

Workspace identities also expose primitive collection, connection, buffer, upgrade and view operations. Consumers own edit history, transactions, rollback, selection and replay. `GameProductionState` exposes live production values; it does not decide whether a machine is running, starved or clogged. Those decisions and their presentation stay in the diagnostic mod.

Register custom machines with `FrameworkWorkspaceNodeDefinition` and implement `FrameworkWorkspaceNode` in the mod. Foundation creates the private native host; it forwards update, custom save/load/copy and menu callbacks to the mod's behavior. `WorkspaceObject.Behaviour` retrieves that behavior through its stable model identity. Port creation, connections and buffers are framework primitives. Routing policy, accounting, recipe behavior and the rest of the machine's feature remain in the mod. Existing `WorkspaceNode` registrations remain available for compatibility.

`Blueprints` exposes individual preview creation, binding, collision, cost-display and drag-position operations. It does not implement capture, compact codes, research-validation policy, multi-building orchestration or a library. `GameCurrency` supplies stable currency slots while Foundation translates native enums. Activity observers receive canonical operation labels, rather than discovering native methods in consumer code.
