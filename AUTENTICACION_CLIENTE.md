# Sistema de Autenticación - Cliente AdvanceControl

## Descripción General

Este documento describe la implementación del sistema de autenticación en el cliente WinUI3 de AdvanceControl, alineado con la especificación del API backend.

## Arquitectura

### Componentes Principales

#### 1. IAuthService / AuthService
**Ubicación**: `/Advance Control/Services/Auth/`

Servicio principal que gestiona todo el ciclo de vida de la autenticación:

```csharp
public interface IAuthService
{
    Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(CancellationToken cancellationToken = default);
    Task ClearTokenAsync();
    bool IsAuthenticated { get; }
}
```

**Características clave:**
- Almacenamiento seguro de tokens en Windows PasswordVault
- Carga automática de tokens al inicializar
- Thread-safe con SemaphoreSlim para operaciones de refresh
- Manejo automático de expiración de tokens

#### 2. AuthenticatedHttpHandler
**Ubicación**: `/Advance Control/Services/Http/AuthenticatedHttpHandler.cs`

DelegatingHandler que intercepta todas las peticiones HTTP para:
- Adjuntar automáticamente el header `Authorization: Bearer <token>`
- Detectar respuestas 401 Unauthorized
- Intentar refresh automático y reintentar la petición

**Características clave:**
- Usa `Lazy<IAuthService>` para evitar dependencias circulares
- Solo adjunta tokens a peticiones dirigidas al API configurado
- Clona requests para reintento después de refresh
- Implementa retry automático (una sola vez)

#### 3. ISecureStorage / SecretStorageWindows
**Ubicación**: `/Advance Control/Services/Security/`

Implementación de almacenamiento seguro usando Windows PasswordVault:
- Cifrado a nivel de sistema operativo
- Asociado a la cuenta de usuario de Windows
- Manejo robusto de errores COM

#### 4. LoginViewModel
**Ubicación**: `/Advance Control/ViewModels/LoginViewModel.cs`

ViewModel para la interfaz de inicio de sesión:
- Validación de credenciales según especificación API
- Gestión de estados (loading, error, success)
- Integración con `IAuthService`

## Flujos de Autenticación

### 1. Login (Inicio de Sesión)

```
Usuario → LoginViewModel.ExecuteLogin()
       → AuthService.AuthenticateAsync()
       → POST /api/Auth/login
       ← {accessToken, refreshToken, expiresIn, tokenType, user}
       → Almacena tokens en SecureStorage
       → Actualiza estado IsAuthenticated = true
```

**Validaciones del cliente:**
- Username: 3-150 caracteres, obligatorio
- Password: 4-100 caracteres, obligatorio

**Request al servidor:**
```json
{
  "username": "usuario_ejemplo",
  "password": "contraseña_segura"
}
```

**Response del servidor:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64_encoded_random_token...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "username": "usuario_ejemplo"
  }
}
```

### 2. Acceso a Recursos Protegidos

```
Cliente → HttpClient.GetAsync("/api/Clientes")
       → AuthenticatedHttpHandler intercepta
       → Obtiene token: AuthService.GetAccessTokenAsync()
       → Si token expira pronto, refresca automáticamente
       → Adjunta header: Authorization: Bearer <token>
       → Envía request al servidor
       
Si respuesta = 401:
       → AuthService.RefreshTokenAsync()
       → Clona request original
       → Adjunta nuevo token
       → Reintenta request
```

### 3. Refresh (Renovación de Token)

```
Sistema detecta token próximo a expirar (< 15 segundos)
       → AuthService.RefreshTokenAsync()
       → POST /api/Auth/refresh {refreshToken}
       ← {accessToken, refreshToken, expiresIn, tokenType, user}
       → Valida que nuevo refreshToken existe (token rotation)
       → Almacena nuevos tokens
       → Revoca token antiguo implícitamente en servidor
```

**Seguridad - Token Rotation:**
- Cada refresh genera un nuevo par de tokens
- El refresh token antiguo es revocado automáticamente en el servidor
- Si se detecta reuso de token revocado, el servidor revoca TODAS las sesiones del usuario

### 4. Validate (Validación de Token)

```
Cliente → AuthService.ValidateTokenAsync()
       → POST /api/Auth/validate {token}
       
Si respuesta = 200:
       ← {valid: true, claims: {...}}
       → return true
       
Si respuesta = 401:
       → Intenta RefreshTokenAsync()
       → Retorna resultado del refresh
```

### 5. Logout (Cerrar Sesión)

```
Usuario → MainViewModel.LogoutAsync()
       → AuthService.LogoutAsync()
       → Obtiene refreshToken de memoria/storage
       → ClearTokenAsync() (limpia estado local primero)
       → POST /api/Auth/logout {refreshToken}
       → Servidor revoca el refresh token
       → Actualiza IsAuthenticated = false
```

**Nota importante:** El access token seguirá siendo válido hasta su expiración natural (60 minutos por defecto). Para invalidación inmediata, el servidor debería implementar una lista negra de tokens o reducir el tiempo de expiración.

## Almacenamiento de Tokens

### En Memoria (Volátil)
```csharp
private string? _accessToken;
private string? _refreshToken;
private DateTime? _accessExpiresAtUtc;
```

### En Storage Seguro (Persistente)
Usando Windows PasswordVault:
```
Resource: "Advance_Control:auth.access_token"
UserName: "auth.access_token"
Password: <actual access token>

Resource: "Advance_Control:auth.refresh_token"
UserName: "auth.refresh_token"
Password: <actual refresh token>

Resource: "Advance_Control:auth.access_expires_at_utc"
UserName: "auth.access_expires_at_utc"
Password: <ISO 8601 timestamp>
```

**Ventajas del PasswordVault:**
- Cifrado a nivel de SO
- No requiere implementación de cifrado manual
- Integrado con las credenciales de Windows
- Limpieza automática al desinstalar la app

## Configuración

### appsettings.json
```json
{
  "ExternalApi": {
    "BaseUrl": "https://localhost:7055/api/",
    "ApiKey": ""
  }
}
```

### Registro de Servicios (App.xaml.cs)
```csharp
// Almacenamiento seguro
services.AddSingleton<ISecureStorage, SecretStorageWindows>();

// AuthenticatedHttpHandler con Lazy para romper dependencia circular
services.AddTransient<AuthenticatedHttpHandler>(sp =>
{
    var lazyAuthService = new Lazy<IAuthService>(() => sp.GetRequiredService<IAuthService>());
    var endpointProvider = sp.GetRequiredService<IApiEndpointProvider>();
    var logger = sp.GetService<ILoggingService>();
    return new AuthenticatedHttpHandler(lazyAuthService, endpointProvider, logger);
});

// AuthService con HttpClient pipeline
services.AddHttpClient<IAuthService, AuthService>((sp, client) =>
{
    var provider = sp.GetRequiredService<IApiEndpointProvider>();
    if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<AuthenticatedHttpHandler>();

// Otros servicios con autenticación automática
services.AddHttpClient<IClienteService, ClienteService>(...)
    .AddHttpMessageHandler<AuthenticatedHttpHandler>();
```

## Seguridad

### Características Implementadas

#### 1. Token Storage
- ✅ Access tokens en memoria (no persisten al cerrar app)
- ✅ Refresh tokens en PasswordVault cifrado
- ✅ Metadatos de expiración protegidos

#### 2. Token Lifecycle
- ✅ Refresh automático 15 segundos antes de expiración
- ✅ Validación de rotación de tokens
- ✅ Limpieza completa en logout
- ✅ Thread-safe con SemaphoreSlim

#### 3. Network Security
- ✅ HTTPS requerido en producción
- ✅ Tokens solo se adjuntan al dominio del API
- ✅ Prevención de token leakage a dominios externos

#### 4. Error Handling
- ✅ Retry automático en 401
- ✅ Limpieza de estado en errores de refresh
- ✅ Logging detallado de operaciones
- ✅ Manejo graceful de errores de storage

### Mejores Prácticas Implementadas

1. **No almacenar access tokens en disco**: Los access tokens viven solo en memoria
2. **Refresh automático**: Tokens se refrescan antes de expirar
3. **Single retry**: Solo un reintento después de 401 para evitar loops
4. **Token scope**: Solo se envían al dominio del API configurado
5. **Async/await**: Todas las operaciones son asíncronas
6. **Cancellation tokens**: Soporte para cancelación de operaciones

## Manejo de Errores

### Login
```csharp
try
{
    var success = await _authService.AuthenticateAsync(username, password);
    if (success)
    {
        // Login exitoso
    }
    else
    {
        // Credenciales inválidas o error de red
        ErrorMessage = "Usuario o contraseña incorrectos.";
    }
}
catch (Exception ex)
{
    // Error inesperado
    ErrorMessage = $"Error al iniciar sesión: {ex.Message}";
}
```

### Acceso a API Protegida
```csharp
try
{
    var response = await _httpClient.GetAsync("/api/Clientes");
    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        // Token inválido y refresh falló
        // El usuario necesita volver a hacer login
        await ShowLoginDialogAsync();
    }
    else
    {
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<List<Cliente>>();
    }
}
catch (HttpRequestException ex)
{
    // Error de red
    await _logger.LogErrorAsync("Error de red", ex);
}
```

### Logout
```csharp
try
{
    var success = await _authService.LogoutAsync();
    // success = true incluso si el servidor falla
    // El estado local siempre se limpia
}
catch (Exception ex)
{
    // Error muy raro, el estado local debería estar limpio de todos modos
    await _logger.LogErrorAsync("Error en logout", ex);
}
```

## Testing

### Escenarios de Prueba Recomendados

1. **Login Exitoso**
   - Ingresar credenciales válidas
   - Verificar que se almacenan tokens
   - Verificar que IsAuthenticated = true

2. **Login Fallido**
   - Ingresar credenciales inválidas
   - Verificar mensaje de error
   - Verificar que no se almacenan tokens

3. **Refresh Automático**
   - Login exitoso
   - Esperar hasta cerca de la expiración (60 minutos)
   - Hacer una petición al API
   - Verificar que se refresca automáticamente

4. **Logout**
   - Login exitoso
   - Hacer logout
   - Verificar que tokens se eliminan
   - Verificar que IsAuthenticated = false

5. **Token Inválido**
   - Modificar manualmente el token en PasswordVault
   - Intentar acceder a recurso protegido
   - Verificar que se intenta refresh y falla
   - Usuario debe volver a hacer login

6. **Sin Conexión**
   - Desconectar red
   - Intentar login
   - Verificar manejo de error de red

### Herramientas de Testing

Para probar sin necesidad de la UI:
```csharp
// En una prueba unitaria o aplicación de consola
var services = new ServiceCollection();
services.AddSingleton<IApiEndpointProvider, ApiEndpointProvider>();
services.AddSingleton<ISecureStorage, SecretStorageWindows>();
services.AddHttpClient<IAuthService, AuthService>()
    .AddHttpMessageHandler<AuthenticatedHttpHandler>();

var serviceProvider = services.BuildServiceProvider();
var authService = serviceProvider.GetRequiredService<IAuthService>();

// Probar login
var success = await authService.AuthenticateAsync("usuario", "password");
Console.WriteLine($"Login: {success}");

// Probar obtención de token
var token = await authService.GetAccessTokenAsync();
Console.WriteLine($"Token: {token?[..20]}...");

// Probar logout
await authService.LogoutAsync();
Console.WriteLine("Logout completado");
```

## Troubleshooting

### "Usuario o contraseña incorrectos" pero las credenciales son correctas

**Posibles causas:**
1. El formato del request no coincide con el API
   - Verificar que se envía `{username, password}` no `{usuario, pass}`
2. El API no está ejecutándose
   - Verificar que el API está en `https://localhost:7055/api/`
3. Problema de HTTPS/certificado
   - En desarrollo, asegurarse de confiar en el certificado de desarrollo

### "Error al cargar tokens desde el almacenamiento seguro"

**Posibles causas:**
1. Permisos insuficientes en Windows
   - Ejecutar la aplicación con permisos de usuario estándar
2. PasswordVault no disponible
   - Verificar que Windows está actualizado
3. Credenciales corruptas
   - Ejecutar `await authService.ClearTokenAsync()` para limpiar

### La aplicación pide login constantemente

**Posibles causas:**
1. Los tokens no se están persistiendo
   - Verificar que `SecretStorageWindows` funciona correctamente
2. El refresh token está expirado o inválido
   - Hacer login nuevamente
3. Error en la rotación de tokens
   - Verificar logs del servidor

### "Access denied" en PasswordVault

**Solución:**
- Verificar que la aplicación tiene el capability `sharedUserCertificates` en el manifest
- Ejecutar la aplicación como usuario estándar (no como administrador)

## Diferencias con la Especificación Original

### ✅ Implementado según especificación:
- Login endpoint con `{username, password}`
- Refresh endpoint con rotación de tokens
- Validate endpoint con validación en servidor
- Logout endpoint con revocación de token
- Validación de credenciales (username: 3-150, password: 4-100)

### ⚠️ Consideraciones adicionales:
- El access token en memoria no es HTTP-only cookie (WinUI3 no tiene cookies)
- El refresh token se almacena en PasswordVault en lugar de HTTP-only cookie
- La validación de token puede usar el header Authorization además del body

### 📝 Notas:
- HTTPS es responsabilidad de la configuración del API
- Rate limiting debe implementarse en el servidor
- El cliente no implementa límite de intentos de login (el servidor debe hacerlo)

## Mantenimiento

### Rotación de Claves
Si el servidor cambia las claves JWT:
1. Los tokens existentes se vuelven inválidos
2. Los usuarios deben hacer logout y volver a hacer login
3. Considerar notificar a los usuarios antes del cambio

### Limpieza de Tokens Expirados
Los tokens en PasswordVault no se limpian automáticamente. Considerar:
```csharp
// Limpiar tokens al cerrar la aplicación
protected override async void OnExit(ExitEventArgs e)
{
    var authService = Host.Services.GetService<IAuthService>();
    await authService?.ClearTokenAsync();
    base.OnExit(e);
}
```

### Monitoreo
Agregar métricas para:
- Número de logins exitosos/fallidos
- Número de refresh automáticos
- Errores de autenticación
- Tiempo de respuesta del API

## Referencias

- [Especificación del API](./ARQUITECTURA_Y_ESTADO.md)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [Windows PasswordVault](https://docs.microsoft.com/en-us/uwp/api/windows.security.credentials.passwordvault)
- [OAuth 2.0 Refresh Token Rotation](https://tools.ietf.org/html/draft-ietf-oauth-security-topics)

## Changelog

### v1.0 - 2025-11-10
- ✅ Implementación inicial completa
- ✅ Alineación con especificación del API
- ✅ Login con `{username, password}`
- ✅ Logout con revocación en servidor
- ✅ Validación de rotación de tokens
- ✅ Credenciales según especificación (3-150, 4-100)
