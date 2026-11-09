> **Current:** Foundation 0.10.7 Stage B1 rewrite; see `Docs/Core/Stage-B1-Rewrite.md`. Earlier 0.10.1 source-only completion statements below are historical and superseded for this milestone.

# Foundation Core API 1.0 (for Foundation 0.10.1)

**Implemented Core infrastructure:** module registration/metadata, BepInEx dependency discovery, module load order, cross-mod services and events, Harmony ownership/conflicts, world and save lifecycle events, scoped cleanup, API version contracts, central content metadata, diagnostics, and source organization.

## Included in this organized source tree

The Core implementation is already integrated into `src/Foundation/Core/` and is compiled with all other gameplay sources by the **single** `Foundation.csproj` project.

There is no one-time organizer, migration launcher, or separate Core installer. Use `INSTALL-FOUNDATION.cmd` in the project root to compile and deploy the resulting Foundation DLL. The directory `Examples/CoreMod` contains an independent consumer example.

## API entry point

```csharp
using KingdomFoundation.Core;

var core = FoundationCoreApi.Core;
var installed = core.Modules.Register(myModule);
var availableMods = core.Plugins.Plugins;
var services = core.Services.Registered;
var content = core.Content.Registered;
var errors = core.Diagnostics.Recent;
```

For mod authoring and ownership rules, read `Docs/Core/Framework.md`. For implementation tracking, read `Docs/Core/Completion-Matrix.md`; for source conventions, read `Docs/Architecture/Source-Conventions.md`. The independent `Examples/CoreMod` shows registering a shared service, world callbacks and cleanup.

**Status:** Complete Core implementation in source, not yet compiled or runtime-proven against the user's Foundation 0.10.1 installation. The only Foundation executable runtime evidence available belongs to 0.9.3.
