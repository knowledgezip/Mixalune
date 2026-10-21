param(
    [string]$DriverPackage = (Join-Path $PSScriptRoot 'package\x64\Release'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'attestation')
)

$ErrorActionPreference = 'Stop'
$package = (Resolve-Path -LiteralPath $DriverPackage).Path
$pdb = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\vendor\Microsoft-SysVAD\TabletAudioSample\x64\Release\TabletAudioSample.pdb')).Path
$stage = Join-Path $OutputDirectory 'MixaluneAudioDriver'
$cab = Join-Path $OutputDirectory 'MixaluneAudioDriverAttestation.cab'

New-Item -ItemType Directory -Path $stage -Force | Out-Null
Get-ChildItem -LiteralPath $stage -File -ErrorAction SilentlyContinue | Remove-Item -Force
foreach ($name in @('MixaluneAudioDriver.inf', 'TabletAudioSample.sys', 'sysvad.cat')) {
    $source = Join-Path $package $name
    if (-not (Test-Path -LiteralPath $source)) { throw "Required package file not found: $source" }
    Copy-Item -LiteralPath $source -Destination $stage -Force
}
Copy-Item -LiteralPath $pdb -Destination (Join-Path $stage 'TabletAudioSample.pdb') -Force

$manifest = Get-ChildItem -LiteralPath $stage -File | Sort-Object Name | ForEach-Object {
    [pscustomobject]@{
        file = $_.Name
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'submission-manifest.json') -Encoding utf8

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$ddf = Join-Path $OutputDirectory 'MixaluneAudioDriverAttestation.ddf'
$lines = @(
    '.OPTION EXPLICIT',
    ".Set CabinetNameTemplate=$([IO.Path]::GetFileName($cab))",
    ".Set DiskDirectoryTemplate=$OutputDirectory",
    '.Set CompressionType=MSZIP',
    '.Set Cabinet=on',
    '.Set Compress=on'
)
$lines += Get-ChildItem -LiteralPath $stage -File | Sort-Object Name | ForEach-Object {
    '"{0}" "MixaluneAudioDriver\{1}"' -f $_.FullName, $_.Name
}
$lines | Set-Content -LiteralPath $ddf -Encoding ascii
if (Test-Path -LiteralPath $cab) { Remove-Item -LiteralPath $cab -Force }
& makecab.exe /f $ddf
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (-not (Test-Path -LiteralPath $cab)) { throw 'MakeCab completed without producing the submission CAB.' }

$cabHash = Get-FileHash -LiteralPath $cab -Algorithm SHA256
"CAB=$cab`nSHA256=$($cabHash.Hash)" | Set-Content -LiteralPath (Join-Path $OutputDirectory 'cab-sha256.txt') -Encoding ascii
Write-Host "Attestation CAB created: $cab"
Write-Host 'The CAB is not submission-ready until it is signed with the EV certificate registered to the Partner Center account.'
