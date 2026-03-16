[CmdletBinding()]
param(
    [string]$AppInstallerUrl = 'https://github.com/baemvav1/AdvanceControl/releases/latest/download/AdvanceControl.appinstaller'
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

$logPath = Join-Path $scriptDirectory 'Install-AutoUpdateFromGitHub.log'
Start-Transcript -Path $logPath -Force | Out-Null

try {
    $trustScriptPath = Join-Path $scriptDirectory 'Trust-LocalInstallerCertificate.ps1'
    $msixPath = Join-Path $scriptDirectory 'AdvanceControl-x64.msix'
    & $trustScriptPath -MsixPath $msixPath

    $tempAppInstallerPath = Join-Path $env:TEMP 'AdvanceControl-GitHub.appinstaller'
    Write-Host "Descargando AppInstaller remoto desde '$AppInstallerUrl'..."
    Invoke-WebRequest -Uri $AppInstallerUrl -OutFile $tempAppInstallerPath

    Write-Host "Instalando desde '$tempAppInstallerPath'..."
    Add-AppxPackage -Path $tempAppInstallerPath -AppInstallerFile

    Write-Host 'Instalacion autoactualizable iniciada correctamente.'
}
catch {
    Write-Error $_
    throw
}
finally {
    Stop-Transcript | Out-Null
}
