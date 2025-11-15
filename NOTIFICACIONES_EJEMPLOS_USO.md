# Ejemplos de Uso - Sistema de Notificaciones

Este documento proporciona ejemplos prácticos de cómo usar el sistema de notificaciones con las nuevas características de tiempo de vida y eliminación manual.

## 🆕 Nuevas Características

### 1. Botón de Eliminar en cada Notificación
Cada notificación ahora incluye un botón con icono de papelera (🗑️) en la esquina superior derecha que permite eliminarla manualmente en cualquier momento.

### 2. Tiempo de Vida Configurable
Las notificaciones pueden configurarse para:
- **Ser estáticas**: Permanecen hasta que el usuario las elimine manualmente (`tiempoDeVidaSegundos: null`)
- **Auto-eliminarse**: Se eliminan automáticamente después de un tiempo específico (`tiempoDeVidaSegundos: X`)

## 📝 Ejemplos de Código

### Ejemplo 1: Notificación Estática (Predeterminado)
```csharp
// Esta notificación permanecerá hasta que el usuario la elimine
await _notificacionService.MostrarNotificacionAsync(
    titulo: "Tarea Completada",
    nota: "El proceso de sincronización ha finalizado exitosamente"
);
```

### Ejemplo 2: Notificación Temporal de 5 Segundos
```csharp
// Esta notificación desaparecerá automáticamente después de 5 segundos
await _notificacionService.MostrarNotificacionAsync(
    titulo: "Guardado",
    nota: "Los cambios se han guardado correctamente",
    tiempoDeVidaSegundos: 5
);
```

### Ejemplo 3: Notificación de Aviso Rápido (3 segundos)
```csharp
// Ideal para confirmaciones rápidas
await _notificacionService.MostrarNotificacionAsync(
    titulo: "¡Copiado!",
    nota: "El contenido se ha copiado al portapapeles",
    tiempoDeVidaSegundos: 3
);
```

### Ejemplo 4: Notificación con Fecha y Tiempo de Vida
```csharp
// Combina información de fecha/hora con auto-eliminación
await _notificacionService.MostrarNotificacionAsync(
    titulo: "Recordatorio de Reunión",
    nota: "Reunión de revisión de proyecto en 15 minutos",
    fechaHoraInicio: DateTime.Now.AddMinutes(15),
    fechaHoraFinal: DateTime.Now.AddMinutes(75),
    tiempoDeVidaSegundos: 900  // 15 minutos
);
```

### Ejemplo 5: Notificación de Alerta Importante (Estática)
```csharp
// Para alertas que requieren atención del usuario
await _notificacionService.MostrarNotificacionAsync(
    titulo: "⚠️ Acción Requerida",
    nota: "Por favor, revise los documentos pendientes de aprobación",
    // Sin tiempoDeVidaSegundos - permanecerá hasta eliminación manual
);
```

### Ejemplo 6: Notificación de Error Temporal
```csharp
// Errores no críticos que desaparecen automáticamente
await _notificacionService.MostrarNotificacionAsync(
    titulo: "Error de Conexión",
    nota: "No se pudo conectar al servidor. Reintentando...",
    tiempoDeVidaSegundos: 10
);
```

### Ejemplo 7: Notificación de Progreso de Larga Duración
```csharp
// Para operaciones que toman tiempo
var notificacion = await _notificacionService.MostrarNotificacionAsync(
    titulo: "Procesando...",
    nota: "Generando reporte mensual. Esto puede tomar varios minutos.",
    tiempoDeVidaSegundos: 300  // 5 minutos
);

// Más tarde, eliminar manualmente cuando el proceso termine
_notificacionService.EliminarNotificacion(notificacion.Id);
```

## 🎯 Casos de Uso Recomendados

### Notificaciones Estáticas (sin tiempo de vida)
Usar cuando:
- ✅ Requiere acción del usuario
- ✅ Información importante que no debe perderse
- ✅ Alertas o advertencias críticas
- ✅ Mensajes de bienvenida
- ✅ Estado de sesión

### Notificaciones Temporales (con tiempo de vida)
Usar cuando:
- ✅ Confirmaciones de acciones (3-5 segundos)
- ✅ Mensajes informativos no críticos (5-10 segundos)
- ✅ Recordatorios con tiempo limitado (30-300 segundos)
- ✅ Estados transitorios (conectando, guardando, etc.)
- ✅ Mensajes de éxito/error no críticos

## 🎨 Mejores Prácticas

### 1. Tiempos Recomendados
```csharp
// Confirmación rápida
tiempoDeVidaSegundos: 3

// Mensaje informativo
tiempoDeVidaSegundos: 5

// Alerta de atención
tiempoDeVidaSegundos: 10

// Recordatorio
tiempoDeVidaSegundos: 30

// Proceso largo
tiempoDeVidaSegundos: 300  // 5 minutos
```

### 2. Títulos Descriptivos
```csharp
// ✅ BUENO
titulo: "Documento Guardado"
titulo: "Error de Validación"
titulo: "Bienvenido, Juan"

// ❌ EVITAR
titulo: "OK"
titulo: "Error"
titulo: "Mensaje"
```

### 3. Notas Informativas
```csharp
// ✅ BUENO - Proporciona contexto
nota: "Los cambios se han guardado correctamente en el servidor"

// ❌ EVITAR - Demasiado vaga
nota: "Todo bien"
```

### 4. Uso del Botón de Eliminar
- Los usuarios pueden eliminar **cualquier** notificación en cualquier momento
- Las notificaciones temporales se eliminan automáticamente, pero los usuarios pueden eliminarlas antes
- Las notificaciones estáticas **deben** ser eliminadas manualmente

## 💡 Ejemplos Avanzados

### Notificación con Actualización Dinámica
```csharp
// Crear notificación de progreso
var notificacion = await _notificacionService.MostrarNotificacionAsync(
    titulo: "Descargando Archivo",
    nota: "Progreso: 0%",
    tiempoDeVidaSegundos: 120
);

// Simular actualización de progreso
// (En realidad, necesitarías eliminar y crear una nueva)
for (int i = 25; i <= 100; i += 25)
{
    await Task.Delay(1000);
    _notificacionService.EliminarNotificacion(notificacion.Id);
    notificacion = await _notificacionService.MostrarNotificacionAsync(
        titulo: "Descargando Archivo",
        nota: $"Progreso: {i}%",
        tiempoDeVidaSegundos: i < 100 ? 120 : 5
    );
}
```

### Sistema de Notificaciones por Tipo
```csharp
public async Task NotificarExito(string mensaje, int? duracion = 5)
{
    await _notificacionService.MostrarNotificacionAsync(
        titulo: "✅ Éxito",
        nota: mensaje,
        tiempoDeVidaSegundos: duracion
    );
}

public async Task NotificarError(string mensaje, int? duracion = null)
{
    await _notificacionService.MostrarNotificacionAsync(
        titulo: "❌ Error",
        nota: mensaje,
        tiempoDeVidaSegundos: duracion  // null = estática para errores importantes
    );
}

public async Task NotificarAdvertencia(string mensaje, int? duracion = 10)
{
    await _notificacionService.MostrarNotificacionAsync(
        titulo: "⚠️ Advertencia",
        nota: mensaje,
        tiempoDeVidaSegundos: duracion
    );
}

// Uso
await NotificarExito("Operación completada");
await NotificarError("No se pudo conectar al servidor");
await NotificarAdvertencia("La sesión expirará en 5 minutos");
```

## 🔧 Interacción Manual vs. Automática

### Eliminación Manual
```csharp
var notificacion = await _notificacionService.MostrarNotificacionAsync(
    titulo: "Proceso Iniciado",
    nota: "El proceso puede tardar varios minutos",
    tiempoDeVidaSegundos: 300
);

// Usuario puede hacer clic en el botón de eliminar
// O eliminar programáticamente
_notificacionService.EliminarNotificacion(notificacion.Id);
```

### Eliminación Automática
```csharp
// Notificación se eliminará automáticamente después de 10 segundos
await _notificacionService.MostrarNotificacionAsync(
    titulo: "Datos Actualizados",
    nota: "La información se ha actualizado desde el servidor",
    tiempoDeVidaSegundos: 10
);

// No se requiere código adicional
// Si el usuario hace clic en eliminar antes, el timer se cancela automáticamente
```

## 📊 Resumen de Parámetros

| Parámetro | Tipo | Obligatorio | Descripción | Ejemplo |
|-----------|------|-------------|-------------|---------|
| `titulo` | `string` | ✅ Sí | Título de la notificación | `"Proceso Completado"` |
| `nota` | `string?` | ❌ No | Contenido detallado | `"Se procesaron 150 registros"` |
| `fechaHoraInicio` | `DateTime?` | ❌ No | Fecha/hora de inicio | `DateTime.Now.AddHours(1)` |
| `fechaHoraFinal` | `DateTime?` | ❌ No | Fecha/hora final | `DateTime.Now.AddHours(2)` |
| `tiempoDeVidaSegundos` | `int?` | ❌ No | Segundos hasta auto-eliminación | `30` (o `null` para estática) |

## ✨ Conclusión

El sistema de notificaciones ahora ofrece:
- 🔄 **Flexibilidad**: Notificaciones estáticas o temporales según necesidad
- 🗑️ **Control del usuario**: Botón de eliminar siempre disponible
- ⏱️ **Auto-gestión**: Notificaciones temporales se limpian automáticamente
- 🎯 **Mejor UX**: Los usuarios no necesitan limpiar notificaciones triviales manualmente

¡Usa estas características para mejorar la experiencia de usuario en tu aplicación!
