#!/usr/bin/env python3
"""Offline check of original game-source method contracts and supplied managed DLL ZIP.

This deliberately does NOT claim a C# compilation or Unity runtime pass.
Run with --game-source <extracted decompiled Source> --game-data <KingdomInc_Data.zip>.
"""
import argparse
import json
import re
import sys
import zipfile
from pathlib import Path

args = argparse.ArgumentParser()
args.add_argument('--game-source', type=Path, required=True)
args.add_argument('--game-data', type=Path, required=True)
args.add_argument('--report', type=Path)
opt = args.parse_args()

contracts = {
    'Assets.Source.Util/SaveGame.cs': [
        r'public\s+static\s+JsonObject\s+SaveCurrentState\s*\(\s*\)',
        r'public\s+static\s+void\s+LoadState\s*\(\s*JsonObject\s+data\s*\)',
    ],
    'Assets.Source.Player/GamePlayer.cs': [
        r'public\s+static\s+GamePlayer\s+CreateNewPlayer\s*\(\s*\)',
        r'public\s+static\s+GamePlayer\s+FromJson\s*\(\s*JsonValue\s+json\s*\)',
        r'public\s+JsonValue\s+ToJson\s*\(\s*\)',
    ],
    'Assets.Source.Flow/FlowElement.cs': [
        r'public\s+static\s+FlowElement\s+Create\s*\(\s*string\s+name\s*\)',
        r'public\s+static\s+FlowElement\s+FromJson\s*\(\s*JsonValue\s+data\s*,\s*FlowDiagram\s+parent\s*\)',
    ],
    'Assets.Source.Flow/FlowPipe.cs': [
        r'public\s+void\s+Connect\s*\(\s*FlowOutput\s+output\s*,\s*FlowInput\s+input\s*,\s*bool\s+ghost\s*=\s*false\s*\)',
        r'public\s+void\s+Update\s*\(\s*float\s+delta\s*\)',
    ],
    'Assets.Source.Flow/FlowDiagram.cs': [
        r'public\s+void\s+DisconnectPipe\s*\(\s*FlowPipe\s+pipe\s*\)',
    ],
    'Assets.Behaviour.Flow/WorldFlowPipe.cs': [
        r'public\s+void\s+ItemTransferred\s*\(\s*InventoryItem\s+item\s*\)',
    ],
    'Assets.Behaviour.Flow/WorldFlowManager.cs': [
        r'public\s+void\s+CreateNewPipe\s*\(\s*FlowPipe\s+pipe\s*\)',
        r'public\s+void\s+RemoveElement\s*\(\s*FlowElement\s+element\s*\)',
        r'public\s+void\s+ShowFlow\s*\(\s*int\s+idx\s*\)',
    ],
    'Assets.Behaviour/GameManager.cs': [
        r'private\s+void\s+Start\s*\(\s*\)',
        r'private\s+void\s+OnApplicationQuit\s*\(\s*\)',
    ],
}

findings = {}
for filename, patterns in contracts.items():
    path = opt.game_source / filename
    if not path.is_file():
        findings[filename] = {'passed': False, 'missing_file': True}
        continue
    text = path.read_text(encoding='utf-8-sig')
    missing = [pattern for pattern in patterns if not re.search(pattern, text)]
    findings[filename] = {'passed': not missing, 'checked_signatures': len(patterns), 'missing': missing}

with zipfile.ZipFile(opt.game_data) as z:
    names = set(z.namelist())
    expected = [
        'Assembly-CSharp.dll', 'UnityEngine.dll', 'UnityEngine.CoreModule.dll',
        'UnityEngine.UIModule.dll', 'UnityEngine.UI.dll',
        'UnityEngine.Physics2DModule.dll', 'Unity.InputSystem.dll',
        'Unity.TextMeshPro.dll', 'UnityEngine.AssetBundleModule.dll',
        'UnityEngine.ImageConversionModule.dll', 'UnityEngine.UnityWebRequestModule.dll',
    ]
    missing_dlls = [s for s in expected if f'KingdomInc_Data/Managed/{s}' not in names]

report = {
    'check': 'offline game source signatures + provided game DLL inventory',
    'compiled': False,
    'gameplay_tested': False,
    'contracts': findings,
    'managed_references_checked': len(expected),
    'missing_managed_references': missing_dlls,
    'passed': not missing_dlls and all(x['passed'] for x in findings.values()),
}
if opt.report:
    opt.report.parent.mkdir(parents=True, exist_ok=True)
    opt.report.write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
sys.exit(0 if report['passed'] else 1)
