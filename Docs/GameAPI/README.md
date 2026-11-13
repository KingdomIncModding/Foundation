# Kingdom Inc Game API

Foundation exposes **real Kingdom Inc systems**, not replacements for them.

See [the framework boundary](../Framework-Boundary.md) for native-free consumer contracts and the distinction between primitive game adapters and mod feature behavior. The older native-facing APIs below remain available for compatibility.

- [Machines](Machines.md): native `FlowElement` types, inspection, capabilities and specialized operations.
- [Logistics](Logistics.md): `FlowIO`, `FlowPipe`, ports and item movement.
- [Gameplay](Gameplay.md): items, resources, economy, research, adventures, combat and workspace state.

Use `FoundationApi` for general services and `FoundationMachineApi` for specialized machine access.

## Native access rules

A game-backed service operates on the game's existing state. Not all machines support the same configuration action. Live Unity objects may become invalid after world unload or object removal; avoid keeping references across world changes. Perform native game operations on the main thread.

Register mod-created content through the [content API](../Content-And-Persistence.md); do not build a parallel gameplay simulation.
