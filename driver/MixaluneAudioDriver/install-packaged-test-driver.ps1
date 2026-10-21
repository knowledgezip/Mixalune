param(
    [string]$PackagePath = (Join-Path $PSScriptRoot 'package\x64\Release')
)

$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$identity
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an Administrator PowerShell window.'
}

$package = (Resolve-Path -LiteralPath $PackagePath).Path
$inf = Join-Path $package 'MixaluneAudioDriver.inf'
$devcon = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\Tools' -Filter devcon.exe -Recurse -ErrorAction SilentlyContinue |
    Where-Object FullName -Match '\\x64\\devcon\.exe$' |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if (-not (Test-Path -LiteralPath $inf)) { throw "Driver INF not found: $inf" }
if (-not $devcon) { throw 'x64 DevCon was not found. Install the complete WDK.' }

$log = Join-Path $PSScriptRoot 'install-result.log'
"Mixalune Audio Driver installation: $(Get-Date -Format o)" | Set-Content -LiteralPath $log
(& bcdedit.exe /enum '{current}' 2>&1 | Out-String) | Add-Content -LiteralPath $log
(& $devcon.FullName install $inf 'Root\MixaluneAudioDriver' 2>&1 | Out-String) | Add-Content -LiteralPath $log
$code = $LASTEXITCODE
"DevConExitCode=$code" | Add-Content -LiteralPath $log
if ($code -ne 0) { exit $code }

(& pnputil.exe /scan-devices 2>&1 | Out-String) | Add-Content -LiteralPath $log
exit $LASTEXITCODE
