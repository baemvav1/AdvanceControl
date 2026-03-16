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
    $AppInstallerPath = Join-Path $scriptDirectory 'AdvanceControl.appinstaller'
}

$logPath = Join-Path $scriptDirectory 'Install-LocalInstaller.log'
Start-Transcript -Path $logPath -Force | Out-Null

try {
    $msixPath = Join-Path $scriptDirectory 'AdvanceControl-x64.msix'
    $trustScriptPath = Join-Path $scriptDirectory 'Trust-LocalInstallerCertificate.ps1'

    & $trustScriptPath -MsixPath $msixPath

    $resolvedMsixPath = (Resolve-Path $msixPath).Path
    Write-Host "Instalando paquete local '$resolvedMsixPath'..."
    Add-AppxPackage -Path $resolvedMsixPath
    Write-Host 'Instalacion local iniciada correctamente.'
}
catch {
    Write-Error $_
    throw
}
finally {
    Stop-Transcript | Out-Null
}
