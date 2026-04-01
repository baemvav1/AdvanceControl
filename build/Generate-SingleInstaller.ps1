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

# Cabecera CMD: al ejecutarse con doble clic, CMD copia el .cmd a un .ps1 temporal
# y se lo pasa a PowerShell con -File. Esto es necesario porque Windows PowerShell 5.1
# RECHAZA archivos que no sean .ps1 con -File (exit code -196608).
# Se pasa la ruta original del .cmd como argumento para que el script PS sepa dónde escribir el log.
$cmdHeader = @'
<# :
@echo off
set "SELF=%~f0"
set "TEMP_PS1=%TEMP%\AC-Installer-%RANDOM%.ps1"
copy /y "%SELF%" "%TEMP_PS1%" >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%TEMP_PS1%" "%SELF%"
set "EC=%ERRORLEVEL%"
del "%TEMP_PS1%" >nul 2>nul
exit /b %EC%
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

# Ruta del .cmd original (pasada como argumento por la cabecera CMD)
$ScriptOrigin = if ($args.Count -gt 0 -and $args[0] -and (Test-Path $args[0])) { $args[0] } else { $PSCommandPath }
$ScriptDir = Split-Path $ScriptOrigin -Parent

$CerBase64 = '{{CERT_BASE64}}'

# Log en la misma carpeta del .cmd original para diagnostico
$logPath = Join-Path $ScriptDir 'instalar.log'
function Log([string]$msg) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Add-Content -Path $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

Log "=== Inicio instalacion Advance Control ==="
Log "Usuario   : $env:USERNAME"
Log "Equipo    : $env:COMPUTERNAME"
Log "PSVersion : $($PSVersionTable.PSVersion)"
Log "Script    : $ScriptOrigin"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Log "Es admin  : $isAdmin"

if (-not $isAdmin) {
    Log "Solicitando elevacion UAC..."
    $tempPs1 = Join-Path $env:TEMP ('AC-Installer-Admin-' + (Get-Random) + '.ps1')
    Copy-Item $ScriptOrigin $tempPs1 -Force
    Start-Process -FilePath 'powershell.exe' `
        -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$tempPs1`" `"$ScriptOrigin`"" `
        -Verb RunAs -Wait
    Remove-Item $tempPs1 -Force -ErrorAction SilentlyContinue
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
