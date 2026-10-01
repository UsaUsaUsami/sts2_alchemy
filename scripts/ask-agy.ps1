# Asks Gemini through the Antigravity CLI (agy) and prints only the answer, so Claude reads a short reply instead of the
# image or log itself. Images and files are attached with -Attach (agy's @path). Tools are not allowed (print mode
# denies them), so agy only reads what is attached.
# Usage: .\scripts\ask-agy.ps1 -Prompt "..." [-Attach artifacts/rig-probe/x/sheet.png,...] [-Effort low|medium|high]
param([Parameter(Mandatory)][string]$Prompt, [string[]]$Attach = @(), [string]$Effort = '', [int]$TimeoutSec = 300)
$ErrorActionPreference = 'Stop'
$agy = Join-Path $env:LOCALAPPDATA 'agy/bin/agy.exe'
if (-not (Test-Path $agy)) { throw 'agy が見つかりません（irm https://antigravity.google/cli/install.ps1 | iex で導入）。' }
$root = Split-Path $PSScriptRoot
Push-Location $root
try {
    foreach ($file in $Attach) { if (-not (Test-Path $file)) { throw "添付ファイルがありません: $file" } }
    $text = (@($Attach | ForEach-Object { [string][char]64 + ($_ -replace '\\', '/') }) + $Prompt) -join ' '
    $flags = @('--output-format', 'json', '--print-timeout', "${TimeoutSec}s")
    if ($Effort) { $flags += @('--effort', $Effort) }
    $out = & $agy @flags -p $text 2>&1 | Out-String
    $json = ($out -split "`n" | Where-Object { $_.TrimStart().StartsWith('{') } | Select-Object -Last 1)
    if (-not $json) { throw "agy の応答を読めませんでした:`n$out" }
    $result = $json | ConvertFrom-Json
    if ($result.denied_actions) { Write-Warning ("agy がツールを使おうとして拒否されました: " + (($result.denied_actions | ForEach-Object { $_.display_name }) -join ', ')) }
    if ($result.status -ne 'SUCCESS') { throw "agy: $($result.status)`n$out" }
    $result.response.Trim()
} finally { Pop-Location }
