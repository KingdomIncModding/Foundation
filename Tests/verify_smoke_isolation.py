"""Source inspection for both read AND write isolation of the smoke-test runtime.

The separate game copy shares Unity's persistentDataPath; blocking LoadLatestSave
alone is NOT enough because GameManager.OnApplicationQuit writes autosave-exit.
This test cannot replace an actual Windows/Unity runtime verification.
"""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
s = (root / 'src' / 'Foundation' / 'Bootstrap' / 'FoundationPlugin.FoundationPlugin.cs').read_text()
read_patch = (root / 'src' / 'Foundation' / 'Bootstrap' / 'Patches' / 'BlockExistingSaveInSmoke.cs').read_text()
write_patch = (root / 'src' / 'Foundation' / 'Bootstrap' / 'Patches' / 'BlockAnyNativeSaveInSmoke.cs').read_text()
smoke_install = s.index('if (Diagnostic)')
smoke_block = s[smoke_install:s.index('Logger.LogInfo("Foundation', smoke_install)]
assert 'IsolateSmokeSaves();' in smoke_block
assert 'hooks.CreateClassProcessor(typeof(BlockExistingSaveInSmoke)).Patch();' in smoke_block
assert 'hooks.CreateClassProcessor(typeof(BlockAnyNativeSaveInSmoke)).Patch();' in smoke_block
assert '[HarmonyPatch(typeof(SaveGame), nameof(SaveGame.LoadLatestSave))]' in read_patch
assert '[HarmonyPatch(typeof(SaveGame), nameof(SaveGame.Store))]' in write_patch
assert 'SaveGame.SavesPath = isolated;' in s
assert 'AccessTools.Field(typeof(SaveGame), "SavesDir").SetValue(null, new DirectoryInfo(isolated));' in s
assert 'static bool Prefix() => false;' in write_patch
native = (root / 'Tests' / 'verify_native_contracts.py').read_text()
assert 'OnApplicationQuit' in native
print('Smoke-test isolation guards: no native reads AND no native disk saves (source check only)')
