# Foundation Documentation

Foundation is the shared C# modding framework for Kingdom Inc. It exposes native game features and gives mods common tools for custom content, UI, signals, services and saved data.

**Start with [Getting Started](Getting-Started.md).**

| Guide | Topic |
| --- | --- |
| [Getting Started](Getting-Started.md) | Create a BepInEx mod using Foundation |
| [API Reference](API-Reference.md) | Find the public service you need |
| [Core Framework](Core/README.md) | Module lifecycle and inter-mod services |
| [Game API](GameAPI/README.md) | Access to the original game's systems |
| [Machines](GameAPI/Machines.md) | Native machine state and operations |
| [Logistics](GameAPI/Logistics.md) | Pipes, ports and real item flow |
| [UI](UI/README.md) | Mod settings, panels and HUD controls |
| [Signals](Signals.md) | Typed properties and bindings |
| [Content and Persistence](Content-And-Persistence.md) | Custom definitions and mod-owned saves |
| [Migration](Migration.md) | Updating existing Foundation mods |

The implementation lives in [src/Foundation](../src/Foundation/). Consumer mod projects are in [Examples](../Examples/).

**The game remains authoritative.** Foundation does not recreate the simulation of machines, crafting, research, combat or item transportation.
