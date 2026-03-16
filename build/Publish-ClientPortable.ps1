[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\Advance Control\Advance Control.csproj'),
    [string]$Configuration = 'Release',
    [ValidateSet('x64')]
    [string]$Platform = 'x64',
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\portable')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedProjectPath = (Resolve-Path $ProjectPath).Path
$projectDirectory = Split-Path $resolvedProjectPath -Parent
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$publishDirectory = Join-Path $resolvedOutputDirectory 'app'
$archivePath = Join-Path $resolvedOutputDirectory "AdvanceControl-portable-$Platform.zip"
$notesPath = Join-Path $resolvedOutputDirectory 'LEEME-PORTABLE.txt'
$exampleConfigPath = Join-Path $projectDirectory 'appsettings.local.example.json'

if (Test-Path $resolvedOutputDirectory) {
    Remove-Item -Path $resolvedOutputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

$publishArguments = @(
    'publish',
    $resolvedProjectPath,
    '-c', $Configuration,
    '-r', "win-$Platform",
    '--self-contained', 'true',
    '-p:Platform=x64',
    '-p:PublishSingleFile=false',
    '-p:PublishTrimmed=false',
    '-p:PublishReadyToRun=false',
    "-p:Version=$Version",
    '-o', $publishDirectory,
    '-nologo'
)

Write-Host "Publicando cliente portable con dotnet..."
& dotnet @publishArguments

if ($LASTEXITCODE -ne 0) {
    throw "La publicacion portable fallo con codigo $LASTEXITCODE."
}

if (Test-Path $exampleConfigPath) {
    Copy-Item -Path $exampleConfigPath -Destination (Join-Path $publishDirectory 'appsettings.local.example.json') -Force
}

$notes = @"
Advance Control portable temporal
Version: $Version

Este paquete temporal no usa firma ni App Installer.
Se distribuye para pruebas internas mientras se configura el certificado MSIX.

Antes de ejecutar el cliente en otra PC:
1. Ejecutar Advance Control.exe una vez para que la app cree %LocalAppData%\Advance Control\appsettings.local.json
2. Editar ExternalApi:BaseUrl con la IP o nombre local del equipo que hospeda el API
3. Volver a ejecutar Advance Control.exe
"@

Set-Content -Path $notesPath -Value $notes -Encoding UTF8

Compress-Archive -Path (Join-Path $publishDirectory '*'), $notesPath -DestinationPath $archivePath -CompressionLevel Optimal

Write-Host "Portable listo: $archivePath"
