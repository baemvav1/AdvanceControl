# Archivos Vacíos - Pendientes de Implementación

Este documento lista todos los archivos que están creados en el proyecto pero que actualmente solo contienen la declaración de clase sin ninguna funcionalidad implementada.

## Resumen

- **Total de archivos vacíos**: 15
- **Archivos con implementación**: 7
- **Porcentaje completado**: ~32%

## Archivos Vacíos por Categoría

### 1. Autenticación (Services/Auth) - 3 archivos

#### `IAuthService.cs`
**Ubicación**: `Advance Control/Services/Auth/IAuthService.cs`

**Estado**: Vacío (debería ser interfaz)

**Propósito**: Definir el contrato para servicios de autenticación

**Implementación sugerida**:
```csharp
public interface IAuthService
{
    /// <summary>
    /// Autentica un usuario con credenciales
    /// </summary>
    Task<TokenDto> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cierra la sesión del usuario actual
    /// </summary>
    Task LogoutAsync();
    
    /// <summary>
    /// Refresca el token de autenticación
    /// </summary>
    Task<TokenDto> RefreshTokenAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica si hay un usuario autenticado
    /// </summary>
    bool IsAuthenticated();
    
    /// <summary>
    /// Obtiene el token actual
    /// </summary>
    Task<string> GetCurrentTokenAsync();
}
```

---

#### `AuthService.cs`
**Ubicación**: `Advance Control/Services/Auth/AuthService.cs`

**Estado**: Vacío

**Propósito**: Implementación real de autenticación contra la API

**Dependencias necesarias**:
- `HttpClient` - Para llamadas HTTP
- `IApiEndpointProvider` - Para construir URLs
- `ISecretStorage` - Para almacenar tokens
- `ILogger<AuthService>` - Para logging

**Funcionalidad requerida**:
- Login con POST a `/auth/login`
- Almacenar token JWT recibido
- Logout (limpiar token almacenado)
- Refresh token
- Validar expiración de token

---

#### `AuthServiceStub.cs`
**Ubicación**: `Advance Control/Services/Auth/AuthServiceStub.cs`

**Estado**: Vacío

**Propósito**: Implementación stub para desarrollo sin API real

**Funcionalidad requerida**:
- Simular login exitoso con token hardcodeado
- Permitir desarrollo sin API
- Útil para pruebas de UI

---

### 2. Seguridad (Services/Security) - 2 archivos

#### `ISecretStorage.cs`
**Ubicación**: `Advance Control/Services/Security/ISecretStorage.cs`

**Estado**: Vacío (debería ser interfaz)

**Propósito**: Interfaz para almacenamiento seguro de secretos

**Implementación sugerida**:
```csharp
public interface ISecretStorage
{
    /// <summary>
    /// Almacena un secreto de forma segura
    /// </summary>
    Task SaveAsync(string key, string value);
    
    /// <summary>
    /// Recupera un secreto almacenado
    /// </summary>
    Task<string> GetAsync(string key);
    
    /// <summary>
    /// Elimina un secreto
    /// </summary>
    Task DeleteAsync(string key);
    
    /// <summary>
    /// Verifica si existe un secreto
    /// </summary>
    Task<bool> ExistsAsync(string key);
}
```

---

#### `SecretStorageWindows.cs`
**Ubicación**: `Advance Control/Services/Security/SecretStorageWindows.cs`

**Estado**: Vacío

**Propósito**: Implementación usando Windows Credential Manager

**Tecnología a usar**:
- `Windows.Security.Credentials.PasswordVault` (WinRT)
- O `System.Security.Cryptography` con DPAPI

**Funcionalidad requerida**:
- Guardar tokens JWT de forma segura
- Recuperar tokens al iniciar app
- Eliminar tokens al hacer logout
- Usar cifrado del sistema operativo

---

### 3. HTTP (Services/Http) - 1 archivo

#### `AuthenticatedHttpHandler.cs`
**Ubicación**: `Advance Control/Services/Http/AuthenticatedHttpHandler.cs`

**Estado**: Vacío

**Propósito**: DelegatingHandler para añadir JWT a peticiones HTTP

**Clase base**: `DelegatingHandler`

**Implementación sugerida**:
```csharp
public class AuthenticatedHttpHandler : DelegatingHandler
{
    private readonly ISecretStorage _secretStorage;
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var token = await _secretStorage.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
        
        return await base.SendAsync(request, cancellationToken);
    }
}
```

**Registro en DI**:
```csharp
services.AddTransient<AuthenticatedHttpHandler>();
services.AddHttpClient("AuthenticatedClient")
    .AddHttpMessageHandler<AuthenticatedHttpHandler>();
```

---

### 4. Modelos (Models) - 2 archivos

#### `CustomerDto.cs`
**Ubicación**: `Advance Control/Models/CustomerDto.cs`

**Estado**: Vacío

**Propósito**: Modelo de datos para clientes

**Propiedades sugeridas**:
```csharp
public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

---

#### `TokenDto.cs`
**Ubicación**: `Advance Control/Models/TokenDto.cs`

**Estado**: Vacío

**Propósito**: Modelo para respuesta de autenticación

**Propiedades sugeridas**:
```csharp
public class TokenDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

---

### 5. Navegación (Navigation) - 1 archivo

#### `INavigationService.cs`
**Ubicación**: `Advance Control/Navigation/INavigationService.cs`

**Estado**: Vacío (debería ser interfaz)

**Propósito**: Servicio para navegación entre vistas

**Implementación sugerida**:
```csharp
public interface INavigationService
{
    /// <summary>
    /// Navega a una vista específica
    /// </summary>
    void Navigate<TViewModel>() where TViewModel : ViewModelBase;
    
    /// <summary>
    /// Navega a una vista con parámetro
    /// </summary>
    void Navigate<TViewModel>(object parameter) where TViewModel : ViewModelBase;
    
    /// <summary>
    /// Retrocede a la vista anterior
    /// </summary>
    bool GoBack();
    
    /// <summary>
    /// Verifica si se puede retroceder
    /// </summary>
    bool CanGoBack { get; }
}
```

---

### 6. Helpers - 1 archivo

#### `JwtUtils.cs`
**Ubicación**: `Advance Control/Helpers/JwtUtils.cs`

**Estado**: Vacío

**Propósito**: Utilidades para manejo de tokens JWT

**Funcionalidad requerida**:
- Decodificar token sin validar firma (leer claims)
- Validar firma del token
- Verificar expiración
- Extraer información del usuario

**Implementación sugerida**:
```csharp
public static class JwtUtils
{
    public static JwtSecurityToken DecodeToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadJwtToken(token);
    }
    
    public static bool IsExpired(string token)
    {
        var jwt = DecodeToken(token);
        return jwt.ValidTo < DateTime.UtcNow;
    }
    
    public static string GetClaim(string token, string claimType)
    {
        var jwt = DecodeToken(token);
        return jwt.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
}
```

---

### 7. ViewModels - 3 archivos

#### `ViewModelBase.cs`
**Ubicación**: `Advance Control/ViewModels/ViewModelBase.cs`

**Estado**: Vacío

**Propósito**: Clase base para todos los ViewModels

**Funcionalidad requerida**:
- Implementar `INotifyPropertyChanged`
- Método helper `SetProperty<T>`
- O usar `ObservableObject` de CommunityToolkit.Mvvm

**Implementación sugerida**:
```csharp
public class ViewModelBase : ObservableObject
{
    // Si no usas CommunityToolkit:
    // - Implementar INotifyPropertyChanged
    // - protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
}
```

---

#### `MainViewModel.cs`
**Ubicación**: `Advance Control/ViewModels/MainViewModel.cs`

**Estado**: Vacío

**Propósito**: ViewModel de la ventana principal

**Responsabilidades**:
- Gestionar navegación principal
- Mostrar estado de autenticación
- Comandos del menú principal

---

#### `CustomersViewModel.cs`
**Ubicación**: `Advance Control/ViewModels/CustomersViewModel.cs`

**Estado**: Vacío

**Propósito**: ViewModel para gestión de clientes

**Responsabilidades**:
- Listar clientes
- Añadir nuevo cliente
- Editar cliente
- Eliminar cliente
- Búsqueda/filtrado

**Propiedades sugeridas**:
```csharp
public class CustomersViewModel : ViewModelBase
{
    public ObservableCollection<CustomerDto> Customers { get; set; }
    public CustomerDto SelectedCustomer { get; set; }
    
    public ICommand LoadCustomersCommand { get; }
    public ICommand AddCustomerCommand { get; }
    public ICommand EditCustomerCommand { get; }
    public ICommand DeleteCustomerCommand { get; }
}
```

---

### 8. Settings - 1 archivo

#### `ClientSettings.cs`
**Ubicación**: `Advance Control/Settings/ClientSettings.cs`

**Estado**: Vacío

**Propósito**: Configuración del cliente

**Propiedades sugeridas**:
```csharp
public class ClientSettings
{
    public string Language { get; set; } = "es-ES";
    public string Theme { get; set; } = "Light";
    public bool AutoLogin { get; set; } = false;
    public bool RememberCredentials { get; set; } = false;
    public int RequestTimeoutSeconds { get; set; } = 30;
}
```

---

### 9. Archivo Duplicado (Debe eliminarse)

#### `Helpers/Converters/BooleanToVisibilityConverter.cs`
**Ubicación**: `Advance Control/Helpers/Converters/BooleanToVisibilityConverter.cs`

**Estado**: Vacío con comentario de prueba

**Problema**: Existe una versión implementada y funcional en `Converters/BooleanToVisibilityConverter.cs`

**Acción requerida**: **ELIMINAR** este archivo duplicado para evitar confusión y mantener una única fuente de verdad.

**Comando para eliminar**:
```bash
git rm "Advance Control/Helpers/Converters/BooleanToVisibilityConverter.cs"
```

**Nota**: Este archivo crea deuda técnica y potencial confusión. Su eliminación debería ser prioritaria antes de continuar con nuevas implementaciones.

---

## Archivos Implementados ✅

Para referencia, estos archivos ya tienen implementación:

1. `Services/OnlineCheck/OnlineCheck.cs` - Verificación de conectividad
2. `Services/OnlineCheck/OnlineCheckResult.cs` - Resultado de verificación
3. `Services/OnlineCheck/IOnlineCheck.cs` - Interfaz de verificación
4. `Services/EndPointProvider/ApiEndpointProvider.cs` - Construcción de URLs
5. `Services/EndPointProvider/IApiEndpointProvider.cs` - Interfaz de endpoints
6. `Services/EndPointProvider/ExternalApiOptions.cs` - Opciones de configuración
7. `Converters/BooleanToVisibilityConverter.cs` - Conversor para XAML

## Priorización de Implementación

### Alta Prioridad (Core Functionality)
1. `IAuthService` + `AuthService` - Autenticación básica
2. `ISecretStorage` + `SecretStorageWindows` - Almacenamiento seguro
3. `TokenDto` - Modelo de token
4. `AuthenticatedHttpHandler` - HTTP con JWT

### Media Prioridad (Essential Features)
5. `ViewModelBase` - Base para ViewModels
6. `CustomerDto` - Modelo de cliente
7. `CustomersViewModel` - Funcionalidad de clientes
8. `MainViewModel` - ViewModel principal

### Baja Prioridad (Nice to Have)
9. `INavigationService` - Navegación avanzada
10. `JwtUtils` - Utilidades JWT
11. `ClientSettings` - Configuración de usuario
12. `AuthServiceStub` - Para desarrollo sin API

## Checklist de Implementación

Para cada archivo a implementar:

- [ ] Revisar documentación de arquitectura
- [ ] Definir interfaz (si aplica)
- [ ] Implementar funcionalidad básica
- [ ] Agregar XML documentation comments
- [ ] Agregar manejo de errores
- [ ] Implementar logging (si aplica)
- [ ] Agregar unit tests
- [ ] Actualizar este documento marcando como completado

## Convenciones de Código

Al implementar estos archivos, seguir:

1. **Naming**: PascalCase para clases, métodos y propiedades
2. **Async**: Todos los métodos I/O deben ser async
3. **CancellationToken**: Incluir en métodos async
4. **Nullable**: Usar nullable reference types
5. **XML Docs**: Agregar documentación XML a APIs públicas
6. **Error Handling**: try-catch apropiado, no silenciar errores
7. **DI**: Usar inyección de dependencias, no instancias estáticas
8. **ConfigureAwait**: Usar `ConfigureAwait(false)` en servicios

## Referencias

- [ARCHITECTURE.md](./ARCHITECTURE.md) - Arquitectura del proyecto
- [API.md](./API.md) - Documentación de servicios implementados
- [README.md](./README.md) - Documentación general
