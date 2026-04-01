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
$ErrorActionPreference = 'Stop'

# --- Certificado embebido ---
$CerBase64 = 'MIIC8DCCAdigAwIBAgIQFRb52xv+nodF2iHmvRpbIjANBgkqhkiG9w0BAQsFADAQMQ4wDAYDVQQDDAViYWVtdjAeFw0yNjA0MDExNDUyMjdaFw0zMTA0MDExNTAyMjZaMBAxDjAMBgNVBAMMBWJhZW12MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA4KlSJCxqj7IT1v1N4ROhjVF04vvo4rqKFeJz86xiaZ00RzA5kFntwP3nxxdO1o1OZsWQLhQLMzHbzw2V1h1fEqncs4wexSdGWg8d8Pd2N2X8OZls6/3+uVgvMhgtPCihqVGwQyVYd3STf+nIssA6VB/xlzJVS2SBni/qEm+k/HbM+rqbEeWMVc8hqoD4p9GSoqob6D3lacwRLp1ejurp7ChQSidVzRTytWh6inMkDFSQgpopsB8RcUVxT6TBgH95oExcFiHyApdqEdgho8je6nxWVdLXjamb0jxMdOTH3x1b6TCgJP3hdjtfs803fs09jH8AtK04QM0Mkx0XbxbOyQIDAQABo0YwRDAOBgNVHQ8BAf8EBAMCB4AwEwYDVR0lBAwwCgYIKwYBBQUHAwMwHQYDVR0OBBYEFHCBUmuF0iG30mLv1FNLHI0xPrdhMA0GCSqGSIb3DQEBCwUAA4IBAQB9e6/cCqK5p+y7ZwNUnT6LSOdKBnQj55pa+zVyDqo8j27lKXR1wTVkqvo08b21/eLc2VT+oXtzYh76uXWgZeSQ0b1dZhisKIfMaLMw9RuY1gVbhbTMF3B4YiAVG6Eddw2XQtuM3kYdwdU3fzALJoGq61FR5dyg40d1YvGgZlQmyAqfTiUzgEvezpx7CeWZJp9saW4/n/RjqEhawwCs4w2T9XaiB32HQb/3cPdc84cjRmcQ6dvcRDYx1+F+t0OnyfcmSmHRjDbIvco3jZtyOwnM6Xk9J8zNIQAuwInaeI5NVLQm0x6GENbckqqIMsdNYKpWEd7WVI3BuqgaUWJaXFVR'

function Write-Step([string]$msg) { Write-Host "
>> $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg)   { Write-Host "   OK: $msg" -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "   AVISO: $msg" -ForegroundColor Yellow }

# --- Auto-elevacion a administrador ---
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host 'Solicitando permisos de administrador...' -ForegroundColor Yellow
    Start-Process -FilePath 'powershell.exe' 
        -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File \"$PSCommandPath\"" 
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
    $url = 'https://github.com/baemvav1/AdvanceControl/releases/download/client-latest/AdvanceControl.appinstaller'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $url -OutFile $appInstallerPath -UseBasicParsing
    Write-Ok "Descargado en $appInstallerPath"

    Write-Step 'Instalando Advance Control...'
    Add-AppxPackage -Path $appInstallerPath -AppInstallerFile
    Write-Ok 'Instalacion completada. La app se actualizara automaticamente en cada inicio.'
}
catch {
    Write-Host "
ERROR: $_" -ForegroundColor Red
    Write-Host 'Presiona Enter para cerrar...'
    Read-Host | Out-Null
    exit 1
}

Write-Host "
Presiona Enter para cerrar..." -ForegroundColor Gray
Read-Host | Out-Null