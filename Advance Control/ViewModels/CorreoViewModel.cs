using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Advance_Control.Models;
using Advance_Control.Services.CorreoUsuario;
using Advance_Control.Services.Email;
using Advance_Control.Services.Session;
using CommunityToolkit.Mvvm.Input;

namespace Advance_Control.ViewModels;

public class CorreoViewModel : ViewModelBase
{
    private readonly ICorreoUsuarioService _correoUsuarioService;
    private readonly IEmailService _emailService;
    private readonly IUserSessionService _userSessionService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _estado = string.Empty;
    private bool _esVerificando;
    private bool _credencialesGuardadas;
    private string _firmaPath = string.Empty;

    public CorreoViewModel(ICorreoUsuarioService correoUsuarioService, IEmailService emailService, IUserSessionService userSessionService)
    {
        _correoUsuarioService = correoUsuarioService ?? throw new ArgumentNullException(nameof(correoUsuarioService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _userSessionService = userSessionService ?? throw new ArgumentNullException(nameof(userSessionService));

        GuardarCommand = new AsyncRelayCommand(GuardarAsync);
        VerificarConexionCommand = new AsyncRelayCommand(VerificarConexionAsync, PuedeVerificar);
        EliminarFirmaCommand = new RelayCommand(EliminarFirma, () => TieneFirma);
    }

    // -------------------------------------------------------------------------
    // Propiedades
    // -------------------------------------------------------------------------

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
                ((AsyncRelayCommand)VerificarConexionCommand).NotifyCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
                ((AsyncRelayCommand)VerificarConexionCommand).NotifyCanExecuteChanged();
        }
    }

    public string Estado
    {
        get => _estado;
        set
        {
            if (SetProperty(ref _estado, value))
            {
                OnPropertyChanged(nameof(TieneEstado));
                OnPropertyChanged(nameof(EstadoExitoso));
            }
        }
    }

    public bool EsVerificando
    {
        get => _esVerificando;
        set => SetProperty(ref _esVerificando, value);
    }

    public bool CredencialesGuardadas
    {
        get => _credencialesGuardadas;
        set => SetProperty(ref _credencialesGuardadas, value);
    }

    /// <summary>Ruta de la imagen de firma configurada para el correo actual.</summary>
    public string FirmaPath
    {
        get => _firmaPath;
        set
        {
            if (SetProperty(ref _firmaPath, value))
            {
                OnPropertyChanged(nameof(TieneFirma));
                ((RelayCommand)EliminarFirmaCommand).NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>True cuando hay una imagen de firma configurada.</summary>
    public bool TieneFirma => !string.IsNullOrEmpty(_firmaPath);

    /// <summary>True cuando hay un mensaje de estado que mostrar.</summary>
    public bool TieneEstado => !string.IsNullOrEmpty(_estado);

    /// <summary>True cuando el estado representa un resultado exitoso.</summary>
    public bool EstadoExitoso =>
        _estado.Contains("exitosa", StringComparison.OrdinalIgnoreCase) ||
        _estado.Contains("correctamente", StringComparison.OrdinalIgnoreCase) ||
        _estado.Contains("válidas", StringComparison.OrdinalIgnoreCase);

    // -------------------------------------------------------------------------
    // Comandos
    // -------------------------------------------------------------------------

    public ICommand GuardarCommand { get; }
    public ICommand VerificarConexionCommand { get; }
    public ICommand EliminarFirmaCommand { get; }

    // -------------------------------------------------------------------------
    // Carga inicial
    // -------------------------------------------------------------------------

    public async Task LoadAsync()
    {
        var configuracion = await _correoUsuarioService.GetCorreoActualAsync();

        Email = configuracion?.Email ?? string.Empty;
        Password = configuracion?.Password ?? string.Empty;

        CredencialesGuardadas = !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

        Estado = CredencialesGuardadas
            ? "Credenciales de correo cargadas para el usuario actual."
            : "El usuario actual no tiene credenciales de correo configuradas.";

        // Cargar firma si existe
        if (!string.IsNullOrWhiteSpace(Email))
            FirmaPath = FirmaCorreoHelper.GetFirmaPath(Email);
    }

    // -------------------------------------------------------------------------
    // Lógica
    // -------------------------------------------------------------------------

    private async Task GuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            Estado = "El correo y la contraseña son obligatorios.";
            return;
        }

        if (!_userSessionService.IsLoaded)
            await _userSessionService.LoadAsync();

        if (_userSessionService.CredencialId <= 0)
        {
            Estado = "No fue posible determinar el usuario actual.";
            return;
        }

        var result = await _correoUsuarioService.SaveCorreoUsuarioAsync(
            _userSessionService.CredencialId,
            new CorreoUsuarioEditDto
            {
                Email = Email.Trim(),
                Password = Password
            });

        if (!result.Success)
        {
            Estado = result.Message;
            return;
        }

        CredencialesGuardadas = true;
        Estado = "Credenciales guardadas correctamente para el usuario actual.";

        // Actualizar ruta de firma según el email guardado
        FirmaPath = FirmaCorreoHelper.GetFirmaPath(Email.Trim());
    }

    private async Task VerificarConexionAsync()
    {
        EsVerificando = true;
        Estado = "Verificando conexión con el servidor...";

        try
        {
            var resultado = await _emailService.VerifyConnectionAsync(Email.Trim(), Password);
            Estado = resultado;
        }
        finally
        {
            EsVerificando = false;
        }
    }

    private bool PuedeVerificar() =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    /// <summary>Actualiza la ruta de firma tras cargar una nueva imagen (llamado desde la View).</summary>
    public void ActualizarFirma(string nuevaRuta)
    {
        FirmaPath = nuevaRuta;
    }

    private void EliminarFirma()
    {
        FirmaCorreoHelper.EliminarFirma(Email);
        FirmaPath = string.Empty;
        Estado = "Firma de correo eliminada.";
    }
}
