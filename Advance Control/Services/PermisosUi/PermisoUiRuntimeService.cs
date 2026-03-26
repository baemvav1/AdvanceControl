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

            if (!IsInitialized || forceSync)
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
                        await _logger.LogInformationAsync($"Sincronización automática completada: {scannedModules.Count} módulos procesados.", "PermisoUiRuntimeService", "InitializeAsync");
                    }
                    else
                    {
                        await _logger.LogWarningAsync("El escáner no encontró módulos. La raíz del proyecto no fue localizada o no contiene archivos XAML.", "PermisoUiRuntimeService", "InitializeAsync");
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogWarningAsync($"No fue posible sincronizar el catálogo de permisos UI durante la inicialización: {ex.Message}", "PermisoUiRuntimeService", "InitializeAsync");
                }
            }

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
            await _logger.LogInformationAsync($"Permisos UI inicializados. Módulos: {_modulos.Count}, Acciones: {_acciones.Count}.", "PermisoUiRuntimeService", "InitializeAsync");
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
