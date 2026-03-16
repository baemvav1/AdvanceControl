[CmdletBinding()]
param(
    [string]$MsixPath = '',
    [switch]$SkipElevation
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

if ([string]::IsNullOrWhiteSpace($MsixPath)) {
    $MsixPath = Join-Path $scriptDirectory 'AdvanceControl-x64.msix'
}

$isElevated = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).
    IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isElevated -and -not $SkipElevation) {
    $resolvedScriptPath = if ($PSCommandPath) { $PSCommandPath } else { $MyInvocation.MyCommand.Path }
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', "`"$resolvedScriptPath`"",
        '-MsixPath', "`"$MsixPath`"",
        '-SkipElevation'
    )

    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $arguments -Wait
    return
}

$logPath = Join-Path $scriptDirectory 'Trust-LocalInstallerCertificate.log'
Start-Transcript -Path $logPath -Force | Out-Null

function Import-CertificateIfMissing {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CertificatePath,
        [Parameter(Mandatory = $true)]
        [string]$StoreLocation,
        [Parameter(Mandatory = $true)]
        [string]$Thumbprint
    )

    if (Get-ChildItem -Path $StoreLocation | Where-Object Thumbprint -eq $Thumbprint) {
        Write-Host "El certificado ya existe en '$StoreLocation'."
        return
    }

    Import-Certificate -FilePath $CertificatePath -CertStoreLocation $StoreLocation | Out-Null
    Write-Host "Certificado importado en '$StoreLocation'."
}

try {
    $resolvedMsixPath = (Resolve-Path $MsixPath).Path
    $signature = Get-AuthenticodeSignature -FilePath $resolvedMsixPath
    $signerCertificate = $signature.SignerCertificate

    if (-not $signerCertificate) {
        throw "No se encontro ningun certificado firmante en '$resolvedMsixPath'."
    }

    $certificateExportPath = Join-Path $scriptDirectory 'AdvanceControl-signing.cer'

    Export-Certificate -Cert $signerCertificate -FilePath $certificateExportPath -Force | Out-Null
    Write-Host "Certificado exportado en '$certificateExportPath'."

    Import-CertificateIfMissing -CertificatePath $certificateExportPath -StoreLocation 'Cert:\CurrentUser\TrustedPeople' -Thumbprint $signerCertificate.Thumbprint
    Import-CertificateIfMissing -CertificatePath $certificateExportPath -StoreLocation 'Cert:\CurrentUser\Root' -Thumbprint $signerCertificate.Thumbprint

    if (-not $isElevated) {
        throw "Se importo el certificado para el usuario actual, pero la instalacion MSIX local requiere tambien importarlo en LocalMachine. Ejecuta este script como administrador."
    }

    Import-CertificateIfMissing -CertificatePath $certificateExportPath -StoreLocation 'Cert:\LocalMachine\TrustedPeople' -Thumbprint $signerCertificate.Thumbprint
    Import-CertificateIfMissing -CertificatePath $certificateExportPath -StoreLocation 'Cert:\LocalMachine\Root' -Thumbprint $signerCertificate.Thumbprint

    Write-Host 'El certificado del instalador quedo confiado para esta maquina.'
}
catch {
    Write-Error $_
    throw
}
finally {
    Stop-Transcript | Out-Null
}
