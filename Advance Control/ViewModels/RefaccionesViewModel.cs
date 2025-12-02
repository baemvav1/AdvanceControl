using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Refacciones;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class RefaccionesViewModel : ViewModelBase
    {
        private readonly IRefaccionService _refaccionService;
        private readonly ILoggingService _logger;
        private ObservableCollection<RefaccionDto> _refacciones;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _marcaFilter;
        private string? _serieFilter;
        private string? _descripcionFilter;

        public RefaccionesViewModel(IRefaccionService refaccionService, ILoggingService logger)
        {
            _refaccionService = refaccionService ?? throw new ArgumentNullException(nameof(refaccionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _refacciones = new ObservableCollection<RefaccionDto>();
        }

        public ObservableCollection<RefaccionDto> Refacciones
        {
            get => _refacciones;
            set => SetProperty(ref _refacciones, value);
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

        public string? MarcaFilter
        {
            get => _marcaFilter;
            set => SetProperty(ref _marcaFilter, value);
        }

        public string? SerieFilter
        {
            get => _serieFilter;
            set => SetProperty(ref _serieFilter, value);
        }

        public string? DescripcionFilter
        {
            get => _descripcionFilter;
            set => SetProperty(ref _descripcionFilter, value);
        }

        /// <summary>
        /// Carga las refacciones desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadRefaccionesAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null; // Limpiar errores anteriores
                await _logger.LogInformationAsync("Cargando refacciones...", "RefaccionesViewModel", "LoadRefaccionesAsync");

                var query = new RefaccionQueryDto
                {
                    Marca = MarcaFilter,
                    Serie = SerieFilter,
                    Descripcion = DescripcionFilter
                };

                var refacciones = await _refaccionService.GetRefaccionesAsync(query, cancellationToken);

                Refacciones.Clear();
                foreach (var refaccion in refacciones)
                {
                    Refacciones.Add(refaccion);
                }

                await _logger.LogInformationAsync($"Se cargaron {refacciones.Count} refacciones exitosamente", "RefaccionesViewModel", "LoadRefaccionesAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de refacciones cancelada por el usuario", "RefaccionesViewModel", "LoadRefaccionesAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar refacciones", ex, "RefaccionesViewModel", "LoadRefaccionesAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar refacciones: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar refacciones", ex, "RefaccionesViewModel", "LoadRefaccionesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todas las refacciones
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                MarcaFilter = null;
                SerieFilter = null;
                DescripcionFilter = null;
                ErrorMessage = null;
                await LoadRefaccionesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar refacciones.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "RefaccionesViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina una refacción por su ID
        /// </summary>
        public async Task<bool> DeleteRefaccionAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando refacción {id}...", "RefaccionesViewModel", "DeleteRefaccionAsync");

                var result = await _refaccionService.DeleteRefaccionAsync(id, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Refacción {id} eliminada exitosamente", "RefaccionesViewModel", "DeleteRefaccionAsync");
                    // Recargar la lista de refacciones
                    await LoadRefaccionesAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar la refacción.";
                    await _logger.LogWarningAsync($"No se pudo eliminar la refacción {id}", "RefaccionesViewModel", "DeleteRefaccionAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar refacción: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar refacción {id}", ex, "RefaccionesViewModel", "DeleteRefaccionAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza una refacción existente
        /// </summary>
        public async Task<bool> UpdateRefaccionAsync(int id, RefaccionQueryDto updateData, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando refacción {id}...", "RefaccionesViewModel", "UpdateRefaccionAsync");

                var result = await _refaccionService.UpdateRefaccionAsync(id, updateData, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Refacción {id} actualizada exitosamente", "RefaccionesViewModel", "UpdateRefaccionAsync");
                    // Recargar la lista de refacciones
                    await LoadRefaccionesAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo actualizar la refacción.";
                    await _logger.LogWarningAsync($"No se pudo actualizar la refacción {id}", "RefaccionesViewModel", "UpdateRefaccionAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar refacción: {ex.Message}";
                await _logger.LogErrorAsync($"Error al actualizar refacción {id}", ex, "RefaccionesViewModel", "UpdateRefaccionAsync");
                return false;
            }
        }

        /// <summary>
        /// Crea una nueva refacción
        /// </summary>
        public async Task<bool> CreateRefaccionAsync(string? marca, string? serie, double? costo, string? descripcion, bool estatus = true, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Creando nueva refacción...", "RefaccionesViewModel", "CreateRefaccionAsync");

                var result = await _refaccionService.CreateRefaccionAsync(marca, serie, costo, descripcion, estatus, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Refacción creada exitosamente", "RefaccionesViewModel", "CreateRefaccionAsync");
                    // Recargar la lista de refacciones
                    await LoadRefaccionesAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear la refacción.";
                    await _logger.LogWarningAsync($"No se pudo crear la refacción", "RefaccionesViewModel", "CreateRefaccionAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al crear refacción: {ex.Message}";
                await _logger.LogErrorAsync($"Error al crear refacción", ex, "RefaccionesViewModel", "CreateRefaccionAsync");
                return false;
            }
        }
    }
}
