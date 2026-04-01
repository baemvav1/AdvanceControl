<#
.SYNOPSIS
    Genera Instalar-AdvanceControl.cmd: un único archivo que instala la app desde cero.
    Incluye el certificado embebido, eleva privilegios automáticamente y descarga el MSIX.

.PARAMETER CertificatePath
    Ruta al archivo .cer del certificado de firma.

.PARAMETER OutputPath
    Ruta donde se escribirá el archivo generado.

.PARAMETER AppInstallerUrl
    URL estable del .appinstaller (release client-latest).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CertificatePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$AppInstallerUrl = 'https://github.com/baemvav1/AdvanceControl/releases/download/client-latest/AdvanceControl.appinstaller'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$cerBytes  = [System.IO.File]::ReadAllBytes($CertificatePath)
$cerBase64 = [Convert]::ToBase64String($cerBytes)

# Archivo poliglota CMD+PowerShell:
#   - Al ejecutarse como .cmd (doble clic) el bloque <# : ... #> es ignorado por CMD
#     y se llama a PowerShell pasándole el mismo archivo como script.
#   - PowerShell lee el archivo normalmente: el bloque <# : ... #> es un comentario de bloque.
$content = @"
<# :
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~f0" %*
exit /b %ERRORLEVEL%
#>
#Requires -Version 5.1
<#
    Instalar-AdvanceControl.cmd
    Instala Advance Control con auto-actualización automática.
    Doble clic para ejecutar — no requiere archivos adicionales.
#>

Set-StrictMode -Version Latest
`$ErrorActionPreference = 'Stop'

# --- Certificado embebido ---
`$CerBase64 = '$cerBase64'

function Write-Step([string]`$msg) { Write-Host "`n>> `$msg" -ForegroundColor Cyan }
function Write-Ok([string]`$msg)   { Write-Host "   OK: `$msg" -ForegroundColor Green }
function Write-Warn([string]`$msg) { Write-Host "   AVISO: `$msg" -ForegroundColor Yellow }

# --- Auto-elevacion a administrador ---
`$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not `$isAdmin) {
    Write-Host 'Solicitando permisos de administrador...' -ForegroundColor Yellow
    Start-Process -FilePath 'powershell.exe' `
        -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File \`"`$PSCommandPath\`"" `
        -Verb RunAs -Wait
    exit
}

try {
    Write-Step 'Preparando certificado de firma...'
    `$certPath = Join-Path `$env:TEMP 'AdvanceControl-signing.cer'
    [System.IO.File]::WriteAllBytes(`$certPath, [Convert]::FromBase64String(`$CerBase64))

    `$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(`$certPath)
    `$thumb = `$cert.Thumbprint

    function Import-IfMissing([string]`$store) {
        `$existing = Get-ChildItem `$store -ErrorAction SilentlyContinue | Where-Object { `$_.Thumbprint -eq `$thumb }
        if (`$existing) { Write-Warn "Certificado ya existe en `$store" ; return }
        Import-Certificate -FilePath `$certPath -CertStoreLocation `$store | Out-Null
        Write-Ok "Certificado importado en `$store"
    }

    Import-IfMissing 'Cert:\LocalMachine\TrustedPeople'
    Import-IfMissing 'Cert:\LocalMachine\Root'

    Write-Step 'Descargando instalador desde GitHub...'
    `$appInstallerPath = Join-Path `$env:TEMP 'AdvanceControl.appinstaller'
    `$url = '$AppInstallerUrl'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri `$url -OutFile `$appInstallerPath -UseBasicParsing
    Write-Ok "Descargado en `$appInstallerPath"

    Write-Step 'Instalando Advance Control...'
    Add-AppxPackage -Path `$appInstallerPath -AppInstallerFile
    Write-Ok 'Instalacion completada. La app se actualizara automaticamente en cada inicio.'
}
catch {
    Write-Host "`nERROR: `$_" -ForegroundColor Red
    Write-Host 'Presiona Enter para cerrar...'
    Read-Host | Out-Null
    exit 1
}

Write-Host "`nPresiona Enter para cerrar..." -ForegroundColor Gray
Read-Host | Out-Null
"@

[System.IO.File]::WriteAllText($OutputPath, $content, [System.Text.Encoding]::UTF8)
Write-Host "Instalador generado: $OutputPath"
