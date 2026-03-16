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
    $resolvedAppInstallerPath = (Resolve-Path $AppInstallerPath).Path
    $msixPath = Join-Path $scriptDirectory 'AdvanceControl-x64.msix'
    $trustScriptPath = Join-Path $scriptDirectory 'Trust-LocalInstallerCertificate.ps1'

    & $trustScriptPath -MsixPath $msixPath

    $resolvedMsixPath = (Resolve-Path $msixPath).Path
    $appInstallerUri = ([System.Uri]::new($resolvedAppInstallerPath)).AbsoluteUri
    $msixUri = ([System.Uri]::new($resolvedMsixPath)).AbsoluteUri

    [xml]$appInstallerDocument = Get-Content -Path $resolvedAppInstallerPath -Raw
    $appInstallerNode = $appInstallerDocument.SelectSingleNode("/*[local-name()='AppInstaller']")
    $mainPackageNode = $appInstallerDocument.SelectSingleNode("/*[local-name()='AppInstaller']/*[local-name()='MainPackage']")

    if (-not $appInstallerNode -or -not $mainPackageNode) {
        throw "No se pudo interpretar correctamente el archivo '$resolvedAppInstallerPath'."
    }

    $appInstallerNode.SetAttribute('Uri', $appInstallerUri)
    $mainPackageNode.SetAttribute('Uri', $msixUri)
    $appInstallerDocument.Save($resolvedAppInstallerPath)

    Write-Host "Uri AppInstaller actualizada a '$appInstallerUri'."
    Write-Host "Uri MSIX actualizada a '$msixUri'."

    Write-Host "Abriendo instalador '$resolvedAppInstallerPath'..."
    Add-AppxPackage -Path $resolvedAppInstallerPath -AppInstallerFile

    Write-Host 'Instalacion iniciada correctamente.'
}
catch {
    Write-Error $_
    throw
}
finally {
    Stop-Transcript | Out-Null
}
