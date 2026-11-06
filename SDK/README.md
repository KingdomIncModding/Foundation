# Foundation SDK 0.6.0

Create real Kingdom Inc research through public C# interfaces. Foundation puts registered research on **Tech Tree → Mods**, using the game's actual TechNode model, UITechNode/UIElementTechNode prefabs, tooltips, costs, hover/glow/checkmark feedback, prerequisite connectors, branch navigation, research list and map camera controls.

Native red **Requires …** labels are clickable. Requirement IDs supplied through `TechNodeOptions.Requirements` automatically link to their actual research models; no extra handler is needed. Clicking opens the correct tab, mod category and branch, centers the destination and does not purchase it. Root technologies at `(0, 0)` are supported.

Version 0.5.0 adds a left **Mod List** with one research map per registering mod and automatic overlap prevention. Existing 0.4.x consumers retain binary compatibility and receive categories without changing their calls.

## Reference Foundation

Install the player package, then reference `lib/Foundation.dll`, BepInEx, UnityEngine/CoreModule and the game's Assembly-CSharp.dll. Keep Foundation.xml beside the DLL for IntelliSense. Target .NET Standard 2.1. Set assembly references to `Private=false` and distribute only your own plugin, with Foundation as a separate requirement.

```csharp
[BepInDependency("urakka.kingdominc.foundation", "0.6.0")]
```

## Create native research

```csharp
using Assets.Source.Player;
using KingdomFoundation.Api;
using UnityEngine;

ITechNode technology;

void Awake()
{
    technology = FoundationApi.CreateTechNode(new TechNodeDefinition(
        id: "your.mod.guid.technology",
        title: "Example Technology",
        position: new Vector2(-300, 180),
        description: "Research this technology to unlock a feature.",
        research: new TechNodeOptions(
            fixedCost: new CurrencyCollection(Currency.Gold, 100),
            iconName: "Icons_17")));

    technology.Clicked += OnResearchUnlocked;
}

void OnResearchUnlocked(ITechNode node)
{
    // The game has successfully unlocked the research at this point.
    Logger.LogInfo("Unlocked: " + node.Id);
}

void OnDestroy() => technology?.Dispose();
```

`ITechNodeRegistry registry = FoundationApi.TechNodes` exposes the same shared service. The player package adds no demonstration research; the separate example plugin is optional.

Register and modify on Unity's main thread. Registration in Awake is supported: Foundation invokes the game's idempotent translation initializer before native tech constructors need it. Early registration happens before player research storage is allocated. Later registrations safely expand the current player's research flags while preserving existing unlocks.

## Contracts

| Contract | Purpose |
| --- | --- |
| ITechNodeDefinition | Id, Title, Description, Icon and Position |
| INativeTechNodeDefinition | Optional native Research settings |
| TechNodeDefinition | Immutable implementation; custom interface definitions are also accepted and snapshotted |
| TechNodeOptions | Native type, cost rules, parent/previous node, requirements and default game icon |
| ITechNodeRegistry | CreateTechNode, TryGet, current registration snapshots and Changed notification |
| ITechNode | Definition, Id, IsRegistered, Update, Dispose and successful-unlock callback Clicked |
| INativeTechNode | NativeNode, the real Assets.Source.Player.TechNode for integration with game systems |

`TechNodeRegistry` can also be instantiated as an isolated in-memory registry for testing. The registry returned by FoundationApi is the one connected to the game.

## Native placement and appearance

Positions are absolute native research-map coordinates: **X right, Y up**, rounded to integer units. A negative Y places a node below its parent. Coordinates are bounded to one million units per axis. The native prefab controls sizes, text, graphics, layout, hover, tooltip and status feedback; Foundation does not draw substitute cards or its own scrolling surface.

Root-level FlowElement definitions use the game's full building-research prefab. Child research uses the appropriate native mini-element or generic upgrade prefab. Different NodeType values can require associated native game data; FlowElement and Utility cover the standalone root/upgrade example. Quest, Dungeon and Recipe categories require the corresponding game definitions supplied by your plugin.

The default icon is the native `Icons_17`. Choose another native IconName or pass a caller-owned Sprite through the definition's `icon` argument. Keep custom sprites alive until you dispose/update the registration. Foundation does not distribute or destroy your assets.

Native mouse-wheel zoom, map dragging and keyboard panning work on both tabs, including Better Zoom. Each tab remembers its camera position, zoom and selected branch. The original game's saved camera fields remain associated with Base Game. Escape/right click and research-list focus use the game's normal behavior: back out of a branch, then close the map.

## Costs, prerequisites and branches

Omit `fixedCost` to use TechNode.GetCost and the native formula, cost tier and prestige discounts. Defaults are FlowElement, tier 1 and multipliers of 1. Use an empty CurrencyCollection for free research. Explicit fixed costs remain fixed when the game resets cost caches. Negative costs and invalid multipliers/tiers are rejected.

```csharp
ITechNode upgrade = FoundationApi.CreateTechNode(new TechNodeDefinition(
    "your.mod.guid.upgrade", "Example Upgrade", new Vector2(0, -160),
    "A native upgrade inside the technology's branch.",
    research: new TechNodeOptions(
        nodeType: TechNodeType.Utility,
        rootId: technology.Id,
        previousId: technology.Id,
        fixedCost: new CurrencyCollection(Currency.Gold, 25))));
```

Register parents/previous nodes first. RootId and PreviousId must refer to active Foundation registrations. `requirements` can additionally reference existing native research IDs, including Base Game technology. Unknown/self-referencing links and cyclic parent/previous links are rejected. RootId creates the native branch; PreviousId determines its prerequisite and connector. Research nodes unlock through the game's SpendCurrency/AddTech flow. Clicking already researched root technology opens its branch; it does not fire an arbitrary action on every click.

## Update and cleanup

```csharp
var current = technology.Definition;
var rules = ((INativeTechNodeDefinition)current).Research;
technology.Update(new TechNodeDefinition(
    technology.Id, "Updated Name", current.Position,
    "Updated description.", current.Icon, rules));
```

Updates keep the same native model/ordinal and preserve purchased progress. Changes refresh the actual game tree by the next frame. Preserve Research settings when changing presentation, as in the example. The identifier cannot change. Duplicate IDs, including existing Base Game IDs, are rejected without replacing the existing node.

Dispose registrations from your plugin's OnDestroy. Disposal is idempotent and removes/hides only the caller's active registration. The native model remains as an inactive entry to preserve ordinal-based player storage and purchased IDs; re-registering the same ID reuses it. Children of a disposed parent are hidden until their parent is available again. Disposing an old handle cannot remove a replacement registration. Updating a disposed handle throws ObjectDisposedException. Callback exceptions are logged and isolated from other subscribers.

Purchased research uses the game's existing Tech ID save list. Recreate registrations with the same IDs every launch before a kingdom is loaded to restore progress. No external progress files or custom save schema are introduced. Branch notification counters stay session-only so their missing dictionary keys cannot obstruct vanilla save loading after a mod is removed. The game skips unknown researched IDs when loading without their defining mod; saving again without that mod can discard those IDs. Removing Foundation also prevents dependent plugins from loading.

## Example and compatibility

The SDK includes a separate BepInEx plugin with two root technologies, a native upgrade branch, and a workspace relay unlocked by the first technology. Extract into `<KingdomInc>/_Mods/FoundationSDK`, then build:

```powershell
dotnet build .\Examples\ExampleTechNodes\ExampleTechNodes.csproj -c Release
```

For another location, pass the real game directory with a trailing slash:

```powershell
dotnet build .\Examples\ExampleTechNodes\ExampleTechNodes.csproj -c Release "-p:GameRoot=G:/Program Files (x86)/Steam/steamapps/common/KingdomInc/"
```

Close the game, copy ExampleTechNodes.dll to BepInEx/plugins/FoundationExample, launch, and open Tech Tree → Mods. Research Example Technology for 100 gold; click it again to enter its 25-gold upgrade branch. Open the native build menu, select Utility, and place Example Relay for 20 gold. Before removing the example plugin, remove its placed relay nodes from any kingdom you intend to keep playing. Its research purchases are normal saved research, so use a disposable kingdom if you only want to test it.

This API is pre-1.0. **Rebuild 0.2.0 consumers against 0.5.0**: positions now use the game's Y-up convention, the constructor supports native research options, and Clicked now means successful native unlock. Node rendering and interactions are entirely native. Tested: Kingdom Inc Release v1.0.12 / BepInEx 5.4.23.6.

## Register a workspace node

The native workspace API was added in 0.4.0 alongside the research API. A registered building type appears in the game's normal build menu after its RequiredTechId is purchased. Foundation uses the game's generic WorldFlowElement prefab, label/drag handles, native ports, collision checks, blueprint placement, copy/refund behavior, pipes and save serialization. No separate workspace overlay is created.

| Contract | Responsibility |
| --- | --- |
| IWorkspaceNodeDefinition | Stable type ID, title, description, factory, research requirement, category, size, icon and purchase cost |
| WorkspaceNodeDefinition | Immutable validated snapshot of this metadata |
| IWorkspaceNodeRegistry | RegisterWorkspaceNode, TryGet, registration snapshots and Changed notification |
| IWorkspaceNodeType | Registration handle: CreateInstance, BeginPlacement and Dispose |
| WorkspaceNode | Abstract native FlowElement base for mod-owned behavior and save/copy hooks |

Register the technology first, then register the building type in the same dependent plugin:

```csharp
IWorkspaceNodeType relayType;

void RegisterRelay()
{
    relayType = FoundationApi.RegisterWorkspaceNode(new WorkspaceNodeDefinition(
        MyRelay.TypeId, "My Relay", () => new MyRelay(),
        requiredTechId: "your.mod.guid.technology",
        size: new Vector2Int(256, 128),
        description: "Passes items from input to output.",
        category: ElementCategory.Utility,
        fixedCost: new CurrencyCollection(Currency.Gold, 20)));
}
```

`FoundationApi.WorkspaceNodes` exposes the same service through IWorkspaceNodeRegistry. Required research can be a native game technology or one created through Foundation. Unknown/inactive research, duplicate IDs, native building ID collisions, invalid sizes, invalid costs and invalid factories are rejected. Sizes default to 256 × 128 and must stay between 64 and 16384 units per axis. Native building category, labels and graphics are applied by the game.

Implement behavior using the game's existing model types:

```csharp
using Assets.Source.Flow;
using KingdomFoundation.Api;
using LightJson;

public sealed class MyRelay : WorkspaceNode
{
    public const string TypeId = "your.mod.guid.relay";
    public int Transfers;

    public MyRelay() : base(TypeId)
    {
        Inputs.Add(new FlowInput(this, 0) {
            Position = -40, BufferSize = 1, Filter = item => true
        });
        Outputs.Add(new FlowOutput(this, 0) {
            Position = 40, BufferSize = 1
        });
    }

    public override void Update(float delta)
    {
        if (!Inputs[0].HasBuffer || !Outputs[0].CanBuffer) return;
        Outputs[0].BufferedItems.Add(Inputs[0].ConsumeItem());
        Transfers++;
    }

    protected override void DataToJson(JsonObject data)
        => data["MyTransfers"] = Transfers;
    protected override void DataFromJson(JsonObject data, FlowDiagram parent)
        => Transfers = data["MyTransfers"];
    protected override void CopySettingsTo(FlowElement copy)
        => ((MyRelay)copy).Transfers = Transfers;
}
```

The factory is called after the definition is registered; the base constructor looks up its immutable metadata. It must return a fresh unplaced model of one stable C# type, with the same ID and size, for every call. Foundation validates native port parent/index, nonnegative buffer capacity and port offsets within the node height. Ports use the game's signed Position convention: negative means left and positive means right, with absolute value measuring distance down from the top. Use stable port order/indices across versions. The native pipe system enforces filters, capacity and backpressure.

WorkspaceNode defaults to no manual craft button and no upgrade slots. Override ordinary FlowElement hooks when your node needs them. Your plugin owns processing behavior, settings and custom state; Foundation does not invent recipes or production logic. Omitting FixedCost uses the native category/count-based building cost formula and the required technology's currency tier. FixedCost is copied on access; normal native refund calculations still apply.

## Place, copy and persist workspace nodes

Players can buy the node in its native build category or search for its name/description. `relayType.BeginPlacement()` opens the same placement ghost from your own UI, after checking that a kingdom is open, research is unlocked and the player can afford it. Currency is charged only by native placement on commit. Native snapping, collision rejection, cancellation, label dragging, mirroring, native copying and deletion retain their usual behavior.

`relayType.CreateInstance()` creates an unplaced model for programmatic use; it does not add a node to a kingdom or charge currency. Prefer BeginPlacement or the normal build menu for player placement. Advanced integrations may use the model with the game's own APIs.

The native serializer stores your stable ElementType, GUID, position, inputs, outputs, buffered items, pipe endpoints/bends and upgrade data. Implement DataToJson/DataFromJson for your additional fields and CopySettingsTo for the settings that native copies should carry. Native copies get fresh GUIDs and do not copy inventories. Register the same type ID before loading saved kingdoms. Support older custom fields and port schemas in your own load hook when changing versions.

Dispose the registration in your plugin's OnDestroy, before disposing its required research. Disposal removes new purchases from the palette and refreshes an open build menu. Existing placed nodes stay intact, and the factory remains available for native save/copy operations during the current process. Registration snapshots remain stable, disposal is idempotent, and an old disposed handle cannot remove a replacement. Re-registering an ID can change the factory for future loads, so retain save/port compatibility when doing so.

**Remove placed custom workspace nodes before uninstalling their defining plugin or Foundation.** Unlike missing research IDs, the vanilla loader cannot construct an unknown saved FlowElement type. Loading a kingdom containing those nodes without its factory is unsupported. The framework does not silently delete or convert player nodes.

The optional SDK example exercises actual relay instances, transfers and custom state. The shipped player package alone creates no sample research or workspace nodes. Rebuild the example against SDK 0.5.0 to try this API.

## Research ownership and the Mod List

When your BepInEx plugin registers a technology, Foundation captures its plugin GUID and display name. All technologies owned by that GUID appear in one left-hand category. The category filters both the native map and native research list; it retains its camera position, zoom and selected branch while changing categories or tabs. Categories appear and disappear as registrations are created and disposed. This navigation state belongs to the current game UI scene and does not add save data.

The original TechNodeDefinition constructor and ITechNodeRegistry interface are unchanged. Automatic detection supports registration through plugin methods, nested callbacks and helper classes in a plugin assembly. For shared helpers, plugins sharing an assembly, or other ambiguous call contexts, provide explicit ownership with the new constructor overload:

```csharp
ITechNode tech = FoundationApi.CreateTechNode(new TechNodeDefinition(
    id: "your.mod.guid.technology",
    title: "Example Technology",
    modId: "your.mod.guid",
    modName: "Your Mod",
    position: new Vector2(0, 0),
    research: new TechNodeOptions(fixedCost: new CurrencyCollection(Currency.Gold, 100))));

ITechMod owner = ((IModTechNode)tech).Mod;
IReadOnlyList<ITechMod> categories = FoundationApi.TechMods;
```

Custom definition classes can implement IModTechNodeDefinition. IDs must be nonempty and stable, and a category ID has one consistent display name. Without explicit metadata, unidentifiable registrations fall back to Foundation's category. Updates preserve the original owner; explicit attempts to change it are rejected. Branch RootId and PreviousId must belong to the same category. Use Requirements for dependencies on Base Game or another mod; clicking those labels selects the correct destination category automatically.

IModTechNodeRegistry extends the original registry interface with Mods, and TechNodeRegistry implements both. FoundationApi.TechMods and each ITechMod.Nodes return stable collection snapshots; categories are sorted by display name and ID, and empty categories are omitted.

## Research placement and overlap prevention

Position is a preferred native map coordinate, with X right and Y up. Foundation measures the active native frames, cost displays and requirement labels on each visible map, then moves later registrations right into free space with a 32-unit gap. Animated glow and hover graphics do not change placement. Native connectors and focus use the resolved positions; ((INativeTechNode)tech).NativeNode.Position exposes them. The original definition coordinates stay unchanged.

This also handles branch roots, generic resource/recipe views, late registration and definition updates. Existing positions are retained when free. Different category pages can use the same coordinates because they are never rendered together. No research IDs, purchases or saved workspace positions are changed by this layout.


## Shared Mod Bar (0.6.0+)

Foundation adds a 48-unit native-style row 2 pixels below the currency bar, with a top-center chevron toggle that smoothly slides the bar open and closed. Register controls on Unity's main thread through `IModBarRegistry bar = FoundationApi.ModBar`. Registration is supported in plugin Awake before the HUD exists, and at runtime. The player package contains no example controls.

```csharp
IModBarRegistration statusHandle, buttonHandle, separatorHandle, periodHandle;
ModBarText status;

void RegisterBar()
{
    IModBarRegistry bar = FoundationApi.ModBar;
    const string owner = "your.mod.guid";
    status = new ModBarText(owner, "status", "Ready", width: 200, order: 0, alignment: ModBarAlignment.Left);
    statusHandle = bar.Register(status);
    buttonHandle = bar.Register(new ModBarButton(owner, "open", "Open report", () => {
        status.Text = "Report opened";
        statusHandle.Refresh();
        // Open your mod's window here.
    }, width: 200, order: 1, alignment: ModBarAlignment.Left));
    separatorHandle = bar.RegisterSeparator(owner, "divider", order: 2, alignment: ModBarAlignment.Left);
    periodHandle = bar.Register(new ModBarDropdown(owner, "period",
        new[] { "Second", "Minute", "Hour" }, selected: index => {
            // Update your own reporting setting.
        }, order: 3, alignment: ModBarAlignment.Left));
}

void OnDestroy()
{
    periodHandle?.Dispose(); separatorHandle?.Dispose(); buttonHandle?.Dispose(); statusHandle?.Dispose();
}
```

`ModBarToggle` adds a typed bool control, and `ModBarSeparator` (or `bar.RegisterSeparator(...)`) adds a vertical divider with configurable `ModBarAlignment` (`Left`, `Center`, `Right`), `Order`, and `Width` (default 14 units). Set `Text`, `Interactable`, `IsVisible`, `Order`, `Alignment`, dropdown `SetSelectedIndex`, or toggle `SetValue`, then call the registration's `Refresh()`. Refresh is applied in the HUD's next layout pass. Programmatic selection/value refresh is silent; user actions call `Clicked`, `SelectionChanged` or `ValueChanged`. The framework isolates subscriber exceptions. State belongs to your objects; persist your own settings when required. There is no polling delegate or implicit save format.

The contracts are polymorphic:

| Interface | Responsibility |
| --- | --- |
| `IModBarRegistry` | Register controls and separators, lookup and enumerate controls; announce registration/presentation changes. |
| `IModBarElement` | Supply stable mod GUID/local ID, width, order, `ModBarAlignment` (`Left`, `Center`, `Right`) and visibility; create a view for each HUD lifetime. |
| `IModBarView` | Refresh the scene's presentation; dispose mod-owned subscriptions/resources on removal or scene exit. |
| `IModBarContext` | Expose a framework-owned `Root` slot and native-style text/button/dropdown/toggle/separator factories. |
| `IModBarRegistration` | Keep the registration alive independently of a HUD, request refresh, or dispose it. |

`ModBarElement` is an optional abstract convenience base. Implementing `IModBarElement` directly is supported; the HUD calls `CreateView` through the interface and has no type switch for built-in controls. The separately compiled example includes a `CustomBarElement`/`IModBarView` implementation alongside the built-in controls. Custom views can put icons, progress bars or other Unity UI children under `context.Root`. Use the native factories (`CreateText`, `CreateButton`, `CreateDropdown`, `CreateToggle`, `CreateSeparator`) as building blocks, then position those returned controls within the slot yourself. One factory call fills its slot by default. References require local UnityEngine.UI and Unity.TextMeshPro assemblies in addition to the normal SDK references; keep `Private=false`.

IDs are unique within each owning GUID; separate mods may reuse local IDs. Dispose is idempotent and stale handles cannot remove a replacement. Collection properties return stable snapshots. Controls group by `Alignment` (`Left`, `Center`, `Right`) and sort within each alignment group by `Order`, then mod GUID and local ID using ordinal comparison. Width is 8–2048 UI units, with a 40-unit-high content slot, 8-unit gaps and a single horizontally scrolling row. Hidden items take no space. Call `Refresh()` after changing custom width/order/alignment/visibility properties too; identity cannot change. Reserve enough width for your label; text can shrink or ellipsize.

The framework owns slot transforms, destroys their Unity objects and closes factory-created dropdown popups. A context is valid for only that HUD lifetime: do not cache it across scene changes, reparent its Root, or destroy another mod's controls. `IModBarView.Dispose()` releases your external event handlers or resources; do not unregister the persistent item there. A fresh view is created next scene, using the same element state. Dispose your registration when your plugin unloads. All registry changes, refresh calls and callbacks must run on Unity's main thread. Custom views must keep their content inside their own slot; custom popups outside Root need their own explicit lifecycle and input handling.

The bar follows native HUD visibility and scaling. Hovering it blocks workspace wheel zoom, overflow uses native ScrollRect plus arrow buttons, and Escape closes a factory-created dropdown before opening the game menu. Research headings and list controls are inset to stay below the new row. Existing research/workspace API contracts are unchanged; 0.5.x consumers remain compatible.
