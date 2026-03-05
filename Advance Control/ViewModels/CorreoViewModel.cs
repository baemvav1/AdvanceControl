using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Advance_Control.Services.Email;
using Advance_Control.Services.Security;
using CommunityToolkit.Mvvm.Input;

namespace Advance_Control.ViewModels;

public class CorreoViewModel : ViewModelBase
{
    private const string ClaveUsuario = "email_smtp_user";
    private const string ClavePassword = "email_smtp_password";

    private readonly ISecureStorage _storage;
    private readonly IEmailService _emailService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _estado = string.Empty;
    private bool _esVerificando;
    private bool _credencialesGuardadas;
    private string _firmaPath = string.Empty;

    public CorreoViewModel(ISecureStorage storage, IEmailService emailService)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));

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
        var usuario = await _storage.GetAsync(ClaveUsuario);
        var pass = await _storage.GetAsync(ClavePassword);

        Email = usuario ?? string.Empty;
        Password = pass ?? string.Empty;

        CredencialesGuardadas = !string.IsNullOrWhiteSpace(usuario) && !string.IsNullOrWhiteSpace(pass);

        Estado = CredencialesGuardadas
            ? "Credenciales cargadas desde almacenamiento seguro."
            : "No hay credenciales configuradas.";

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

        await _storage.SetAsync(ClaveUsuario, Email.Trim());
        await _storage.SetAsync(ClavePassword, Password);

        CredencialesGuardadas = true;
        Estado = "Credenciales guardadas correctamente.";

        // Actualizar ruta de firma según el email guardado
        FirmaPath = FirmaCorreoHelper.GetFirmaPath(Email.Trim());
    }

    private async Task VerificarConexionAsync()
    {
        // Guardar antes de verificar para que el servicio lea las credenciales actuales
        await GuardarAsync();

        EsVerificando = true;
        Estado = "Verificando conexión con el servidor...";

        try
        {
            var resultado = await _emailService.VerifyConnectionAsync();
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
