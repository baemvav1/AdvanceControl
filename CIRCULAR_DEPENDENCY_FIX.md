# Fix: System.InvalidOperationException - Circular Dependency

## Problema Original

La aplicación lanzaba una excepción durante la ejecución:

```
System.InvalidOperationException: 'ValueFactory attempted to access the Value property of this instance.'
```

En el archivo: `App.xaml.cs`  
En la línea: `var window = Host.Services.GetRequiredService<MainWindow>();`

## Causa Raíz

Existía una **dependencia circular** en el contenedor de inyección de dependencias (DI):

```
AuthService → AuthenticatedHttpHandler → IAuthService
```

### Cadena de Dependencias Completa:

1. **MainWindow** requiere **MainViewModel**
2. **MainViewModel** requiere **IAuthService**
3. **IAuthService** se implementa como **AuthService** con un HttpClient
4. El HttpClient de **AuthService** tiene **AuthenticatedHttpHandler** en su pipeline
5. **AuthenticatedHttpHandler** requiere **IAuthService** (dependencia circular)

El contenedor DI de .NET detecta esta dependencia circular y lanza la excepción `InvalidOperationException`.

## Solución Implementada

Se utilizó el patrón **Lazy Loading** para romper la dependencia circular:

### Cambios en `AuthenticatedHttpHandler.cs`:

```csharp
// Antes:
private readonly IAuthService _authService;

public AuthenticatedHttpHandler(IAuthService authService, ...)
{
    _authService = authService;
    // ...
}

// Después:
private readonly Lazy<IAuthService> _authService;

public AuthenticatedHttpHandler(Lazy<IAuthService> authService, ...)
{
    _authService = authService;
    // ...
}

// Uso:
var token = await _authService.Value.GetAccessTokenAsync(cancellationToken);
```

### Cambios en `App.xaml.cs`:

```csharp
// Antes:
services.AddTransient<Services.Http.AuthenticatedHttpHandler>();

// Después:
services.AddTransient<Services.Http.AuthenticatedHttpHandler>(sp =>
{
    var lazyAuthService = new Lazy<IAuthService>(() => sp.GetRequiredService<IAuthService>());
    var endpointProvider = sp.GetRequiredService<IApiEndpointProvider>();
    var logger = sp.GetService<ILoggingService>(); // optional
    return new Services.Http.AuthenticatedHttpHandler(lazyAuthService, endpointProvider, logger);
});
```

## Cómo Funciona Lazy<T>

`Lazy<T>` permite que:
- El objeto `AuthenticatedHttpHandler` se pueda crear sin necesitar inmediatamente una instancia de `IAuthService`
- `IAuthService` solo se resuelve cuando se accede a `_authService.Value` por primera vez
- Esto rompe la dependencia circular en tiempo de construcción

## Beneficios de la Solución

1. ✅ **Elimina la dependencia circular** sin cambiar la arquitectura general
2. ✅ **Mantiene la separación de responsabilidades** de cada servicio
3. ✅ **Performance**: El servicio solo se instancia cuando realmente se necesita
4. ✅ **Thread-safe**: `Lazy<T>` es thread-safe por defecto
5. ✅ **Mínima modificación**: Solo se requieren cambios en dos archivos

## Verificación

La dependencia circular ha sido eliminada. La cadena de dependencias ahora es:

```
MainWindow → MainViewModel → IAuthService → AuthService (HttpClient + Handler)
                                              ↓
                                         AuthenticatedHttpHandler (con Lazy<IAuthService>)
```

El `Lazy<IAuthService>` se resuelve en tiempo de ejecución cuando se necesita, no durante la construcción del objeto.

## Notas Adicionales

- La solución utiliza el patrón estándar de Microsoft para resolver dependencias circulares en DI
- No se han introducido cambios en la lógica de negocio
- El código sigue siendo testeable y mantenible
