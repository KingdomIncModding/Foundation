#!/usr/bin/env python3
"""Source-only Gate B invariants; does not claim native Windows/Unity verification.

Usage: python Tests/GateB/verify_gate_b.py [--baseline-archive path/to/0.10.6.zip]
This is deliberately independent of Unity and of developer-owned save files.
"""
from pathlib import Path
from zipfile import ZipFile
from argparse import ArgumentParser
import hashlib, json, sys

root = Path(__file__).resolve().parents[2]
p = ArgumentParser()
p.add_argument('--baseline-archive', type=Path)
p.add_argument('--report', type=Path)
args = p.parse_args()
audit = json.loads((root/'Docs/GateB/Gate-B-Source-Compatibility-Audit.json').read_text('utf-8'))
freeze = json.loads((root/'Docs/GameAPI/GateA/Game-API-Freeze-Candidate-Hashes.json').read_text('utf-8'))
coverage = json.loads((root/'Docs/StageB/Runtime-Smoke-Coverage.json').read_text('utf-8-sig'))
checks = {}

def check(name, yes, reason=''):
    checks[name] = {'pass':bool(yes), 'reason':reason}
    print(('PASS' if yes else 'FAIL'),name,reason)

now={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in (root/'src/Foundation').rglob('*.cs')}
check('all-24-ids',len(audit['workstreams'])==24 and
      {x['id'] for x in audit['workstreams']}=={f'R{i:02d}' for i in range(1,25)})
check('no-removed-csharp',not audit['removed'] and not(set(freeze['files'])-set(now)))
check('native-implementation-baseline-preserved',not audit['nativeIntrinsicImplementationChanges'])
expected_changed=set(audit['modified']);added=set(audit['added'])
check('modified-files-exist',all(n in now for n in expected_changed))
check('added-files-exist',all(n in now for n in added))
check('modified-file-digest-locked',all(now.get(n)==h for n,h in audit['modifiedHashes'].items()))
check('added-file-digest-locked',all(now.get(n)==h for n,h in audit['addedHashes'].items()))
check('all-unmodified-file-hashes-unchanged', all(now.get(n)==h for n,h in audit['unchangedHashes'].items()),
      f"{len(audit['unchangedHashes'])} source files")
check('original-source-file-coverage',len(set(audit['unchangedHashes'])|expected_changed|added)==len(now))
check('sources-and-review-coverage',all((root/x['primarySource']).is_file() and
      (root/x['secondarySource']).is_file() for x in audit['workstreams']))
check('all-24-have-test-ids',all(x['testEvidence'] for x in audit['workstreams']) and
      {x['id'] for x in audit['workstreams']}=={x['id'] for x in coverage['workstreams']})
check('b6-rewrite-policy',set(x['id'] for x in audit['workstreams'] if x['stage']=='B6')=={'R05','R14','R19','R23','R24'} and
      'REWRITE' in (root/'Docs/GateB/Gate-B-Compatibility-And-Decision-Record.md').read_text('utf-8'))
check('migration-risks-documented',
      all(x in (root/'Docs/StageB/Mod-Migration-Guide.md').read_text('utf-8-sig') for x in
          ['IFoundationCore','ICoreModDirectory','ICoreServiceDirectory','save/reload','R23','R24']))
runner=(root/'Tests/FullSuite/Run-FoundationFullSuite.ps1').read_text('utf-8-sig')
check('full-suite-regression-fixes',all(x in runner for x in
      ['$script:abort=$false','StaleReport','B4.StaleInteractiveReport',
       '$safeName=($test -replace','Recover-ConsoleConfig\nRequire-Game']))
check('one-production-project', (root/'Foundation.csproj').is_file() and
      len(list((root/'src').rglob('*.csproj')))==0)

if args.baseline_archive:
    with ZipFile(args.baseline_archive) as z:
        previous={n[len('Foundation/'):]:hashlib.sha256(z.read(n)).hexdigest() for n in z.namelist()
                  if n.startswith('Foundation/src/Foundation/') and n.endswith('.cs')}
    check('baseline-archive-equals-freeze',previous==freeze['files'])
    check('full-diff-correct',
          {n for n in now if n not in previous}==added and
          {n for n in now if n in previous and now[n]!=previous[n]}==expected_changed and
          not(set(previous)-set(now)))
else:
    check('baseline-archive-optional',True,'Pass --baseline-archive for independent full source diff.')
report={'kind':'static_only','pass':all(x['pass'] for x in checks.values()),'checks':checks,
        'runtimeEvidence':'NOT_VERIFIED_IN_THIS_SCRIPT',
        'note':'A successful static audit is not Gate B release approval or proof of external-mod binary compatibility.'}
if args.report:
    args.report.parent.mkdir(parents=True,exist_ok=True)
    args.report.write_text(json.dumps(report,indent=2)+'\n','utf-8')
print(f"Gate B offline source checks: {sum(x['pass'] for x in checks.values())}/{len(checks)} PASS")
sys.exit(0 if report['pass'] else 1)
