# Checklist de Verificación - Sistema de Autenticación

Este documento proporciona una lista de verificación completa para validar que el sistema de autenticación del cliente cumple con la especificación del API.

## ✅ Verificación Completada (Revisión de Código)

### Endpoints del API

- [x] **Login**: `POST /api/Auth/login`
  - [x] Request body usa `{username, password}` (no `{usuario, pass}`)
  - [x] Validación: username 3-150 caracteres
  - [x] Validación: password 4-100 caracteres
  - [x] Procesa response: `{accessToken, refreshToken, expiresIn, tokenType, user}`
  - [x] Almacena tokens en SecureStorage
  - [x] Actualiza estado IsAuthenticated

- [x] **Refresh**: `POST /api/Auth/refresh`
  - [x] Request body usa `{refreshToken}`
  - [x] Procesa response: `{accessToken, refreshToken, expiresIn, tokenType, user}`
  - [x] Valida que servidor devuelve nuevo refreshToken (rotation)
  - [x] Almacena nuevos tokens
  - [x] Limpia tokens en 401 (detección de reuso)

- [x] **Validate**: `POST /api/Auth/validate`
  - [x] Request body usa `{token}`
  - [x] Procesa response: `{valid, claims}`
  - [x] Intenta refresh en 401
  - [x] Retorna boolean de validación

- [x] **Logout**: `POST /api/Auth/logout`
  - [x] Request body usa `{refreshToken}`
  - [x] Limpia estado local antes de llamar al servidor
  - [x] Revoca token en servidor
  - [x] Maneja errores gracefully
  - [x] Operación idempotente

### Validaciones de Credenciales

- [x] **Username**
  - [x] Obligatorio (no nulo, no vacío)
  - [x] Mínimo 3 caracteres
  - [x] Máximo 150 caracteres
  - [x] Implementado en LoginViewModel.ValidateCredentials
  - [x] Implementado en LogInDto con DataAnnotations

- [x] **Password**
  - [x] Obligatorio (no nulo, no vacío)
  - [x] Mínimo 4 caracteres
  - [x] Máximo 100 caracteres
  - [x] Implementado en LoginViewModel.ValidateCredentials
  - [x] Implementado en LogInDto con DataAnnotations

### Seguridad

- [x] **Token Storage**
  - [x] Access token en memoria (no persiste)
  - [x] Refresh token en PasswordVault cifrado
  - [x] Metadata de expiración en storage seguro

- [x] **Token Lifecycle**
  - [x] Refresh automático 15 segundos antes de expiración
  - [x] Validación de rotación de tokens
  - [x] Limpieza completa en logout
  - [x] Thread-safe con SemaphoreSlim

- [x] **Network Security**
  - [x] Tokens solo se adjuntan al dominio del API
  - [x] Prevención de token leakage a dominios externos
  - [x] Authorization header con Bearer token

- [x] **Error Handling**
  - [x] Retry automático en 401
  - [x] Limpieza de estado en errores de refresh
  - [x] Logging detallado de operaciones
  - [x] Manejo graceful de errores de storage

### Arquitectura

- [x] **IAuthService / AuthService**
  - [x] Implementa todos los métodos requeridos
  - [x] Carga tokens al inicializar
  - [x] GetAccessTokenAsync con refresh automático
  - [x] RefreshTokenAsync thread-safe
  - [x] ValidateTokenAsync con fallback a refresh
  - [x] LogoutAsync con revocación en servidor
  - [x] ClearTokenAsync para limpieza local
  - [x] IsAuthenticated property

- [x] **AuthenticatedHttpHandler**
  - [x] Adjunta Authorization header automáticamente
  - [x] Detecta 401 y intenta refresh
  - [x] Clona request para retry
  - [x] Solo adjunta tokens al API configurado
  - [x] Usa Lazy<IAuthService> para evitar circular dependency

- [x] **SecureStorage**
  - [x] Implementa ISecureStorage
  - [x] Usa Windows PasswordVault
  - [x] Manejo robusto de errores COM
  - [x] SetAsync, GetAsync, RemoveAsync, ClearAsync

- [x] **LoginViewModel**
  - [x] Validación de credenciales
  - [x] Estados: loading, error, success
  - [x] Integración con AuthService
  - [x] Manejo de errores

- [x] **MainViewModel**
  - [x] ShowLoginDialogAsync
  - [x] LogoutAsync llama a AuthService.LogoutAsync
  - [x] Actualiza estado IsAuthenticated

### Configuración

- [x] **appsettings.json**
  - [x] ExternalApi.BaseUrl configurado
  - [x] Endpoint base termina en /api/

- [x] **Dependency Injection**
  - [x] ISecureStorage registrado
  - [x] AuthenticatedHttpHandler registrado con Lazy
  - [x] IAuthService registrado con HttpClient
  - [x] HttpClient configurado con BaseAddress
  - [x] AuthenticatedHttpHandler en pipeline

### Documentación

- [x] **AUTENTICACION_CLIENTE.md**
  - [x] Descripción general
  - [x] Arquitectura de componentes
  - [x] Flujos detallados (login, refresh, validate, logout)
  - [x] Almacenamiento de tokens
  - [x] Configuración
  - [x] Seguridad
  - [x] Manejo de errores
  - [x] Testing
  - [x] Troubleshooting
  - [x] Mantenimiento

## ⏳ Verificación Pendiente (Requiere Testing Manual)

### Pruebas Funcionales en Windows

- [ ] **Login Exitoso**
  - [ ] Ingresar credenciales válidas
  - [ ] Verificar que se almacenan tokens en PasswordVault
  - [ ] Verificar que IsAuthenticated = true
  - [ ] Verificar que se puede acceder a recursos protegidos

- [ ] **Login Fallido**
  - [ ] Ingresar credenciales inválidas
  - [ ] Verificar mensaje de error apropiado
  - [ ] Verificar que no se almacenan tokens
  - [ ] Verificar que IsAuthenticated = false

- [ ] **Validación de Credenciales**
  - [ ] Usuario < 3 caracteres: muestra error
  - [ ] Usuario > 150 caracteres: muestra error
  - [ ] Password < 4 caracteres: muestra error
  - [ ] Password > 100 caracteres: muestra error
  - [ ] Campos vacíos: muestra error

- [ ] **Refresh Automático**
  - [ ] Login exitoso
  - [ ] Esperar cerca de la expiración del token
  - [ ] Hacer petición al API
  - [ ] Verificar que se refresca automáticamente sin intervención del usuario
  - [ ] Verificar que se obtiene nuevo refreshToken

- [ ] **Logout**
  - [ ] Login exitoso
  - [ ] Hacer logout
  - [ ] Verificar que tokens se eliminan del PasswordVault
  - [ ] Verificar que IsAuthenticated = false
  - [ ] Verificar que el refreshToken fue revocado en el servidor
  - [ ] Intentar usar el refreshToken revocado (debe fallar)

- [ ] **Token Rotation**
  - [ ] Login exitoso
  - [ ] Capturar refreshToken inicial
  - [ ] Forzar refresh (esperar expiración o invalidar accessToken)
  - [ ] Verificar que se recibe nuevo refreshToken
  - [ ] Intentar usar refreshToken antiguo (debe fallar con 401)
  - [ ] Verificar que el servidor revocó todas las sesiones (si detecta reuso)

- [ ] **Validate Token**
  - [ ] Login exitoso
  - [ ] Llamar ValidateTokenAsync()
  - [ ] Verificar que retorna true con token válido
  - [ ] Invalidar token manualmente
  - [ ] Llamar ValidateTokenAsync()
  - [ ] Verificar que intenta refresh

- [ ] **Manejo de Errores de Red**
  - [ ] Desconectar red
  - [ ] Intentar login
  - [ ] Verificar mensaje de error apropiado
  - [ ] Reconectar red
  - [ ] Verificar que funciona normalmente

- [ ] **Manejo de 401**
  - [ ] Login exitoso
  - [ ] Hacer petición a recurso protegido
  - [ ] Servidor retorna 401
  - [ ] Verificar que AuthenticatedHttpHandler intenta refresh
  - [ ] Verificar que reintenta la petición original
  - [ ] Si refresh falla, verificar que retorna 401 al llamador

- [ ] **Thread Safety**
  - [ ] Login exitoso
  - [ ] Hacer múltiples peticiones concurrentes
  - [ ] Forzar que el token expire
  - [ ] Verificar que solo se hace un refresh (no múltiples)
  - [ ] Verificar que todas las peticiones eventualmente obtienen el nuevo token

- [ ] **Persistencia**
  - [ ] Login exitoso
  - [ ] Cerrar aplicación
  - [ ] Reabrir aplicación
  - [ ] Verificar que los tokens persisten
  - [ ] Verificar que IsAuthenticated = true sin nuevo login

- [ ] **Expiración de Refresh Token**
  - [ ] Modificar configuración para refreshToken con 1 minuto de vida
  - [ ] Login exitoso
  - [ ] Esperar más de 1 minuto
  - [ ] Intentar acceder a recurso protegido
  - [ ] Verificar que el refresh falla
  - [ ] Verificar que se solicita nuevo login

### Pruebas de Seguridad

- [ ] **Token Leakage**
  - [ ] Configurar proxy (Fiddler, Charles)
  - [ ] Hacer peticiones al API
  - [ ] Hacer peticiones a dominio externo
  - [ ] Verificar que tokens solo se envían al dominio del API

- [ ] **Token Storage**
  - [ ] Login exitoso
  - [ ] Verificar que accessToken NO está en disco
  - [ ] Verificar que refreshToken está cifrado en PasswordVault
  - [ ] Intentar leer PasswordVault desde otra app (debe fallar)

- [ ] **HTTPS**
  - [ ] Configurar API con HTTPS
  - [ ] Verificar que todas las peticiones usan HTTPS
  - [ ] Intentar configurar HTTP (debe rechazarse en producción)

- [ ] **Token Reuse Detection**
  - [ ] Login exitoso
  - [ ] Capturar refreshToken
  - [ ] Forzar refresh para obtener nuevo token
  - [ ] Intentar usar refreshToken antiguo
  - [ ] Verificar que servidor rechaza con 401
  - [ ] Verificar que servidor revoca todas las sesiones del usuario

### Pruebas de Performance

- [ ] **Tiempo de Login**
  - [ ] Medir tiempo desde submit hasta IsAuthenticated = true
  - [ ] Objetivo: < 2 segundos en red local

- [ ] **Tiempo de Refresh**
  - [ ] Medir tiempo de RefreshTokenAsync()
  - [ ] Objetivo: < 1 segundo en red local

- [ ] **Overhead de AuthenticatedHttpHandler**
  - [ ] Comparar tiempo de petición con y sin handler
  - [ ] Objetivo: overhead < 50ms

### Pruebas de Integración

- [ ] **Con API Real**
  - [ ] Conectar a API de desarrollo
  - [ ] Ejecutar todos los flujos
  - [ ] Verificar logs del servidor
  - [ ] Verificar que los stored procedures funcionan correctamente

- [ ] **Múltiples Usuarios**
  - [ ] Login con usuario A
  - [ ] Logout
  - [ ] Login con usuario B
  - [ ] Verificar que tokens no se mezclan
  - [ ] Verificar limpieza correcta

- [ ] **Múltiples Sesiones**
  - [ ] Login en dos dispositivos diferentes
  - [ ] Hacer logout en uno
  - [ ] Verificar que el otro sigue funcionando

## 📋 Checklist de Deployment

### Antes de Producción

- [ ] **Configuración**
  - [ ] HTTPS obligatorio en producción
  - [ ] BaseUrl apunta a servidor de producción
  - [ ] Timeouts configurados apropiadamente
  - [ ] Logging configurado (nivel apropiado)

- [ ] **Seguridad**
  - [ ] Certificados SSL válidos
  - [ ] Claves JWT rotadas y seguras (servidor)
  - [ ] RefreshToken.Secret único y seguro (servidor)
  - [ ] Rate limiting activo (servidor)

- [ ] **Testing**
  - [ ] Todas las pruebas funcionales pasadas
  - [ ] Todas las pruebas de seguridad pasadas
  - [ ] Pruebas de carga ejecutadas (servidor)
  - [ ] Pruebas de penetración ejecutadas (servidor)

- [ ] **Documentación**
  - [ ] Documentación actualizada
  - [ ] Guía de troubleshooting disponible
  - [ ] Runbook de operaciones preparado

- [ ] **Monitoreo**
  - [ ] Métricas de autenticación configuradas
  - [ ] Alertas configuradas
  - [ ] Dashboard de monitoreo activo

## 📝 Notas

### Limitaciones Conocidas

1. **WinUI3 en Linux**: No se puede compilar ni ejecutar en Linux, solo Windows 10/11
2. **Access Token Persistence**: El access token no persiste (diseño intencional)
3. **PasswordVault Dependency**: Requiere Windows PasswordVault disponible

### Recomendaciones

1. **Testing Continuo**: Ejecutar todas las pruebas después de cada cambio en el servidor
2. **Monitoreo**: Implementar logging detallado en producción
3. **Rotación de Claves**: Planificar rotación periódica de claves JWT
4. **Rate Limiting Cliente**: Considerar implementar límite de intentos de login en el cliente
5. **Timeout Configuration**: Ajustar timeouts según latencia de red en producción

### Próximos Pasos

1. Ejecutar todas las pruebas manuales en Windows
2. Validar con API de desarrollo
3. Ejecutar pruebas de seguridad
4. Ejecutar pruebas de performance
5. Documentar resultados
6. Preparar para producción

## ✅ Resumen

### Estado Actual: ✅ CÓDIGO VERIFICADO

- ✅ Todos los cambios de código implementados
- ✅ Alineación con especificación del API verificada
- ✅ Documentación completa creada
- ✅ Seguridad revisada y validada en código

### Estado Pendiente: ⏳ TESTING MANUAL REQUERIDO

- ⏳ Pruebas funcionales en Windows pendientes
- ⏳ Pruebas de seguridad pendientes
- ⏳ Pruebas de integración con API real pendientes
- ⏳ Validación de performance pendiente

### Conclusión

El sistema de autenticación está **completamente implementado** según la especificación del API y listo para testing manual en un entorno Windows con el servidor API disponible.
