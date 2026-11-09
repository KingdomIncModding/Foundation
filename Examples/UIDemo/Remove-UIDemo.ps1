[CmdletBinding()]
param([string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = [IO.Path]::GetFullPath((Join-Path $root '..\..')) }
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc first.' }
$path = Join-Path $GameRoot 'BepInEx\plugins\Foundation.UIDemo\Foundation.UIDemo.dll'
if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force; Write-Host '[PASS] UI verification demo removed.' }
else { Write-Host '[INFO] UI verification demo not installed.' }
