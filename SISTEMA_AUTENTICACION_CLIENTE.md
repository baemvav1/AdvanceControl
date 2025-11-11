# Sistema de Autenticación - Cliente Advance Control

## 📋 Descripción General

El cliente **Advance Control** implementa un sistema completo de autenticación JWT con refresh tokens que cumple con la especificación del API **AdvanceControlApi**. El sistema proporciona autenticación segura y persistente utilizando una arquitectura de tokens rotativos.

---

## 🏗️ Arquitectura del Cliente

### Componentes Principales

#### 1. **IAuthService** / **AuthService**
**Ubicación:** `/Advance Control/Services/Auth/`

Gestiona todo el ciclo de vida de la autenticación:
- Login con credenciales
- Obtención de access tokens
- Refresh automático de tokens
- Validación de tokens
- Logout con revocación en servidor
- Almacenamiento seguro de tokens

#### 2. **AuthenticatedHttpHandler**
**Ubicación:** `/Advance Control/Services/Http/`

DelegatingHandler que:
- Adjunta automáticamente el access token a las peticiones HTTP
- Maneja respuestas 401 (Unauthorized)
- Intenta refresh automático y reintenta la petición
- Previene token leakage a dominios externos

#### 3. **LoginViewModel**
**Ubicación:** `/Advance Control/ViewModels/`

ViewModel para la vista de login:
- Validación de credenciales del lado del cliente
- Gestión del estado de carga
- Manejo de errores
- Comandos MVVM para login

#### 4. **ISecureStorage** / **SecretStorageWindows**
**Ubicación:** `/Advance Control/Services/Security/`

Almacenamiento seguro de tokens:
- Usa Windows PasswordVault
- Cifrado a nivel de sistema operativo
- No almacena tokens en texto plano

---

## 🔄 Flujos de Autenticación

### 1. Login (Inicio de Sesión)

```
Usuario → LoginViewModel → AuthService → API Server
                                ↓
                         Windows PasswordVault
                                ↓
                         IsAuthenticated = true
```

**Implementación:**

```csharp
// Usuario ingresa credenciales en LoginView
await _authService.AuthenticateAsync(username, password);

// AuthService internamente:
// 1. Valida que las credenciales no estén vacías
// 2. POST /api/Auth/login con { username, password }
// 3. Recibe { accessToken, refreshToken, expiresIn, tokenType, user }
// 4. Almacena tokens en Windows PasswordVault (cifrado)
// 5. Guarda tiempo de expiración
// 6. Marca IsAuthenticated = true
```

**Request al API:**
```json
POST /api/Auth/login
Content-Type: application/json

{
  "username": "usuario_ejemplo",
  "password": "contraseña_segura"
}
```

**Response del API:**
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

**Validaciones del Cliente:**
- Username: 3-150 caracteres
- Password: 4-100 caracteres

### 2. Uso de Access Token en Peticiones

```
HttpClient → AuthenticatedHttpHandler → Adjunta Bearer Token → API Server
```

**Implementación Automática:**

```csharp
// El desarrollador solo hace una petición normal:
var clientes = await _httpClient.GetFromJsonAsync<List<Cliente>>("/api/Clientes");

// AuthenticatedHttpHandler automáticamente:
// 1. Obtiene el access token válido (refresh si es necesario)
// 2. Adjunta "Authorization: Bearer {token}"
// 3. Si recibe 401, intenta refresh y reintenta la petición
```

**Prevención de Token Leakage:**
- Solo adjunta token a peticiones al API configurado
- Verifica el host antes de adjuntar el token
- No envía tokens a dominios externos

### 3. Refresh Token (Renovación Automática)

```
GetAccessTokenAsync → ¿Token expira pronto? → RefreshTokenAsync → API Server
                                                        ↓
                                                   Nuevo Access Token
                                                   Nuevo Refresh Token (rotación)
```

**Implementación:**

```csharp
// Automático al obtener un access token:
var token = await _authService.GetAccessTokenAsync();

// Si el token expira en menos de 15 segundos:
// 1. POST /api/Auth/refresh con { refreshToken }
// 2. Recibe nuevo accessToken y nuevo refreshToken
// 3. Actualiza tokens en memoria y PasswordVault
// 4. El refresh token antiguo queda revocado en el servidor
```

**Request al API:**
```json
POST /api/Auth/refresh
Content-Type: application/json

{
  "refreshToken": "base64_encoded_refresh_token..."
}
```

**Response del API:**
```json
{
  "accessToken": "nuevo_jwt_token...",
  "refreshToken": "nuevo_refresh_token...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "username": "usuario_ejemplo"
  }
}
```

**Manejo de Errores:**
- Si refresh falla con 401: limpia todos los tokens (sesión inválida)
- Thread-safe con `SemaphoreSlim` para evitar race conditions
- Solo un refresh a la vez, aunque se llame concurrentemente

### 4. Validate Token (Validación)

```csharp
// Valida que el token actual sea válido:
var esValido = await _authService.ValidateTokenAsync();

// Internamente:
// 1. Obtiene el access token (con refresh si es necesario)
// 2. POST /api/Auth/validate con { token }
// 3. El servidor valida firma, expiración, issuer, audience
// 4. Si es inválido (401), intenta refresh automáticamente
```

**Request al API:**
```json
POST /api/Auth/validate
Content-Type: application/json

{
  "token": "jwt_token_opcional..."
}
```

También acepta el token en el header:
```
Authorization: Bearer jwt_token...
```

**Response del API:**
```json
{
  "valid": true,
  "claims": {
    "sub": "usuario_ejemplo",
    "jti": "guid_unico",
    "iat": "timestamp",
    "exp": "timestamp",
    "iss": "AdvanceApi",
    "aud": "AdvanceApiUsuarios"
  }
}
```

### 5. Logout (Cerrar Sesión)

```
LogoutAsync → API Server (revoca refresh token) → Limpia tokens locales
                                                         ↓
                                                  IsAuthenticated = false
```

**Implementación:**

```csharp
// Cierra sesión del usuario:
await _authService.LogoutAsync();

// Internamente:
// 1. POST /api/Auth/logout con { refreshToken }
// 2. Servidor revoca el refresh token (marca Revoked = true)
// 3. Limpia tokens de Windows PasswordVault
// 4. Limpia tokens de memoria
// 5. Marca IsAuthenticated = false
// 6. El access token sigue válido hasta su expiración natural
```

**Request al API:**
```json
POST /api/Auth/logout
Content-Type: application/json

{
  "refreshToken": "refresh_token_a_revocar..."
}
```

**Response del API:**
```
204 No Content
```

**Características:**
- **Operación idempotente**: si el token no existe, también retorna 204
- **Limpieza local garantizada**: aunque el servidor falle, limpia tokens locales
- **Access token sigue válido**: hasta su expiración (máx. 60 minutos)

---

## 🔒 Características de Seguridad Implementadas

### 1. Almacenamiento Seguro
✅ **Windows PasswordVault** para tokens
- Cifrado a nivel de sistema operativo
- Protección contra acceso no autorizado
- Integración con Windows Hello / BitLocker

✅ **No hay credenciales hardcodeadas**
- Tokens nunca en texto plano
- Tokens nunca en logs

### 2. Tokens JWT
✅ **Access Token de corta duración** (60 minutos por defecto)
✅ **Refresh Token de larga duración** (30 días por defecto)
✅ **Rotación automática de refresh tokens**
- Cada refresh genera un nuevo refresh token
- El antiguo se revoca automáticamente
- Previene reuso de tokens robados

### 3. Thread Safety
✅ **SemaphoreSlim** para prevenir race conditions en refresh
✅ **ConfigureAwait(false)** para evitar deadlocks
✅ **Lazy initialization** del token desde storage

### 4. Prevención de Token Leakage
✅ **Validación de host** antes de adjuntar token
✅ **Solo adjunta token al API configurado**
✅ **No envía tokens a dominios externos**

### 5. Manejo de Errores
✅ **Retry automático** en 401 con nuevo token
✅ **Limpieza de tokens** cuando son inválidos
✅ **Logging sin datos sensibles**
✅ **Graceful degradation** en errores de storage

### 6. Validación de Entrada
✅ **Username:** 3-150 caracteres
✅ **Password:** 4-100 caracteres
✅ **Feedback claro al usuario**
✅ **Validación antes de enviar al servidor**

---

## 📝 Código de Ejemplo

### Flujo Completo de Autenticación

```csharp
// 1. LOGIN
var loginExitoso = await _authService.AuthenticateAsync("usuario", "contraseña");
if (loginExitoso)
{
    // Usuario autenticado exitosamente
    Console.WriteLine($"Autenticado: {_authService.IsAuthenticated}");
}

// 2. USAR ACCESO TOKEN EN PETICIONES (automático)
// AuthenticatedHttpHandler adjunta el token automáticamente
var clientes = await _httpClient.GetFromJsonAsync<List<Cliente>>("/api/Clientes");

// 3. OBTENER ACCESS TOKEN MANUALMENTE (con refresh automático si es necesario)
var token = await _authService.GetAccessTokenAsync();
if (!string.IsNullOrEmpty(token))
{
    // Token válido obtenido
}

// 4. VALIDAR TOKEN
var esValido = await _authService.ValidateTokenAsync();
if (esValido)
{
    // Token válido
}

// 5. REFRESH MANUAL (normalmente es automático)
var refreshExitoso = await _authService.RefreshTokenAsync();
if (refreshExitoso)
{
    // Nuevo token obtenido
}

// 6. LOGOUT
await _authService.LogoutAsync();
Console.WriteLine($"Autenticado: {_authService.IsAuthenticated}"); // False
```

### Configuración del Sistema

```csharp
// App.xaml.cs - ConfigureServices
services.AddSingleton<ISecureStorage, SecretStorageWindows>();
services.AddTransient<IAuthService, AuthService>();

// Configurar HttpClient con AuthenticatedHttpHandler
services.AddHttpClient<IClienteService, ClienteService>((sp, client) =>
{
    var endpoints = sp.GetRequiredService<IApiEndpointProvider>();
    var baseUri = new Uri(endpoints.GetApiBaseUrl());
    client.BaseAddress = baseUri;
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler(sp =>
{
    // Lazy<T> para romper dependencia circular
    var lazyAuth = new Lazy<IAuthService>(() => sp.GetRequiredService<IAuthService>());
    var endpoints = sp.GetRequiredService<IApiEndpointProvider>();
    var logger = sp.GetRequiredService<ILoggingService>();
    return new AuthenticatedHttpHandler(lazyAuth, endpoints, logger);
});
```

---

## 🧪 Testing

El sistema incluye pruebas unitarias completas:

### AuthServiceTests.cs
- ✅ Login con credenciales válidas
- ✅ Login con credenciales inválidas
- ✅ Login con campos vacíos
- ✅ Obtención de access token
- ✅ Refresh token automático
- ✅ Logout exitoso
- ✅ Logout sin refresh token
- ✅ Logout cuando el servidor falla
- ✅ Limpieza de tokens

### LoginViewModelTests.cs
- ✅ Validación de usuario (longitud, requerido)
- ✅ Validación de contraseña (longitud, requerida)
- ✅ Estado del botón de login (CanLogin)
- ✅ Manejo de errores
- ✅ Property changed notifications
- ✅ Limpieza del formulario

---

## 📊 Diagrama de Flujo

```
┌─────────────┐
│   Usuario   │
└──────┬──────┘
       │ Ingresa credenciales
       ▼
┌─────────────────┐
│  LoginViewModel │
└────────┬────────┘
         │ AuthenticateAsync(user, pass)
         ▼
┌──────────────┐
│  AuthService │
└──────┬───────┘
       │ POST /api/Auth/login
       ▼
┌─────────────────┐
│   API Server    │
└────────┬────────┘
         │ { accessToken, refreshToken, ... }
         ▼
┌──────────────┐
│  AuthService │◄─────┐
└──────┬───────┘      │
       │              │
       │ Almacena     │ Obtiene
       ▼              │
┌────────────────┐    │
│ PasswordVault  │────┘
└────────────────┘
       │
       ▼
┌────────────────────────┐
│ AuthenticatedHttpHandler│
└───────────┬────────────┘
            │ Adjunta Bearer Token
            ▼
      ┌──────────┐
      │ API Call │
      └──────────┘
```

---

## ⚠️ Consideraciones Importantes

### Duración de Tokens
- **Access Token:** 60 minutos (configurable en API)
- **Refresh Token:** 30 días (configurable en API)
- El cliente intenta refresh 15 segundos antes de la expiración

### Límites y Configuración
- **Usuario:** 3-150 caracteres
- **Contraseña:** 4-100 caracteres
- **Max Refresh Tokens por Usuario:** 10 (configurable en API, no implementado aún)

### Manejo de 401 Unauthorized
1. **AuthenticatedHttpHandler** recibe 401
2. Intenta **RefreshTokenAsync()**
3. Si el refresh es exitoso, **reintenta la petición original** con el nuevo token
4. Si el refresh falla (401), **limpia todos los tokens** y retorna 401 al cliente

### Detección de Reuso de Tokens (Servidor)
Según la especificación del API:
- Si se detecta un refresh token revocado siendo reutilizado
- Se asume compromiso de seguridad
- **Se revocan TODOS los refresh tokens del usuario**
- El cliente recibe 401 y debe hacer login nuevamente

---

## 🚀 Mejoras Futuras

### Planificadas
- [ ] Rate limiting del lado del cliente
- [ ] Implementar límite de sesiones activas
- [ ] Monitoreo de sesiones activas
- [ ] Biometría (Windows Hello)
- [ ] Certificate pinning (opcional)

### Opcionales
- [ ] Refresh token automático en background
- [ ] Notificación al usuario cuando otra sesión cierra todas las sesiones (detección de reuso)
- [ ] UI para gestionar sesiones activas
- [ ] Logs de actividad de sesión

---

## 📚 Referencias

### Especificación del API
El sistema cumple completamente con la especificación del API **AdvanceControlApi** que incluye:
- Endpoints: `/api/Auth/login`, `/api/Auth/refresh`, `/api/Auth/validate`, `/api/Auth/logout`
- JWT con HMAC-SHA256
- Refresh token rotation
- Detección de reuso de tokens
- Metadatos de sesión (IP, User-Agent)

### Estándares de Seguridad
- ✅ **OWASP Top 10** compliance
- ✅ **Microsoft Security Development Lifecycle (SDL)**
- ✅ **JWT Best Practices**
- ✅ **OAuth 2.0 patterns** (aunque no es OAuth estricto)

---

## 📞 Soporte

Para problemas con el sistema de autenticación:
1. Verificar que el API esté configurado correctamente
2. Revisar los logs del cliente (ILoggingService)
3. Verificar la configuración en `appsettings.json`
4. Asegurar que HTTPS esté habilitado

**Errores Comunes:**
- **"Credenciales inválidas"**: Usuario o contraseña incorrectos
- **"Token inválido o expirado"**: Usar RefreshTokenAsync o hacer login nuevamente
- **"Refresh token revocado"**: Todas las sesiones fueron revocadas por seguridad, hacer login

---

**Documento:** Sistema de Autenticación - Cliente  
**Versión:** 1.0  
**Fecha:** 11 de Noviembre de 2025  
**Estado:** ✅ Implementado y Funcional  
