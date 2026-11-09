# Inspects the latest in-game UI screenshot audit and creates a shareable ZIP.
[CmdletBinding()]
param([string]$GameRoot = '', [switch]$SkipZip)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$foundationRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($GameRoot)) { $GameRoot = [IO.Path]::GetFullPath((Join-Path $foundationRoot '..\..')) }
$folder = Join-Path $GameRoot '_Mods\Foundation\Verification\UISmoke'
if (-not (Test-Path -LiteralPath $folder -PathType Container)) {
    Write-Host "[PENDING] No UI screenshot runs yet: $folder" -ForegroundColor Yellow
    Write-Host 'Install the UI smoke plugin, load a kingdom, and click UI Smoke + Screenshots.'
    exit 0
}
$runs = @(Get-ChildItem -LiteralPath $folder -Directory | Sort-Object Name -Descending)
if ($runs.Count -eq 0) { Write-Host '[PENDING] No screenshot run directories found.' -ForegroundColor Yellow; exit 0 }
$run = $runs[0]
$json = Join-Path $run.FullName 'ui-smoke-results.json'
if (-not (Test-Path -LiteralPath $json -PathType Leaf)) { throw "Latest run lacks result manifest: $json" }
$report = Get-Content -LiteralPath $json -Raw -Encoding UTF8 | ConvertFrom-Json
$cases = @($report.Cases)
$passes = @($cases | Where-Object Status -EQ 'PASS').Count
$fails = @($cases | Where-Object Status -EQ 'FAIL').Count
$skips = @($cases | Where-Object Status -EQ 'SKIP').Count
Write-Host "[INFO] Run: $($run.FullName)"
Write-Host "[INFO] Foundation: $($report.FoundationVersion), resolution: $($report.GameResolution)"
Write-Host "[INFO] Start UTC: $($report.StartedUtc), finish UTC: $($report.FinishedUtc)"
Write-Host "[RESULT] pass=$passes fail=$fails skip=$skips total=$($cases.Count)" -ForegroundColor $(if ($fails -gt 0) { 'Yellow' } else { 'Green' })
$missing = New-Object System.Collections.Generic.List[string]
foreach ($case in $cases) {
    $files = @($case.Screenshots)
    foreach ($file in $files) {
        $path = Join-Path $run.FullName $file
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $missing.Add($file); continue }
        $bytes = [IO.File]::ReadAllBytes($path)
        if ($bytes.Length -lt 24 -or $bytes[0] -ne 137 -or $bytes[1] -ne 80 -or $bytes[2] -ne 78 -or $bytes[3] -ne 71) {
            $missing.Add("$file (invalid PNG header)")
        }
    }
    if ($case.Status -eq 'FAIL' -or $case.Status -eq 'SKIP') {
        Write-Host ("[{0}] {1}: {2}" -f $case.Status, $case.Name, $case.Error + ' ' + $case.Note)
    }
}
if ($missing.Count -gt 0) { Write-Warning "Missing/invalid PNGs: $($missing.Count)"; $missing | ForEach-Object { Write-Host "  $_" } }
else { Write-Host '[PASS] All reported screenshot files exist with PNG signatures.' -ForegroundColor Green }
$images = @(Get-ChildItem -LiteralPath $run.FullName -Filter '*.png' -File)
Write-Host "[INFO] Images: $($images.Count)"
# Build a self-contained local HTML contact sheet for reviewing every element.
# CSS/HTML are local; no internet or remote scripts are loaded.
$parts = New-Object System.Collections.Generic.List[string]
$parts.Add('<!DOCTYPE html><html lang="en"><head><meta charset="utf-8"><title>Foundation UI Screenshot Audit</title>')
$parts.Add('<style>body{background:#202024;color:#ededed;font:16px Arial,sans-serif;margin:24px}h1{color:#e2c38c}h2{font-size:19px}section{background:#303036;border:1px solid #555;border-radius:9px;padding:16px;margin:18px 0}a{color:#e6c78f}img{max-width:min(100%,900px);max-height:520px;object-fit:contain;display:block;margin:12px 0;border:1px solid #696969}.PASS{color:#91df9a}.FAIL{color:#f99}.SKIP{color:#ddd}.info{color:#bdbdbd;font-size:14px}</style></head><body>')
$parts.Add('<h1>Foundation UI Screenshot Audit</h1>')
$parts.Add('<p>Foundation ' + [Net.WebUtility]::HtmlEncode([string]$report.FoundationVersion) + ' &bull; ' + [Net.WebUtility]::HtmlEncode([string]$report.GameResolution) + ' &bull; ' + $passes + ' PASS / ' + $fails + ' FAIL / ' + $skips + ' SKIP</p>')
foreach ($case in $cases) {
    $label = [Net.WebUtility]::HtmlEncode([string]$case.Name)
    $kind = [Net.WebUtility]::HtmlEncode([string]$case.Kind)
    $status = [string]$case.Status
    $parts.Add('<section><h2>' + $label + ' <span class="' + $status + '">[' + $status + ']</span></h2><p class="info">' + $kind + ' &mdash; ' + [Net.WebUtility]::HtmlEncode([string]$case.Note) + ' ' + [Net.WebUtility]::HtmlEncode([string]$case.Error) + '</p>')
    foreach ($file in @($case.Screenshots)) {
        $safeFile = [Net.WebUtility]::HtmlEncode([string]$file)
        if ((Test-Path -LiteralPath (Join-Path $run.FullName $file) -PathType Leaf)) {
            if ($file -match '(_panel|_element)\.png$') { $parts.Add('<a href="' + $safeFile + '"><img loading="lazy" alt="' + $label + '" src="' + $safeFile + '"></a>') }
            else { $parts.Add('<p><a href="' + $safeFile + '">Full-screen screenshot</a></p>') }
        }
    }
    $parts.Add('</section>')
}
$parts.Add('</body></html>')
$index = Join-Path $run.FullName 'index.html'
[IO.File]::WriteAllText($index, [string]::Join([Environment]::NewLine, $parts.ToArray()), [Text.Encoding]::UTF8)
Write-Host "[PASS] Local screenshot gallery: $index" -ForegroundColor Green
if (-not $SkipZip) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $dest = Join-Path $folder ("UISmoke-" + $run.Name + ".zip")
    if (Test-Path -LiteralPath $dest) { Remove-Item -LiteralPath $dest -Force }
    [IO.Compression.ZipFile]::CreateFromDirectory($run.FullName, $dest, [IO.Compression.CompressionLevel]::Optimal, $true)
    Write-Host "[PASS] Shareable ZIP: $dest" -ForegroundColor Green
    Write-Host "[INFO] ZIP size: $([math]::Round((Get-Item -LiteralPath $dest).Length / 1MB,2)) MB"
}
if ($fails -gt 0 -or $missing.Count -gt 0 -or [string]::IsNullOrWhiteSpace($report.FinishedUtc)) {
    Write-Host '[WARN] Audit is incomplete or has failed checks. Attach the ZIP and BepInEx log for debugging.' -ForegroundColor Yellow
}
