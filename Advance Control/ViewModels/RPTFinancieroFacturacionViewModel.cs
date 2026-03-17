using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Reportes;

namespace Advance_Control.ViewModels
{
    public class RPTFinancieroFacturacionViewModel : ViewModelBase
    {
        private readonly IReporteFinancieroFacturacionService _reporteService;
        private readonly List<ReporteFinancieroFacturacionCabeceraDto> _cabecerasBase = new();
        private readonly List<ReporteFinancieroFacturacionDetalleDto> _detallesBase = new();
        private ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> _cabeceras = new();
        private ObservableCollection<ReporteFinancieroFacturacionDetalleDto> _detallesVisibles = new();
        private ReporteFinancieroFacturacionCabeceraDto? _cabeceraSeleccionada;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;
        private string? _receptorRfcFiltro;
        private string? _referenciaFiltro;
        private DateTimeOffset? _fechaInicioFiltro;
        private DateTimeOffset? _fechaFinFiltro;
        private bool _soloFiniquitadas;

        public RPTFinancieroFacturacionViewModel(IReporteFinancieroFacturacionService reporteService)
        {
            _reporteService = reporteService ?? throw new ArgumentNullException(nameof(reporteService));
        }

        public ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> Cabeceras
        {
            get => _cabeceras;
            private set
            {
                if (SetProperty(ref _cabeceras, value))
                {
                    NotificarResumenes();
                }
            }
        }

        public ObservableCollection<ReporteFinancieroFacturacionDetalleDto> DetallesVisibles
        {
            get => _detallesVisibles;
            private set
            {
                if (SetProperty(ref _detallesVisibles, value))
                {
                    NotificarResumenes();
                }
            }
        }

        public ReporteFinancieroFacturacionCabeceraDto? CabeceraSeleccionada
        {
            get => _cabeceraSeleccionada;
            private set
            {
                if (SetProperty(ref _cabeceraSeleccionada, value))
                {
                    OnPropertyChanged(nameof(TieneCabeceraSeleccionada));
                    OnPropertyChanged(nameof(CabeceraActivaTexto));
                    OnPropertyChanged(nameof(ResumenDetalleTexto));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string? SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        public string? ReceptorRfcFiltro
        {
            get => _receptorRfcFiltro;
            set => SetProperty(ref _receptorRfcFiltro, value);
        }

        public string? ReferenciaFiltro
        {
            get => _referenciaFiltro;
            set => SetProperty(ref _referenciaFiltro, value);
        }

        public DateTimeOffset? FechaInicioFiltro
        {
            get => _fechaInicioFiltro;
            set => SetProperty(ref _fechaInicioFiltro, value);
        }

        public DateTimeOffset? FechaFinFiltro
        {
            get => _fechaFinFiltro;
            set => SetProperty(ref _fechaFinFiltro, value);
        }

        public bool SoloFiniquitadas
        {
            get => _soloFiniquitadas;
            set => SetProperty(ref _soloFiniquitadas, value);
        }

        public bool TieneCabeceraSeleccionada => CabeceraSeleccionada != null;
        public string TotalClientesTexto => $"{Cabeceras.Count} RFC(s)";
        public string TotalFacturadoTexto => Cabeceras.Sum(item => item.TotalFacturado).ToString("C2", new CultureInfo("es-MX"));
        public string TotalAbonadoTexto => DetallesVisibles.Sum(item => item.Abono ?? 0m).ToString("C2", new CultureInfo("es-MX"));
        public string ResumenCabecerasTexto => $"{Cabeceras.Sum(item => item.NumeroFacturas)} factura(s) en {Cabeceras.Count} cliente(s)";
        public string CabeceraActivaTexto => CabeceraSeleccionada == null
            ? "Mostrando todas las facturas del reporte."
            : $"Cliente seleccionado: {CabeceraSeleccionada.ReceptorNombreTexto} ({CabeceraSeleccionada.ReceptorRfcTexto})";
        public string ResumenDetalleTexto => $"{DetallesVisibles.Count} detalle(s) visibles";

        public async Task CargarReporteAsync()
        {
            if (FechaInicioFiltro.HasValue && FechaFinFiltro.HasValue && FechaFinFiltro.Value.Date < FechaInicioFiltro.Value.Date)
            {
                ErrorMessage = "La fecha fin no puede ser menor que la fecha inicio.";
                SuccessMessage = null;
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _reporteService.ObtenerReporteAsync(
                    string.IsNullOrWhiteSpace(ReceptorRfcFiltro) ? null : ReceptorRfcFiltro.Trim().ToUpperInvariant(),
                    SoloFiniquitadas ? true : null,
                    string.IsNullOrWhiteSpace(ReferenciaFiltro) ? null : ReferenciaFiltro.Trim(),
                    FechaInicioFiltro,
                    FechaFinFiltro);

                _cabecerasBase.Clear();
                _cabecerasBase.AddRange(resultado.Cabeceras.OrderBy(item => item.ReceptorNombreTexto).ThenBy(item => item.ReceptorRfcTexto));
                _detallesBase.Clear();
                _detallesBase.AddRange(resultado.Detalles.OrderByDescending(item => item.FechaTimbrado).ThenBy(item => item.FolioTexto));

                Cabeceras = new ObservableCollection<ReporteFinancieroFacturacionCabeceraDto>(_cabecerasBase);
                CabeceraSeleccionada = Cabeceras.FirstOrDefault();
                ActualizarDetallesVisibles();

                SuccessMessage = _detallesBase.Count == 0
                    ? "No se encontraron registros con los filtros actuales."
                    : $"Reporte cargado correctamente. Se obtuvieron {_detallesBase.Count} factura(s).";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el reporte financiero de facturación: {ex.Message}";
                Cabeceras = new ObservableCollection<ReporteFinancieroFacturacionCabeceraDto>();
                DetallesVisibles = new ObservableCollection<ReporteFinancieroFacturacionDetalleDto>();
                CabeceraSeleccionada = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LimpiarFiltros()
        {
            ReceptorRfcFiltro = null;
            ReferenciaFiltro = null;
            FechaInicioFiltro = null;
            FechaFinFiltro = null;
            SoloFiniquitadas = false;
        }

        public void SeleccionarCabecera(ReporteFinancieroFacturacionCabeceraDto? cabecera)
        {
            CabeceraSeleccionada = cabecera;
            ActualizarDetallesVisibles();
        }

        private void ActualizarDetallesVisibles()
        {
            IEnumerable<ReporteFinancieroFacturacionDetalleDto> detalles = _detallesBase;

            if (!string.IsNullOrWhiteSpace(CabeceraSeleccionada?.ReceptorRfc))
            {
                detalles = detalles.Where(item => string.Equals(item.ReceptorRfc, CabeceraSeleccionada.ReceptorRfc, StringComparison.OrdinalIgnoreCase));
            }

            DetallesVisibles = new ObservableCollection<ReporteFinancieroFacturacionDetalleDto>(detalles);
            OnPropertyChanged(nameof(ResumenDetalleTexto));
        }

        private void NotificarResumenes()
        {
            OnPropertyChanged(nameof(TotalClientesTexto));
            OnPropertyChanged(nameof(TotalFacturadoTexto));
            OnPropertyChanged(nameof(TotalAbonadoTexto));
            OnPropertyChanged(nameof(ResumenCabecerasTexto));
            OnPropertyChanged(nameof(ResumenDetalleTexto));
        }
    }
}
