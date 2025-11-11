# Guía de Pruebas - Modo Desarrollo y Corrección de Diálogo de Login

## Propósito

Este documento describe cómo probar las nuevas características implementadas:
1. Modo de desarrollo con timeouts deshabilitados
2. Corrección del cierre del diálogo de login

## Pre-requisitos

- Windows 10/11
- Visual Studio 2022 con soporte para WinUI 3
- Acceso al servidor de API configurado en appsettings.json

## Escenarios de Prueba

### Escenario 1: Verificar Modo Producción (Por Defecto)

**Objetivo**: Confirmar que el comportamiento normal funciona correctamente.

**Configuración en appsettings.json**:
```json
{
  "DevelopmentMode": {
    "Enabled": false,
    "DisableAuthTimeouts": false,
    "DisableHttpTimeouts": false
  }
}
```

**Pasos**:
1. Iniciar la aplicación
2. Verificar que NO aparece el mensaje de advertencia en los logs: "⚠️ MODO DESARROLLO ACTIVO"
3. Intentar login con credenciales válidas
4. Observar que el token expira después del tiempo configurado (normalmente 30 segundos)
5. Verificar que las peticiones HTTP tienen timeout de 30 segundos

**Resultado Esperado**:
- Los timeouts funcionan normalmente
- No hay mensajes de advertencia sobre modo desarrollo
- El sistema funciona en modo producción

---

### Escenario 2: Activar Modo Desarrollo Completo

**Objetivo**: Verificar que los timeouts se desactivan correctamente.

**Configuración en appsettings.json**:
```json
{
  "DevelopmentMode": {
    "Enabled": true,
    "DisableAuthTimeouts": true,
    "DisableHttpTimeouts": true
  }
}
```

**Pasos**:
1. Iniciar la aplicación
2. Verificar en los logs que aparece: "⚠️ MODO DESARROLLO ACTIVO: Los timeouts de autenticación están deshabilitados"
3. Hacer login con credenciales válidas
4. Esperar más del tiempo normal de expiración del token (más de 30 segundos)
5. Realizar operaciones que requieran autenticación
6. Colocar un breakpoint en el código y pausar la ejecución por varios minutos
7. Continuar y verificar que la petición HTTP no falla por timeout

**Resultado Esperado**:
- Aparece el mensaje de advertencia en los logs
- El token NO expira después del tiempo normal
- Las peticiones HTTP pueden tomar tiempo indefinido sin fallar
- Puedes debuggear sin preocuparte por timeouts

---

### Escenario 3: Solo Desactivar Auth Timeouts

**Objetivo**: Verificar que se pueden desactivar solo los timeouts de autenticación.

**Configuración en appsettings.json**:
```json
{
  "DevelopmentMode": {
    "Enabled": true,
    "DisableAuthTimeouts": true,
    "DisableHttpTimeouts": false
  }
}
```

**Pasos**:
1. Iniciar la aplicación
2. Hacer login con credenciales válidas
3. Esperar más del tiempo normal de expiración del token
4. Verificar que el token sigue funcionando
5. Hacer una petición HTTP que normalmente tarde más de 30 segundos
6. Verificar que la petición falla por timeout HTTP

**Resultado Esperado**:
- El token NO expira
- Las peticiones HTTP SÍ respetan los timeouts normales

---

### Escenario 4: Verificar Cierre del Diálogo de Login (Caso Exitoso)

**Objetivo**: Confirmar que el diálogo se cierra correctamente después de login exitoso.

**Configuración**: Cualquier configuración es válida.

**Pasos**:
1. Iniciar la aplicación
2. Abrir el diálogo de login
3. Ingresar credenciales VÁLIDAS
4. Hacer clic en "Iniciar Sesión"
5. Observar el comportamiento del diálogo

**Resultado Esperado**:
- El diálogo muestra el indicador de carga (IsLoading)
- Después de la autenticación exitosa, el diálogo se cierra AUTOMÁTICAMENTE
- NO se requiere hacer clic en "Cancelar" o cerrar manualmente
- El estado `IsAuthenticated` se actualiza a `true` en MainViewModel
- No hay errores en los logs relacionados con el cierre del diálogo

---

### Escenario 5: Verificar Diálogo de Login (Caso Fallido)

**Objetivo**: Confirmar que el diálogo NO se cierra automáticamente en caso de error.

**Configuración**: Cualquier configuración es válida.

**Pasos**:
1. Iniciar la aplicación
2. Abrir el diálogo de login
3. Ingresar credenciales INVÁLIDAS
4. Hacer clic en "Iniciar Sesión"
5. Observar el comportamiento del diálogo

**Resultado Esperado**:
- El diálogo muestra el mensaje de error
- El diálogo NO se cierra automáticamente
- Se puede corregir las credenciales y reintentar
- El botón "Cancelar" cierra el diálogo

---

### Escenario 6: Verificar Cierre Manual del Diálogo

**Objetivo**: Confirmar que el botón "Cancelar" funciona correctamente.

**Pasos**:
1. Iniciar la aplicación
2. Abrir el diálogo de login
3. Hacer clic en "Cancelar" (sin ingresar credenciales o con credenciales parciales)

**Resultado Esperado**:
- El diálogo se cierra inmediatamente
- Los campos del formulario se limpian
- El método `ShowLoginDialogAsync` retorna `false`
- El estado `IsAuthenticated` permanece en `false`

---

## Verificación de Logs

Durante todas las pruebas, verificar que los logs contengan:

### En Modo Desarrollo:
```
⚠️ MODO DESARROLLO ACTIVO: Los timeouts de autenticación están deshabilitados
```

### En Login Exitoso:
```
Usuario autenticado exitosamente: [nombre_usuario]
```

### En Login Fallido:
```
Intento de login fallido para usuario: [nombre_usuario]
```

### En Cierre de Sesión:
```
Usuario cerró sesión
```

## Problemas Conocidos y Soluciones

### Problema: El diálogo no se cierra después de login exitoso

**Síntoma**: El diálogo permanece abierto incluso después de autenticación exitosa.

**Verificación**:
1. Revisar los logs para buscar: "Error al cerrar diálogo de login"
2. Verificar que `LoginViewModel.LoginSuccessful` cambia a `true`
3. Verificar que el evento `PropertyChanged` se dispara

**Solución Implementada**:
- El cierre del diálogo ahora se ejecuta en el hilo de UI usando `DispatcherQueue.TryEnqueue`
- Se manejan excepciones si el diálogo ya está cerrado
- Se registran errores sin interrumpir el flujo

### Problema: Timeouts no se respetan en modo desarrollo

**Síntoma**: Las peticiones fallan por timeout incluso con `DisableHttpTimeouts = true`.

**Verificación**:
1. Confirmar que `Enabled = true` en la sección `DevelopmentMode`
2. Verificar que la configuración se está cargando correctamente
3. Revisar que aparece el mensaje de advertencia en los logs

**Solución**:
- Asegurar que el archivo appsettings.json está marcado como "Copy to Output Directory" = "Copy if newer"
- Reiniciar la aplicación después de cambiar la configuración

## Checklist de Pruebas Completas

- [ ] Escenario 1: Modo producción funciona correctamente
- [ ] Escenario 2: Modo desarrollo completo desactiva todos los timeouts
- [ ] Escenario 3: Desactivación selectiva de auth timeouts funciona
- [ ] Escenario 4: Diálogo se cierra automáticamente con login exitoso
- [ ] Escenario 5: Diálogo permanece abierto con login fallido
- [ ] Escenario 6: Botón cancelar funciona correctamente
- [ ] Logs muestran mensajes apropiados en cada escenario
- [ ] No hay excepciones no manejadas en ningún escenario

## Notas Adicionales

- **Seguridad**: NUNCA habilitar el modo desarrollo en producción
- **Performance**: El modo desarrollo puede hacer que la aplicación se comporte de manera diferente que en producción
- **Debugging**: El modo desarrollo es ideal para colocar breakpoints sin que los timeouts interrumpan el debugging
