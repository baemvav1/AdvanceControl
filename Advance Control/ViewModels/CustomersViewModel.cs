using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
                await _logger.LogInfoAsync("Cargando clientes...", "CustomersViewModel", "LoadClientesAsync");

                var query = new ClienteQueryDto
                {
                    Search = SearchText,
                    Rfc = RfcFilter,
                    Curp = CurpFilter,
                    Notas = NotasFilter,
                    Prioridad = PrioridadFilter.HasValue ? (int?)PrioridadFilter.Value : null
                };

                var clientes = await _clienteService.GetClientesAsync(query, cancellationToken);

                Customers.Clear();
                foreach (var cliente in clientes)
                {
                    Customers.Add(cliente);
                }

                await _logger.LogInfoAsync($"Se cargaron {clientes.Count} clientes", "CustomersViewModel", "LoadClientesAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar clientes", ex, "CustomersViewModel", "LoadClientesAsync");
                // Podrías mostrar un mensaje de error al usuario aquí
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
            SearchText = null;
            RfcFilter = null;
            CurpFilter = null;
            NotasFilter = null;
            PrioridadFilter = null;
            await LoadClientesAsync(cancellationToken);
        }
    }
}
