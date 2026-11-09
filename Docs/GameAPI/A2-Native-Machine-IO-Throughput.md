# Foundation 0.10.4 Candidate — Native Machine IO & Throughput (Stage A2)

**Controlling roadmap:** `Foundation_1.0_Game_API_First_Rewrite_Roadmap.md`, Stage A2. This adds a **thin native Game API bridge**, not a new item transport, throttling algorithm, signal network, or machine behavior. The 24 custom Foundation-system rewrites remain frozen until Gate A. Existing UI, Signals, Core, services and all previously verified APIs are preserved in the updated baseline.

**Source baseline:** user's `Foundation-Source-20261109-094126-36516.zip`. Game evidence: user's decompiled `KingdomIncSrc.zip`, including `Assets.Source.Flow` and `Assets.Source.Flow.Elements`. This package is a **single complete updated source tree** (not successive mini-archives). The compiled BepInEx test consumer is development-only and NOT part of the production `Foundation.dll`.

## Exact native transfer rules (verified by reading game source)

The game invokes `GamePlayer.Update(Time.deltaTime)` from `WorldFlowManager.Update`. `GamePlayer.Update` calls **every** `FlowDiagram.Update(delta)`, which calls each `FlowElement.Update(delta)` **before** each `FlowPipe.Update(delta)`. This occurs on game updates, not at a fixed 1-second transport interval.

`FlowPipe.Update`: clear `ItemRejected`, take the source output's `AvailableItem`, ask destination input's **native `CanAcceptItem(item)`**, then call input's `InputItem(item)` (which checks its current `CanBuffer`). On success, remove from output, call optional native `WorldFlowPipe.ItemTransferred`, and repeat in a do/while until no further transfer succeeds. **The pipe itself has no fixed items/second cap.** It is limited by upstream production/scheduling, the output buffer, destination buffer, actual item predicate and the number of native updates. If the destination filter rejects the item, `ItemRejected` becomes true for that update. A full buffer does not set `ItemRejected` because filter acceptance and buffering are checked separately.

Native `FlowOutput` starts with `BufferSize = 1`. `FlowIO.CanBuffer` uses `BufferedItems.Count < BufferSize`. Native `FlowIO.CanAcceptItem` uses a custom `Filter` delegate if present, otherwise compares native `ItemType`; absent both it returns false. `FlowInput.InputItem` **only tests buffer space**; the pipe is responsible for the separate type predicate. `FlowIO.GetPreviewTypes` is not complete for a custom filter with `FilterPreview=false`—for example, on Collectors and Distributors. The SDK now explicitly labels these previews non-exhaustive.

### Native routing, not invented rates

| Machine | Native port layout | What `Update(delta)` really does | Maximum port count (native UI) | Throughput statement |
|---|---|---|---|---|
| **Collector** | 2→4 inputs (buffer 1 each), 1 output (buffer 1) | If its sole output can buffer: consume at most **one** item from `CurrentInput`, put it in the output, advance to the next input, favoring connected inputs. May advance even without consuming. | 4 inputs | **At most 1 forwarded item per Collector update**, NOT an invariant items/sec limit. |
| **Distributor** | 1 input (buffer 1), 2→4 outputs (buffer 1 each) | If `CurrentOutput` is full, advance and return; otherwise consume at most **one** input item into selected output, then advance to next connected output. | 4 outputs | **At most 1 forwarded item per Distributor update**, subject to selected output availability. |
| **Manifold** | 2→4 paired input and output slots (buffers 1 each) | Scan inputs and outputs, send the first buffered input to the first **connected and available** output and return. Does not do lane-to-lane matching or distribute multiple items in one update. | 4 input/output pairs | **At most 1 forwarded item per Manifold update**, even with four output ports. |

These limits are counts **per native machine-update call**. The game source does **not** specify a universal numeric throughput per second for these routers. Fabricating `60 items/sec`, `4 items/sec`, a pipe speed multiplier or lane-specific restrictions would violate Stage A2. Item transfer rates measured from world events are **observations**, not capacity. One native transport tick could transfer multiple buffered items along one pipe; routers themselves forward one at most per tick.

## New API surface (same `Foundation.dll`)

`FoundationMachineApi.IO` and `FoundationApi.NativeIO` are the **same single `MachineNativeIO` instance**. The existing `FoundationApi.IO`, `FoundationApi.Flow`, `FoundationMachineApi.Registry`, `FoundationMachineApi.Operations`, `FoundationMachineApi.Production`, `FoundationMachineApi.Telemetry` remain compatible. `MachineNativeIO.Connect`, `Disconnect`, `ReplaceRoute`, `InsertRouteNode` and `RemoveRouteNode` delegate to the **existing** `FoundationApi.IO` native workspace bridge, so no second authoritative pipe graph/transport system is introduced.

| Call | Backing | Contract |
|---|---|---|
| `Ports(machine)`, `Port(port)` | `FlowElement.Inputs/Outputs` + `FlowIO` | Captured buffer/port/filter/connection snapshots, native preview types with an exhaustive flag |
| `Pipe(pipe)`, `Pipes(flowIndex)` | `FlowDiagram.Pipes` + existing 10-second event counter | Snapshot native endpoints, bend nodes, transient rejection, measured transfer observations |
| `Throughput(machine)` | Same native pipe list & shared transfer counter, plus `MachineProduction.NativeOutputRate` | Directional observed item rates and counts; native production rate is separate and nullable |
| `ValidateConnection(output,input)` | Native port/workspace identity, occupancy and editor availability | **Pre-placement feasibility**. The game does not check item compatibility when creating a pipe. |
| `CanAccept(input,item)` | Native `FlowIO.CanAcceptItem` **and** `CanBuffer` | Live check for a *concrete* native item; does not enqueue/mutate |
| `Connect`, `Disconnect` | Existing Foundation native call -> `WorldFlowManager.CreateNewPipe/DisconnectPipe` | Real editable flow only, no ghost or cross-flow pipe |
| `ReplaceRoute`, `InsertRouteNode`, `RemoveRouteNode` | Existing native workspace editing bridge -> `FlowPipe.Nodes`, `WorldFlowPipe.UpdateLinePositions()` | Actual route/collider regeneration, rollback through existing helper on exception |
| `SnapRouteNode` | `WorldFlowPipeNode.ClampToGrid` | Matches native grid snapping; optional because game permits Ctrl+drag off-grid |
| `Router(machine)` | Native `Collector.CurrentInput`, `Distributor.CurrentOutput`, manifold input count and native item fields | Exposes live native routing state, without inventing new routing modes |
| `ExpandRouter`, `CollapseRouter` | Existing `MachineOperations.ExpandPorts/CollapsePorts`, which invoke native `AddInputSlot`, `AddOutputSlot`, `Collapse()` | Uses native 4-port UI cap; wrapper guards against dropping buffered trailing slots during collapse |
| `ObserveConnections(callback)` | Harmony **postfix** on real `WorldFlowManager.CreateNewPipe`, pre/post on native `FlowDiagram.DisconnectPipe` | Only committed non-ghost topology events; subscriber errors isolated; dispose subscriptions on plugin unload |

`FoundationApi.Flow.PipeConnected` is retained unchanged for compatibility with its earlier, **low-level `FlowPipe.Connect`-time** semantics. The new committed event is intentionally different because the low-level method may be called before the pipe is registered on the live flow, and `ghost:true` remains a preview. The new observer records only committed creation and actual removal; snapshots are immutable.

### Configuration scope

Vanilla `FlowIO` exposes public `Item`, `Filter`, `FilterPreview`, `BufferSize` fields, but **the game does not provide a universal settings UI or validated generic setter for arbitrary ports**. Foundation therefore exposes these as **read-only native facts** instead of allowing mods to create invalid filter/buffer configurations. All existing machine-specific configuration goes through dedicated native methods: `MachineOperations.SetFilter`, `SetPortalDirection`, `SetArmoryOutput`, `SetStockpileCapacity`, `ExpandPorts` and `CollapsePorts`, plus the other native machine controls in A1. No extra fictional port settings added.

The existing source has **no native atomic batch-pipe edit**. Stage A2 therefore classifies batch operations as **not present in vanilla** and defers a potential Foundation utility until after Gate A instead of claiming a missing native feature was implemented. Native pipe bends/geometry are NOT a physical transport-speed limitation.

## Threading, ownership, save data, failures

- Main Unity thread only, enforced by existing `MachineThread.Verify()`. Snapshots are immutable copies and are valid only as observations at capture time.
- All edits require real placed native machines or pipes. Connect requires both endpoints on the **visible native flow**. No self-pipes through Foundation's pre-existing helper. Native UI may disconnect an occupied endpoint first; Foundation's helper deliberately rejects an occupied endpoint to avoid implicit destructive rewiring.
- `FlowDiagram.ToJson`/`FlowPipe.ToJson` are the **game's sole saved graph**; Foundation does not create an additional pipe store or transient graph cache. Native port buffer serialization is performed by `FlowIO.ToJson`.
- The ten-second rate is **observed** `WorldFlowPipe.ItemTransferred` events per ten seconds. If none are observed, rate 0 does **not** mean the native maximum is 0. It may mean empty buffers, no world view, not enough observation time or disconnected pipes. Existing 5-second `MachineTelemetry` remains for compatibility but the new API shares the existing 10-second `FoundationApi.IO` meter.
- `LastUpdateRejectedItem` comes directly from `FlowPipe.ItemRejected`, reset each native update. Do not confuse with buffer backpressure.
- Safe callback isolation applies only to Foundation observers. It does not mask failure of the game's own flow mechanics.
- The memory-based rate meter is bound to the actual `FlowPipe` reference and is not persisted between game sessions. Native pipes/ports are the source of truth.

## Verification matrix (A2 acceptance)

| A2 item | Source completion | Runtime evidence needed |
|---|---|---|
| Input / output inspection | Reused + native snapshots | In-game port count, parent index, buffer and item type parity |
| Connection validation | Read-only preflight added | Occupied, cross-flow, invalid and free active ports tested |
| Item compatibility | Native predicate + buffer check | Compare with actual `FlowIO.CanAcceptItem` for a real item |
| Native port configuration | Exposed read-only + native subclass methods | Expand/collapse and special native setters tested where available |
| Pipe editing | Existing native bridge reused | Connect/disconnect, route replace, insert, remove, restore |
| Native connection events | Committed event hooks added | In-game connect/removal and throwing subscriber isolation |
| Throughput observation | Existing ten-second meter reused + machine aggregation | Real `ItemTransferred` events and consistency with native pipe count |
| Special Collector/Distributor/Manifold | Dedicated native router snapshots | Each machine present, correct topology and selector indices, expand/collapse |
| Diagnostics | Port/pipe/router snapshots | Verify rejection flag, native preview completeness, backed values |
| Batch editing | **Not native** | No invented API; explicitly deferred per roadmap |

The package provides `Tests/NativeIO/NATIVE-IO-TEST.cmd`: independent runtime test mod with a single `nativeio-verification.json` report. The check report distinguishes **PASS / FAIL / PENDING**; no test reports PASS until its actual scenario was seen. Normal operation is **read-only**. Optional `-foundation-nativeio-mutate` performs reversible edits only when explicitly launched against a disposable kingdom (the test does not back up saves automatically). In-game verification is mandatory before claiming 100% runtime completion, as source/ZIP tests cannot prove the Unity hooks work on the user's install.

## Usage from a dependent mod

```csharp
using KingdomFoundation.Machines;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;

var native = FoundationMachineApi.IO;
var machines = FoundationMachineApi.Registry.OfType<Collector>();
foreach (var collector in machines)
{
    var topology = native.Router(collector); // actual native CurrentInput / InputCount
    var ports = native.Ports(collector);      // native filters, buffers, peers
    var rate = native.Throughput(collector); // observed incoming/outgoing, not a cap
}

// A real game operation: never creates a new transport simulation.
// var allowed = native.ValidateConnection(actualNativeOutput, actualNativeInput);
// if (allowed.Allowed) native.Connect(actualNativeOutput, actualNativeInput);
```

## Version/release state

Foundation **0.10.4 SOURCE CANDIDATE**. Compiled behavior on Windows is not verified in this environment because the installed game/BepInEx assemblies are not available here. The archive is a complete single source release. It is **not** a claim that Gate A is satisfied: A1 and A3–A7 remain separate workstreams in the controlling roadmap.

## Single-package installation and evidence collection

Extract the one complete `Foundation-0.10.4-Native-IO-Complete-Source.zip` under `KingdomInc\_Mods` so the source lives in `KingdomInc\_Mods\Foundation\`. The existing, tested UI files and two controlling 1.0 roadmaps remain in this directory. Close Kingdom Inc before running the installer.

**One-step install:** run `Foundation\Tests\NativeIO\INSTALL-NATIVE-IO-TEST.cmd`. It calls the existing Foundation installer, builds the entire unified source using the installed game and BepInEx assemblies, deploys **one** production `Foundation.dll`, then builds and installs the separate development-only test consumer against that freshly installed DLL. It aborts on build failure. No guessed stubs or source-code-only DLL substitutions.

**In-game audit:** launch Kingdom Inc normally, load a disposable kingdom with at least one **Collector, Distributor, Manifold** and actual **item-transfer pipes**. Observe flow, add/remove a connection, visit a second kingdom, return to the menu and exit. Open `Foundation\Tests\NativeIO\NATIVE-IO-TEST.cmd` and choose **3 Report**. The report is written to `KingdomInc\BepInEx\plugins\Foundation.NativeIOVerification\Reports\nativeio-verification.json`. The PowerShell reader checks report freshness against both installed DLLs and the current BepInEx log. It reports PASS, FAIL, PENDING per scenario; **PENDING is not PASS**.

**Explicit mutation tests:** for edit coverage run a **disposable** game world with the Unity command-line argument `-foundation-nativeio-mutate` added to the game launch options. The verifier attempts reversible pipe route and slot edits and an available-endpoint connect/disconnect, only when the flag is present. This is an in-game edit; back up/discard the disposable save. The test restores route nodes and port counts in normal success paths and reports failures otherwise. Never enable this on a valuable kingdom.

When verification is complete, close the game and choose **4 Remove Test**. Keep `Foundation.dll`; only the development-only verifier DLL is removed.

A2 is **source-implemented**, but acceptance requires the reader to report `FAIL=0 PENDING=0` for the actually installed 0.10.4 assembly, plus Core/Machine/UI regression results. This archive itself does not claim those Windows results.
