# Development probe (tests/Alchemy.RigProbe): runs the isolated game WITH rendering, saves rig frames as PNG.
# The smoke mod is set aside while it runs, so only the probe acts. Output: artifacts/rig-probe/<Name>/
param([string]$Name = 'probe', [string]$Mode = '', [string]$Donor = '', [string]$Rig = '', [string]$Riders = '', [string]$Skel = '', [string]$Anims = '', [int]$Zoom = 1, [int]$Frames = 6000)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/env.ps1"
$root = Split-Path $PSScriptRoot
$runtime = Join-Path $root '.research/runtime'
Push-Location $root
try {
    & "$env:DOTNET_ROOT/dotnet.exe" build tests/Alchemy.RigProbe/Alchemy.RigProbe.csproj -c Release --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'ビルドに失敗しました。' }
    Copy-Item src/Alchemy/bin/Release/net9.0/Alchemy.dll, src/Alchemy/Alchemy.json "$runtime/mods/Alchemy" -Force
    $rigDir = "$runtime/mods/Alchemy/art/character/rig"
    if (Test-Path $rigDir) { Remove-Item $rigDir -Recurse -Force }
    if (Test-Path assets/art/character/rig) { Copy-Item assets/art/character/rig $rigDir -Recurse -Force }
    New-Item -ItemType Directory -Force "$runtime/mods/AlchemyRigProbe" | Out-Null
    Copy-Item tests/Alchemy.RigProbe/bin/Release/net9.0/AlchemyRigProbe.dll, tests/Alchemy.RigProbe/AlchemyRigProbe.json "$runtime/mods/AlchemyRigProbe" -Force
    $out = Join-Path $root "artifacts/rig-probe/$Name"
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }
    New-Item -ItemType Directory -Force $out | Out-Null
    $smoke = "$runtime/mods/AlchemySmoke"; $parked = "$runtime/AlchemySmoke.parked"
    if (Test-Path $smoke) { Move-Item $smoke $parked }
    $appdata = $env:APPDATA
    $env:APPDATA = Join-Path $root 'artifacts/smoke/appdata'
    $env:ALCHEMY_PROBE_OUT = $out
    $env:ALCHEMY_PROBE_MODE = $Mode
    $env:ALCHEMY_PROBE_DONOR = $Donor
    $env:ALCHEMY_PROBE_RIG = $Rig
    $env:ALCHEMY_PROBE_RIDERS = $Riders
    # -Mode custom: a script-written rig (-Skel path to its .json/.skel, .atlas beside it), -Anims to capture.
    $env:ALCHEMY_PROBE_SKEL = if ($Skel) { (Resolve-Path $Skel).Path } else { '' }
    $env:ALCHEMY_PROBE_ANIMS = $Anims
    $env:ALCHEMY_PROBE_ZOOM = "$Zoom"
    try {
        Start-Process -FilePath "$runtime/SlayTheSpire2.exe" -WorkingDirectory $runtime -NoNewWindow -Wait `
            -ArgumentList '--quit-after', $Frames, '--force-steam=off', '--windowed', '--resolution', '1280x720' `
            -RedirectStandardOutput "$out/log.txt" -RedirectStandardError "$out/err.txt"
    } finally {
        $env:APPDATA = $appdata
        Remove-Item "$runtime/mods/AlchemyRigProbe" -Recurse -Force
        if (Test-Path $parked) { Move-Item $parked $smoke }
    }
    Get-Content "$out/log.txt", "$out/err.txt" | Select-String 'ALCHEMY_PROBE' | ForEach-Object { $_.Line }
} finally { Pop-Location }
