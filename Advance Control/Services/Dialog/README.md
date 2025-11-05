# DialogService - Servicio de Diálogos Genérico

## Descripción General

El `DialogService` es un servicio genérico y reutilizable que permite mostrar cualquier `UserControl` en un diálogo modal (`ContentDialog`) y obtener resultados de diferentes tipos (bool, string, array, objetos personalizados, etc.).

## Características Principales

- **Genérico**: Funciona con cualquier `UserControl`
- **Tipado Fuerte**: Soporte para resultados de cualquier tipo mediante genéricos
- **Reutilizable**: Un solo servicio para todos los diálogos de la aplicación
- **Integración con DI**: Compatible con inyección de dependencias

## Componentes

### 1. IDialogService
Interfaz del servicio de diálogos.

```csharp
public interface IDialogService
{
    Task<DialogResult<T>> ShowDialogAsync<T>(
        UserControl content,
        string title = "",
        string primaryButtonText = "OK",
        string secondaryButtonText = "Cancel");
}
```

### 2. DialogService
Implementación del servicio que muestra diálogos en WinUI 3.

### 3. DialogResult<T>
Clase que encapsula el resultado de un diálogo:
- `IsConfirmed`: Indica si se hizo clic en el botón principal
- `Result`: Valor del resultado de tipo T

### 4. IDialogContent<T>
Interfaz que deben implementar los UserControls que quieran devolver un resultado:

```csharp
public interface IDialogContent<T>
{
    T GetResult();
}
```

## Uso Básico

### Paso 1: Crear un UserControl con Resultado

```csharp
public sealed partial class LoginView : UserControl, IDialogContent<bool>
{
    private readonly LoginViewModel _viewModel;

    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public bool GetResult()
    {
        return _viewModel.LoginResult;
    }
}
```

### Paso 2: Usar el DialogService

```csharp
public async Task<bool> ShowLoginDialogAsync()
{
    var loginViewModel = new LoginViewModel(_authService, _logger);
    var loginView = new LoginView(loginViewModel);

    var result = await _dialogService.ShowDialogAsync<bool>(
        content: loginView,
        title: "Iniciar Sesión",
        primaryButtonText: "Iniciar Sesión",
        secondaryButtonText: "Cancelar");

    if (result.IsConfirmed && result.Result)
    {
        // El usuario inició sesión exitosamente
        return true;
    }
    
    return false;
}
```

## Ejemplos de Uso con Diferentes Tipos

### Ejemplo 1: Diálogo con Resultado Booleano (LoginView)

Ya implementado en `LoginView.xaml` y `LoginView.xaml.cs`.

```csharp
// En MainViewModel
var success = await ShowLoginDialogAsync();
if (success)
{
    // Usuario autenticado
}
```

### Ejemplo 2: Diálogo con Resultado String

```csharp
// UserControl para input de texto
public sealed partial class InputDialog : UserControl, IDialogContent<string>
{
    private string _inputText = "";
    
    public string GetResult() => _inputText;
    
    // Binding en XAML: Text="{Binding InputText, Mode=TwoWay}"
}

// Uso
var inputView = new InputDialog();
var result = await _dialogService.ShowDialogAsync<string>(
    content: inputView,
    title: "Ingrese un valor",
    primaryButtonText: "Aceptar",
    secondaryButtonText: "Cancelar");

if (result.IsConfirmed)
{
    string userInput = result.Result;
    // Usar el texto ingresado
}
```

### Ejemplo 3: Diálogo con Resultado de Objeto Personalizado

```csharp
// Clase de datos
public class CustomerData
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

// UserControl
public sealed partial class CustomerDialog : UserControl, IDialogContent<CustomerData>
{
    private readonly CustomerViewModel _viewModel;
    
    public CustomerData GetResult()
    {
        return new CustomerData
        {
            Name = _viewModel.Name,
            Email = _viewModel.Email,
            Age = _viewModel.Age
        };
    }
}

// Uso
var customerView = new CustomerDialog(customerViewModel);
var result = await _dialogService.ShowDialogAsync<CustomerData>(
    content: customerView,
    title: "Datos del Cliente",
    primaryButtonText: "Guardar",
    secondaryButtonText: "Cancelar");

if (result.IsConfirmed)
{
    CustomerData customer = result.Result;
    await SaveCustomer(customer);
}
```

### Ejemplo 4: Diálogo con Resultado de Lista/Array

```csharp
// UserControl para selección múltiple
public sealed partial class MultiSelectDialog : UserControl, IDialogContent<List<string>>
{
    public List<string> GetResult()
    {
        return SelectedItems.ToList();
    }
}

// Uso
var selectView = new MultiSelectDialog();
var result = await _dialogService.ShowDialogAsync<List<string>>(
    content: selectView,
    title: "Seleccionar elementos",
    primaryButtonText: "Confirmar",
    secondaryButtonText: "Cancelar");

if (result.IsConfirmed)
{
    List<string> selectedItems = result.Result;
    // Procesar elementos seleccionados
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
    
    if (this.Content is FrameworkElement element)
    {
        dialogService.SetXamlRoot(element.XamlRoot);
    }
}
```

## Notas Importantes

1. **XamlRoot**: El servicio necesita un `XamlRoot` para mostrar diálogos en WinUI 3. Esto se configura automáticamente en `MainWindow`.

2. **IDialogContent<T>**: Para que un `UserControl` pueda devolver un resultado, debe implementar `IDialogContent<T>`.

3. **ViewModels**: Es recomendable que los `UserControl` tengan sus propios `ViewModel` para manejar la lógica de negocio.

4. **Validación**: Realice validaciones en el `ViewModel` antes de devolver el resultado.

5. **Buttons**: El diálogo mostrará dos botones (Primary y Secondary). Puede personalizar sus textos o usar solo uno dejando el otro vacío.

## Ventajas del Diseño

✅ **Reutilizable**: Un solo servicio para todos los diálogos
✅ **Type-Safe**: Resultados fuertemente tipados
✅ **Flexible**: Soporta cualquier tipo de resultado
✅ **Testeable**: Fácil de mockear para pruebas unitarias
✅ **Mantenible**: Separación clara de responsabilidades
✅ **Escalable**: Agregar nuevos diálogos no requiere modificar el servicio

## Migración de Código Existente

Si tenía código que creaba diálogos directamente, puede migrarlo fácilmente:

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

**Después:**
```csharp
var loginView = new LoginView(loginViewModel);
var result = await _dialogService.ShowDialogAsync<bool>(
    loginView, 
    "Login", 
    "OK"
);
```
