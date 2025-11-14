# Sistema de Notificaciones - Documentación

## Resumen

Este documento describe el sistema de notificaciones implementado en Advance Control. El sistema permite mostrar notificaciones desde cualquier parte de la aplicación en un panel colapsable en la ventana principal.

## Componentes Implementados

### 1. Modelo de Datos

**Ubicación:** `Models/Notificacion.cs`

```csharp
public class Notificacion
{
    public string Titulo { get; set; }              // Requerido
    public string? Nota { get; set; }               // Opcional
    public DateTime? FechaHoraInicio { get; set; }  // Opcional
    public DateTime? FechaHoraFinal { get; set; }   // Opcional
    public DateTime FechaCreacion { get; set; }     // Auto-generado
    public bool Leida { get; set; }                 // Default: false
}
```

### 2. Servicio de Notificaciones

**Interfaz:** `Services/Notificacion/INotificationService.cs`

```csharp
public interface INotificationService
{
    ObservableCollection<Notificacion> Notificaciones { get; }
    
    Task AgregarNotificacionAsync(
        string titulo,
        string? nota = null,
        DateTime? fechaHoraInicio = null,
        DateTime? fechaHoraFinal = null);
    
    void MarcarComoLeida(Notificacion notificacion);
    void EliminarNotificacion(Notificacion notificacion);
    void LimpiarNotificaciones();
}
```

**Implementación:** `Services/Notificacion/NotificationService.cs`

- Usa `ObservableCollection` para notificaciones reactivas
- Integrado con `ILoggingService` para auditoría
- Maneja dispatcher queue para contextos UI y no-UI
- Actualmente funciona como mock (sin endpoint)

### 3. Interfaz de Usuario

**Grid Colapsable en MainWindow:**

- **Nombre:** `x:Name="Notificaciones"`
- **Ubicación:** Grid.Row="2" en MainWindow.xaml
- **Estado inicial:** Collapsed
- **Altura máxima:** 300px con scroll vertical

**Funcionalidad UI:**

- Botón "Notificaciones" en header para toggle
- Botón "Limpiar" para eliminar todas las notificaciones
- Botón "✕" por cada notificación para eliminar individualmente
- Lista scrollable con diseño responsive

## Uso del Servicio

### Inyección de Dependencias

El servicio está registrado como Singleton en `App.xaml.cs`:

```csharp
services.AddSingleton<INotificationService, NotificationService>();
```

### Desde ViewModels

```csharp
public class MiViewModel
{
    private readonly INotificationService _notificationService;
    
    public MiViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    public async Task EjemploUso()
    {
        // Notificación simple (solo título requerido)
        await _notificationService.AgregarNotificacionAsync("Tarea completada");
        
        // Notificación completa
        await _notificationService.AgregarNotificacionAsync(
            titulo: "Nueva Reunión",
            nota: "Reunión con el equipo de desarrollo",
            fechaHoraInicio: DateTime.Now.AddHours(1),
            fechaHoraFinal: DateTime.Now.AddHours(2)
        );
    }
}
```

### Desde Code-Behind

```csharp
public class MiVista
{
    private readonly INotificationService _notificationService;
    
    // Obtener del service provider o constructor
    
    private async void OnBotonClick(object sender, RoutedEventArgs e)
    {
        await _notificationService.AgregarNotificacionAsync(
            titulo: "Operación exitosa",
            nota: "Los datos se guardaron correctamente"
        );
    }
}
```

## Ejemplos de Uso

### Notificación de Bienvenida (Login Exitoso)

Implementado en `MainViewModel.ShowLoginDialogAsync()`:

```csharp
await _notificationService.AgregarNotificacionAsync(
    titulo: "Bienvenido",
    nota: $"Inicio de sesión exitoso. Usuario: {loginViewModel.User}",
    fechaHoraInicio: DateTime.Now
);
```

### Notificación de Tarea Completada

```csharp
await _notificationService.AgregarNotificacionAsync(
    titulo: "Tarea Completada",
    nota: "Se procesaron 150 registros exitosamente"
);
```

### Notificación de Evento Programado

```csharp
await _notificationService.AgregarNotificacionAsync(
    titulo: "Mantenimiento Programado",
    nota: "El sistema estará en mantenimiento",
    fechaHoraInicio: DateTime.Now.AddDays(1).Date.AddHours(22),
    fechaHoraFinal: DateTime.Now.AddDays(2).Date.AddHours(2)
);
```

### Gestión de Notificaciones

```csharp
// Marcar como leída
_notificationService.MarcarComoLeida(notificacion);

// Eliminar una notificación
_notificationService.EliminarNotificacion(notificacion);

// Limpiar todas las notificaciones
_notificationService.LimpiarNotificaciones();
```

## Arquitectura

### Flujo de Datos

```
[ViewModel/Service] 
    ↓ (llama)
[INotificationService.AgregarNotificacionAsync()]
    ↓ (agrega a)
[ObservableCollection<Notificacion>]
    ↓ (notifica cambio a)
[MainWindow.xaml ItemsControl]
    ↓ (muestra en)
[UI - Panel de Notificaciones]
```

### Patrón de Diseño

- **Dependency Injection:** El servicio se inyecta donde se necesita
- **Observer Pattern:** ObservableCollection notifica cambios automáticamente
- **Repository Pattern:** El servicio abstrae el almacenamiento (actualmente mock)

## Testing

**Ubicación:** `Advance Control.Tests/Services/NotificationServiceTests.cs`

**Cobertura:**
- ✅ Constructor y validación de dependencias
- ✅ Agregar notificaciones con diferentes combinaciones de parámetros
- ✅ Validación de parámetros requeridos
- ✅ Marcar notificaciones como leídas
- ✅ Eliminar notificaciones
- ✅ Limpiar todas las notificaciones
- ✅ Mantener orden de notificaciones

**Ejecutar tests:**
```bash
dotnet test "Advance Control.Tests/Advance Control.Tests.csproj"
```

## Extensibilidad Futura

### Integración con Endpoint

El servicio está diseñado para ser fácilmente extendido con un endpoint real:

```csharp
public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    
    public async Task AgregarNotificacionAsync(...)
    {
        // 1. Enviar al endpoint
        var response = await _httpClient.PostAsJsonAsync("/api/notifications", notificacion);
        
        // 2. Si es exitoso, agregar localmente
        if (response.IsSuccessStatusCode)
        {
            _notificaciones.Add(notificacion);
        }
    }
    
    // Método para sincronizar con servidor
    public async Task SincronizarNotificacionesAsync()
    {
        var notificaciones = await _httpClient.GetFromJsonAsync<List<Notificacion>>("/api/notifications");
        // Actualizar colección local
    }
}
```

### Notificaciones Push

Se puede extender para soportar notificaciones push del servidor:

```csharp
public class NotificationService : INotificationService
{
    private readonly HubConnection _hubConnection;
    
    public async Task ConectarSignalRAsync()
    {
        _hubConnection.On<Notificacion>("ReceiveNotification", async (notificacion) =>
        {
            await AgregarNotificacionAsync(
                notificacion.Titulo,
                notificacion.Nota,
                notificacion.FechaHoraInicio,
                notificacion.FechaHoraFinal
            );
        });
        
        await _hubConnection.StartAsync();
    }
}
```

## Consideraciones de Rendimiento

1. **ObservableCollection:** Optimizada para cambios en UI
2. **Límite de notificaciones:** Considerar implementar límite máximo
3. **Persistencia:** Las notificaciones actuales solo existen en memoria
4. **Threading:** El servicio maneja correctamente el dispatcher queue

## Seguridad

- ✅ Validación de entrada en `AgregarNotificacionAsync`
- ✅ Manejo de excepciones
- ✅ Logging de operaciones para auditoría
- ✅ Sin vulnerabilidades detectadas (CodeQL)

## Mantenimiento

### Archivos Relacionados

```
Advance Control/
├── Models/
│   └── Notificacion.cs
├── Services/
│   └── Notificacion/
│       ├── INotificationService.cs
│       └── NotificationService.cs
├── ViewModels/
│   └── MainViewModel.cs (integración)
├── Views/
│   ├── MainWindow.xaml (UI)
│   └── MainWindow.xaml.cs (event handlers)
├── Converters/
│   ├── NullToVisibilityConverter.cs
│   ├── CountToVisibilityConverter.cs
│   └── CountToCollapsedConverter.cs
└── App.xaml.cs (registro DI)

Advance Control.Tests/
└── Services/
    └── NotificationServiceTests.cs
```

### Modificaciones Comunes

**Agregar nueva propiedad al modelo:**
1. Actualizar `Notificacion.cs`
2. Actualizar `MainWindow.xaml` DataTemplate
3. Actualizar tests si es necesario

**Cambiar diseño UI:**
1. Modificar `MainWindow.xaml` Grid "Notificaciones"
2. Ajustar estilos y converters según necesidad

**Integrar con endpoint:**
1. Modificar `NotificationService.cs`
2. Agregar HttpClient en constructor
3. Implementar llamadas al endpoint
4. Actualizar tests con mocks de HTTP

## Troubleshooting

### La notificación no aparece

1. Verificar que el Grid "Notificaciones" esté visible (Visibility="Visible")
2. Verificar que el binding sea correcto: `{Binding NotificationService.Notificaciones}`
3. Verificar que el DataContext esté establecido correctamente
4. Revisar logs para ver si hay errores

### Error al agregar notificación

1. Verificar que el título no esté vacío
2. Verificar que el servicio esté inyectado correctamente
3. Revisar logs del `ILoggingService`

### El panel no es colapsable

1. Verificar que el evento `ToggleNotifications_Click` esté conectado
2. Verificar que el Grid tenga `x:Name="Notificaciones"`

## Changelog

### Versión 1.0 (Implementación Inicial)

**Fecha:** 2025-11-14

**Cambios:**
- ✅ Implementación inicial del sistema de notificaciones
- ✅ Grid colapsable en MainWindow
- ✅ Servicio mock con ObservableCollection
- ✅ Notificación de bienvenida en login exitoso
- ✅ Tests unitarios completos
- ✅ Documentación

**Commits:**
- `0d0222a` - Add notification service and collapsible notifications grid
- `6214caf` - Add unit tests for notification service and fix dispatcher queue handling

---

**Última actualización:** 2025-11-14
