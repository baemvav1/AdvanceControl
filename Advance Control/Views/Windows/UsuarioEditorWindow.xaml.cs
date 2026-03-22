using Advance_Control.Models;
using Advance_Control.Services.CorreoUsuario;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Email;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Proveedores;
using Advance_Control.Services.TipoUsuario;
using Advance_Control.Services.UsuariosAdmin;
using Advance_Control.Utilities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    public sealed partial class UsuarioEditorWindow : Window
    {
        private readonly IUsuarioAdminService _usuarioAdminService;
        private readonly ICorreoUsuarioService _correoUsuarioService;
        private readonly IContactoService _contactoService;
        private readonly IClienteService _clienteService;
        private readonly IProveedorService _proveedorService;
        private readonly ITipoUsuarioService _tipoUsuarioService;
        private readonly IEmailService _emailService;
        private readonly INotificacionService _notificacionService;
        private readonly ILoggingService _loggingService;
        private readonly UsuarioAdminDto? _usuario;
        private bool _contactosCargados;
        private bool _cargandoDesdeContacto;
        private bool _limpiarIdProveedor;
        private bool _limpiarIdCliente;
        private bool _limpiarCorreoUsuario;
        private bool _tieneCorreoConfigurado;

        public ObservableCollection<ContactoDto> Contactos { get; } = new();
        public ObservableCollection<CustomerDto> Clientes { get; } = new();
        public ObservableCollection<ProveedorDto> Proveedores { get; } = new();
        public ObservableCollection<TipoUsuarioDto> TiposUsuario { get; } = new();

        public bool SavedChanges { get; private set; }

        public UsuarioEditorWindow(UsuarioAdminDto? usuario = null)
        {
            _usuarioAdminService = AppServices.Get<IUsuarioAdminService>();
            _correoUsuarioService = AppServices.Get<ICorreoUsuarioService>();
            _contactoService = AppServices.Get<IContactoService>();
            _clienteService = AppServices.Get<IClienteService>();
            _proveedorService = AppServices.Get<IProveedorService>();
            _tipoUsuarioService = AppServices.Get<ITipoUsuarioService>();
            _emailService = AppServices.Get<IEmailService>();
            _notificacionService = AppServices.Get<INotificacionService>();
            _loggingService = AppServices.Get<ILoggingService>();
            _usuario = usuario;

            InitializeComponent();
            RootGrid.DataContext = this;

            Title = usuario == null ? "Nuevo usuario" : $"Editar usuario - {usuario.Usuario}";
            TitleTextBlock.Text = usuario == null ? "Nuevo usuario" : $"Editar usuario - {usuario.Usuario}";
            PasswordBox.PlaceholderText = usuario == null ? "Contraseña inicial" : "Nueva contraseña (opcional)";

            AjustarTamano(1100, 820);
            CargarFormularioDesdeUsuario();
            Activated += UsuarioEditorWindow_Activated;
        }

        private async void UsuarioEditorWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= UsuarioEditorWindow_Activated;
            await CargarCatalogosAsync();
        }

        private void AjustarTamano(int ancho, int alto)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(ancho, alto));
        }

        private void CargarFormularioDesdeUsuario()
        {
            if (_usuario == null)
            {
                EstaActivaCheckBox.IsChecked = true;
                return;
            }

            UsuarioTextBox.Text = _usuario.Usuario;
            EstaActivaCheckBox.IsChecked = _usuario.EstaActiva;
            NombreTextBox.Text = _usuario.Nombre ?? string.Empty;
            ApellidoTextBox.Text = _usuario.Apellido ?? string.Empty;
            CorreoTextBox.Text = _usuario.Correo ?? string.Empty;
            TelefonoTextBox.Text = _usuario.Telefono ?? string.Empty;
            DepartamentoTextBox.Text = _usuario.Departamento ?? string.Empty;
            CargoTextBox.Text = _usuario.Cargo ?? string.Empty;
            TratamientoTextBox.Text = _usuario.Tratamiento ?? string.Empty;
            CodigoInternoTextBox.Text = _usuario.CodigoInterno ?? string.Empty;
            NotasTextBox.Text = _usuario.Notas ?? string.Empty;
        }

        private async Task CargarCatalogosAsync()
        {
            await Task.WhenAll(
                CargarTiposUsuarioAsync(),
                CargarContactosAsync(),
                CargarClientesAsync(),
                CargarProveedoresAsync());

            await CargarCorreoUsuarioAsync();
        }

        private async Task CargarTiposUsuarioAsync()
        {
            try
            {
                var tiposUsuario = await _tipoUsuarioService.GetTiposUsuarioAsync();
                TiposUsuario.Clear();
                foreach (var tipoUsuario in tiposUsuario.OrderBy(t => t.IdTipoUsuario))
                {
                    TiposUsuario.Add(tipoUsuario);
                }

                NivelComboBox.ItemsSource = TiposUsuario;
                var nivelSeleccionado = _usuario?.Nivel ?? 1;
                NivelComboBox.SelectedItem = TiposUsuario.FirstOrDefault(t => t.IdTipoUsuario == nivelSeleccionado)
                    ?? TiposUsuario.FirstOrDefault();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar tipos de usuario en UsuarioEditorWindow", ex, nameof(UsuarioEditorWindow), nameof(CargarTiposUsuarioAsync));
                await _notificacionService.MostrarAsync("Error", "No fue posible cargar los tipos de usuario.");
            }
        }

        private async Task CargarContactosAsync()
        {
            try
            {
                var contactos = await _contactoService.GetContactosAsync();
                Contactos.Clear();
                foreach (var contacto in contactos
                    .Where(ContactoDisponibleParaUsuario)
                    .OrderBy(c => c.NombreCompleto))
                {
                    Contactos.Add(contacto);
                }

                ContactoComboBox.ItemsSource = Contactos;
                _contactosCargados = true;

                if (_usuario?.ContactoId is long contactoId)
                {
                    ContactoComboBox.SelectedItem = Contactos.FirstOrDefault(c => c.ContactoId == contactoId);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar contactos en UsuarioEditorWindow", ex, nameof(UsuarioEditorWindow), nameof(CargarContactosAsync));
                await _notificacionService.MostrarAsync("Error", "No fue posible cargar los contactos.");
            }
        }

        private async Task CargarClientesAsync()
        {
            try
            {
                var clientes = await _clienteService.GetClientesAsync();
                Clientes.Clear();
                foreach (var cliente in clientes.OrderBy(c => c.NombreComercial).ThenBy(c => c.RazonSocial))
                {
                    Clientes.Add(cliente);
                }

                ClienteComboBox.ItemsSource = Clientes;
                SeleccionarCliente(_usuario?.IdCliente);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar clientes en UsuarioEditorWindow", ex, nameof(UsuarioEditorWindow), nameof(CargarClientesAsync));
                await _notificacionService.MostrarAsync("Error", "No fue posible cargar los clientes.");
            }
        }

        private async Task CargarProveedoresAsync()
        {
            try
            {
                var proveedores = await _proveedorService.GetProveedoresAsync();
                Proveedores.Clear();
                foreach (var proveedor in proveedores.OrderBy(p => p.NombreComercial).ThenBy(p => p.RazonSocial))
                {
                    Proveedores.Add(proveedor);
                }

                ProveedorComboBox.ItemsSource = Proveedores;
                SeleccionarProveedor(_usuario?.IdProveedor);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar proveedores en UsuarioEditorWindow", ex, nameof(UsuarioEditorWindow), nameof(CargarProveedoresAsync));
                await _notificacionService.MostrarAsync("Error", "No fue posible cargar los proveedores.");
            }
        }

        private async void RecargarContactosButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarContactosAsync();
        }

        private void QuitarContactoButton_Click(object sender, RoutedEventArgs e)
        {
            ContactoComboBox.SelectedItem = null;
        }

        private async void RecargarProveedoresButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarProveedoresAsync();
        }

        private void QuitarProveedorButton_Click(object sender, RoutedEventArgs e)
        {
            ProveedorComboBox.SelectedItem = null;
            _limpiarIdProveedor = true;
        }

        private async void RecargarClientesButton_Click(object sender, RoutedEventArgs e)
        {
            await CargarClientesAsync();
        }

        private void QuitarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            ClienteComboBox.SelectedItem = null;
            _limpiarIdCliente = true;
        }

        private void ContactoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_contactosCargados || _cargandoDesdeContacto)
                return;

            if (ContactoComboBox.SelectedItem is not ContactoDto contacto)
                return;

            _cargandoDesdeContacto = true;
            try
            {
                NombreTextBox.Text = contacto.Nombre ?? string.Empty;
                ApellidoTextBox.Text = contacto.Apellido ?? string.Empty;
                CorreoTextBox.Text = contacto.Correo ?? string.Empty;
                TelefonoTextBox.Text = contacto.Telefono ?? string.Empty;
                DepartamentoTextBox.Text = contacto.Departamento ?? string.Empty;
                CargoTextBox.Text = contacto.Cargo ?? string.Empty;
                SeleccionarProveedor(contacto.IdProveedor);
                SeleccionarCliente(contacto.IdCliente);
                TratamientoTextBox.Text = contacto.Tratamiento ?? string.Empty;
                CodigoInternoTextBox.Text = contacto.CodigoInterno ?? string.Empty;
                NotasTextBox.Text = contacto.Notas ?? string.Empty;
            }
            finally
            {
                _cargandoDesdeContacto = false;
            }
        }

        private void ProveedorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cargandoDesdeContacto)
                return;

            _limpiarIdProveedor = ProveedorComboBox.SelectedItem is null;
        }

        private void ClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_cargandoDesdeContacto)
                return;

            _limpiarIdCliente = ClienteComboBox.SelectedItem is null;
        }

        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsuarioTextBox.Text))
            {
                await _notificacionService.MostrarAsync("Validación", "El login es obligatorio.");
                return;
            }

            if (_usuario == null && string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                await _notificacionService.MostrarAsync("Validación", "La contraseña inicial es obligatoria.");
                return;
            }

            try
            {
                var request = BuildRequest();
                UsuarioAdminOperationResponse result = _usuario == null
                    ? await _usuarioAdminService.CreateUsuarioAsync(request)
                    : await _usuarioAdminService.UpdateUsuarioAsync(_usuario.CredencialId, request);

                if (!result.Success)
                {
                    await _notificacionService.MostrarAsync("Error", result.Message);
                    return;
                }

                await GuardarCorreoUsuarioAsync(result.CredencialId);

                await _notificacionService.MostrarAsync("Usuario guardado", result.Message);

                SavedChanges = true;
                Close();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al guardar usuario desde UsuarioEditorWindow", ex, nameof(UsuarioEditorWindow), nameof(GuardarButton_Click));
                await _notificacionService.MostrarAsync("Error", ex.Message);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private UsuarioAdminEditDto BuildRequest()
        {
            return new UsuarioAdminEditDto
            {
                Usuario = Normalize(UsuarioTextBox.Text),
                Password = string.IsNullOrWhiteSpace(PasswordBox.Password) ? null : PasswordBox.Password,
                Nivel = (NivelComboBox.SelectedItem as TipoUsuarioDto)?.IdTipoUsuario,
                EstaActiva = EstaActivaCheckBox.IsChecked ?? true,
                ContactoId = (ContactoComboBox.SelectedItem as ContactoDto)?.ContactoId,
                Nombre = Normalize(NombreTextBox.Text),
                Apellido = Normalize(ApellidoTextBox.Text),
                Correo = Normalize(CorreoTextBox.Text),
                Telefono = Normalize(TelefonoTextBox.Text),
                Departamento = Normalize(DepartamentoTextBox.Text),
                CodigoInterno = Normalize(CodigoInternoTextBox.Text),
                Cargo = Normalize(CargoTextBox.Text),
                IdProveedor = (ProveedorComboBox.SelectedItem as ProveedorDto)?.IdProveedor,
                LimpiarIdProveedor = _limpiarIdProveedor,
                IdCliente = (ClienteComboBox.SelectedItem as CustomerDto)?.IdCliente,
                LimpiarIdCliente = _limpiarIdCliente,
                Tratamiento = Normalize(TratamientoTextBox.Text),
                Notas = Normalize(NotasTextBox.Text)
            };
        }

        private void SeleccionarProveedor(int? idProveedor)
        {
            ProveedorComboBox.SelectedItem = idProveedor.HasValue
                ? Proveedores.FirstOrDefault(p => p.IdProveedor == idProveedor.Value)
                : null;
            _limpiarIdProveedor = !idProveedor.HasValue;
        }

        private void SeleccionarCliente(int? idCliente)
        {
            ClienteComboBox.SelectedItem = idCliente.HasValue
                ? Clientes.FirstOrDefault(c => c.IdCliente == idCliente.Value)
                : null;
            _limpiarIdCliente = !idCliente.HasValue;
        }

        private bool ContactoDisponibleParaUsuario(ContactoDto contacto)
        {
            return contacto.CredencialId == null
                || contacto.CredencialId == 0
                || contacto.CredencialId == _usuario?.CredencialId;
        }

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task CargarCorreoUsuarioAsync()
        {
            if (_usuario == null)
            {
                CorreoInfoBar.IsOpen = false;
                return;
            }

            try
            {
                var correo = await _correoUsuarioService.GetCorreoUsuarioAsync(_usuario.CredencialId);
                CorreoUsuarioTextBox.Text = correo?.Email ?? string.Empty;
                CorreoPasswordBox.Password = correo?.Password ?? string.Empty;
                _limpiarCorreoUsuario = false;
                _tieneCorreoConfigurado = correo != null;
                CorreoInfoBar.IsOpen = false;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el correo del usuario", ex, nameof(UsuarioEditorWindow), nameof(CargarCorreoUsuarioAsync));
                MostrarEstadoCorreo("No fue posible cargar la configuración de correo del usuario.", InfoBarSeverity.Error);
            }
        }

        private async Task GuardarCorreoUsuarioAsync(long credencialId)
        {
            var email = Normalize(CorreoUsuarioTextBox.Text);
            var password = string.IsNullOrWhiteSpace(CorreoPasswordBox.Password) ? null : CorreoPasswordBox.Password;

            if (_limpiarCorreoUsuario || string.IsNullOrWhiteSpace(email))
            {
                var deleteResult = await _correoUsuarioService.DeleteCorreoUsuarioAsync(credencialId);
                if (!deleteResult.Success)
                    throw new InvalidOperationException(deleteResult.Message);

                _tieneCorreoConfigurado = false;
                return;
            }

            if (!_tieneCorreoConfigurado && string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("La contraseña SMTP es obligatoria cuando se captura un correo.");

            var saveResult = await _correoUsuarioService.SaveCorreoUsuarioAsync(
                credencialId,
                new CorreoUsuarioEditDto
                {
                    Email = email,
                    Password = password
                });

            if (!saveResult.Success)
                throw new InvalidOperationException(saveResult.Message);

            _tieneCorreoConfigurado = true;
        }

        private async void VerificarCorreoButton_Click(object sender, RoutedEventArgs e)
        {
            MostrarEstadoCorreo("Verificando conexión con el servidor...", InfoBarSeverity.Informational);

            try
            {
                var resultado = await _emailService.VerifyConnectionAsync(
                    CorreoUsuarioTextBox.Text.Trim(),
                    CorreoPasswordBox.Password);

                var esExitoso = resultado.Contains("exitosa", StringComparison.OrdinalIgnoreCase)
                    || resultado.Contains("válidas", StringComparison.OrdinalIgnoreCase);

                MostrarEstadoCorreo(resultado, esExitoso ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al verificar el correo del usuario", ex, nameof(UsuarioEditorWindow), nameof(VerificarCorreoButton_Click));
                MostrarEstadoCorreo("No fue posible verificar la conexión del correo.", InfoBarSeverity.Error);
            }
        }

        private void QuitarCorreoButton_Click(object sender, RoutedEventArgs e)
        {
            CorreoUsuarioTextBox.Text = string.Empty;
            CorreoPasswordBox.Password = string.Empty;
            _limpiarCorreoUsuario = true;
            _tieneCorreoConfigurado = false;
            MostrarEstadoCorreo("La configuración de correo se eliminará al guardar.", InfoBarSeverity.Warning);
        }

        private void MostrarEstadoCorreo(string mensaje, InfoBarSeverity severity)
        {
            CorreoInfoBar.Message = mensaje;
            CorreoInfoBar.Severity = severity;
            CorreoInfoBar.IsOpen = true;
        }
    }
}
