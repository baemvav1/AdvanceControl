# Arquitectura del Servicio de Notificaciones

## Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────────┐
│                         App.xaml.cs                              │
│                    (Dependency Injection)                        │
│                                                                   │
│  services.AddSingleton<INotificacionService, NotificacionService>│
└────────────────────┬────────────────────────────────────────────┘
                     │ Registers
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                   INotificacionService                           │
│                        (Interface)                               │
│                                                                   │
│  + MostrarNotificacionAsync(titulo, nota?, inicio?, fin?)       │
│  + ObtenerNotificaciones()                                      │
│  + LimpiarNotificaciones()                                      │
│  + EliminarNotificacion(id)                                     │
└────────────────────┬────────────────────────────────────────────┘
                     │ Implements
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                   NotificacionService                            │
│                   (Mock Implementation)                          │
│                                                                   │
│  - _notificaciones: ObservableCollection<NotificacionDto>       │
│  - _logger: ILoggingService                                     │
│  + NotificacionAgregada: Event                                  │
│  + NotificacionesObservable: Property                           │
└────┬──────────────────┬─────────────────────┬──────────────────┘
     │                  │                     │
     │ Stores           │ Logs to             │ Notifies
     ▼                  ▼                     ▼
┌─────────────┐  ┌──────────────┐  ┌─────────────────────┐
│NotificacionDto│  │LoggingService│  │Event Subscribers   │
│  - Id       │  │              │  │(UI, ViewModels)    │
│  - Titulo   │  └──────────────┘  └─────────────────────┘
│  - Nota     │
│  - Inicio   │
│  - Final    │
│  - Creacion │
└─────────────┘
```

## Flujo de Datos: Login → Notificación

```
┌─────────────────┐
│   LoginView     │  User enters credentials
│   (XAML)        │
└────────┬────────┘
         │ User clicks Login
         ▼
┌─────────────────────────────────────┐
│      LoginViewModel                 │
│  ExecuteLogin()                     │
│   ├─ ValidateCredentials()          │
│   ├─ AuthService.AuthenticateAsync()│
│   ├─ if (success):                  │
│   │   ├─ LoginSuccessful = true     │
│   │   └─ NotificacionService        │
│   │       .MostrarNotificacionAsync()│
└──────────────────┬──────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────┐
│        NotificacionService                 │
│  MostrarNotificacionAsync(                 │
│    titulo: "Bienvenido",                   │
│    nota: "Usuario X ha iniciado sesión",   │
│    fechaHoraInicio: DateTime.Now)          │
│                                            │
│  1. Creates NotificacionDto                │
│  2. Adds to _notificaciones collection     │
│  3. Logs to ILoggingService                │
│  4. Fires NotificacionAgregada event       │
└─────────────────┬──────────────────────────┘
                  │
                  │ Collection Updated
                  ▼
┌─────────────────────────────────────┐
│       MainViewModel                 │
│  Notificaciones: ObservableCollection│
│  (Bound to NotificacionService)     │
└────────────┬────────────────────────┘
             │ Data Binding
             ▼
┌────────────────────────────────────┐
│      MainWindow                    │
│      (Notificaciones Panel)        │
│                                    │
│  ItemsControl displays:           │
│  ┌────────────────────────┐       │
│  │ Bienvenido             │       │
│  │ Usuario X ha iniciado...│      │
│  │ Inicio: 15/11/2025     │       │
│  │ 15/11/2025 14:30       │       │
│  └────────────────────────┘       │
└────────────────────────────────────┘
```

## Inyección de Dependencias

```
┌──────────────────────────────────────────────────────────┐
│                    Service Provider                       │
│                   (DI Container)                          │
└───┬──────────────────────────────────────────────────┬───┘
    │                                                  │
    │ Injects                                         │ Injects
    ▼                                                  ▼
┌────────────────────┐                     ┌──────────────────────┐
│   LoginViewModel   │                     │    MainViewModel     │
│                    │                     │                      │
│ Constructor:       │                     │ Constructor:         │
│  - IAuthService    │                     │  - INavigationService│
│  - ILoggingService │                     │  - IOnlineCheck      │
│  - INotificacion   │                     │  - ILoggingService   │
│    Service         │                     │  - IAuthService      │
└────────────────────┘                     │  - IDialogService    │
                                           │  - IServiceProvider  │
                                           │  - INotificacion     │
                                           │    Service           │
                                           └──────────────────────┘
```

## Capa de Presentación (UI)

```
┌─────────────────────────────────────────────────────────────┐
│                       MainWindow.xaml                        │
│                                                               │
│  ┌─────────────────────┐  ┌────────────────────────────┐    │
│  │   Main Content      │  │  Notification Panel        │    │
│  │   Area              │  │  (Grid Column 1)           │    │
│  │   (Grid Column 0)   │  │                            │    │
│  │                     │  │  ┌──────────────────────┐  │    │
│  │  ┌────────────────┐ │  │  │ NOTIFICACIONES       │  │    │
│  │  │ Navigation     │ │  │  └──────────────────────┘  │    │
│  │  │   - Operaciones│ │  │                            │    │
│  │  │   - Asesoría   │ │  │  <ScrollViewer>           │    │
│  │  │   - Mtto       │ │  │    <ItemsControl          │    │
│  │  │   - Clientes   │ │  │      ItemsSource=         │    │
│  │  └────────────────┘ │  │      "{Binding            │    │
│  │                     │  │       Notificaciones}">   │    │
│  │  <Frame>            │  │                            │    │
│  │    [Content Views]  │  │      <DataTemplate>       │    │
│  │  </Frame>           │  │        <Border>           │    │
│  │                     │  │          <StackPanel>     │    │
│  └─────────────────────┘  │            - Titulo       │    │
│                           │            - Nota         │    │
│                           │            - FechaInicio  │    │
│                           │            - FechaFinal   │    │
│                           │            - Creacion     │    │
│                           │          </StackPanel>    │    │
│                           │        </Border>          │    │
│                           │      </DataTemplate>      │    │
│                           │    </ItemsControl>        │    │
│                           │  </ScrollViewer>          │    │
│                           └────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## Converters de UI

```
┌──────────────────────────────────────────────────────────────┐
│                         App.xaml                              │
│                    (Resource Dictionary)                      │
│                                                                │
│  <converters:NullToVisibilityConverter                        │
│      x:Key="NullToVisibilityConverter" />                     │
│                                                                │
│  <converters:DateTimeFormatConverter                          │
│      x:Key="DateTimeFormatConverter" />                       │
└────────────────┬─────────────────────┬────────────────────────┘
                 │                     │
                 ▼                     ▼
    ┌────────────────────┐   ┌──────────────────────┐
    │NullToVisibility    │   │DateTimeFormat        │
    │Converter           │   │Converter             │
    │                    │   │                      │
    │ null → Collapsed   │   │ DateTime →           │
    │ value → Visible    │   │ "DD/MM/YYYY HH:MM"  │
    │ "" → Collapsed     │   │ null → ""            │
    └────────────────────┘   └──────────────────────┘
                 │                     │
                 └──────────┬──────────┘
                            │ Used in
                            ▼
                ┌────────────────────────┐
                │  MainWindow.xaml       │
                │  DataTemplate          │
                │                        │
                │  Visibility="{Binding  │
                │    Nota, Converter=    │
                │    {StaticResource     │
                │    NullToVisibility}}  │
                │                        │
                │  Text="{Binding        │
                │    FechaCreacion,      │
                │    Converter=          │
                │    {StaticResource     │
                │    DateTimeFormat}}"   │
                └────────────────────────┘
```

## Modelo de Datos

```
┌─────────────────────────────────────────────────────────┐
│                   NotificacionDto                        │
│                   (Data Model)                           │
│                                                           │
│  Properties:                                             │
│  ┌────────────────────────────────────────────────────┐ │
│  │ Guid Id                    [Auto-generated]        │ │
│  │ string Titulo              [Required]              │ │
│  │ string? Nota               [Optional]              │ │
│  │ DateTime? FechaHoraInicio  [Optional]              │ │
│  │ DateTime? FechaHoraFinal   [Optional]              │ │
│  │ DateTime FechaCreacion     [Auto-set to Now]       │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

## Testing Architecture

```
┌────────────────────────────────────────────────────────────┐
│               Advance Control.Tests                         │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  NotificacionServiceTests (15 tests)                │   │
│  │   - Test parameter validation                       │   │
│  │   - Test notification creation                      │   │
│  │   - Test collection management                      │   │
│  │   - Test events                                     │   │
│  │   - Test observable collections                     │   │
│  │   - Test logging integration                        │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  NullToVisibilityConverterTests (7 tests)           │   │
│  │   - Test null → Collapsed                           │   │
│  │   - Test value → Visible                            │   │
│  │   - Test empty string handling                      │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  DateTimeFormatConverterTests (6 tests)             │   │
│  │   - Test DateTime formatting                        │   │
│  │   - Test null handling                              │   │
│  │   - Test edge cases                                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
│  Uses: xUnit, Moq                                           │
└────────────────────────────────────────────────────────────┘
```

## Future: Endpoint Integration

```
Current (Mock):                    Future (Real Endpoint):

┌──────────────────┐              ┌──────────────────┐
│NotificacionService│              │NotificacionService│
│  (Mock)          │              │  (Real)          │
│                  │              │                  │
│ Store in         │              │ POST to API      │
│ ObservableCollection│           │ /api/notificaciones│
│                  │              │                  │
│ No network calls │              │ + HttpClient     │
│                  │              │ + Auth headers   │
└──────────────────┘              │ + Error handling │
                                  │                  │
                                  │ ObservableCollection│
                                  │ synced with      │
                                  │ server data      │
                                  └──────────────────┘

Easy migration:
1. Inject HttpClient
2. Add endpoint URL
3. Implement POST/GET/DELETE
4. Add AuthenticatedHttpHandler
5. Keep UI bindings unchanged
```

## Pattern Summary

### Design Patterns Used:
1. **Dependency Injection** - Service registered in DI container
2. **MVVM** - Clear separation of concerns (Model-View-ViewModel)
3. **Observer Pattern** - ObservableCollection and Events
4. **Repository Pattern** - NotificacionService as data repository
5. **Single Responsibility** - Each class has one clear purpose
6. **Interface Segregation** - Clean interface definition
7. **Dependency Inversion** - Depend on abstractions (INotificacionService)

### SOLID Principles:
✅ **S**ingle Responsibility - Each class has one job  
✅ **O**pen/Closed - Easy to extend (mock → real endpoint)  
✅ **L**iskov Substitution - Interface implementation  
✅ **I**nterface Segregation - Minimal interface  
✅ **D**ependency Inversion - Depend on INotificacionService  

This architecture ensures maintainability, testability, and scalability.
