using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Advance_Control.Services.DevOps;
using Advance_Control.Services.Logging;
using Advance_Control.Services.PermisosUi;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la página de herramientas DevOps.
    /// </summary>
    public class DevOpsViewModel : ViewModelBase
    {
        private readonly IDevOpsService _devOpsService;
        private readonly ILoggingService _logger;
        private readonly IPermisoUiRuntimeService _permisoRuntime;

        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private bool _hasError;

        public DevOpsViewModel(IDevOpsService devOpsService, ILoggingService logger, IPermisoUiRuntimeService permisoRuntime)
        {
            _devOpsService = devOpsService ?? throw new ArgumentNullException(nameof(devOpsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permisoRuntime = permisoRuntime ?? throw new ArgumentNullException(nameof(permisoRuntime));

            Estadisticas = new ObservableCollection<DevOpsStatsResult>();
            UltimosResultados = new ObservableCollection<DevOpsWipeResult>();
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public ObservableCollection<DevOpsStatsResult> Estadisticas { get; }
        public ObservableCollection<DevOpsWipeResult> UltimosResultados { get; }

        /// <summary>Carga las estadísticas de la base de datos.</summary>
        public async Task CargarEstadisticasAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                StatusMessage = "Cargando estadísticas...";

                var stats = await _devOpsService.ObtenerEstadisticasAsync();

                Estadisticas.Clear();
                foreach (var s in stats)
                    Estadisticas.Add(s);

                StatusMessage = $"Estadísticas cargadas: {stats.Count} tablas";
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Error al cargar estadísticas: {ex.Message}";
                await _logger.LogErrorAsync(ex.Message, ex, "DevOpsViewModel", "CargarEstadisticasAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Ejecuta una limpieza por módulo.</summary>
        public async Task<bool> EjecutarLimpiezaAsync(string modulo)
        {
            try
            {
                IsLoading = true;
                HasError = false;
                StatusMessage = $"Limpiando {modulo}...";

                var resultados = modulo.ToLowerInvariant() switch
                {
                    "financiero" => await _devOpsService.LimpiarFinancieroAsync(),
                    "operaciones" => await _devOpsService.LimpiarOperacionesAsync(),
                    "mantenimiento" => await _devOpsService.LimpiarMantenimientoAsync(),
                    "levantamientos" => await _devOpsService.LimpiarLevantamientosAsync(),
                    "servicios" => await _devOpsService.LimpiarServiciosAsync(),
                    "logs" => await _devOpsService.LimpiarLogsAsync(),
                    "ubicaciones" => await _devOpsService.LimpiarUbicacionesAsync(),
                    "permisos" => await _devOpsService.LimpiarPermisosAsync(),
                    _ => throw new ArgumentException($"Módulo desconocido: {modulo}")
                };

                UltimosResultados.Clear();
                long totalEliminados = 0;
                foreach (var r in resultados)
                {
                    UltimosResultados.Add(r);
                    totalEliminados += r.RegistrosEliminados;
                }

                StatusMessage = $"Limpieza de {modulo} completada. {totalEliminados} registros eliminados en {resultados.Count} tablas.";

                // Si se limpiaron permisos, regenerar automáticamente
                if (modulo.Equals("permisos", StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage += " Regenerando permisos...";
                    await _permisoRuntime.InitializeAsync(_permisoRuntime.NivelUsuario, forceSync: true);
                    StatusMessage = $"Permisos reseteados y regenerados correctamente. {totalEliminados} registros eliminados.";
                }

                // Recargar estadísticas automáticamente
                await CargarEstadisticasAsync();

                return true;
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Error al limpiar {modulo}: {ex.Message}";
                await _logger.LogErrorAsync(ex.Message, ex, "DevOpsViewModel", "EjecutarLimpiezaAsync");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
        /// <summary>Ejecuta una limpieza de conciliación por rango de fechas.</summary>
        public async Task<bool> EjecutarLimpiezaRangoAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                IsLoading = true;
                HasError = false;
                StatusMessage = $"Limpiando conciliación del {fechaInicio:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}...";

                var resultados = await _devOpsService.LimpiarConciliacionPorRangoAsync(fechaInicio, fechaFin);

                UltimosResultados.Clear();
                long totalEliminados = 0;
                foreach (var r in resultados)
                {
                    UltimosResultados.Add(r);
                    totalEliminados += r.RegistrosEliminados;
                }

                StatusMessage = $"Limpieza de conciliación completada. {totalEliminados} registros eliminados en {resultados.Count} tablas.";

                await CargarEstadisticasAsync();

                return true;
            }
            catch (Exception ex)
            {
                HasError = true;
                StatusMessage = $"Error al limpiar conciliación por rango: {ex.Message}";
                await _logger.LogErrorAsync(ex.Message, ex, "DevOpsViewModel", "EjecutarLimpiezaRangoAsync");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
