using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Operaciones.
    /// Gestiona la lógica de presentación para operaciones del sistema.
    /// </summary>
    public class OperacionesViewModel : ViewModelBase
    {
        private readonly IOperacionService _operacionService;
        private readonly ILoggingService _logger;
        private ObservableCollection<OperacionDto> _operaciones;
        private bool _isLoading;
        private string? _errorMessage;
        private int _idTipoFilter;
        private int _idClienteFilter;
        private int _idEquipoFilter;
        private int _idAtiendeFilter;
        private string? _notaFilter;
        private string? _selectedClienteText;
        private string? _selectedEquipoText;

        public OperacionesViewModel(IOperacionService operacionService, ILoggingService logger)
        {
            _operacionService = operacionService ?? throw new ArgumentNullException(nameof(operacionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _operaciones = new ObservableCollection<OperacionDto>();
        }

        public ObservableCollection<OperacionDto> Operaciones
        {
            get => _operaciones;
            set => SetProperty(ref _operaciones, value);
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

        public int IdTipoFilter
        {
            get => _idTipoFilter;
            set => SetProperty(ref _idTipoFilter, value);
        }

        public int IdClienteFilter
        {
            get => _idClienteFilter;
            set => SetProperty(ref _idClienteFilter, value);
        }

        public int IdEquipoFilter
        {
            get => _idEquipoFilter;
            set => SetProperty(ref _idEquipoFilter, value);
        }

        public int IdAtiendeFilter
        {
            get => _idAtiendeFilter;
            set => SetProperty(ref _idAtiendeFilter, value);
        }

        public string? NotaFilter
        {
            get => _notaFilter;
            set => SetProperty(ref _notaFilter, value);
        }

        /// <summary>
        /// Texto que muestra el cliente seleccionado
        /// </summary>
        public string? SelectedClienteText
        {
            get => _selectedClienteText;
            set => SetProperty(ref _selectedClienteText, value);
        }

        /// <summary>
        /// Texto que muestra el equipo seleccionado
        /// </summary>
        public string? SelectedEquipoText
        {
            get => _selectedEquipoText;
            set => SetProperty(ref _selectedEquipoText, value);
        }

        /// <summary>
        /// Inicializa los datos de la vista
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Vista de Operaciones inicializada", "OperacionesViewModel", "InitializeAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al inicializar la vista de Operaciones.";
                await _logger.LogErrorAsync("Error al inicializar OperacionesViewModel", ex, "OperacionesViewModel", "InitializeAsync");
            }
        }

        /// <summary>
        /// Carga las operaciones desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadOperacionesAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await _logger.LogInformationAsync("Cargando operaciones...", "OperacionesViewModel", "LoadOperacionesAsync");

                var query = new OperacionQueryDto
                {
                    IdTipo = IdTipoFilter,
                    IdCliente = IdClienteFilter,
                    IdEquipo = IdEquipoFilter,
                    IdAtiende = IdAtiendeFilter,
                    Nota = NotaFilter
                };

                var operaciones = await _operacionService.GetOperacionesAsync(query, cancellationToken);

                Operaciones.Clear();
                foreach (var operacion in operaciones)
                {
                    Operaciones.Add(operacion);
                }

                await _logger.LogInformationAsync($"Se cargaron {operaciones.Count} operaciones exitosamente", "OperacionesViewModel", "LoadOperacionesAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de operaciones cancelada por el usuario", "OperacionesViewModel", "LoadOperacionesAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar operaciones", ex, "OperacionesViewModel", "LoadOperacionesAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar operaciones: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar operaciones", ex, "OperacionesViewModel", "LoadOperacionesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todas las operaciones
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IdTipoFilter = 0;
                IdClienteFilter = 0;
                IdEquipoFilter = 0;
                IdAtiendeFilter = 0;
                NotaFilter = null;
                SelectedClienteText = null;
                SelectedEquipoText = null;
                ErrorMessage = null;
                await LoadOperacionesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar operaciones.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "OperacionesViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina una operación por su ID
        /// </summary>
        public async Task<bool> DeleteOperacionAsync(int idOperacion, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando operación {idOperacion}...", "OperacionesViewModel", "DeleteOperacionAsync");

                var result = await _operacionService.DeleteOperacionAsync(idOperacion, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Operación {idOperacion} eliminada exitosamente", "OperacionesViewModel", "DeleteOperacionAsync");
                    // Recargar la lista de operaciones
                    await LoadOperacionesAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar la operación.";
                    await _logger.LogWarningAsync($"No se pudo eliminar la operación {idOperacion}", "OperacionesViewModel", "DeleteOperacionAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar operación: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar operación {idOperacion}", ex, "OperacionesViewModel", "DeleteOperacionAsync");
                return false;
            }
        }
    }
}
