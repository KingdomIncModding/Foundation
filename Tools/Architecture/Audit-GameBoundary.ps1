[CmdletBinding()]
param(
    [string]$GameRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..')),
    [string[]]$Mod = @(),
    [switch]$Enforce
)
$ErrorActionPreference = 'Stop'
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
Add-Type -Path (Join-Path $GameRoot 'BepInEx\core\Mono.Cecil.dll')
$updates = Join-Path $GameRoot '_ModUpdates'
$destination = Join-Path $updates 'Foundation\Architecture'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$nativePath = Join-Path $GameRoot 'KingdomInc_Data\Managed\Assembly-CSharp.dll'
$native = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($nativePath)
try {
    $surface = @(
        foreach ($type in $native.MainModule.GetTypes()) {
            [pscustomobject]@{
                Type = $type.FullName
                BaseType = if ($type.BaseType) { $type.BaseType.FullName } else { $null }
                Public = $type.IsPublic -or $type.IsNestedPublic
                Methods = @($type.Methods | ForEach-Object { $_.FullName })
                Fields = @($type.Fields | ForEach-Object { $_.FullName })
                Properties = @($type.Properties | ForEach-Object { $_.FullName })
                Events = @($type.Events | ForEach-Object { $_.FullName })
            }
        }
    )
    [pscustomobject]@{
        GameVersion = '1.0.14'
        AssemblySHA256 = (Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash.ToLowerInvariant()
        Types = $surface
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $destination 'native-surface-1.0.14.json')
} finally { $native.Dispose() }
$summary = @()
foreach ($folder in Get-ChildItem -LiteralPath (Join-Path $GameRoot '_Mods') -Directory) {
    $metadata = Join-Path $folder.FullName 'mod.json'
    if (-not (Test-Path -LiteralPath $metadata) -or $folder.Name -eq 'Foundation') { continue }
    if ($Mod.Count -gt 0 -and $folder.Name -notin $Mod) { continue }
    $identity = Get-Content -LiteralPath $metadata -Raw | ConvertFrom-Json
    $project = @(Get-ChildItem -LiteralPath $folder.FullName -File -Filter '*.csproj' | Where-Object { $_.Name -notmatch 'Smoke|Probe|Verification' })
    if ($project.Count -eq 0) { continue }
    [xml]$xml = Get-Content -LiteralPath $project[0].FullName -Raw
    $assemblyName = @($xml.Project.PropertyGroup.AssemblyName | Where-Object { $_ })[0]
    if (-not $assemblyName) { $assemblyName = $project[0].BaseName }
    $dll = Join-Path $folder.FullName ('bin\Release\netstandard2.1\' + $assemblyName + '.dll')
    if (-not (Test-Path -LiteralPath $dll)) { $dll = Join-Path $folder.FullName ('Release\' + $assemblyName + '.dll') }
    if (-not (Test-Path -LiteralPath $dll)) { throw ('Build output missing: ' + $folder.Name) }
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
    try {
        $references = @($assembly.MainModule.AssemblyReferences | Where-Object { $_.Name -eq 'Assembly-CSharp' } | ForEach-Object { $_.FullName })
        $types = @($assembly.MainModule.GetTypeReferences() | Where-Object { $_.Scope.Name -eq 'Assembly-CSharp' } | ForEach-Object { $_.FullName } | Sort-Object -Unique)
        $members = @($assembly.MainModule.GetMemberReferences() | Where-Object { $_.DeclaringType.Scope.Name -eq 'Assembly-CSharp' } | ForEach-Object { $_.FullName } | Sort-Object -Unique)
        $compileOutput = & dotnet msbuild $project[0].FullName -nologo -getItem:Compile -p:Configuration=Release
        if ($LASTEXITCODE -ne 0) { throw ('Could not evaluate production source items: ' + $folder.Name) }
        $compileItems = ($compileOutput -join "`n" | ConvertFrom-Json).Items.Compile
        $reflection = @(
            foreach ($item in $compileItems) {
                if (-not (Test-Path -LiteralPath $item.FullPath)) { continue }
                $source = Get-Item -LiteralPath $item.FullPath
                $line = 0
                foreach ($text in [IO.File]::ReadLines($source.FullName)) {
                    $line++
                    if ($text -match 'AccessTools\.|HarmonyPatch|Type\.GetType\(|TypeByName|Assembly-CSharp|FoundationApi\.Native\.') {
                        [pscustomobject]@{ File=$source.FullName.Substring($folder.FullName.Length + 1); Line=$line; Code=$text.Trim() }
                    }
                }
            }
        )
        $result = [pscustomobject]@{
            Mod=$folder.Name; Version=$identity.version; Assembly=$dll
            AssemblySHA256=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
            NativeAssemblies=$references; NativeTypes=$types; NativeMembers=$members; ReflectionReview=$reflection
            Passed=($references.Count -eq 0 -and $types.Count -eq 0 -and $members.Count -eq 0)
        }
        $modDestination = Join-Path $updates ($folder.Name + '\Architecture')
        New-Item -ItemType Directory -Path $modDestination -Force | Out-Null
        $result | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $modDestination 'game-boundary.json')
        $summary += [pscustomobject]@{ Mod=$folder.Name; NativeTypes=$types.Count; NativeMembers=$members.Count; ReflectionReview=$reflection.Count; Passed=$result.Passed }
    } finally { $assembly.Dispose() }
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $destination 'consumer-boundaries.json')
$summary | Format-Table -AutoSize
if ($Enforce -and @($summary | Where-Object { -not $_.Passed }).Count -gt 0) { throw 'Direct game dependencies remain. See per-mod Architecture/game-boundary.json.' }
