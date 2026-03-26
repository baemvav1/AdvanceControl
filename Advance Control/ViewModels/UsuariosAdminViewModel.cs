using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Services.PermisosUi;
using Advance_Control.Services.TipoUsuario;
using Advance_Control.Services.UsuariosAdmin;
using Advance_Control.Utilities;

namespace Advance_Control.ViewModels
{
    public class UsuariosAdminViewModel : ViewModelBase
    {
        private readonly IUsuarioAdminService _usuarioAdminService;
        private readonly IPermisoUiService _permisoUiService;
        private readonly ITipoUsuarioService _tipoUsuarioService;
        private readonly ILoggingService _logger;
        private ObservableCollection<UsuarioAdminDto> _usuarios = new();
        private ObservableCollection<PermisoModuloDto> _permisosModulo = new();
        private ObservableCollection<TipoUsuarioDto> _tiposUsuarioPermisos = new();
        private bool _isLoading;
        private bool _isLoadingPermisos;
        private string? _errorMessage;
        private string? _permisosErrorMessage;

        public UsuariosAdminViewModel(
            IUsuarioAdminService usuarioAdminService,
            IPermisoUiService permisoUiService,
            ITipoUsuarioService tipoUsuarioService,
            ILoggingService logger)
        {
            _usuarioAdminService = usuarioAdminService ?? throw new ArgumentNullException(nameof(usuarioAdminService));
            _permisoUiService = permisoUiService ?? throw new ArgumentNullException(nameof(permisoUiService));
            _tipoUsuarioService = tipoUsuarioService ?? throw new ArgumentNullException(nameof(tipoUsuarioService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ObservableCollection<UsuarioAdminDto> Usuarios
        {
            get => _usuarios;
            set => SetProperty(ref _usuarios, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<PermisoModuloDto> PermisosModulo
        {
            get => _permisosModulo;
            set => SetProperty(ref _permisosModulo, value);
        }

        public ObservableCollection<TipoUsuarioDto> TiposUsuarioPermisos
        {
            get => _tiposUsuarioPermisos;
            set => SetProperty(ref _tiposUsuarioPermisos, value);
        }

        public bool IsLoadingPermisos
        {
            get => _isLoadingPermisos;
            set => SetProperty(ref _isLoadingPermisos, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string? PermisosErrorMessage
        {
            get => _permisosErrorMessage;
            set
            {
                if (SetProperty(ref _permisosErrorMessage, value))
                {
                    OnPropertyChanged(nameof(HasPermisosError));
                }
            }
        }

        public bool HasPermisosError => !string.IsNullOrWhiteSpace(PermisosErrorMessage);

        public async Task LoadUsuariosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var usuarios = await _usuarioAdminService.GetUsuariosAsync(cancellationToken: cancellationToken);
                Usuarios.Clear();
                foreach (var usuario in usuarios)
                {
                    Usuarios.Add(usuario);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar usuarios: {ex.Message}";
                await _logger.LogErrorAsync("Error al cargar usuarios administrativos", ex, "UsuariosAdminViewModel", "LoadUsuariosAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<UsuarioAdminOperationResponse> CreateUsuarioAsync(UsuarioAdminEditDto request, CancellationToken cancellationToken = default)
        {
            var result = await _usuarioAdminService.CreateUsuarioAsync(request, cancellationToken);
            if (result.Success)
                await LoadUsuariosAsync(cancellationToken);

            return result;
        }

        public async Task<UsuarioAdminOperationResponse> UpdateUsuarioAsync(long credencialId, UsuarioAdminEditDto request, CancellationToken cancellationToken = default)
        {
            var result = await _usuarioAdminService.UpdateUsuarioAsync(credencialId, request, cancellationToken);
            if (result.Success)
                await LoadUsuariosAsync(cancellationToken);

            return result;
        }

        public async Task<UsuarioAdminOperationResponse> DeleteUsuarioAsync(long credencialId, CancellationToken cancellationToken = default)
        {
            var result = await _usuarioAdminService.DeleteUsuarioAsync(credencialId, cancellationToken);
            if (result.Success)
                await LoadUsuariosAsync(cancellationToken);

            return result;
        }

        public async Task LoadPermisosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoadingPermisos)
                return;

            try
            {
                IsLoadingPermisos = true;
                PermisosErrorMessage = null;

                var tiposUsuario = await _tipoUsuarioService.GetTiposUsuarioAsync(cancellationToken: cancellationToken);
                TiposUsuarioPermisos.Clear();
                foreach (var tipoUsuario in tiposUsuario)
                {
                    TiposUsuarioPermisos.Add(tipoUsuario);
                }

                var modulos = await _permisoUiService.GetCatalogoAsync(cancellationToken: cancellationToken);
                PermisosModulo.Clear();
                foreach (var modulo in modulos)
                {
                    PermisosModulo.Add(modulo);
                }
            }
            catch (Exception ex)
            {
                PermisosErrorMessage = $"Error al cargar permisos: {ex.Message}";
                await _logger.LogErrorAsync("Error al cargar permisos UI", ex, "UsuariosAdminViewModel", "LoadPermisosAsync");
            }
            finally
            {
                IsLoadingPermisos = false;
            }
        }

        public async Task SyncPermisosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                PermisosErrorMessage = null;
                var scanner = AppServices.Get<IPermisoUiScanner>();
                var modulos = await scanner.ScanAsync(cancellationToken);

                if (modulos.Count == 0)
                {
                    PermisosErrorMessage = "El escáner no encontró módulos. Verifique que la aplicación se ejecuta desde el directorio del proyecto.";
                    await _logger.LogWarningAsync("SyncPermisosAsync: el escáner retornó 0 módulos.", "UsuariosAdminViewModel", "SyncPermisosAsync");
                    await LoadPermisosAsync(cancellationToken);
                    return;
                }

                await _permisoUiService.SyncCatalogoAsync(new PermisoUiSyncRequestDto { Modulos = modulos }, cancellationToken);
                await LoadPermisosAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                PermisosErrorMessage = $"Error al sincronizar permisos: {ex.Message}";
                await _logger.LogErrorAsync("Error al sincronizar permisos UI", ex, "UsuariosAdminViewModel", "SyncPermisosAsync");
            }
        }

        public async Task UpdateNivelModuloAsync(int idPermisoModulo, int nivelRequerido, CancellationToken cancellationToken = default)
        {
            try
            {
                PermisosErrorMessage = null;
                await _permisoUiService.UpdateNivelModuloAsync(new PermisoModuloNivelUpdateDto
                {
                    IdPermisoModulo = idPermisoModulo,
                    NivelRequerido = nivelRequerido
                }, cancellationToken);

                await LoadPermisosAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                PermisosErrorMessage = $"Error al actualizar nivel del módulo: {ex.Message}";
                await _logger.LogErrorAsync("Error al actualizar nivel del módulo", ex, "UsuariosAdminViewModel", "UpdateNivelModuloAsync");
            }
        }

        public async Task UpdateNivelAccionAsync(int idPermisoAccionModulo, int nivelRequerido, CancellationToken cancellationToken = default)
        {
            try
            {
                PermisosErrorMessage = null;
                await _permisoUiService.UpdateNivelAccionAsync(new PermisoAccionNivelUpdateDto
                {
                    IdPermisoAccionModulo = idPermisoAccionModulo,
                    NivelRequerido = nivelRequerido
                }, cancellationToken);

                await LoadPermisosAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                PermisosErrorMessage = $"Error al actualizar nivel de la acción: {ex.Message}";
                await _logger.LogErrorAsync("Error al actualizar nivel de la acción", ex, "UsuariosAdminViewModel", "UpdateNivelAccionAsync");
            }
        }
    }
}
