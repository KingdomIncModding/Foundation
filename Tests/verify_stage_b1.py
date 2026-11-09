#!/usr/bin/env python3
"""Stage B1 source boundary / migration checks. Static-only, not a C# runtime audit."""
import hashlib
import json
import re
from pathlib import Path
from collections import Counter

root = Path(__file__).resolve().parents[1]
source = root/'src'/'Foundation'
checks = []
# B1's historical source-freeze assertions are scoped to the 0.10.7 B1-only
# package. In 0.11+, later authorized Stage B work intentionally changes the
# same files; verify_stage_b_all.py compares legacy interfaces and native code.
import re as _re
project=(root/'Foundation.csproj').read_text()
_version_match=_re.search(r'<Version>([^<]+)</Version>',project)
_b1_source_scope=(_version_match is None or tuple(int(i) for i in _version_match.group(1).split('.')[:3]) <= (0,10,7))
def read(rel):return (source/rel).read_text(encoding='utf-8-sig')
def check(label, predicate, details=''):
    checks.append((label, bool(predicate), details))

freeze=json.loads((root/'Docs'/'GameAPI'/'GateA'/'Game-API-Freeze-Candidate-Hashes.json').read_text())
changed=[]
for path, digest in freeze['files'].items():
    file=root/path
    if not file.exists() or hashlib.sha256(file.read_bytes()).hexdigest()!=digest:
        changed.append(path)
allowed = ('src/Foundation/Core/','src/Foundation/Api/FoundationEventBus.cs','src/Foundation/Integration/Nested/Entry.cs','src/Foundation/Bootstrap/FoundationPlugin.FoundationPlugin.cs')
check('gate-a-native-unchanged', not _b1_source_scope or all(x.startswith(allowed[0]) or x in allowed[1:] for x in changed), 'Stage B1 historical freeze only' if not _b1_source_scope else repr(changed))
check('b1-has-source-changes', len(changed)>=5, str(len(changed)))

check('core-same-singleton', 'private static readonly FoundationCore Instance = new FoundationCore()' in read('Core/Runtime/FoundationCoreApi.cs'))
check('core-mod-directory', all(t in read('Core/Runtime/CoreModDirectory.cs') for t in ('modules.Registered','plugins.Plugins','unique.Values','new ModDirectoryEntry')))
check('core-no-secondary-mod-authority', 'new Dictionary<string, ModRegistration>' not in read('Core/Runtime/CoreModDirectory.cs'))
check('core-visible-entrypoints', all(t in read('Core/Contracts/IFoundationCore.cs') for t in ('ICoreModDirectory Mods','ICoreServiceDirectory ServiceDirectory','ICoreModules Modules','ICoreServices Services','ICoreEventBus Events')))
check('service-discovery-same-registry', 'CoreServiceRegistry : ICoreServices, ICoreServiceDirectory' in read('Core/Services/CoreServiceRegistry.cs'))
check('service-discovery-type-version', all(t in read('Core/Services/CoreServiceRegistry.cs') for t in ('x.Info.ContractType == typeof(T)','x.Info.OwnerId == providerId','x.Info.Version >= minimumVersion')))
check('provider-ambiguous-resolution-rejected', 'matches.Length != 1' in read('Core/Services/CoreServiceRegistry.cs'))
check('service-lifecycle-owner', all(t in read('Core/Services/CoreServiceRegistry.cs') for t in ('ReleaseOwner','ReleaseWorld','ReleaseAll')))
check('legacy-event-delegates-core', 'FoundationCoreApi.Core.Events.Publish(message)' in read('Api/FoundationEventBus.cs'))
check('legacy-subscriptions-same-core-bus', 'FoundationCoreApi.Core.Events.Subscribe(modId, handler)' in read('Api/FoundationEventBus.cs'))
check('legacy-no-private-event-dispatch', '((Action<T>)entry.Callback)(message)' not in read('Api/FoundationEventBus.cs'))
check('event-cleanup', 'token?.Dispose();' in read('Api/FoundationEventBus.cs') and 'Interlocked.Exchange(ref active, 0)' in read('Core/Events/CoreEventSubscription.cs'))
check('world-transition-before-init', read('Core/Modules/CoreModuleRegistry.cs').index('if (!ReferenceEquals(currentWorld, world)) ChangeWorld(world);') < read('Core/Modules/CoreModuleRegistry.cs').index('StartEligible();'))
check('dependent-closure', all(t in read('Core/Modules/CoreModuleRegistry.cs') for t in ('DependentClosure(modId)','HashSet<string> DependentClosure','affected.Contains(dependent.Descriptor.Id)','ModState.WaitingForDependencies')))
check('module-idempotent-unregister', 'item.State == ModState.Stopping || item.State == ModState.Disposed' in read('Core/Modules/CoreModuleRegistry.cs'))
check('descriptors-frozen', 'readonly ModDescriptor descriptor;' in read('Core/Modules/ModRegistration.cs'))
check('native-api-untouched', not _b1_source_scope or all(x not in changed for x in freeze['files'] if x.startswith(('src/Foundation/Gameplay/','src/Foundation/Machines/','src/Foundation/Workspace/'))))
check('stage-b-non-b1-untouched', not _b1_source_scope or all(x not in changed for x in freeze['files'] if x.startswith(('src/Foundation/Signals/','src/Foundation/UI/','src/Foundation/Research/','src/Foundation/Persistence/'))))
check('one-prod-dll-project', 'AssemblyName>Foundation<' in (root/'Foundation.csproj').read_text() and 'src\\Foundation\\**\\*.cs' in (root/'Foundation.csproj').read_text())
verifier=(root/'Tests'/'StageB1'/'StageB1VerificationPlugin.cs').read_text()
check('independent-one-shot', 'StartCoroutine(RunOnce())' in verifier and 'void Update(' not in verifier and 'File.WriteAllText(reportPath' in verifier)
check('b1-failure-detection', all(t in verifier for t in ('DependencyWait','ProviderRecovery','CrossModResolve','OneEventBus','SubscriberIsolation','UnexpectedException')))
check('installer-removable', 'Remove' in (root/'Tests'/'StageB1'/'Invoke-StageB1Verification.ps1').read_text())
print('\n'.join('[{}] {} {}'.format('PASS' if v else 'FAIL',k,d) for k,v,d in checks))
counts=Counter('PASS' if v else 'FAIL' for _,v,_ in checks)
print('Stage B1 source checks:',dict(counts))
raise SystemExit(1 if counts['FAIL'] else 0)
