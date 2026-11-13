param([string]$GameRoot = '')
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = Join-Path $modRoot '..\..' }
$gamePath = (Resolve-Path -LiteralPath $GameRoot).Path.Replace('\','/') + '/'
$meta = Get-Content -LiteralPath (Join-Path $modRoot 'mod.json') -Raw | ConvertFrom-Json
$version = [string]$meta.version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'SDK packaging requires a semantic release version.' }
$dll = Join-Path $modRoot 'bin\Release\netstandard2.1\Foundation.dll'
$xml = [IO.Path]::ChangeExtension($dll, '.xml')
if (-not (Test-Path -LiteralPath $dll) -or -not (Test-Path -LiteralPath $xml)) { throw 'Build Foundation in Release before packaging the SDK.' }
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3) -ne $version) { throw 'SDK binary version differs from mod.json.' }
$dllHash = (Get-FileHash -LiteralPath $dll).Hash
$runtimeDll = Join-Path $modRoot 'Release\Foundation.dll'
if (-not (Test-Path -LiteralPath $runtimeDll) -or (Get-FileHash -LiteralPath $runtimeDll).Hash -ne $dllHash) { throw 'SDK DLL must match the staged runtime release.' }
$scratchRoot = Join-Path $modRoot '_AI\Scratch'
$work = Join-Path $scratchRoot ('FoundationSdk-' + [Guid]::NewGuid().ToString('N'))
$stage = Join-Path $work 'Stage'
$extracted = Join-Path $work 'Extracted'
$output = Join-Path $modRoot 'Release'
$archive = Join-Path $output ('Foundation-SDK-v' + $version + '.zip')
try {
    New-Item -ItemType Directory -Path (Join-Path $stage 'lib'),(Join-Path $stage 'Docs'),(Join-Path $stage 'Examples') -Force | Out-Null
    Copy-Item -LiteralPath $dll,$xml -Destination (Join-Path $stage 'lib')
    foreach ($doc in Get-ChildItem -LiteralPath (Join-Path $modRoot 'Docs') -Recurse -File -Filter '*.md') {
        $relative = $doc.FullName.Substring((Join-Path $modRoot 'Docs').Length + 1)
        $destination = Join-Path (Join-Path $stage 'Docs') $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $doc.FullName -Destination $destination
    }
    foreach ($name in @('ExampleTechNodes','ExampleServices','ExampleGameplay','CoreMod','UIDemo','FrameworkConsumer')) {
        $destination = Join-Path (Join-Path $stage 'Examples') $name
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        $source = Join-Path (Join-Path $modRoot 'Examples') $name
        foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
            if ($file.FullName -match '[\\/](bin|obj)[\\/]') { continue }
            if ($file.Extension -in @('.cs','.csproj') -or $file.Name -eq 'mod.json') {
                $target = Join-Path $destination $file.FullName.Substring($source.Length + 1)
                New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
                Copy-Item -LiteralPath $file.FullName -Destination $target
            }
        }
    }
    $readme = [IO.File]::ReadAllText((Join-Path $modRoot 'SDK\README.md'))
    $readme = $readme.Replace('# Foundation SDK', '# Foundation SDK ' + $version).Replace('](../Docs/', '](Docs/').Replace('](../Examples/', '](Examples/')
    [IO.File]::WriteAllText((Join-Path $stage 'README.md'), $readme, [Text.UTF8Encoding]::new($false))
    foreach ($doc in Get-ChildItem -LiteralPath $stage -Recurse -File -Filter '*.md') {
        foreach ($link in [regex]::Matches([IO.File]::ReadAllText($doc.FullName), '\]\(([^)]+)\)')) {
            $target = $link.Groups[1].Value.Split('#')[0]
            if (-not $target -or $target -match '^https?://') { continue }
            if (-not (Test-Path -LiteralPath (Join-Path $doc.DirectoryName $target))) { throw "Broken packaged documentation link: $($doc.Name) -> $target" }
        }
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $candidate = Join-Path $work 'Foundation-SDK.zip'
    [IO.Compression.ZipFile]::CreateFromDirectory($stage, $candidate)
    [IO.Compression.ZipFile]::ExtractToDirectory($candidate, $extracted)
    $extractedDll = Join-Path $extracted 'lib\Foundation.dll'
    if ((Get-FileHash -LiteralPath $extractedDll).Hash -ne $dllHash) { throw 'Extracted SDK DLL does not match the runtime.' }
    $assemblies = @(Get-ChildItem -LiteralPath $extracted -Recurse -File -Filter '*.dll')
    if ($assemblies.Count -ne 1 -or $assemblies[0].Name -ne 'Foundation.dll') { throw 'SDK contains unexpected assemblies.' }
    if (-not (Test-Path -LiteralPath (Join-Path $extracted 'Docs\UI\Options-Tabs.md'))) { throw 'SDK Options API guide is missing.' }
    $projects = @(Get-ChildItem -LiteralPath (Join-Path $extracted 'Examples') -Recurse -File -Filter '*.csproj')
    if ($projects.Count -ne 6) { throw 'Expected six SDK example projects.' }
    foreach ($project in $projects) {
        & dotnet build $project.FullName -c Release --nologo ('-p:GameRoot=' + $gamePath)
        if ($LASTEXITCODE -ne 0) { throw "Extracted SDK example failed: $($project.Name)" }
    }
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    if (Test-Path -LiteralPath $archive) {
        $backup = Join-Path $modRoot ('_AI\Notes\Backups\SDK-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
        New-Item -ItemType Directory -Path $backup -Force | Out-Null
        Copy-Item -LiteralPath $archive -Destination $backup
    }
    Copy-Item -LiteralPath $candidate -Destination $archive -Force
    $hash = (Get-FileHash -LiteralPath $archive).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText(($archive + '.sha256'), ($hash + '  ' + [IO.Path]::GetFileName($archive) + "`n"))
    [pscustomobject]@{ Archive = $archive; Version = $version; SHA256 = $hash; RuntimeDllSHA256 = $dllHash; ExampleBuilds = $projects.Count; DocsLinksVerified = $true } | ConvertTo-Json
} finally {
    $absoluteWork = [IO.Path]::GetFullPath($work)
    if (-not $absoluteWork.StartsWith($scratchRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Refusing cleanup outside SDK scratch directory.' }
    if (Test-Path -LiteralPath $absoluteWork) {
        Add-Type -AssemblyName Microsoft.VisualBasic
        [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory($absoluteWork, [Microsoft.VisualBasic.FileIO.UIOption]::OnlyErrorDialogs, [Microsoft.VisualBasic.FileIO.RecycleOption]::SendToRecycleBin)
    }
}
