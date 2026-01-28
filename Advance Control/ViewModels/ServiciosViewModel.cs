using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Servicios;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class ServiciosViewModel : ViewModelBase
    {
        private readonly IServicioService _servicioService;
        private readonly ILoggingService _logger;
        private ObservableCollection<ServicioDto> _servicios;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _conceptoFilter;
        private string? _descripcionFilter;
        private string? _costoFilter;

        public ServiciosViewModel(IServicioService servicioService, ILoggingService logger)
        {
            _servicioService = servicioService ?? throw new ArgumentNullException(nameof(servicioService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _servicios = new ObservableCollection<ServicioDto>();
        }

        public ObservableCollection<ServicioDto> Servicios
        {
            get => _servicios;
            set => SetProperty(ref _servicios, value);
        }

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

        public string? ConceptoFilter
        {
            get => _conceptoFilter;
            set => SetProperty(ref _conceptoFilter, value);
        }

        public string? DescripcionFilter
        {
            get => _descripcionFilter;
            set => SetProperty(ref _descripcionFilter, value);
        }

        public string? CostoFilter
        {
            get => _costoFilter;
            set => SetProperty(ref _costoFilter, value);
        }

        /// <summary>
        /// Carga los servicios desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadServiciosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null; // Limpiar errores anteriores
                await _logger.LogInformationAsync("Cargando servicios...", "ServiciosViewModel", "LoadServiciosAsync");

                // Parse costo filter if provided
                double? costoValue = null;
                if (!string.IsNullOrWhiteSpace(CostoFilter))
                {
                    if (double.TryParse(CostoFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedCosto))
                    {
                        costoValue = parsedCosto;
                    }
                }

                var query = new ServicioQueryDto
                {
                    Concepto = ConceptoFilter,
                    Descripcion = DescripcionFilter,
                    Costo = costoValue
                };

                var servicios = await _servicioService.GetServiciosAsync(query, cancellationToken);

                Servicios.Clear();
                foreach (var servicio in servicios)
                {
                    Servicios.Add(servicio);
                }

                await _logger.LogInformationAsync($"Se cargaron {servicios.Count} servicios exitosamente", "ServiciosViewModel", "LoadServiciosAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de servicios cancelada por el usuario", "ServiciosViewModel", "LoadServiciosAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar servicios", ex, "ServiciosViewModel", "LoadServiciosAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error inesperado al cargar servicios. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync("Error inesperado al cargar servicios", ex, "ServiciosViewModel", "LoadServiciosAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los servicios
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ConceptoFilter = null;
                DescripcionFilter = null;
                CostoFilter = null;
                ErrorMessage = null;
                await LoadServiciosAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar servicios.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "ServiciosViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina un servicio por su ID
        /// </summary>
        public async Task<bool> DeleteServicioAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando servicio {id}...", "ServiciosViewModel", "DeleteServicioAsync");

                var result = await _servicioService.DeleteServicioAsync(id, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Servicio {id} eliminado exitosamente", "ServiciosViewModel", "DeleteServicioAsync");
                    // Recargar la lista de servicios
                    await LoadServiciosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar el servicio.";
                    await _logger.LogWarningAsync($"No se pudo eliminar el servicio {id}", "ServiciosViewModel", "DeleteServicioAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al eliminar servicio. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync($"Error al eliminar servicio {id}", ex, "ServiciosViewModel", "DeleteServicioAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza un servicio existente
        /// </summary>
        public async Task<bool> UpdateServicioAsync(int id, ServicioQueryDto updateData, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando servicio {id}...", "ServiciosViewModel", "UpdateServicioAsync");

                var result = await _servicioService.UpdateServicioAsync(id, updateData, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Servicio {id} actualizado exitosamente", "ServiciosViewModel", "UpdateServicioAsync");
                    // Recargar la lista de servicios
                    await LoadServiciosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo actualizar el servicio.";
                    await _logger.LogWarningAsync($"No se pudo actualizar el servicio {id}", "ServiciosViewModel", "UpdateServicioAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al actualizar servicio. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync($"Error al actualizar servicio {id}", ex, "ServiciosViewModel", "UpdateServicioAsync");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo servicio
        /// </summary>
        public async Task<bool> CreateServicioAsync(string concepto, string descripcion, double costo, bool estatus = true, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Creando nuevo servicio...", "ServiciosViewModel", "CreateServicioAsync");

                var result = await _servicioService.CreateServicioAsync(concepto, descripcion, costo, estatus, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Servicio creado exitosamente", "ServiciosViewModel", "CreateServicioAsync");
                    // Recargar la lista de servicios
                    await LoadServiciosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear el servicio.";
                    await _logger.LogWarningAsync($"No se pudo crear el servicio", "ServiciosViewModel", "CreateServicioAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al crear servicio. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync($"Error al crear servicio", ex, "ServiciosViewModel", "CreateServicioAsync");
                return false;
            }
        }
    }
}
