# Modo de Desarrollo

## Descripción

El modo de desarrollo es una característica que permite desactivar los timeouts de autenticación y HTTP para facilitar las pruebas y debugging de la aplicación.

## Configuración

Para habilitar el modo de desarrollo, edita el archivo `appsettings.json`:

```json
{
  "DevelopmentMode": {
    "Enabled": true,
    "DisableAuthTimeouts": true,
    "DisableHttpTimeouts": true
  }
}
```

### Opciones disponibles

- **Enabled**: Activa o desactiva el modo de desarrollo (true/false)
- **DisableAuthTimeouts**: Desactiva la verificación de expiración de tokens de autenticación (true/false)
- **DisableHttpTimeouts**: Establece timeouts infinitos para todas las peticiones HTTP (true/false)

## Comportamiento

### Con DisableAuthTimeouts = true

- Los tokens de acceso no se verifican por expiración
- No se intentan refrescar automáticamente los tokens
- La sesión permanece activa indefinidamente (útil para debugging)

### Con DisableHttpTimeouts = true

- Todas las peticiones HTTP tienen timeout infinito
- Permite debuggear endpoints sin preocuparse por timeouts
- Útil para pruebas con breakpoints

## Advertencias de Seguridad

⚠️ **IMPORTANTE**: El modo de desarrollo debe estar desactivado en producción.

- Cuando el modo de desarrollo está activo, se registra un mensaje de advertencia en los logs
- No debe habilitarse en builds de producción
- Solo usar en entornos de desarrollo local

## Logs

Cuando el modo de desarrollo está activo, se registra el siguiente mensaje al iniciar el AuthService:

```
⚠️ MODO DESARROLLO ACTIVO: Los timeouts de autenticación están deshabilitados
```

## Solución de Problemas del Diálogo de Login

El diálogo de login ahora incluye mejoras para asegurar que se cierre correctamente:

1. El cierre del diálogo se ejecuta en el hilo de UI (DispatcherQueue)
2. Se maneja correctamente cuando el diálogo ya está cerrado
3. Los errores se registran pero no interrumpen el flujo

### Verificación del Login

Para verificar que el login funciona correctamente:

1. Ingresa credenciales válidas
2. El diálogo debe cerrarse automáticamente cuando el login sea exitoso
3. La propiedad `LoginSuccessful` se establece en `true`
4. El estado de autenticación se actualiza en `MainViewModel`

## Ejemplo de Uso

### Para pruebas locales:

```json
{
  "DevelopmentMode": {
    "Enabled": true,
    "DisableAuthTimeouts": true,
    "DisableHttpTimeouts": true
  }
}
```

### Para producción:

```json
{
  "DevelopmentMode": {
    "Enabled": false,
    "DisableAuthTimeouts": false,
    "DisableHttpTimeouts": false
  }
}
```
