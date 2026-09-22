param([string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'StS2を終了してから更新してください。' }
$release = Get-Content -LiteralPath (Join-Path $GameRoot 'release_info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($release.version -ne 'v0.111.0') { throw "対象外ゲーム版: $($release.version)" }
$base = Get-Content -LiteralPath (Join-Path $GameRoot 'mods/BaseLib/BaseLib.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($base.version -ne 'v3.4.5') { throw "対象外BaseLib版: $($base.version)" }
$destination = Join-Path $GameRoot 'mods/Alchemist'
$source = Join-Path $root 'dist/Alchemist'
$files = @('Alchemist.dll', 'Alchemist.json')
foreach ($file in $files) {
    if (!(Test-Path -LiteralPath (Join-Path $source $file))) { throw '先にscripts/build.ps1を実行してください。' }
    if (!(Test-Path -LiteralPath (Join-Path $destination $file))) { throw '既存MODがありません。初回はscripts/install.ps1を使ってください。' }
}
$manifest = Get-Content -LiteralPath (Join-Path $source 'Alchemist.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$existing = Get-Content -LiteralPath (Join-Path $destination 'Alchemist.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.id -ne 'Alchemist' -or $existing.id -ne 'Alchemist') { throw 'MODの識別子が一致しません。' }
$backup = Join-Path $root ('artifacts/backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '/Alchemist')
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $destination $file) -Destination $backup }
try {
    foreach ($file in $files) {
        Copy-Item -LiteralPath (Join-Path $source $file) -Destination (Join-Path $destination $file) -Force
        if ((Get-FileHash -LiteralPath (Join-Path $source $file)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $destination $file)).Hash) { throw "コピー検証失敗: $file" }
    }
} catch {
    foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $backup $file) -Destination (Join-Path $destination $file) -Force }
    throw
}
Write-Output "更新完了: Alchemist $($manifest.version) / $destination"
Write-Output "旧版バックアップ: $backup"
