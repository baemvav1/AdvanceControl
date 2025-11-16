using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class CustomersViewModel : ViewModelBase
    {
        private readonly IClienteService _clienteService;
        private readonly ILoggingService _logger;
        private ObservableCollection<CustomerDto> _customers;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _searchText;
        private string? _rfcFilter;
        private string? _curpFilter;
        private string? _notasFilter;
        private double? _prioridadFilter;

        public CustomersViewModel(IClienteService clienteService, ILoggingService logger)
        {
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _customers = new ObservableCollection<CustomerDto>();
        }

        public ObservableCollection<CustomerDto> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
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

        public string? SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string? RfcFilter
        {
            get => _rfcFilter;
            set => SetProperty(ref _rfcFilter, value);
        }

        public string? CurpFilter
        {
            get => _curpFilter;
            set => SetProperty(ref _curpFilter, value);
        }

        public string? NotasFilter
        {
            get => _notasFilter;
            set => SetProperty(ref _notasFilter, value);
        }

        public double? PrioridadFilter
        {
            get => _prioridadFilter;
            set => SetProperty(ref _prioridadFilter, value);
        }

        /// <summary>
        /// Carga los clientes desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadClientesAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null; // Limpiar errores anteriores
                await _logger.LogInformationAsync("Cargando clientes...", "CustomersViewModel", "LoadClientesAsync");

                var query = new ClienteQueryDto
                {
                    Search = SearchText,
                    Rfc = RfcFilter,
                    Curp = CurpFilter,
                    Notas = NotasFilter,
                    Prioridad = PrioridadFilter.HasValue ? (int?)PrioridadFilter.Value : null
                };

                var clientes = await _clienteService.GetClientesAsync(query, cancellationToken);

                if (clientes == null)
                {
                    ErrorMessage = "Error: El servicio no devolvió datos válidos.";
                    await _logger.LogWarningAsync("GetClientesAsync devolvió null", "CustomersViewModel", "LoadClientesAsync");
                    return;
                }

                Customers.Clear();
                foreach (var cliente in clientes)
                {
                    Customers.Add(cliente);
                }

                await _logger.LogInformationAsync($"Se cargaron {clientes.Count} clientes exitosamente", "CustomersViewModel", "LoadClientesAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de clientes cancelada por el usuario", "CustomersViewModel", "LoadClientesAsync");
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorMessage = "Error de autenticación: " + ex.Message;
                await _logger.LogWarningAsync("Error de autorización al cargar clientes", "CustomersViewModel", "LoadClientesAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar clientes", ex, "CustomersViewModel", "LoadClientesAsync");
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                await _logger.LogErrorAsync("Error de operación al cargar clientes", ex, "CustomersViewModel", "LoadClientesAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar clientes: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar clientes", ex, "CustomersViewModel", "LoadClientesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los clientes
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                SearchText = null;
                RfcFilter = null;
                CurpFilter = null;
                NotasFilter = null;
                PrioridadFilter = null;
                ErrorMessage = null;
                await LoadClientesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar clientes.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "CustomersViewModel", "ClearFiltersAsync");
            }
        }
    }
}
