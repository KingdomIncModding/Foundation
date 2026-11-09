# Repository cleanup — 2026-11-09

**Inputs:** Uploaded `Foundation-0.10.1-expanded-source(1).zip` and previously delivered Foundation Core 1.0 source overlay.

## Result

- Source from `Api/` and `Plugin/` was reorganized into `src/Foundation/<domain>/`; Core 1.0 is included. The source project now compiles one recursive `src/Foundation/**/*.cs` glob.
- The pre-organization source migration produced 277 standalone type files from 84 original files. Combined with 75 Core files, the distribution contains 352 C# source files under `src/Foundation/`.
- The only root command file is `INSTALL-FOUNDATION.cmd`.
- Repeatable PowerShell scripts are in `Tools/Build/`, `Tools/Install/`, and `Tools/Development/`; references were updated to the new script locations.
- Current technical docs are in `Docs/Guides/`, `Docs/Core/`, `Docs/Architecture/` and `Docs/Releases/`. Old release notes and guides are under `Docs/Archive/`.
- The player package README is at `Packaging/Player/README.txt` and the SDK reference remains at `SDK/README.md`.

## Removed as obsolete / single-use

`Organize-Foundation.cmd`, `Tools/organize_foundation.py`, `VERIFY-INSTALLED.cmd`, `verify-installed.ps1`, `README-AUDIT.md`, `AUDIT-HOTFIX-README.md`, `INSTALLER-HOTFIX-README.md`, `build-ready.ps1`, and the unnecessary `Directory.Build.targets` compilation shim.

The source migration backup ZIP was used as a safety copy while working and is not redistributed. None of the uploaded source ZIP's entries had a `.png` extension, so there was nothing to migrate into an images folder.

**Status:** File structure, source type-count and references were statically checked. The combined source was not compiled or launched in the Windows game environment. The package has no precompiled runtime plugin.
