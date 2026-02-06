using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Quotes;
using Advance_Control.Services.Entidades;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Operaciones.
    /// Gestiona la lógica de presentación para operaciones del sistema.
    /// </summary>
    public class OperacionesViewModel : ViewModelBase
    {
        /// <summary>
        /// IVA rate (16%) for Mexican tax calculation
        /// </summary>
        private const double IVA_RATE = 0.16;
        
        private readonly IOperacionService _operacionService;
        private readonly IEquipoService _equipoService;
        private readonly IUbicacionService _ubicacionService;
        private readonly ILoggingService _logger;
        private readonly IQuoteService _quoteService;
        private readonly IEntidadService _entidadService;
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

        public OperacionesViewModel(IOperacionService operacionService, IEquipoService equipoService, IUbicacionService ubicacionService, ILoggingService logger, IQuoteService quoteService, IEntidadService entidadService)
        {
            _operacionService = operacionService ?? throw new ArgumentNullException(nameof(operacionService));
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _ubicacionService = ubicacionService ?? throw new ArgumentNullException(nameof(ubicacionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _entidadService = entidadService ?? throw new ArgumentNullException(nameof(entidadService));
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

        /// <summary>
        /// Genera una cotización PDF para la operación especificada con sus cargos
        /// </summary>
        public async Task<string?> GenerateQuoteAsync(OperacionDto operacion, CancellationToken cancellationToken = default)
        {
            try
            {
                if (operacion == null)
                {
                    ErrorMessage = "No se puede generar cotización: operación no válida.";
                    return null;
                }

                if (operacion.Cargos == null || operacion.Cargos.Count == 0)
                {
                    ErrorMessage = "No se puede generar cotización: no hay cargos asociados a esta operación.";
                    return null;
                }

                await _logger.LogInformationAsync($"Generando cotización para operación {operacion.IdOperacion}...", "OperacionesViewModel", "GenerateQuoteAsync");

                // Get active entity to use company name
                string? nombreEmpresa = null;
                try
                {
                    var entidadActiva = await _entidadService.GetActiveEntidadAsync(cancellationToken);
                    nombreEmpresa = entidadActiva?.NombreComercial;
                    if (!string.IsNullOrWhiteSpace(nombreEmpresa))
                    {
                        await _logger.LogInformationAsync($"Usando nombre comercial de entidad activa: {nombreEmpresa}", "OperacionesViewModel", "GenerateQuoteAsync");
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if we can't get the entity
                    await _logger.LogWarningAsync($"No se pudo obtener la entidad activa: {ex.Message}", "OperacionesViewModel", "GenerateQuoteAsync");
                }

                // Get equipment location if available
                string? ubicacionNombre = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(operacion.Identificador))
                    {
                        // Search for equipment by identifier
                        var equipos = await _equipoService.GetEquiposAsync(new EquipoQueryDto { Identificador = operacion.Identificador }, cancellationToken);
                        var equipo = equipos?.FirstOrDefault();
                        
                        if (equipo?.IdUbicacion.HasValue == true && equipo.IdUbicacion.Value > 0)
                        {
                            var ubicacion = await _ubicacionService.GetUbicacionByIdAsync(equipo.IdUbicacion.Value, cancellationToken);
                            ubicacionNombre = ubicacion?.Nombre;
                            await _logger.LogInformationAsync($"Ubicación encontrada para equipo {operacion.Identificador}: {ubicacionNombre}", "OperacionesViewModel", "GenerateQuoteAsync");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail if we can't get the location
                    await _logger.LogWarningAsync($"No se pudo obtener la ubicación del equipo: {ex.Message}", "OperacionesViewModel", "GenerateQuoteAsync");
                }

                var filePath = await _quoteService.GenerateQuotePdfAsync(operacion, operacion.Cargos, ubicacionNombre, nombreEmpresa);

                // Calculate total with IVA and update operation monto
                if (operacion.IdOperacion.HasValue)
                {
                    try
                    {
                        var subtotal = operacion.Cargos.Sum(c => c.Monto ?? 0);
                        var iva = subtotal * IVA_RATE;
                        var totalConIva = (decimal)(subtotal + iva);

                        var updateResult = await _operacionService.UpdateOperacionMontoAsync(operacion.IdOperacion.Value, totalConIva, cancellationToken);
                        if (updateResult)
                        {
                            // Update local model
                            operacion.Monto = totalConIva;
                            await _logger.LogInformationAsync($"Monto de operación {operacion.IdOperacion} actualizado a {totalConIva:N2}", "OperacionesViewModel", "GenerateQuoteAsync");
                        }
                        else
                        {
                            await _logger.LogWarningAsync($"No se pudo actualizar el monto de la operación {operacion.IdOperacion}", "OperacionesViewModel", "GenerateQuoteAsync");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the quote generation if update fails
                        await _logger.LogWarningAsync($"Error al actualizar monto de operación: {ex.Message}", "OperacionesViewModel", "GenerateQuoteAsync");
                    }
                }

                await _logger.LogInformationAsync($"Cotización generada exitosamente: {filePath}", "OperacionesViewModel", "GenerateQuoteAsync");

                return filePath;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar cotización: {ex.Message}";
                await _logger.LogErrorAsync($"Error al generar cotización para operación {operacion?.IdOperacion}", ex, "OperacionesViewModel", "GenerateQuoteAsync");
                return null;
            }
        }

        /// <summary>
        /// Genera un reporte PDF para la operación especificada con sus cargos y fotografías
        /// </summary>
        public async Task<string?> GenerateReporteAsync(OperacionDto operacion, CancellationToken cancellationToken = default)
        {
            try
            {
                if (operacion == null)
                {
                    ErrorMessage = "No se puede generar reporte: operación no válida.";
                    return null;
                }

                if (operacion.Cargos == null || operacion.Cargos.Count == 0)
                {
                    ErrorMessage = "No se puede generar reporte: no hay cargos asociados a esta operación.";
                    return null;
                }

                await _logger.LogInformationAsync($"Generando reporte para operación {operacion.IdOperacion}...", "OperacionesViewModel", "GenerateReporteAsync");

                var filePath = await _quoteService.GenerateReportePdfAsync(operacion, operacion.Cargos);

                await _logger.LogInformationAsync($"Reporte generado exitosamente: {filePath}", "OperacionesViewModel", "GenerateReporteAsync");

                return filePath;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar reporte: {ex.Message}";
                await _logger.LogErrorAsync($"Error al generar reporte para operación {operacion?.IdOperacion}", ex, "OperacionesViewModel", "GenerateReporteAsync");
                return null;
            }
        }
    }
}
