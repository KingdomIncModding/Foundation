# Unified UI Verification runner. Windows PowerShell 5.1+.
[CmdletBinding()]
param([string]$Task = '4', [string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = [IO.Path]::GetFullPath((Join-Path $root '..\..')) }
if (-not (Test-Path -LiteralPath (Join-Path $GameRoot 'KingdomInc.exe'))) { throw "Kingdom Inc installation not found at [$GameRoot]" }
$installer = Join-Path $root 'Tools\Install\Install-Foundation.ps1'
$demo = Join-Path $root 'Examples\UIDemo\Install-UIDemo.ps1'
$remove = Join-Path $root 'Examples\UIDemo\Remove-UIDemo.ps1'
$smoke = Join-Path $root 'Tools\Development\Verify-Isolated.ps1'
$uiSmokeInstall = Join-Path $root 'Examples\UISmoke\Install-UISmoke.ps1'
$uiSmokeRemove = Join-Path $root 'Examples\UISmoke\Remove-UISmoke.ps1'
$uiSmokeReport = Join-Path $root 'Tests\Read-UISmokeReport.ps1'
$framework = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
$demoDll = Join-Path $GameRoot 'BepInEx\plugins\Foundation.UIDemo\Foundation.UIDemo.dll'
$log = Join-Path $GameRoot 'BepInEx\LogOutput.log'
try {
 switch ($Task) {
  '1' { & $installer -GameRoot $GameRoot -BuildOnly -UseInstalledBepInExOnly; if (-not $?) { throw 'Build-only failed.' } }
  '2' { & $installer -GameRoot $GameRoot -UseInstalledBepInExOnly; if (-not $?) { throw 'Build/install failed.' } }
  '3' { & $demo -GameRoot $GameRoot; if (-not $?) { throw 'Demo install failed.' } }
  '5' { & $remove -GameRoot $GameRoot; if (-not $?) { throw 'Demo removal failed.' } }
  '6' { & $smoke -GameRoot $GameRoot; if (-not $?) { throw 'Isolated smoke failed.' } }
  '7' { & $uiSmokeInstall -GameRoot $GameRoot; if (-not $?) { throw 'UI screenshot smoke install failed.' } }
  '8' { & $uiSmokeReport -GameRoot $GameRoot; if (-not $?) { throw 'UI screenshot report failed.' } }
  '9' { & $uiSmokeRemove -GameRoot $GameRoot; if (-not $?) { throw 'UI screenshot smoke removal failed.' } }
  '4' {
    $missing = @()
    foreach ($path in @($framework,$demoDll,$log)) { if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $missing += $path } }
    if ($missing.Count -gt 0) {
        Write-Host '[PENDING] Required installed files or log missing:' -ForegroundColor Yellow
        foreach ($path in $missing) { Write-Host "  $path" }
        Write-Host 'Build Foundation (2), install UI demo (3), and run Kingdom Inc.'
        break
    }
    $gameVersion = [Reflection.AssemblyName]::GetAssemblyName($framework).Version.ToString()
    $logStamp = (Get-Item -LiteralPath $log).LastWriteTimeUtc
    $dllStamp = @((Get-Item -LiteralPath $framework).LastWriteTimeUtc; (Get-Item -LiteralPath $demoDll).LastWriteTimeUtc) | Sort-Object -Descending | Select-Object -First 1
    Write-Host "[INFO] Installed Foundation assembly version: $gameVersion"
    Write-Host "[INFO] Game BepInEx log UTC: $($logStamp.ToString('o'))"
    if ($logStamp -lt $dllStamp) { Write-Host '[PENDING] Game log predates latest DLL installation. Launch game again.' -ForegroundColor Yellow; break }
    $startup = [bool](Select-String -LiteralPath $log -Pattern '[FOUNDATION UI DEMO] Control gallery registered' -SimpleMatch -Quiet)
    $machine = [bool](Select-String -LiteralPath $log -Pattern '[FOUNDATION UI DEMO] Machine popup opened:' -SimpleMatch -Quiet)
    $actions = [bool](Select-String -LiteralPath $log -Pattern '[FOUNDATION UI DEMO] Action clicked' -SimpleMatch -Quiet)
    if ($startup) { Write-Host '[PASS] UI demo loaded inside game.' -ForegroundColor Green } else { Write-Host '[PENDING] Demo startup not observed.' -ForegroundColor Yellow }
    if ($machine) { Write-Host '[PASS] Machine popup opened inside game.' -ForegroundColor Green } else { Write-Host '[PENDING] Machine popup not yet opened.' -ForegroundColor Yellow }
    if ($actions) { Write-Host '[PASS] Native control button click observed.' -ForegroundColor Green } else { Write-Host '[PENDING] No UI gallery button click observed.' -ForegroundColor Yellow }
    $errors = @(Select-String -LiteralPath $log -Pattern 'Mod panel failed|Foundation UI|FOUNDATION UI DEMO|Exception' | Select-Object -Last 35)
    foreach ($line in $errors) { Write-Host ("  L{0}: {1}" -f $line.LineNumber,$line.Line.Trim()) }
    Write-Host '[INFO] Visual alignment, input behavior and Core/Machine regressions still require game-side checks.'
  }
  default { throw 'Unknown menu choice.' }
 }
} catch {
 Write-Host "[FAIL] $($_.Exception.Message)" -ForegroundColor Red
 exit 1
}
