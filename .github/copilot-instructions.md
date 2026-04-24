# Instrucciones para Copilot — AdvanceControl (Cliente WinUI 3)

Cliente de escritorio **WinUI 3 / Windows App SDK** (.NET 8, MVVM estricto) que consume `AdvanceControlApi` (por defecto `https://localhost:7055`). **Plataforma obligatoria: x64** (no AnyCPU). Idioma del código, comentarios y mensajes de UI: **español**.

> Lectura obligatoria antes de cambios no triviales: [`README.md`](../../README.md). Contiene catálogo de servicios, ViewModels, Views, dialogs, converters, y patrones detallados.

## Build y test

```bash
dotnet restore
dotnet build -p:Platform=x64                       # SIEMPRE x64
dotnet test  -p:Platform=x64                       # corre todas las pruebas
dotnet test  -p:Platform=x64 --filter "FullyQualifiedName~NombreClase"   # un test/clase
```

Proyecto de pruebas: `Advance Control.Tests/` (xUnit, organizado por `Services/`, `ViewModels/`, `Converters/`).

Ejecución desde Visual Studio: perfil **"Advance Control (Package)"**.

## Arquitectura — MVVM estricto

```
View (.xaml + .xaml.cs)  →  ViewModel : ViewModelBase  →  IXxxService  →  HttpClient → API
```

Reglas no negociables:
- **Cero lógica de negocio en code-behind** (`.xaml.cs`): solo navegación y plomería de eventos UI.
- ViewModels heredan `ViewModelBase` y usan `SetProperty<T>(ref field, value)` para notificar.
- Servicios se inyectan por **constructor**; nunca `new` directamente desde un ViewModel/Controller.
- ViewModels registrados como **Transient**; servicios HTTP como tipados con `AddHttpClient<I,Impl>` + `AddHttpMessageHandler<AuthenticatedHttpHandler>()`.

## Patrón obligatorio para un servicio HTTP nuevo

En `App.xaml.cs`:

```csharp
services.AddHttpClient<IXxxService, XxxService>((sp, client) =>
{
    var provider = sp.GetRequiredService<IApiEndpointProvider>();
    if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
        client.BaseAddress = baseUri;
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<AuthenticatedHttpHandler>();
```

`AuthenticatedHttpHandler` añade `Authorization: Bearer` automáticamente y auto-refresca el token (con `SemaphoreSlim` para evitar carreras). No reimplementes esa lógica en servicios.

## Autenticación y secretos

- Tokens JWT y refresh se guardan en **Windows PasswordVault** vía `ISecureStorage` (claves: `auth_access_token`, `auth_refresh_token`).
- Credenciales SMTP también en PasswordVault (`email_smtp_user`, `email_smtp_password`).
- **Nunca** persistas secrets en `appsettings.json` o en disco plano.

## Configuración runtime

`appsettings.json` declara la URL del API y `DevelopmentMode`. La instalación distribuida puede sobreescribir la URL sin recompilar:

1. `%LocalAppData%\Advance Control\appsettings.local.json` (la app lo crea automáticamente al primer arranque)
2. Variable de entorno `ADVANCECONTROL_ExternalApi__BaseUrl`

Ejemplo plantilla: `Advance Control/appsettings.local.example.json`.

## Acceso al contenedor desde Views

```csharp
AppServices.Get<IEmailService>()   // helper estático para code-behind
App.MainWindow!                    // propiedad estática para XamlRoot/HWND
```

## Modelos del cliente (Models/) — patrón clave

Los DTOs del cliente **no son los del servidor**: añaden estado de UI. `OperacionDto`, `CheckOperacionDto`, `CargoDto` implementan `INotifyPropertyChanged` directamente y manejan flags de carga lazy (`CargosLoaded`, `ImagesLoaded`, `Expand`, etc.). Mantén ese contrato si los modificas.

`CargoDto` se deserializa con `CargoDtoJsonConverter` personalizado.

## OperacionesView — el corazón del sistema

Contiene `ItemsRepeater` con cards expansibles y un sistema de seguimiento de 8 pasos (`checkOperacion`) representado como 8 puntos verde/gris en el header. Para marcar un paso:

```csharp
await ViewModel.UpdateCheckAsync(operacion, "campoCamelCase");
```

Campos: `cotizacionGenerada`, `cotizacionEnviada`, `reporteGenerado`, `reporteEnviado`, `prefacturaCargada`, `hojaServicioCargada`, `ordenCompraCargada`, `facturaCargada`. La carga del check es lazy al expandir el card.

## Niveles de acceso en XAML

```xml
IsEnabled="{Binding Converter={StaticResource AccessLevelConverter}, ConverterParameter=2}"
```

Niveles: 1 lectura · 2 operaciones estándar · 3+ administración. El filtrado por nivel ocurre **solo en el cliente** (la API no lo aplica).

## Restricciones técnicas conocidas (no las "arregles")

- **Compilar con `-p:Platform=x64`**; AnyCPU rompe el packager.
- **Sin `global using`**: cada archivo nuevo lleva sus `using` explícitos.
- WinUI 3 **no permite dos `ContentDialog` simultáneos**: cierra el primero antes de abrir el segundo.
- `PasswordBox` no soporta binding bidireccional → manejar `PasswordChanged` en code-behind y empujar al ViewModel.
- WebView2 requiere `await EnsureCoreWebView2Async()` en `Loaded`.
- `BoolToColorBrushConverter` dentro de `ItemsRepeater`: usa `x:Bind` con `Mode=OneWay`, no `{Binding}`.
- Imágenes en correos: usa **CID** (`builder.LinkedResources.Add(...)` + `<img src="cid:email-firma"/>`); Gmail/Outlook bloquean `data:image;base64`.

## Archivos locales generados por la app

```
Documents\Advance Control\
├── Cotizaciones\      # PDFs generados (IQuoteService)
├── Reportes\
├── Cabeceras\         # imágenes para PDFs de empresa
└── Firmas Correos\    # email_at_dominio.ext (@ → _)
```

Acceso a la firma: `FirmaCorreoHelper.GetFirmaPath(email)` / `GetFirmaCidHtml()`.

## Convenciones C#

- PascalCase clases/métodos, camelCase locales, `_camelCase` campos privados.
- Interfaces con prefijo `I`. Métodos de I/O y HTTP siempre `async Task<T>`.
- Nullable habilitado; valida nulos explícitamente.
- Mensajes y excepciones en **español**.

## Distribución (MSIX + App Installer)

- Push a `main` → workflow `.github/workflows/publish-client-installer.yml` compila, firma y publica `AdvanceControl.appinstaller` + `AdvanceControl-x64.msix` en releases.
- Empaquetado local: `build/Publish-ClientInstaller.ps1` (con o sin certificado).
- Fallback sin firma: `build/Publish-ClientPortable.ps1` y workflow `publish-client-portable.yml` (solo pruebas internas).
- Secretos requeridos en GitHub: `WINDOWS_PFX_BASE64`, `WINDOWS_PFX_PASSWORD`.
