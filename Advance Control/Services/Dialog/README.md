# DialogService - Servicio de Diálogos Genérico y Reutilizable

## Descripción General

El `DialogService` es un servicio genérico y reutilizable que permite mostrar cualquier `UserControl` en un diálogo modal (`ContentDialog`) con soporte para:
- Resultados tipados de cualquier tipo (bool, string, objetos personalizados, etc.)
- Uso sin resultado (void)
- Paso de parámetros al UserControl
- Total reutilización en toda la aplicación

## Características Principales

- **Genérico**: Funciona con cualquier `UserControl`
- **Tipado Fuerte**: Soporte para resultados de cualquier tipo mediante genéricos
- **Sin Resultado**: También puede usarse sin esperar un resultado tipado
- **Paso de Parámetros**: Permite enviar parámetros al UserControl vía DataContext
- **Reutilizable**: Un solo servicio para todos los diálogos de la aplicación
- **Integración con DI**: Compatible con inyección de dependencias

## Componentes

### 1. IDialogService
Interfaz del servicio de diálogos.

```csharp
public interface IDialogService
{
    // Muestra un diálogo con resultado tipado
    Task<DialogResult<T>> ShowDialogAsync<T>(
        UserControl content,
        string title = "",
        string primaryButtonText = "OK",
        string secondaryButtonText = "Cancel",
        object? parameters = null);

    // Muestra un diálogo sin resultado tipado
    Task<bool> ShowDialogAsync(
        UserControl content,
        string title = "",
        string primaryButtonText = "OK",
        string secondaryButtonText = "Cancel",
        object? parameters = null);
}
```

### 2. DialogService
Implementación del servicio que muestra diálogos en WinUI 3.

### 3. DialogResult<T>
Clase que encapsula el resultado de un diálogo:
- `IsConfirmed`: Indica si se hizo clic en el botón principal
- `Result`: Valor del resultado de tipo T

## Uso Básico

### Opción 1: Diálogo con Resultado Tipado

```csharp
// Crear un UserControl
var myControl = new MyCustomControl();

// Mostrar diálogo con parámetros opcionales
var result = await _dialogService.ShowDialogAsync<CustomerData>(
    content: myControl,
    title: "Editar Cliente",
    primaryButtonText: "Guardar",
    secondaryButtonText: "Cancelar",
    parameters: new { CustomerId = 123, Mode = "Edit" });

if (result.IsConfirmed)
{
    // El resultado está en el DataContext del UserControl
    var data = myControl.DataContext as CustomerData;
    await SaveCustomer(data);
}
```

### Opción 2: Diálogo sin Resultado Tipado

```csharp
// Crear un UserControl
var confirmControl = new ConfirmationControl();

// Mostrar diálogo simple que solo retorna true/false
bool confirmed = await _dialogService.ShowDialogAsync(
    content: confirmControl,
    title: "Confirmar Acción",
    primaryButtonText: "Sí",
    secondaryButtonText: "No");

if (confirmed)
{
    // Usuario confirmó la acción
    await DeleteItem();
}
```

### Opción 3: Diálogo con Parámetros

```csharp
// Crear un UserControl que espera parámetros
var editControl = new ProductEditControl();

// Los parámetros se establecen como DataContext
var parameters = new ProductEditParams
{
    ProductId = 456,
    ProductName = "Widget",
    Price = 29.99m
};

var result = await _dialogService.ShowDialogAsync<Product>(
    content: editControl,
    title: "Editar Producto",
    primaryButtonText: "Guardar",
    secondaryButtonText: "Cancelar",
    parameters: parameters);

if (result.IsConfirmed)
{
    // Obtener el resultado del DataContext
    var updatedProduct = editControl.DataContext as Product;
    await UpdateProduct(updatedProduct);
}
```

## Ejemplos de Uso Completos

### Ejemplo 1: Diálogo de Confirmación Simple

```csharp
// UserControl simple (XAML)
<UserControl x:Class="MyApp.ConfirmDialog">
    <StackPanel Padding="20">
        <TextBlock Text="¿Está seguro de eliminar este elemento?" 
                   TextWrapping="Wrap"/>
    </StackPanel>
</UserControl>

// Uso
var confirmDialog = new ConfirmDialog();
bool confirmed = await _dialogService.ShowDialogAsync(
    confirmDialog,
    "Confirmar Eliminación",
    "Eliminar",
    "Cancelar");

if (confirmed)
{
    await DeleteItem();
}
```

### Ejemplo 2: Diálogo con Input y Resultado

```csharp
// UserControl con ViewModel (Code-behind)
public sealed partial class InputDialog : UserControl
{
    public InputDialog()
    {
        InitializeComponent();
        // DataContext será establecido por el servicio si se pasan parámetros
    }
    
    public string InputText { get; set; }
}

// Uso
var inputDialog = new InputDialog();
var result = await _dialogService.ShowDialogAsync<string>(
    inputDialog,
    "Ingrese un valor",
    "Aceptar",
    "Cancelar");

if (result.IsConfirmed)
{
    var userInput = inputDialog.InputText;
    await ProcessInput(userInput);
}
```

### Ejemplo 3: Diálogo con Parámetros Complejos

```csharp
// Clase de parámetros
public class CustomerEditParams
{
    public int CustomerId { get; set; }
    public string Mode { get; set; } // "Edit" o "Create"
    public CustomerData InitialData { get; set; }
}

// UserControl que recibe parámetros
public sealed partial class CustomerEditDialog : UserControl
{
    public CustomerEditDialog()
    {
        InitializeComponent();
        // El DataContext será un CustomerEditParams cuando se pasen parámetros
        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is CustomerEditParams params)
            {
                // Inicializar UI con los parámetros
                LoadCustomerData(params);
            }
        };
    }
    
    private void LoadCustomerData(CustomerEditParams params)
    {
        // Cargar datos según el modo
        if (params.Mode == "Edit")
        {
            // Cargar para edición
        }
        else
        {
            // Nuevo cliente
        }
    }
    
    public CustomerData GetResult()
    {
        // Construir y retornar los datos del formulario
        return new CustomerData
        {
            Name = NameTextBox.Text,
            Email = EmailTextBox.Text,
            // ... otros campos
        };
    }
}

// Uso
var editDialog = new CustomerEditDialog();
var parameters = new CustomerEditParams
{
    CustomerId = 123,
    Mode = "Edit",
    InitialData = existingCustomer
};

var result = await _dialogService.ShowDialogAsync<CustomerData>(
    content: editDialog,
    title: "Editar Cliente",
    primaryButtonText: "Guardar",
    secondaryButtonText: "Cancelar",
    parameters: parameters);

if (result.IsConfirmed)
{
    var updatedCustomer = editDialog.GetResult();
    await SaveCustomer(updatedCustomer);
}
```

### Ejemplo 4: Diálogo con Lista de Selección

```csharp
// UserControl con lista de opciones
public sealed partial class SelectionDialog : UserControl
{
    public List<string> SelectedItems { get; private set; } = new();
    
    public SelectionDialog()
    {
        InitializeComponent();
    }
    
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItems = ItemsListView.SelectedItems.Cast<string>().ToList();
    }
}

// Uso con parámetros iniciales
var selectionDialog = new SelectionDialog();
var availableItems = new List<string> { "Item 1", "Item 2", "Item 3" };

var result = await _dialogService.ShowDialogAsync<List<string>>(
    content: selectionDialog,
    title: "Seleccionar Elementos",
    primaryButtonText: "Confirmar",
    secondaryButtonText: "Cancelar",
    parameters: availableItems);

if (result.IsConfirmed)
{
    var selected = selectionDialog.SelectedItems;
    await ProcessSelection(selected);
}
```

## Configuración en Dependency Injection

El servicio está registrado en `App.xaml.cs`:

```csharp
services.AddSingleton<IDialogService, DialogService>();
```

Y se inicializa en `MainWindow.xaml.cs` con el `XamlRoot` necesario:

```csharp
public MainWindow(MainViewModel viewModel, IDialogService dialogService)
{
    InitializeComponent();
    
    if (this.Content is FrameworkElement element && element.XamlRoot != null)
    {
        dialogService.SetXamlRoot(element.XamlRoot);
    }
}
```

## Obtención de Resultados

Hay dos patrones principales para obtener resultados:

### Patrón 1: DataContext
El UserControl puede modificar su propio DataContext o propiedades que luego se leen:

```csharp
var dialog = new MyDialog();
var result = await _dialogService.ShowDialogAsync<MyData>(dialog, "Título");

if (result.IsConfirmed)
{
    // Leer del DataContext
    var data = dialog.DataContext as MyData;
    
    // O leer propiedades públicas
    var value = dialog.SomeProperty;
}
```

### Patrón 2: Propiedades Públicas
El UserControl expone propiedades que el código llamador puede leer:

```csharp
public sealed partial class InputDialog : UserControl
{
    public string UserInput { get; set; }
}

// Uso
var dialog = new InputDialog();
await _dialogService.ShowDialogAsync(dialog, "Input");
var input = dialog.UserInput; // Leer la propiedad
```

## Notas Importantes

1. **XamlRoot**: El servicio necesita un `XamlRoot` para mostrar diálogos en WinUI 3. Esto se configura automáticamente en `MainWindow`.

2. **Parámetros**: Los parámetros pasados se establecen como `DataContext` del UserControl. El UserControl puede reaccionar a esto mediante el evento `DataContextChanged`.

3. **Resultados**: El resultado puede obtenerse:
   - Del `DataContext` del UserControl
   - De propiedades públicas del UserControl
   - De métodos públicos que retornen el resultado

4. **Sin Interfaces**: No se requiere implementar interfaces especiales. El UserControl es completamente libre en su implementación.

5. **Validación**: La validación debe hacerse en el UserControl o su ViewModel antes de que el diálogo se cierre. Si necesita prevenir el cierre, deberá implementar su propia lógica en el UserControl.

6. **Botones**: El diálogo mostrará dos botones (Primary y Secondary). Puede personalizar sus textos.

## Ventajas del Diseño

✅ **Reutilizable**: Un solo servicio para todos los diálogos
✅ **Type-Safe**: Resultados fuertemente tipados (cuando se usan)
✅ **Flexible**: Soporta cualquier tipo de resultado o sin resultado
✅ **Simple**: No requiere interfaces especiales en los UserControls
✅ **Parámetros**: Fácil paso de parámetros vía DataContext
✅ **Testeable**: Fácil de mockear para pruebas unitarias
✅ **Mantenible**: Separación clara de responsabilidades
✅ **Escalable**: Agregar nuevos diálogos no requiere modificar el servicio

## Migración de Código Existente

**Antes:**
```csharp
var dialog = new ContentDialog
{
    Title = "Login",
    Content = new LoginView(),
    PrimaryButtonText = "OK",
    XamlRoot = this.XamlRoot
};
var result = await dialog.ShowAsync();
```

**Después (con resultado):**
```csharp
var loginView = new LoginView();
var result = await _dialogService.ShowDialogAsync<bool>(
    loginView, 
    "Login", 
    "OK"
);
```

**Después (sin resultado):**
```csharp
var confirmView = new ConfirmView();
bool confirmed = await _dialogService.ShowDialogAsync(
    confirmView, 
    "Confirmar", 
    "Sí",
    "No"
);
```
