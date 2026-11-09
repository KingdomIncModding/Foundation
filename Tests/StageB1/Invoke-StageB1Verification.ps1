[CmdletBinding()]
param(
  [ValidateSet('Preflight','BuildInstall','Report','Remove')][string]$Action = 'Preflight',
  [string]$GameRoot = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$foundationRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = [IO.Path]::GetFullPath((Join-Path $foundationRoot '..\..')) }
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
$foundation = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
$pluginDir = Join-Path $GameRoot 'BepInEx\plugins\Foundation.StageB1Verification'
$plugin = Join-Path $pluginDir 'Foundation.StageB1Verification.dll'
$report = Join-Path $GameRoot '_Mods\Foundation\Verification\StageB1\stage-b1-verification.json'
$project = Join-Path $PSScriptRoot 'Foundation.StageB1Verification.csproj'
if ($Action -eq 'Remove') {
  if (Get-Process KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc first.' }
  if (Test-Path -LiteralPath $plugin) { Remove-Item -LiteralPath $plugin -Force }
  Write-Host '[PASS] Independent Stage B1 verifier removed. Production Foundation unchanged.' -ForegroundColor Green
  exit 0
}
foreach($file in @($foundation,(Join-Path $GameRoot 'KingdomInc_Data\Managed\Assembly-CSharp.dll'),(Join-Path $GameRoot 'BepInEx\core\BepInEx.dll'))) {
  if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing installed game dependency: $file" }
}
if ($Action -eq 'Preflight') { Write-Host '[PASS] Actual installed Foundation and game assemblies available.';exit 0 }
if ($Action -eq 'BuildInstall') {
  if (Get-Process KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc first.' }
  if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'The .NET SDK is required.' }
  $gameArg = '-p:GameRoot=' + $GameRoot.Replace('\','/')
  $dllArg = '-p:FoundationDll=' + $foundation.Replace('\','/')
  & dotnet build $project -c Release --nologo $gameArg $dllArg
  if ($LASTEXITCODE -ne 0) { throw 'Stage B1 independent verifier compilation failed.' }
  $built = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Foundation.StageB1Verification.dll'
  if (!(Test-Path -LiteralPath $built)) { throw 'Freshly built verifier DLL missing.' }
  New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
  if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report -Force }
  Copy-Item -LiteralPath $built -Destination $plugin -Force
  if ((Get-FileHash -LiteralPath $built).Hash -ne (Get-FileHash -LiteralPath $plugin).Hash) { throw 'Installed verifier hash mismatch.' }
  Write-Host '[PASS] Installed independent ONE-SHOT B1 verifier. Launch Kingdom Inc, then exit and choose option 3.' -ForegroundColor Green
  Write-Host 'This verifier never edits native game state, never runs an Update loop and produces only one report.'
  exit 0
}
if (!(Test-Path -LiteralPath $plugin)) { throw 'Stage B1 verifier DLL not installed.' }
if (!(Test-Path -LiteralPath $report)) { throw 'No Stage B1 report yet; launch the game first.' }
$body = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$version=[Reflection.AssemblyName]::GetAssemblyName($foundation).Version.ToString()
$hash=(Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash
if ([string]$body.foundationVersion -ne $version) { throw 'Stale report: Foundation version differs.' }
if ([string]$body.foundationSha256 -ne $hash) { throw 'Stale report: Foundation SHA256 differs.' }
if ((Get-Item $report).LastWriteTimeUtc -le (Get-Item $plugin).LastWriteTimeUtc) { throw 'Stale report: run the game again.' }
$pass=@($body.checks | Where-Object {$_.result -eq 'PASS'})
$fail=@($body.checks | Where-Object {$_.result -eq 'FAIL'})
foreach($check in $body.checks) {
  $color=if ($check.result -eq 'PASS') {'Green'} else {'Red'}
  Write-Host ('['+$check.result+'] '+$check.id+': '+$check.details) -ForegroundColor $color
}
Write-Host "B1 RESULTS: PASS=$($pass.Count), FAIL=$($fail.Count)"
if ($fail.Count -gt 0 -or $pass.Count -eq 0) { throw 'Stage B1 runtime verification did not pass.' }
Write-Host '[PASS] One-shot Stage B1 runtime test passed. Use option 4 to remove the testing DLL.' -ForegroundColor Green
