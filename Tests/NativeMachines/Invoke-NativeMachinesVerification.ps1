[CmdletBinding()]
param(
  [ValidateSet('BuildInstall','Report','Remove','Preflight')][string]$Action = 'Preflight',
  [string]$GameRoot = ''
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$sourceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot=[IO.Path]::GetFullPath((Join-Path $sourceRoot '..\..')) }
$GameRoot=[IO.Path]::GetFullPath($GameRoot)
$foundation=Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
$project=Join-Path $PSScriptRoot 'Foundation.NativeMachinesVerification.csproj'
$testDll=Join-Path $GameRoot 'BepInEx\plugins\Foundation.NativeMachinesVerification\Foundation.NativeMachinesVerification.dll'
$report=Join-Path $GameRoot 'BepInEx\plugins\Foundation.NativeMachinesVerification\Reports\native-a1-machines.json'
$log=Join-Path $GameRoot 'BepInEx\LogOutput.log'
if ($Action -eq 'Remove') {
  if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before removing the development-only verifier.' }
  if (Test-Path -LiteralPath $testDll) { Remove-Item -LiteralPath $testDll -Force; Write-Host '[PASS] Development-only test DLL removed.' -ForegroundColor Green }
  else { Write-Host '[INFO] Development-only test DLL is not installed.' }
  exit 0
}
foreach ($item in @($foundation,(Join-Path $GameRoot 'KingdomInc_Data\Managed\Assembly-CSharp.dll'),(Join-Path $GameRoot 'BepInEx\core\BepInEx.dll'))) {
  if (!(Test-Path -LiteralPath $item -PathType Leaf)) { throw "Missing actual game/Foundation DLL: $item" }
}
Write-Host '[INFO] Foundation DLL:' $foundation
Write-Host '[INFO] Foundation SHA256:' (Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash
if ($Action -eq 'Preflight') { Write-Host '[PASS] Actual installed Foundation/game references found.'; exit 0 }
if ($Action -eq 'BuildInstall') {
  if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc first.' }
  if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK required to compile independent test consumer.' }
  $gameArg='-p:GameRoot=' + $GameRoot.TrimEnd('\','/').Replace('\','/') + '/'
  $dllArg='-p:FoundationDll=' + $foundation.Replace('\','/')
  & dotnet build $project -c Release --nologo $gameArg $dllArg
  if ($LASTEXITCODE -ne 0) { throw 'Native Machine A1 verifier build FAILED. Nothing installed.' }
  $built=Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Foundation.NativeMachinesVerification.dll'
  if (!(Test-Path -LiteralPath $built -PathType Leaf)) { throw 'Missing freshly built verifier DLL.' }
  $dir=Split-Path -Parent $testDll
  New-Item -ItemType Directory -Path $dir -Force | Out-Null
  if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report -Force }
  Copy-Item -LiteralPath $built -Destination $testDll -Force
  if ((Get-FileHash -LiteralPath $testDll).Hash -ne (Get-FileHash -LiteralPath $built).Hash) { throw 'Installed verifier hash differs from freshly built DLL.' }
  Write-Host '[PASS] Independent verification mod installed from actual game assemblies.' -ForegroundColor Green
  Write-Host 'Start Kingdom Inc. Enter a disposable kingdom and place every available native machine type.'
  Write-Host 'A1.Type checks pass only when the actual class is placed and inspected. Non-registered classes do not create pending checks.'
  Write-Host 'No mutation flags are needed: the A1 verifier is read-only; create machine types as part of normal gameplay.' -ForegroundColor Yellow
  Write-Host 'After quitting, run Report. The report will show PENDING until actual scenarios occur.'
  exit 0
}
if (!(Test-Path -LiteralPath $testDll -PathType Leaf)) { throw 'Verifier test DLL is not installed. Run BuildInstall.' }
if (!(Test-Path -LiteralPath $report -PathType Leaf)) { Write-Host '[PENDING] No native Game API report yet; launch the game and enter a kingdom.';exit 2 }
$runtime=(Get-Item -LiteralPath $foundation).LastWriteTimeUtc
$created=(Get-Item -LiteralPath $report).LastWriteTimeUtc
$installed=(Get-Item -LiteralPath $testDll).LastWriteTimeUtc
if ($created -le $runtime -or $created -le $installed) { throw 'Stale report predates Foundation or test DLL. Launch a fresh game session.' }
if (!(Test-Path -LiteralPath $log -PathType Leaf) -or (Get-Item -LiteralPath $log).LastWriteTimeUtc -le $installed) { throw 'Fresh BepInEx startup log missing.' }
$expectedVersion=[Reflection.AssemblyName]::GetAssemblyName($foundation).Version.ToString()
$expectedHash=(Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash
$body=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
if ([string]$body.foundationVersion -ne $expectedVersion) { throw "Report assembly version $($body.foundationVersion) does not match installed $expectedVersion." }
if ([string]$body.foundationSha256 -ne $expectedHash) { throw 'Report Foundation binary SHA256 does not match current installed DLL.' }
$marker=Select-String -LiteralPath $log -Pattern 'Loading [Foundation Native A1 Machine Verification' -SimpleMatch -Quiet
if (-not $marker) { throw 'Current BepInEx startup log has no Native Machine A1 verification plugin loading marker.' }
$pass=@($body.checks | Where-Object {$_.result -eq 'PASS'})
$fail=@($body.checks | Where-Object {$_.result -eq 'FAIL'})
$pending=@($body.checks | Where-Object {$_.result -eq 'PENDING'})
Write-Host ("Native Machine A1 VERIFY: PASS={0} FAIL={1} PENDING={2}; worldLoads={3}; registeredNativeTypes={4}" -f $pass.Count,$fail.Count,$pending.Count,$body.worldLoads,$body.registeredNativeTypes)
foreach($check in $body.checks) {
  $color=if ($check.result -eq 'PASS') {'Green'} elseif ($check.result -eq 'FAIL') {'Red'} else {'Yellow'}
  Write-Host ('['+$check.result+'] '+$check.id+': '+$check.details) -ForegroundColor $color
}
if ($fail.Count -gt 0) {throw "Native Machine A1 report has $($fail.Count) failed scenarios."}
if ($pending.Count -gt 0) {Write-Host '[PENDING] Not 100% runtime verified. Exercise remaining scenarios in a disposable kingdom.' -ForegroundColor Yellow; exit 2}
Write-Host '[PASS] All registered native machine types independently inspected on current installed Foundation.' -ForegroundColor Green
