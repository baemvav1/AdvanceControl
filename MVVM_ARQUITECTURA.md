# ARQUITECTURA MVVM - MainWindow y MainViewModel

## 📋 CAMBIOS REALIZADOS

### Objetivo
Conectar MainWindow con MainViewModel siguiendo el patrón MVVM, moviendo toda la lógica de presentación al ViewModel y dejando el código preparado para implementar login en el futuro.

---

## ✅ IMPLEMENTACIÓN COMPLETADA

### 1️⃣ MainViewModel - Lógica Centralizada

**Archivo:** `/Advance Control/ViewModels/MainViewModel.cs`

Se transformó de una clase simple con solo una propiedad `Title` a una clase completa con:

#### Servicios Inyectados
```csharp
private readonly INavigationService _navigationService;
private readonly IOnlineCheck _onlineCheck;
private readonly ILoggingService _logger;
private readonly IAuthService _authService;
```

#### Propiedades Observables
```csharp
public string Title { get; set; }              // Título de la ventana
public bool IsAuthenticated { get; set; }      // Estado de autenticación
public bool IsBackEnabled { get; set; }        // Habilita botón "Atrás"
```

#### Métodos de Navegación
```csharp
public void InitializeNavigation(Frame contentFrame)
public void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
public void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
```

#### Métodos para Login (Preparados para el Futuro)
```csharp
public async Task<bool> LoginAsync(string username, string password)
public async Task LogoutAsync()
public async Task<bool> CheckOnlineStatusAsync()
```

---

### 2️⃣ MainWindow - Vista Simplificada

**Archivo:** `/Advance Control/Views/MainWindow.xaml.cs`

**ANTES:**
- Constructor inyectaba 3 servicios: `IOnlineCheck`, `ILoggingService`, `INavigationService`
- Contenía toda la lógica de navegación
- Manejaba eventos directamente
- ~140 líneas de código

**DESPUÉS:**
- Constructor inyecta solo `MainViewModel`
- Establece `DataContext = _viewModel`
- Delega eventos al ViewModel usando lambdas
- ~30 líneas de código

```csharp
public MainWindow(MainViewModel viewModel)
{
    this.InitializeComponent();
    _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    
    // Establece el DataContext para data binding
    this.DataContext = _viewModel;
    
    // Inicializa navegación
    _viewModel.InitializeNavigation(contentFrame);
    
    // Delega eventos al ViewModel
    nvSample.ItemInvoked += (sender, args) => _viewModel.OnNavigationItemInvoked(sender, args);
    nvSample.BackRequested += (sender, args) => _viewModel.OnBackRequested(sender, args);
}
```

---

### 3️⃣ MainWindow.xaml - Data Binding

**Archivo:** `/Advance Control/Views/MainWindow.xaml`

Se agregó binding para la propiedad `IsBackEnabled`:

```xml
<NavigationView x:Name="nvSample" 
                IsBackButtonVisible="Auto" 
                IsBackEnabled="{Binding IsBackEnabled, Mode=OneWay}">
```

Esto permite que el estado del botón "Atrás" se actualice automáticamente cuando cambia en el ViewModel.

---

### 4️⃣ Dependency Injection - Registro de ViewModel

**Archivo:** `/Advance Control/App.xaml.cs`

Se agregó el registro del `MainViewModel` en el contenedor de DI:

```csharp
// Registrar ViewModels
services.AddTransient<ViewModels.MainViewModel>();

// Registrar MainWindow para que DI pueda resolverlo y proporcionar sus dependencias
services.AddTransient<MainWindow>();
```

---

## 🔐 PREPARACIÓN PARA LOGIN FUTURO

### Estado Actual

El sistema está completamente preparado para implementar login. Todos los componentes necesarios están en su lugar:

#### 1. **Servicios de Autenticación Disponibles**
- ✅ `IAuthService` - Maneja autenticación, tokens, refresh
- ✅ `ISecureStorage` - Almacena tokens de forma segura
- ✅ `AuthenticatedHttpHandler` - Agrega tokens a requests HTTP

#### 2. **Propiedades en MainViewModel**
```csharp
public bool IsAuthenticated { get; set; }  // Para mostrar/ocultar UI según estado
```

#### 3. **Métodos Implementados**
```csharp
// Autenticar usuario
public async Task<bool> LoginAsync(string username, string password)
{
    try
    {
        var success = await _authService.AuthenticateAsync(username, password);
        if (success)
        {
            IsAuthenticated = true;
            await _logger.LogInfoAsync($"Usuario autenticado exitosamente: {username}", 
                                       "MainViewModel", "LoginAsync");
        }
        return success;
    }
    catch (Exception ex)
    {
        await _logger.LogErrorAsync($"Error al intentar autenticar usuario: {username}", 
                                    ex, "MainViewModel", "LoginAsync");
        return false;
    }
}

// Cerrar sesión
public async Task LogoutAsync()
{
    try
    {
        await _authService.ClearTokenAsync();
        IsAuthenticated = false;
        await _logger.LogInfoAsync("Usuario cerró sesión", "MainViewModel", "LogoutAsync");
    }
    catch (Exception ex)
    {
        await _logger.LogErrorAsync("Error al cerrar sesión", ex, "MainViewModel", "LogoutAsync");
    }
}
```

---

## 🚀 CÓMO IMPLEMENTAR LOGIN (PASOS FUTUROS)

### Opción 1: Pantalla de Login Separada

#### Paso 1: Crear LoginView.xaml
```xml
<Page x:Class="Advance_Control.Views.LoginView"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Width="300">
        <TextBlock Text="Login" FontSize="24" Margin="0,0,0,20"/>
        <TextBox x:Name="UsernameTextBox" PlaceholderText="Usuario" Margin="0,10"/>
        <PasswordBox x:Name="PasswordBox" PlaceholderText="Contraseña" Margin="0,10"/>
        <Button Content="Iniciar Sesión" Click="LoginButton_Click" Margin="0,10"/>
        <TextBlock x:Name="ErrorTextBlock" Foreground="Red" Margin="0,10"/>
    </StackPanel>
</Page>
```

#### Paso 2: Crear LoginViewModel.cs
```csharp
public class LoginViewModel : ViewModelBase
{
    private readonly MainViewModel _mainViewModel;
    private string _username;
    private string _password;
    private string _errorMessage;
    private bool _isLoading;

    public LoginViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public async Task<bool> LoginAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var success = await _mainViewModel.LoginAsync(Username, Password);
            if (!success)
            {
                ErrorMessage = "Usuario o contraseña incorrectos";
            }
            return success;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

#### Paso 3: Modificar App.xaml.cs para mostrar LoginView primero
```csharp
protected override async void OnLaunched(LaunchActivatedEventArgs args)
{
    await Host.StartAsync();
    
    var mainViewModel = Host.Services.GetRequiredService<MainViewModel>();
    
    // Si no está autenticado, mostrar login
    if (!mainViewModel.IsAuthenticated)
    {
        var loginWindow = new LoginWindow(); // Ventana separada para login
        loginWindow.Activate();
    }
    else
    {
        // Si ya está autenticado, mostrar ventana principal
        var window = Host.Services.GetRequiredService<MainWindow>();
        window.Activate();
    }
}
```

---

### Opción 2: Login Dentro de MainWindow

#### Modificar MainWindow.xaml para incluir Panel de Login
```xml
<Grid>
    <!-- Panel de Login (visible cuando NO está autenticado) -->
    <Grid Visibility="{Binding IsAuthenticated, Converter={StaticResource InverseBoolToVisibilityConverter}}">
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Width="400">
            <TextBlock Text="Advance Control" FontSize="32" FontWeight="Bold" 
                       HorizontalAlignment="Center" Margin="0,0,0,30"/>
            <TextBox x:Name="UsernameTextBox" PlaceholderText="Usuario" Margin="0,10"/>
            <PasswordBox x:Name="PasswordBox" PlaceholderText="Contraseña" Margin="0,10"/>
            <Button x:Name="LoginButton" Content="Iniciar Sesión" 
                    HorizontalAlignment="Stretch" Margin="0,20"/>
            <TextBlock x:Name="ErrorTextBlock" Foreground="Red" 
                       HorizontalAlignment="Center" Margin="0,10"/>
        </StackPanel>
    </Grid>
    
    <!-- Panel Principal (visible cuando SÍ está autenticado) -->
    <Grid Visibility="{Binding IsAuthenticated, Converter={StaticResource BoolToVisibilityConverter}}">
        <Grid.RowDefinitions>
            <RowDefinition Height="50" />
            <RowDefinition />
            <RowDefinition Height="10"/>
        </Grid.RowDefinitions>
        <!-- ... NavigationView existente ... -->
    </Grid>
</Grid>
```

#### Agregar evento en MainWindow.xaml.cs
```csharp
public MainWindow(MainViewModel viewModel)
{
    this.InitializeComponent();
    _viewModel = viewModel;
    this.DataContext = _viewModel;
    
    // Configurar evento de login
    LoginButton.Click += OnLoginButtonClick;
    
    // Solo inicializar navegación si está autenticado
    if (_viewModel.IsAuthenticated)
    {
        _viewModel.InitializeNavigation(contentFrame);
    }
    
    nvSample.ItemInvoked += (sender, args) => _viewModel.OnNavigationItemInvoked(sender, args);
    nvSample.BackRequested += (sender, args) => _viewModel.OnBackRequested(sender, args);
}

private async void OnLoginButtonClick(object sender, RoutedEventArgs e)
{
    LoginButton.IsEnabled = false;
    ErrorTextBlock.Text = string.Empty;
    
    try
    {
        var success = await _viewModel.LoginAsync(UsernameTextBox.Text, PasswordBox.Password);
        
        if (success)
        {
            // Inicializar navegación después del login exitoso
            _viewModel.InitializeNavigation(contentFrame);
        }
        else
        {
            ErrorTextBlock.Text = "Usuario o contraseña incorrectos";
        }
    }
    catch (Exception ex)
    {
        ErrorTextBlock.Text = $"Error: {ex.Message}";
    }
    finally
    {
        LoginButton.IsEnabled = true;
    }
}
```

---

### Opción 3: Login con CommunityToolkit.Mvvm (Más MVVM)

Usar comandos con `CommunityToolkit.Mvvm` (ya está en el proyecto):

#### Actualizar MainViewModel con Comandos
```csharp
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ViewModelBase
{
    // ... propiedades existentes ...
    
    private string _loginUsername;
    private string _loginPassword;
    private string _loginErrorMessage;
    private bool _isLoggingIn;

    public string LoginUsername
    {
        get => _loginUsername;
        set => SetProperty(ref _loginUsername, value);
    }

    public string LoginPassword
    {
        get => _loginPassword;
        set => SetProperty(ref _loginPassword, value);
    }

    public string LoginErrorMessage
    {
        get => _loginErrorMessage;
        set => SetProperty(ref _loginErrorMessage, value);
    }

    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set => SetProperty(ref _isLoggingIn, value);
    }

    [RelayCommand]
    private async Task PerformLoginAsync()
    {
        LoginErrorMessage = string.Empty;
        IsLoggingIn = true;

        try
        {
            var success = await LoginAsync(LoginUsername, LoginPassword);
            
            if (success)
            {
                // Login exitoso, inicializar navegación si es necesario
                LoginPassword = string.Empty; // Limpiar contraseña
            }
            else
            {
                LoginErrorMessage = "Usuario o contraseña incorrectos";
            }
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    [RelayCommand]
    private async Task PerformLogoutAsync()
    {
        await LogoutAsync();
    }
}
```

#### Usar en XAML con Binding
```xml
<TextBox Text="{Binding LoginUsername, Mode=TwoWay}" 
         PlaceholderText="Usuario" 
         IsEnabled="{Binding IsLoggingIn, Converter={StaticResource InverseBoolConverter}}"/>

<PasswordBox Password="{Binding LoginPassword, Mode=TwoWay}" 
             PlaceholderText="Contraseña"
             IsEnabled="{Binding IsLoggingIn, Converter={StaticResource InverseBoolConverter}}"/>

<Button Content="Iniciar Sesión" 
        Command="{Binding PerformLoginCommand}"
        IsEnabled="{Binding IsLoggingIn, Converter={StaticResource InverseBoolConverter}}"/>

<ProgressRing IsActive="{Binding IsLoggingIn}" />

<TextBlock Text="{Binding LoginErrorMessage}" 
           Foreground="Red" />
```

---

## 📊 BENEFICIOS DE LA ARQUITECTURA ACTUAL

### ✅ Separación de Responsabilidades
- **Vista (MainWindow.xaml):** Solo UI, sin lógica
- **Code-Behind (MainWindow.xaml.cs):** Mínimo, solo delegación
- **ViewModel (MainViewModel.cs):** Toda la lógica de presentación
- **Servicios:** Lógica de negocio reutilizable

### ✅ Testabilidad
```csharp
// Se puede testear el ViewModel sin UI
[Fact]
public async Task LoginAsync_ValidCredentials_SetsIsAuthenticatedTrue()
{
    // Arrange
    var mockAuthService = new Mock<IAuthService>();
    mockAuthService.Setup(x => x.AuthenticateAsync("user", "pass"))
                   .ReturnsAsync(true);
    var viewModel = new MainViewModel(..., mockAuthService.Object);
    
    // Act
    var result = await viewModel.LoginAsync("user", "pass");
    
    // Assert
    Assert.True(result);
    Assert.True(viewModel.IsAuthenticated);
}
```

### ✅ Mantenibilidad
- Código más limpio y organizado
- Fácil de modificar sin romper otras partes
- Lógica centralizada en el ViewModel

### ✅ Reutilización
- Los ViewModels pueden reutilizarse
- Los servicios son independientes
- Data binding reduce código repetitivo

---

## 🎯 ESTADO ACTUAL DEL PROYECTO

### Completado ✅
- [x] MainViewModel con todas las dependencias inyectadas
- [x] Lógica de navegación movida al ViewModel
- [x] Data binding configurado en MainWindow.xaml
- [x] MainWindow simplificado, solo delega al ViewModel
- [x] Métodos de login/logout preparados en MainViewModel
- [x] Propiedad IsAuthenticated para controlar acceso
- [x] Servicios de autenticación completamente implementados

### Pendiente (Para Futuro) 🔜
- [ ] Crear UI de login (Opción 1, 2 o 3 de arriba)
- [ ] Agregar validación de formulario de login
- [ ] Implementar "Recordar usuario"
- [ ] Agregar timeout de sesión automático
- [ ] Implementar "Olvidé mi contraseña"
- [ ] Agregar feedback visual durante login (loading spinner)

---

## 📖 REFERENCIAS TÉCNICAS

### Servicios Utilizados
- **IAuthService:** Autenticación, tokens, refresh
- **INavigationService:** Navegación entre vistas
- **IOnlineCheck:** Verificar conectividad con API
- **ILoggingService:** Registro de eventos y errores

### Patrones Implementados
- **MVVM (Model-View-ViewModel):** Separación de UI y lógica
- **Dependency Injection:** Inyección de dependencias
- **Repository Pattern:** En servicios de autenticación
- **Observer Pattern:** INotifyPropertyChanged para binding

---

**Fecha:** 2025-11-05  
**Estado:** ✅ COMPLETADO  
**Próximo Paso:** Implementar UI de login (elegir una de las 3 opciones)
