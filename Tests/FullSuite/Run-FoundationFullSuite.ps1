#Requires -Version 5.1
<#
.SYNOPSIS
  Single command to build, run and report Foundation's complete independent test suites.
.DESCRIPTION
  Verifiers execute one game session at a time and are removed immediately
  after that session. Stage B's HUD buttons stay visible during its session.
  Tests needing actual gameplay are never spoofed: they remain PENDING.
  The Developer Console is enabled ONLY for the Stage B test, then the
  original BepInEx configuration is restored byte-for-byte.
#>
[CmdletBinding()]
param(
  [ValidateSet('Run','Preflight','Report','Cleanup')][string]$Action = 'Run',
  [ValidateSet('All','StageB')][string]$Scope = 'All',
  [ValidateSet('Auto','Manual','Executable')][string]$Launch = 'Auto',
  [string]$GameRoot = '',
  [switch]$SkipFoundationBuild,
  [switch]$SkipStaticChecks
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$sourceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot=[IO.Path]::GetFullPath((Join-Path $sourceRoot '..\..')) }
$GameRoot=[IO.Path]::GetFullPath($GameRoot)
$foundation=Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
$pluginsDir=Join-Path $GameRoot 'BepInEx\plugins'
$configPath=Join-Path $GameRoot 'BepInEx\config\urakka.kingdominc.foundation.cfg'
$resultsRoot=Join-Path $sourceRoot 'Verification\FullSuite'
$latestFile=Join-Path $resultsRoot 'latest.txt'
$logPath=Join-Path $GameRoot 'BepInEx\LogOutput.log'
$script:rows=New-Object 'System.Collections.Generic.List[object]'
$script:phases=New-Object 'System.Collections.Generic.List[object]'
$script:currentDir=$null
$script:configRecoveryFile=$null
$script:configBackup=$null
$script:configExisted=$false
$script:configModified=$false
$script:abort=$false

$suites=@(
  @{ Id='A1'; Name='Native Machines'; Script='Tests\NativeMachines\Invoke-NativeMachinesVerification.ps1'; Dll='Foundation.NativeMachinesVerification'; Report='BepInEx\plugins\Foundation.NativeMachinesVerification\Reports\native-a1-machines.json' },
  @{ Id='A2'; Name='Native IO & Throughput'; Script='Tests\NativeIO\Invoke-NativeIOVerification.ps1'; Dll='Foundation.NativeIOVerification'; Report='BepInEx\plugins\Foundation.NativeIOVerification\Reports\nativeio-verification.json' },
  @{ Id='A3-A7'; Name='Native Game API'; Script='Tests\NativeGameApi\Invoke-NativeGameApiVerification.ps1'; Dll='Foundation.NativeGameApiVerification'; Report='BepInEx\plugins\Foundation.NativeGameApiVerification\Reports\gameapi-a3-a7.json' },
  @{ Id='B1'; Name='Core & Inter-Mod'; Script='Tests\StageB1\Invoke-StageB1Verification.ps1'; Dll='Foundation.StageB1Verification'; Report='_Mods\Foundation\Verification\StageB1\stage-b1-verification.json' },
  @{ Id='B1-B6'; Name='Stage B & Native UI'; Script='Tests\StageB\Invoke-StageBVerification.ps1'; Dll='Foundation.StageBVerification'; Report='_Mods\Foundation\Verification\StageB\stage-b-verification.json'; Visual='_Mods\Foundation\Verification\StageB\stage-b-visual-verification.json' }
)
if ($Scope -eq 'StageB') { $suites=@($suites | Where-Object {$_.Id -eq 'B1-B6'}) }

function Require-IdleGame {
  if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before installing or removing verification DLLs.' }
}
function Require-Game {
  foreach ($p in @((Join-Path $GameRoot 'KingdomInc.exe'), (Join-Path $GameRoot 'KingdomInc_Data\Managed\Assembly-CSharp.dll'),(Join-Path $GameRoot 'BepInEx\core\BepInEx.dll'))) {
    if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw "Required installed game file missing: $p" }
  }
  if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet SDK not found.' }
  if (-not (Test-Path -LiteralPath $foundation -PathType Leaf) -and $SkipFoundationBuild) { throw "Installed Foundation.dll missing: $foundation" }
}
function Record([string]$group,[string]$id,[string]$status,[string]$details,[string]$proof='') {
  $script:rows.Add([pscustomobject]@{group=$group;id=$id;result=$status;details=$details;proof=$proof})
}
function Invoke-Child([string]$name,[string[]]$arguments,[string]$outlog) {
  Write-Host "`n=== $name ===" -ForegroundColor Cyan
  # Windows PowerShell 5.1 can promote redirected native stderr to an
  # ErrorRecord when ErrorActionPreference=Stop. Treat the child's *exit code*
  # as authoritative instead, preserving its entire output in a log.
  $previous=$ErrorActionPreference
  try {
    $ErrorActionPreference='Continue'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $name @arguments 2>&1 | Tee-Object -FilePath $outlog
    $code=$LASTEXITCODE
  } finally { $ErrorActionPreference=$previous }
  if ($null -eq $code) { $code=1 }
  return [int]$code
}
function Get-TestVerifiers {
  if (-not (Test-Path -LiteralPath $pluginsDir)) {return @()}
  return @(Get-ChildItem -LiteralPath $pluginsDir -Filter '*.dll' -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match '(?i)(verification|verifier|uismoke)' -and $_.Name -notmatch '^Foundation\.dll$'
  })
}
function Assert-NoConcurrentVerifiers([string]$allowed) {
  $bad=@(Get-TestVerifiers | Where-Object {$_.Name -ne $allowed})
  if ($bad.Count) {
    Write-Host '[BLOCKED] Another verifier is installed; concurrent tests previously caused severe FPS loss:' -ForegroundColor Yellow
    $bad | ForEach-Object {Write-Host ('  '+$_.FullName) -ForegroundColor Yellow}
    throw 'Remove old test DLLs before the run. This tool never deletes unknown third-party files.'
  }
}
function Prepare-ConsoleConfig {
  $script:configExisted=Test-Path -LiteralPath $configPath -PathType Leaf
  $script:configBackup=Join-Path (Split-Path -Parent $configPath) ('.urakka.kingdominc.foundation.fullsuite-'+(Get-Date -Format 'yyyyMMddHHmmss')+'.bak')
  New-Item -ItemType Directory -Path (Split-Path -Parent $configPath) -Force | Out-Null
  if ($script:configExisted) { Copy-Item -LiteralPath $configPath -Destination $script:configBackup -Force }
  $script:configRecoveryFile=Join-Path $script:currentDir 'console-config-recovery.json'
  @{Config=$configPath;Backup=$script:configBackup;Existed=$script:configExisted} | ConvertTo-Json | Set-Content -LiteralPath $script:configRecoveryFile -Encoding UTF8
  $lines=if ($script:configExisted) {@(Get-Content -LiteralPath $configPath)} else {@()}
  $output=New-Object 'System.Collections.Generic.List[string]'
  $inside=$false;$hasDeveloper=$false;$written=$false
  foreach ($line in $lines) {
    if ($line -match '^\s*\[([^\]]+)\]') {
      if ($inside -and -not $written) {$output.Add('EnableConsole = true');$written=$true}
      $inside=($Matches[1] -eq 'Developer')
      if ($inside) {$hasDeveloper=$true}
    }
    if ($inside -and $line -match '^\s*EnableConsole\s*=') {
      if (-not $written) {$output.Add('EnableConsole = true');$written=$true}
      continue
    }
    $output.Add($line)
  }
  if ($inside -and -not $written) {$output.Add('EnableConsole = true');$written=$true}
  if (-not $hasDeveloper) {$output.Add('');$output.Add('[Developer]');$output.Add('EnableConsole = true')}
  [IO.File]::WriteAllLines($configPath,$output.ToArray(),([Text.UTF8Encoding]::new($false)))
  $script:configModified=$true
  Write-Host '[CONFIG] Foundation Developer/EnableConsole temporarily enabled for the F8 test.' -ForegroundColor Cyan
  Write-Host '[CONFIG] Your existing configuration will be restored after the game closes.'
}
function Restore-ConsoleConfig {
  if (-not $script:configModified) {return}
  Require-IdleGame
  if ($script:configExisted) {
    if (-not (Test-Path -LiteralPath $script:configBackup)) {throw 'Console config backup missing: '+$script:configBackup}
    Copy-Item -LiteralPath $script:configBackup -Destination $configPath -Force
    Remove-Item -LiteralPath $script:configBackup -Force
  } elseif (Test-Path -LiteralPath $configPath) {Remove-Item -LiteralPath $configPath -Force}
  if ($script:configRecoveryFile -and (Test-Path -LiteralPath $script:configRecoveryFile)) {Remove-Item -LiteralPath $script:configRecoveryFile -Force}
  $script:configModified=$false
  Write-Host '[CONFIG] Original Developer Console setting restored.' -ForegroundColor Green
}
function Recover-ConsoleConfig {
  if (-not (Test-Path -LiteralPath $latestFile)) {return}
  $last=Get-Content -LiteralPath $latestFile -Raw
  $record=Join-Path $last.Trim() 'console-config-recovery.json'
  if (-not (Test-Path -LiteralPath $record)) {return}
  $old=Get-Content -LiteralPath $record -Raw | ConvertFrom-Json
  Require-IdleGame
  if ($old.Existed) {
    if (-not (Test-Path -LiteralPath $old.Backup)) {throw 'Original console settings backup missing; do not overwrite config.'}
    Copy-Item -LiteralPath $old.Backup -Destination $old.Config -Force
    Remove-Item -LiteralPath $old.Backup -Force
  } elseif (Test-Path -LiteralPath $old.Config) {Remove-Item -LiteralPath $old.Config -Force}
  Remove-Item -LiteralPath $record -Force
  Write-Host '[CONFIG] Recovered original console configuration from interrupted run.' -ForegroundColor Green
}
function Get-SteamAppId {
  $idFile=Join-Path $GameRoot 'steam_appid.txt'
  if (Test-Path -LiteralPath $idFile) {
    $id=(Get-Content -LiteralPath $idFile -TotalCount 1).Trim()
    if ($id -match '^\d+$') {return $id}
  }
  $steamapps=Split-Path (Split-Path $GameRoot -Parent) -Parent
  if ((Split-Path $steamapps -Leaf) -ne 'steamapps') {return $null}
  $installName=Split-Path $GameRoot -Leaf
  foreach ($manifest in Get-ChildItem -LiteralPath $steamapps -Filter 'appmanifest_*.acf' -ErrorAction SilentlyContinue) {
    $content=Get-Content -LiteralPath $manifest.FullName -Raw
    $match=[regex]::Match($content,'"installdir"\s+"([^"]+)"')
    if ($match.Success -and $match.Groups[1].Value -eq $installName) {
      $candidate=[regex]::Match($content,'"appid"\s+"(\d+)"')
      if ($candidate.Success) {return $candidate.Groups[1].Value}
    }
  }
  return $null
}
function Start-And-WaitForGame([string]$stage) {
  Write-Host "`n=== ${stage}: GAME SESSION ===" -ForegroundColor Cyan
  if ($stage -eq 'B1-B6') {
    Write-Host 'Load a disposable kingdom. In the Mod Bar, click STAGE B UI TEST.' -ForegroundColor Yellow
    Write-Host 'Use the HUD toggle/dropdown, click ALL panel controls and visually approve.' -ForegroundColor Yellow
    Write-Host 'Also press F8 (Foundation Developer Inspector) and inspect every tab.' -ForegroundColor Yellow
  } elseif ($stage -eq 'A1') {Write-Host 'Load a kingdom with all relevant native machine types.' -ForegroundColor Yellow}
  elseif ($stage -eq 'A2') {Write-Host 'Exercise connected pipes, transfers and Collector/Distributor/Manifold.' -ForegroundColor Yellow}
  elseif ($stage -eq 'A3-A7') {Write-Host 'Exercise native research, items, quests, workspaces, inventory, menus, progression.' -ForegroundColor Yellow}
  else {Write-Host 'Load a kingdom and allow the one-shot Core test to finish.' -ForegroundColor Yellow}
  if ($Launch -eq 'Executable') {
    Start-Process -FilePath (Join-Path $GameRoot 'KingdomInc.exe') -WorkingDirectory $GameRoot
  } elseif ($Launch -eq 'Auto') {
    $steamId=Get-SteamAppId
    if ($steamId) {
      Write-Host ('Launching Kingdom Inc through Steam app '+$steamId)
      Start-Process ('steam://rungameid/'+$steamId)
    } else {Write-Host 'Steam app ID not discovered. Launch Kingdom Inc via Steam manually.'}
  } else {Write-Host 'Launch Kingdom Inc normally through Steam.'}
  Write-Host 'The script waits for the game to close. Do NOT launch other verification DLLs concurrently.'
  # Manual mode always provides an explicit checkpoint; Auto and Executable
  # detect the process. A normal Steam launcher may take time to create it.
  if ($Launch -eq 'Manual' -or ($Launch -eq 'Auto' -and -not $steamId)) {
    [void](Read-Host 'Press ENTER when Kingdom Inc is running (or type Ctrl+C to cancel)')
  }
  $found=$false
  for ($i=0;$i -lt 150;$i++) {
    if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) {$found=$true;break}
    Start-Sleep -Milliseconds 800
  }
  if (-not $found) {throw 'Kingdom Inc did not start; no runtime result can be claimed.'}
  while (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) {Start-Sleep -Seconds 2}
  Write-Host '[SESSION] Kingdom Inc exited. Collecting results...' -ForegroundColor Green
}
function Read-PhaseChecks($suite) {
  $source=Join-Path $GameRoot $suite.Report
  if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    Record $suite.Id ($suite.Id+'.ReportMissing') 'PENDING' 'Game session produced no verifier JSON report.'
    return
  }
  $verifierDll=Join-Path $pluginsDir ($suite.Dll+'\'+$suite.Dll+'.dll')
  if (-not (Test-Path -LiteralPath $verifierDll -PathType Leaf)) {
    Record $suite.Id ($suite.Id+'.VerifierMissing') 'FAIL' 'Verifier DLL missing at report collection.'
    return
  }
  $reportTime=(Get-Item -LiteralPath $source).LastWriteTimeUtc
  $installTime=(Get-Item -LiteralPath $verifierDll).LastWriteTimeUtc
  if ($reportTime -le $installTime -or $reportTime -lt $script:sessionStartedUtc) {
    Record $suite.Id ($suite.Id+'.StaleReport') 'FAIL' 'Report predates the installed verifier or the current game session.' $source
    return
  }
  $copy=Join-Path $script:currentDir ($suite.Id+'.json')
  Copy-Item -LiteralPath $source -Destination $copy -Force
  $body=Get-Content -LiteralPath $source -Raw | ConvertFrom-Json
  $expectedHash=(Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash
  $expectedVer=[Reflection.AssemblyName]::GetAssemblyName($foundation).Version.ToString()
  if ([string]$body.foundationSha256 -ne $expectedHash -or [string]$body.foundationVersion -ne $expectedVer) {
    Record $suite.Id ($suite.Id+'.ReportMismatch') 'FAIL' 'Report Foundation version/hash differs from installed DLL.' $copy
    return
  }
  if (@($body.checks).Count -eq 0) {
    Record $suite.Id ($suite.Id+'.NoChecks') 'FAIL' 'Report has no checks.' $copy
    return
  }
  foreach ($c in @($body.checks)) {
    $status=[string]$c.result
    if ($status -notin @('PASS','FAIL','PENDING')) {$status='FAIL'}
    Record $suite.Id ([string]$c.id) $status ([string]$c.details) $copy
  }
  if ($suite.ContainsKey('Visual')) {
    $visualFile=Join-Path $GameRoot $suite.Visual
    if (Test-Path -LiteralPath $visualFile) {
      $visualTime=(Get-Item -LiteralPath $visualFile).LastWriteTimeUtc
      if ($visualTime -le (Get-Item -LiteralPath $verifierDll).LastWriteTimeUtc -or
          $visualTime -lt $script:sessionStartedUtc) {
        Record $suite.Id 'B4.StaleInteractiveReport' 'PENDING' 'Visual report is from a previous game session; it cannot count as current interaction evidence.'
        return
      }
      $visual=Get-Content -LiteralPath $visualFile -Raw | ConvertFrom-Json
      if ([string]$visual.foundationSha256 -ne $expectedHash) {Record $suite.Id 'B4.VisualHash' 'FAIL' 'Visual result targets different Foundation binary.'}
      else {
        $visualCopy=Join-Path $script:currentDir 'B1-B6-visual.json'
        Copy-Item -LiteralPath $visualFile -Destination $visualCopy -Force
        foreach ($vc in @($visual.checks)) {
          $visualStatus=[string]$vc.result
          if ($visualStatus -notin @('PASS','FAIL','PENDING')) {$visualStatus='FAIL'}
          Record $suite.Id ([string]$vc.id+'.Visual') $visualStatus ([string]$vc.details) $visualCopy
          if ($vc.id -in @('B4.RenderingVisualAudit','B6.ModBarNativeVisual') -and $visualStatus -eq 'PASS') {
            for($i=0;$i -lt $script:rows.Count;$i++) {
              if ($script:rows[$i].id -eq [string]$vc.id -and $script:rows[$i].group -eq $suite.Id) {
                $script:rows[$i].result='PASS'
                $script:rows[$i].details=([string]$vc.details)+' (verified from separate interactive report)'
                $script:rows[$i].proof=$visualCopy
              }
            }
          }
        }
      }
    } else {Record $suite.Id 'B4.InteractiveReport' 'PENDING' 'No interactive visual report; click STAGE B UI TEST in a kingdom.'}
  }
}
function Get-F8LogEvidence([long]$oldLength) {
  if (-not (Test-Path -LiteralPath $logPath)) {return $false}
  if ((Get-Item -LiteralPath $logPath).LastWriteTimeUtc -lt $script:sessionStartedUtc) {return $false}
  # BepInEx typically truncates LogOutput.log at startup; the new session
  # can be LONGER than the old one. Reading from the old length would miss
  # valid F8 events, so search the current log and require the operator's
  # independent visible-window confirmation as well.
  $fs=[IO.File]::Open($logPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
  try {
    $reader=[IO.StreamReader]::new($fs)
    $data=$reader.ReadToEnd()
    return $data.Contains('[FOUNDATION CONSOLE] F8 toggled OPEN')
  } finally {$fs.Dispose()}
}

function Run-StaticChecks {
  if ($SkipStaticChecks) {Record 'Source' 'SourceChecks' 'PENDING' 'Skipped by operator.';return}
  $python=Get-Command python -ErrorAction SilentlyContinue
  if (-not $python) {Record 'Source' 'PythonNotAvailable' 'PENDING' 'Python is required for static checks. Runtime tests can still run.';return}
  $tests=@('verify_source.py','verify_ui_source.py','verify_ui_smoke_source.py','verify_game_theme.py','verify_native_machine_a1.py','verify_native_io_a2.py','verify_game_api_a3_a7.py','verify_stage_b1.py','verify_stage_b_all.py','verify_stage_b_visual_ui.py','GateB/verify_gate_b.py','FullSuite/verify_full_suite_runner.py')
  foreach($test in $tests) {
    $scriptFile=Join-Path $sourceRoot ('Tests\'+$test)
    if (-not (Test-Path -LiteralPath $scriptFile)) {Record 'Source' $test 'PENDING' 'Static test is absent.';continue}
    # A nested test name (FullSuite/verify_*.py) is not a valid flat log
    # filename. Sanitize only the report filename, not the test path.
    $safeName=($test -replace '[\\/]', '-')
    $outLog=Join-Path $script:currentDir ('static-'+$safeName+'.log')
    Push-Location $sourceRoot
    try {
      $old=$ErrorActionPreference
      try {
        $ErrorActionPreference='Continue'
        & python $scriptFile 2>&1 | Tee-Object -FilePath $outLog
        $pythonExit=$LASTEXITCODE
      } finally { $ErrorActionPreference=$old }
      if ($pythonExit -ne 0) {Record 'Source' $test 'FAIL' 'Source regression script failed; see its log.' $outLog}
      else {Record 'Source' $test 'PASS' 'Static source check passed.' $outLog}
    } catch {Record 'Source' $test 'FAIL' $_.Exception.Message $outLog}
    finally {Pop-Location}
  }
}
function Write-Summary {
  if (-not $script:currentDir) {return}
  $count=@{PASS=0;FAIL=0;PENDING=0}
  foreach($row in $script:rows) {if ($count.ContainsKey($row.result)) {$count[$row.result]++}}
  $sha=if(Test-Path -LiteralPath $foundation) {(Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash} else {''}
  $out=[ordered]@{createdUtc=[DateTime]::UtcNow.ToString('o');source=$sourceRoot;gameRoot=$GameRoot;scope=$Scope;foundationSha256=$sha;counts=$count;checks=@($script:rows.ToArray());note='PASS means tested; PENDING means still requires a real scenario or user interaction. These reports are not a substitute for independent native save/patch conflict testing.'}
  $outFile=Join-Path $script:currentDir 'summary.json'
  $out | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $outFile -Encoding UTF8
  $md=New-Object 'System.Collections.Generic.List[string]'
  $md.Add('# Foundation Full-Suite — Collected Results')
  $md.Add('')
  $md.Add("Scope: **$Scope**. PASS=$($count.PASS), FAIL=$($count.FAIL), PENDING=$($count.PENDING).")
  $md.Add('')
  $md.Add('| Group | Check | Status | Details |')
  $md.Add('| --- | --- | --- | --- |')
  foreach ($row in $script:rows) {
    $detail=([string]$row.details).Replace('|','/').Replace("`r",' ').Replace("`n",' ')
    if ($detail.Length -gt 170) {$detail=$detail.Substring(0,167)+'...'}
    $md.Add('| '+$row.group+' | '+$row.id+' | **'+$row.result+'** | '+$detail+' |')
  }
  $md.Add('')
  $md.Add('Tests requiring a missing native fixture, actual kingdom reload, or external mod conflict remain PENDING; the runner does not fabricate their effects.')
  $md | Set-Content -LiteralPath (Join-Path $script:currentDir 'summary.md') -Encoding UTF8
  Write-Host "`nFULL SUITE: PASS=$($count.PASS) FAIL=$($count.FAIL) PENDING=$($count.PENDING)" -ForegroundColor Cyan
  Write-Host ('Reports: '+$script:currentDir) -ForegroundColor Cyan
}
function Show-LastReport {
  if (-not (Test-Path $latestFile)) {throw 'No previous full-suite run found.'}
  $folder=(Get-Content -LiteralPath $latestFile -Raw).Trim()
  $summary=Join-Path $folder 'summary.json'
  if (-not (Test-Path $summary)) {throw 'No summary.json found: '+$summary}
  $body=Get-Content -LiteralPath $summary -Raw | ConvertFrom-Json
  Write-Host ("FULL SUITE: PASS={0} FAIL={1} PENDING={2}" -f $body.counts.PASS,$body.counts.FAIL,$body.counts.PENDING) -ForegroundColor Cyan
  foreach($row in $body.checks) {
    $color=if($row.result -eq 'PASS'){'Green'}elseif($row.result -eq 'FAIL'){'Red'}else{'Yellow'}
    Write-Host ('['+$row.result+'] '+$row.group+'.'+$row.id+': '+$row.details) -ForegroundColor $color
  }
  Write-Host ('Summary: '+$summary)
}

if ($Action -eq 'Report') {Show-LastReport;exit 0}
if ($Action -eq 'Cleanup') {
  Require-IdleGame
  Recover-ConsoleConfig
  foreach($suite in $suites) {
    $dll=Join-Path $pluginsDir ($suite.Dll+'\'+$suite.Dll+'.dll')
    if (Test-Path -LiteralPath $dll) {Remove-Item -LiteralPath $dll -Force;Write-Host ('Removed '+$dll)}
  }
  Write-Host 'Only recognized temporary verifiers were removed; Foundation.dll and other mods were not modified.' -ForegroundColor Green
  exit 0
}
Require-IdleGame
# Restore an interrupted previous run before overwriting latest.txt. Otherwise
# the backup marker is orphaned and a later run may keep F8 enabled forever.
Recover-ConsoleConfig
Require-Game
if ($Action -eq 'Preflight') {
  Assert-NoConcurrentVerifiers ''
  Write-Host ('[PASS] Game, dotnet and installed Foundation prerequisites ready: '+$GameRoot) -ForegroundColor Green
  Write-Host 'Developer inspector is disabled by default; -Action Run enables it for Stage B, then restores your existing setting.'
  exit 0
}
New-Item -ItemType Directory -Path $resultsRoot -Force | Out-Null
$script:currentDir=Join-Path $resultsRoot ('run-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $script:currentDir -Force | Out-Null
Set-Content -LiteralPath $latestFile -Value $script:currentDir -Encoding UTF8
$script:activeSuite=$null
try {
  Assert-NoConcurrentVerifiers ''
  if (-not $SkipFoundationBuild) {
    $installer=Join-Path $sourceRoot 'Tools\Install\Install-Foundation.ps1'
    $code=Invoke-Child -name $installer -arguments @('-GameRoot',$GameRoot,'-UseInstalledBepInExOnly') -outlog (Join-Path $script:currentDir 'install-foundation.log')
    if ($code -ne 0) {Record 'Build' 'FoundationCompilation' 'FAIL' 'Production Foundation build/install failed.'; throw 'Production Foundation build failed.'}
    Record 'Build' 'FoundationCompilation' 'PASS' 'Production Foundation built and installed against installed game.'
  } else {Record 'Build' 'FoundationCompilation' 'PENDING' 'Existing installed Foundation used; source changes not rebuilt.'}
  Run-StaticChecks
  foreach ($suite in $suites) {
    Require-IdleGame
    $script:activeSuite=$suite
    Assert-NoConcurrentVerifiers ''
    $tool=Join-Path $sourceRoot $suite.Script
    $stageLog=Join-Path $script:currentDir ($suite.Id+'-install.log')
    $code=Invoke-Child -name $tool -arguments @('-Action','BuildInstall','-GameRoot',$GameRoot) -outlog $stageLog
    if ($code -ne 0) {
      Record $suite.Id ($suite.Id+'.VerifierBuild') 'FAIL' 'Verifier compilation or installation failed; see build log.' $stageLog
      Write-Host '[FAIL] Verifier installation failed. Skipping only its in-game session.' -ForegroundColor Red
      # Build can fail before a copy; if it copied anything, safely remove
      # only this suite's own temporary DLL before continuing.
      $testDll=Join-Path $pluginsDir ($suite.Dll+'\'+$suite.Dll+'.dll')
      if (Test-Path -LiteralPath $testDll) {Remove-Item -LiteralPath $testDll -Force}
      $script:activeSuite=$null
      continue
    }
    Record $suite.Id ($suite.Id+'.VerifierBuild') 'PASS' 'Compiled against the installed Foundation and game.' $stageLog
    $oldLength=if(Test-Path -LiteralPath $logPath){(Get-Item -LiteralPath $logPath).Length}else{0L}
    if ($suite.Id -eq 'B1-B6') {Prepare-ConsoleConfig}
    $script:sessionStartedUtc=[DateTime]::UtcNow
    $sessionOk=$false
    try {
      Start-And-WaitForGame $suite.Id
      $sessionOk=$true
      Read-PhaseChecks $suite
      if ($suite.Id -eq 'B1-B6') {
        $evidence=Get-F8LogEvidence $oldLength
        $answer=(Read-Host 'Did F8 OPEN the Foundation Developer Inspector and did you inspect the tabs? [Y/N, ENTER=pending]')
        if ($evidence -and $answer -match '^[Yy]$') {
          Record 'B5' 'ConsoleKeyboardF8.Manual' 'PASS' 'F8 opened the inspector: game log marker + human visual confirmation.'
          foreach ($row in $script:rows) {
            if ($row.group -eq 'B1-B6' -and $row.id -eq 'B5.ConsoleKeyboardF8') {
              $row.result='PASS'; $row.details='F8 opened the inspector: native game log marker and operator tab inspection.'
            }
          }
        } elseif ($answer -match '^[Nn]$') {
          Record 'B5' 'ConsoleKeyboardF8.Manual' 'FAIL' ('Operator reported that the F8 inspector did not open. Log marker observed: '+$evidence)
        } else {
          Record 'B5' 'ConsoleKeyboardF8.Manual' 'PENDING' ('Need real F8 press and visible tab inspection. Game log marker observed: '+$evidence)
        }
      }
    } catch {
      Record $suite.Id ($suite.Id+'.GameSession') 'FAIL' $_.Exception.Message
      Write-Host ('[FAIL] '+$_.Exception.Message) -ForegroundColor Red
    } finally {
      Require-IdleGame
      if ($suite.Id -eq 'B1-B6') {Restore-ConsoleConfig}
      # The Report action validates the DLL hash, event markers and freshness;
      # its nonzero exit 2 signals PENDING, not an installer failure.
      if ($sessionOk) {
        $code=Invoke-Child -name $tool -arguments @('-Action','Report','-GameRoot',$GameRoot) -outlog (Join-Path $script:currentDir ($suite.Id+'-report.log'))
        if ($code -ne 0 -and $code -ne 2) {
          Record $suite.Id ($suite.Id+'.ReportValidation') 'FAIL' 'Underlying verifier rejected report integrity or had failed checks.'
        }
      }
      $code=Invoke-Child -name $tool -arguments @('-Action','Remove','-GameRoot',$GameRoot) -outlog (Join-Path $script:currentDir ($suite.Id+'-cleanup.log'))
      if ($code -ne 0) {Record $suite.Id ($suite.Id+'.Cleanup') 'FAIL' 'Could not remove temporary verifier DLL.'}
      else {Record $suite.Id ($suite.Id+'.Cleanup') 'PASS' 'Development-only test DLL removed after this game session.'}
    }
    $script:activeSuite=$null
  }
} catch {
  $script:abort=$true
  Record 'Runner' 'UnexpectedError' 'FAIL' $_.Exception.Message
  Write-Host ('[FAIL] '+$_.Exception.Message) -ForegroundColor Red
} finally {
  try {
    if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) {
      Write-Host '[WARNING] Game is still running; will not remove active test DLL or restore config mid-session.' -ForegroundColor Yellow
      Record 'Runner' 'Cleanup' 'PENDING' 'Close the game and run -Action Cleanup to restore config/remove test DLL.'
    } else {
      if ($script:configModified) {Restore-ConsoleConfig}
      if ($script:activeSuite) {
        $tool=Join-Path $sourceRoot $script:activeSuite.Script
        $code=Invoke-Child -name $tool -arguments @('-Action','Remove','-GameRoot',$GameRoot) -outlog (Join-Path $script:currentDir 'emergency-cleanup.log')
        if ($code -ne 0) {Record 'Runner' 'EmergencyCleanup' 'FAIL' 'Verifier removal failed; run -Action Cleanup.'}
      }
    }
  } catch {Record 'Runner' 'CleanupError' 'FAIL' $_.Exception.Message}
  Write-Summary
}
$bad=@($script:rows | Where-Object {$_.result -eq 'FAIL'})
$pending=@($script:rows | Where-Object {$_.result -eq 'PENDING'})
if ($bad.Count -gt 0) {exit 1}
if ($pending.Count -gt 0) {exit 2}
exit 0
