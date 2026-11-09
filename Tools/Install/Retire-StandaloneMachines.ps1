# Windows PowerShell 5.1 compatible. Run only after the unified Foundation DLL is installed.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameRoot)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
$unified = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
if (!(Test-Path -LiteralPath $unified -PathType Leaf)) { throw 'Unified Foundation is not installed; refusing migration.' }
$built = Join-Path $Root 'bin\Release\netstandard2.1\Foundation.dll'
if (!(Test-Path -LiteralPath $built -PathType Leaf)) { throw 'Unified Foundation build output is missing.' }
$hashBuilt = (Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash
$hashInstalled = (Get-FileHash -LiteralPath $unified -Algorithm SHA256).Hash
if ($hashBuilt -ne $hashInstalled) { throw 'Installed Foundation does not match freshly built unified DLL.' }
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close game before migration.' }
$plugins = Join-Path $GameRoot 'BepInEx\plugins'
$legacy = @(
    (Join-Path $plugins 'Foundation.Machines\Foundation.Machines.dll'),
    (Join-Path $plugins 'Foundation.MachineVerification\Foundation.MachineVerification.dll')
)
$present = @($legacy | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })
if ($present.Count -eq 0) { Write-Host 'No legacy machine plugin binaries to migrate.'; return }
$backup = Join-Path $Root ('_Backup\Machine-Merge-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach ($file in $present) {
    $name = [IO.Path]::GetFileName($file)
    $dst = Join-Path $backup $name
    Copy-Item -LiteralPath $file -Destination $dst -ErrorAction Stop
    if ((Get-FileHash -LiteralPath $file).Hash -ne (Get-FileHash -LiteralPath $dst).Hash) {
        throw "Backup checksum failed for $name. No legacy plugin was removed."
    }
}
# Only the two exact legacy DLLs are removed, not any other plugin or user data.
$removed = @()
try {
    foreach ($file in $present) {
        Remove-Item -LiteralPath $file -Force -ErrorAction Stop
        $removed += $file
        Write-Host "Retired standalone plugin: $file"
    }
} catch {
    foreach ($file in $removed) {
        Copy-Item -LiteralPath (Join-Path $backup ([IO.Path]::GetFileName($file))) -Destination $file -Force
    }
    throw
}
Write-Host "Legacy DLL backups: $backup"
Write-Host 'Rebuild external mods referencing Foundation.Machines against the unified Foundation.dll.'
