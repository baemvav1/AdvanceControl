# LISTA DE ERRORES Y MEJORAS - Advance Control

## 📋 ÍNDICE
1. [Errores Críticos](#errores-críticos)
2. [Errores de Diseño](#errores-diseño)
3. [Problemas de Código](#problemas-código)
4. [Mejoras Recomendadas](#mejoras-recomendadas)
5. [Deuda Técnica](#deuda-técnica)

---

## 🔴 1. ERRORES CRÍTICOS {#errores-críticos}

### ERROR-001: Falta implementación de NavigationService.ConfigureFactory
**Ubicación:** `/Advance Control/Navigation/NavigationService.cs`  
**Línea:** No implementado  
**Severidad:** Media  
**Descripción:**  
El método `ConfigureFactory` está documentado en los comentarios del archivo (líneas 77-83) pero no está implementado en la clase.

**Código problemático:**
```csharp
// Documentado pero NO implementado
public void ConfigureFactory(string tag, Func<object> factory)
```

**Solución recomendada:**
Ya existe el método implementado (líneas 79-84). Verificar que funciona correctamente.

**Impacto:**
Bajo - El método sí está implementado, solo hay confusión en la documentación.

---

### ERROR-002: MainViewModel.ShowInfoDialogAsync muestra LoginView incorrectamente
**Ubicación:** `/Advance Control/ViewModels/MainViewModel.cs`  
**Línea:** 167-169  
**Severidad:** Alta  
**Descripción:**  
El método `ShowInfoDialogAsync()` está diseñado para mostrar un diálogo de información pero está configurado para mostrar LoginView, lo cual no tiene sentido semántico y además LoginView no tiene funcionalidad según las especificaciones.

**Código problemático:**
```csharp
public async Task ShowInfoDialogAsync()
{
    await _dialogService.ShowDialogAsync<LoginView>(title: "login", primaryButtonText: "OK");
}
```

**Solución recomendada:**
Eliminar este método o cambiar su propósito. Si se necesita un diálogo de información, crear un UserControl específico:

```csharp
// Opción 1: Eliminar el método (recomendado)
// public async Task ShowInfoDialogAsync() { ... } // ELIMINAR

// Opción 2: Crear un InfoDialogUserControl y usarlo
public async Task ShowInfoDialogAsync(string message)
{
    await _dialogService.ShowDialogAsync<InfoDialogUserControl>(
        configureControl: control => control.Message = message,
        title: "Información", 
        primaryButtonText: "Aceptar"
    );
}
```

**Impacto:**
Medio - No afecta funcionalidad actual pero es confuso y podría causar problemas futuros.

---

### ERROR-003: Views no tienen ViewModels asignados
**Ubicación:** 
- `/Advance Control/Views/Pages/ClientesView.xaml.cs`
- `/Advance Control/Views/Pages/OperacionesView.xaml.cs`
- `/Advance Control/Views/Pages/AcesoriaView.xaml.cs`
- `/Advance Control/Views/Pages/MttoView.xaml.cs`

**Severidad:** Alta  
**Descripción:**  
Las vistas de páginas no tienen ViewModels asignados, por lo que no pueden usar data binding MVVM apropiadamente. Solo tienen constructor vacío.

**Código problemático:**
```csharp
public sealed partial class ClientesView : Page
{
    public ClientesView()
    {
        this.InitializeComponent();
    }
}
```

**Solución recomendada:**
Para cada vista, crear y asignar su ViewModel correspondiente:

```csharp
// Opción 1: Resolver desde DI si el ViewModel está registrado
public sealed partial class ClientesView : Page
{
    public ClientesView()
    {
        this.InitializeComponent();
        
        // Resolver ViewModel desde DI (requiere registrarlo en App.xaml.cs)
        if (App.Current is App app)
        {
            this.DataContext = app.Host.Services.GetRequiredService<CustomersViewModel>();
        }
    }
}

// Opción 2: Crear instancia directa (menos recomendado)
public sealed partial class ClientesView : Page
{
    public ClientesView()
    {
        this.InitializeComponent();
        this.DataContext = new CustomersViewModel();
    }
}
```

**Registrar ViewModels en App.xaml.cs:**
```csharp
// En ConfigureServices
services.AddTransient<CustomersViewModel>();
services.AddTransient<OperacionesViewModel>(); // Crear este
services.AddTransient<AcesoriaViewModel>();    // Crear este
services.AddTransient<MttoViewModel>();        // Crear este
```

**Impacto:**
Alto - Las vistas no pueden usar binding de datos apropiadamente sin ViewModels.

---

### ERROR-004: CustomersViewModel no tiene métodos para cargar datos
**Ubicación:** `/Advance Control/ViewModels/CustomersViewModel.cs`  
**Severidad:** Alta  
**Descripción:**  
El `CustomersViewModel` tiene una colección `Customers` pero no tiene ningún método para cargar datos desde la API o servicio.

**Código problemático:**
```csharp
public class CustomersViewModel : ViewModelBase
{
    private ObservableCollection<CustomerDto> _customers;
    private bool _isLoading;

    public CustomersViewModel()
    {
        _customers = new ObservableCollection<CustomerDto>();
    }
    // ... propiedades pero no hay métodos para cargar datos
}
```

**Solución recomendada:**
Agregar servicio HTTP para clientes y métodos de carga:

```csharp
public class CustomersViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService; // Crear este servicio
    private readonly ILoggingService _logger;
    private ObservableCollection<CustomerDto> _customers;
    private bool _isLoading;
    private string? _errorMessage;

    public CustomersViewModel(ICustomerService customerService, ILoggingService logger)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _customers = new ObservableCollection<CustomerDto>();
    }

    public ObservableCollection<CustomerDto> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public async Task LoadCustomersAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var customers = await _customerService.GetCustomersAsync();
            
            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }

            await _logger.LogInformationAsync(
                $"Cargados {customers.Count} clientes", 
                "CustomersViewModel", 
                "LoadCustomersAsync"
            );
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar clientes";
            await _logger.LogErrorAsync(
                "Error al cargar clientes", 
                ex, 
                "CustomersViewModel", 
                "LoadCustomersAsync"
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshAsync()
    {
        await LoadCustomersAsync();
    }
}
```

**Crear ICustomerService:**
```csharp
public interface ICustomerService
{
    Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateCustomerAsync(CustomerDto customer, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateCustomerAsync(int id, CustomerDto customer, CancellationToken cancellationToken = default);
    Task<bool> DeleteCustomerAsync(int id, CancellationToken cancellationToken = default);
}
```

**Impacto:**
Alto - Sin esto, la vista de Clientes no puede mostrar datos reales.

---

### ERROR-005: LoginView.xaml.cs no tiene implementación funcional
**Ubicación:** `/Advance Control/Views/Login/LoginView.xaml.cs`  
**Severidad:** Media (según especificaciones no se debe cambiar)  
**Descripción:**  
LoginView existe pero no tiene funcionalidad. Según las especificaciones del proyecto, no se harán cambios en este view, pero debe documentarse para desarrollo futuro.

**Estado actual:**
```csharp
public sealed partial class LoginView : UserControl
{
    public LoginView()
    {
        this.InitializeComponent();
    }
}
```

**Documentación para desarrollo futuro:**
```
LoginView requiere:
1. LoginViewModel con propiedades:
   - Username (string)
   - Password (string)
   - ErrorMessage (string)
   - IsLoading (bool)
   - LoginCommand (ICommand)

2. Integración con MainViewModel.LoginAsync()

3. UI en LoginView.xaml con:
   - TextBox para username
   - PasswordBox para password
   - Button para login
   - TextBlock para mensajes de error
   - ProgressRing para loading state

Ver MVVM_ARQUITECTURA.md para ejemplos de implementación.
```

**Impacto:**
Medio - No afecta funcionalidad actual pero es necesario para futuro.

---

## 🟡 2. ERRORES DE DISEÑO {#errores-diseño}

### DISEÑO-001: Falta de servicios para módulos de negocio
**Severidad:** Alta  
**Descripción:**  
Solo existen servicios de infraestructura (Auth, Logging, Navigation) pero no hay servicios para lógica de negocio como Clientes, Operaciones, Asesoría, Mantenimiento.

**Servicios faltantes:**
```
- ICustomerService / CustomerService
- IOperacionesService / OperacionesService
- IAsesoriaService / AsesoriaService
- IMantenimientoService / MantenimientoService
```

**Solución recomendada:**
Crear servicios para cada módulo de negocio siguiendo el patrón de AuthService:

```csharp
// Ejemplo: ICustomerService
public interface ICustomerService
{
    Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateCustomerAsync(CustomerDto customer, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateCustomerAsync(int id, CustomerDto customer, CancellationToken cancellationToken = default);
    Task<bool> DeleteCustomerAsync(int id, CancellationToken cancellationToken = default);
}

// Implementación
public class CustomerService : ICustomerService
{
    private readonly HttpClient _http;
    private readonly IApiEndpointProvider _endpoints;
    private readonly ILoggingService _logger;

    public CustomerService(HttpClient http, IApiEndpointProvider endpoints, ILoggingService logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = _endpoints.GetEndpoint("api", "Customers");
            var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CustomerDto>>(cancellationToken: cancellationToken) 
                   ?? new List<CustomerDto>();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Error al obtener clientes", ex, "CustomerService", "GetCustomersAsync");
            throw;
        }
    }

    // Implementar otros métodos...
}
```

**Registrar en App.xaml.cs:**
```csharp
// Registrar CustomerService con HttpClient tipado
services.AddHttpClient<ICustomerService, CustomerService>((sp, client) =>
{
    var provider = sp.GetRequiredService<IApiEndpointProvider>();
    if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>();
```

**Impacto:**
Alto - Sin estos servicios, las vistas no pueden interactuar con la API.

---

### DISEÑO-002: Falta de ViewModels para todas las vistas
**Severidad:** Alta  
**Descripción:**  
Solo existe `MainViewModel` y `CustomersViewModel`. Faltan ViewModels para:
- OperacionesView
- AcesoriaView  
- MttoView (Mantenimiento)

**Solución recomendada:**
Crear ViewModels siguiendo el patrón de CustomersViewModel:

```csharp
// OperacionesViewModel.cs
public class OperacionesViewModel : ViewModelBase
{
    private readonly IOperacionesService _operacionesService;
    private readonly ILoggingService _logger;
    private bool _isLoading;
    private string? _errorMessage;

    public OperacionesViewModel(IOperacionesService operacionesService, ILoggingService logger)
    {
        _operacionesService = operacionesService ?? throw new ArgumentNullException(nameof(operacionesService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    // Agregar propiedades y métodos específicos del módulo
}

// Crear AsesoriaViewModel y MttoViewModel de forma similar
```

**Impacto:**
Alto - Sin ViewModels, las vistas no pueden implementar MVVM apropiadamente.

---

### DISEÑO-003: Falta de manejo centralizado de errores
**Severidad:** Media  
**Descripción:**  
No hay un sistema centralizado para manejar errores y mostrarlos al usuario de manera consistente.

**Solución recomendada:**
Crear un servicio de manejo de errores:

```csharp
public interface IErrorHandlingService
{
    Task HandleErrorAsync(Exception exception, string context, bool showToUser = true);
    Task ShowErrorToUserAsync(string message, string? details = null);
    Task ShowWarningToUserAsync(string message);
    Task ShowSuccessToUserAsync(string message);
}

public class ErrorHandlingService : IErrorHandlingService
{
    private readonly IDialogService _dialogService;
    private readonly ILoggingService _logger;

    public ErrorHandlingService(IDialogService dialogService, ILoggingService logger)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleErrorAsync(Exception exception, string context, bool showToUser = true)
    {
        // Log error
        await _logger.LogErrorAsync($"Error en {context}", exception, "ErrorHandlingService", "HandleErrorAsync");

        // Show to user if requested
        if (showToUser)
        {
            var message = GetUserFriendlyMessage(exception);
            await ShowErrorToUserAsync(message, exception.Message);
        }
    }

    public async Task ShowErrorToUserAsync(string message, string? details = null)
    {
        // Crear un ErrorMessageUserControl y mostrarlo
        await _dialogService.ShowDialogAsync<ErrorMessageUserControl>(
            configureControl: control => 
            {
                control.Message = message;
                control.Details = details;
            },
            title: "Error",
            primaryButtonText: "Aceptar"
        );
    }

    private string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => "No se pudo conectar con el servidor. Verifique su conexión.",
            TaskCanceledException => "La operación tardó demasiado tiempo y fue cancelada.",
            UnauthorizedAccessException => "No tiene permisos para realizar esta operación.",
            _ => "Ocurrió un error inesperado. Por favor, intente nuevamente."
        };
    }

    // Implementar otros métodos...
}
```

**Impacto:**
Medio - Mejora significativamente la experiencia del usuario.

---

### DISEÑO-004: Falta de validación en modelos
**Severidad:** Media  
**Descripción:**  
Los DTOs como `CustomerDto`, `TokenDto` no tienen validación de datos.

**Solución recomendada:**
Agregar validación usando Data Annotations o FluentValidation:

```csharp
// Opción 1: Data Annotations
public class CustomerDto
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string? Name { get; set; }
    
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El formato de email no es válido")]
    public string? Email { get; set; }
    
    [Phone(ErrorMessage = "El formato de teléfono no es válido")]
    public string? Phone { get; set; }
    
    public DateTime? CreatedAt { get; set; }
}

// Opción 2: FluentValidation (recomendado)
public class CustomerDtoValidator : AbstractValidator<CustomerDto>
{
    public CustomerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");
            
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El formato de email no es válido");
            
        RuleFor(x => x.Phone)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("El formato de teléfono no es válido");
    }
}
```

**Impacto:**
Medio - Previene datos inválidos en el sistema.

---

### DISEÑO-005: Falta de manejo de estado de la aplicación
**Severidad:** Baja  
**Descripción:**  
No hay un servicio para manejar el estado global de la aplicación (usuario actual, configuración, etc.).

**Solución recomendada:**
Crear un servicio de estado:

```csharp
public interface IApplicationStateService
{
    // Usuario actual
    string? CurrentUsername { get; }
    bool IsAuthenticated { get; }
    
    // Configuración
    Task<T?> GetSettingAsync<T>(string key);
    Task SetSettingAsync<T>(string key, T value);
    
    // Eventos
    event EventHandler<AuthenticationStateChangedEventArgs> AuthenticationStateChanged;
    event EventHandler<SettingChangedEventArgs> SettingChanged;
}
```

**Impacto:**
Bajo - Es útil pero no crítico para el funcionamiento básico.

---

## 🟠 3. PROBLEMAS DE CÓDIGO {#problemas-código}

### CODIGO-001: Uso inconsistente de logging
**Ubicación:** Varios archivos  
**Severidad:** Baja  
**Descripción:**  
Algunos servicios usan `_logger?.LogXxxAsync()` (con null-conditional) y otros no. Debería ser consistente.

**Ejemplos inconsistentes:**
```csharp
// En AuthenticatedHttpHandler.cs
_ = _logger?.LogWarningAsync(...);  // Usando null-conditional y descartando Task

// En SecretStorageWindows.cs
_ = _logger?.LogDebugAsync(...);    // Usando null-conditional y descartando Task

// En OnlineCheck.cs
if (_logger != null)
    await _logger.LogErrorAsync(...); // Verificando null y await
```

**Solución recomendada:**
Estandarizar el patrón en todos los archivos:

```csharp
// Patrón recomendado: Usar null-conditional con discard cuando no importa el resultado
_ = _logger?.LogInformationAsync(...);

// Patrón recomendado: Usar await cuando sí importa
if (_logger != null)
    await _logger.LogErrorAsync(...);
```

**Impacto:**
Bajo - Es más un problema de consistencia que funcional.

---

### CODIGO-002: Magic strings en configuración de rutas
**Ubicación:** `/Advance Control/ViewModels/MainViewModel.cs`  
**Línea:** 73-76  
**Severidad:** Baja  
**Descripción:**  
Las rutas están hardcodeadas como strings, lo que puede causar errores si hay typos.

**Código problemático:**
```csharp
_navigationService.Configure<Views.OperacionesView>("Operaciones");
_navigationService.Configure<Views.AcesoriaView>("Asesoria");
_navigationService.Configure<Views.MttoView>("Mantenimiento");
_navigationService.Configure<Views.ClientesView>("Clientes");
```

**Solución recomendada:**
Crear constantes para las rutas:

```csharp
// Crear clase de constantes
public static class NavigationRoutes
{
    public const string Operaciones = "Operaciones";
    public const string Asesoria = "Asesoria";
    public const string Mantenimiento = "Mantenimiento";
    public const string Clientes = "Clientes";
}

// Usar en MainViewModel
_navigationService.Configure<Views.OperacionesView>(NavigationRoutes.Operaciones);
_navigationService.Configure<Views.AcesoriaView>(NavigationRoutes.Asesoria);
_navigationService.Configure<Views.MttoView>(NavigationRoutes.Mantenimiento);
_navigationService.Configure<Views.ClientesView>(NavigationRoutes.Clientes);
```

**Impacto:**
Bajo - Previene errores tipográficos.

---

### CODIGO-003: Falta de using statements para IDisposable
**Ubicación:** Varios archivos  
**Severidad:** Baja  
**Descripción:**  
Algunos objetos IDisposable como `HttpRequestMessage` no se disponen apropiadamente.

**Código problemático en AuthenticatedHttpHandler.cs:**
```csharp
// Línea 31: HttpRequestMessage no se dispone si hay excepción
using (var req = new HttpRequestMessage(HttpMethod.Head, endpoint))
{
    var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    
    if (resp.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
    {
        resp.Dispose();
        resp = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    }
    // ...
}
```

**Solución recomendada:**
Ya está usando `using` correctamente. Verificar que todos los IDisposable se manejen apropiadamente.

**Impacto:**
Bajo - Ya está bien manejado en la mayoría de casos.

---

### CODIGO-004: Falta de cancellation token en algunos métodos
**Ubicación:** Varios ViewModels  
**Severidad:** Baja  
**Descripción:**  
Algunos métodos async no aceptan `CancellationToken` para permitir cancelación de operaciones largas.

**Solución recomendada:**
Agregar parámetro CancellationToken en métodos públicos async:

```csharp
// Antes
public async Task LoadCustomersAsync()

// Después
public async Task LoadCustomersAsync(CancellationToken cancellationToken = default)
{
    // ...
    var customers = await _customerService.GetCustomersAsync(cancellationToken);
    // ...
}
```

**Impacto:**
Bajo - Mejora responsividad pero no es crítico.

---

### CODIGO-005: Namespace inconsistente con nombre de proyecto
**Ubicación:** Todos los archivos  
**Severidad:** Baja (Informativo)  
**Descripción:**  
El proyecto se llama "Advance Control" (con espacio) pero el namespace es `Advance_Control` (con underscore). Esto es correcto ya que los espacios no son válidos en namespaces, pero puede causar confusión.

**Recomendación:**
Mantener tal como está. Es la forma correcta de manejar nombres con espacios en C#. Considerar renombrar el proyecto a "AdvanceControl" (sin espacio) en una futura refactorización mayor.

**Impacto:**
Ninguno - Es solo informativo.

---

## ✅ 4. MEJORAS RECOMENDADAS {#mejoras-recomendadas}

### MEJORA-001: Agregar caché para reducir llamadas a la API
**Prioridad:** Media  
**Descripción:**  
Implementar caché en memoria para datos que no cambian frecuentemente (lista de clientes, catálogos, etc.).

**Implementación sugerida:**
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task ClearAsync();
}

public class MemoryCacheService : ICacheService
{
    private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public Task<T?> GetAsync<T>(string key)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    // Implementar otros métodos...
}
```

**Uso en CustomerService:**
```csharp
public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
{
    const string cacheKey = "customers_list";
    
    // Intentar obtener del caché
    var cached = await _cacheService.GetAsync<List<CustomerDto>>(cacheKey);
    if (cached != null)
    {
        await _logger.LogDebugAsync("Clientes obtenidos del caché", "CustomerService", "GetCustomersAsync");
        return cached;
    }

    // Si no está en caché, obtener de la API
    var customers = await FetchCustomersFromApiAsync(cancellationToken);
    
    // Guardar en caché por 5 minutos
    await _cacheService.SetAsync(cacheKey, customers, TimeSpan.FromMinutes(5));
    
    return customers;
}
```

**Beneficios:**
- Reduce carga en el servidor
- Mejora tiempo de respuesta
- Reduce uso de ancho de banda

---

### MEJORA-002: Implementar retry policy con Polly
**Prioridad:** Media  
**Descripción:**  
Agregar reintentos automáticos para operaciones HTTP que fallan por problemas transitorios.

**Implementación sugerida:**
```csharp
// Instalar NuGet: Microsoft.Extensions.Http.Polly

// En App.xaml.cs, ConfigureServices
services.AddHttpClient<ICustomerService, CustomerService>((sp, client) =>
{
    var provider = sp.GetRequiredService<IApiEndpointProvider>();
    if (Uri.TryCreate(provider.GetApiBaseUrl(), UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<Services.Http.AuthenticatedHttpHandler>()
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            // Log retry attempt
            Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s");
        }
    ))
.AddTransientHttpErrorPolicy(policy => 
    policy.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30)
    ));
```

**Beneficios:**
- Mayor resiliencia ante fallos transitorios
- Mejor experiencia de usuario
- Circuit breaker previene sobrecarga del servidor

---

### MEJORA-003: Agregar Unit Tests
**Prioridad:** Alta  
**Descripción:**  
Crear proyecto de tests unitarios para servicios y ViewModels.

**Estructura sugerida:**
```
AdvanceControl.Tests/
├── Services/
│   ├── AuthServiceTests.cs
│   ├── CustomerServiceTests.cs
│   ├── NavigationServiceTests.cs
│   └── LoggingServiceTests.cs
├── ViewModels/
│   ├── MainViewModelTests.cs
│   └── CustomersViewModelTests.cs
└── Helpers/
    └── MockHelpers.cs
```

**Ejemplo de test:**
```csharp
public class AuthServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange
        var mockHttp = new Mock<HttpClient>();
        var mockEndpoints = new Mock<IApiEndpointProvider>();
        var mockStorage = new Mock<ISecureStorage>();
        var mockLogger = new Mock<ILoggingService>();
        
        var authService = new AuthService(
            mockHttp.Object,
            mockEndpoints.Object,
            mockStorage.Object,
            mockLogger.Object
        );

        // Act
        var result = await authService.AuthenticateAsync("testuser", "testpass");

        // Assert
        Assert.True(result);
        mockStorage.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeast(2));
    }
}
```

**Beneficios:**
- Detecta bugs temprano
- Facilita refactorización
- Documenta comportamiento esperado

---

### MEJORA-004: Implementar Command pattern para ViewModels
**Prioridad:** Media  
**Descripción:**  
Usar `CommunityToolkit.Mvvm` (ya está instalado) para implementar comandos en ViewModels.

**Implementación sugerida:**
```csharp
using CommunityToolkit.Mvvm.Input;

public partial class CustomersViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;
    private readonly ILoggingService _logger;
    private ObservableCollection<CustomerDto> _customers;
    private bool _isLoading;
    private CustomerDto? _selectedCustomer;

    // Propiedades...

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                // Notificar que los comandos deben reevaluarse
                DeleteCustomerCommand.NotifyCanExecuteChanged();
                EditCustomerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    // Comandos con source generators de CommunityToolkit.Mvvm
    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        IsLoading = true;
        try
        {
            var customers = await _customerService.GetCustomersAsync();
            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCustomer))]
    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;
        
        var success = await _customerService.DeleteCustomerAsync(SelectedCustomer.Id);
        if (success)
        {
            Customers.Remove(SelectedCustomer);
        }
    }

    private bool CanDeleteCustomer() => SelectedCustomer != null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanEditCustomer))]
    private async Task EditCustomerAsync()
    {
        // Abrir diálogo de edición
    }

    private bool CanEditCustomer() => SelectedCustomer != null && !IsLoading;
}
```

**Uso en XAML:**
```xml
<Button Content="Cargar" Command="{Binding LoadCustomersCommand}" />
<Button Content="Eliminar" Command="{Binding DeleteCustomerCommand}" />
<Button Content="Editar" Command="{Binding EditCustomerCommand}" />
```

**Beneficios:**
- Código más limpio y mantenible
- Binding directo desde XAML
- CanExecute automático para habilitar/deshabilitar botones

---

### MEJORA-005: Agregar indicadores de progreso en UI
**Prioridad:** Media  
**Descripción:**  
Mostrar ProgressRing o ProgressBar durante operaciones largas.

**Implementación sugerida en XAML:**
```xml
<Grid>
    <!-- Contenido principal -->
    <ListView ItemsSource="{Binding Customers}" 
              Visibility="{Binding IsLoading, Converter={StaticResource InverseBoolToVisibilityConverter}}">
        <!-- ... -->
    </ListView>

    <!-- Indicador de carga -->
    <StackPanel VerticalAlignment="Center" 
                HorizontalAlignment="Center"
                Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}">
        <ProgressRing IsActive="True" Width="50" Height="50" />
        <TextBlock Text="Cargando..." Margin="0,10,0,0" />
    </StackPanel>

    <!-- Mensaje de error -->
    <InfoBar Severity="Error"
             IsOpen="{Binding ErrorMessage, Converter={StaticResource StringToBoolConverter}}"
             Message="{Binding ErrorMessage}"
             IsClosable="True" />
</Grid>
```

**Crear InverseBoolToVisibilityConverter:**
```csharp
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue && boolValue 
            ? Visibility.Collapsed 
            : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
```

**Beneficios:**
- Mejor feedback al usuario
- Previene confusión durante operaciones largas

---

### MEJORA-006: Implementar logging local como fallback
**Prioridad:** Baja  
**Descripción:**  
Si el envío de logs al servidor falla, guardar logs localmente en archivo.

**Implementación sugerida:**
```csharp
public class LoggingService : ILoggingService
{
    private readonly HttpClient _http;
    private readonly IApiEndpointProvider _endpoints;
    private readonly string _localLogPath;

    public LoggingService(HttpClient http, IApiEndpointProvider endpoints)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        
        // Ruta para logs locales
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logFolder = Path.Combine(appData, "AdvanceControl", "Logs");
        Directory.CreateDirectory(logFolder);
        _localLogPath = Path.Combine(logFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");
    }

    public async Task LogAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        if (logEntry == null) return;

        try
        {
            // Intentar enviar al servidor
            var url = _endpoints.GetEndpoint("api", "Logging", "log");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await _http.PostAsJsonAsync(url, logEntry, cts.Token);
        }
        catch
        {
            // Si falla, guardar localmente
            await SaveLogLocallyAsync(logEntry);
        }
    }

    private async Task SaveLogLocallyAsync(LogEntry logEntry)
    {
        try
        {
            var logLine = $"{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss} [{logEntry.Level}] {logEntry.Source}.{logEntry.Method}: {logEntry.Message}";
            if (!string.IsNullOrEmpty(logEntry.Exception))
            {
                logLine += $"\nException: {logEntry.Exception}";
            }
            
            await File.AppendAllTextAsync(_localLogPath, logLine + Environment.NewLine);
        }
        catch
        {
            // Si incluso esto falla, no hacer nada para no afectar la aplicación
        }
    }
}
```

**Beneficios:**
- No se pierden logs importantes
- Útil para debugging en producción

---

### MEJORA-007: Agregar configuración de entornos (Dev, QA, Prod)
**Prioridad:** Media  
**Descripción:**  
Permitir múltiples configuraciones de appsettings para diferentes entornos.

**Implementación sugerida:**
```csharp
// En App.xaml.cs, ConfigureAppConfiguration
.ConfigureAppConfiguration((context, cfg) =>
{
    var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
    
    cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    cfg.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
    
    // User secrets solo en desarrollo
    if (environment == "Development")
    {
        cfg.AddUserSecrets<App>();
    }
})
```

**Crear archivos de configuración:**
```json
// appsettings.Development.json
{
  "ExternalApi": {
    "BaseUrl": "https://localhost:7055/api/",
    "ApiKey": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}

// appsettings.Production.json
{
  "ExternalApi": {
    "BaseUrl": "https://api.production.com/api/",
    "ApiKey": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

**Beneficios:**
- Configuración específica por entorno
- Secrets no se commitean al repo
- Fácil deployment

---

## 📊 5. DEUDA TÉCNICA {#deuda-técnica}

### DEUDA-001: Documentación XML comments incompleta
**Descripción:**  
Algunos métodos y clases públicas no tienen comentarios XML para documentación.

**Recomendación:**
Agregar XML comments a todas las APIs públicas:

```csharp
/// <summary>
/// Servicio para gestionar operaciones de clientes.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Obtiene la lista completa de clientes.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar la operación</param>
    /// <returns>Lista de clientes</returns>
    /// <exception cref="HttpRequestException">Si hay error de red</exception>
    Task<List<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
}
```

---

### DEUDA-002: Falta de internacionalización (i18n)
**Descripción:**  
Todos los strings están hardcodeados en español. No hay soporte para múltiples idiomas.

**Recomendación futura:**
Implementar sistema de recursos para i18n:

```csharp
// Crear Resources.resx, Resources.es.resx, Resources.en.resx
public static class Strings
{
    public static string GetString(string key)
    {
        // Obtener string del recurso según idioma actual
        return Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
    }
}

// Uso
var message = Strings.GetString("ErrorLoadingCustomers");
```

---

### DEUDA-003: Falta de telemetría y analytics
**Descripción:**  
No hay sistema para rastrear uso de la aplicación, errores frecuentes, performance, etc.

**Recomendación futura:**
Integrar Application Insights o similar para telemetría.

---

### DEUDA-004: Falta de documentación de API endpoints
**Descripción:**  
No hay documentación de qué endpoints espera cada servicio del backend.

**Recomendación:**
Crear documento de especificación de API o usar Swagger/OpenAPI.

---

## 📋 RESUMEN DE PRIORIDADES

### 🔴 Alta Prioridad (Debe hacerse pronto)
1. ERROR-003: Asignar ViewModels a todas las vistas
2. ERROR-004: Implementar carga de datos en CustomersViewModel
3. DISEÑO-001: Crear servicios para módulos de negocio
4. DISEÑO-002: Crear ViewModels faltantes
5. MEJORA-003: Agregar Unit Tests

### 🟡 Media Prioridad (Debe hacerse eventualmente)
6. ERROR-002: Corregir ShowInfoDialogAsync en MainViewModel
7. DISEÑO-003: Implementar manejo centralizado de errores
8. DISEÑO-004: Agregar validación en modelos
9. MEJORA-001: Implementar caché
10. MEJORA-002: Implementar retry policy con Polly
11. MEJORA-004: Usar Command pattern
12. MEJORA-005: Agregar indicadores de progreso
13. MEJORA-007: Configuración de entornos

### 🟢 Baja Prioridad (Nice to have)
14. ERROR-001: Verificar método ConfigureFactory
15. CODIGO-001: Estandarizar uso de logging
16. CODIGO-002: Usar constantes en lugar de magic strings
17. CODIGO-004: Agregar CancellationToken a más métodos
18. MEJORA-006: Logging local como fallback
19. DEUDA-001 a DEUDA-004: Documentación y mejoras futuras

---

## ✅ CHECKLIST DE PREPARACIÓN PARA DESARROLLO FUTURO

- [ ] Crear servicios HTTP para todos los módulos (Customers, Operaciones, Asesoría, Mantenimiento)
- [ ] Crear ViewModels para todas las vistas
- [ ] Asignar ViewModels a vistas en constructores
- [ ] Implementar métodos de carga de datos en ViewModels
- [ ] Agregar manejo centralizado de errores
- [ ] Implementar comandos con CommunityToolkit.Mvvm
- [ ] Agregar indicadores de progreso en todas las vistas
- [ ] Crear Unit Tests para servicios y ViewModels
- [ ] Implementar validación de datos
- [ ] Documentar API endpoints esperados
- [ ] Agregar configuración de entornos
- [ ] Implementar sistema de caché
- [ ] Agregar retry policies
- [ ] Crear UserControls para diálogos comunes (Error, Success, Confirmation)
- [ ] Documentar proceso de desarrollo de nuevos módulos

---

**Nota final:** Esta lista está organizada para que otro agente pueda tomar cada item y trabajar en él de forma independiente. Cada error/mejora tiene contexto suficiente, código de ejemplo, y explicación de impacto.
