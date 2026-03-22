# AdvanceControl — Documentación Técnica (Cliente WinUI 3)

> **Versión del stack:** .NET 8 · WinUI 3 (Windows App SDK) · MVVM estricto  
> **Plataforma:** Windows (x64 obligatorio — no AnyCPU)  
> **API objetivo por defecto:** `https://localhost:7055`

---

## 1. Arquitectura MVVM

```
View (.xaml + .xaml.cs)
    │  x:Bind / {Binding}
    ▼
ViewModel  (hereda ViewModelBase : INotifyPropertyChanged)
    │  inyectado por constructor via DI
    ▼
IXxxService  (interfaz)
    │
    ▼
XxxService  (HttpClient tipado → AdvanceControlApi)
```

**Reglas estrictas:**
- Cero lógica de negocio en el code-behind (`.xaml.cs`): solo navegación y eventos de UI.
- Los ViewModels NO hacen `new` de servicios; todo se inyecta por constructor.
- `SetProperty<T>(ref field, value)` para notificar cambios de propiedades.

---

## 2. Estructura de Carpetas

```
Advance Control/
├── App.xaml / App.xaml.cs    # Host de DI, registro de servicios, ventana principal
├── appsettings.json           # URL base de la API, modo desarrollo
├── Converters/                # IValueConverter para XAML bindings
├── Models/                    # DTOs del cliente
├── Navigation/                # Servicio de navegación entre páginas
├── Services/                  # Servicios organizados por carpeta de dominio
├── Settings/                  # Clases de opciones de configuración
├── Utilities/                 # Helpers de uso general
├── ViewModels/                # Un ViewModel por View
└── Views/
    ├── Dialogs/               # ContentDialogs y UserControls
    ├── Login/                 # Pantalla de login
    ├── Pages/                 # Páginas del menú principal
    └── MainWindow.xaml        # Ventana raíz con NavigationView
```

---

## 3. App.xaml.cs — Contenedor de DI

El Host de DI se crea en el constructor de `App`. Todos los servicios se registran aquí.

### Patrón para HTTP clients autenticados
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

### `AuthenticatedHttpHandler`
Intercepta cada petición HTTP y agrega `Authorization: Bearer <token>`. Si el token expiró, auto-refresca antes de reintentar. Usa `SemaphoreSlim` para evitar condiciones de carrera.

### Acceso al contenedor desde Views (code-behind)
```csharp
AppServices.Get<IEmailService>()  // helper estático para Views/code-behind
```

### `App.MainWindow`
```csharp
App.MainWindow!  // propiedad estática — usarla para XamlRoot y HWND
```

---

## 4. Configuración (appsettings.json)

```json
{
  "ExternalApi": {
    "BaseUrl": "https://localhost:7055/"
  },
  "DevelopmentMode": {
    "Enabled": false,
    "DisableAuthTimeouts": false,
    "DisableHttpTimeouts": false
  }
}
```

`DevelopmentMode.Enabled = true` desactiva timeouts HTTP y de autenticación.

### Override para instalaciones distribuidas

El paquete instalado puede sobreescribir la URL del API sin recompilar usando alguno de estos mecanismos:

1. Archivo local: `%LocalAppData%\Advance Control\appsettings.local.json`
2. Variable de entorno: `ADVANCECONTROL_ExternalApi__BaseUrl`

En instalaciones MSIX y en la distribucion portable, la app crea automaticamente `%LocalAppData%\Advance Control\appsettings.local.json` en el primer arranque si todavia no existe. Durante esta fase de pruebas puedes editar ese archivo para apuntar el cliente a la IP local actual del API y, mas adelante, cambiarlo de nuevo cuando el API quede publicado, sin recompilar ni reinstalar.

Ejemplo de `%LocalAppData%\Advance Control\appsettings.local.json`:

```json
{
  "ExternalApi": {
    "BaseUrl": "https://servidor-o-ip-local:7055/"
  }
}
```

El repositorio incluye un ejemplo en `Advance Control\appsettings.local.example.json`.

---

## 5. Autenticación

### Flujo
1. `LoginViewModel` llama `IAuthService.LoginAsync(usuario, password)`
2. La API devuelve `{ accessToken, refreshToken }`
3. Tokens almacenados en **Windows PasswordVault** (`ISecureStorage`)
4. `AuthenticatedHttpHandler` los inyecta automáticamente en cada request

### Claves en PasswordVault
| Clave | Contenido |
|---|---|
| `auth_access_token` | JWT actual |
| `auth_refresh_token` | Refresh token |
| `email_smtp_user` | Correo SMTP configurado |
| `email_smtp_password` | Contraseña SMTP |

### Nivel de acceso
El nivel del usuario (del JWT) controla visibilidad de controles en XAML:
```xml
IsEnabled="{Binding Converter={StaticResource AccessLevelConverter}, ConverterParameter=2}"
```
- Nivel 1: lectura  
- Nivel 2: operaciones estándar  
- Nivel 3+: administración

---

## 6. Servicios del Cliente

### Servicios de infraestructura
| Servicio | Descripción |
|---|---|
| `IApiEndpointProvider` | Construye URLs completas desde la base URL configurada |
| `IAuthService` | Login, logout, refresh token |
| `ISecureStorage` | Windows PasswordVault |
| `AuthenticatedHttpHandler` | Inyecta Bearer token en requests HTTP |
| `IOnlineCheck` | Verifica conectividad con la API |
| `INavigationService` | Navegación entre páginas (Frame) |
| `INotificacionService` | Muestra ContentDialogs de alerta/confirmación |
| `ILoggingService` | Envía logs a la API |
| `IActivityService` | Registra actividad del usuario |
| `IImageViewerService` | Abre imágenes en visor flotante |
| `ILocalStorageService` | Persistencia local simple |
| `IEmailService` | Envío de correos SMTP (Hostinger/MailKit) |

### Servicios de dominio (HTTP hacia la API)
| Servicio | Endpoint API |
|---|---|
| `IClienteService` | `/api/Clientes` |
| `IContactoService` | `/api/Contacto` |
| `IOperacionService` | `/api/Operaciones` |
| `ICheckOperacionService` | `/api/CheckOperacion` |
| `ICargoService` | `/api/Cargos` |
| `IEquipoService` | `/api/Equipo` |
| `IMantenimientoService` | `/api/Mantenimiento` |
| `IProveedorService` | `/api/Proveedores` |
| `IRefaccionService` | `/api/Refaccion` |
| `IServicioService` | `/api/Servicio` |
| `IAreaService` | `/api/Areas` |
| `IUbicacionService` | `/api/Ubicacion` |
| `IRelacionEquipoClienteService` | `/api/Relaciones` |
| `IRelacionRefaccionEquipoService` | `/api/RelacionRefaccionEquipo` |
| `IRelacionProveedorRefaccionService` | `/api/RelacionProveedorRefaccion` |
| `IRelacionOperacionProveedorRefaccionService` | `/api/RelacionOpPR` |
| `IEntidadService` | `/api/Entidad` |
| `INivelService` | `/api/Nivel` |
| `IUserInfoService` | `/api/UserInfo` |
| `IGoogleMapsService` | `/api/GoogleMaps` |
| `IEstadoCuentaService` (+ financieros) | `/api/EstadoCuenta`, etc. |
| `IQuoteService` | Local — genera PDFs (cotización/reporte) |

---

## 7. ViewModels

Todos heredan `ViewModelBase`:
```csharp
public class ViewModelBase : INotifyPropertyChanged
{
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null);
    public void OnPropertyChanged([CallerMemberName] string? name = null);
}
```

| ViewModel | Page |
|---|---|
| `LoginViewModel` | Login |
| `MainViewModel` | MainWindow (NavigationView) |
| `DashboardViewModel` | DashboardPage |
| `OperacionesViewModel` | OperacionesView |
| `CustomersViewModel` | ClientesView |
| `ContactosViewModel` | ContactosView |
| `EquiposViewModel` | EquiposView |
| `MttoViewModel` | MttoView (Mantenimiento) |
| `ProveedoresViewModel` | ProveedoresView |
| `RefaccionesViewModel` | RefaaccionView |
| `ServiciosViewModel` | ServiciosView |
| `UbicacionesViewModel` | UbicacionesView |
| `AreasViewModel` | AreasView |
| `EntidadesViewModel` | EntidadesView |
| `CorreoViewModel` | CorreoView |
| `EsCuentaViewModel` | EsCuentaView (Estado de Cuenta) |
| `AcesoriaViewModel` | AcesoriaView |
| `NuevoEquipoViewModel` | NuevoEquipoView |

---

## 8. Páginas (Views/Pages)

| Page | Función |
|---|---|
| `DashboardPage` | Vista principal / resumen |
| `OperacionesView` | Gestión de operaciones (núcleo del sistema) |
| `ClientesView` | CRUD de clientes |
| `ContactosView` | CRUD de contactos |
| `EquiposView` | CRUD de equipos |
| `MttoView` | Mantenimientos pendientes → convertir en operación |
| `ProveedoresView` | CRUD de proveedores |
| `RefaaccionView` | CRUD de refacciones |
| `ServiciosView` | CRUD de servicios |
| `UbicacionesView` | Gestión de ubicaciones con mapa Google Maps |
| `AreasView` | Áreas geográficas (polígonos, círculos) con mapa |
| `EntidadesView` | Datos de la empresa (para cabeceras de PDFs) |
| `CorreoView` | Configuración de correo SMTP + firma de correo |
| `EsCuentaView` | Estado de cuenta bancario |
| `AcesoriaView` | Vista de asesoría |

---

## 9. Dialogs y UserControls (Views/Dialogs)

| Dialog/Control | Uso |
|---|---|
| `CotizacionVisorDialog` | Visor WebView2 de PDF. Parámetro `tipo="Cotización"/"Reporte"`. Botones: Enviar por correo / Abrir externo / Cerrar |
| `EnviarCotizacionDialog` | Composición y envío de correo con PDF adjunto. Parámetro `tipo`. Soporta Para, CC (checkboxes de contactos), CC manual, CCO |
| `NuevoClienteUserControl` | Formulario de alta de cliente |
| `NuevoMantenimientoUserControl` | Alta de mantenimiento |
| `NuevoEquipoView` | Alta de equipo |
| `AgregarCargoUserControl` | Agrega cargo (servicio/refacción/proveedor) a operación |
| `SeleccionarClienteUserControl` | Picker de cliente |
| `SeleccionarEquipoUserControl` | Picker de equipo |
| `SeleccionarServicioUserControl` | Picker de servicio |
| `SeleccionarRefaccionUserControl` | Picker de refacción |
| `SeleccionarUbicacionUserControl` | Picker de ubicación |
| `ViewerImagenes` | Visor reutilizable de imágenes con zoom/pan hospedado en ventana independiente |
| `RefaccionesViewerUserControl` | Lista de refacciones de un equipo |

---

## 10. OperacionesView — Sistema Central

`OperacionesView` es la vista más compleja del sistema. Contiene un `ItemsRepeater` con cards expansibles por operación.

### Flujo de una operación
```
Mantenimiento (MttoView) → "Convertir a Operación"
    │
    ▼
Operación (OperacionesView)
    ├── Cargos (servicios/refacciones añadidos al card)
    ├── Generar Cotización → PDF → Visor → [Enviar por correo]
    ├── Generar Reporte   → PDF → Visor → [Enviar por correo]
    ├── Upload Prefactura  (imagen)
    ├── Upload Hoja de Servicio (imagen)
    ├── Upload Orden de Compra  (imagen)
    └── Upload Factura    (PDF → finaliza la operación automáticamente)
```

### checkOperacion — Sistema de Seguimiento de Pasos
Al expandir un card de operación, `ToggleExpandButton_Click` carga lazy `CheckOperacionDto` vía `ViewModel.LoadCheckAsync(operacion)`.

| Flag | Se marca `true` en |
|---|---|
| `cotizacionGenerada` | Después de generar PDF de cotización |
| `cotizacionEnviada` | Después de enviar correo de cotización |
| `reporteGenerado` | Después de generar PDF de reporte |
| `reporteEnviado` | Después de enviar correo de reporte |
| `prefacturaCargada` | Después de `UploadPrefacturaAsync` exitoso |
| `hojaServicioCargada` | Después de `UploadHojaServicioAsync` exitoso |
| `ordenCompraCargada` | Después de `UploadOrdenCompraAsync` exitoso |
| `facturaCargada` | Después de `UploadFacturaAsync` exitoso |

El header de cada card muestra una fila de 8 puntos: **verde** = completado, **gris** = pendiente.  
Para actualizar un paso: `await ViewModel.UpdateCheckAsync(operacion, "campoCamelCase")`.

### Generación de PDFs (IQuoteService — local, sin API)
Los PDFs se generan localmente. Los archivos se guardan en:
```
Documents\Advance Control\Cotizaciones\
Documents\Advance Control\Reportes\
```
Los datos de la empresa emisora (nombre, RFC, dirección) provienen de `IEntidadService`.

---

## 11. Sistema de Correo

### Configuración (CorreoView + CorreoViewModel)
- Credenciales SMTP guardadas en PasswordVault (`email_smtp_user`, `email_smtp_password`)
- Firma: imagen guardada en `Documents\Advance Control\Firmas Correos\{email_at_dominio.ext}`
- Verificación de conexión incluida en la UI

### `EmailService` (smtp.hostinger.com)
- **SMTP:** `smtp.hostinger.com:587` STARTTLS (MailKit)
- **IMAP:** `imap.hostinger.com:993` SSL (MailKit)
- Soporta `EmailMessage.CCO` (BCC), `Adjuntos[]`, `FirmaImagePath`

### `FirmaCorreoHelper` (estático)
```csharp
FirmaCorreoHelper.GetFirmaPath(email)         // ruta del archivo de firma
FirmaCorreoHelper.GetFirmaCidHtml()           // "<img src=\"cid:email-firma\"/>"
FirmaCorreoHelper.GuardarFirmaAsync(email, srcPath)
FirmaCorreoHelper.EliminarFirma(email)
```

### Firma inline (CID) — Por qué no base64
Gmail y Outlook bloquean `data:image/...;base64,...` en HTML de correo.  
Usar `builder.LinkedResources.Add(path)` con `ContentId = "email-firma"`.  
El HTML usa `<img src="cid:email-firma"/>`.

---

## 12. Converters XAML Disponibles

| Converter | ResourceKey | Uso |
|---|---|---|
| `BooleanToVisibilityConverter` | `BooleanToVisibilityConverter` | `bool → Visibility` |
| `BooleanToCornerRadiusConverter` | `BooleanToCornerRadiusConverter` | Esquinas según expand |
| `BooleanToExpandTextConverter` | `BooleanToExpandTextConverter` | Texto del botón expandir |
| `NullToVisibilityConverter` | `NullToVisibilityConverter` | `null → Collapsed` |
| `CurrencyConverter` | `CurrencyConverter` | `float → "$X,XXX.XX"` |
| `AccessLevelConverter` | `AccessLevelConverter` | `nivel >= param → IsEnabled` |
| `BooleanToSystemColorBrushConverter` | `BoolToColorBrushConverter` | `bool → Brush (param: 'Color1\|Color2')` |
| `RefaccionVisibilityConverter` | `RefaccionVisibilityConverter` | Visibility según tipo de cargo |
| `BoolNegationConverter` | — | Negación bool |
| `NullableNumberToStringConverter` | — | Nullable numérico a string |
| `DateTimeFormatConverter` | — | Formateo de fechas |
| `LevelToBackgroundConverter` / `LevelToForegroundConverter` | — | Colores por nivel de log |

---

## 13. Modelos del Cliente (Models/)

Los DTOs del cliente **no son los mismos** que los del servidor. Tienen propiedades adicionales para el estado de UI:

### `OperacionDto` (cliente)
- Implementa `INotifyPropertyChanged` directamente
- `Expand` — si el card está expandido
- `CargosLoaded`, `ImagesLoaded`, `IsLoadingCargos` — estado de carga lazy
- `CollectionChangedSubscribed` — evita suscripciones duplicadas al CollectionChanged
- `CheckOperacion` — lazy load, tipo `CheckOperacionDto?` con `INotifyPropertyChanged`
- `TieneCheck` — computed, `true` cuando `CheckOperacion != null`
- `HasPrefactura`, `HasHojaServicio`, `HasOrdenCompra`, `HasFactura` — indicadores de documentos
- `RazonSocial`, `Identificador`, `Atiende` — campos JOIN del servidor

### `CheckOperacionDto` (cliente)
- 8 campos `bool` + computed: `StepsCompletados`, `TotalSteps` (=8), `PorcentajeCompletado`, `Completo`

### `CargoDto` (cliente)
- Implementa `INotifyPropertyChanged`
- Deserialización personalizada con `CargoDtoJsonConverter`
- `Imágenes` se cargan lazy

---

## 14. Patrón para Implementar una Pantalla Nueva

### Paso 1: Servicio (si hay nuevo endpoint)
```
Services/NombreEntidad/INombreEntidadService.cs
Services/NombreEntidad/NombreEntidadService.cs
```

### Paso 2: Modelo
```
Models/NombreEntidadDto.cs
```

### Paso 3: ViewModel
```csharp
public class NombreEntidadViewModel : ViewModelBase
{
    private readonly INombreEntidadService _service;
    private readonly ILoggingService _logger;
    public NombreEntidadViewModel(INombreEntidadService service, ILoggingService logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### Paso 4: View
```xml
<!-- Views/Pages/NombreEntidadView.xaml -->
<Page x:Class="Advance_Control.Views.Pages.NombreEntidadView" ...>
    <Page.DataContext>
        <!-- No asignar aquí; se inyecta via INavigationService -->
    </Page.DataContext>
</Page>
```

### Paso 5: Registrar en App.xaml.cs
```csharp
// Servicio con HttpClient autenticado
services.AddHttpClient<INombreEntidadService, NombreEntidadService>((sp, client) =>
{
    client.BaseAddress = new Uri(sp.GetRequiredService<IApiEndpointProvider>().GetApiBaseUrl());
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthenticatedHttpHandler>();

// ViewModel siempre Transient
services.AddTransient<NombreEntidadViewModel>();
```

### Paso 6: Agregar al NavigationView (MainWindow.xaml)
```xml
<NavigationViewItem Content="Nombre" Tag="NombreEntidadView" Icon="..." />
```

---

## 15. Restricciones Técnicas Conocidas

| Restricción | Solución |
|---|---|
| WinUI 3 no soporta dos `ContentDialog` simultáneos | Cerrar el primero (via Primary/Secondary) antes de abrir el segundo |
| `PasswordBox` no soporta `{Binding}` bidireccional | Usar evento `PasswordChanged` en code-behind que empuja al ViewModel |
| Compilar requiere `-p:Platform=x64` | No usar AnyCPU; el packager falla |
| Sin `global using` en el proyecto | Cada archivo nuevo necesita sus propios `using` explícitos |
| Imágenes inline en correo bloqueadas por Gmail/Outlook | Usar CID en lugar de base64 |
| `App.MainWindow` es propiedad estática | Acceder como `App.MainWindow!` |
| WebView2 requiere inicialización async | Llamar `await EnsureCoreWebView2Async()` en el evento `Loaded` |
| `BoolToColorBrushConverter` en ItemsRepeater | Usar `x:Bind` (no `{Binding}`) con `Mode=OneWay` |

---

## 16. Archivos Locales de la Aplicación

```
Documents\Advance Control\
├── Cotizaciones\      # PDFs de cotizaciones generadas
├── Reportes\          # PDFs de reportes generados
├── Cabeceras\         # Imágenes de cabecera de empresa para PDFs
└── Firmas Correos\    # Imágenes de firma de correo
                       # Nombre: email_at_dominio.com.png (@ → _)
```

---

## 17. Compilar y Ejecutar

```bash
# Restaurar dependencias
dotnet restore

# Compilar (SIEMPRE Platform=x64)
dotnet build -p:Platform=x64

# Ejecutar en Visual Studio con perfil "Advance Control (Package)"
```

## 18. Instalador autoactualizable del cliente

El cliente queda preparado para distribuirse como `MSIX` con `App Installer`.

### Flujo esperado

1. Cada push a `main` dispara `.github/workflows/publish-client-installer.yml`
2. El workflow compila, prueba y empaqueta el cliente en `x64`
3. El workflow firma el `MSIX` con un certificado `.pfx`
4. Se publica un release con dos artefactos estables:
   - `AdvanceControl.appinstaller`
   - `AdvanceControl-x64.msix`
5. Las instalaciones hechas desde `AdvanceControl.appinstaller` revisan actualizaciones en cada lanzamiento

### Secretos requeridos en GitHub

| Secreto | Descripción |
|---|---|
| `WINDOWS_PFX_BASE64` | Certificado `.pfx` codificado en Base64 |
| `WINDOWS_PFX_PASSWORD` | Contraseña del certificado |

### Publicación

El workflow publica usando la URL estable:

`https://github.com/<owner>/<repo>/releases/latest/download/AdvanceControl.appinstaller`

El archivo `.appinstaller` apunta a:

`https://github.com/<owner>/<repo>/releases/latest/download/AdvanceControl-x64.msix`

### Script de empaquetado local/CI

Para generar el instalador manualmente en una máquina con Visual Studio Build Tools / MSBuild:

```powershell
.\build\Publish-ClientInstaller.ps1 `
  -Version 1.0.0.1 `
  -AppInstallerBaseUri "https://github.com/<owner>/<repo>/releases/latest/download" `
  -CertificatePath "C:\ruta\certificado.pfx" `
  -CertificatePassword "<password>"
```

Si solo quieres validar el empaquetado sin firma, puedes omitir `CertificatePath` y `CertificatePassword`; el `.msix` se generará, pero no será apto para distribución final.

### Instalacion local en una PC de pruebas

Si al abrir el `.appinstaller` o el `.msix` aparece el error `0x800B0109`, falta confiar el certificado de firma en la maquina.

1. Genera el instalador localmente.
2. Ejecuta el instalador local de un solo paso:

```cmd
build\Install-LocalInstaller.cmd
```

o, si prefieres PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build\Install-LocalInstaller.ps1
```

Este script:

1. Se eleva automaticamente con UAC cuando hace falta.
2. Exporta y confia el certificado del paquete en `CurrentUser` y `LocalMachine`.
3. Inicia la instalacion desde `artifacts\installer\AdvanceControl.appinstaller`.

El script exporta el certificado del paquete a `artifacts\installer\AdvanceControl-signing.cer` e importa ese certificado en `CurrentUser` y `LocalMachine` para que la instalacion MSIX sea confiable en la misma PC.

### Flujo temporal sin firma

Mientras no exista certificado de firma, el repositorio tambien incluye un fallback temporal:

- Workflow: `.github/workflows/publish-client-portable.yml`
- Script: `build/Publish-ClientPortable.ps1`
- Artefacto: `AdvanceControl-portable-x64.zip`

Este flujo sirve para pruebas internas y despliegue manual del cliente, pero **no reemplaza** el flujo `MSIX + App Installer` porque no ofrece instalacion firmada ni autoactualizacion.

La referencia completa de esta decision temporal esta en:

`docs/implementacion-temporal-sin-firma.md`
