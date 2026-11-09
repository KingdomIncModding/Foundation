param(
    [string]$GameRoot = '',
    [switch]$WithExistingPlugins
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = Join-Path $ProjectRoot '..\..' }
$gameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$verification = Join-Path $ProjectRoot 'Verification'
$runtime = Join-Path $verification ('Runtime-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
# Unity, Doorstop and renderer extras differ across game updates. Hardlink required
# launch files only; carry optional files/directories when present.
foreach ($name in @('KingdomInc.exe','UnityPlayer.dll','winhttp.dll','doorstop_config.ini')) {
    $source = Join-Path $gameRoot $name
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing runtime launch dependency: $source" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $runtime $name)
}
foreach ($name in @('UnityCrashHandler64.exe','.doorstop_version')) {
    $source = Join-Path $gameRoot $name
    if (Test-Path -LiteralPath $source -PathType Leaf) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $runtime $name)
    }
}
foreach ($name in @('KingdomInc_Data','MonoBleedingEdge','D3D12')) {
    $source = Join-Path $gameRoot $name
    if ($name -eq 'KingdomInc_Data' -and !(Test-Path -LiteralPath $source -PathType Container)) {
        throw "Missing game data directory: $source"
    }
    if (Test-Path -LiteralPath $source -PathType Container) {
        New-Item -ItemType Junction -Path (Join-Path $runtime $name) -Value $source | Out-Null
    }
}
$bep = Join-Path $runtime 'BepInEx'
New-Item -ItemType Directory -Path (Join-Path $bep 'plugins\Foundation'),(Join-Path $bep 'config') -Force | Out-Null
New-Item -ItemType Junction -Path (Join-Path $bep 'core') -Value (Join-Path $gameRoot 'BepInEx\core') | Out-Null
$originalBepConfig = Join-Path $gameRoot 'BepInEx\config\BepInEx.cfg'
if (Test-Path -LiteralPath $originalBepConfig -PathType Leaf) {
    Copy-Item -LiteralPath $originalBepConfig -Destination (Join-Path $bep 'config\BepInEx.cfg')
} # Clean BepInEx installations generate their defaults on first launch.
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'bin\Release\netstandard2.1\Foundation.dll') -Destination (Join-Path $bep 'plugins\Foundation\Foundation.dll')
# Exercise the public API from a separate consumer assembly with a real BepInEx dependency.
$example = Join-Path $ProjectRoot 'Examples\ExampleTechNodes\bin\Release\netstandard2.1\ExampleTechNodes.dll'
if (!(Test-Path -LiteralPath $example)) { throw 'Build Examples/ExampleTechNodes in Release first.' }
New-Item -ItemType Directory -Path (Join-Path $bep 'plugins\FoundationExample') -Force | Out-Null
Copy-Item -LiteralPath $example -Destination (Join-Path $bep 'plugins\FoundationExample\ExampleTechNodes.dll')
$services = Join-Path $ProjectRoot 'Examples\ExampleServices\bin\Release\netstandard2.1\ExampleServices.dll'
if (!(Test-Path -LiteralPath $services)) { throw 'Build Examples/ExampleServices in Release first.' }
New-Item -ItemType Directory -Path (Join-Path $bep 'plugins\FoundationServices') -Force | Out-Null
Copy-Item -LiteralPath $services -Destination (Join-Path $bep 'plugins\FoundationServices\ExampleServices.dll')
if ($WithExistingPlugins) {
    # BepInEx accepts both subdirectory plugins and DLLs loose in plugins/.
    # A compatibility run must include both or it can falsely report success.
    $installedPlugins = Join-Path $gameRoot 'BepInEx\plugins'
    if (Test-Path -LiteralPath $installedPlugins -PathType Container) {
        foreach ($plugin in Get-ChildItem -LiteralPath $installedPlugins -Force) {
            if ($plugin.Name -in @('Foundation','FoundationExample','FoundationServices',
                'Foundation.dll','ExampleTechNodes.dll','ExampleServices.dll')) { continue }
            Copy-Item -LiteralPath $plugin.FullName -Destination (Join-Path $bep 'plugins') -Recurse -Force
        }
    }
    # The normal game may already own port 17840. This private runtime uses 17849.
    [IO.File]::WriteAllText((Join-Path $bep 'config\urakka.kingdominc.factoryapi.cfg'), "[Server]`nPort = 17849`n")
}
$logPath = Join-Path $runtime 'diagnostic.log'
$resultFolder = Join-Path $runtime '_Mods\Foundation\Verification'
$resultPath = Join-Path $resultFolder 'runtime-tests.json'
$exe = Join-Path $runtime 'KingdomInc.exe'
$owned = Start-Process -FilePath $exe -WorkingDirectory $runtime -WindowStyle Hidden -ArgumentList @('-foundation-smoke','-screen-width','1600','-screen-height','1000','-screen-fullscreen','0','-logFile',('"' + $logPath + '"')) -PassThru
try {
    $deadline = (Get-Date).AddSeconds(55)
    while ((Get-Date) -lt $deadline -and -not (Test-Path -LiteralPath $resultPath)) { Start-Sleep -Milliseconds 500 }
    if (-not (Test-Path -LiteralPath $resultPath)) {
        if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Tail 75 }
        throw "Isolated diagnostic did not complete. See $logPath"
    }
    # Let Unity finish teardown so post-report exceptions are included in the log.
    $owned.WaitForExit(5000) | Out-Null
    $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    foreach ($file in Get-ChildItem -LiteralPath $resultFolder -File) {
        if ($WithExistingPlugins -and $file.Name -eq 'runtime-tests.json') { continue }
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $verification $file.Name) -Force
    }
    if ($WithExistingPlugins) { Copy-Item -LiteralPath $resultPath -Destination (Join-Path $verification 'compatibility-tests.json') -Force }
    $savedLog = if ($WithExistingPlugins) { 'unity-companion.log' } else { 'unity-isolated.log' }
    Copy-Item -LiteralPath $logPath -Destination (Join-Path $verification $savedLog) -Force
    $result | Select-Object passed,version,gameVersion,count,plugins,error | ConvertTo-Json -Depth 8
    if (-not $result.passed) { throw "Isolated diagnostic failed. See $logPath" }
    # The runtime suite intentionally triggers a failed mod-data migration and logs
    # that handled exception to prove earlier save JSON is preserved. A blanket
    # search for 'Exception:' therefore rejects a passing test. RuntimeCheck
    # subscribes to Unity's exception callback and reports unexpected exceptions
    # through runtime-tests.json, which is the authoritative test result. Preserve
    # full Unity logs for diagnosis rather than failing on expected test fixtures.
    # BepInEx plugin resolution errors usually appear in its own log, not Unity's
    # -logFile output. Keep a copy and treat loader errors as verification failures.
    $bepLog = Join-Path $bep 'LogOutput.log'
    if (Test-Path -LiteralPath $bepLog -PathType Leaf) {
        $destBepLog = if ($WithExistingPlugins) { 'bepinex-companion.log' } else { 'bepinex-isolated.log' }
        Copy-Item -LiteralPath $bepLog -Destination (Join-Path $verification $destBepLog) -Force
        if (Select-String -LiteralPath $bepLog -Pattern '\[Error\s*:' -Quiet) {
            throw "BepInEx loader/plugin error found in $bepLog"
        }
    }
} finally {
    $owned.Refresh()
    if (-not $owned.HasExited) { Stop-Process -Id $owned.Id }
    foreach ($replacement in Get-CimInstance Win32_Process -Filter "Name='KingdomInc.exe'") {
        if ($replacement.ExecutablePath -eq $exe -and $replacement.CommandLine.Contains($logPath)) { Stop-Process -Id $replacement.ProcessId }
    }
}


