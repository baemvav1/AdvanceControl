using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Operaciones;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel del dashboard de Acceso Cliente: permite buscar un cliente
    /// y muestra contadores agregados de sus operaciones.
    /// </summary>
    public class AccesoClienteViewModel : ViewModelBase
    {
        private readonly IClienteService _clienteService;
        private readonly IOperacionService _operacionService;
        private readonly ILoggingService _logger;

        private List<(int IdCliente, string Texto)> _todosLosClientes = new();
        private ObservableCollection<string> _sugerencias = new();

        private string? _textoBusqueda;
        private string? _clienteSeleccionadoTexto;
        private int _clienteSeleccionadoId;

        private bool _isLoading;
        private bool _hasCliente;
        private string? _errorMessage;

        private int _totalOperaciones;
        private int _totalFacturadas;
        private int _totalFinalizadas;
        private int _totalSinFinalizar;

        public AccesoClienteViewModel(
            IClienteService clienteService,
            IOperacionService operacionService,
            ILoggingService logger)
        {
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _operacionService = operacionService ?? throw new ArgumentNullException(nameof(operacionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ObservableCollection<string> Sugerencias
        {
            get => _sugerencias;
            set => SetProperty(ref _sugerencias, value);
        }

        public string? TextoBusqueda
        {
            get => _textoBusqueda;
            set => SetProperty(ref _textoBusqueda, value);
        }

        public string? ClienteSeleccionadoTexto
        {
            get => _clienteSeleccionadoTexto;
            private set
            {
                if (SetProperty(ref _clienteSeleccionadoTexto, value))
                    OnPropertyChanged(nameof(EncabezadoCliente));
            }
        }

        public int ClienteSeleccionadoId
        {
            get => _clienteSeleccionadoId;
            private set
            {
                if (SetProperty(ref _clienteSeleccionadoId, value))
                    OnPropertyChanged(nameof(EncabezadoCliente));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public bool HasCliente
        {
            get => _hasCliente;
            private set
            {
                if (SetProperty(ref _hasCliente, value))
                    OnPropertyChanged(nameof(NoHasCliente));
            }
        }

        public bool NoHasCliente => !_hasCliente;

        public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                    OnPropertyChanged(nameof(HasError));
            }
        }

        public int TotalOperaciones
        {
            get => _totalOperaciones;
            private set => SetProperty(ref _totalOperaciones, value);
        }

        public int TotalFacturadas
        {
            get => _totalFacturadas;
            private set => SetProperty(ref _totalFacturadas, value);
        }

        public int TotalFinalizadas
        {
            get => _totalFinalizadas;
            private set => SetProperty(ref _totalFinalizadas, value);
        }

        public int TotalSinFinalizar
        {
            get => _totalSinFinalizar;
            private set => SetProperty(ref _totalSinFinalizar, value);
        }

        public string EncabezadoCliente => HasCliente
            ? $"Cliente: {ClienteSeleccionadoTexto}"
            : "Selecciona un cliente para ver sus operaciones";

        /// <summary>
        /// Carga el catálogo de clientes para alimentar las sugerencias del buscador.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var clientes = await _clienteService.GetClientesAsync();
                _todosLosClientes = (clientes ?? new List<CustomerDto>())
                    .Select(c => (c.IdCliente, Texto: !string.IsNullOrWhiteSpace(c.RazonSocial) ? c.RazonSocial : c.NombreComercial ?? string.Empty))
                    .Where(c => c.IdCliente > 0 && !string.IsNullOrWhiteSpace(c.Texto))
                    .OrderBy(c => c.Texto, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "No se pudo cargar el catálogo de clientes.";
                await _logger.LogErrorAsync("Error inicializando AccesoClienteViewModel", ex, nameof(AccesoClienteViewModel), nameof(InitializeAsync));
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Refresca las sugerencias visibles del buscador según el texto ingresado.
        /// </summary>
        public void ActualizarSugerencias(string? texto)
        {
            _sugerencias.Clear();
            if (string.IsNullOrWhiteSpace(texto)) return;

            foreach (var c in _todosLosClientes
                .Where(c => c.Texto.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(15))
            {
                _sugerencias.Add(c.Texto);
            }
        }

        /// <summary>
        /// Selecciona un cliente por texto y carga sus contadores.
        /// </summary>
        public async Task SeleccionarClienteAsync(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                ResetCliente();
                return;
            }

            var match = _todosLosClientes.FirstOrDefault(c => c.Texto.Equals(texto, StringComparison.OrdinalIgnoreCase));
            if (match.IdCliente <= 0)
            {
                ResetCliente();
                ErrorMessage = "Cliente no encontrado en el catálogo.";
                return;
            }

            ClienteSeleccionadoId = match.IdCliente;
            ClienteSeleccionadoTexto = match.Texto;
            HasCliente = true;
            await CargarContadoresAsync();
        }

        /// <summary>
        /// Carga las operaciones del cliente seleccionado y calcula los 4 contadores en memoria.
        /// </summary>
        public async Task CargarContadoresAsync()
        {
            if (ClienteSeleccionadoId <= 0) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var query = new OperacionQueryDto { IdCliente = ClienteSeleccionadoId };
                var operaciones = await _operacionService.GetOperacionesAsync(query);

                operaciones ??= new List<OperacionDto>();
                TotalOperaciones = operaciones.Count;
                TotalFacturadas = operaciones.Count(o => o.IsFinalized);
                TotalFinalizadas = operaciones.Count(o => o.TFinalizado);
                TotalSinFinalizar = operaciones.Count(o => !o.TFinalizado);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al obtener las operaciones del cliente.";
                await _logger.LogErrorAsync("Error cargando contadores AccesoCliente", ex, nameof(AccesoClienteViewModel), nameof(CargarContadoresAsync));
            }
            finally
            {
                IsLoading = false;
            }
        }

        public AccesoClienteContext? BuildContext(AccesoClienteFiltro filtro)
        {
            if (ClienteSeleccionadoId <= 0) return null;
            return new AccesoClienteContext
            {
                IdCliente = ClienteSeleccionadoId,
                NombreCliente = ClienteSeleccionadoTexto ?? string.Empty,
                Filtro = filtro,
                BypassAcceso = true,
            };
        }

        private void ResetCliente()
        {
            ClienteSeleccionadoId = 0;
            ClienteSeleccionadoTexto = null;
            HasCliente = false;
            TotalOperaciones = 0;
            TotalFacturadas = 0;
            TotalFinalizadas = 0;
            TotalSinFinalizar = 0;
        }
    }
}
