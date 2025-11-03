# Lista de Archivos Vacíos

Este documento proporciona una lista rápida de todos los archivos que están creados pero solo contienen declaraciones de clase sin funcionalidad.

## Archivos Vacíos (15 total)

### Autenticación (3)
- ✗ `Advance Control/Services/Auth/IAuthService.cs` - Interfaz de servicio de autenticación
- ✗ `Advance Control/Services/Auth/AuthService.cs` - Implementación de autenticación
- ✗ `Advance Control/Services/Auth/AuthServiceStub.cs` - Stub de autenticación para testing

### Seguridad (2)
- ✗ `Advance Control/Services/Security/ISecretStorage.cs` - Interfaz de almacenamiento seguro
- ✗ `Advance Control/Services/Security/SecretStorageWindows.cs` - Implementación para Windows

### HTTP (1)
- ✗ `Advance Control/Services/Http/AuthenticatedHttpHandler.cs` - Handler HTTP con JWT

### Modelos (2)
- ✗ `Advance Control/Models/CustomerDto.cs` - Modelo de datos de cliente
- ✗ `Advance Control/Models/TokenDto.cs` - Modelo de token JWT

### Navegación (1)
- ✗ `Advance Control/Navigation/INavigationService.cs` - Interfaz de servicio de navegación

### Helpers (1)
- ✗ `Advance Control/Helpers/JwtUtils.cs` - Utilidades para JWT

### ViewModels (3)
- ✗ `Advance Control/ViewModels/ViewModelBase.cs` - Clase base para ViewModels
- ✗ `Advance Control/ViewModels/MainViewModel.cs` - ViewModel de ventana principal
- ✗ `Advance Control/ViewModels/CustomersViewModel.cs` - ViewModel de gestión de clientes

### Settings (1)
- ✗ `Advance Control/Settings/ClientSettings.cs` - Configuración del cliente

### Archivos Duplicados/A Eliminar (1)
- ⚠️ `Advance Control/Helpers/Converters/BooleanToVisibilityConverter.cs` - DUPLICADO (eliminar)

## Archivos Implementados (7 total)

### OnlineCheck
- ✓ `Advance Control/Services/OnlineCheck/IOnlineCheck.cs`
- ✓ `Advance Control/Services/OnlineCheck/OnlineCheck.cs`
- ✓ `Advance Control/Services/OnlineCheck/OnlineCheckResult.cs`

### EndPointProvider
- ✓ `Advance Control/Services/EndPointProvider/IApiEndpointProvider.cs`
- ✓ `Advance Control/Services/EndPointProvider/ApiEndpointProvider.cs`
- ✓ `Advance Control/Services/EndPointProvider/ExternalApiOptions.cs`

### Converters
- ✓ `Advance Control/Converters/BooleanToVisibilityConverter.cs`

## Estadísticas

- **Total archivos**: 22
- **Archivos implementados**: 7 (31.8%)
- **Archivos vacíos**: 15 (68.2%)
- **Archivos duplicados**: 1

## Prioridad de Implementación

### Alta Prioridad ⭐⭐⭐
1. IAuthService + AuthService
2. ISecretStorage + SecretStorageWindows
3. TokenDto
4. AuthenticatedHttpHandler

### Media Prioridad ⭐⭐
5. ViewModelBase
6. CustomerDto
7. CustomersViewModel
8. MainViewModel

### Baja Prioridad ⭐
9. INavigationService
10. JwtUtils
11. ClientSettings
12. AuthServiceStub

## Notas

- Ver [EMPTY_FILES.md](./EMPTY_FILES.md) para detalles completos y sugerencias de implementación
- Ver [DEVELOPER_GUIDE.md](./DEVELOPER_GUIDE.md) para patrones de desarrollo
- Ver [ARCHITECTURE.md](./ARCHITECTURE.md) para contexto arquitectónico
