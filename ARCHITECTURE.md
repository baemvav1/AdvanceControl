# Arquitectura - Advance Control

## Visión General

Advance Control es una aplicación de escritorio WinUI 3 que sigue el patrón **MVVM (Model-View-ViewModel)** para proporcionar una clara separación de responsabilidades entre la interfaz de usuario y la lógica de negocio.

## Patrones y Principios

### MVVM (Model-View-ViewModel)

```
┌─────────────────────────────────────────────────┐
│                    View                         │
│  (XAML - CustomersView, MainWindow)             │
│                                                 │
│  • Interfaz de usuario                          │
│  • Data binding con ViewModels                  │
│  • Sin lógica de negocio                        │
└─────────────────┬───────────────────────────────┘
                  │
                  │ Data Binding
                  │
┌─────────────────▼───────────────────────────────┐
│                ViewModel                        │
│  (CustomersViewModel, MainViewModel)            │
│                                                 │
│  • Lógica de presentación                       │
│  • Propiedades observables                      │
│  • Comandos para interacciones del usuario      │
│  • Orquesta servicios y modelos                 │
└─────────────────┬───────────────────────────────┘
                  │
                  │ Usa servicios
                  │
┌─────────────────▼───────────────────────────────┐
│              Services / Models                  │
│  (AuthService, OnlineCheck, DTOs)               │
│                                                 │
│  • Lógica de negocio                            │
│  • Acceso a datos                               │
│  • Comunicación con API                         │
└─────────────────────────────────────────────────┘
```

### Inyección de Dependencias

La aplicación utiliza `Microsoft.Extensions.DependencyInjection` para:
- Gestión de ciclo de vida de servicios
- Inversión de control
- Facilitar pruebas unitarias con mocks

## Capas de la Aplicación

### 1. Capa de Presentación (Views + ViewModels)

**Views (XAML)**
- `MainWindow.xaml` - Ventana principal de la aplicación
- `CustomersView.xaml` - Vista de gestión de clientes

**ViewModels**
- `ViewModelBase` - Clase base con INotifyPropertyChanged
- `MainViewModel` - ViewModel de la ventana principal
- `CustomersViewModel` - ViewModel para gestión de clientes

**Converters**
- `BooleanToVisibilityConverter` - Convierte bool a Visibility para XAML

### 2. Capa de Servicios

#### Autenticación (`Services/Auth`)
- `IAuthService` - Interfaz para autenticación
- `AuthService` - Implementación de autenticación con API
- `AuthServiceStub` - Implementación stub para desarrollo/pruebas

#### Seguridad (`Services/Security`)
- `ISecretStorage` - Interfaz para almacenamiento seguro
- `SecretStorageWindows` - Implementación usando Windows Credential Manager

#### HTTP (`Services/Http`)
- `AuthenticatedHttpHandler` - DelegatingHandler que añade JWT a peticiones

#### Conectividad (`Services/OnlineCheck`)
- `IOnlineCheck` - Interfaz para verificación de conectividad
- `OnlineCheck` - Implementación que verifica disponibilidad de API
- `OnlineCheckResult` - Resultado de la verificación

#### Endpoints (`Services/EndPointProvider`)
- `IApiEndpointProvider` - Interfaz para construcción de URLs
- `ApiEndpointProvider` - Implementación que combina BaseUrl con rutas
- `ExternalApiOptions` - Opciones de configuración de API

### 3. Capa de Modelos

**DTOs (Data Transfer Objects)**
- `CustomerDto` - Modelo de datos de cliente
- `TokenDto` - Modelo de token de autenticación

### 4. Navegación

- `INavigationService` - Servicio para navegación entre vistas

### 5. Helpers y Utilidades

- `JwtUtils` - Utilidades para decodificar y validar tokens JWT

### 6. Configuración

- `ClientSettings` - Configuración del cliente
- `appsettings.json` - Archivo de configuración externa

## Flujo de Datos

### Flujo de Autenticación

```
Usuario → View → ViewModel → AuthService → API
                    ↓
              SecretStorage (guardar token)
                    ↓
              AuthenticatedHttpHandler (usar token en peticiones)
```

### Flujo de Peticiones HTTP

```
ViewModel → Service → AuthenticatedHttpHandler → API
                            ↓
                    Añade JWT Header
                            ↓
                       HttpClient
```

### Verificación de Conectividad

```
App Startup → OnlineCheck.CheckAsync()
                    ↓
            ApiEndpointProvider.GetEndpoint("Online")
                    ↓
            HttpClient → HEAD/GET request
                    ↓
            OnlineCheckResult
```

## Configuración y Bootstrapping

La aplicación se inicializa en `App.xaml.cs`:

1. Carga configuración desde `appsettings.json`
2. Configura servicios en el contenedor de DI
3. Registra ViewModels y servicios
4. Configura HttpClient con handlers personalizados
5. Crea y muestra MainWindow

## Comunicación con API Externa

### Configuración

La URL base se configura en `appsettings.json`:

```json
{
  "ExternalApi": {
    "BaseUrl": "https://api.example.com/",
    "ApiKey": "optional-api-key"
  }
}
```

### Construcción de URLs

El `ApiEndpointProvider` combina la BaseUrl con rutas relativas:

```csharp
// Configurado: BaseUrl = "https://api.example.com/"
var endpoint = _endpointProvider.GetEndpoint("customers", "123");
// Resultado: "https://api.example.com/customers/123"
```

### Autenticación

Las peticiones incluyen JWT en header Authorization:

```
Authorization: Bearer <jwt-token>
```

## Seguridad

### Almacenamiento de Tokens

Los tokens JWT se almacenan de forma segura usando:
- **Windows Credential Manager** (producción)
- Encriptación a nivel de sistema operativo
- No se almacenan en texto plano

### Validación de Tokens

`JwtUtils` proporciona:
- Decodificación de tokens
- Validación de firma
- Verificación de expiración

## Manejo de Errores

### Niveles de Manejo

1. **Servicios**: Capturan excepciones y devuelven resultados estructurados
2. **ViewModels**: Procesan resultados y actualizan UI
3. **Views**: Muestran mensajes al usuario

### Ejemplo: OnlineCheck

```csharp
try {
    // Intenta conexión
} catch (OperationCanceledException) {
    return OnlineCheckResult.FromException("Operation cancelled");
} catch (Exception ex) {
    return OnlineCheckResult.FromException(ex.Message);
}
```

## Testing

### Estrategia de Pruebas

- **Unit Tests**: Para servicios y ViewModels
- **Integration Tests**: Para flujos completos
- **Stubs**: Para desarrollo sin API (`AuthServiceStub`)

### Interfaces para Testing

Todas las dependencias usan interfaces para facilitar mocking:
- `IAuthService`
- `IOnlineCheck`
- `IApiEndpointProvider`
- `ISecretStorage`
- `INavigationService`

## Extensibilidad

### Añadir un Nuevo Servicio

1. Crear interfaz `IMyService`
2. Implementar clase `MyService`
3. Registrar en DI container (App.xaml.cs)
4. Inyectar en ViewModels que lo necesiten

### Añadir una Nueva Vista

1. Crear `MyView.xaml` + `MyView.xaml.cs`
2. Crear `MyViewModel` heredando de `ViewModelBase`
3. Registrar ViewModel en DI
4. Configurar navegación en `INavigationService`

## Mejores Prácticas

1. **Separación de Responsabilidades**: Cada clase tiene una única responsabilidad
2. **Dependency Injection**: Usar DI para todas las dependencias
3. **Async/Await**: Operaciones asíncronas para no bloquear UI
4. **CancellationToken**: Permitir cancelación de operaciones largas
5. **ConfigureAwait(false)**: En servicios que no requieren sincronización con UI
6. **Dispose Pattern**: Implementar IDisposable donde sea necesario
7. **XML Documentation**: Documentar APIs públicas
8. **Nullable Reference Types**: Habilitado en proyecto (.csproj)

## Rendimiento

### Optimizaciones

- **HttpClient reutilizable**: Registrado como singleton en DI
- **HEAD requests**: Para verificación de conectividad (más ligero que GET)
- **ResponseHeadersRead**: Para operaciones que solo necesitan headers
- **ConfigureAwait(false)**: Evita overhead de sincronización innecesaria

## Diagrama de Componentes

```
┌─────────────────────────────────────────────────────┐
│                   Presentation                      │
│  ┌──────────┐  ┌──────────┐  ┌───────────────┐     │
│  │MainWindow│  │Customers │  │ Converters    │     │
│  │  .xaml   │  │View.xaml │  │               │     │
│  └────┬─────┘  └────┬─────┘  └───────────────┘     │
│       │             │                               │
│  ┌────▼─────┐  ┌───▼──────┐  ┌───────────────┐     │
│  │  Main    │  │Customers │  │  ViewModelBase│     │
│  │ViewModel │  │ViewModel │  │               │     │
│  └────┬─────┘  └────┬─────┘  └───────────────┘     │
└───────┼─────────────┼───────────────────────────────┘
        │             │
┌───────▼─────────────▼───────────────────────────────┐
│                    Services                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │  Auth    │  │ Online   │  │ ApiEndpoint      │  │
│  │ Service  │  │  Check   │  │   Provider       │  │
│  └────┬─────┘  └────┬─────┘  └─────┬────────────┘  │
│       │             │               │               │
│  ┌────▼─────┐  ┌───▼──────┐  ┌─────▼────────────┐  │
│  │ Secret   │  │HttpClient│  │ ExternalApi      │  │
│  │ Storage  │  │          │  │   Options        │  │
│  └──────────┘  └──────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────┘
        │
┌───────▼─────────────────────────────────────────────┐
│                  External API                       │
│              (REST API con JWT)                     │
└─────────────────────────────────────────────────────┘
```

## Estado Actual vs. Objetivo

### Implementado ✅
- Estructura base MVVM
- OnlineCheck service completo
- ApiEndpointProvider completo
- Converters para XAML
- Configuración con appsettings.json

### Pendiente 🚧
- Implementación completa de autenticación
- Almacenamiento seguro de credenciales
- ViewModels con lógica de negocio
- Navegación entre vistas
- Manejo de JWT
- DTOs con propiedades

### Próximos Pasos
1. Implementar `IAuthService` y `AuthService`
2. Implementar `ISecretStorage` para Windows
3. Crear `AuthenticatedHttpHandler`
4. Definir DTOs (CustomerDto, TokenDto)
5. Implementar ViewModels base
6. Configurar navegación
