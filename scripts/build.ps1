param([string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/env.ps1"
$root = Split-Path $PSScriptRoot
Push-Location $root
try {
    & "$env:DOTNET_ROOT/dotnet.exe" run --project tests/Alchemist.Core.Tests -c Release --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'ルール検証に失敗しました。' }
    & "$env:DOTNET_ROOT/dotnet.exe" build src/Alchemist/Alchemist.csproj -c Release --disable-build-servers -p:UseSharedCompilation=false "-p:GameRoot=$GameRoot"
    if ($LASTEXITCODE -ne 0) { throw 'ビルドに失敗しました。' }
    New-Item -ItemType Directory -Force dist/Alchemist | Out-Null
    Copy-Item src/Alchemist/bin/Release/net9.0/Alchemist.dll,src/Alchemist/Alchemist.json dist/Alchemist
    Write-Output "配布ファイル: $root\dist\Alchemist"
} finally { Pop-Location }
