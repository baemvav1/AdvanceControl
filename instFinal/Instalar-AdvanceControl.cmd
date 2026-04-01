<# :
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~f0" %*
exit /b %ERRORLEVEL%
#>
#Requires -Version 5.1
<#
    Instalar-AdvanceControl.cmd
    Instala Advance Control con auto-actualizacion automatica.
    Doble clic para ejecutar - no requiere archivos adicionales.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$CerBase64 = 'MIIC8DCCAdigAwIBAgIQFRb52xv+nodF2iHmvRpbIjANBgkqhkiG9w0BAQsFADAQMQ4wDAYDVQQDDAViYWVtdjAeFw0yNjA0MDExNDUyMjdaFw0zMTA0MDExNTAyMjZaMBAxDjAMBgNVBAMMBWJhZW12MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA4KlSJCxqj7IT1v1N4ROhjVF04vvo4rqKFeJz86xiaZ00RzA5kFntwP3nxxdO1o1OZsWQLhQLMzHbzw2V1h1fEqncs4wexSdGWg8d8Pd2N2X8OZls6/3+uVgvMhgtPCihqVGwQyVYd3STf+nIssA6VB/xlzJVS2SBni/qEm+k/HbM+rqbEeWMVc8hqoD4p9GSoqob6D3lacwRLp1ejurp7ChQSidVzRTytWh6inMkDFSQgpopsB8RcUVxT6TBgH95oExcFiHyApdqEdgho8je6nxWVdLXjamb0jxMdOTH3x1b6TCgJP3hdjtfs803fs09jH8AtK04QM0Mkx0XbxbOyQIDAQABo0YwRDAOBgNVHQ8BAf8EBAMCB4AwEwYDVR0lBAwwCgYIKwYBBQUHAwMwHQYDVR0OBBYEFHCBUmuF0iG30mLv1FNLHI0xPrdhMA0GCSqGSIb3DQEBCwUAA4IBAQB9e6/cCqK5p+y7ZwNUnT6LSOdKBnQj55pa+zVyDqo8j27lKXR1wTVkqvo08b21/eLc2VT+oXtzYh76uXWgZeSQ0b1dZhisKIfMaLMw9RuY1gVbhbTMF3B4YiAVG6Eddw2XQtuM3kYdwdU3fzALJoGq61FR5dyg40d1YvGgZlQmyAqfTiUzgEvezpx7CeWZJp9saW4/n/RjqEhawwCs4w2T9XaiB32HQb/3cPdc84cjRmcQ6dvcRDYx1+F+t0OnyfcmSmHRjDbIvco3jZtyOwnM6Xk9J8zNIQAuwInaeI5NVLQm0x6GENbckqqIMsdNYKpWEd7WVI3BuqgaUWJaXFVR'

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
    $url = 'https://github.com/baemvav1/AdvanceControl/releases/download/client-latest/AdvanceControl.appinstaller'
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