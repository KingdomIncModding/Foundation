#!/usr/bin/env python3
"""Read-only source layout checks for the organized Foundation repository.

This is a development tool, not an installer prerequisite or C# compiler.
No generated files or game saves are modified.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "Foundation"


def load_game_dir(argument: Path | None) -> Path | None:
    if argument is None:
        return None
    return argument.resolve()


def check(results: dict, label: str, condition: bool, details: str = "") -> None:
    results[label] = {"passed": bool(condition), "details": details}


def has_balanced_braces(code: str) -> bool:
    """Check brace balance without counting braces in comments or C# literals."""
    pairs = {"}": "{", "]": "[", ")": "("}
    stack = []
    index = 0
    state = "code"
    while index < len(code):
        current = code[index]
        following = code[index + 1] if index + 1 < len(code) else ""
        if state == "code":
            if current == "/" and following == "/":
                state = "line"
                index += 1
            elif current == "/" and following == "*":
                state = "block"
                index += 1
            elif current == '@' and following == '"':
                state = "verbatim"
                index += 1
            elif current == '"':
                state = "string"
            elif current == "'":
                state = "char"
            elif current in "{[ (":
                if current != " ":
                    stack.append(current)
            elif current in pairs:
                if not stack or stack.pop() != pairs[current]:
                    return False
        elif state == "line":
            if current == "\n":
                state = "code"
        elif state == "block":
            if current == "*" and following == "/":
                state = "code"
                index += 1
        elif state == "verbatim":
            if current == '"' and following == '"':
                index += 1
            elif current == '"':
                state = "code"
        else:
            if current == "\\":
                index += 1
            elif current == ('"' if state == "string" else "'"):
                state = "code"
        index += 1
    return len(stack) == 0 and state in {"code", "line"}


def check_game_signatures(results: dict, directory: Path | None) -> None:
    """Optional source-only contract lookup when an extracted game source is provided."""
    if directory is None:
        return
    required = {
        "save_pipeline": ("Assets.Source.Util/SaveGame.cs", ["SaveCurrentState", "LoadState"]),
        "player": ("Assets.Source.Player/GamePlayer.cs", ["CreateNewPlayer"]),
        "native_flow": ("Assets.Behaviour.Flow/WorldFlowManager.cs", ["CreateNewElement", "RemoveElement", "ShowFlow"]),
        "item_pipes": ("Assets.Source.Flow/FlowDiagram.cs", ["DisconnectPipe"]),
    }
    for label, (relative, methods) in required.items():
        path = directory / relative
        if not path.is_file():
            check(results, label, False, "Missing source: " + str(path))
            continue
        code = path.read_text(encoding="utf-8-sig")
        match = all(re.search(r"\b" + re.escape(m) + r"\s*\(", code) for m in methods)
        check(results, label, match, str(path))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-source", type=Path, default=None)
    args = parser.parse_args()
    results = {}
    files = sorted(SOURCE.rglob("*.cs"))
    check(results, "organized_code_exists", bool(files), f"{len(files)} files")
    check(results, "legacy_code_removed", not list((ROOT / "Api").glob("*.cs")) and
          not list((ROOT / "Plugin").glob("*.cs")))
    unbalanced = [str(f.relative_to(ROOT)) for f in files if not has_balanced_braces(f.read_text(encoding="utf-8-sig"))]
    check(results, "csharp_brackets", not unbalanced, ", ".join(unbalanced) if unbalanced else "balanced")
    project = (ROOT / "Foundation.csproj").read_text(encoding="utf-8-sig")
    check(results, "single_recursive_compile_glob", '<Compile Include="src\\Foundation\\**\\*.cs" />' in project and
          '<Compile Include="Api\\*.cs"' not in project and '<Compile Include="Plugin\\*.cs"' not in project)
    check(results, "required_unity_references", all(ref in project for ref in
          ("UnityEngine.IMGUIModule.dll", "UnityEngine.TextRenderingModule.dll")))
    project_version = ET.fromstring(project).findtext("./PropertyGroup/Version")
    plugin_source = (SOURCE / "Bootstrap" / "FoundationPlugin.FoundationPlugin.cs").read_text(encoding="utf-8-sig")
    check(results, "framework_version", f'Version = "{project_version}"' in plugin_source, project_version or "unknown")
    for feature, names in {
        "core_plugin": ("Core/Runtime/FoundationCorePlugin.cs", "Core/Runtime/FoundationCoreApi.cs"),
        "gameplay_plugin": ("Bootstrap/FoundationPlugin.FoundationPlugin.cs", "Gameplay/GameMachinesService.cs"),
        "signals": ("Signals/SignalRegistry.cs",),
        "persistence": ("Persistence/ModSaveRegistry.cs",),
        "public_api": ("Public/FoundationApi.cs",),
    }.items():
        check(results, feature, all((SOURCE / name).is_file() for name in names), ", ".join(names))
    launchers = {p.name for p in ROOT.glob("*.cmd")}
    check(results, "single_root_launcher", "INSTALL-FOUNDATION.cmd" in launchers and
          launchers.issubset({"INSTALL-FOUNDATION.cmd", "EXPORT-FOUNDATION-SOURCE.cmd", "UI-FRAMEWORK-TEST.cmd", "RUN-ALL-TESTS.cmd"}),
          ", ".join(sorted(launchers)))
    check(results, "no_one_off_scripts", not any((ROOT / name).exists() for name in
          ("Organize-Foundation.cmd", "VERIFY-INSTALLED.cmd", "verify-installed.ps1", "build-ready.ps1")))
    required_build_scripts = (
        "Tools/Build/Build-Foundation.ps1", "Tools/Build/Package-Foundation.ps1",
        "Tools/Install/Install-Foundation.ps1", "Tools/Install/Deploy-Foundation.ps1",
    )
    check(results, "installer_targets_exist", all((ROOT / path).is_file() for path in required_build_scripts))
    check_game_signatures(results, load_game_dir(args.game_source))
    passed = all(value["passed"] for value in results.values())
    print(json.dumps({"version": project_version, "static_only": True, "passed": passed,
                      "source_files": len(files), "checks": results}, indent=2))
    return 0 if passed else 1


if __name__ == "__main__":
    sys.exit(main())
