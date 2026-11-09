# Read-only Gate A evidence collection. No game actions, no verifier installs,
# no file edits to saves, and no mutation launch arguments. Safe after cleanup.
[CmdletBinding()]
param([string]$GameRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot=[IO.Path]::GetFullPath((Join-Path $projectRoot '..\..')) }
$GameRoot=[IO.Path]::GetFullPath($GameRoot)
$foundation=Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
$native=Join-Path $GameRoot 'KingdomInc_Data\Managed\Assembly-CSharp.dll'
foreach ($file in @($foundation,$native)) {
    if (!(Test-Path -LiteralPath $file -PathType Leaf)) {throw "Required actual installed assembly not found: $file"}
}
$foundationHash=(Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash
$gameHash=(Get-FileHash -LiteralPath $native -Algorithm SHA256).Hash
$version=[Reflection.AssemblyName]::GetAssemblyName($foundation).Version.ToString()
$cases=@(
    @{Stage='A1';Id='Native Machines';Report='BepInEx\plugins\Foundation.NativeMachinesVerification\Reports\native-a1-machines.json';Plugin='Foundation.NativeMachinesVerification'},
    @{Stage='A2';Id='Native IO';Report='BepInEx\plugins\Foundation.NativeIOVerification\Reports\nativeio-verification.json';Plugin='Foundation.NativeIOVerification'},
    @{Stage='A3-A7';Id='Game API';Report='BepInEx\plugins\Foundation.NativeGameApiVerification\Reports\gameapi-a3-a7.json';Plugin='Foundation.NativeGameApiVerification'}
)
$results=@()
foreach ($item in $cases) {
    $path=Join-Path $GameRoot $item.Report
    $pluginFile=Join-Path (Join-Path $GameRoot 'BepInEx\plugins') ($item.Plugin+'\'+$item.Plugin+'.dll')
    $entry=[ordered]@{
        Stage=$item.Stage; Name=$item.Id; ReportPresent=(Test-Path -LiteralPath $path -PathType Leaf)
        VerifierInstalled=(Test-Path -LiteralPath $pluginFile -PathType Leaf)
        ReportPath=$path; Result='MISSING';Pass=0;Fail=0;Pending=0;Notes='No report available: user may have removed the development verifier.'
    }
    if ($entry.ReportPresent) {
        try {
            $data=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
            $counts=@($data.checks)
            $entry.Pass=@($counts|Where-Object {$_.result -eq 'PASS'}).Count
            $entry.Fail=@($counts|Where-Object {$_.result -eq 'FAIL'}).Count
            $entry.Pending=@($counts|Where-Object {$_.result -eq 'PENDING'}).Count
            $sameFoundation=($data.foundationSha256 -eq $foundationHash)
            $sameVersion=($data.foundationVersion -eq $version)
            # A1 and A2 verifier JSON never included the game-assembly SHA;
            # only A3-A7 writes it. Read optional fields safely in PS 5 StrictMode.
            $hasGameHash=($null -ne $data.PSObject.Properties['gameAssemblySha256'])
            $sameGame=if ($hasGameHash) { [string]$data.gameAssemblySha256 -eq $gameHash } else { $false }
            $wrongGame=($hasGameHash -and !$sameGame)
            $entry.Result=if (!$sameFoundation -or !$sameVersion -or $wrongGame) {'STALE_OR_WRONG_DLL'} elseif ($entry.Fail -ne 0) {'FAIL'} elseif ($entry.Pending -ne 0) {'PENDING'} elseif ($entry.Pass -eq 0) {'EMPTY'} elseif (!$hasGameHash) {'PASS_FOUNDATION_BOUND'} else {'PASS'}
            $entry.Notes="Foundation/version match: $sameFoundation/$sameVersion. Game DLL hash: $(if ($hasGameHash) {$sameGame} else {'not recorded by this verifier'}). Passing only proves the named scenarios."
        } catch {
            $entry.Result='INVALID_REPORT'; $entry.Notes=$_.Exception.Message
        }
    }
    $results+=New-Object psobject -Property $entry
}
$installedVerifiers=@()
$plugins=Join-Path $GameRoot 'BepInEx\plugins'
if (Test-Path -LiteralPath $plugins) {
    $installedVerifiers=@(Get-ChildItem -LiteralPath $plugins -Recurse -File -Filter '*Verification*.dll' -ErrorAction SilentlyContinue |
        Where-Object {$_.Name -ne 'Foundation.dll'} | ForEach-Object {$_.FullName})
}
$summary=[ordered]@{
    Status='EVIDENCE_SNAPSHOT_ONLY_NOT_AUTOMATIC_GATE_APPROVAL'
    GeneratedUtc=(Get-Date).ToUniversalTime().ToString('o')
    GameRoot=$GameRoot
    InstalledFoundationVersion=$version
    FoundationSha256=$foundationHash
    InstalledGameAssemblySha256=$gameHash
    GameIsRunning=[bool](Get-Process -Name KingdomInc -ErrorAction SilentlyContinue)
    VerificationDllsStillPresent=$installedVerifiers
    StageReports=$results
    UserAttestation='Prior chat: user reported all A1-A7 tests passed and FPS restored after removing old test DLLs.'
    Limitation='This script is READ ONLY. It cannot recover deleted reports or prove untested public API operations.'
}
$outDir=Join-Path $projectRoot 'Verification\GateA'
New-Item -ItemType Directory -Force -Path $outDir|Out-Null
$out=Join-Path $outDir 'gate-a-installed-evidence.json'
$summary|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $out -Encoding UTF8
Write-Host ('Game Assembly SHA256: '+$gameHash)
Write-Host ('Foundation '+$version+' SHA256: '+$foundationHash)
foreach ($entry in $results) {Write-Host ('[{0}] {1}: PASS={2}, FAIL={3}, PENDING={4}; {5}' -f $entry.Result,$entry.Stage,$entry.Pass,$entry.Fail,$entry.Pending,$entry.Notes)}
if ($installedVerifiers.Count -gt 0) {
    Write-Warning ('Found '+$installedVerifiers.Count+' test DLL(s). Avoid stacking old verifiers: this previously caused severe FPS drops.')
}
Write-Host ('Read-only Gate A snapshot written to '+$out)
if (@($results | Where-Object {$_.Result -ne 'PASS' -and $_.Result -ne 'PASS_FOUNDATION_BOUND'}).Count -gt 0) {
    Write-Host '[PENDING] Evidence incomplete. This does not invalidate the earlier user-reported passes.' -ForegroundColor Yellow
}
