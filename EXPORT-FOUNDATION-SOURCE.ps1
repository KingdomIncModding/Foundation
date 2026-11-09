# Export Foundation source without build output, logs, diagnostics, analytics, or local secrets.
# Windows PowerShell 5.1 and PowerShell 7 compatible. No third-party tools needed.
[CmdletBinding()]
param(
    [string]$SourceRoot = '',
    [string]$OutputDirectory,
    [int]$MaxFileSizeMB = 50,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Should-SkipDirectory {
    param([string]$Name, [string]$RelativePath)

    # Never export build artifacts, previous archives, temporary data or credentials.
    $alwaysSkip = @(
        '.git', '.vs', '.idea', '.vscode', '.foundation-deps', '.tools-cache',
        'bin', 'obj', 'Releases', 'Release', 'Debug', 'publish', 'dist',
        'artifacts', 'packages-cache', 'node_modules', '__pycache__',
        '.pytest_cache', '.mypy_cache', '.ruff_cache', '.cache', 'Cache',
        'Caches', 'Logs', 'Log', 'Reports', 'Report', 'TestResults',
        'Verification', 'Verifications', 'CrashDiagnostics',
        '_Backup', 'Backup', 'Backups', 'Temp', 'Temporary', 'tmp',
        'SourceExports', 'Foundation_Source_Exports',
        '.secrets', 'Secrets', 'Credentials', 'Private', 'UserData',
        '.venv', 'venv', 'coverage', 'htmlcov', '.coverage'
    )
    if ($alwaysSkip -contains $Name) { return $true }

    if ($Name -match '^(CrashDiagnostics|MachineDiagnostics|DiagnosticReports)[-_]') { return $true }
    if ($Name -match '^Foundation[_-].*(Verification_Package|Framework_1\.|Test_Hotfix|Unified_0\.)') { return $true }
    if ($Name -match '^(CleanSdk|Sdk|Isolated|Staging|Extract)-[a-f0-9]{8,}$') { return $true }

    # Exclude generated analytics/diagnostics data, but keep any source code
    # implementing analytics, telemetry, or diagnostics inside src/.
    $inSource = $RelativePath -match '^(?i:src)/'
    if (-not $inSource -and $Name -match '^(?i:Analytics|Diagnostics|TelemetryOutput|MetricsOutput|Profiling|ProfilerData)$') {
        return $true
    }
    return $false
}

function Should-SkipFile {
    param([string]$Name, [long]$Length)

    if ($Length -gt ([long]$MaxFileSizeMB * 1MB)) { return $true }
    if ($Name -ieq 'SOURCE-EXPORT-MANIFEST.json') { return $true }
    if ($Name -match '^(?i:\.env($|\.)|\.npmrc$|\.pypirc$|id_rsa|id_ed25519)') {
        if ($Name -ine '.env.example') { return $true }
    }
    if ($Name -match '^(?i:CrashDiagnostics|MachineDiagnostics|BepInEx-Log|unity-isolated|core-verification|machine-verification|foundation-native-smoke|runtime-tests|combined-verification|last-(Smoke|Play)|diagnostic-index|deployed-Foundation)') {
        return $true
    }
    if ($Name -match '(?i)(-report|-results|-metrics|-analytics|-diagnostics|\.coverage)\.(json|jsonl|csv|txt|log|xml)$') { return $true }
    if ($Name -match '(?i)\.(dll|exe|pdb|mdb|zip|7z|rar|tar|gz|nupkg|snupkg|log|log\.[0-9]+|dmp|mdmp|etl|trace|trx|coverage|cache|pid|bak|tmp|temp|user|suo|vsidx|db|sqlite|sqlite3|sqlite3-wal|sqlite3-shm|psess|diagsession|bin|obj|ilk|pfx|p12|pem|key|cer|crt|sha256|sha512|sha1|pyc)$') { return $true }
    if ($Name -match '(?i)(~$|\.orig$|\.rej$|\.swp$|\.swo$)') { return $true }
    if ($Name -in @('Thumbs.db', '.DS_Store', 'desktop.ini')) { return $true }
    return $false
}

# Resolve script location AFTER parameter binding. $PSScriptRoot is not a
# reliable default parameter expression in every Windows PowerShell host.
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $scriptLocation = $PSCommandPath
    if ([string]::IsNullOrWhiteSpace($scriptLocation)) {
        $scriptLocation = $MyInvocation.MyCommand.Path
    }
    if ([string]::IsNullOrWhiteSpace($scriptLocation)) {
        throw 'Cannot determine script location. Re-run with -SourceRoot "C:\path\to\Foundation".'
    }
    $SourceRoot = Split-Path -Path $scriptLocation -Parent
}

# Some Windows CMD launchers passed a quoted directory with a trailing
# backslash ("C:\...\Foundation\"). Windows can interpret the trailing
# backslash as escaping the closing quote, leaving a literal quote in the
# parameter. The v1.0.2 CMD launcher avoids passing this argument at all.
# Still sanitize the legacy invocation so it fails clearly or works safely.
$SourceRoot = [Environment]::ExpandEnvironmentVariables($SourceRoot.Trim().Trim([char]34))
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    throw 'SourceRoot is empty. Specify -SourceRoot with your Foundation folder.'
}
if ($SourceRoot.IndexOf([char]34) -ge 0 -or $SourceRoot.IndexOf([char]0) -ge 0) {
    throw "SourceRoot contains illegal quote/NUL characters: [$SourceRoot]. Use the CMD launcher from v1.0.2."
}
try {
    $sourceExists = Test-Path -LiteralPath $SourceRoot -PathType Container -ErrorAction Stop
} catch {
    throw "Cannot access SourceRoot [$SourceRoot]: $($_.Exception.Message)"
}
if (-not $sourceExists) {
    throw "Source directory does not exist: [$SourceRoot]"
}
$root = (Get-Item -LiteralPath $SourceRoot -ErrorAction Stop).FullName.TrimEnd([char[]]@('\', '/'))
if (-not (Test-Path -LiteralPath (Join-Path $root 'Foundation.csproj') -PathType Leaf)) {
    throw "Foundation.csproj not found in: $root. Place the script at the Foundation project root, or provide -SourceRoot."
}
if (-not (Test-Path -LiteralPath (Join-Path $root 'src') -PathType Container)) {
    throw "src directory missing in: $root"
}
if ($MaxFileSizeMB -le 0) { throw 'MaxFileSizeMB must be greater than zero.' }

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $parent = [IO.Directory]::GetParent($root).FullName
    $OutputDirectory = Join-Path $parent 'Foundation_Source_Exports'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    throw 'OutputDirectory is empty. Specify a valid output path.'
}
try {
    $outDir = [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($OutputDirectory.Trim()))
} catch {
    throw "Invalid OutputDirectory '$OutputDirectory': $($_.Exception.Message)"
}
if (-not $DryRun) { [IO.Directory]::CreateDirectory($outDir) | Out-Null }

# This also guards against including the ZIP in itself if a custom output
# folder inside the project is supplied.
$files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
$skippedDirs = 0
$skippedFiles = 0
$skippedBytes = [long]0
$totalBytes = [long]0

function Visit-Directory {
    param([string]$DirectoryPath)
    foreach ($entry in (Get-ChildItem -LiteralPath $DirectoryPath -Force -ErrorAction Stop | Sort-Object FullName)) {
        if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            $script:skippedFiles++
            continue
        }
        $relative = $entry.FullName.Substring($root.Length).TrimStart([char[]]@('\', '/')).Replace('\', '/')
        if ($entry.PSIsContainer) {
            if ($entry.FullName.TrimEnd([char[]]@('\', '/')).Equals($outDir.TrimEnd([char[]]@('\', '/')), [StringComparison]::OrdinalIgnoreCase)) {
                $script:skippedDirs++
                continue
            }
            if (Should-SkipDirectory -Name $entry.Name -RelativePath $relative) {
                $script:skippedDirs++
                continue
            }
            Visit-Directory -DirectoryPath $entry.FullName
        } else {
            if (Should-SkipFile -Name $entry.Name -Length $entry.Length) {
                $script:skippedFiles++
                $script:skippedBytes += $entry.Length
                continue
            }
            $files.Add([System.IO.FileInfo]$entry)
            $script:totalBytes += $entry.Length
        }
    }
}

Visit-Directory -DirectoryPath $root
if ($files.Count -lt 10) { throw "Only $($files.Count) files selected. Aborting: unexpected Foundation source layout." }

Write-Host "[INFO] Foundation source: $root" -ForegroundColor Cyan
Write-Host "[INFO] Files included: $($files.Count); files skipped: $skippedFiles; directories pruned: $skippedDirs"
Write-Host ('[INFO] Uncompressed source selected: {0:N2} MB' -f ($totalBytes / 1MB))
Write-Host '[INFO] Includes source, project files, tools, docs, SDK text, tests, and examples.'
Write-Host '[INFO] Excludes builds, old release packages, reports, diagnostics, logs, generated analytics, and secrets.'
if ($DryRun) {
    Write-Host '[PASS] Dry run complete: ZIP was not created.' -ForegroundColor Green
    return
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$zipPath = Join-Path $outDir ("Foundation-Source-{0}-{1}.zip" -f $timestamp, $PID)
$zip = $null
try {
    $zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
    $i = 0
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@('\', '/')).Replace('\', '/')
        $entryName = 'Foundation/' + $relative
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
        $i++
        if (($i % 250) -eq 0) { Write-Host "[INFO] Archived $i / $($files.Count) source files..." }
    }

    $meta = [ordered]@{
        package = 'Foundation clean source export'
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        includedFiles = $files.Count
        includedSourceBytes = $totalBytes
        omittedFiles = $skippedFiles
        prunedDirectories = $skippedDirs
        notes = 'Generated build outputs, analytics, logs, reports, diagnostics, binaries, backups and local credentials are intentionally excluded.'
    }
    $entry = $zip.CreateEntry('Foundation/SOURCE-EXPORT-MANIFEST.json')
    $writer = [System.IO.StreamWriter]::new($entry.Open(), [System.Text.UTF8Encoding]::new($false))
    try { $writer.Write(($meta | ConvertTo-Json -Depth 4)) }
    finally { $writer.Dispose() }
}
catch {
    if ($null -ne $zip) { $zip.Dispose(); $zip = $null }
    Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
    throw
}
finally { if ($null -ne $zip) { $zip.Dispose() } }

$check = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if ($check.Entries.Count -ne ($files.Count + 1)) {
        throw "ZIP entry count mismatch: got $($check.Entries.Count), expected $($files.Count + 1)"
    }
} finally { $check.Dispose() }

$digest = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
Write-Host "[PASS] Clean Foundation source archive: $zipPath" -ForegroundColor Green
Write-Host ('[PASS] ZIP size: {0:N2} MB' -f ((Get-Item -LiteralPath $zipPath).Length / 1MB))
Write-Host "[INFO] SHA256: $digest"
Write-Host '[INFO] Upload that ZIP to ChatGPT for the next Foundation implementation.'
