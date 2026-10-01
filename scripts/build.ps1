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
    # The golem pet sprite (PetArt in CardArt.cs).
    if (Test-Path assets/art/character) { New-Item -ItemType Directory -Force dist/Alchemy/art/character | Out-Null; Copy-Item assets/art/character/* -Include *.png,*.ctex dist/Alchemy/art/character }
    # The combat rig's own drawings (NecroRig.cs).
    if (Test-Path assets/art/character/rig) { Copy-Item assets/art/character/rig dist/Alchemy/art/character/rig -Recurse -Force }
    # The alchemist's own rig (AlchemistRig.cs) ships only its atlas, page and skeleton; the parts it is built from stay.
    Get-ChildItem dist/Alchemy/art/character/rig/alchemist -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notin @('alchemist.atlas', 'alchemist.png', 'alchemist.spine-json') } |
        Remove-Item -Recurse -Force
    # Relic, power, enchantment and map icons (IconArt in CardArt.cs).
    if (Test-Path assets/art/icons) { New-Item -ItemType Directory -Force dist/Alchemy/art/icons | Out-Null; Copy-Item assets/art/icons/*.ctex dist/Alchemy/art/icons }
    if (Test-Path assets/art/pets) { New-Item -ItemType Directory -Force dist/Alchemy/art/pets | Out-Null; Copy-Item assets/art/pets/*.png dist/Alchemy/art/pets }
    Write-Output "配布ファイル: $root\dist\Alchemy"
} finally { Pop-Location }
