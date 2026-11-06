$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'Foundation.csproj'
[xml]$metadata = Get-Content -LiteralPath $project
$version = [string]$metadata.Project.PropertyGroup.Version
$dll = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Foundation.dll'
if (!(Test-Path -LiteralPath $dll)) { throw 'Build the Release project first.' }
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3) -ne $version) { throw 'Binary version does not match the project.' }
$releases = Join-Path $PSScriptRoot 'Releases'
$stage = Join-Path $releases ('Package-' + [Guid]::NewGuid().ToString('N'))
$plugin = Join-Path $stage 'BepInEx\plugins\Foundation'
New-Item -ItemType Directory -Path $plugin -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $plugin 'Foundation.dll')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Package\Foundation README.txt') -Destination $stage
$archive = Join-Path $releases ('Foundation-' + $version + '.zip')
if (Test-Path -LiteralPath $archive) {
    $backupDir = Join-Path $PSScriptRoot ('_Backup\Releases-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Move-Item -LiteralPath $archive -Destination $backupDir -Force
    if (Test-Path -LiteralPath ($archive + '.sha256')) { Move-Item -LiteralPath ($archive + '.sha256') -Destination $backupDir -Force }
}
Compress-Archive -LiteralPath (Join-Path $stage 'BepInEx'),(Join-Path $stage 'Foundation README.txt') -DestinationPath $archive
$clean = Join-Path $releases ('CleanExtraction-' + [Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $archive -DestinationPath $clean
$files = @(Get-ChildItem -LiteralPath $clean -Recurse -File)
$extracted = Join-Path $clean 'BepInEx\plugins\Foundation\Foundation.dll'
if ($files.Count -ne 2 -or !(Test-Path -LiteralPath $extracted) -or !(Test-Path -LiteralPath (Join-Path $clean 'Foundation README.txt'))) { throw 'Unexpected archive contents.' }
if ((Get-FileHash -LiteralPath $dll).Hash -ne (Get-FileHash -LiteralPath $extracted).Hash) { throw 'Cleanly extracted DLL does not match the build.' }
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant()
Set-Content -LiteralPath ($archive + '.sha256') -Value ($hash + '  ' + [IO.Path]::GetFileName($archive))
[pscustomobject]@{ Archive = $archive; Bytes = (Get-Item -LiteralPath $archive).Length; SHA256 = $hash; CleanExtractionVerified = $true } | ConvertTo-Json

# SDK contents are separate from the player install. Include only our assembly,
# IntelliSense documentation, guide and the consumer's two source files.
$sdkStage = Join-Path $releases ('Sdk-' + [Guid]::NewGuid().ToString('N'))
$sdkLib = Join-Path $sdkStage 'lib'
$sdkExample = Join-Path $sdkStage 'Examples\ExampleTechNodes'
New-Item -ItemType Directory -Path $sdkLib,$sdkExample -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination $sdkLib
Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($dll, '.xml')) -Destination $sdkLib
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SDK\README.md') -Destination $sdkStage
foreach ($name in @('ExamplePlugin.cs','ExampleTechNodes.csproj')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Examples\ExampleTechNodes\' + $name)) -Destination $sdkExample
}
$sdkArchive = Join-Path $releases ('Foundation-SDK-' + $version + '.zip')
if (Test-Path -LiteralPath $sdkArchive) {
    $backupDir = Join-Path $PSScriptRoot ('_Backup\Releases-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Move-Item -LiteralPath $sdkArchive -Destination $backupDir -Force
    if (Test-Path -LiteralPath ($sdkArchive + '.sha256')) { Move-Item -LiteralPath ($sdkArchive + '.sha256') -Destination $backupDir -Force }
}
Compress-Archive -LiteralPath $sdkLib,(Join-Path $sdkStage 'Examples'),(Join-Path $sdkStage 'README.md') -DestinationPath $sdkArchive
$sdkClean = Join-Path $releases ('CleanSdk-' + [Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $sdkArchive -DestinationPath $sdkClean
$sdkFiles = @(Get-ChildItem -LiteralPath $sdkClean -Recurse -File)
if ($sdkFiles.Count -ne 5 -or !(Test-Path -LiteralPath (Join-Path $sdkClean 'lib\Foundation.dll')) -or
    !(Test-Path -LiteralPath (Join-Path $sdkClean 'lib\Foundation.xml')) -or
    !(Test-Path -LiteralPath (Join-Path $sdkClean 'README.md')) -or
    !(Test-Path -LiteralPath (Join-Path $sdkClean 'Examples\ExampleTechNodes\ExamplePlugin.cs')) -or
    !(Test-Path -LiteralPath (Join-Path $sdkClean 'Examples\ExampleTechNodes\ExampleTechNodes.csproj'))) { throw 'Unexpected SDK contents.' }
if ((Get-FileHash -LiteralPath $dll).Hash -ne (Get-FileHash -LiteralPath (Join-Path $sdkClean 'lib\Foundation.dll')).Hash) { throw 'SDK DLL does not match the build.' }
$gameRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path.Replace('\','/') + '/'
& dotnet build (Join-Path $sdkClean 'Examples\ExampleTechNodes\ExampleTechNodes.csproj') -c Release --nologo ('-p:GameRoot=' + $gameRoot)
if ($LASTEXITCODE -ne 0) { throw 'The cleanly extracted SDK example did not compile.' }
$sdkHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sdkArchive).Hash.ToLowerInvariant()
Set-Content -LiteralPath ($sdkArchive + '.sha256') -Value ($sdkHash + '  ' + [IO.Path]::GetFileName($sdkArchive))
[pscustomobject]@{ Archive = $sdkArchive; Bytes = (Get-Item -LiteralPath $sdkArchive).Length; SHA256 = $sdkHash; CleanExampleBuildVerified = $true } | ConvertTo-Json
