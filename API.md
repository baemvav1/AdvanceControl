# API Documentation - Servicios Implementados

Esta documentación describe los servicios que ya están implementados en el proyecto Advance Control.

## Tabla de Contenidos

1. [OnlineCheck Service](#onlinecheck-service)
2. [ApiEndpointProvider Service](#apiendpointprovider-service)
3. [Converters](#converters)

---

## OnlineCheck Service

**Namespace**: `Advance_Control.Services.OnlineCheck`

**Propósito**: Verificar la disponibilidad y conectividad con la API externa.

### IOnlineCheck

Interfaz que define el contrato para verificación de conectividad.

#### Métodos

##### `Task<OnlineCheckResult> CheckAsync(CancellationToken cancellationToken = default)`

Comprueba la conexión contra la URL configurada de la API.

**Parámetros**:
- `cancellationToken` (opcional): Token para cancelar la operación

**Retorna**: 
- `OnlineCheckResult` con el estado de la conexión

**Comportamiento**:
- Devuelve `IsOnline = true` si la respuesta HTTP es 2xx
- Devuelve `IsOnline = false` con detalles en caso de error

**Ejemplo de uso**:
```csharp
public class MyViewModel
{
    private readonly IOnlineCheck _onlineCheck;
    
    public MyViewModel(IOnlineCheck onlineCheck)
    {
        _onlineCheck = onlineCheck;
    }
    
    public async Task CheckConnectionAsync()
    {
        var result = await _onlineCheck.CheckAsync();
        
        if (result.IsOnline)
        {
            Console.WriteLine("API está disponible");
        }
        else
        {
            Console.WriteLine($"API no disponible: {result.ErrorMessage}");
        }
    }
}
```

---

### OnlineCheck (Implementación)

Implementación concreta de `IOnlineCheck`.

#### Constructor

```csharp
public OnlineCheck(HttpClient httpClient, IApiEndpointProvider endpointProvider)
```

**Dependencias**:
- `HttpClient`: Cliente HTTP para realizar peticiones
- `IApiEndpointProvider`: Proveedor de endpoints para obtener la URL

#### Funcionamiento Interno

1. Obtiene el endpoint "Online" usando `IApiEndpointProvider`
2. Intenta hacer una petición HEAD (más ligera)
3. Si HEAD no está soportado (405), hace fallback a GET
4. Evalúa el código de estado HTTP
5. Retorna resultado estructurado

**Códigos de estado**:
- `200-299`: Éxito (`IsOnline = true`)
- Otros: Fallo (`IsOnline = false`, con código de estado)
- Excepciones: Fallo (`IsOnline = false`, con mensaje de error)

**Optimizaciones**:
- Usa `HttpCompletionOption.ResponseHeadersRead` para no descargar cuerpo de respuesta
- Prefiere HEAD sobre GET para reducir transferencia de datos
- Maneja correctamente cancelación de operaciones

**Excepciones manejadas**:
- `OperationCanceledException`: Operación cancelada por usuario
- Otras excepciones: DNS, connection refused, TLS/SSL, timeout, etc.

---

### OnlineCheckResult

Clase que representa el resultado de una verificación de conectividad.

#### Propiedades

```csharp
public bool IsOnline { get; set; }      // Indica si la API está disponible
public int? StatusCode { get; set; }    // Código de estado HTTP (si aplica)
public string ErrorMessage { get; set; } // Mensaje de error (si hay fallo)
```

#### Métodos Estáticos

##### `OnlineCheckResult Success()`

Crea un resultado exitoso.

**Retorna**:
```csharp
{
    IsOnline = true,
    StatusCode = 200,
    ErrorMessage = null
}
```

##### `OnlineCheckResult FromHttpStatus(int statusCode, string errorMessage = null)`

Crea un resultado basado en código de estado HTTP.

**Parámetros**:
- `statusCode`: Código HTTP recibido
- `errorMessage` (opcional): Mensaje descriptivo del error

**Retorna**:
- `IsOnline = true` si statusCode está entre 200-299
- `IsOnline = false` en caso contrario

**Ejemplo**:
```csharp
var result = OnlineCheckResult.FromHttpStatus(404, "Endpoint not found");
// IsOnline = false, StatusCode = 404, ErrorMessage = "Endpoint not found"
```

##### `OnlineCheckResult FromException(string message)`

Crea un resultado de error basado en una excepción.

**Parámetros**:
- `message`: Mensaje de error de la excepción

**Retorna**:
```csharp
{
    IsOnline = false,
    StatusCode = null,
    ErrorMessage = message
}
```

**Ejemplo**:
```csharp
var result = OnlineCheckResult.FromException("Connection timed out");
// IsOnline = false, StatusCode = null, ErrorMessage = "Connection timed out"
```

---

## ApiEndpointProvider Service

**Namespace**: `Advance_Control.Services.EndPointProvider`

**Propósito**: Construir URLs absolutas combinando la URL base de configuración con rutas relativas.

### IApiEndpointProvider

Interfaz que define el contrato para construcción de endpoints.

#### Métodos

##### `string GetEndpoint(string routeRelative)`

Devuelve la URI absoluta para la ruta relativa provista.

**Parámetros**:
- `routeRelative`: Ruta relativa al endpoint (ej: "Online", "auth/login", "customers/123")

**Retorna**: URL completa como string

**Ejemplo**:
```csharp
// Asumiendo BaseUrl = "https://api.example.com/"
var url = provider.GetEndpoint("customers/123");
// Resultado: "https://api.example.com/customers/123"
```

##### `string GetEndpoint(params string[] routeParts)`

Variante que permite pasar partes de la ruta como parámetros separados.

**Parámetros**:
- `routeParts`: Array de partes de la ruta

**Retorna**: URL completa como string

**Ejemplo**:
```csharp
// Asumiendo BaseUrl = "https://api.example.com/"
var url = provider.GetEndpoint("customers", "123", "orders");
// Resultado: "https://api.example.com/customers/123/orders"
```

---

### ApiEndpointProvider (Implementación)

Implementación concreta de `IApiEndpointProvider`.

#### Constructor

```csharp
public ApiEndpointProvider(IOptions<ExternalApiOptions> options)
```

**Dependencias**:
- `IOptions<ExternalApiOptions>`: Opciones de configuración de la API externa

**Validaciones en constructor**:
- Lanza `ArgumentNullException` si options es null
- Lanza `ArgumentException` si `ExternalApi:BaseUrl` no está configurado

#### Funcionamiento Interno

**Normalización de URLs**:
- Asegura que BaseUrl termina con `/`
- Elimina `/` inicial de rutas relativas
- Evita barras dobles en la concatenación
- Usa `Uri` de .NET para combinar correctamente las partes

**Método privado `Combine`**:
```csharp
private static string Combine(string baseUrl, string relative)
{
    // 1. Normaliza baseUrl para terminar con /
    // 2. Normaliza relative sin / inicial
    // 3. Combina usando new Uri() para manejo correcto
}
```

**Validaciones**:
- `GetEndpoint(string)`: Lanza excepción si routeRelative es null o vacío
- `GetEndpoint(params string[])`: Lanza excepción si routeParts es null o vacío
- Filtra partes vacías en el método de parámetros múltiples

**Ejemplos de uso**:
```csharp
// Configuración en appsettings.json:
// "ExternalApi": { "BaseUrl": "https://api.example.com" }

var provider = new ApiEndpointProvider(options);

// Uso básico
var url1 = provider.GetEndpoint("customers");
// "https://api.example.com/customers"

// Con subrutas
var url2 = provider.GetEndpoint("auth/login");
// "https://api.example.com/auth/login"

// Con parámetros múltiples
var url3 = provider.GetEndpoint("customers", "123", "details");
// "https://api.example.com/customers/123/details"

// Maneja barras correctamente
var url4 = provider.GetEndpoint("/customers/");
// "https://api.example.com/customers"
```

---

### ExternalApiOptions

Clase de configuración para opciones de API externa.

#### Propiedades

```csharp
public string BaseUrl { get; set; }  // URL base de la API
public string ApiKey { get; set; }   // Clave API (opcional)
```

#### Configuración en appsettings.json

```json
{
  "ExternalApi": {
    "BaseUrl": "https://api.example.com/",
    "ApiKey": "your-api-key-here"
  }
}
```

#### Registro en DI

```csharp
// En App.xaml.cs o Startup
builder.Services.Configure<ExternalApiOptions>(
    configuration.GetSection("ExternalApi")
);
```

---

## Converters

**Namespace**: `AdvanceControl.Converters`

### BooleanToVisibilityConverter

Conversor para usar en data binding XAML que convierte valores booleanos a `Visibility`.

**Hereda de**: `IValueConverter`

#### Método Convert

Convierte un booleano a `Visibility`.

**Parámetros**:
- `value`: Valor booleano a convertir
- `parameter`: Parámetro opcional para modificar comportamiento
  - `"invert"`: Invierte la lógica (true → Collapsed, false → Visible)
  - `"UseHidden"`: Usa `Hidden` en lugar de `Collapsed`

**Retorna**:
- `Visibility.Visible` si el booleano es true
- `Visibility.Collapsed` si el booleano es false
- `Visibility.Collapsed` si el valor no es booleano

**Ejemplo de uso en XAML**:
```xml
<Page.Resources>
    <converters:BooleanToVisibilityConverter x:Key="BoolToVisibility"/>
</Page.Resources>

<!-- Uso normal -->
<TextBlock Text="Visible si IsLoading es true"
           Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}"/>

<!-- Invertido -->
<TextBlock Text="Visible si IsLoading es false"
           Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}, ConverterParameter=invert}"/>
```

#### Método ConvertBack

Convierte `Visibility` de vuelta a booleano.

**Parámetros**:
- `value`: Valor `Visibility` a convertir

**Retorna**:
- `true` si visibility es `Visible`
- `false` si visibility es `Collapsed` o `Hidden`
- `null` si el valor no es `Visibility`

**Uso típico**: En two-way binding

```xml
<CheckBox IsChecked="{Binding IsVisible, Mode=TwoWay}"
          Visibility="{Binding IsEnabled, Converter={StaticResource BoolToVisibility}}"/>
```

---

## Registro de Servicios en DI

### Configuración Recomendada

```csharp
// En App.xaml.cs
public partial class App : Application
{
    private readonly IHost _host;
    
    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Configuración
                services.Configure<ExternalApiOptions>(
                    context.Configuration.GetSection("ExternalApi")
                );
                
                // Servicios
                services.AddSingleton<IApiEndpointProvider, ApiEndpointProvider>();
                services.AddTransient<IOnlineCheck, OnlineCheck>();
                
                // HttpClient
                services.AddHttpClient();
            })
            .Build();
    }
}
```

### Ciclos de Vida

- **Singleton**: `IApiEndpointProvider` (sin estado, una instancia global)
- **Transient**: `IOnlineCheck` (nueva instancia cada vez)
- **Scoped**: No aplicable en desktop app (sin requests HTTP entrantes)

---

## Ejemplos de Integración

### Verificar Conectividad al Iniciar

```csharp
public class MainViewModel : ViewModelBase
{
    private readonly IOnlineCheck _onlineCheck;
    private bool _isOnline;
    
    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }
    
    public MainViewModel(IOnlineCheck onlineCheck)
    {
        _onlineCheck = onlineCheck;
    }
    
    public async Task InitializeAsync()
    {
        var result = await _onlineCheck.CheckAsync();
        IsOnline = result.IsOnline;
        
        if (!IsOnline)
        {
            // Mostrar mensaje de error al usuario
            ShowErrorDialog(result.ErrorMessage);
        }
    }
}
```

### Construir URLs Dinámicas

```csharp
public class CustomerService
{
    private readonly HttpClient _httpClient;
    private readonly IApiEndpointProvider _endpointProvider;
    
    public CustomerService(HttpClient httpClient, IApiEndpointProvider endpointProvider)
    {
        _httpClient = httpClient;
        _endpointProvider = endpointProvider;
    }
    
    public async Task<CustomerDto> GetCustomerAsync(int id)
    {
        var url = _endpointProvider.GetEndpoint("customers", id.ToString());
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<CustomerDto>();
    }
}
```

---

## Manejo de Errores

### Patrones Implementados

Los servicios implementados siguen este patrón de manejo de errores:

1. **No lanzan excepciones directamente**: Devuelven objetos de resultado
2. **Capturan excepciones específicas**: Como `OperationCanceledException`
3. **Usan ConfigureAwait(false)**: Para evitar deadlocks
4. **Proporcionan información detallada**: Códigos de estado y mensajes de error

### Ejemplo de Manejo en Consumidores

```csharp
public async Task PerformActionAsync()
{
    try
    {
        var result = await _onlineCheck.CheckAsync();
        
        if (!result.IsOnline)
        {
            if (result.StatusCode.HasValue)
            {
                // Error HTTP
                LogWarning($"API returned {result.StatusCode}: {result.ErrorMessage}");
            }
            else
            {
                // Error de red/conexión
                LogError($"Connection failed: {result.ErrorMessage}");
            }
            return;
        }
        
        // Continuar con operación normal
    }
    catch (Exception ex)
    {
        // Manejar errores inesperados
        LogError($"Unexpected error: {ex.Message}");
    }
}
```

---

## Testing

### Unit Tests para OnlineCheck

```csharp
[TestClass]
public class OnlineCheckTests
{
    [TestMethod]
    public async Task CheckAsync_WhenApiReturns200_ReturnsSuccess()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(mockHandler);
        var mockProvider = new Mock<IApiEndpointProvider>();
        mockProvider.Setup(p => p.GetEndpoint("Online"))
            .Returns("https://api.test.com/online");
        
        var onlineCheck = new OnlineCheck(httpClient, mockProvider.Object);
        
        // Act
        var result = await onlineCheck.CheckAsync();
        
        // Assert
        Assert.IsTrue(result.IsOnline);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNull(result.ErrorMessage);
    }
}
```

### Unit Tests para ApiEndpointProvider

```csharp
[TestClass]
public class ApiEndpointProviderTests
{
    [TestMethod]
    public void GetEndpoint_WithSimplePath_ReturnsCorrectUrl()
    {
        // Arrange
        var options = Options.Create(new ExternalApiOptions 
        { 
            BaseUrl = "https://api.test.com/" 
        });
        var provider = new ApiEndpointProvider(options);
        
        // Act
        var result = provider.GetEndpoint("customers");
        
        // Assert
        Assert.AreEqual("https://api.test.com/customers", result);
    }
}
```

---

## Referencias

- [ARCHITECTURE.md](./ARCHITECTURE.md) - Arquitectura completa
- [EMPTY_FILES.md](./EMPTY_FILES.md) - Componentes pendientes
- [README.md](./README.md) - Documentación general
