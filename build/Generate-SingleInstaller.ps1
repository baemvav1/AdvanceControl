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

# Cabecera CMD: al ejecutarse con doble clic, CMD llama a PowerShell pasándole el mismo archivo.
# PowerShell ignora el bloque <# : ... #> como comentario de bloque.
# Se escribe con @'...'@ (literal, sin expansión) para evitar problemas de escaping.
$cmdHeader = @'
<# :
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~f0" %*
exit /b %ERRORLEVEL%
#>
'@

# Cuerpo PowerShell con marcadores {{CERT_BASE64}} y {{APP_URL}} para sustituir después.
# Usar @'...'@ evita TODOS los problemas de escaping de backticks, comillas y variables.
$psBody = @'

#Requires -Version 5.1
<#
    Instalar-AdvanceControl.cmd
    Instala Advance Control con auto-actualizacion automatica.
    Doble clic para ejecutar - no requiere archivos adicionales.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$CerBase64 = '{{CERT_BASE64}}'

function Write-Step([string]$msg) { Write-Host "`n>> $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host "   OK: $msg" -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "   AVISO: $msg" -ForegroundColor Yellow }

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host 'Solicitando permisos de administrador...' -ForegroundColor Yellow
    Start-Process -FilePath 'powershell.exe' `
        -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" `
        -Verb RunAs -Wait
    exit
}

try {
    Write-Step 'Preparando certificado de firma...'
    $certPath = Join-Path $env:TEMP 'AdvanceControl-signing.cer'
    [System.IO.File]::WriteAllBytes($certPath, [Convert]::FromBase64String($CerBase64))

    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)
    $thumb = $cert.Thumbprint

    function Import-IfMissing([string]$store) {
        $existing = Get-ChildItem $store -ErrorAction SilentlyContinue | Where-Object { $_.Thumbprint -eq $thumb }
        if ($existing) { Write-Warn "Certificado ya existe en $store" ; return }
        Import-Certificate -FilePath $certPath -CertStoreLocation $store | Out-Null
        Write-Ok "Certificado importado en $store"
    }

    Import-IfMissing 'Cert:\LocalMachine\TrustedPeople'
    Import-IfMissing 'Cert:\LocalMachine\Root'

    Write-Step 'Descargando instalador desde GitHub...'
    $appInstallerPath = Join-Path $env:TEMP 'AdvanceControl.appinstaller'
    $url = '{{APP_URL}}'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $url -OutFile $appInstallerPath -UseBasicParsing
    Write-Ok "Descargado en $appInstallerPath"

    Write-Step 'Instalando Advance Control...'
    # Add-AppxPackage -AppInstallerFile puede fallar en W10 si App Installer no esta actualizado.
    # Fallback: abrir el .appinstaller con el handler del sistema (UI de App Installer).
    try {
        Add-AppxPackage -Path $appInstallerPath -AppInstallerFile
        Write-Ok 'Instalacion completada. La app se actualizara automaticamente en cada inicio.'
    }
    catch {
        Write-Warn "Metodo silencioso fallo ($($_.Exception.Message)). Abriendo instalador visual..."
        Start-Process -FilePath $appInstallerPath
        Write-Ok 'Se abrio el instalador. Completa la instalacion en la ventana que aparecio.'
    }
}
catch {
    Write-Host "`nERROR: $_" -ForegroundColor Red
    Write-Host 'Presiona Enter para cerrar...'
    Read-Host | Out-Null
    exit 1
}

Write-Host "`nInstalacion exitosa. Presiona Enter para cerrar..." -ForegroundColor Green
Read-Host | Out-Null
'@

$psBody = $psBody.Replace('{{CERT_BASE64}}', $cerBase64).Replace('{{APP_URL}}', $AppInstallerUrl)

$content = $cmdHeader + $psBody

# UTF-8 SIN BOM: el BOM rompe el parsing de CMD al inicio del archivo.
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($OutputPath, $content, $utf8NoBom)
Write-Host "Instalador generado: $OutputPath"
