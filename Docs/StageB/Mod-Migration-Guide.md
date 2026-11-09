# Foundation 0.10.7 → 0.11.0: mod author migration and functional preservation

This guide accompanies the **one complete Stage B source revision**. Foundation has ONE production assembly. The B6 scope is a **compatibility-preserving rewrite** of R05/R14/R19/R23/R24. Their previous public interfaces, configuration names, data keys, panel contracts and runtime behavior must continue working while internal execution is delegated to the stable Core and Game API.

## Step 0: existing mod safety

- Back up the kingdom save and the old `Foundation.dll`; do not delete a content provider from a save as a migration strategy.
- Remove development-only verification DLLs after testing, but leave the production `BepInEx/plugins/Foundation/Foundation.dll` installed.
- Keep the old mod GUIDs, native content IDs, research IDs, workspace node IDs, signal source IDs, property IDs and save schema identifiers. **Renaming these identifiers loses references in native saves.**
- Rebuild against the target `Foundation.dll` and installed Kingdom Inc game assemblies, not a guessed stub.
- The new APIs are additive; mods using existing public entry points are not forced to change immediately. New code should prefer the owner-scoped SDK wrapper.

## Migration quick map: B2–B6

| Before | After / preferred API | Behavior guaranteed |
| --- | --- | --- |
| `FoundationApi.Assets.LoadSprite(modId, id, path)` | `using var mod = FoundationApi.ForMod(modId); mod.LoadSprite(id, path)` | Same Sprite import and Unity lifetime; context tracks token. |
| `FoundationApi.Assets.LoadAssetBundle(modId, id, path)` | `mod.LoadBundle(id, path)` | Same AssetBundle lifetime; no forced `Unload(true)` of instantiated game objects. |
| Tracking owner assets manually | `FoundationApi.AssetDirectory.ForOwner(id)` and `ReleaseOwner(id)` | Owner-scoped discovery and cleanup, no global purge. Caller must ensure resources are no longer referenced. |
| `FoundationApi.Content.RegisterItem(modId,item)` etc | `mod.RegisterItem(item)` and analogous recipe/quest/dungeon/war/prestige/enhancement methods | Same native registration call and same identifiers; intentionally permanent until process exit. |
| Need to inspect mod ownership | `FoundationApi.ContentDirectory.TryGetOwner(kind,id,out owner)` / `OwnedIds(owner)` | Read-only lookups into the original content ownership map. |
| `FoundationApi.ModSave.Register(modId,version,save,load)` | `mod.Persist(version,save,load)` or `FoundationApi.SaveMigrations.RegisterMigrating(...)` | **Same** save root and callback shape. Optional migration receives a deep clone and original is retained on error. |
| `FoundationApi.Signals.Register(definition)` | `mod.Expose(definition)` | Same source/property IDs and callback semantics, with token cleanup. |
| Directly searching `Signals.Sources` | `FoundationApi.SignalDirectory.Find(query,owner)`; `FoundationApi.SignalDirectory.FindProperties(sourceId,type,writable)` | Same underlying registry, no shadow graph; type filtering is exact. |
| Store signal source ID + property ID separately | `new SignalSelection(sourceId,propertyId)`; `SignalDirectory.TryResolve(...)` | Preserves both independent identities. |
| Ad hoc signal callback wiring | `mod.Link(sourceSelection,destinationSelection)` | Strict same-type routing, never native pipes; missing provider unresolved until re-registered. |
| Custom saved signal selection serialization | `FoundationApi.SignalLinks.ExportOwner(owner)` / `ImportOwner(owner,json)` inside the mod's existing `ModSave` state | Existing mod schema controls persistence; importing selection IDs alone performs no game state mutation. |
| Direct `ModConfig.GetOrCreateSection` | `mod.Settings(title,description)` | Same native Options Apply/Cancel semantics and same section. |
| Register `FoundationApi.Panels.Register(def)` | `mod.RegisterPanel(def)` | Same panel registry/native game theme; automatically disposes registered token on unload. |
| `FoundationApi.Events` | Keep using it, or `FoundationCoreApi.Core.Events` | Shared B1 delivery, owner cleanup and exception isolation. |
| Query `GamePlayer.Current` directly in inspector | `FoundationApi.World.Player` / `ActiveFlow` / `FoundationApi.Flow.Inspect(...)` | Native game data remains authoritative; do not store your own pipe graph. |
| Manually create parallel diagnostics logs | `FoundationApi.Diagnostics.Write(...)` / `FoundationCoreApi.Core.Diagnostics.Record(...)` | Existing Foundation diagnostic history and logger behavior preserved. |

## B6 R05 — Harmony Patch Manager (REWRITE, preserve functionality)

**Preserved:** `FoundationCoreApi.Core.Patches.PatchClass(ownerId, patchType)`, `Conflicts(ownerId)`, `ReleaseOwner(ownerId)`. The same Harmony owner prefix (`urakka.kingdominc.foundation.core.<owner>`) and scoped cleanup remain. The Foundation Core manager is the shared owner-aware patch façade; Harmony is still necessarily used under that bridge for game methods with no native hook. Third-party mods that already patch through `scope.Patches.PatchClass(...)` need no changes.

**Migrate:** When a mod implements `IFoundationModule.Initialize(IModContext context)`, call `context.Patches.PatchClass(typeof(MyNativePatch))`; Core will release on the module lifecycle. Alternatively keep a stable owner GUID with `FoundationCoreApi.Core.Patches.PatchClass(ownerGuid,typeof(MyNativePatch))` and release on teardown. Don't change the patch GUID on an upgrade. **Known caveat:** Harmony's multi-method container patching is not an atomic operation; a partial failure is logged. Only a test on a disposable kingdom can establish full runtime conflict/cleanup behavior.

## B6 R14 — ModBarContext (REWRITE, preserve functionality)

**Preserved:** `IModBarContext.Root`, `CreateText`, `CreateButton`, `CreateDropdown`, `CreateToggle`, `CreateSeparator`. The existing HUD slot remains game-owned; internal rect creation delegates to `FoundationApi.NativeUiFactory.CreateRect`, which calls the same native `TechTreeTabs.Rect` routine. Existing `IModBarElement.CreateView(IModBarContext context)` implementations continue to work and keep their button callbacks, labels, toggles, dropdown choices, native UI sounds, and cleanup. The 0.10.3–0.10.6 native themed button/toggle layout improvements are retained.

**Migrate:** Prefer `mod.Register(new ModBarText(mod.ModId, "status", "Ready"))` or implement `IModBarElement.CreateView` using the provided `IModBarContext` factories. Do not construct an independent Unity Canvas or an independent registry. For full machine configuration use `mod.RegisterPanel(...)`; keep the mod bar for short actions and indicators. Test both 1600×1000 and narrow UI layout; use the native screenshot harness for visible clipping/contrast.

## B6 R19 — Foundation Verification System (REWRITE, preserve functionality)

**Preserved:** Existing `Foundation/Tests` scripts, independent A1/A2/A3–A7/B1 tests and previous reports. They are *not* deleted. New unified Stage B one-shot smoke is at `Tests/StageB/STAGE-B-TEST.cmd`; option 2 builds/installs, 3 prints **PASS/FAIL/PENDING** after validating installed DLL version and SHA256, 4 uninstalls test plugin. No continuous `Update()` loop and no native-world mutation.

**Migrate:** Build verification mods as standalone development-only BepInEx plugins referencing the installed unified `Foundation.dll`. Do not package these test DLLs in ordinary mod distributions or keep more than one suite installed simultaneously. When a test requires a real world operation (save/load, other mod, Harmony patch conflict or UI interaction), report PENDING until exercised; never mark it PASS based on a type existence check.

## B6 R23 — Missing Research Recovery (REWRITE, preserve functionality)

**Preserved save keys:** native `Player.Tech` and `Player.NewTechCount`, including unknown/missing content; existing `PrepareLoad`/`RestoreInputOnFailure`/`Loaded`/`Extend` native save hooks; missing IDs stay in saves for restoration when the original content mod returns. No fake TechNode is created or unlocked. On load failure, original unrecognized notifications are restored to in-memory input JSON. Do **not** delete missing tech IDs from a player's save to silence a warning.

**Migrate:** Mod authors should use `FoundationApi.Progression.FindTech(id)` to test whether a native/registered tech currently exists, and `FoundationApi.ResearchRecovery.RetainedTechIds` / `.RetainedNotificationIds` for **read-only** diagnostics. Register mod-owned tech during plugin Awake before loading a kingdom, with *exactly the same IDs*. Keep the source code's native pre-deserialization shim because `GamePlayer.FromJson` does not tolerate null unknown dictionary keys; there is no native alternative to preserving this data. Exercise a saved kingdom with temporarily removed content in a disposable copy.

## B6 R24 — Workspace Node Registry (REWRITE, preserve functionality)

**Preserved:** `FoundationApi.WorkspaceNodes`, `RegisterWorkspaceNode`, `TryGet`, `Nodes`, `Changed`, each `IWorkspaceNodeType` identity/factory/preview, native placement/persistence, research prerequisites, icon handling, duplicate native type rejection and disposal tokens. The registry remains the only mod-owned workspace type catalog, not a second native FlowElement graph. Registration now resolves native prerequisite technology and sprites through `FoundationApi.Progression` and `FoundationApi.Presentation`. These preserve the **same validation contract**: missing tech or icon IDs are rejected, duplicate mod IDs cannot silently replace native types, and preview factory failures roll back registration.

**Migrate:** Continue to register `WorkspaceNodeDefinition` in plugin Awake. `using var token = FoundationApi.RegisterWorkspaceNode(definition)` remains supported; alternatively `mod.Register(definition)` tracks it. Keep `Id`, `RequiredTechId`, `IconName` and generated native `FlowElement` type constant between versions. Do not remove a provider while one of its buildings is present in any loaded kingdom without a verified recovery/migration path; the game persists native IDs and references, not C# factory closures.

## B2 mod saves: safe schema evolution example

```csharp
// Existing mod GUID stays identical across versions.
var saved = FoundationApi.SaveMigrations.RegisterMigrating(
    "example.kingdom.mod", 3,
    () => new LightJson.JsonObject { { "score", currentScore } },
    (schema, json) => {
        currentScore = schema == 0 ? 0 : json["score"].AsInteger;
    },
    (oldSchema, raw) => {
        // Only receives older stored data. It is a deep copy.
        // Transform the old payload into the version-3 shape.
        if (oldSchema == 1) return new LightJson.JsonObject { { "score", raw["points"].AsInteger } };
        return raw;
    });
// Dispose `saved` in OnDestroy; do NOT rename the mod GUID.
```

### Signal persistence example

```csharp
// In the mod's existing save callback, include:
var signalLinks = FoundationApi.SignalLinks.ExportOwner("example.kingdom.mod");
// Persist the JsonValue under a mod-specific key in FoundationApi.ModSave.
// During load: FoundationApi.SignalLinks.ImportOwner("example.kingdom.mod", savedLinks);
// This restores wiring only. It does NOT write the property's value on import.
```

### ABI and behavior warnings

- The five original interfaces `IFoundationAsset`, `IFoundationAssets`, `IModSaveRegistry`, `IGameContentRegistry`, and `ISignalRegistry` are **unchanged**. New operations are exposed through optional derived interfaces, so old consumer mods and third-party interface implementers retain the old ABI.
- `FoundationApi.ForMod(modId)` is an owner-scoped convenience handle; disposing it releases tokens created **by that context**, then performs the existing legacy source/event/config owner cleanup. Do not mix independent `ForMod` lifetimes with the same ID unless you intend that shared owner cleanup.
- The native game has **no safe general-purpose hot-unregister for static content**. This release does not invent one.
- Native pipe connections and throughput continue through `FoundationApi.NativeIO`/`FoundationApi.IO`. Foundation Signals are a separate custom system.
- Visual/theme and input compatibility must be confirmed in the actual game after every UI control rewrite. The static tests are not a substitute for screenshots.

## Migration test checklist for every dependent mod

1. Install only the new production Foundation DLL, **no old verification DLLs**.
2. Load the mod with its original GUID, before opening a save; check BepInEx startup log for missing methods, version rejection and owner collision warnings.
3. Create a **copy** of a kingdom saved with the previous Foundation version and the mod installed. Confirm all custom items, recipes, technologies, buildings and saved per-mod configuration resolve under the original identifiers.
4. Uninstall the mod from the **copied** kingdom, save and load, then reinstall it: missing mod save and missing tech data must survive, never be overwritten.
5. Verify mod-owned assets are no longer needed before disposing; no UI texture becomes pink/missing in a still-visible panel.
6. Exercise native UI interactions (mouse, keyboard, Apply/Cancel, tooltips), then verify layouts at multiple UI scales.
7. Exercise module dependency removal and recovery, shared services, and cross-mod events with at least **two** real BepInEx mods; confirm cleanup and no duplicate handler invocation.
8. Verify a placed custom native `FlowElement` can save/reload after the workspace registry rewrite, preserving its identity and connections.
9. Run `Tests/StageB/STAGE-B-TEST.cmd`, archive the resulting JSON + installed Foundation SHA256, remove the test plugin (option 4), and confirm FPS returns to normal.
10. A PASS is valid only for the specific operation actually exercised. PENDING cases remain release blockers for 100% signoff.


## IMPORTANT: B1 interface ABI change (Gate B audit)

The Gate A frozen `KingdomFoundation.Core.IFoundationCore` contract did not declare
`ICoreModDirectory Mods` or `ICoreServiceDirectory ServiceDirectory`.
Stage B1 added both **required properties**. Although no old member was deleted,
this can break a third-party mod that **implements** `IFoundationCore` itself.
Existing mods that only obtain `FoundationCoreApi.Core` and **consume** the
framework interface are not the same case. Do not assume binary compatibility
without rebuilding and launching actual consumer DLLs.

* For interface implementers, implement both new properties by delegating to
  the authoritative Core directories. Never add a second independent registry.
* For consumer-only mods, continue to resolve Core services through
  `FoundationCoreApi.Core`; prefer provider/owner scoped API operations.
* Preserve BepInEx GUID, game content IDs, research IDs, workspace-node IDs,
  signal source/property IDs and mod save keys. Test a **save/reload** scenario
  in a copied kingdom, including missing mod content restoration.
* This risk must be addressed before a strict Foundation 1.0 binary-compatibility
  promise. See `Docs/GateB/Gate-B-Compatibility-And-Decision-Record.md`.

**The five B6 decisions are REWRITE, not REMOVE.** There is no planned removal of
R05 Harmony Patch Manager, R14 ModBarContext, R19 Verification, R23 Missing
Research Recovery or R24 Workspace Node Registry. Their current contracts must
keep working while native-backed execution replaces private gameplay access.
