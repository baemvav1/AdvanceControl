[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\Advance Control\Advance Control.csproj'),
    [string]$Configuration = 'Release',
    [ValidateSet('x64')]
    [string]$Platform = 'x64',
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [Parameter(Mandatory = $true)]
    [string]$AppInstallerBaseUri,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\installer'),
    [string]$CertificatePath,
    [string]$CertificatePassword
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-MSBuildPath {
    $msbuildCommand = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($msbuildCommand) {
        return $msbuildCommand.Source
    }

    $vswherePath = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswherePath) {
        $resolved = & $vswherePath -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($resolved) {
            return $resolved
        }
    }

    throw 'No se encontro MSBuild. Instala Visual Studio Build Tools 2022 o ejecuta este script desde un agente con MSBuild.'
}

$resolvedProjectPath = (Resolve-Path $ProjectPath).Path
$projectDirectory = Split-Path $resolvedProjectPath -Parent
$manifestPath = Join-Path $projectDirectory 'Package.appxmanifest'
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$packageOutputDirectory = Join-Path $resolvedOutputDirectory 'package'
$stablePackageName = "AdvanceControl-$Platform.msix"
$stableAppInstallerName = 'AdvanceControl.appinstaller'
$trimmedBaseUri = $AppInstallerBaseUri.TrimEnd('/')
$appInstallerUri = "$trimmedBaseUri/$stableAppInstallerName"
$packageUri = "$trimmedBaseUri/$stablePackageName"

if (Test-Path $packageOutputDirectory) {
    Remove-Item -Path $packageOutputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $packageOutputDirectory -Force | Out-Null

[xml]$manifest = Get-Content -Path $manifestPath -Raw
$identity = $manifest.Package.Identity
if (-not $identity) {
    throw "No se encontro el nodo Identity en '$manifestPath'."
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$originalManifestContent = Get-Content -Path $manifestPath -Raw
$originalVersion = [string]$identity.Version
$publisher = [string]$identity.Publisher
$packageName = [string]$identity.Name
if ([string]::IsNullOrWhiteSpace($publisher) -or [string]::IsNullOrWhiteSpace($packageName)) {
    throw "El manifiesto '$manifestPath' no contiene Name y Publisher validos para generar el AppInstaller."
}

$signingEnabled = if ([string]::IsNullOrWhiteSpace($CertificatePath)) { 'false' } else { 'true' }
$publishProfile = 'Properties\PublishProfiles\installer-x64.pubxml'
$msbuildPath = Get-MSBuildPath

$msbuildArguments = @(
    $resolvedProjectPath,
    '/restore',
    '/t:Publish',
    '/nologo',
    '/verbosity:minimal',
    '/maxCpuCount:1',
    '/nodeReuse:false',
    "/p:Configuration=$Configuration",
    "/p:Platform=$Platform",
    "/p:RuntimeIdentifier=win-$Platform",
    "/p:PublishProfile=$publishProfile",
    "/p:AppxPackageDir=$packageOutputDirectory\",
    '/p:GenerateAppxPackageOnBuild=true',
    "/p:AppxBundle=Never",
    '/p:UapAppxPackageBuildMode=SideLoadOnly',
    '/p:GenerateAppInstallerFile=false',
    '/p:PublishReadyToRun=false',
    '/p:PublishTrimmed=false',
    "/p:AppxPackageVersion=$Version",
    "/p:AppxPackageSigningEnabled=$signingEnabled"
)

if ($signingEnabled -eq 'true') {
    $resolvedCertificatePath = (Resolve-Path $CertificatePath).Path
    $msbuildArguments += "/p:PackageCertificateKeyFile=$resolvedCertificatePath"
    $msbuildArguments += "/p:PackageCertificatePassword=$CertificatePassword"
}

try {
    if ($originalVersion -ne $Version) {
        $identity.Version = $Version
        [System.IO.File]::WriteAllText($manifestPath, $manifest.OuterXml, $utf8NoBom)
    }

    Write-Host "Publicando instalador con MSBuild desde '$msbuildPath'..."
    & $msbuildPath @msbuildArguments

    if ($LASTEXITCODE -ne 0) {
        throw "La publicacion del instalador fallo con codigo $LASTEXITCODE."
    }

    $generatedPackage = Get-ChildItem -Path $packageOutputDirectory -Recurse -File |
        Where-Object { $_.Extension -eq '.msix' } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if (-not $generatedPackage) {
        throw "No se encontro ningun archivo .msix en '$packageOutputDirectory'."
    }

    $stablePackagePath = Join-Path $resolvedOutputDirectory $stablePackageName
    Copy-Item -Path $generatedPackage.FullName -Destination $stablePackagePath -Force

    $appInstallerContent = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller
    xmlns="http://schemas.microsoft.com/appx/appinstaller/2017"
    xmlns:s4="http://schemas.microsoft.com/appx/appinstaller/2021"
    Version="$Version"
    Uri="$appInstallerUri">
    <MainPackage
        Name="$packageName"
        Publisher="$publisher"
        Version="$Version"
        ProcessorArchitecture="$Platform"
        Uri="$packageUri" />
    <UpdateSettings>
        <OnLaunch s4:HoursBetweenUpdateChecks="0" s4:ShowPrompt="true" s4:UpdateBlocksActivation="false" />
    </UpdateSettings>
</AppInstaller>
"@

    $appInstallerPath = Join-Path $resolvedOutputDirectory $stableAppInstallerName
    Set-Content -Path $appInstallerPath -Value $appInstallerContent -Encoding UTF8

    Write-Host "Paquete listo: $stablePackagePath"
    Write-Host "AppInstaller listo: $appInstallerPath"
}
finally {
    if ($originalVersion -ne $Version) {
        [System.IO.File]::WriteAllText($manifestPath, $originalManifestContent, $utf8NoBom)
    }
}
