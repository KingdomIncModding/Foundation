[CmdletBinding()]
param([string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$foundationRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) {
    $GameRoot = [IO.Path]::GetFullPath((Join-Path $foundationRoot '..\..'))
}
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
if (-not (Test-Path -LiteralPath (Join-Path $GameRoot 'KingdomInc.exe') -PathType Leaf)) { throw "Game not found at: $GameRoot" }
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before installing the UI smoke plugin.' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet SDK is missing from PATH.' }
$foundation = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
if (-not (Test-Path -LiteralPath $foundation -PathType Leaf)) { throw "Unified Foundation.dll not installed: $foundation" }
$version = [Reflection.AssemblyName]::GetAssemblyName($foundation).Version.ToString()
Write-Host "[INFO] Installed Foundation DLL version: $version"
if (-not $version.StartsWith('0.10.3')) { Write-Warning 'This UI smoke harness is written against Foundation 0.10.3. Check compatibility.' }
$project = Join-Path $PSScriptRoot 'Foundation.UISmoke.csproj'
$gameArg = '-p:GameRoot=' + ($GameRoot.TrimEnd('\','/') -replace '\\','/') + '/'
$foundationArg = '-p:FoundationDll=' + ($foundation -replace '\\','/')
& dotnet build $project --configuration Release --nologo $gameArg $foundationArg
if ($LASTEXITCODE -ne 0) { throw 'UI screenshot smoke plugin failed compilation; no DLL installed.' }
$built = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Foundation.UISmoke.dll'
if (-not (Test-Path -LiteralPath $built -PathType Leaf)) { throw "Build success but expected DLL missing: $built" }
$target = Join-Path $GameRoot 'BepInEx\plugins\Foundation.UISmoke'
New-Item -Path $target -ItemType Directory -Force | Out-Null
$destination = Join-Path $target 'Foundation.UISmoke.dll'
Copy-Item -LiteralPath $built -Destination $destination -Force
if ((Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash) {
    throw 'Smoke DLL installation hash mismatch.'
}
Write-Host "[PASS] Development-only screenshot smoke plugin installed: $destination" -ForegroundColor Green
Write-Host 'Load a test kingdom and click UI Smoke + Screenshots on the Foundation Mod Bar.'
Write-Host 'Images and results: _Mods\Foundation\Verification\UISmoke'
