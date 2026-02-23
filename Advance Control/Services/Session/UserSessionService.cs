using System;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;
using Advance_Control.Services.UserInfo;

namespace Advance_Control.Services.Session
{
    /// <summary>
    /// Implementación singleton de IUserSessionService.
    /// Llama al API una sola vez (en LoadAsync) y mantiene los datos en memoria
    /// para que estén disponibles en tiempo real desde cualquier parte del código.
    /// </summary>
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserInfoService _userInfoService;
        private readonly ILoggingService _logger;

        public int IdUsuario => CredencialId;
        public int CredencialId { get; private set; }
        public int IdProveedor { get; private set; }
        public string? NombreCompleto { get; private set; }
        public string? Correo { get; private set; }
        public string? Telefono { get; private set; }
        public int Nivel { get; private set; }
        public string? TipoUsuario { get; private set; }
        public bool IsLoaded { get; private set; }

        public UserSessionService(IUserInfoService userInfoService, ILoggingService logger)
        {
            _userInfoService = userInfoService ?? throw new ArgumentNullException(nameof(userInfoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene la información del usuario desde el API y la almacena en memoria.
        /// Si la sesión ya fue cargada, no realiza una segunda llamada al API.
        /// </summary>
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var userInfo = await _userInfoService.GetUserInfoAsync(cancellationToken);
                if (userInfo != null)
                {
                    CredencialId = userInfo.CredencialId;
                    IdProveedor = userInfo.IdProveedor;
                    NombreCompleto = userInfo.NombreCompleto;
                    Correo = userInfo.Correo;
                    Telefono = userInfo.Telefono;
                    Nivel = userInfo.Nivel;
                    TipoUsuario = userInfo.TipoUsuario;
                    IsLoaded = true;
                    await _logger.LogInformationAsync(
                        $"Sesión de usuario cargada: CredencialId={CredencialId}, IdProveedor={IdProveedor}",
                        "UserSessionService", "LoadAsync");
                }
                else
                {
                    await _logger.LogWarningAsync(
                        "No se pudo obtener información del usuario desde el API",
                        "UserSessionService", "LoadAsync");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar sesión de usuario", ex, "UserSessionService", "LoadAsync");
            }
        }

        /// <summary>
        /// Limpia todos los datos de sesión. Llamar al cerrar sesión.
        /// </summary>
        public void Clear()
        {
            CredencialId = 0;
            IdProveedor = 0;
            NombreCompleto = null;
            Correo = null;
            Telefono = null;
            Nivel = 0;
            TipoUsuario = null;
            IsLoaded = false;
        }
    }
}
