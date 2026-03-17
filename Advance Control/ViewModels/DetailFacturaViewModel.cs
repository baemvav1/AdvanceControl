using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Facturas;

namespace Advance_Control.ViewModels
{
    public class DetailFacturaViewModel : ViewModelBase
    {
        private readonly IFacturaService _facturaService;
        private FacturaResumenDto? _factura;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;
        private double _montoAbono;
        private DateTimeOffset? _fechaAbono = DateTimeOffset.Now;
        private string? _referenciaAbono;
        private string? _observacionesAbono;

        public DetailFacturaViewModel(IFacturaService facturaService)
        {
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            Conceptos = new ObservableCollection<FacturaConceptoDto>();
            TrasladosGlobales = new ObservableCollection<FacturaTrasladoDto>();
            Abonos = new ObservableCollection<AbonoFacturaDto>();
        }

        public FacturaResumenDto? Factura
        {
            get => _factura;
            private set
            {
                if (SetProperty(ref _factura, value))
                {
                    OnPropertyChanged(nameof(CanRegistrarAbono));
                }
            }
        }

        public ObservableCollection<FacturaConceptoDto> Conceptos { get; }
        public ObservableCollection<FacturaTrasladoDto> TrasladosGlobales { get; }
        public ObservableCollection<AbonoFacturaDto> Abonos { get; }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanRegistrarAbono));
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public string? SuccessMessage
        {
            get => _successMessage;
            private set => SetProperty(ref _successMessage, value);
        }

        public double MontoAbono
        {
            get => _montoAbono;
            set
            {
                if (SetProperty(ref _montoAbono, value))
                {
                    OnPropertyChanged(nameof(CanRegistrarAbono));
                }
            }
        }

        public DateTimeOffset? FechaAbono
        {
            get => _fechaAbono;
            set => SetProperty(ref _fechaAbono, value);
        }

        public string? ReferenciaAbono
        {
            get => _referenciaAbono;
            set => SetProperty(ref _referenciaAbono, value);
        }

        public string? ObservacionesAbono
        {
            get => _observacionesAbono;
            set => SetProperty(ref _observacionesAbono, value);
        }

        public string ResumenConceptos => $"Conceptos ({Conceptos.Count})";
        public string ResumenTraslados => $"Traslados globales ({TrasladosGlobales.Count})";
        public string ResumenAbonos => $"Abonos registrados ({Abonos.Count})";
        public bool CanRegistrarAbono => Factura != null && !IsLoading && Factura.SaldoPendiente > 0 && MontoAbono > 0;

        public async Task CargarDetalleAsync(int idFactura)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var detalle = await _facturaService.ObtenerDetalleFacturaAsync(idFactura);
                if (detalle?.Factura == null)
                {
                    ErrorMessage = "No se encontro el detalle de la factura seleccionada.";
                    LimpiarDatos();
                    return;
                }

                Factura = detalle.Factura;
                ReemplazarColeccion(Conceptos, detalle.Conceptos);
                ReemplazarColeccion(TrasladosGlobales, detalle.TrasladosGlobales);
                ReemplazarColeccion(Abonos, detalle.Abonos);
                FechaAbono = DateTimeOffset.Now;
                MontoAbono = Factura.SaldoPendiente > 0 ? (double)Factura.SaldoPendiente : 0;
                ReferenciaAbono = null;
                ObservacionesAbono = null;
                OnPropertyChanged(nameof(ResumenConceptos));
                OnPropertyChanged(nameof(ResumenTraslados));
                OnPropertyChanged(nameof(ResumenAbonos));
                OnPropertyChanged(nameof(CanRegistrarAbono));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el detalle de la factura: {ex.Message}";
                LimpiarDatos();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void UsarSaldoCompleto()
        {
            if (Factura == null)
            {
                return;
            }

            MontoAbono = (double)Factura.SaldoPendiente;
        }

        public async Task RegistrarAbonoAsync()
        {
            if (Factura == null)
            {
                ErrorMessage = "No hay una factura cargada para registrar el abono.";
                return;
            }

            if (MontoAbono <= 0)
            {
                ErrorMessage = "El monto del abono debe ser mayor a cero.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var result = await _facturaService.RegistrarAbonoAsync(new RegistrarAbonoFacturaRequestDto
                {
                    IdFactura = Factura.IdFactura,
                    IdMovimiento = null,
                    FechaAbono = FechaAbono?.DateTime ?? DateTime.Now,
                    MontoAbono = Convert.ToDecimal(MontoAbono),
                    Referencia = ReferenciaAbono,
                    Observaciones = ObservacionesAbono
                });

                if (!result.Success)
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? "No se pudo registrar el abono."
                        : result.Message;
                    return;
                }

                await CargarDetalleAsync(Factura.IdFactura);
                SuccessMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Abono registrado correctamente."
                    : result.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al registrar el abono: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LimpiarDatos()
        {
            Factura = null;
            Conceptos.Clear();
            TrasladosGlobales.Clear();
            Abonos.Clear();
            MontoAbono = 0;
            FechaAbono = DateTimeOffset.Now;
            ReferenciaAbono = null;
            ObservacionesAbono = null;
            OnPropertyChanged(nameof(ResumenConceptos));
            OnPropertyChanged(nameof(ResumenTraslados));
            OnPropertyChanged(nameof(ResumenAbonos));
            OnPropertyChanged(nameof(CanRegistrarAbono));
        }

        private static void ReemplazarColeccion<T>(ObservableCollection<T> destino, System.Collections.Generic.IReadOnlyCollection<T> origen)
        {
            destino.Clear();
            foreach (var item in origen)
            {
                destino.Add(item);
            }
        }
    }
}
