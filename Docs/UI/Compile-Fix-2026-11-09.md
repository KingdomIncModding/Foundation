# Foundation UI 0.10.3 — Compile Hotfix (2026-11-09)

This source hotfix addresses **all five compiler errors** in the supplied Windows `Foundation` build log (`foundation-20261109-051724.log`). It does not change the production DLL name, plugin identity, or Foundation public API.

| Compiler error | Location | Fix |
| --- | --- | --- |
| CS0119 | `UI/Panels/FoundationPanelBuilder.cs`, slider `Direction` | Qualify the Unity component enum as `UnityEngine.UI.Slider.Direction.LeftToRight`, because `FoundationPanelBuilder.Slider(...)` shadows the type name. |
| CS0165 (three reported variables) | `UI/Panels/FoundationPanelBuilder.cs`, paginated search | Initialize `previousButton`, `nextButton` and `caption` before callbacks capture the local `update` function. |
| CS0119 | `UI/Panels/FoundationNativeMachinePanels.cs`, Stockpile capacity | Alias the native machine class `Assets.Source.Flow.Elements.Stockpile` to `StockpileNode` to avoid collision with the panel's `Stockpile(...)` method. |

## Apply

1. Exit Kingdom Inc.
2. Extract `Foundation_UI_0.10.3_Compile_Fix.zip` into the **`KingdomInc\_Mods\` directory**, allowing existing `Foundation\...` source files to be overwritten. Do **not** extract into `Foundation` itself or it will create a nested `Foundation\Foundation\` directory.
3. From `KingdomInc\_Mods\Foundation\`, run `INSTALL-FOUNDATION.cmd`, or use `UI-FRAMEWORK-TEST.cmd` and choose option **2: Build and install**.
4. Build must succeed before any game runtime verification is attempted. If further compiler errors appear, collect the new `Verification\InstallLogs\foundation-*.log`.
5. Re-run the optional UI demo and existing Core and Machine regression suites after a successful build.

## Checks performed

- The C# changes were inspected against the exact five reported diagnostics.
- Static source structure verification passed.
- UI source checks, including native signature comparisons, passed **48/48** (three compile regression checks added).
- Unity smoke-isolation source checks passed.
- **C# compilation and Unity runtime testing could not be performed here** because the installed game assemblies and Windows .NET toolchain are not present. The earlier 0.10.2 binary remains the verified rollback baseline.
