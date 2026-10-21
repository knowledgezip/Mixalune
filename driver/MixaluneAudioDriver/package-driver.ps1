param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [string]$Thumbprint,
    [string]$OutputPath = (Join-Path $PSScriptRoot "package\x64\$Configuration")
)

$ErrorActionPreference = 'Stop'
$sysvadRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\vendor\Microsoft-SysVAD')).Path
$msbuild = Get-ChildItem 'C:\Program Files\Microsoft Visual Studio' -Filter MSBuild.exe -Recurse -ErrorAction SilentlyContinue |
    Where-Object FullName -Match '\\MSBuild\\Current\\Bin\\MSBuild.exe$' | Select-Object -First 1
$kit = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory -ErrorAction SilentlyContinue |
    Where-Object Name -Match '^\d+\.\d+\.' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (-not $msbuild -or -not $kit) { throw 'Visual Studio C++ and the complete Windows Driver Kit are required.' }

$stampInf = Join-Path $kit.FullName 'x86\stampinf.exe'
$inf2Cat = Join-Path $kit.FullName 'x86\Inf2Cat.exe'
$signTool = Join-Path $kit.FullName 'x64\signtool.exe'
foreach ($tool in @($stampInf, $inf2Cat, $signTool)) {
    if (-not (Test-Path -LiteralPath $tool)) { throw "Required WDK tool not found: $tool" }
}

$commonProject = Join-Path $sysvadRoot 'EndpointsCommon\EndpointsCommon.vcxproj'
$driverProject = Join-Path $sysvadRoot 'TabletAudioSample\TabletAudioSample.vcxproj'
& $msbuild.FullName $commonProject /m /t:Build /p:Configuration=$Configuration /p:Platform=x64
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $msbuild.FullName $driverProject /m /t:Build /p:Configuration=$Configuration /p:Platform=x64 /p:SignMode=Off /p:SkipPackageVerification=true /p:ApiValidator_Enable=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$driverBuild = Join-Path $sysvadRoot "TabletAudioSample\x64\$Configuration"
$driver = Join-Path $driverBuild 'TabletAudioSample.sys'
$sourceInf = Join-Path $sysvadRoot 'TabletAudioSample\ComponentizedAudioSample.inx'
if (-not (Test-Path -LiteralPath $driver)) { throw "Driver output not found: $driver" }

New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
Get-ChildItem -LiteralPath $OutputPath -File -ErrorAction SilentlyContinue | Remove-Item -Force
Copy-Item -LiteralPath $driver -Destination (Join-Path $OutputPath 'TabletAudioSample.sys') -Force

# The keyword detector is unrelated to Mixalune. Produce a self-contained INF
# containing only the audio adapter binary rather than shipping sample DLLs.
$infText = [IO.File]::ReadAllText($sourceInf)
$infText = $infText.Replace("keywordDetectorContosoAdapter.dll=222`r`n", '')
$infText = $infText.Replace("keyworddetectorcontosoadapter.dll=SignatureAttributes.PETrust`r`n", '')
$infText = $infText.Replace("KEYWORDDETECTORCONTOSOADAPTER.CopyList=13 ;`r`n", '')
$infText = [regex]::Replace($infText, '(?ms)^\[KEYWORDDETECTORCONTOSOADAPTER\.CopyList\]\r?\nkeyworddetectorcontosoadapter\.dll\r?\n', '')
$infText = [regex]::Replace($infText, '(?ms)^\[KEYWORDDETECTORCONTOSOADAPTER\.AddReg\]\r?\n.*?(?=^;={10,})', '')
$infText = $infText.Replace('CopyFiles=SYSVAD_SA.CopyList,KEYWORDDETECTORCONTOSOADAPTER.CopyList', 'CopyFiles=SYSVAD_SA.CopyList')
$infText = $infText.Replace('AddReg=SYSVAD_SA.AddReg,KEYWORDDETECTORCONTOSOADAPTER.AddReg', 'AddReg=SYSVAD_SA.AddReg')
$inf = Join-Path $OutputPath 'MixaluneAudioDriver.inf'
[IO.File]::WriteAllText($inf, $infText, [Text.Encoding]::Unicode)
& $stampInf -f $inf -a amd64 -k 1.15 -d '*' -v '*'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$packagedDriver = Join-Path $OutputPath 'TabletAudioSample.sys'
if ($Thumbprint) {
    & $signTool sign /sha1 $Thumbprint /s My /fd SHA256 $packagedDriver
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
& $inf2Cat /driver:$OutputPath /os:10_X64
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$catalog = Join-Path $OutputPath 'sysvad.cat'
if ($Thumbprint) {
    & $signTool sign /sha1 $Thumbprint /s My /fd SHA256 $catalog
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $signTool verify /pa $packagedDriver
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & $signTool verify /pa $catalog
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Warning 'Package is unsigned. Supply -Thumbprint for development signing or submit the CAB through Microsoft Partner Center.'
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install-test-driver.ps1') -Destination $OutputPath -Force
Write-Host "Mixalune Audio Driver package created at $OutputPath"
