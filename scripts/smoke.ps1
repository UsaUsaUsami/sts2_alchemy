param([string]$Name = 'smoke', [int]$Frames = 15000)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/env.ps1"
$root = Split-Path $PSScriptRoot
$runtime = Join-Path $root '.research/runtime'
if (!(Test-Path (Join-Path $runtime 'SlayTheSpire2.exe'))) { throw "隔離実行環境がありません: $runtime" }
Push-Location $root
try {
    foreach ($project in 'src/Alchemy/Alchemy.csproj', 'tests/Alchemy.Smoke/Alchemy.Smoke.csproj') {
        & "$env:DOTNET_ROOT/dotnet.exe" build $project -c Release --disable-build-servers -p:UseSharedCompilation=false
        if ($LASTEXITCODE -ne 0) { throw "ビルドに失敗しました: $project" }
    }
    Copy-Item src/Alchemy/bin/Release/net9.0/Alchemy.dll, src/Alchemy/Alchemy.json "$runtime/mods/Alchemy" -Force
    Copy-Item tests/Alchemy.Smoke/bin/Release/net9.0/AlchemySmoke.dll, tests/Alchemy.Smoke/AlchemySmoke.json "$runtime/mods/AlchemySmoke" -Force
    # Card art (CardArt.cs): the shipped PNGs, then a fixture the smoke test checks for.
    $art = "$runtime/mods/Alchemy/art/cards"
    if (Test-Path $art) { Remove-Item $art -Recurse -Force }
    New-Item -ItemType Directory -Force $art | Out-Null
    if (Test-Path assets/art/cards) { Copy-Item assets/art/cards/*.png $art -Force -ErrorAction SilentlyContinue }
    Copy-Item tests/Alchemy.Smoke/fixtures/art/cards/*.png $art -Force

    New-Item -ItemType Directory -Force artifacts/smoke | Out-Null
    $log = Join-Path $root "artifacts/smoke/$Name.log"
    $errLog = "$log.err"
    $appdata = $env:APPDATA
    $env:APPDATA = Join-Path $root 'artifacts/smoke/appdata'
    # Start-Process keeps the game's stderr out of the PowerShell error stream, which would otherwise
    # abort the run under $ErrorActionPreference = 'Stop' even on a clean exit.
    try {
        Start-Process -FilePath "$runtime/SlayTheSpire2.exe" -WorkingDirectory $runtime -NoNewWindow -Wait `
            -ArgumentList '--headless', '--quit-after', $Frames, '--force-steam=off' `
            -RedirectStandardOutput $log -RedirectStandardError $errLog
    } finally { $env:APPDATA = $appdata }

    $lines = @(Get-Content -LiteralPath $log) + @(Get-Content -LiteralPath $errLog)
    $passed = @($lines | Select-String -SimpleMatch 'ALCHEMIST_SMOKE_PASS').Count
    $failed = @($lines | Select-String -SimpleMatch 'ALCHEMIST_SMOKE_FAIL')
    $done = @('ALCHEMIST_SMOKE_COMPLETE', 'ALCHEMIST_LOOP_COMPLETE') | Where-Object { $lines -contains $_ }
    Write-Output "ログ: $log"
    Write-Output "PASS: $passed / 完了マーカー: $($done -join ', ')"
    if ($failed) { $failed | ForEach-Object { Write-Output $_.Line }; throw 'スモークテストに失敗があります。' }
    if ($done.Count -ne 2) { throw "完了マーカーが揃いません。--Frames を増やして再実行してください（現在 $Frames）。" }
} finally { Pop-Location }
