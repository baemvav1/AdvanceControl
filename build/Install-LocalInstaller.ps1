[CmdletBinding()]
param(
    [string]$AppInstallerPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = if ($PSScriptRoot) {
    $PSScriptRoot
} elseif ($PSCommandPath) {
    Split-Path -Parent $PSCommandPath
} else {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}

if ([string]::IsNullOrWhiteSpace($AppInstallerPath)) {
    $AppInstallerPath = Join-Path $scriptDirectory '..\artifacts\installer\AdvanceControl.appinstaller'
}

$resolvedAppInstallerPath = if ([string]::IsNullOrWhiteSpace($AppInstallerPath)) { $null } else { (Resolve-Path $AppInstallerPath).Path }
$msixBaseDirectory = if ($resolvedAppInstallerPath) { Split-Path $resolvedAppInstallerPath -Parent } else { Join-Path $scriptDirectory '..\artifacts\installer' }
$msixPath = Join-Path $msixBaseDirectory 'AdvanceControl-x64.msix'
$trustScriptPath = Join-Path $scriptDirectory 'Trust-LocalInstallerCertificate.ps1'

& $trustScriptPath -MsixPath $msixPath

$resolvedMsixPath = (Resolve-Path $msixPath).Path
Write-Host "Instalando paquete local '$resolvedMsixPath'..."
Add-AppxPackage -Path $resolvedMsixPath
Write-Host 'Instalacion local iniciada correctamente.'
