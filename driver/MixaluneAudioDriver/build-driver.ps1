param(
    [string]$SysvadRoot = (Join-Path $PSScriptRoot '..\vendor\Microsoft-SysVAD'),
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$msbuild = Get-ChildItem 'C:\Program Files\Microsoft Visual Studio' -Filter MSBuild.exe -Recurse -ErrorAction SilentlyContinue |
    Where-Object FullName -Match '\\MSBuild\\Current\\Bin\\MSBuild.exe$' | Select-Object -First 1
$kits = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory -ErrorAction SilentlyContinue |
    Where-Object Name -Match '^\d+\.\d+\.' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$kitIncludes = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\Include' -Directory -ErrorAction SilentlyContinue |
    Where-Object Name -Match '^\d+\.\d+\.' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$portCls = if ($kitIncludes) { Join-Path $kitIncludes.FullName 'km\portcls.h' } else { $null }
if (-not $msbuild -or -not $kits -or -not $portCls -or -not (Test-Path $portCls)) {
    throw 'The complete Windows Driver Kit is not installed. Install the WDK driver headers/libraries and the Visual Studio C++ driver workload first.'
}
$root = (Resolve-Path $SysvadRoot).Path
$commonProject = Join-Path $root 'EndpointsCommon\EndpointsCommon.vcxproj'
$driverProject = Join-Path $root 'TabletAudioSample\TabletAudioSample.vcxproj'
Write-Host 'Building the Mixalune Audio Driver for x64.'
& $msbuild.FullName $commonProject /m /t:Build /p:Configuration=$Configuration /p:Platform=x64
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $msbuild.FullName $driverProject /m /t:Build /p:Configuration=$Configuration /p:Platform=x64 /p:SignMode=Off /p:SkipPackageVerification=true /p:ApiValidator_Enable=false
exit $LASTEXITCODE
