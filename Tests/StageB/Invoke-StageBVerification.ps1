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
$pluginDir = Join-Path $GameRoot 'BepInEx\plugins\Foundation.StageBVerification'
$plugin = Join-Path $pluginDir 'Foundation.StageBVerification.dll'
$report = Join-Path $GameRoot '_Mods\Foundation\Verification\StageB\stage-b-verification.json'
$visualReport = Join-Path $GameRoot '_Mods\Foundation\Verification\StageB\stage-b-visual-verification.json'
$project = Join-Path $PSScriptRoot 'Foundation.StageBVerification.csproj'
if ($Action -eq 'Remove') {
  if (Get-Process KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc first.' }
  if (Test-Path -LiteralPath $plugin) { Remove-Item -LiteralPath $plugin -Force }
  Write-Host '[PASS] Independent Stage B verifier removed. Production Foundation unchanged.' -ForegroundColor Green
  exit 0
}
foreach($file in @($foundation,(Join-Path $GameRoot 'KingdomInc_Data\Managed\Assembly-CSharp.dll'),(Join-Path $GameRoot 'BepInEx\core\BepInEx.dll'))) {
  if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing installed game dependency: $file" }
}
if ($Action -eq 'Preflight') { Write-Host '[PASS] Actual installed Foundation and game assemblies available.';exit 0 }
if ($Action -eq 'BuildInstall') {
  $oldVerifiers = @(Get-ChildItem (Join-Path $GameRoot 'BepInEx\plugins') -Filter '*.dll' -File -Recurse -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match 'Verification|UISmoke' -and $_.Name -ne 'Foundation.StageBVerification.dll'
  })
  if ($oldVerifiers.Count -gt 0) {
    Write-Host '[WARNING] Other verification DLLs are installed. Remove completed test DLLs first to avoid severe FPS degradation:' -ForegroundColor Yellow
    foreach($dll in $oldVerifiers) { Write-Host ('  ' + $dll.FullName) }
    throw 'Please remove old verification DLLs and retry. No test DLL was installed.'
  }

  if (Get-Process KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc first.' }
  if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'The .NET SDK is required.' }
  $gameArg = '-p:GameRoot=' + $GameRoot.Replace('\','/')
  $dllArg = '-p:FoundationDll=' + $foundation.Replace('\','/')
  & dotnet build $project -c Release --nologo $gameArg $dllArg
  if ($LASTEXITCODE -ne 0) { throw 'Stage B independent verifier compilation failed.' }
  $built = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Foundation.StageBVerification.dll'
  if (!(Test-Path -LiteralPath $built)) { throw 'Freshly built verifier DLL missing.' }
  New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
  if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report -Force }
  if (Test-Path -LiteralPath $visualReport) { Remove-Item -LiteralPath $visualReport -Force }
  Copy-Item -LiteralPath $built -Destination $plugin -Force
  if ((Get-FileHash -LiteralPath $built).Hash -ne (Get-FileHash -LiteralPath $plugin).Hash) { throw 'Installed verifier hash mismatch.' }
  Write-Host '[PASS] Installed Stage B verifier with native UI interaction tests.' -ForegroundColor Green
  Write-Host 'Launch Kingdom Inc and LOAD A KINGDOM. Look below the currency HUD for STAGE B UI TEST.' -ForegroundColor Cyan
  Write-Host 'Use the top-center Mod Bar chevron if the bar is collapsed. Click the button and use the visible controls.' -ForegroundColor Cyan
  Write-Host 'Interact with the panel button, checkbox, dropdown and input; confirm the visual layout, exit, then choose option 3.' -ForegroundColor Cyan
  Write-Host 'The automated checks run once. The separate visual report is updated ONLY by your UI interactions, never every frame.'
  exit 0
}
if (!(Test-Path -LiteralPath $plugin)) { throw 'Stage B verifier DLL not installed.' }
if (!(Test-Path -LiteralPath $report)) { throw 'No Stage B report yet; launch the game first.' }
$body = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$version=[Reflection.AssemblyName]::GetAssemblyName($foundation).Version.ToString()
$hash=(Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash
if ([string]$body.foundationVersion -ne $version) { throw 'Stale report: Foundation version differs.' }
if ([string]$body.foundationSha256 -ne $hash) { throw 'Stale report: Foundation SHA256 differs.' }
if ((Get-Item $report).LastWriteTimeUtc -le (Get-Item $plugin).LastWriteTimeUtc) { throw 'Stale report: run the game again.' }
# The automated snapshot is intentionally one-shot. The separate visual file
# contains actual UI callbacks from the same temporary DLL. Reconcile the two
# B4/B6 pending IDs only when the signed-by-hash visual evidence is fresh.
$visualChecks = @()
if (Test-Path -LiteralPath $visualReport) {
  $visual = Get-Content -LiteralPath $visualReport -Raw | ConvertFrom-Json
  if ([string]$visual.foundationVersion -eq $version -and
      [string]$visual.foundationSha256 -eq $hash -and
      (Get-Item $visualReport).LastWriteTimeUtc -gt (Get-Item $plugin).LastWriteTimeUtc) {
    $visualChecks = @($visual.checks)
    if (-not [string]::IsNullOrWhiteSpace([string]$visual.warning)) {
      Write-Host ('[VISUAL WARNING] ' + $visual.warning) -ForegroundColor Yellow
    }
  } else {
    Write-Host '[WARNING] Ignoring stale or different-Foundation visual report.' -ForegroundColor Yellow
  }
}
foreach ($check in $body.checks) {
  if ($check.id -eq 'B4.RenderingVisualAudit' -or $check.id -eq 'B6.ModBarNativeVisual') {
    $verified = @($visualChecks | Where-Object { $_.id -eq $check.id -and $_.result -eq 'PASS' })
    if ($verified.Count -eq 1) {
      $check.result = 'PASS'
      $check.details = $verified[0].details + ' (native visual interaction report)'
    } elseif ($visualChecks.Count -gt 0) {
      $partial = @($visualChecks | Where-Object { $_.id -eq $check.id })
      if ($partial.Count -eq 1) { $check.details = $partial[0].details }
    }
  }
}
$pass=@($body.checks | Where-Object {$_.result -eq 'PASS'})
$fail=@($body.checks | Where-Object {$_.result -eq 'FAIL'})
$pending=@($body.checks | Where-Object {$_.result -eq 'PENDING'})
foreach($check in $body.checks) {
  $color=if ($check.result -eq 'PASS') {'Green'} elseif ($check.result -eq 'PENDING') {'Yellow'} else {'Red'}
  Write-Host ('['+$check.result+'] '+$check.id+': '+$check.details) -ForegroundColor $color
}
if ($visualChecks.Count -gt 0) {
  Write-Host '--- Actual Stage B on-screen UI interaction results ---' -ForegroundColor Cyan
  foreach ($v in $visualChecks) {
    $color = if ($v.result -eq 'PASS') {'Green'} elseif ($v.result -eq 'PENDING') {'Yellow'} else {'Red'}
    Write-Host ('['+$v.result+'] '+$v.id+': '+$v.details) -ForegroundColor $color
  }
} else {
  Write-Host '[PENDING] Native visual test: no fresh visual report. Load a kingdom and click STAGE B UI TEST in the Mod Bar.' -ForegroundColor Yellow
}
Write-Host "STAGE B RESULTS: PASS=$($pass.Count), FAIL=$($fail.Count), PENDING=$($pending.Count)"
if ($fail.Count -gt 0 -or $pass.Count -eq 0) { throw 'Stage B verifier has failed checks.' }
if ($pending.Count -gt 0) {
  Write-Host '[PARTIAL] One-shot smoke passed its exercised checks. Pending real-world tests prevent a 100%% acceptance claim.' -ForegroundColor Yellow
} else {
  Write-Host '[PASS] All Stage B smoke checks passed. Use option 4 to remove the testing DLL.' -ForegroundColor Green
}
