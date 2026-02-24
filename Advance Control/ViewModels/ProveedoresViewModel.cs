using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Proveedores;

namespace Advance_Control.ViewModels
{
    public class ProveedoresViewModel : ViewModelBase
    {
        private readonly IProveedorService _proveedorService;
        private readonly ILoggingService _logger;
        private ObservableCollection<ProveedorDto> _proveedores;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _rfcFilter;
        private string? _razonSocialFilter;
        private string? _nombreComercialFilter;
        private string? _notaFilter;

        public ProveedoresViewModel(IProveedorService proveedorService, ILoggingService logger)
        {
            _proveedorService = proveedorService ?? throw new ArgumentNullException(nameof(proveedorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _proveedores = new ObservableCollection<ProveedorDto>();
        }

        public ObservableCollection<ProveedorDto> Proveedores
        {
            get => _proveedores;
            set => SetProperty(ref _proveedores, value);
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

        public string? RfcFilter
        {
            get => _rfcFilter;
            set => SetProperty(ref _rfcFilter, value);
        }

        public string? RazonSocialFilter
        {
            get => _razonSocialFilter;
            set => SetProperty(ref _razonSocialFilter, value);
        }

        public string? NombreComercialFilter
        {
            get => _nombreComercialFilter;
            set => SetProperty(ref _nombreComercialFilter, value);
        }

        public string? NotaFilter
        {
            get => _notaFilter;
            set => SetProperty(ref _notaFilter, value);
        }

        /// <summary>
        /// Carga los proveedores desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadProveedoresAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null; // Limpiar errores anteriores
                await _logger.LogInformationAsync("Cargando proveedores...", "ProveedoresViewModel", "LoadProveedoresAsync");

                var query = new ProveedorQueryDto
                {
                    Rfc = RfcFilter,
                    RazonSocial = RazonSocialFilter,
                    NombreComercial = NombreComercialFilter,
                    Nota = NotaFilter
                };

                var proveedores = await _proveedorService.GetProveedoresAsync(query, cancellationToken);

                if (proveedores == null)
                {
                    ErrorMessage = "Error: El servicio no devolvió datos válidos.";
                    await _logger.LogWarningAsync("GetProveedoresAsync devolvió null", "ProveedoresViewModel", "LoadProveedoresAsync");
                    return;
                }

                Proveedores.Clear();
                foreach (var proveedor in proveedores)
                {
                    Proveedores.Add(proveedor);
                }

                await _logger.LogInformationAsync($"Se cargaron {proveedores.Count} proveedores exitosamente", "ProveedoresViewModel", "LoadProveedoresAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de proveedores cancelada por el usuario", "ProveedoresViewModel", "LoadProveedoresAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar proveedores", ex, "ProveedoresViewModel", "LoadProveedoresAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar proveedores: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar proveedores", ex, "ProveedoresViewModel", "LoadProveedoresAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los proveedores
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                RfcFilter = null;
                RazonSocialFilter = null;
                NombreComercialFilter = null;
                NotaFilter = null;
                ErrorMessage = null;
                await LoadProveedoresAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar proveedores.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "ProveedoresViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Crea un nuevo proveedor
        /// </summary>
        public async Task<bool> CreateProveedorAsync(string rfc, string? razonSocial, string? nombreComercial, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Creando nuevo proveedor...", "ProveedoresViewModel", "CreateProveedorAsync");

                var result = await _proveedorService.CreateProveedorAsync(rfc, razonSocial, nombreComercial, nota, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync("Proveedor creado exitosamente", "ProveedoresViewModel", "CreateProveedorAsync");
                    await LoadProveedoresAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear el proveedor.";
                    await _logger.LogWarningAsync("No se pudo crear el proveedor", "ProveedoresViewModel", "CreateProveedorAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al crear proveedor. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync("Error al crear proveedor", ex, "ProveedoresViewModel", "CreateProveedorAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza un proveedor existente
        /// </summary>
        public async Task<bool> UpdateProveedorAsync(int id, string? rfc, string? razonSocial, string? nombreComercial, string? nota, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando proveedor {id}...", "ProveedoresViewModel", "UpdateProveedorAsync");

                var result = await _proveedorService.UpdateProveedorAsync(id, rfc, razonSocial, nombreComercial, nota, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Proveedor {id} actualizado exitosamente", "ProveedoresViewModel", "UpdateProveedorAsync");
                    await LoadProveedoresAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo actualizar el proveedor.";
                    await _logger.LogWarningAsync($"No se pudo actualizar el proveedor {id}", "ProveedoresViewModel", "UpdateProveedorAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al actualizar proveedor. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync($"Error al actualizar proveedor {id}", ex, "ProveedoresViewModel", "UpdateProveedorAsync");
                return false;
            }
        }

        /// <summary>
        /// Elimina un proveedor por su ID
        /// </summary>
        public async Task<bool> DeleteProveedorAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando proveedor {id}...", "ProveedoresViewModel", "DeleteProveedorAsync");

                var result = await _proveedorService.DeleteProveedorAsync(id, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Proveedor {id} eliminado exitosamente", "ProveedoresViewModel", "DeleteProveedorAsync");
                    await LoadProveedoresAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar el proveedor.";
                    await _logger.LogWarningAsync($"No se pudo eliminar el proveedor {id}", "ProveedoresViewModel", "DeleteProveedorAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al eliminar proveedor. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync($"Error al eliminar proveedor {id}", ex, "ProveedoresViewModel", "DeleteProveedorAsync");
                return false;
            }
        }
    }
}
