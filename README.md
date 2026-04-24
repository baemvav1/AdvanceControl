# AdvanceControl — Documentación Técnica (Cliente WinUI 3)

> **Versión del stack:** .NET 8 · WinUI 3 (Windows App SDK 1.8) · MVVM estricto  
> **Plataforma:** Windows (x64, x86, ARM64)  
> **API objetivo por defecto:** `https://localhost:7055`  
> **Tests:** xUnit 2.9 + Moq 4.20

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
├── Models/                    # DTOs del cliente (~130 clases)
├── Navigation/                # Servicio de navegación entre páginas (Frame)
├── Rules/                     # Reglas de negocio (conciliación bancaria)
├── Services/                  # Servicios organizados por carpeta de dominio (~48 categorías)
├── Settings/                  # Clases de opciones de configuración
├── Utilities/                 # Helpers de uso general
├── ViewModels/                # Un ViewModel por View (~28 ViewModels)
└── Views/
    ├── Details/               # Vistas de detalle (estado de cuenta)
    ├── Dialogs/               # ContentDialogs y UserControls
    ├── Items/                 # ItemTemplates para listas (administración, conciliación, etc.)
    ├── Login/                 # Pantalla de login
    ├── Pages/                 # Páginas del menú principal (~24 páginas)
    ├── Viewers/               # Visor reutilizable de imágenes
    ├── Windows/               # Ventanas secundarias (factura, operación, usuario, conciliación)
    └── MainWindow.xaml        # Ventana raíz con NavigationView

Advance Control.Tests/         # Proyecto de pruebas unitarias (xUnit + Moq)
├── Converters/                # Tests de convertidores XAML
├── Services/                  # Tests de servicios
└── ViewModels/                # Tests de ViewModels

build/                         # Scripts PowerShell para empaquetado MSIX y distribución
installer/                     # Certificados, scripts de instalación y LEEME
docs/                          # Documentación técnica adicional
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
| `ISecureStorage` | Windows PasswordVault (almacén de secretos) |
| `AuthenticatedHttpHandler` | Inyecta Bearer token en requests HTTP |
| `IOnlineCheck` | Verifica conectividad con la API |
| `INavigationService` | Navegación entre páginas (Frame) con registro de rutas |
| `INotificacionService` | Muestra ContentDialogs de alerta/confirmación |
| `ILoggingService` | Envía logs a la API |
| `IActivityService` | Registra actividad del usuario |
| `IImageViewerService` | Abre imágenes en visor flotante |
| `IEmailService` | Envío de correos SMTP (Hostinger/MailKit) |
| `IDialogService` | Gestión centralizada de diálogos modales |
| `IThemeService` | Persistencia y cambio de tema (Claro/Oscuro/Sistema) |
| `IUserSessionService` | Datos de sesión del usuario actual |
| `IAccessControlService` | Control de acceso local (singleton) |
| `INotificacionAlertaService` | Alertas push por credencial |

### Servicios de almacenamiento local de imágenes
| Servicio | Descripción |
|---|---|
| `ICargoImageService` | Imágenes de cargos (local) |
| `ILevantamientoImageService` | Imágenes de levantamientos (local) |
| `IOperacionImageService` | Imágenes de operaciones (local) |

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
| `IRelacionUsuarioAreaService` | `/api/RelacionUsuarioArea` |
| `IEntidadService` | `/api/Entidad` |
| `INivelService` | `/api/Nivel` |
| `IUserInfoService` | `/api/UserInfo` |
| `IGoogleMapsConfigService` | `/api/GoogleMaps` |
| `IDashboardService` | `/api/Dashboard` (conteos) |
| `IFacturaService` | `/api/factura` (CRUD, abonos, conciliación) |
| `IEstadoCuentaXmlService` | Parseo de estados de cuenta bancarios (XML) |
| `ILevantamientoApiService` | `/api/Levantamiento` |
| `IPermisoUiService` | `/api/PermisosUi` (catálogo, sincronización, niveles) |
| `IUsuarioAdminService` | `/api/UsuariosAdmin` |
| `ITipoUsuarioService` | `/api/TipoUsuario` |
| `ICorreoUsuarioService` | `/api/CorreoUsuario` |
| `IDevOpsService` | `/api/DevOps` (estadísticas, limpieza de módulos) |
| `IReporteFinancieroFacturacionService` | `/api/reportefinancierofacturacion` |
| `IReporteFinancieroFacturacionExportService` | Exportación de reportes financieros |
| `ILevantamientoReportService` | Generación de reportes de levantamiento |
| `IQuoteService` | Local — genera PDFs (cotización/reporte) |

### Motor de conciliación (lógica local)
| Servicio | Descripción |
|---|---|
| `ConciliacionMatchingEngine` | Motor de conciliación automática (sin API) |
| `IConciliacionRulesProvider` | Proveedor de reglas de conciliación configurables |

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

| ViewModel | Page / Ventana |
|---|---|
| `LoginViewModel` | Login |
| `MainViewModel` | MainWindow (NavigationView) |
| `DashboardViewModel` | DashboardPage |
| `OperacionesViewModel` | OperacionesPage |
| `CustomersViewModel` | ClientesPage |
| `ContactosViewModel` | ContactosPage |
| `EquiposViewModel` | EquiposPage |
| `MttoViewModel` | MantenimientoPage |
| `ProveedoresViewModel` | ProveedoresPage |
| `RefaccionesViewModel` | RefaccionPage |
| `ServiciosViewModel` | ServiciosPage |
| `UbicacionesViewModel` | UbicacionesPage |
| `AreasViewModel` | AreasPage |
| `EntidadesViewModel` | EntidadesPage |
| `CorreoViewModel` | CorreoPage |
| `EsCuentaViewModel` | EstadoCuentaPage |
| `AcesoriaViewModel` | AsesoriaPage |
| `NuevoEquipoViewModel` | NuevoEquipoUserControl |
| `FacturasViewModel` | FacturasPage |
| `DetailFacturaViewModel` | DetailFacturaWindow |
| `DetailEstadoCuentaViewModel` | DetailEstadoCuentaView |
| `ConciliacionViewModel` | ConciliacionPage |
| `ConciliacionAutomaticaWindowViewModel` | ConfirmacionConciliacionWindow |
| `LevantamientosViewModel` | LevantamientosView |
| `LevantamientoViewModel` | LevantamientoView |
| `DevOpsViewModel` | DevOpsPage |
| `UsuariosAdminViewModel` | AdministracionPage |
| `RPTFinancieroFacturacionViewModel` | ReporteFinancieroFacturacionPage |

---

## 8. Páginas (Views/Pages)

| Page | Función |
|---|---|
| `DashboardPage` | Vista principal / resumen con conteos |
| `OperacionesPage` | Gestión de operaciones (núcleo del sistema) |
| `ClientesPage` | CRUD de clientes |
| `ContactosPage` | CRUD de contactos |
| `EquiposPage` | CRUD de equipos |
| `MantenimientoPage` | Mantenimientos pendientes → convertir en operación |
| `ProveedoresPage` | CRUD de proveedores |
| `RefaccionPage` | CRUD de refacciones |
| `ServiciosPage` | CRUD de servicios |
| `UbicacionesPage` | Gestión de ubicaciones con mapa Google Maps |
| `AreasPage` | Áreas geográficas (polígonos, círculos) con mapa |
| `EntidadesPage` | Datos de la empresa (para cabeceras de PDFs) |
| `CorreoPage` | Configuración de correo SMTP + firma de correo |
| `EstadoCuentaPage` | Estado de cuenta bancario (carga XML, movimientos) |
| `AsesoriaPage` | Vista de asesoría |
| `FacturasPage` | Gestión de facturas CFDI (carga XML, conceptos, abonos) |
| `ConciliacionPage` | Conciliación bancaria automática y manual |
| `LevantamientosView` | Lista de levantamientos / auditorías de campo |
| `LevantamientoView` | Detalle de un levantamiento (hotspots, imágenes, árbol) |
| `AdministracionPage` | Administración de usuarios, seguridad y permisos UI |
| `ReporteFinancieroFacturacionPage` | Reporte financiero de facturación |
| `DevOpsPage` | Estadísticas y herramientas de limpieza del sistema |
| `SettingsPage` | Configuración de apariencia (tema claro/oscuro/sistema) |

---

## 9. Dialogs, Ventanas Secundarias e Item Templates

### Dialogs y UserControls (Views/Dialogs)

| Dialog/Control | Uso |
|---|---|
| `CotizacionVisorDialog` | Visor WebView2 de PDF. Parámetro `tipo="Cotización"/"Reporte"`. Botones: Enviar por correo / Abrir externo / Cerrar |
| `EnviarCotizacionDialog` | Composición y envío de correo con PDF adjunto. Parámetro `tipo`. Soporta Para, CC (checkboxes de contactos), CC manual, CCO |
| `NuevoClienteUserControl` | Formulario de alta de cliente |
| `NuevoMantenimientoUserControl` | Alta de mantenimiento |
| `NuevoEquipoUserControl` | Alta de equipo |
| `AgregarCargoUserControl` | Agrega cargo (servicio/refacción/proveedor) a operación |
| `SeleccionarClienteUserControl` | Picker de cliente |
| `SeleccionarEquipoUserControl` | Picker de equipo |
| `SeleccionarServicioUserControl` | Picker de servicio |
| `SeleccionarRefaccionUserControl` | Picker de refacción |
| `SeleccionarUbicacionUserControl` | Picker de ubicación |
| `ImageViewerUserControl` | Visor de imágenes embebido en diálogos |
| `RefaccionesViewerUserControl` | Lista de refacciones de un equipo |
| `ConfirmacionConciliacionUserControl` | Confirmación de match de conciliación |
| `NormalizarFacturaUsdDialog` | Normalización de montos de facturas en USD |

### Ventanas secundarias (Views/Windows)

| Ventana | Uso |
|---|---|
| `DetailFacturaWindow` | Detalle completo de una factura (conceptos, abonos, registro de pagos) |
| `OperacionVisorWindow` | Visor expandido de operación con todos sus documentos y cargos |
| `UsuarioEditorWindow` | Crear/editar usuario admin (acceso, contacto, asignaciones, correo) |
| `ConfirmacionConciliacionWindow` | Confirmación de resultados de conciliación automática |

### Vistas de detalle (Views/Details)

| Vista | Uso |
|---|---|
| `DetailEstadoCuentaView` | Detalle de estado de cuenta bancario con movimientos |

### Visor de imágenes (Views/Viewers)

| Vista | Uso |
|---|---|
| `ViewerImagenes` | Visor reutilizable de imágenes con zoom/pan hospedado en ventana independiente |

### Item Templates (Views/Items)

| Categoría | Item Template | Uso |
|---|---|---|
| Raíz | `MovimientoItemView` | Movimiento de estado de cuenta bancario |
| Administración | `UsuarioAdminItemView` | Tarjeta de usuario con acciones editar/eliminar |
| Administración | `PermisoModuloItemView` | Permiso a nivel de módulo |
| Administración | `PermisoAccionItemView` | Permiso a nivel de acción |
| Conciliación | `ConciliacionFacturaItemView` | Factura en vista de conciliación |
| Conciliación | `ConciliacionMovimientoItemView` | Movimiento bancario en vista de conciliación |
| Levantamiento | `LevantamientoTreeItemView` | Nodo del árbol de levantamiento |
| RPT Facturas | `ReporteFacturacionCabeceraItemView` | Cabecera de reporte financiero |
| RPT Facturas | `ReporteFacturacionDetalleItemView` | Detalle de reporte financiero |

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

## 12. Sistema de Facturas (CFDI)

El módulo de facturas permite cargar, visualizar y gestionar facturas CFDI (Comprobantes Fiscales Digitales por Internet).

### Flujo principal
```
Carga de XML CFDI → Parseo → Guardado en API
    │
    ▼
FacturasPage (lista de facturas)
    ├── Detalle (DetailFacturaWindow)
    │   ├── Conceptos (líneas de factura)
    │   ├── Abonos registrados
    │   └── Registrar nuevo abono
    ├── Normalizar USD (NormalizarFacturaUsdDialog)
    └── Conciliación bancaria
```

### Servicios involucrados
- `IFacturaService` — CRUD de facturas, registro de abonos, conciliación
- `IEstadoCuentaXmlService` — Parseo de estados de cuenta bancarios (XML bancario)

---

## 13. Sistema de Conciliación Bancaria

La conciliación bancaria permite cruzar automáticamente facturas CFDI con movimientos bancarios del estado de cuenta.

### Modos de conciliación
| Modo | Descripción |
|---|---|
| Uno a uno | Match directo entre una factura y un movimiento |
| Combinacional | Match de un movimiento contra múltiples facturas |
| Abonos | Match de múltiples movimientos contra una factura |

### Motor de reglas (`Rules/ConciliacionRules.cs`)
```csharp
ConciliacionUnoAUnoRules        // Reglas 1:1 (saldo pendiente, sin abonos previos, no finiquitada)
ConciliacionCombinacionalRules  // Reglas N:1 (mínimo facturas por grupo)
ConciliacionAbonosRules         // Reglas 1:N (máximo candidatos, mínimo por combinación)
ConciliacionMetodoPagoRules     // PUE vs PPD, meses posteriores para pago diferido
```

### Servicios involucrados
- `ConciliacionMatchingEngine` — Motor de matching local (sin API)
- `IConciliacionRulesProvider` — Proveedor de reglas configurables
- `IFacturaService` — Endpoints de conciliación (`inicializar-bitacora`, `deshacer-ultimo`, `deshacer-todo`)

### Flujo
1. Cargar estado de cuenta (XML bancario) → `IEstadoCuentaXmlService`
2. Cargar facturas del periodo → `IFacturaService`
3. Ejecutar conciliación automática → `ConciliacionMatchingEngine`
4. Confirmar matches → `ConfirmacionConciliacionWindow`
5. Registrar abonos → `IFacturaService.RegistrarAbono`

---

## 14. Sistema de Levantamientos

Los levantamientos son auditorías/inspecciones de campo con estructura de árbol, imágenes y hotspots.

### Estructura
- `LevantamientosView` — Lista de todos los levantamientos
- `LevantamientoView` — Detalle de un levantamiento con árbol jerárquico
- `LevantamientoTreeItemView` — Nodo del árbol con imágenes y hotspots

### Servicios involucrados
- `ILevantamientoApiService` — CRUD de levantamientos (`/api/Levantamiento`)
- `ILevantamientoImageService` — Almacenamiento local de imágenes
- `ILevantamientoReportService` — Generación de reportes de levantamiento

---

## 15. Administración de Usuarios y Permisos

### AdministracionPage
Página central de administración con pestañas:
1. **Usuarios** — Lista, crear, editar y desactivar usuarios admin
2. **Permisos** — Gestión de permisos por módulo y acción

### Sistema de permisos UI (`PermisosUi`)
El sistema escanea la UI en runtime y controla visibilidad/habilitación de controles según el nivel del usuario.

| Servicio | Descripción |
|---|---|
| `IPermisoUiService` | CRUD de permisos en API (`/api/PermisosUi`) |
| `IPermisoUiRuntimeService` | Evaluación de permisos en runtime |
| `IPermisoUiScanner` | Escaneo automático de módulos y acciones de la UI |
| `PermisoUiKeyBuilder` | Generación de claves únicas para permisos |
| `PermisoUiVisualBinder` | Aplicación visual de permisos en controles XAML |

### Editor de usuarios (`UsuarioEditorWindow`)
Ventana con 4 secciones:
1. **Acceso** — Login, contraseña, nivel de acceso
2. **Vinculación de contacto** — Asociar usuario con contacto existente
3. **Datos de contacto y asignaciones** — Áreas y equipos asignados
4. **Correo** — Configuración de correo del usuario

---

## 16. Dashboard y DevOps

### DashboardPage
Vista principal con conteos generales del sistema vía `IDashboardService` (`/api/Dashboard/conteos`).

### DevOpsPage
Herramientas de administración técnica:
- **Estadísticas** del sistema (`/api/DevOps/estadisticas`)
- **Limpieza de módulos** — permite limpiar datos por módulo (financiero, operaciones, mantenimiento, levantamientos, servicios, logs, ubicaciones)

---

## 17. Converters XAML Disponibles

| Converter | ResourceKey | Uso |
|---|---|---|
| `BooleanToVisibilityConverter` | `BooleanToVisibilityConverter` | `bool → Visibility` |
| `BooleanToCornerRadiusConverter` | `BooleanToCornerRadiusConverter` | Esquinas según expand |
| `BooleanToExpandTextConverter` | `BooleanToExpandTextConverter` | Texto del botón expandir |
| `BooleanToArrowConverter` | `BooleanToArrowConverter` | `bool → dirección de flecha` |
| `BooleanToGridBrushConverter` | `BoolToColorBrushConverter` | `bool → Brush (param: 'Color1\|Color2')` para grids |
| `NullToVisibilityConverter` | `NullToVisibilityConverter` | `null → Collapsed` |
| `NullToBoolConverter` | `NullToBoolConverter` | `null → false`, `non-null → true` |
| `CurrencyConverter` | `CurrencyConverter` | `float → "$X,XXX.XX"` |
| `AccessLevelConverter` | `AccessLevelConverter` | `nivel >= param → IsEnabled` |
| `RefaccionVisibilityConverter` | `RefaccionVisibilityConverter` | Visibility según tipo de cargo |
| `CargoTypeToVisibilityConverter` | `CargoTypeToVisibilityConverter` | Visibility según tipo de cargo |
| `BoolNegationConverter` | — | Negación bool |
| `NullableNumberToStringConverter` | — | Nullable numérico a string |
| `DateTimeFormatConverter` | — | Formateo de fechas |
| `LevelToBackgroundConverter` | — | Colores de fondo por nivel de log |
| `LevelToForegroundConverter` | — | Colores de texto por nivel de log |
| `CargoDtoJsonConverter` | — | Deserialización personalizada de `CargoDto` (JSON) |

---

## 18. Modelos del Cliente (Models/)

Los DTOs del cliente **no son los mismos** que los del servidor. Tienen propiedades adicionales para el estado de UI. El proyecto contiene ~130 clases de modelo organizadas por dominio.

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

### Modelos de facturas y finanzas
| Modelo | Descripción |
|---|---|
| `FacturaResumenDto` | Resumen de factura CFDI |
| `FacturaDetalleDto` | Detalle completo de factura con conceptos y abonos |
| `FacturaConceptoDto` | Línea de concepto de factura |
| `FacturaTrasladoDto` | Traslado/impuesto de factura |
| `AbonoFacturaDto` | Abono/pago registrado contra factura |
| `GuardarFacturaRequestDto` / `ResponseDto` | Request/response para guardar factura |
| `RegistrarAbonoFacturaRequestDto` / `ResponseDto` | Request/response para registrar abono |

### Modelos de estado de cuenta bancario
| Modelo | Descripción |
|---|---|
| `EstadoCuentaBancario` | Estado de cuenta completo (parseado de XML) |
| `EstadoCuentaResumenDto` / `DetalleDto` | Resumen y detalle de estado de cuenta |
| `MovimientoBancario` | Movimiento individual |
| `MovimientoEstadoCuentaDto` | Movimiento con relaciones |
| `DepositoBancario`, `ComisionBancaria`, `TransferenciaSPEI`, etc. | Tipos específicos de movimientos |

### Modelos de conciliación
| Modelo | Descripción |
|---|---|
| `ConciliacionMovimientoResumenDto` | Resumen de movimiento para conciliación |
| `ConciliacionMatchPropuestaDto` | Propuesta de match automático |
| `ConciliacionAutomaticaRequestDto` / `ResponseDto` | Request/response de conciliación automática |
| `BitacoraConciliacionResponseDto` | Bitácora/auditoría de conciliación |

### Modelos de levantamiento
| Modelo | Descripción |
|---|---|
| `LevantamientoTreeItemModel` | Nodo del árbol de levantamiento |
| `LevantamientoImageItem` | Imagen asociada a un nodo |
| `LevantamientoHotspotItem` | Hotspot de imagen |
| `LevantamientoReporteDto` | Datos para reporte de levantamiento |

### Otros modelos relevantes
| Modelo | Descripción |
|---|---|
| `ApiResponse<T>` | Wrapper genérico de respuesta de API |
| `DashboardConteoDto` | Conteos para el dashboard |
| `PermisosUiDto` | Permisos de interfaz por módulo/acción |
| `NotificacionAlerta` | Alerta del sistema |
| `RelacionUsuarioAreaDto` | Relación usuario-área |
| `TipoUsuarioDto` | Tipo de usuario |
| `UsuarioAdminDto` | Usuario administrador |
| `GoogleMapsConfigDto` | Configuración de Google Maps |

---

## 19. Patrón para Implementar una Pantalla Nueva

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

## 20. Restricciones Técnicas Conocidas

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

## 21. Archivos Locales de la Aplicación

```
Documents\Advance Control\
├── Cotizaciones\      # PDFs de cotizaciones generadas
├── Reportes\          # PDFs de reportes generados
├── Cabeceras\         # Imágenes de cabecera de empresa para PDFs
└── Firmas Correos\    # Imágenes de firma de correo
                       # Nombre: email_at_dominio.com.png (@ → _)
```

---

## 22. Compilar, Ejecutar y Probar

```bash
# Restaurar dependencias
dotnet restore

# Compilar (SIEMPRE Platform=x64)
dotnet build -p:Platform=x64

# Ejecutar pruebas unitarias
dotnet test -p:Platform=x64

# Ejecutar en Visual Studio con perfil "Advance Control (Package)"
```

### Proyecto de pruebas (`Advance Control.Tests`)

- **Framework:** xUnit 2.9.2 + Moq 4.20.72
- **Cobertura:** coverlet.collector 6.0.2
- **Target:** net8.0-windows10.0.19041.0

| Área | Tests |
|---|---|
| Converters | `BooleanToArrowConverterTests`, `BooleanToCornerRadiusConverterTests`, `CurrencyConverterTests`, `DateTimeFormatConverterTests`, `LevelToForegroundConverterTests`, `NullToVisibilityConverterTests`, `PriorityToBackgroundConverterTests` |
| Services | `AuthServiceTests`, `MantenimientoServiceTests`, `UserInfoServiceTests`, `NotificacionServiceTests`, `ConciliacionMatchingEngineTests` |
| ViewModels | `LoginViewModelTests`, `MainViewModelTests`, `CustomersViewModelTests`, `EntidadesViewModelTests`, `EsCuentaViewModelNamespaceTests`, `EsCuentaViewModelParsingTests` |

## 23. Instalador autoactualizable del cliente

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
