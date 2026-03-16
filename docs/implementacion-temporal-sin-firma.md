# Implementacion temporal sin firma

## Objetivo

Se agrego una via temporal de distribucion del cliente `Advance Control` mientras el flujo principal de `MSIX + App Installer` espera un certificado de firma valido.

## Que se implemento

### Flujo principal firmado

- Workflow: `.github/workflows/publish-client-installer.yml`
- Script: `build/Publish-ClientInstaller.ps1`
- Resultado esperado: `AdvanceControl.appinstaller` + `AdvanceControl-x64.msix`
- Estado: listo en codigo, pero depende de los secretos `WINDOWS_PFX_BASE64` y `WINDOWS_PFX_PASSWORD`

### Flujo temporal sin firma

- Workflow: `.github/workflows/publish-client-portable.yml`
- Script: `build/Publish-ClientPortable.ps1`
- Resultado esperado:
  - `AdvanceControl-portable-x64.zip`
  - `LEEME-PORTABLE.txt`
- Estado: pensado para pruebas internas y despliegue manual mientras no exista certificado

## Como funciona el flujo temporal

1. GitHub Actions compila y prueba el cliente en `main`
2. Se ejecuta `dotnet publish` para `win-x64`
3. Se genera un paquete portable en ZIP
4. El workflow publica un release marcado como `prerelease`

## Limitaciones importantes

- No genera `MSIX` instalable ni `AppInstaller`
- No ofrece autoactualizacion de Windows
- La actualizacion sigue siendo manual: descargar ZIP nuevo y reemplazar carpeta
- Es una solucion temporal, no la via final de distribucion

## Configuracion del API para clientes en red local

Como el API sigue siendo local, cada PC cliente debe crear:

`%LocalAppData%\Advance Control\appsettings.local.json`

Ejemplo:

```json
{
  "ExternalApi": {
    "BaseUrl": "http://192.168.1.72:5030/"
  }
}
```

`localhost` solo funciona en la misma maquina donde corre el API.

## Criterio para retirar este flujo temporal

Cuando el repositorio tenga:

- `WINDOWS_PFX_BASE64`
- `WINDOWS_PFX_PASSWORD`

y el workflow firmado publique releases correctamente, el flujo portable puede mantenerse solo como fallback o eliminarse si ya no hace falta.
