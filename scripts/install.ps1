param([string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'StS2を終了してから導入してください。' }
$release = Get-Content -LiteralPath (Join-Path $GameRoot 'release_info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($release.version -ne 'v0.111.0') { throw "対象外ゲーム版: $($release.version)。対象はv0.111.0です。" }
$base = Get-Content -LiteralPath (Join-Path $GameRoot 'mods/BaseLib/BaseLib.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($base.version -ne 'v3.4.5') { throw "対象外BaseLib版: $($base.version)。対象はv3.4.5です。" }
$destination = Join-Path $GameRoot 'mods/Alchemist'
if (Test-Path -LiteralPath $destination) { throw "既存の$destinationがあります。上書きを避けるため停止しました。" }
if (!(Test-Path "$root/dist/Alchemist/Alchemist.dll")) { throw '先にscripts/build.ps1を実行してください。' }
Copy-Item -LiteralPath "$root/dist/Alchemist" -Destination $destination -Recurse
Write-Output "導入完了: $destination"
