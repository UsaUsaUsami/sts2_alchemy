param([string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/env.ps1"
$root = Split-Path $PSScriptRoot
Push-Location $root
try {
    & "$env:DOTNET_ROOT/dotnet.exe" run --project tests/Alchemy.Core.Tests -c Release --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'ルール検証に失敗しました。' }
    & "$env:DOTNET_ROOT/dotnet.exe" build src/Alchemy/Alchemy.csproj -c Release --disable-build-servers -p:UseSharedCompilation=false "-p:GameRoot=$GameRoot"
    if ($LASTEXITCODE -ne 0) { throw 'ビルドに失敗しました。' }
    New-Item -ItemType Directory -Force dist/Alchemy | Out-Null
    Copy-Item src/Alchemy/bin/Release/net9.0/Alchemy.dll,src/Alchemy/Alchemy.json dist/Alchemy
    # Card art (CardArt.cs loads art/cards/*.png next to the DLL). Rebuilt each time so removed art goes too.
    if (Test-Path dist/Alchemy/art) { Remove-Item dist/Alchemy/art -Recurse -Force }
    New-Item -ItemType Directory -Force dist/Alchemy/art/cards | Out-Null
    if (Test-Path assets/art/cards) { Copy-Item assets/art/cards/*.png dist/Alchemy/art/cards -ErrorAction SilentlyContinue }
    Write-Output "カード絵: $(@(Get-ChildItem dist/Alchemy/art/cards -Filter *.png).Count)枚"
    Write-Output "配布ファイル: $root\dist\Alchemy"
} finally { Pop-Location }
