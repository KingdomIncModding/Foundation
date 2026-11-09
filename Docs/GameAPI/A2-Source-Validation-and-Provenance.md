# Foundation A2 Source Validation and Provenance

## Input and scope

- Foundation authoritative input: `Foundation-Source-20261109-094126-36516.zip`, SHA256 `6dc561c14270840725758d7a1c50568710890da889b5eb21b5a9fbf152d98629`.
- Decompiled original game source (for reference only): `KingdomIncSrc.zip`, SHA256 `988651a5649a1f95caadd5e0b04a59d0b7b84e3fd79a555bed47597afe4be3c8`.
- Controlling requirements: Foundation 1.0 Game API First roadmap, Stage **A2**. The user's roadmap files were retained without modification.
- Scope of this change: Native FlowIO/FlowPipe inspection, actual port/item compatibility, native logistics router state, per-machine observed-throughput aggregation, native connection preflight, native editing facade, committed connection events, and independent runtime verifier. No independent transport logic, Signal System rewrite, UI rewrite, or machine simulation.

## Actual game methods inspected

| Original game source path | Observed API/behavior | Foundation binding |
|---|---|---|
| `Assets.Source.Flow/FlowIO.cs` | `CanAcceptItem`, `GetPreviewTypes`, `CanBuffer`, `BufferSize`, `Filter`, `Item`, `BufferedItems` | `NativeIOPortSnapshot`, `MachineNativeIO.CanAccept` |
| `Assets.Source.Flow/FlowInput.cs` | `InputItem` only checks buffer availability | No synthetic item mutation; validation matches predicate + space |
| `Assets.Source.Flow/FlowOutput.cs` | `AvailableItem`, initial output buffer=1 | Port snapshot and game owned transfers |
| `Assets.Source.Flow/FlowPipe.cs` | `Update` do/while transfer until blocked; `Connect`, `ItemRejected`, route nodes | `NativeIOPipeSnapshot`, read-only native contract |
| `Assets.Source.Flow/FlowDiagram.cs` | Native element-then-pipe update; disconnect operation; native JSON persistence | Existing `FoundationApi.IO` bridge; removal hook |
| `Assets.Behaviour.Flow/WorldFlowManager.cs` | `CreateNewPipe`, `DisconnectPipe`, `CurrentFlow` | Post-commit hook; existing native edit operations |
| `Assets.Behaviour.Flow/WorldFlowPipe.cs` | `UpdateLinePositions`, `ItemTransferred` | Existing 10-second counter, world visuals |
| `Assets.Behaviour.Flow/WorldFlowPipeNode.cs` | `ClampToGrid` | `SnapRouteNode` |
| `Assets.Source.Flow.Elements/Collector.cs` | Slot count, current input, one forward step per update | Native router snapshot + existing native expand/collapse |
| `Assets.Source.Flow.Elements/Distributor.cs` | Slot count, current output, one forward step per update | Native router snapshot + existing native expand/collapse |
| `Assets.Source.Flow.Elements/Manifold.cs` | Paired ports, first eligible output, one forward step per update | Native router snapshot + existing native expand/collapse |
| `Assets.Behaviour.Flow.Elements/WorldFlowCollector.cs` and `WorldFlowDistributor.cs` | Native UI expansion count `< 4` | Existing `MachineOperations.ExpandPorts` cap |

## Source-only verification completed

- A2 native source contracts (including original decompiled game comparisons): **35 PASS / 0 FAIL / 0 SKIP**.
- Existing UI source contracts against actual game decompilation: **51 PASS / 0 FAIL / 0 SKIP**.
- Game-authentic UI theme contract checks: **24 PASS / 0 FAIL**.
- Existing Foundation organized source structure checks: **PASS**.
- Existing UI screenshot harness source checks and smoke isolation checks: **PASS**.
- `.NET 8+ SDK` / Windows Kingdom Inc managed references: **not available in this analysis container**. No successful C# compilation, install, or in-game test is claimed. All new A2 runtime scenarios start as **PENDING** and must be exercised by independent consumer on the user's actual installation.

## Non-native items intentionally not implemented

- No static items-per-second pipe maximum: it does not exist in the supplied `FlowPipe.Update` code.
- No generic `FlowIO.Item`, `BufferSize` or `Filter` setter: vanilla does not provide a general validated configuration operation; machine-specific native operations are already exposed elsewhere in the unified framework.
- No batch pipe mutation contract: no equivalent atomic native operation in the reviewed code; a custom utility is deferred until after Gate A.
- No new transport paths, signals, ghost-to-real conversion, custom machine balancing, queue simulation, duplicate authoritative graph, or custom UI generation.
- No change to existing BepInEx gameplay plugin ID; the independent test mod is development-only, with explicit opt-in for mutations.

**Gate:** A2 code is integrated in this full source release. Verification cannot be declared 100% until the installer builds against the actual installed game and the independent `Tests/NativeIO` reporter yields a fresh PASS for every applicable native scenario with FAIL=0/PENDING=0.
