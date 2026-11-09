[CmdletBinding()]
param([string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = [IO.Path]::GetFullPath((Join-Path $root '..\..')) }
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before installing the UI demo.' }
$foundation = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
if (-not (Test-Path -LiteralPath $foundation -PathType Leaf)) { throw "Unified Foundation DLL not installed: $foundation" }
$project = Join-Path $PSScriptRoot 'Foundation.UIDemo.csproj'
$gameArg = '-p:GameRoot=' + ($GameRoot.TrimEnd('\','/') -replace '\\','/') + '/'
$foundationArg = '-p:FoundationDll=' + ($foundation -replace '\\','/')
& dotnet build $project --configuration Release --nologo $gameArg $foundationArg
if ($LASTEXITCODE -ne 0) { throw 'UI demo failed compilation; no DLL installed.' }
$built = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Foundation.UIDemo.dll'
if (-not (Test-Path -LiteralPath $built -PathType Leaf)) { throw "Built DLL missing: $built" }
$dir = Join-Path $GameRoot 'BepInEx\plugins\Foundation.UIDemo'
New-Item -Path $dir -ItemType Directory -Force | Out-Null
$dest = Join-Path $dir 'Foundation.UIDemo.dll'
Copy-Item -LiteralPath $built -Destination $dest -Force
if ((Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash) {
    throw 'Copy checksum mismatch.'
}
Write-Host "[PASS] Development-only UI demo installed: $dest"
Write-Host 'Start Kingdom Inc and click Test UI / Machine UI on the native Foundation Mod Bar.'
