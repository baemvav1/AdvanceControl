using System;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.AccessControl;
using Advance_Control.Services.Logging;
using Advance_Control.Services.PermisosUi;
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
        private readonly IPermisoUiRuntimeService _permisoUiRuntimeService;
        private readonly SemaphoreSlim _loadLock = new(1, 1);

        public int CredencialId { get; private set; }
        public int IdProveedor { get; private set; }
        public string? NombreCompleto { get; private set; }
        public string? Correo { get; private set; }
        public string? Telefono { get; private set; }
        public int Nivel { get; private set; }
        public string? TipoUsuario { get; private set; }
        public bool IsLoaded { get; private set; }

        public UserSessionService(IUserInfoService userInfoService, ILoggingService logger, IPermisoUiRuntimeService permisoUiRuntimeService)
        {
            _userInfoService = userInfoService ?? throw new ArgumentNullException(nameof(userInfoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permisoUiRuntimeService = permisoUiRuntimeService ?? throw new ArgumentNullException(nameof(permisoUiRuntimeService));
        }

        /// <summary>
        /// Obtiene la información del usuario desde el API y la almacena en memoria.
        /// Si la sesión ya fue cargada, no realiza una segunda llamada al API.
        /// </summary>
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoaded) return;

            await _loadLock.WaitAsync(cancellationToken);

            try
            {
                if (IsLoaded) return;

                var userInfo = await _userInfoService.GetUserInfoAsync(cancellationToken);
                if (userInfo == null)
                    throw new InvalidOperationException("La API no devolvió la información del usuario autenticado.");

                CredencialId = userInfo.CredencialId;
                IdProveedor = userInfo.IdProveedor;
                NombreCompleto = userInfo.NombreCompleto;
                Correo = userInfo.Correo;
                Telefono = userInfo.Telefono;
                Nivel = userInfo.Nivel;
                TipoUsuario = userInfo.TipoUsuario;
                AccessControlService.Current.SetNivel(Nivel);

                await _permisoUiRuntimeService.InitializeAsync(Nivel, cancellationToken: cancellationToken);

                IsLoaded = true;

                await _logger.LogInformationAsync(
                    $"Sesión de usuario cargada: CredencialId={CredencialId}, IdProveedor={IdProveedor}",
                    "UserSessionService", "LoadAsync");
            }
            catch (Exception ex)
            {
                Clear();
                await _logger.LogErrorAsync("Error al cargar sesión de usuario", ex, "UserSessionService", "LoadAsync");
                throw;
            }
            finally
            {
                _loadLock.Release();
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
            _permisoUiRuntimeService.Reset();
        }
    }
}
