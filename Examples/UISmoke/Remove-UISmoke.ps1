[CmdletBinding()]
param([string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
$foundationRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = [IO.Path]::GetFullPath((Join-Path $foundationRoot '..\..')) }
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before uninstalling the screenshot test plugin.' }
$target = Join-Path $GameRoot 'BepInEx\plugins\Foundation.UISmoke\Foundation.UISmoke.dll'
if (Test-Path -LiteralPath $target -PathType Leaf) {
    Remove-Item -LiteralPath $target -Force
    Write-Host "[PASS] UI smoke test DLL removed: $target" -ForegroundColor Green
} else {
    Write-Host '[INFO] Screenshot test plugin is not installed.'
}
