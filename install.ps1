param(
    [string]$GameRoot = (Join-Path $PSScriptRoot '..\..'),
    [switch]$StageForNextLaunch,
    [switch]$WhenGameCloses,
    [string]$PendingDll,
    [string]$ExpectedSha256
)
$ErrorActionPreference = 'Stop'
$GameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
if (!(Test-Path -LiteralPath (Join-Path $GameRoot 'KingdomInc.exe'))) { throw 'Choose the Kingdom Inc game directory.' }
$source = if ($PendingDll) { $PendingDll } else { Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Foundation.dll' }
if (!(Test-Path -LiteralPath $source)) { throw 'Build the Release project first.' }
if ($PendingDll) {
    if (!$ExpectedSha256 -or (Get-FileHash -LiteralPath $source).Hash -ne $ExpectedSha256) { throw 'Pending DLL hash verification failed.' }
    do {
        $running = @(Get-Process -Name KingdomInc -ErrorAction SilentlyContinue)
        foreach ($game in $running) { try { $game.WaitForExit() } catch { if (!$game.HasExited) { throw } } }
    } while (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue)
    $installed = Join-Path $GameRoot 'BepInEx\plugins\Foundation\Foundation.dll'
    if ((Test-Path -LiteralPath $installed) -and
        [Reflection.AssemblyName]::GetAssemblyName($installed).Version -ge [Reflection.AssemblyName]::GetAssemblyName($source).Version) {
        Write-Output 'An equal or newer Foundation version is already installed.'
        return
    }
}
$gameRunning = [bool](Get-Process -Name KingdomInc -ErrorAction SilentlyContinue)
if ($gameRunning -and $WhenGameCloses) {
    $pending = Join-Path $PSScriptRoot ('Releases\Pending-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $pending -Force | Out-Null
    $snapshot = Join-Path $pending 'Foundation.dll'
    Copy-Item -LiteralPath $source -Destination $snapshot
    $digest = (Get-FileHash -LiteralPath $snapshot).Hash
    $shellExe = (Get-Process -Id $PID).Path
    $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',('"' + $PSCommandPath + '"'),'-GameRoot',('"' + $GameRoot + '"'),'-PendingDll',('"' + $snapshot + '"'),'-ExpectedSha256',$digest)
    $helper = Start-Process -FilePath $shellExe -WindowStyle Hidden -ArgumentList $arguments -RedirectStandardOutput (Join-Path $pending 'install.log') -RedirectStandardError (Join-Path $pending 'install-error.log') -PassThru
    [pscustomobject]@{ Status = 'Queued until Kingdom Inc closes'; HelperPID = $helper.Id; PendingDll = $snapshot; SHA256 = $digest } | ConvertTo-Json
    return
}
if ($gameRunning -and !$StageForNextLaunch) { throw 'Close Kingdom Inc before installing Foundation, or use -StageForNextLaunch for a first installation.' }
$destination = Join-Path $GameRoot 'BepInEx\plugins\Foundation'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$target = Join-Path $destination 'Foundation.dll'
if (Test-Path -LiteralPath $target) {
    if ($gameRunning) { throw 'An installed DLL already exists. Close Kingdom Inc before replacing it.' }
    $backup = Join-Path $PSScriptRoot ('_Backup\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Copy-Item -LiteralPath $target -Destination $backup
}
Copy-Item -LiteralPath $source -Destination $target -Force
if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw 'Installed DLL verification failed.' }
Write-Output "Installed Foundation: $target"
if ($gameRunning) { Write-Output 'First installation staged. Restart Kingdom Inc to load Foundation.' }

