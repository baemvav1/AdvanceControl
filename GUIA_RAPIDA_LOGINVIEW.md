# Guía Rápida: LoginView Corregido

## 📖 Cómo Usar el Nuevo LoginView

### Para Desarrolladores

#### 1. Usar LoginView en un Diálogo

```csharp
// En MainViewModel o cualquier ViewModel
public async Task<bool> MostrarLoginAsync()
{
    var resultado = await _dialogService.ShowDialogAsync<LoginView>(
        title: "Iniciar Sesión",
        primaryButtonText: "Iniciar Sesión",
        closeButtonText: "Cancelar"
    );
    
    if (resultado)
    {
        // Usuario presionó "Iniciar Sesión"
        // Aquí puedes acceder a las credenciales si es necesario
    }
    
    return resultado;
}
```

#### 2. Integrar con IAuthService (Próximo Paso)

```csharp
// Modificar LoginViewModel.cs - Agregar en el constructor:
private readonly IAuthService _authService;

public LoginViewModel(IAuthService authService)
{
    _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
}

// Modificar ExecuteLogin():
private async void ExecuteLogin()
{
    try
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        if (!ValidateCredentials())
        {
            return;
        }

        // Llamada real al servicio de autenticación
        var loginDto = LogInDto.Create(User, Password);
        var success = await _authService.AuthenticateAsync(User, Password);
        
        if (!success)
        {
            ErrorMessage = "Usuario o contraseña incorrectos.";
        }
    }
    catch (Exception ex)
    {
        ErrorMessage = $"Error al iniciar sesión: {ex.Message}";
    }
    finally
    {
        IsLoading = false;
    }
}
```

#### 3. Registrar LoginViewModel en DI

```csharp
// En App.xaml.cs - Agregar en ConfigureServices:
services.AddTransient<ViewModels.LoginViewModel>();
```

#### 4. Modificar LoginView.xaml.cs para DI

```csharp
public sealed partial class LoginView : UserControl
{
    public LoginViewModel ViewModel { get; }

    // Constructor sin parámetros para uso directo
    public LoginView() : this(new LoginViewModel())
    {
    }
    
    // Constructor con DI
    public LoginView(LoginViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        this.InitializeComponent();
        this.DataContext = ViewModel;
    }
}
```

---

## 🔍 Estructura de Archivos

```
Advance Control/
├── Views/
│   └── Login/
│       ├── LoginView.xaml          # Vista XAML con bindings
│       └── LoginView.xaml.cs       # Code-behind con ViewModel
├── ViewModels/
│   ├── LoginViewModel.cs           # Lógica de presentación
│   └── ViewModelBase.cs            # Clase base con INotifyPropertyChanged
└── Models/
    └── LogInDto.cs                 # DTO con Data Annotations
```

---

## 🎯 Componentes Principales

### LoginView.xaml
- **UserTextBox**: Campo de texto para usuario
- **PasswordBox**: Campo seguro para contraseña
- **ErrorInfoBar**: Muestra mensajes de error
- **LoginButton**: Ejecuta LoginCommand
- **CancelButton**: Cierra el diálogo

### LoginViewModel.cs
- **User**: Propiedad para nombre de usuario
- **Password**: Propiedad para contraseña
- **ErrorMessage**: Mensaje de error actual
- **HasError**: Indica si hay un error
- **IsLoading**: Indica si está cargando
- **CanLogin**: Indica si se puede hacer login
- **LoginCommand**: Comando para ejecutar login
- **ValidateCredentials()**: Valida las credenciales
- **ExecuteLogin()**: Ejecuta el proceso de login
- **ClearForm()**: Limpia el formulario

### LogInDto.cs
- **User**: Nombre de usuario (validado con Data Annotations)
- **Password**: Contraseña (validada con Data Annotations)
- **Create()**: Método factory para crear instancia

---

## ✅ Validaciones Implementadas

### Validación de Usuario
- ✅ No puede estar vacío
- ✅ Mínimo 3 caracteres
- ✅ Máximo 50 caracteres

### Validación de Contraseña
- ✅ No puede estar vacía
- ✅ Mínimo 6 caracteres
- ✅ Máximo 100 caracteres

### Estado del Botón
- ✅ Se deshabilita si usuario o contraseña están vacíos
- ✅ Se deshabilita mientras está cargando (IsLoading = true)
- ✅ Se habilita solo cuando ambos campos son válidos

---

## 🔐 Seguridad

### Implementado ✅
- PasswordBox en lugar de TextBox (contraseña oculta)
- Validación de entrada en cliente
- Data Annotations en modelo
- Manejo de excepciones

### Por Implementar ⚠️
- Límite de intentos de login
- Captcha después de X intentos fallidos
- Token JWT en respuesta
- Refresh token
- Autenticación multifactor (opcional)

---

## 🐛 Depuración y Errores Comunes

### Error: "Bindings no funcionan"
**Solución:** Verificar que DataContext esté establecido en el constructor

### Error: "LoginCommand es null"
**Solución:** Verificar que LoginCommand se inicializa en el constructor del ViewModel

### Error: "PasswordBox no actualiza Password"
**Solución:** Usar binding Mode=TwoWay en el PasswordBox

### Error: "InfoBar no se muestra"
**Solución:** Verificar que HasError retorna true cuando hay ErrorMessage

### Error: "Botón no se deshabilita"
**Solución:** Verificar binding IsEnabled="{x:Bind ViewModel.CanLogin, Mode=OneWay}"

---

## 📊 Testing

### Tests Unitarios Recomendados

```csharp
[TestClass]
public class LoginViewModelTests
{
    [TestMethod]
    public void User_WhenSet_NotifiesPropertyChanged()
    {
        // Arrange
        var vm = new LoginViewModel();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(vm.User))
                propertyChanged = true;
        };
        
        // Act
        vm.User = "testuser";
        
        // Assert
        Assert.IsTrue(propertyChanged);
    }
    
    [TestMethod]
    public void CanLogin_WhenUserAndPasswordEmpty_ReturnsFalse()
    {
        // Arrange
        var vm = new LoginViewModel();
        
        // Act & Assert
        Assert.IsFalse(vm.CanLogin);
    }
    
    [TestMethod]
    public void ValidateCredentials_WhenUserTooShort_ReturnsFalse()
    {
        // Arrange
        var vm = new LoginViewModel();
        vm.User = "ab"; // Menos de 3 caracteres
        vm.Password = "password123";
        
        // Act
        var result = vm.ValidateCredentials(); // Necesitarás hacer público este método
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(vm.ErrorMessage.Contains("3 caracteres"));
    }
}
```

---

## 📚 Referencias

### Documentación Completa
- **REPORTE_LOGINVIEW.md** - Análisis exhaustivo (24KB)
- **RESUMEN_CORRECCION_LOGINVIEW.md** - Resumen ejecutivo (7.7KB)

### Patrones Utilizados
- **MVVM (Model-View-ViewModel)** - Patrón de diseño principal
- **Command Pattern** - Para LoginCommand
- **Data Transfer Object (DTO)** - LogInDto
- **Dependency Injection** - Para servicios

### Librerías Utilizadas
- **CommunityToolkit.Mvvm** - Para RelayCommand
- **System.ComponentModel.DataAnnotations** - Para validaciones
- **Microsoft.UI.Xaml** - Framework WinUI 3

---

## 🎓 Mejores Prácticas Aplicadas

1. ✅ **Separación de Responsabilidades** - Vista, ViewModel y Modelo separados
2. ✅ **Binding Bidireccional** - Sincronización automática entre Vista y ViewModel
3. ✅ **Comandos en lugar de Eventos** - ICommand para acciones
4. ✅ **Validación en Dos Capas** - Cliente (ViewModel) y Modelo (Data Annotations)
5. ✅ **Feedback al Usuario** - InfoBar para errores
6. ✅ **Estado de Carga** - IsLoading para deshabilitar UI
7. ✅ **Documentación XML** - Todo el código está documentado
8. ✅ **Manejo de Excepciones** - Try-catch en operaciones críticas
9. ✅ **Seguridad** - PasswordBox para contraseñas
10. ✅ **Accesibilidad** - ThemeResource para respetar tema del sistema

---

## 🔄 Flujo de Ejecución

```
1. Usuario abre diálogo de login
   ↓
2. LoginView se instancia con LoginViewModel
   ↓
3. Usuario ingresa credenciales
   ↓
4. Bindings actualizan User y Password en ViewModel
   ↓
5. CanLogin se evalúa automáticamente
   ↓
6. Usuario presiona "Iniciar Sesión"
   ↓
7. LoginCommand.Execute() se dispara
   ↓
8. ExecuteLogin() se ejecuta:
   - IsLoading = true (botón se deshabilita)
   - ValidateCredentials() valida entrada
   - Si válido: llama a AuthService (TODO)
   - Si inválido: muestra ErrorMessage
   - IsLoading = false (botón se habilita)
   ↓
9. InfoBar muestra resultado (éxito o error)
```

---

## 🚀 Roadmap

### Completado ✅
- [x] Corrección de todos los errores
- [x] Implementación de MVVM
- [x] Validación de entrada
- [x] Feedback de errores
- [x] Documentación completa

### En Progreso 🔄
- [ ] Integración con IAuthService
- [ ] Tests unitarios
- [ ] Inyección de dependencias

### Planificado 📋
- [ ] Internacionalización (i18n)
- [ ] Animaciones
- [ ] Recordar usuario
- [ ] Autenticación multifactor
- [ ] Biometría (Windows Hello)

---

**Última actualización:** 7 de Noviembre de 2025  
**Versión:** 1.0  
**Estado:** ✅ Producción
