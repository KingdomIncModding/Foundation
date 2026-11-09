# Foundation â€” GitHub Repository Guide

## Repository contents

`src/Foundation/` is the production source for the **single** Foundation DLL.
`Foundation.csproj` is the .NET Standard 2.1 project. `SDK/`, `Examples/`,
`Tests/`, `Tools/`, `Docs/`, and `Packaging/` remain separate by purpose.

The GitHub repo intentionally excludes build products, diagnostics, release ZIPs,
local backups, Steam game assemblies, BepInEx DLLs, decompiled game source, and
binary art. These belong in local installs or separately distributed releases,
not in the source repository.

## Build on a licensed local copy of the game

Foundation is a mod and needs the installed Kingdom Inc and BepInEx assemblies.
GitHub CI can run **static source checks**, but must not pretend to compile a
working game DLL without the licensed game dependencies. From a local terminal:

```powershell
dotnet build .\Foundation.csproj -c Release -p:GameRoot="G:\Program Files (x86)\Steam\steamapps\common\KingdomInc\"
```

Adjust `GameRoot` for your installation. Existing install scripts also accept
`-GameRoot` when the repository is located outside `_Mods/Foundation`.

## GitHub publication checklist

1. Review `git status --short` and every staged file for copyright and secrets.
2. Verify no game DLLs, bundled assets, crash reports, credentials or saves.
3. Add an explicit LICENSE only if you have the rights to license these files.
4. Build and run Foundation locally against the target Kingdom Inc version.
5. Keep the user's current installed game copy and this Git repo separate.
6. Publish prebuilt runtime/SDK ZIPs as GitHub Releases only when tested.
7. Preserve mod GUIDs, saved-content IDs and existing public contracts.

The source repository alone does not certify Foundation 1.0 compatibility.
