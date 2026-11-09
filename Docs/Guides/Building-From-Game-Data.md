# Building Foundation from the supplied game data

Foundation targets `netstandard2.1` and compiles against real Kingdom Inc / Unity Mono assemblies and BepInEx 5. The source package never redistributes the user's game DLLs or saves.

## Normal install

Close Kingdom Inc and run **`INSTALL-FOUNDATION.cmd`** in the Foundation root. It builds, packages and installs, using the game found two folders above `Foundation` by default (when located under `KingdomInc\_Mods\Foundation`). No audit step runs by default.

## Build with `KingdomInc_Data.zip`

Run from the Foundation root in PowerShell:

```powershell
& '.\Tools\Build\Build-Foundation.ps1' -GameDataZip '..\KingdomInc_Data.zip'
```

The reusable build script stages only the actual managed DLLs, acquires/checks BepInEx 5.4.23.6 for Unity Mono Windows x64 if missing, and invokes `dotnet build` for Foundation and SDK examples. It needs Windows PowerShell and a .NET SDK 8+ installation. A build against game-data ZIP does not establish in-game compatibility.

## Additional commands

```powershell
# Compile but do not package or install
& '.\Tools\Install\Install-Foundation.ps1' -BuildOnly

# Build against a selected installed game
& '.\Tools\Build\Build-Foundation.ps1' -GameRoot 'D:\SteamLibrary\steamapps\common\KingdomInc'

# Optional developer isolated runtime diagnostic (not part of default install)
& '.\Tools\Development\Verify-Isolated.ps1' -GameRoot 'D:\SteamLibrary\steamapps\common\KingdomInc'
```

Source output: `bin/`; packaged releases: `Releases/`; temporary dependencies: `.foundation-deps/`; install logs: `Verification/InstallLogs/`. These are ignored by Git.
