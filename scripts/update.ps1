param([string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'StS2を終了してから更新してください。' }
$release = Get-Content -LiteralPath (Join-Path $GameRoot 'release_info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($release.version -ne 'v0.111.0') { throw "対象外ゲーム版: $($release.version)" }
$base = Get-Content -LiteralPath (Join-Path $GameRoot 'mods/BaseLib/BaseLib.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($base.version -ne 'v3.4.5') { throw "対象外BaseLib版: $($base.version)" }
$destination = Join-Path $GameRoot 'mods/Alchemy'
$source = Join-Path $root 'dist/Alchemy'
$files = @('Alchemy.dll', 'Alchemy.json')
foreach ($file in $files) {
    if (!(Test-Path -LiteralPath (Join-Path $source $file))) { throw '先にscripts/build.ps1を実行してください。' }
}
$manifest = Get-Content -LiteralPath (Join-Path $source 'Alchemy.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.id -ne 'Alchemy') { throw 'MODの識別子が一致しません。' }
$backupRoot = Join-Path $root ('artifacts/backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))

# v0.24.0: the mod was renamed from Alchemist (design-axes 11). The old folder is backed up and removed, because
# both folders would load the same cards twice. The game enables a mod id it has not seen before by default.
$legacy = Join-Path $GameRoot 'mods/Alchemist'
if (Test-Path -LiteralPath $legacy) {
    $legacyManifest = Get-Content -LiteralPath (Join-Path $legacy 'Alchemist.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($legacyManifest.id -ne 'Alchemist') { throw "想定外のフォルダです: $legacy" }
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    Copy-Item -LiteralPath $legacy -Destination (Join-Path $backupRoot 'Alchemist') -Recurse
    Remove-Item -LiteralPath $legacy -Recurse -Force
    Write-Output "旧MOD（Alchemist）をバックアップして外しました: $backupRoot\Alchemist"
}

if (!(Test-Path -LiteralPath $destination)) {
    Copy-Item -LiteralPath $source -Destination $destination -Recurse
    foreach ($file in $files) {
        if ((Get-FileHash -LiteralPath (Join-Path $source $file)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $destination $file)).Hash) { throw "コピー検証失敗: $file" }
    }
    Write-Output "導入完了: Alchemy $($manifest.version) / $destination"
    return
}

$existing = Get-Content -LiteralPath (Join-Path $destination 'Alchemy.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($existing.id -ne 'Alchemy') { throw 'MODの識別子が一致しません。' }
$backup = Join-Path $backupRoot 'Alchemy'
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
Write-Output "更新完了: Alchemy $($manifest.version) / $destination"
Write-Output "旧版バックアップ: $backup"
