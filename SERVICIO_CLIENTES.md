# Servicio de Clientes - Documentación

## Descripción

Se ha implementado un servicio completo para recuperar información de clientes desde el endpoint de la API: `https://localhost:7055/api/Clientes`

## Componentes Implementados

### 1. Modelos

#### `ClienteQueryDto`
Ubicación: `Advance Control/Models/ClienteQueryDto.cs`

DTO para los parámetros de búsqueda opcionales:
- `Search` (string?): Búsqueda en razón social o nombre comercial (LIKE)
- `Rfc` (string?): Búsqueda parcial por RFC (LIKE)
- `Curp` (string?): Búsqueda parcial por CURP (LIKE)
- `Notas` (string?): Búsqueda parcial en notas (LIKE)
- `Prioridad` (int?): Coincidencia exacta de prioridad

#### `CustomerDto`
Ubicación: `Advance Control/Models/CustomerDto.cs` (ya existente)

Modelo que representa la información completa de un cliente devuelta por la API.

### 2. Servicio

#### `IClienteService`
Ubicación: `Advance Control/Services/Clientes/IClienteService.cs`

Interfaz del servicio con el método:
```csharp
Task<List<CustomerDto>> GetClientesAsync(ClienteQueryDto? query = null, CancellationToken cancellationToken = default);
```

#### `ClienteService`
Ubicación: `Advance Control/Services/Clientes/ClienteService.cs`

Implementación del servicio que:
- Utiliza HttpClient con autenticación automática (Bearer token)
- Construye URLs con parámetros de consulta correctamente escapados
- Maneja errores y registra logs
- Devuelve lista de clientes desde la API

### 3. ViewModel

#### `CustomersViewModel`
Ubicación: `Advance Control/ViewModels/CustomersViewModel.cs`

ViewModel actualizado con:
- Inyección de dependencias de `IClienteService` y `ILoggingService`
- Propiedades para todos los filtros de búsqueda
- Método `LoadClientesAsync()` para cargar clientes con filtros
- Método `ClearFiltersAsync()` para limpiar filtros
- Observable collection de clientes para binding con la UI

### 4. Vista

#### `ClientesView`
Ubicación: `Advance Control/Views/Pages/ClientesView.xaml` y `.xaml.cs`

Vista actualizada con:
- Interfaz de usuario para filtros de búsqueda
- Lista de clientes con información detallada
- Indicador de carga
- Botones para buscar y limpiar filtros
- Carga automática de clientes al navegar a la página

## Uso del Servicio

### Ejemplo básico (sin filtros)

```csharp
public class MiClase
{
    private readonly IClienteService _clienteService;

    public MiClase(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    public async Task ObtenerTodosLosClientes()
    {
        var clientes = await _clienteService.GetClientesAsync();
        // Procesar clientes...
    }
}
```

### Ejemplo con filtros

```csharp
public async Task BuscarClientesPorRFC(string rfc)
{
    var query = new ClienteQueryDto
    {
        Rfc = rfc
    };
    
    var clientes = await _clienteService.GetClientesAsync(query);
    return clientes;
}
```

### Ejemplo con múltiples filtros

```csharp
public async Task BuscarClientesConFiltros()
{
    var query = new ClienteQueryDto
    {
        Search = "ACME",           // Busca en razón social o nombre comercial
        Rfc = "ABC",               // RFC que contenga "ABC"
        Prioridad = 1              // Prioridad exacta = 1
    };
    
    var clientes = await _clienteService.GetClientesAsync(query);
    return clientes;
}
```

## Registro en DI Container

El servicio ya está registrado en `App.xaml.cs` con el pipeline completo:

```csharp
services.AddHttpClient<IClienteService, ClienteService>((sp, client) =>
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

Esto significa que:
1. El HttpClient se configura automáticamente con la URL base de la API
2. Se adjunta automáticamente el token de autenticación Bearer a cada petición
3. Si el token expira, se refresca automáticamente y reintenta la petición

## Endpoint de la API

El servicio construye URLs con el siguiente formato:

**Sin parámetros:**
```
GET https://localhost:7055/api/Clientes
```

**Con parámetros (ejemplo completo):**
```
GET https://localhost:7055/api/Clientes?search=ACME&rfc=ABC123&curp=DEF456&notas=importante&prioridad=1
```

## Autenticación

El servicio requiere que el usuario esté autenticado. El token JWT se adjunta automáticamente mediante el `AuthenticatedHttpHandler` registrado en el pipeline del HttpClient.

Si la petición devuelve 401 (Unauthorized):
1. El handler intenta refrescar el token automáticamente
2. Reintenta la petición con el nuevo token
3. Si el refresh falla, devuelve 401 al llamador

## Manejo de Errores

El servicio incluye manejo completo de errores:
- `HttpRequestException`: Errores de red (se envuelven en `InvalidOperationException`)
- Otras excepciones: Se propagan al llamador después de registrar en el log
- Respuestas no exitosas: Se registran en el log y devuelven lista vacía

## Logging

Todas las operaciones importantes se registran:
- Inicio de petición con URL completa
- Número de clientes obtenidos exitosamente
- Errores de red o de respuesta
- Errores inesperados

Los logs se envían al servicio `ILoggingService` que puede enviarlos a la API o almacenarlos localmente.

## Notas Técnicas

- Los parámetros de consulta se escapan correctamente usando `Uri.EscapeDataString()`
- Todos los parámetros son opcionales
- Los parámetros nulos o vacíos no se incluyen en la URL
- El servicio es thread-safe
- Soporta `CancellationToken` para cancelar operaciones
