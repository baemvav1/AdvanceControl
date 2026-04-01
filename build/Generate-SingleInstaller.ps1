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

# Log en la misma carpeta del script para diagnostico
$logPath = Join-Path (Split-Path $PSCommandPath -Parent) 'instalar.log'
function Log([string]$msg) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Add-Content -Path $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

Log "=== Inicio instalacion Advance Control ==="
Log "Usuario   : $env:USERNAME"
Log "Equipo    : $env:COMPUTERNAME"
Log "PSVersion : $($PSVersionTable.PSVersion)"
Log "Script    : $PSCommandPath"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Log "Es admin  : $isAdmin"

if (-not $isAdmin) {
    Log "Solicitando elevacion UAC..."
    Start-Process -FilePath 'powershell.exe' `
        -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" `
        -Verb RunAs -Wait
    exit
}

try {
    Log "--- Paso 1: Certificado ---"
    $certPath = Join-Path $env:TEMP 'AdvanceControl-signing.cer'
    [System.IO.File]::WriteAllBytes($certPath, [Convert]::FromBase64String($CerBase64))
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)
    $thumb = $cert.Thumbprint
    Log "Thumbprint: $thumb"

    function Import-IfMissing([string]$store) {
        $existing = Get-ChildItem $store -ErrorAction SilentlyContinue | Where-Object { $_.Thumbprint -eq $thumb }
        if ($existing) { Log "Cert ya existe en $store" ; return }
        Import-Certificate -FilePath $certPath -CertStoreLocation $store | Out-Null
        Log "Cert importado en $store"
    }

    Import-IfMissing 'Cert:\LocalMachine\TrustedPeople'
    Import-IfMissing 'Cert:\LocalMachine\Root'

    Log "--- Paso 2: Descarga ---"
    $appInstallerPath = Join-Path $env:TEMP 'AdvanceControl.appinstaller'
    $url = '{{APP_URL}}'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $url -OutFile $appInstallerPath -UseBasicParsing
    Log "Descargado: $appInstallerPath"

    Log "--- Paso 3: Instalacion ---"
    try {
        Add-AppxPackage -Path $appInstallerPath -AppInstallerFile
        Log "RESULTADO: Instalacion completada OK. Auto-actualizacion activa."
        Write-Host "`n   Instalacion exitosa!" -ForegroundColor Green
    }
    catch {
        Log "Add-AppxPackage fallo: $($_.Exception.Message) (HRESULT: $($_.Exception.HResult))"
        Log "Abriendo instalador visual como fallback..."
        Start-Process -FilePath $appInstallerPath
        Log "Instalador visual abierto. Completa la instalacion en la ventana emergente."
        Write-Host "`n   Completa la instalacion en la ventana que se abrio." -ForegroundColor Yellow
    }
}
catch {
    Log "ERROR FATAL: $($_.Exception.Message)"
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Revisa el log en: $logPath" -ForegroundColor Yellow
    Write-Host 'Presiona Enter para cerrar...'
    Read-Host | Out-Null
    exit 1
}

Log "=== Fin. Log guardado en: $logPath ==="
Write-Host "`nLog guardado en: $logPath" -ForegroundColor Gray
Write-Host 'Presiona Enter para cerrar...'
Read-Host | Out-Null
'@

$psBody = $psBody.Replace('{{CERT_BASE64}}', $cerBase64).Replace('{{APP_URL}}', $AppInstallerUrl)

$content = $cmdHeader + $psBody

# UTF-8 SIN BOM: el BOM rompe el parsing de CMD al inicio del archivo.
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($OutputPath, $content, $utf8NoBom)
Write-Host "Instalador generado: $OutputPath"
