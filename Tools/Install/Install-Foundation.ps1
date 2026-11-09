# Foundation installer. Default: build, package, deploy (no one-off audit scripts).
# Does not copy game assemblies or saves into distribution packages.
[CmdletBinding()]
param(
    [string]$GameRoot = '',
    [switch]$BuildOnly,
    [switch]$UseInstalledBepInExOnly
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($GameRoot)) {
    $GameRoot = Join-Path $ProjectRoot '..\..'
}
$GameRoot = [IO.Path]::GetFullPath($GameRoot)

function Assert-File([string]$Path, [string]$Name) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Name missing: $Path"
    }
}
# Pass named parameters as a hashtable. A string array splat is POSITIONAL in
# PowerShell; its '-InstallBepInEx' entry is not recognized as a switch.
function Run-Script {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][hashtable]$Parameters
    )
    $scriptPath = Join-Path $ProjectRoot $Name
    Assert-File $scriptPath $Name
    Write-Host "`n===== $Name =====" -ForegroundColor Cyan
    & $scriptPath @Parameters
    $completed = $?
    if (-not $completed) { throw "$Name failed. Installation stopped." }
    if ($null -ne (Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue)) {
        if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE." }
    }
}

Assert-File (Join-Path $GameRoot 'KingdomInc.exe') 'Kingdom Inc executable'
Assert-File (Join-Path $GameRoot 'KingdomInc_Data\Managed\Assembly-CSharp.dll') 'Kingdom Inc managed assembly'
if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) {
    throw 'Kingdom Inc is running. Exit the game before building or installing Foundation.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'A .NET 8+ SDK is required. Install it and rerun this script.'
}
$sdks = @(& dotnet --list-sdks 2>&1)
if ($LASTEXITCODE -ne 0 -or -not (@($sdks | Where-Object { "$_" -match '^([89]|[1-9][0-9])\.' }).Count)) {
    throw "A .NET 8+ SDK is required. Installed SDKs: $($sdks -join ', ')"
}

$logDir = Join-Path $ProjectRoot 'Logs\Install'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logPath = Join-Path $logDir ('foundation-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
Write-Host "Target game: $GameRoot"
Write-Host "Build log:   $logPath"
$transcriptStarted = $false
try {
    Start-Transcript -LiteralPath $logPath -ErrorAction Stop | Out-Null
    $transcriptStarted = $true
    # Named splatting supports paths containing spaces, switch arguments, and
    # PowerShell 5.1 without accidental positional binding.
    $bootstrapParams = @{
        GameRoot = $GameRoot
        SkipPackaging = $true
    }
    if (-not $UseInstalledBepInExOnly) { $bootstrapParams['InstallBepInEx'] = $true }
    Run-Script -Name 'Tools\Build\Build-Foundation.ps1' -Parameters $bootstrapParams

    if ($BuildOnly) {
        Write-Host 'Build-only requested. Nothing was installed.'
        return
    }

    Run-Script -Name 'Tools\Build\Package-Foundation.ps1' -Parameters @{ GameRoot = $GameRoot }
    Run-Script -Name 'Tools\Install\Deploy-Foundation.ps1' -Parameters @{ GameRoot = $GameRoot }
    # Prevent duplicate Harmony patches and an obsolete test dependency after merge.
    Run-Script -Name 'Tools\Install\Retire-StandaloneMachines.ps1' -Parameters @{ GameRoot = $GameRoot }

    $plugin = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
    Assert-File $plugin 'Installed Foundation DLL'
    $hash = (Get-FileHash -LiteralPath $plugin -Algorithm SHA256).Hash
    Write-Host "`nFoundation installed successfully." -ForegroundColor Green
    Write-Host "DLL:    $plugin"
    Write-Host "SHA256: $hash"
    Write-Host 'Launch Kingdom Inc through Steam to use Foundation.'
}
catch {
    # Do not use Write-Error here: ErrorActionPreference=Stop masks the original
    # exception before the rethrow, making the installer log less useful.
    Write-Host "`nFoundation install failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Full build output: $logPath" -ForegroundColor Yellow
    throw
}
finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
