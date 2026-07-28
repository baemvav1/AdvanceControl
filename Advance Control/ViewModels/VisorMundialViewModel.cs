using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Reportes;
using Advance_Control.Services.VisorMundial;

namespace Advance_Control.ViewModels
{
    public class VisorMundialViewModel : ViewModelBase
    {
        private readonly IGoogleMapsConfigService _googleMapsConfigService;
        private readonly Services.VisorMundial.IVisorMundialService _visorMundialService;
        private readonly IClienteService _clienteService;
        private readonly IEquipoService _equipoService;
        private readonly IContactoService _contactoService;
        private readonly IReporteFinancieroFacturacionService _reporteFinancieroService;

        private GoogleMapsConfigDto? _mapsConfig;
        private ObservableCollection<VisorMundialUbicacionDto> _ubicaciones;
        private ObservableCollection<CustomerDto> _clientes;
        private ObservableCollection<EquipoDto> _equipos;
        private ObservableCollection<ContactoDto> _tecnicos;
        private ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> _topClientesSaldo;
        private ObservableCollection<VisorMundialEquipoDto> _equiposUbicacionSeleccionada;
        private VisorMundialUbicacionDto? _ubicacionSeleccionada;
        private Dictionary<string, decimal> _saldoPorRfc = new(StringComparer.OrdinalIgnoreCase);
        private bool _isLoading;
        private bool _isLoadingDetalle;
        private string? _errorMessage;
        private bool _isMapInitialized;
        private bool _isPanelOpen = true;
        private string _modoColoreo = "operaciones";
        private decimal _totalFacturadoGlobal;
        private decimal _totalAbonadoGlobal;

        public VisorMundialViewModel(
            IGoogleMapsConfigService googleMapsConfigService,
            Services.VisorMundial.IVisorMundialService visorMundialService,
            IClienteService clienteService,
            IEquipoService equipoService,
            IContactoService contactoService,
            IReporteFinancieroFacturacionService reporteFinancieroService)
        {
            _googleMapsConfigService = googleMapsConfigService ?? throw new ArgumentNullException(nameof(googleMapsConfigService));
            _visorMundialService = visorMundialService ?? throw new ArgumentNullException(nameof(visorMundialService));
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _contactoService = contactoService ?? throw new ArgumentNullException(nameof(contactoService));
            _reporteFinancieroService = reporteFinancieroService ?? throw new ArgumentNullException(nameof(reporteFinancieroService));

            _ubicaciones = new ObservableCollection<VisorMundialUbicacionDto>();
            _clientes = new ObservableCollection<CustomerDto>();
            _equipos = new ObservableCollection<EquipoDto>();
            _tecnicos = new ObservableCollection<ContactoDto>();
            _topClientesSaldo = new ObservableCollection<ReporteFinancieroFacturacionCabeceraDto>();
            _equiposUbicacionSeleccionada = new ObservableCollection<VisorMundialEquipoDto>();
        }

        public GoogleMapsConfigDto? MapsConfig
        {
            get => _mapsConfig;
            set => SetProperty(ref _mapsConfig, value);
        }

        public ObservableCollection<VisorMundialUbicacionDto> Ubicaciones
        {
            get => _ubicaciones;
            set => SetProperty(ref _ubicaciones, value);
        }

        public ObservableCollection<CustomerDto> Clientes
        {
            get => _clientes;
            set => SetProperty(ref _clientes, value);
        }

        public ObservableCollection<EquipoDto> Equipos
        {
            get => _equipos;
            set => SetProperty(ref _equipos, value);
        }

        public ObservableCollection<ContactoDto> Tecnicos
        {
            get => _tecnicos;
            set => SetProperty(ref _tecnicos, value);
        }

        public ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> TopClientesSaldo
        {
            get => _topClientesSaldo;
            set => SetProperty(ref _topClientesSaldo, value);
        }

        public ObservableCollection<VisorMundialEquipoDto> EquiposUbicacionSeleccionada
        {
            get => _equiposUbicacionSeleccionada;
            set => SetProperty(ref _equiposUbicacionSeleccionada, value);
        }

        public VisorMundialUbicacionDto? UbicacionSeleccionada
        {
            get => _ubicacionSeleccionada;
            set
            {
                if (SetProperty(ref _ubicacionSeleccionada, value))
                {
                    OnPropertyChanged(nameof(HasUbicacionSeleccionada));
                    OnPropertyChanged(nameof(NoHayUbicacionSeleccionada));
                }
            }
        }

        public bool HasUbicacionSeleccionada => UbicacionSeleccionada != null;
        public bool NoHayUbicacionSeleccionada => UbicacionSeleccionada == null;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsLoadingDetalle
        {
            get => _isLoadingDetalle;
            set => SetProperty(ref _isLoadingDetalle, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsMapInitialized
        {
            get => _isMapInitialized;
            set => SetProperty(ref _isMapInitialized, value);
        }

        public bool IsPanelOpen
        {
            get => _isPanelOpen;
            set => SetProperty(ref _isPanelOpen, value);
        }

        /// <summary>"operaciones" o "cobranza" — controla cómo se colorean los pines.</summary>
        public string ModoColoreo
        {
            get => _modoColoreo;
            set => SetProperty(ref _modoColoreo, value);
        }

        public decimal TotalFacturadoGlobal
        {
            get => _totalFacturadoGlobal;
            set => SetProperty(ref _totalFacturadoGlobal, value);
        }

        public decimal TotalAbonadoGlobal
        {
            get => _totalAbonadoGlobal;
            set => SetProperty(ref _totalAbonadoGlobal, value);
        }

        public decimal TotalPendienteGlobal => TotalFacturadoGlobal - TotalAbonadoGlobal;

        public string TotalFacturadoGlobalTexto => TotalFacturadoGlobal.ToString("C2", new CultureInfo("es-MX"));
        public string TotalAbonadoGlobalTexto => TotalAbonadoGlobal.ToString("C2", new CultureInfo("es-MX"));
        public string TotalPendienteGlobalTexto => TotalPendienteGlobal.ToString("C2", new CultureInfo("es-MX"));

        public string ResumenUbicaciones => $"{Ubicaciones.Count} ubicación(es) en el mapa";

        public async Task InitializeAsync()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                MapsConfig = await _googleMapsConfigService.GetConfigAsync();

                var ubicacionesTask = _visorMundialService.ObtenerUbicacionesAsync();
                var clientesTask = _clienteService.GetClientesAsync();
                var equiposTask = _equipoService.GetEquiposAsync();
                var tecnicosTask = _contactoService.GetContactosAsync();
                var reporteTask = _reporteFinancieroService.ObtenerReporteAsync(null, null, null, null, null);

                await Task.WhenAll(ubicacionesTask, clientesTask, equiposTask, tecnicosTask, reporteTask);

                Ubicaciones = new ObservableCollection<VisorMundialUbicacionDto>(ubicacionesTask.Result);
                Clientes = new ObservableCollection<CustomerDto>(clientesTask.Result);
                Equipos = new ObservableCollection<EquipoDto>(equiposTask.Result);
                Tecnicos = new ObservableCollection<ContactoDto>(tecnicosTask.Result);

                AplicarReporteCobranza(reporteTask.Result);

                OnPropertyChanged(nameof(ResumenUbicaciones));
                IsMapInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el Visor Mundial: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AplicarReporteCobranza(ReporteFinancieroFacturacionResponseDto? reporte)
        {
            var cabeceras = reporte?.Cabeceras ?? new List<ReporteFinancieroFacturacionCabeceraDto>();

            _saldoPorRfc = cabeceras
                .Where(c => !string.IsNullOrWhiteSpace(c.ReceptorRfc))
                .GroupBy(c => c.ReceptorRfc!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(c => c.TotalFacturado - c.TotalAbonadoMovimientos),
                    StringComparer.OrdinalIgnoreCase);

            TotalFacturadoGlobal = cabeceras.Sum(c => c.TotalFacturado);
            TotalAbonadoGlobal = cabeceras.Sum(c => c.TotalAbonadoMovimientos);

            var topClientes = cabeceras
                .Where(c => (c.TotalFacturado - c.TotalAbonadoMovimientos) > 0)
                .OrderByDescending(c => c.TotalFacturado - c.TotalAbonadoMovimientos)
                .Take(15)
                .ToList();

            TopClientesSaldo = new ObservableCollection<ReporteFinancieroFacturacionCabeceraDto>(topClientes);

            OnPropertyChanged(nameof(TotalPendienteGlobal));
            OnPropertyChanged(nameof(TotalFacturadoGlobalTexto));
            OnPropertyChanged(nameof(TotalAbonadoGlobalTexto));
            OnPropertyChanged(nameof(TotalPendienteGlobalTexto));
        }

        /// <summary>
        /// Color hexadecimal para una ubicación según el modo indicado ("operaciones" o "cobranza").
        /// </summary>
        public string ObtenerColorUbicacion(VisorMundialUbicacionDto ubicacion, string modo)
        {
            if (string.Equals(modo, "cobranza", StringComparison.OrdinalIgnoreCase))
            {
                var rfcs = ubicacion.ClientesRfcList;
                if (rfcs.Count == 0)
                {
                    return "#9AA0A6"; // gris: sin cliente asociado
                }

                var tieneSaldoPendiente = rfcs.Any(rfc => _saldoPorRfc.TryGetValue(rfc, out var saldo) && saldo > 0);
                return tieneSaldoPendiente ? "#EA4335" : "#34A853";
            }

            return ubicacion.TieneOperacionAbierta ? "#EA4335" : "#34A853";
        }

        public async Task CargarEquiposDeUbicacionAsync(VisorMundialUbicacionDto ubicacion)
        {
            if (ubicacion == null)
            {
                return;
            }

            try
            {
                IsLoadingDetalle = true;
                UbicacionSeleccionada = ubicacion;
                IsPanelOpen = true;

                var equipos = await _visorMundialService.ObtenerEquiposPorUbicacionAsync(ubicacion.IdUbicacion);
                EquiposUbicacionSeleccionada = new ObservableCollection<VisorMundialEquipoDto>(equipos);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar los equipos de la ubicación: {ex.Message}";
            }
            finally
            {
                IsLoadingDetalle = false;
            }
        }
    }
}
