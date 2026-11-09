# Foundation build/bootstrap for Kingdom Inc on Windows PowerShell 5.1 or PowerShell 7.
# This script builds the real Foundation sources against the supplied KingdomInc_Data
# managed assemblies and BepInEx 5 Unity Mono. It does not redistribute game DLLs.
[CmdletBinding()]
param(
    [string]$GameRoot,
    [string]$GameDataZip,
    [string]$BepInExZip,
    [switch]$Verify,
    [switch]$Install,
    [switch]$InstallBepInEx,
    [switch]$WithExistingPlugins,
    [switch]$SkipPackaging
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Set-StrictMode -Version Latest
$BepInExVersion = '5.4.23.6'
$BepInExAsset = "BepInEx_win_x64_$BepInExVersion.zip"
$BepInExSha256 = '6c57a714a892efb4e9e1fef63d608a3391f30ec984031c465404e4ee381a0755'
$BepInExUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$BepInExVersion/$BepInExAsset"

function Require-File([string]$Path, [string]$Purpose) {
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing $Purpose : $Path" }
}
function Get-BepInExArchive {
    if ($BepInExZip) {
        $archive = [IO.Path]::GetFullPath($BepInExZip)
        Require-File $archive 'BepInEx ZIP'
        $digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($digest -ne $BepInExSha256) { throw "Unsupported or modified BepInEx ZIP (SHA256 $digest); expected official v$BepInExVersion x64 package." }
        return $archive
    }
    $downloadDir = Join-Path $ProjectRoot '.foundation-deps\downloads'
    New-Item -ItemType Directory -Path $downloadDir -Force | Out-Null
    $archive = Join-Path $downloadDir $BepInExAsset
    if (!(Test-Path -LiteralPath $archive)) {
        Write-Host "Downloading official BepInEx $BepInExVersion for Unity Mono Windows x64 ..."
        Invoke-WebRequest -Uri $BepInExUrl -OutFile $archive -UseBasicParsing
    }
    $digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($digest -ne $BepInExSha256) {
        Remove-Item -LiteralPath $archive -Force
        throw "BepInEx archive checksum mismatch (got $digest). Download cancelled."
    }
    return $archive
}
function Install-BepInExFiles([string]$Archive, [string]$Destination) {
    $work = Join-Path $ProjectRoot ('.foundation-deps\extract-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    try {
        Expand-Archive -LiteralPath $Archive -DestinationPath $work
        Require-File (Join-Path $work 'BepInEx\core\BepInEx.dll') 'BepInEx core assembly'
        Require-File (Join-Path $work 'BepInEx\core\0Harmony.dll') 'HarmonyX assembly'
        foreach ($entry in Get-ChildItem -LiteralPath $work -Force) {
            $target = Join-Path $Destination $entry.Name
            if (Test-Path -LiteralPath $target) {
                if ($entry.Name -eq 'BepInEx') {
                    # Never wipe an existing BepInEx plugin, config, or patcher folder.
                    $srcCore = Join-Path $entry.FullName 'core'
                    $dstCore = Join-Path $target 'core'
                    if (Test-Path -LiteralPath $dstCore) {
                        throw 'Existing BepInEx/core detected. Refusing to overwrite; supply a compatible installed BepInEx manually.'
                    }
                    Copy-Item -LiteralPath $srcCore -Destination $dstCore -Recurse
                    continue
                }
                throw "File already exists; refusing to overwrite loader file: $target"
            }
            Copy-Item -LiteralPath $entry.FullName -Destination $target -Recurse
        }
    } finally {
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    }
}
function Expand-ManagedAssemblies([string]$Archive, [string]$Destination) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    Require-File $Archive 'KingdomInc_Data.zip'
    $target = Join-Path $Destination 'KingdomInc_Data\Managed'
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    $zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($Archive))
    $count = 0
    try {
        foreach ($entry in $zip.Entries) {
            if (!$entry.FullName.StartsWith('KingdomInc_Data/Managed/', [StringComparison]::Ordinal)) { continue }
            if (!$entry.FullName.EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase)) { continue }
            # Only flat DLL names from the expected directory are extracted.
            $name = [IO.Path]::GetFileName($entry.FullName)
            if (!$name -or $entry.FullName -ne ('KingdomInc_Data/Managed/' + $name)) { continue }
            $dest = Join-Path $target $name
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $dest, $true)
            $count++
        }
    } finally { $zip.Dispose() }
    if ($count -lt 15) { throw "Only $count managed assemblies extracted; wrong game-data archive?" }
    Write-Host "Staged $count managed assemblies from the user's supplied game data archive."
}

if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK (8 or newer) is required to compile Foundation. Install it from Microsoft, then rerun.'
}
if ($InstallBepInEx -and !$GameRoot) { throw '-InstallBepInEx requires -GameRoot pointing to the installed game.' }
if (!$GameRoot -and !$GameDataZip) { throw 'Pass -GameRoot for an installed game OR -GameDataZip for the uploaded KingdomInc_Data.zip.' }
if ($GameRoot) {
    $buildGameRoot = [IO.Path]::GetFullPath($GameRoot)
    Require-File (Join-Path $buildGameRoot 'KingdomInc_Data\Managed\Assembly-CSharp.dll') 'Kingdom Inc managed assembly'
    if (($Verify -or $Install -or $InstallBepInEx)) {
        Require-File (Join-Path $buildGameRoot 'KingdomInc.exe') 'installed game executable'
    }
} else {
    $buildGameRoot = Join-Path $ProjectRoot '.foundation-deps\StagedKingdomInc'
    New-Item -ItemType Directory -Path $buildGameRoot -Force | Out-Null
    Expand-ManagedAssemblies $GameDataZip $buildGameRoot
}
$loader = Join-Path $buildGameRoot 'BepInEx\core\BepInEx.dll'
$harmony = Join-Path $buildGameRoot 'BepInEx\core\0Harmony.dll'
if (!(Test-Path -LiteralPath $loader) -or !(Test-Path -LiteralPath $harmony)) {
    if ($GameRoot -and !$InstallBepInEx) {
        throw 'BepInEx 5 is not installed in the game. To set it up, rerun with -InstallBepInEx; for an isolated build instead use -GameDataZip.'
    }
    if ($GameRoot -and (Get-Process -Name 'KingdomInc' -ErrorAction SilentlyContinue)) {
        throw 'Close Kingdom Inc before adding BepInEx.'
    }
    $archive = Get-BepInExArchive
    Install-BepInExFiles $archive $buildGameRoot
}
Require-File $loader 'BepInEx.dll'
Require-File $harmony '0Harmony.dll'
$managed = Join-Path $buildGameRoot 'KingdomInc_Data\Managed'
foreach ($assembly in @('UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.UI.dll','Unity.TextMeshPro.dll',
    'Unity.InputSystem.dll','UnityEngine.AssetBundleModule.dll','UnityEngine.ImageConversionModule.dll',
    'UnityEngine.UnityWebRequestModule.dll','UnityEngine.Physics2DModule.dll','UnityEngine.UIModule.dll',
    'UnityEngine.IMGUIModule.dll','UnityEngine.TextRenderingModule.dll')) {
    Require-File (Join-Path $managed $assembly) $assembly
}
$gameArg = '-p:GameRoot=' + $buildGameRoot.TrimEnd('\', '/').Replace('\', '/') + '/'
foreach ($project in @('Foundation.csproj','Examples\ExampleTechNodes\ExampleTechNodes.csproj','Examples\ExampleServices\ExampleServices.csproj','Examples\ExampleGameplay\ExampleGameplay.csproj','Examples\CoreMod\ExampleCoreMod.csproj')) {
    Write-Host "Building $project against the supplied game assemblies and BepInEx 5..."
    & dotnet build (Join-Path $ProjectRoot $project) -c Release --nologo $gameArg
    if ($LASTEXITCODE -ne 0) { throw "Build FAILED: $project. No release or installation was produced." }
}
if ($Verify) {
    if (!$GameRoot) { throw 'Runtime verification requires -GameRoot and the actual executable; game data alone cannot be launched.' }
    if (Get-Process -Name 'KingdomInc' -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before isolated verification.' }
    & (Join-Path $ProjectRoot 'Tools\Development\Verify-Isolated.ps1') -GameRoot $buildGameRoot -WithExistingPlugins:$WithExistingPlugins
    if ($LASTEXITCODE -ne 0) { throw 'Runtime verification failed; installation blocked.' }
}
if (!$SkipPackaging) {
    & (Join-Path $ProjectRoot 'Tools\Build\Package-Foundation.ps1') -GameRoot $buildGameRoot
    if ($LASTEXITCODE -ne 0) { throw 'Packaging failed.' }
}
if ($Install) {
    & (Join-Path $ProjectRoot 'Tools\Install\Deploy-Foundation.ps1') -GameRoot $buildGameRoot
    if ($LASTEXITCODE -ne 0) { throw 'Installation failed.' }
}
Write-Host "Foundation build complete. Release packages: $(Join-Path $ProjectRoot 'Releases')"
if (!$Verify) { Write-Host 'Runtime smoke checks were skipped (optional -Verify switch).'}
