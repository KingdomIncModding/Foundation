[CmdletBinding()]
param([string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = Join-Path $root '..\..' }
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc first.' }
$foundation = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
if (!(Test-Path -LiteralPath $foundation)) { throw "Missing unified Foundation.dll: $foundation" }
$legacy = Join-Path $GameRoot 'BepInEx\plugins\Foundation.Machines\Foundation.Machines.dll'
if (Test-Path -LiteralPath $legacy) { throw 'Legacy Foundation.Machines.dll is still installed; run INSTALL-FOUNDATION.cmd first.' }
$project = Join-Path $root 'Tests\Foundation.MachineVerification.csproj'
$gameArg = '-p:GameRoot=' + $GameRoot.TrimEnd('\','/').Replace('\','/') + '/'
$foundationArg = '-p:FoundationDll=' + $foundation.Replace('\','/')
& dotnet build $project -c Release --nologo $gameArg $foundationArg
if ($LASTEXITCODE -ne 0) { throw 'Unified machine verification mod failed to build.' }
$built = Join-Path $root 'Tests\bin\Release\netstandard2.1\Foundation.MachineVerification.dll'
if (!(Test-Path -LiteralPath $built)) { throw 'Built verification DLL missing.' }
$destdir = Join-Path $GameRoot 'BepInEx\plugins\Foundation.MachineVerification'
New-Item -Path $destdir -ItemType Directory -Force | Out-Null
$dest = Join-Path $destdir 'Foundation.MachineVerification.dll'
Copy-Item -LiteralPath $built -Destination $dest -Force
if ((Get-FileHash -LiteralPath $built).Hash -ne (Get-FileHash -LiteralPath $dest).Hash) { throw 'Verification plugin install hash mismatch.' }
Write-Host "Installed separate DEVELOPMENT-ONLY verification mod: $dest"
Write-Host 'Load a disposable kingdom; add/remove/change machines; enter a second kingdom; exit game.'
Write-Host 'Run Tests\READ-MACHINE-REPORT.ps1 to see results. Remove verification DLL after testing.'
