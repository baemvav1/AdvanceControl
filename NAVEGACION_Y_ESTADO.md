# Navegación y Preservación de Estado

## Resumen

Este documento describe cómo la aplicación Advance Control preserva el estado de las páginas durante la navegación y proporciona funcionalidad de recarga.

## Problema Resuelto

Anteriormente, al navegar entre páginas:
- Se perdía toda la información ingresada por el usuario
- Los ViewModels se recreaban cada vez (Transient)
- Las páginas se destruían al navegar a otra vista
- Se realizaban cargas redundantes de datos desde el servidor
- No había forma de recargar manualmente los datos de una página

## Solución Implementada

### 1. ViewModels como Singleton

Los ViewModels de páginas ahora están registrados como **Singleton** en el contenedor de DI:

```csharp
// En App.xaml.cs
services.AddSingleton<ViewModels.CustomersViewModel>();
services.AddSingleton<ViewModels.OperacionesViewModel>();
services.AddSingleton<ViewModels.AcesoriaViewModel>();
services.AddSingleton<ViewModels.MttoViewModel>();
```

**Beneficios:**
- El estado se preserva entre navegaciones
- No se pierden datos filtrados, búsquedas, o información cargada
- Una sola instancia por toda la vida de la aplicación

**Nota:** `MainViewModel` y `LoginViewModel` permanecen como Transient porque son de nivel aplicación/diálogo.

### 2. NavigationCacheMode Habilitado

Todas las páginas ahora tienen el modo de caché de navegación habilitado:

```csharp
// En el constructor de cada página
this.NavigationCacheMode = NavigationCacheMode.Enabled;
```

**Efecto:**
- Las páginas no se destruyen al navegar a otra vista
- El árbol visual se preserva
- Los controles mantienen su estado (scroll position, inputs, etc.)

### 3. Prevención de Cargas Redundantes

#### ClientesView
Solo carga datos si la colección está vacía:

```csharp
protected override async void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    
    if (ViewModel.Customers.Count == 0)
    {
        await ViewModel.LoadClientesAsync();
    }
}
```

#### Otras Páginas (Operaciones, Asesoría, Mantenimiento)
Utilizan un flag `_isInitialized` en el ViewModel:

```csharp
public async Task InitializeAsync(bool forceReload = false, CancellationToken cancellationToken = default)
{
    if (_isInitialized && !forceReload) return;
    
    try
    {
        // Código de inicialización...
        _isInitialized = true;
    }
    // ...
}
```

### 4. Funcionalidad de Recarga

#### Interfaz IReloadable

```csharp
public interface IReloadable
{
    Task ReloadAsync();
}
```

Todas las páginas implementan esta interfaz para soportar recarga explícita.

#### NavigationService.Reload()

```csharp
public bool Reload()
{
    if (_frame.Content is IReloadable reloadablePage)
    {
        _ = reloadablePage.ReloadAsync();
        return true;
    }
    return false;
}
```

#### Botón de Recarga en MainWindow

```xml
<Button Command="{Binding ReloadCommand}" 
        Content="Reload" 
        ToolTipService.ToolTip="Recargar la página actual" />
```

## Flujo de Trabajo

### Primera Navegación a una Página
1. Frame navega al tipo de página
2. Página se crea (constructor se ejecuta)
3. `NavigationCacheMode.Enabled` se establece
4. ViewModel Singleton se obtiene de DI
5. `OnNavigatedTo` se dispara
6. Datos se cargan (si es necesario)

### Navegación de Regreso a una Página Visitada
1. Frame recupera la página del caché
2. NO se ejecuta el constructor
3. `OnNavigatedTo` se dispara
4. Datos NO se recargan (ya existen)
5. Estado de UI se preserva (scroll, inputs, etc.)

### Uso del Botón Reload
1. Usuario hace clic en "Reload"
2. `MainViewModel.ReloadCommand` se ejecuta
3. `NavigationService.Reload()` se llama
4. Página actual (si implementa `IReloadable`) recibe `ReloadAsync()`
5. ViewModel recarga sus datos (con `forceReload=true` si aplica)
6. UI se actualiza con nuevos datos

## Consideraciones de Diseño

### ¿Por Qué Singleton y No Scoped?

WinUI 3 no tiene un concepto de "scope" de navegación como ASP.NET Core. Las opciones son:
- **Transient**: Nueva instancia cada vez (se pierde estado)
- **Singleton**: Una instancia para toda la app (preserva estado) ✓
- **Scoped**: No aplicable en WinUI 3

### ¿Por Qué NavigationCacheMode.Enabled?

Alternativas consideradas:
- `NavigationCacheMode.Disabled`: Destruye la página (pierde estado)
- `NavigationCacheMode.Required`: Similar a Enabled pero más agresivo
- `NavigationCacheMode.Enabled`: Balance perfecto ✓

### Gestión de Memoria

Con páginas en caché y ViewModels Singleton:
- **Ventaja**: Mejor experiencia de usuario
- **Costo**: Mayor uso de memoria
- **Mitigación**: 4 páginas es un número razonable; si fueran muchas más, considerar un sistema de cache LRU

## Casos de Uso

### 1. Usuario Busca Clientes
1. Navega a "Clientes"
2. Aplica filtros y busca
3. Ve resultados
4. Navega a "Operaciones"
5. **Vuelve a "Clientes"** → Filtros y resultados siguen ahí ✓

### 2. Usuario Actualiza Datos
1. Está viendo "Clientes"
2. Otro proceso actualiza datos en el servidor
3. Usuario hace clic en **"Reload"**
4. Datos se refrescan sin perder la página

### 3. Usuario Ingresa Formulario
1. Llena campos en una página
2. Navega a otra página para consultar info
3. Vuelve a la página anterior
4. Campos mantienen sus valores ✓

## Archivos Modificados

- `App.xaml.cs` - Registro de ViewModels como Singleton
- `Navigation/INavigationService.cs` - Añadido método Reload()
- `Navigation/NavigationService.cs` - Implementación de Reload()
- `Navigation/IReloadable.cs` - Nueva interfaz (NUEVO ARCHIVO)
- `ViewModels/MainViewModel.cs` - Añadido ReloadCommand
- `ViewModels/CustomersViewModel.cs` - Sin cambios (ya tenía lógica apropiada)
- `ViewModels/OperacionesViewModel.cs` - Añadido flag _isInitialized
- `ViewModels/AcesoriaViewModel.cs` - Añadido flag _isInitialized
- `ViewModels/MttoViewModel.cs` - Añadido flag _isInitialized
- `Views/MainWindow.xaml` - Añadido botón Reload
- `Views/Pages/ClientesView.xaml.cs` - NavigationCacheMode + IReloadable
- `Views/Pages/OperacionesView.xaml.cs` - NavigationCacheMode + IReloadable
- `Views/Pages/AcesoriaView.xaml.cs` - NavigationCacheMode + IReloadable
- `Views/Pages/MttoView.xaml.cs` - NavigationCacheMode + IReloadable

## Testing

Para verificar la implementación:

1. **Test de Estado**:
   - Navegar a Clientes
   - Aplicar un filtro
   - Navegar a Operaciones
   - Volver a Clientes
   - Verificar que el filtro sigue aplicado

2. **Test de Reload**:
   - Navegar a cualquier página
   - Hacer clic en botón "Reload"
   - Verificar que los datos se recargan

3. **Test de Memoria** (opcional):
   - Usar herramientas de profiling
   - Navegar entre páginas múltiples veces
   - Verificar que no hay memory leaks

## Extensibilidad

Para añadir una nueva página con preservación de estado:

1. Registrar el ViewModel como Singleton en `App.xaml.cs`
2. Implementar `IReloadable` en la página
3. Establecer `NavigationCacheMode.Enabled` en el constructor
4. Añadir lógica de prevención de carga redundante si es necesario

```csharp
// Ejemplo
public sealed partial class NuevaPaginaView : Page, IReloadable
{
    public NuevaPaginaView()
    {
        ViewModel = ((App)Application.Current).Host.Services
            .GetRequiredService<NuevaPaginaViewModel>();
        this.InitializeComponent();
        this.NavigationCacheMode = NavigationCacheMode.Enabled;
    }

    public async Task ReloadAsync()
    {
        await ViewModel.LoadDataAsync(forceReload: true);
    }
}
```

## Conclusión

Esta implementación proporciona:
- ✓ Preservación de estado entre navegaciones
- ✓ Prevención de cargas redundantes
- ✓ Funcionalidad de recarga manual
- ✓ Mejor experiencia de usuario
- ✓ Uso eficiente de recursos del servidor
- ✓ Arquitectura extensible y mantenible
