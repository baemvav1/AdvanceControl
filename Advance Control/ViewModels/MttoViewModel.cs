using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Mantenimiento;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Mantenimiento (Mtto).
    /// Gestiona la lógica de presentación para operaciones de mantenimiento.
    /// </summary>
    public class MttoViewModel : ViewModelBase
    {
        private readonly IMantenimientoService _mantenimientoService;
        private readonly ILoggingService _logger;
        private ObservableCollection<MantenimientoDto> _mantenimientos;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _identificadorFilter;
        private int _idClienteFilter;

        public MttoViewModel(IMantenimientoService mantenimientoService, ILoggingService logger)
        {
            _mantenimientoService = mantenimientoService ?? throw new ArgumentNullException(nameof(mantenimientoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mantenimientos = new ObservableCollection<MantenimientoDto>();
        }

        public ObservableCollection<MantenimientoDto> Mantenimientos
        {
            get => _mantenimientos;
            set => SetProperty(ref _mantenimientos, value);
        }

        /// <summary>
        /// Indica si hay una operación en curso
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
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

        /// <summary>
        /// Indica si hay un mensaje de error activo
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string? IdentificadorFilter
        {
            get => _identificadorFilter;
            set => SetProperty(ref _identificadorFilter, value);
        }

        public int IdClienteFilter
        {
            get => _idClienteFilter;
            set => SetProperty(ref _idClienteFilter, value);
        }

        /// <summary>
        /// Carga los mantenimientos desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadMantenimientosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await _logger.LogInformationAsync("Cargando mantenimientos...", "MttoViewModel", "LoadMantenimientosAsync");

                var query = new MantenimientoQueryDto
                {
                    Identificador = IdentificadorFilter,
                    IdCliente = IdClienteFilter
                };

                var mantenimientos = await _mantenimientoService.GetMantenimientosAsync(query, cancellationToken);

                Mantenimientos.Clear();
                foreach (var mantenimiento in mantenimientos)
                {
                    Mantenimientos.Add(mantenimiento);
                }

                await _logger.LogInformationAsync($"Se cargaron {mantenimientos.Count} mantenimientos exitosamente", "MttoViewModel", "LoadMantenimientosAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de mantenimientos cancelada por el usuario", "MttoViewModel", "LoadMantenimientosAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar mantenimientos", ex, "MttoViewModel", "LoadMantenimientosAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar mantenimientos: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar mantenimientos", ex, "MttoViewModel", "LoadMantenimientosAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los mantenimientos
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IdentificadorFilter = null;
                IdClienteFilter = 0;
                ErrorMessage = null;
                await LoadMantenimientosAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar mantenimientos.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "MttoViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina un mantenimiento por su ID
        /// </summary>
        public async Task<bool> DeleteMantenimientoAsync(int idMantenimiento, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando mantenimiento {idMantenimiento}...", "MttoViewModel", "DeleteMantenimientoAsync");

                var result = await _mantenimientoService.DeleteMantenimientoAsync(idMantenimiento, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Mantenimiento {idMantenimiento} eliminado exitosamente", "MttoViewModel", "DeleteMantenimientoAsync");
                    // Recargar la lista de mantenimientos
                    await LoadMantenimientosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar el mantenimiento.";
                    await _logger.LogWarningAsync($"No se pudo eliminar el mantenimiento {idMantenimiento}", "MttoViewModel", "DeleteMantenimientoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar mantenimiento: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar mantenimiento {idMantenimiento}", ex, "MttoViewModel", "DeleteMantenimientoAsync");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo mantenimiento
        /// </summary>
        public async Task<bool> CreateMantenimientoAsync(int idTipoMantenimiento, int idCliente, int idEquipo, double costo, string? nota = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Creando nuevo mantenimiento...", "MttoViewModel", "CreateMantenimientoAsync");

                var result = await _mantenimientoService.CreateMantenimientoAsync(idTipoMantenimiento, idCliente, idEquipo, costo, nota, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Mantenimiento creado exitosamente", "MttoViewModel", "CreateMantenimientoAsync");
                    // Recargar la lista de mantenimientos
                    await LoadMantenimientosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear el mantenimiento.";
                    await _logger.LogWarningAsync($"No se pudo crear el mantenimiento", "MttoViewModel", "CreateMantenimientoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al crear mantenimiento: {ex.Message}";
                await _logger.LogErrorAsync($"Error al crear mantenimiento", ex, "MttoViewModel", "CreateMantenimientoAsync");
                return false;
            }
        }
    }
}
