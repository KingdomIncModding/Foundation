[CmdletBinding()]
param(
    [string]$GameRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..')),
    [string[]]$Mod = @(),
    [string]$AssemblyPath = '',
    [hashtable]$BuildProperty = @{},
    [string]$AllowlistPath = (Join-Path $PSScriptRoot 'reflection-allowlist.json'),
    [switch]$Enforce
)
$ErrorActionPreference = 'Stop'
$GameRoot = [IO.Path]::GetFullPath($GameRoot)
if ($AssemblyPath -and $Mod.Count -ne 1) {
    throw '-AssemblyPath requires specifying exactly one -Mod name.'
}
Add-Type -Path (Join-Path $GameRoot 'BepInEx\core\Mono.Cecil.dll')
$updates = Join-Path $GameRoot '_ModUpdates'
$destination = Join-Path $updates 'Foundation\Architecture'
New-Item -ItemType Directory -Path $destination -Force | Out-Null

$allowlist = @()
if ($AllowlistPath -and (Test-Path -LiteralPath $AllowlistPath -PathType Leaf)) {
    $parsedAllowlist = Get-Content -LiteralPath $AllowlistPath -Raw | ConvertFrom-Json
    if ($parsedAllowlist) { $allowlist = @($parsedAllowlist) }
}

function Get-StringSha256([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}

function Get-NormalizedRelPath([string]$baseDir, [string]$fullPath) {
    $baseFull = [IO.Path]::GetFullPath($baseDir).TrimEnd('\', '/')
    $itemFull = [IO.Path]::GetFullPath($fullPath)
    if ($itemFull.StartsWith($baseFull + '\', [StringComparison]::OrdinalIgnoreCase)) {
        return $itemFull.Substring($baseFull.Length + 1).Replace('/', '\')
    }
    return $itemFull
}

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

$msbuildProps = @()
$effectiveProps = @{ Configuration = 'Release' }
foreach ($k in $BuildProperty.Keys) {
    $effectiveProps[[string]$k] = [string]$BuildProperty[$k]
}
foreach ($k in ($effectiveProps.Keys | Sort-Object)) {
    $msbuildProps += ("-p:{0}={1}" -f $k, $effectiveProps[$k])
}
$invocationSummary = [pscustomobject]@{
    Script = $PSCommandPath
    ModFilter = @($Mod)
    AssemblyPath = $AssemblyPath
    BuildProperties = $effectiveProps
    AllowlistPath = $AllowlistPath
    Enforce = [bool]$Enforce
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
}

$summary = @()
foreach ($folder in Get-ChildItem -LiteralPath (Join-Path $GameRoot '_Mods') -Directory) {
    $metadata = Join-Path $folder.FullName 'mod.json'
    if (-not (Test-Path -LiteralPath $metadata) -or $folder.Name -eq 'Foundation') { continue }
    if ($Mod.Count -gt 0 -and $folder.Name -notin $Mod) { continue }
    $identity = Get-Content -LiteralPath $metadata -Raw | ConvertFrom-Json
    $project = @(Get-ChildItem -LiteralPath $folder.FullName -File -Filter '*.csproj' | Where-Object { $_.Name -notmatch 'Smoke|Probe|Verification' })
    if ($project.Count -eq 0) { continue }

    $msbuildEvalOutput = & dotnet msbuild $project[0].FullName -nologo -getProperty:TargetPath,FoundationDll -getItem:Compile,Reference @msbuildProps
    if ($LASTEXITCODE -ne 0) { throw ('Could not evaluate TargetPath and production source items for: ' + $folder.Name) }
    $evalText = ($msbuildEvalOutput -join "`n")
    $jsonStart = $evalText.IndexOf('{')
    if ($jsonStart -lt 0) { throw ('MSBuild evaluation returned unexpected non-JSON output for: ' + $folder.Name) }
    $evalJson = $evalText.Substring($jsonStart) | ConvertFrom-Json
    $evaluatedTargetPath = [string]$evalJson.Properties.TargetPath
    if (-not $evaluatedTargetPath) { throw ('MSBuild TargetPath evaluated empty for: ' + $folder.Name) }
    $evaluatedTargetPath = [IO.Path]::GetFullPath($evaluatedTargetPath)
    $foundationDllProp = if ($evalJson.Properties.FoundationDll) { [string]$evalJson.Properties.FoundationDll } else { $null }
    $foundationRefItem = @($evalJson.Items.Reference | Where-Object { $_.Identity -eq 'Foundation' } | Select-Object -First 1)
    $foundationRefHint = if ($foundationRefItem.Count -gt 0 -and $foundationRefItem[0].HintPath) {
        [IO.Path]::GetFullPath([string]$foundationRefItem[0].HintPath)
    } else { $null }
    $foundationRefHash = if ($foundationRefHint -and (Test-Path -LiteralPath $foundationRefHint -PathType Leaf)) {
        (Get-FileHash -LiteralPath $foundationRefHint -Algorithm SHA256).Hash.ToLowerInvariant()
    } else { $null }

    $dll = if ($AssemblyPath) { [IO.Path]::GetFullPath($AssemblyPath) } else { $evaluatedTargetPath }
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
        throw ("Evaluated build output missing for $($folder.Name): $dll")
    }

    $gitRev = $null
    if (Test-Path -LiteralPath (Join-Path $folder.FullName '.git')) {
        $gitOut = & git -C $folder.FullName rev-parse HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and $gitOut) { $gitRev = [string]$gitOut.Trim() }
    }

    $compileItems = @($evalJson.Items.Compile)

    $projHash = (Get-FileHash -LiteralPath $project[0].FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $fingerprintLines = New-Object System.Collections.Generic.List[string]
    $fingerprintLines.Add("csproj:$projHash")

    $modAllowlist = @($allowlist | Where-Object { [string]$_.mod -eq $folder.Name })
    $allowlistedReflection = New-Object System.Collections.Generic.List[object]
    $unresolvedReflection = New-Object System.Collections.Generic.List[object]
    $allowlistedFiles = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)

    foreach ($item in ($compileItems | Sort-Object FullPath)) {
        if (-not $item.FullPath -or -not (Test-Path -LiteralPath $item.FullPath -PathType Leaf)) { continue }
        $source = Get-Item -LiteralPath $item.FullPath
        $relFile = Get-NormalizedRelPath $folder.FullName $source.FullName
        $fileHash = (Get-FileHash -LiteralPath $source.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $fingerprintLines.Add("${relFile}:${fileHash}")

        $line = 0
        foreach ($text in [IO.File]::ReadLines($source.FullName)) {
            $line++
            $trimmed = $text.Trim()
            if ($trimmed.StartsWith('//') -or $trimmed.StartsWith('/*') -or $trimmed.StartsWith('*')) { continue }
            if ($text -cmatch 'AccessTools\.|\bHarmonyPatch\b|HarmonyLib\.|new\s+Harmony\s*\(|Type\.GetType\(|TypeByName|Assembly-CSharp|FoundationApi\.Native\.|GetAssemblies\s*\(|\.(?:GetType\s*\([^\)]|GetMethod\s*\(|GetField\s*\(|GetProperty\s*\(|GetConstructor\s*\(|GetMember\s*\()|Assembly\.(?:Load|LoadFrom)\s*\(') {
                $matchedRule = $modAllowlist | Where-Object {
                    ($_.file.Replace('/', '\') -eq $relFile) -and ($text.IndexOf([string]$_.match, [StringComparison]::Ordinal) -ge 0)
                } | Select-Object -First 1
                if ($null -ne $matchedRule) {
                    $allowlistedFiles.Add($relFile) | Out-Null
                    $allowlistedReflection.Add([pscustomobject]@{
                        File = $relFile
                        Line = $line
                        Code = $trimmed
                        Kind = $matchedRule.kind
                        Reason = $matchedRule.reason
                    })
                } else {
                    $unresolvedReflection.Add([pscustomobject]@{
                        File = $relFile
                        Line = $line
                        Code = $trimmed
                        Source = 'SourceScan'
                    })
                }
            }
        }
    }

    $sourceFingerprint = Get-StringSha256 ($fingerprintLines -join "`n")

    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
    try {
        $references = @($assembly.MainModule.AssemblyReferences | Where-Object { $_.Name -eq 'Assembly-CSharp' } | ForEach-Object { $_.FullName })
        $types = @($assembly.MainModule.GetTypeReferences() | Where-Object { $_.Scope.Name -eq 'Assembly-CSharp' } | ForEach-Object { $_.FullName } | Sort-Object -Unique)
        $members = @($assembly.MainModule.GetMemberReferences() | Where-Object { $_.DeclaringType.Scope.Name -eq 'Assembly-CSharp' } | ForEach-Object { $_.FullName } | Sort-Object -Unique)

        # Also inspect IL calls for dynamic reflection that could bypass literal source regexes.
        $ilReflectionRefs = @(
            $assembly.MainModule.GetMemberReferences() | Where-Object {
                $decl = $_.DeclaringType.FullName
                $name = $_.Name
                ($decl -eq 'System.Type' -and $name -in @('GetType','GetMethod','GetField','GetProperty','GetConstructor','GetMember','InvokeMember')) -or
                ($decl -eq 'System.Reflection.Assembly' -and $name -in @('GetType','Load','LoadFrom')) -or
                ($decl -eq 'HarmonyLib.AccessTools') -or
                ($decl -eq 'HarmonyLib.Harmony' -and $name -eq 'Patch')
            } | ForEach-Object { $_.FullName } | Sort-Object -Unique
        )
        if ($ilReflectionRefs.Count -gt 0 -and $allowlistedReflection.Count -eq 0 -and $unresolvedReflection.Count -eq 0) {
            foreach ($ref in $ilReflectionRefs) {
                $unresolvedReflection.Add([pscustomobject]@{
                    File = $dll
                    Line = 0
                    Code = $ref
                    Source = 'ILMemberRef'
                })
            }
        }

        $unresolvedArr = $unresolvedReflection.ToArray()
        $allowlistedArr = $allowlistedReflection.ToArray()
        $reflectionAll = @($unresolvedArr) + @($allowlistedArr)
        $passed = ($references.Count -eq 0 -and $types.Count -eq 0 -and $members.Count -eq 0 -and $unresolvedArr.Length -eq 0)
        $result = [pscustomobject]@{
            Mod = $folder.Name
            Version = $identity.version
            TargetPath = $evaluatedTargetPath
            FoundationDllProperty = $foundationDllProp
            FoundationReferencePath = $foundationRefHint
            FoundationReferenceSHA256 = $foundationRefHash
            Assembly = $dll
            AssemblySHA256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
            AssemblyLastWriteUtc = (Get-Item -LiteralPath $dll).LastWriteTimeUtc.ToString('o')
            GitRevision = $gitRev
            SourceFingerprint = $sourceFingerprint
            Invocation = $invocationSummary
            NativeAssemblies = $references
            NativeTypes = $types
            NativeMembers = $members
            ILReflectionRefs = $ilReflectionRefs
            AllowlistedReflection = @($allowlistedArr)
            UnresolvedReflection = @($unresolvedArr)
            ReflectionReview = @($reflectionAll)
            Passed = $passed
        }
        $modDestination = Join-Path $updates ($folder.Name + '\Architecture')
        New-Item -ItemType Directory -Path $modDestination -Force | Out-Null
        $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $modDestination 'game-boundary.json')
        $summary += [pscustomobject]@{
            Mod = $folder.Name
            NativeTypes = $types.Count
            NativeMembers = $members.Count
            AllowlistedReflection = $allowlistedReflection.Count
            UnresolvedReflection = $unresolvedReflection.Count
            Passed = $result.Passed
        }
    } finally { $assembly.Dispose() }
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $destination 'consumer-boundaries.json')
$summary | Format-Table -AutoSize
if ($Enforce -and @($summary | Where-Object { -not $_.Passed }).Count -gt 0) {
    throw 'Direct game dependencies or unresolved reflection findings remain. See per-mod Architecture/game-boundary.json.'
}
