[CmdletBinding()]
param([string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = Join-Path $root '..\..' }
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
$plugin = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
$testDll = Join-Path $GameRoot 'BepInEx\plugins\Foundation.MachineVerification\Foundation.MachineVerification.dll'
$report = Join-Path $GameRoot 'BepInEx\plugins\Foundation.MachineVerification\Reports\machine-verification.json'
foreach ($p in @($plugin,$testDll,$report)) {if (!(Test-Path -LiteralPath $p)) {throw "File missing: $p"}}
$latestDll = @(Get-Item -LiteralPath $plugin,$testDll | Sort-Object LastWriteTimeUtc -Descending)[0]
if ((Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $latestDll.LastWriteTimeUtc) { throw 'Test report predates installed DLL; rerun game tests.' }
$data = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$pass = @($data.checks | Where-Object { $_.result -eq 'PASS' }).Count
$fail = @($data.checks | Where-Object { $_.result -eq 'FAIL' }).Count
$pending = @($data.checks | Where-Object { $_.result -eq 'PENDING' }).Count
Write-Host "MACHINE VERIFICATION: pass=$pass fail=$fail pending=$pending; worldLoads=$($data.worldLoads); worldUnloads=$($data.worldUnloads); machinesSeen=$($data.machinesSeen); transfers=$($data.transfers)"
foreach ($item in $data.checks) { Write-Host "[$($item.result)] $($item.id): $($item.details)" }
if ($pass -ne 26 -or $fail -gt 0 -or $pending -gt 0) { throw 'Unified machine API is NOT fully verified yet.' }
Write-Host 'PASS: all 26 independent machine verification scenarios completed on unified Foundation.'
