#!/usr/bin/env python3
"""Read-only offline audit of the user-provided KingdomInc_Data.zip and Foundation projects."""
import argparse, zipfile, pathlib, hashlib, re, json, sys
parser = argparse.ArgumentParser()
parser.add_argument('--game-data', required=True, type=pathlib.Path)
parser.add_argument('--source', type=pathlib.Path, default=pathlib.Path(__file__).resolve().parents[1])
args = parser.parse_args()
with zipfile.ZipFile(args.game_data) as z:
    files = set(z.namelist())
    managed = {pathlib.PurePosixPath(x).name:x for x in files if x.startswith('KingdomInc_Data/Managed/') and x.endswith('.dll')}
    expected = set()
    project_counts={}
    for project in [args.source/'Foundation.csproj',*sorted((args.source/'Examples').glob('*/*.csproj'))]:
        data = project.read_text(encoding='utf-8')
        refs = re.findall(r'(?i)KingdomInc_Data[\\/]Managed[\\/]([^<\\/]+\.dll)', data)
        expected.update(refs)
        project_counts[str(project.relative_to(args.source))]=len(refs)
    missing = sorted(expected-managed.keys())
    main_hash = hashlib.sha256(z.read(managed['Assembly-CSharp.dll'])).hexdigest() if 'Assembly-CSharp.dll' in managed else None
    report={'archive':args.game_data.name,'managed_dll_count':len(managed), 'game_dll_sha256':main_hash,'project_reference_counts':project_counts,'required_game_references':sorted(expected),'missing_references':missing,'static_dependency_check_passed':not missing}
    print(json.dumps(report,indent=2))
    sys.exit(1 if missing else 0)
