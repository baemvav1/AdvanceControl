using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.AccessControl;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.PermisosUi
{
    public class PermisoUiRuntimeService : IPermisoUiRuntimeService
    {
        private readonly IPermisoUiService _permisoUiService;
        private readonly IPermisoUiScanner _scanner;
        private readonly ILoggingService _logger;
        private readonly Dictionary<string, PermisoModuloDto> _modulos = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PermisoAccionModuloDto> _acciones = new(StringComparer.Ordinal);

        public bool IsInitialized { get; private set; }
        public int NivelUsuario { get; private set; }
        public IReadOnlyDictionary<string, PermisoModuloDto> Modulos => _modulos;

        public PermisoUiRuntimeService(IPermisoUiService permisoUiService, IPermisoUiScanner scanner, ILoggingService logger)
        {
            _permisoUiService = permisoUiService ?? throw new ArgumentNullException(nameof(permisoUiService));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync(int nivelUsuario, bool forceSync = false, CancellationToken cancellationToken = default)
        {
            NivelUsuario = nivelUsuario > 0 ? nivelUsuario : 8;
            AccessControlService.Current.SetNivel(NivelUsuario);

            // El SCAN+SYNC del catálogo es costoso (lee N archivos XAML del FS y
            // los parsea con XDocument, luego POST grande al servidor). El catálogo
            // sólo cambia con cada release del cliente, así que no necesita
            // ejecutarse en cada login del usuario.
            //
            // - forceSync=true (administrador re-sincronizando): se ejecuta inline.
            // - forceSync=false: se dispara fire-and-forget para no bloquear el login.
            if (forceSync)
            {
                await RunScanAndSyncAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (!IsInitialized)
            {
                _ = Task.Run(() => RunScanAndSyncAsync(CancellationToken.None));
            }

            // El GetCatalogo SÍ es necesario para validar permisos en la UI.
            var catalog = await _permisoUiService.GetCatalogoAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            _modulos.Clear();
            _acciones.Clear();

            foreach (var modulo in catalog)
            {
                _modulos[modulo.ClaveModulo] = modulo;
                foreach (var accion in modulo.Acciones ?? Enumerable.Empty<PermisoAccionModuloDto>())
                {
                    _acciones[accion.ClaveAccion] = accion;
                }
            }

            IsInitialized = true;
            _ = _logger.LogInformationAsync($"Permisos UI inicializados. Módulos: {_modulos.Count}, Acciones: {_acciones.Count}.", "PermisoUiRuntimeService", "InitializeAsync");
        }

        private async Task RunScanAndSyncAsync(CancellationToken cancellationToken)
        {
            try
            {
                var scannedModules = await _scanner.ScanAsync(cancellationToken).ConfigureAwait(false);
                if (scannedModules.Count > 0)
                {
                    await _permisoUiService.SyncCatalogoAsync(new PermisoUiSyncRequestDto
                    {
                        Modulos = scannedModules
                    }, cancellationToken).ConfigureAwait(false);
                    _ = _logger.LogInformationAsync($"Sincronización automática completada: {scannedModules.Count} módulos procesados.", "PermisoUiRuntimeService", "RunScanAndSyncAsync");
                }
                else
                {
                    _ = _logger.LogWarningAsync("El escáner no encontró módulos. La raíz del proyecto no fue localizada o no contiene archivos XAML.", "PermisoUiRuntimeService", "RunScanAndSyncAsync");
                }
            }
            catch (Exception ex)
            {
                _ = _logger.LogWarningAsync($"No fue posible sincronizar el catálogo de permisos UI en background: {ex.Message}", "PermisoUiRuntimeService", "RunScanAndSyncAsync");
            }
        }

        public void Reset()
        {
            _modulos.Clear();
            _acciones.Clear();
            NivelUsuario = 8;
            IsInitialized = false;
            AccessControlService.Current.SetNivel(8);
        }

        public bool CanAccessModule(string moduleKey)
        {
            if (!IsInitialized)
                return true;

            if (!_modulos.TryGetValue(moduleKey, out var modulo))
                return true;

            return NivelUsuario > 0 && NivelUsuario <= modulo.NivelRequerido;
        }

        public bool CanAccessAction(string actionKey)
        {
            if (!IsInitialized)
                return true;

            if (!_acciones.TryGetValue(actionKey, out var accion))
                return true;

            return NivelUsuario > 0 && NivelUsuario <= accion.NivelRequerido;
        }

        public bool TryGetModulo(string moduleKey, out PermisoModuloDto? modulo)
        {
            if (_modulos.TryGetValue(moduleKey, out var found))
            {
                modulo = found;
                return true;
            }

            modulo = null;
            return false;
        }

        public string BuildModuleKey(Type moduleType)
        {
            return PermisoUiKeyBuilder.BuildModuleKey(moduleType);
        }

        public string BuildActionKey(string moduleKey, string controlType, string controlKey)
        {
            return PermisoUiKeyBuilder.BuildActionKey(moduleKey, controlType, controlKey);
        }
    }
}
