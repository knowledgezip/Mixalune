param([Parameter(Mandatory=$true)][string]$InfPath)
$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$identity
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run this script from an Administrator PowerShell window.' }
if (-not (Test-Path $InfPath)) { throw "INF not found: $InfPath" }
Write-Warning 'This installs a kernel driver. Use only a Mixalune package built from reviewed source and signed with your test or production certificate.'
pnputil.exe /add-driver (Resolve-Path $InfPath) /install
exit $LASTEXITCODE
