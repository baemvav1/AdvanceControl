# Servicio de Notificaciones

## Descripción

El servicio de notificaciones (`NotificacionService`) proporciona una forma centralizada de gestionar y mostrar notificaciones en la aplicación. Actualmente opera en modo mock (almacenamiento en memoria), pero está diseñado para conectarse a un endpoint real en el futuro.

## Características

- **Notificaciones con parámetros flexibles**: Solo el título es requerido, los demás campos son opcionales
- **Tiempo de vida configurable**: Las notificaciones pueden ser estáticas o auto-eliminarse después de un tiempo
- **Eliminación manual**: Botón de eliminar en cada notificación para eliminarlas manualmente
- **Almacenamiento en memoria**: Las notificaciones se mantienen durante la sesión de la aplicación
- **Observable Collection**: Soporte para binding MVVM automático
- **Eventos**: Sistema de eventos para notificar cuando se agregan notificaciones
- **Logging integrado**: Todas las operaciones se registran automáticamente

## Uso

### Inyección de Dependencia

El servicio está registrado en el contenedor de DI como singleton:

```csharp
// Ya configurado en App.xaml.cs
services.AddSingleton<INotificacionService, NotificacionService>();
```

### Mostrar una Notificación

```csharp
public class MiViewModel
{
    private readonly INotificacionService _notificacionService;
    
    public MiViewModel(INotificacionService notificacionService)
    {
        _notificacionService = notificacionService;
    }
    
    public async Task MostrarNotificacionSimple()
    {
        // Solo título (requerido)
        await _notificacionService.MostrarNotificacionAsync("Mi Notificación");
    }
    
    public async Task MostrarNotificacionCompleta()
    {
        // Con todos los parámetros
        await _notificacionService.MostrarNotificacionAsync(
            titulo: "Reunión Importante",
            nota: "Reunión de equipo para revisar el sprint",
            fechaHoraInicio: DateTime.Now.AddHours(2),
            fechaHoraFinal: DateTime.Now.AddHours(3)
        );
    }
    
    public async Task MostrarNotificacionTemporal()
    {
        // Notificación que se auto-elimina después de 30 segundos
        await _notificacionService.MostrarNotificacionAsync(
            titulo: "Notificación Temporal",
            nota: "Esta notificación desaparecerá en 30 segundos",
            tiempoDeVidaSegundos: 30
        );
    }
    
    public async Task MostrarNotificacionEstatica()
    {
        // Notificación sin tiempo de vida (estática hasta que se elimine manualmente)
        await _notificacionService.MostrarNotificacionAsync(
            titulo: "Notificación Estática",
            nota: "Esta notificación permanecerá hasta que la elimines manualmente"
        );
    }
}
```

### Obtener Notificaciones

```csharp
// Obtener todas las notificaciones como lista
var notificaciones = _notificacionService.ObtenerNotificaciones();

// O usar la colección observable para binding (solo en NotificacionService)
if (_notificacionService is NotificacionService notifService)
{
    var coleccionObservable = notifService.NotificacionesObservable;
    // Bindear a UI
}
```

### Gestionar Notificaciones

```csharp
// Eliminar una notificación específica
bool eliminada = _notificacionService.EliminarNotificacion(notificacionId);

// Limpiar todas las notificaciones
_notificacionService.LimpiarNotificaciones();
```

## Modelo de Notificación

```csharp
public class NotificacionDto
{
    public Guid Id { get; set; }                      // Generado automáticamente
    public string Titulo { get; set; }                // Requerido
    public string? Nota { get; set; }                 // Opcional
    public DateTime? FechaHoraInicio { get; set; }    // Opcional
    public DateTime? FechaHoraFinal { get; set; }     // Opcional
    public DateTime FechaCreacion { get; set; }       // Asignado automáticamente
    public int? TiempoDeVidaSegundos { get; set; }   // Opcional - null = estática
}
```

### Tiempo de Vida de Notificaciones

- **`TiempoDeVidaSegundos = null`**: La notificación es estática y permanecerá visible hasta que el usuario la elimine manualmente
- **`TiempoDeVidaSegundos > 0`**: La notificación se auto-eliminará después del tiempo especificado en segundos
- **`TiempoDeVidaSegundos = 0`**: Se trata como estática (no se auto-elimina)

Ejemplos:
- `tiempoDeVidaSegundos: 5` → Se elimina después de 5 segundos
- `tiempoDeVidaSegundos: 30` → Se elimina después de 30 segundos
- `tiempoDeVidaSegundos: null` → Permanece hasta eliminación manual

## Ejemplo de Uso: Notificación de Bienvenida

El servicio se utiliza actualmente en `LoginViewModel` para mostrar una notificación de bienvenida cuando el usuario inicia sesión exitosamente:

```csharp
// En LoginViewModel.ExecuteLogin()
if (success)
{
    LoginSuccessful = true;
    await _logger.LogInformationAsync($"Usuario autenticado exitosamente: {User}", 
        "LoginViewModel", "ExecuteLogin");
    
    // Mostrar notificación de bienvenida
    await _notificacionService.MostrarNotificacionAsync(
        titulo: "Bienvenido",
        nota: $"Usuario {User} ha iniciado sesión exitosamente",
        fechaHoraInicio: DateTime.Now);
}
```

## Integración con UI

Las notificaciones se muestran automáticamente en el panel de notificaciones de `MainWindow`. El panel incluye:

- Título destacado
- Nota (si está disponible)
- Fechas de inicio y fin (si están disponibles)
- Fecha de creación
- **Botón de eliminar** en cada notificación con icono de papelera
- Estilo de tarjeta con borde y fondo
- Scroll automático cuando hay muchas notificaciones

Para activar/desactivar el panel de notificaciones, usa el botón "Toggle Notificaciones" en la barra superior.

### Eliminar Notificaciones

Cada notificación incluye un botón de eliminar (icono de papelera) en la esquina superior derecha. Al hacer clic:
- La notificación se elimina inmediatamente
- Si la notificación tenía un tiempo de vida, el timer se cancela automáticamente
- El cambio se refleja instantáneamente en la UI gracias a ObservableCollection

## Migración a Endpoint Real

Cuando esté listo para conectarse a un endpoint real:

1. Actualizar `NotificacionService.MostrarNotificacionAsync` para hacer una llamada HTTP
2. Implementar sincronización de notificaciones desde el servidor
3. Agregar autenticación usando `AuthenticatedHttpHandler`
4. Mantener la colección observable para actualización en tiempo real

Ejemplo:

```csharp
public async Task<NotificacionDto> MostrarNotificacionAsync(
    string titulo, 
    string? nota = null, 
    DateTime? fechaHoraInicio = null, 
    DateTime? fechaHoraFinal = null)
{
    // Crear notificación localmente
    var notificacion = new NotificacionDto { ... };
    
    // Enviar al servidor
    var response = await _httpClient.PostAsJsonAsync("/api/notificaciones", notificacion);
    response.EnsureSuccessStatusCode();
    
    // Obtener ID del servidor
    var notificacionServidor = await response.Content.ReadFromJsonAsync<NotificacionDto>();
    
    // Agregar a colección local
    _notificaciones.Add(notificacionServidor);
    
    return notificacionServidor;
}
```

## Testing

El servicio incluye pruebas unitarias completas en `NotificacionServiceTests.cs`:

- Validación de parámetros requeridos
- Funcionalidad con parámetros opcionales
- Eventos y observables
- Operaciones de gestión (agregar, eliminar, limpiar)
- **Funcionalidad de tiempo de vida y auto-eliminación**
- **Cancelación de timers al eliminar notificaciones**
- Integración con logging

Total: 20+ tests que cubren todos los escenarios

Ejecutar tests:
```bash
dotnet test
```
