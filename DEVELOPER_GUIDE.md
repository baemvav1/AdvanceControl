# Guía del Desarrollador - Advance Control

Esta guía proporciona información práctica para desarrolladores que trabajan en el proyecto Advance Control.

## Tabla de Contenidos

1. [Configuración del Entorno](#configuración-del-entorno)
2. [Primeros Pasos](#primeros-pasos)
3. [Estructura del Código](#estructura-del-código)
4. [Patrones de Desarrollo](#patrones-de-desarrollo)
5. [Implementar Nuevas Funcionalidades](#implementar-nuevas-funcionalidades)
6. [Debugging](#debugging)
7. [Mejores Prácticas](#mejores-prácticas)
8. [Recursos Útiles](#recursos-útiles)

---

## Configuración del Entorno

### Requisitos

1. **Visual Studio 2022** (versión 17.0 o superior)
   - Carga de trabajo: "Desarrollo de la Plataforma universal de Windows"
   - Carga de trabajo: ".NET Desktop Development"
   - Componente: Windows 10 SDK (10.0.19041.0 o superior)

2. **.NET 8.0 SDK**
   - Descargar de: https://dotnet.microsoft.com/download/dotnet/8.0

3. **Windows 10/11**
   - Versión mínima: 1809 (build 17763)

### Instalación

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/baemvav1/AdvanceControl.git
   cd AdvanceControl
   ```

2. Restaurar paquetes NuGet:
   ```bash
   dotnet restore "Advance Control/Advance Control.csproj"
   ```

3. Abrir la solución en Visual Studio:
   ```bash
   start "Advance Control.sln"
   ```

4. Configurar `appsettings.json`:
   ```json
   {
     "ExternalApi": {
       "BaseUrl": "https://your-api-url.com/",
       "ApiKey": "your-api-key"
     }
   }
   ```

---

## Primeros Pasos

### Compilar el Proyecto

**Desde Visual Studio:**
- Presionar `Ctrl+Shift+B` o ir a Build > Build Solution

**Desde línea de comandos:**
```bash
dotnet build "Advance Control/Advance Control.csproj"
```

### Ejecutar la Aplicación

**Desde Visual Studio:**
- Presionar `F5` (con debugging) o `Ctrl+F5` (sin debugging)

**Desde línea de comandos:**
```bash
dotnet run --project "Advance Control/Advance Control.csproj"
```

### Configuración de Debug

En Visual Studio, configurar el proyecto de inicio:
1. Click derecho en "Advance Control" en Solution Explorer
2. Seleccionar "Set as Startup Project"

---

## Estructura del Código

### Organización por Capas

```
Advance Control/
├── Views/              # Interfaz de usuario (XAML)
│   ├── MainWindow.xaml
│   └── CustomersView.xaml
│
├── ViewModels/         # Lógica de presentación
│   ├── ViewModelBase.cs
│   ├── MainViewModel.cs
│   └── CustomersViewModel.cs
│
├── Services/           # Lógica de negocio
│   ├── Auth/          # Autenticación
│   ├── OnlineCheck/   # Verificación de conectividad
│   ├── EndPointProvider/ # Construcción de URLs
│   ├── Http/          # Manejo HTTP
│   └── Security/      # Almacenamiento seguro
│
├── Models/             # Modelos de datos
│   ├── CustomerDto.cs
│   └── TokenDto.cs
│
├── Converters/         # Conversores XAML
├── Helpers/            # Utilidades
├── Navigation/         # Servicios de navegación
└── Settings/           # Configuración
```

### Convención de Nombres

- **Clases**: PascalCase (`CustomerService`)
- **Interfaces**: I + PascalCase (`IAuthService`)
- **Métodos**: PascalCase (`GetCustomerAsync`)
- **Propiedades**: PascalCase (`IsAuthenticated`)
- **Campos privados**: _camelCase (`_httpClient`)
- **Constantes**: PascalCase (`MaxRetryCount`)
- **Parámetros**: camelCase (`customerId`)
- **Variables locales**: camelCase (`customerList`)

---

## Patrones de Desarrollo

### 1. MVVM Pattern

**ViewModel básico:**

```csharp
public class MyViewModel : ViewModelBase
{
    private string _message;
    
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }
    
    public ICommand MyCommand { get; }
    
    public MyViewModel()
    {
        MyCommand = new RelayCommand(ExecuteMyCommand);
    }
    
    private void ExecuteMyCommand()
    {
        Message = "Command executed!";
    }
}
```

**View (XAML):**

```xml
<Page DataContext="{Binding MyViewModel}">
    <StackPanel>
        <TextBlock Text="{Binding Message}"/>
        <Button Command="{Binding MyCommand}" Content="Click Me"/>
    </StackPanel>
</Page>
```

### 2. Dependency Injection

**Registrar servicios (App.xaml.cs):**

```csharp
services.AddSingleton<IMyService, MyService>();
services.AddTransient<MyViewModel>();
```

**Inyectar en ViewModel:**

```csharp
public class MyViewModel : ViewModelBase
{
    private readonly IMyService _myService;
    
    public MyViewModel(IMyService myService)
    {
        _myService = myService;
    }
}
```

### 3. Async/Await Pattern

**Operaciones asíncronas:**

```csharp
public async Task LoadDataAsync()
{
    try
    {
        IsBusy = true;
        var data = await _service.GetDataAsync();
        Items = new ObservableCollection<Item>(data);
    }
    catch (Exception ex)
    {
        ErrorMessage = ex.Message;
    }
    finally
    {
        IsBusy = false;
    }
}
```

**Con CancellationToken:**

```csharp
private CancellationTokenSource _cts;

public async Task LoadDataAsync()
{
    _cts?.Cancel();
    _cts = new CancellationTokenSource();
    
    try
    {
        var data = await _service.GetDataAsync(_cts.Token);
        // Procesar datos
    }
    catch (OperationCanceledException)
    {
        // Operación cancelada
    }
}
```

---

## Implementar Nuevas Funcionalidades

### Añadir un Nuevo Servicio

1. **Crear interfaz:**

```csharp
// Services/MyFeature/IMyService.cs
public interface IMyService
{
    Task<Result> DoSomethingAsync(string parameter);
}
```

2. **Implementar servicio:**

```csharp
// Services/MyFeature/MyService.cs
public class MyService : IMyService
{
    private readonly HttpClient _httpClient;
    private readonly IApiEndpointProvider _endpointProvider;
    
    public MyService(HttpClient httpClient, IApiEndpointProvider endpointProvider)
    {
        _httpClient = httpClient;
        _endpointProvider = endpointProvider;
    }
    
    public async Task<Result> DoSomethingAsync(string parameter)
    {
        var url = _endpointProvider.GetEndpoint("my-endpoint");
        var response = await _httpClient.GetAsync(url);
        // Procesar respuesta
    }
}
```

3. **Registrar en DI (App.xaml.cs):**

```csharp
services.AddTransient<IMyService, MyService>();
```

### Añadir una Nueva Vista

1. **Crear XAML + Code-behind:**

```xml
<!-- Views/MyView.xaml -->
<Page x:Class="Advance_Control.Views.MyView">
    <Grid>
        <!-- Contenido -->
    </Grid>
</Page>
```

```csharp
// Views/MyView.xaml.cs
public sealed partial class MyView : Page
{
    public MyViewModel ViewModel { get; }
    
    public MyView()
    {
        this.InitializeComponent();
        // Obtener ViewModel del DI container
        ViewModel = App.GetService<MyViewModel>();
        DataContext = ViewModel;
    }
}
```

2. **Crear ViewModel:**

```csharp
// ViewModels/MyViewModel.cs
public class MyViewModel : ViewModelBase
{
    private readonly IMyService _service;
    
    public MyViewModel(IMyService service)
    {
        _service = service;
    }
}
```

3. **Registrar ViewModel:**

```csharp
// App.xaml.cs
services.AddTransient<MyViewModel>();
```

### Añadir un Modelo DTO

```csharp
// Models/MyDto.cs
public class MyDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## Debugging

### Puntos de Interrupción (Breakpoints)

1. Click en el margen izquierdo del editor para añadir breakpoint
2. `F5` para iniciar debugging
3. `F10` para Step Over
4. `F11` para Step Into
5. `Shift+F11` para Step Out

### Debugging de XAML

**Ver jerarquía visual:**
- Debug > Windows > Live Visual Tree

**Ver propiedades:**
- Debug > Windows > Live Property Explorer

### Logging

**Usar ILogger:**

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
    
    public void DoSomething()
    {
        _logger.LogInformation("Doing something...");
        _logger.LogWarning("Warning: {Message}", message);
        _logger.LogError(ex, "Error occurred");
    }
}
```

**Ver logs:**
- Output window en Visual Studio (Debug > Windows > Output)

### Hot Reload

- Modificar código mientras la app está corriendo
- `Alt+F10` para aplicar cambios
- Funciona con XAML y algunos cambios de código C#

---

## Mejores Prácticas

### 1. Manejo de Errores

**En servicios:**

```csharp
public async Task<Result<T>> GetDataAsync()
{
    try
    {
        // Lógica
        return Result<T>.Success(data);
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "HTTP request failed");
        return Result<T>.Failure("Network error");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        return Result<T>.Failure("Unexpected error occurred");
    }
}
```

**En ViewModels:**

```csharp
private async Task LoadDataAsync()
{
    try
    {
        var result = await _service.GetDataAsync();
        if (result.IsSuccess)
        {
            Items = result.Data;
        }
        else
        {
            await ShowErrorDialogAsync(result.ErrorMessage);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading data");
        await ShowErrorDialogAsync("Failed to load data");
    }
}
```

### 2. Null Safety

**Usar nullable reference types:**

```csharp
public class MyClass
{
    public string Name { get; set; } = string.Empty; // No nullable
    public string? OptionalName { get; set; }        // Nullable
    
    public void Process(string input, string? optionalInput = null)
    {
        if (optionalInput is null)
            return;
            
        // optionalInput no es null aquí
    }
}
```

### 3. Recursos Desechables

**Implementar IDisposable:**

```csharp
public class MyViewModel : ViewModelBase, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

**Usar using para recursos temporales:**

```csharp
using var response = await httpClient.GetAsync(url);
// response se dispone automáticamente
```

### 4. Performance

**Usar ConfigureAwait(false) en servicios:**

```csharp
var data = await _httpClient.GetAsync(url).ConfigureAwait(false);
```

**No usar ConfigureAwait en ViewModels:**

```csharp
// En ViewModels, necesitamos volver al UI thread
var data = await _service.GetDataAsync();
Items = new ObservableCollection<T>(data);
```

**Usar ObservableCollection para binding:**

```csharp
public ObservableCollection<Item> Items { get; set; } = new();
```

### 5. Testing

**Estructura de test:**

```csharp
[TestClass]
public class MyServiceTests
{
    [TestMethod]
    public async Task GetData_WhenSuccessful_ReturnsData()
    {
        // Arrange
        var mockHttp = new Mock<HttpClient>();
        var service = new MyService(mockHttp.Object);
        
        // Act
        var result = await service.GetDataAsync();
        
        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
    }
}
```

---

## Recursos Útiles

### Documentación del Proyecto

- [README.md](./README.md) - Introducción y configuración
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Arquitectura detallada
- [API.md](./API.md) - Documentación de APIs implementadas
- [EMPTY_FILES.md](./EMPTY_FILES.md) - Componentes pendientes

### Documentación Externa

- [WinUI 3 Docs](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)
- [.NET MVVM Toolkit](https://docs.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

### Shortcuts de Visual Studio

| Shortcut | Acción |
|----------|--------|
| `Ctrl+K, Ctrl+D` | Formatear documento |
| `Ctrl+K, Ctrl+C` | Comentar selección |
| `Ctrl+K, Ctrl+U` | Descomentar selección |
| `Ctrl+.` | Quick Actions (sugerencias) |
| `F12` | Ir a definición |
| `Ctrl+F12` | Ir a implementación |
| `Shift+F12` | Buscar todas las referencias |
| `Ctrl+Shift+F` | Buscar en archivos |
| `Ctrl+H` | Buscar y reemplazar |

### Herramientas Recomendadas

1. **Visual Studio Extensions:**
   - ReSharper (análisis de código)
   - XAML Styler (formateo XAML)
   - CodeMaid (limpieza de código)

2. **Herramientas de Desarrollo:**
   - Postman (testing de API)
   - Git Extensions (gestión de Git)
   - LINQPad (testing de consultas LINQ)

3. **Análisis de Código:**
   - SonarLint
   - StyleCop Analyzers

---

## FAQ - Preguntas Frecuentes

### ¿Cómo agrego un nuevo NuGet package?

```bash
dotnet add "Advance Control/Advance Control.csproj" package PackageName
```

### ¿Cómo actualizo todos los packages?

```bash
dotnet list "Advance Control/Advance Control.csproj" package --outdated
dotnet add "Advance Control/Advance Control.csproj" package PackageName
```

### ¿Cómo trabajo con archivos pendientes de implementación?

Ver [EMPTY_FILES.md](./EMPTY_FILES.md) para lista completa y sugerencias de implementación.

### ¿Cómo implemento autenticación?

Seguir el patrón en `Services/OnlineCheck/OnlineCheck.cs` y consultar la documentación en [EMPTY_FILES.md](./EMPTY_FILES.md) sección de autenticación.

### ¿Cómo pruebo sin API real?

Implementar `AuthServiceStub` que simula respuestas de API. Ver sugerencias en [EMPTY_FILES.md](./EMPTY_FILES.md).

---

## Contribuir al Proyecto

1. Crear una rama para tu funcionalidad:
   ```bash
   git checkout -b feature/mi-funcionalidad
   ```

2. Hacer commits descriptivos:
   ```bash
   git commit -m "Add: descripción clara del cambio"
   ```

3. Seguir las convenciones de código del proyecto

4. Agregar XML documentation a APIs públicas

5. Actualizar documentación si es necesario

6. Push y crear Pull Request:
   ```bash
   git push origin feature/mi-funcionalidad
   ```

---

## Soporte

Para preguntas o problemas:
- Revisar la documentación en `/docs`
- Revisar issues existentes en GitHub
- Crear un nuevo issue con descripción detallada
