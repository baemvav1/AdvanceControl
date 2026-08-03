using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Facturacion;
using Advance_Control.Services.Facturas;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Advance_Control.ViewModels
{
    public class FacturacionViewModel : ViewModelBase
    {
        private readonly IFacturaService _facturaService;
        private readonly FacturaOperacionMatchingEngine _matchingEngine;
        private ObservableCollection<OperacionSinFacturaDto> _operacionesSinFactura;
        private ObservableCollection<OperacionFacturadaDto> _operacionesFacturadas;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;

        public FacturacionViewModel(IFacturaService facturaService, FacturaOperacionMatchingEngine matchingEngine)
        {
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _matchingEngine = matchingEngine ?? throw new ArgumentNullException(nameof(matchingEngine));
            _operacionesSinFactura = new ObservableCollection<OperacionSinFacturaDto>();
            _operacionesFacturadas = new ObservableCollection<OperacionFacturadaDto>();
        }

        public ObservableCollection<OperacionSinFacturaDto> OperacionesSinFactura
        {
            get => _operacionesSinFactura;
            set => SetProperty(ref _operacionesSinFactura, value);
        }

        public ObservableCollection<OperacionFacturadaDto> OperacionesFacturadas
        {
            get => _operacionesFacturadas;
            set => SetProperty(ref _operacionesFacturadas, value);
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

        public string ResumenSinFactura => $"{OperacionesSinFactura.Count} operación(es) sin factura";
        public string ResumenFacturadas => $"{OperacionesFacturadas.Count} operación(es) facturada(s)";

        public async Task CargarAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var sinFactura = await _facturaService.ObtenerOperacionesSinFacturaAsync();
                var facturadas = await _facturaService.ObtenerOperacionesFacturadasAsync();
                var todasLasFacturas = await _facturaService.ObtenerFacturasAsync();

                var facturasSinOperacion = todasLasFacturas
                    .Where(factura => !factura.IdOperacion.HasValue || factura.IdOperacion.Value <= 0)
                    .ToList();

                foreach (var operacion in sinFactura)
                {
                    operacion.Sugerencias = _matchingEngine
                        .ObtenerCandidatos(operacion, facturasSinOperacion)
                        .ToList();
                }

                OperacionesSinFactura = new ObservableCollection<OperacionSinFacturaDto>(sinFactura);
                OperacionesFacturadas = new ObservableCollection<OperacionFacturadaDto>(facturadas);
                OnPropertyChanged(nameof(ResumenSinFactura));
                OnPropertyChanged(nameof(ResumenFacturadas));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar las operaciones: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task VincularSugerenciaAsync(OperacionSinFacturaDto operacion, FacturaResumenDto factura)
        {
            if (operacion == null || factura == null)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                await _facturaService.VincularFacturaOperacionAsync(factura.IdFactura, operacion.IdOperacion);

                SuccessMessage = $"Factura {factura.Folio} vinculada a la operación #{operacion.IdOperacion}.";
                await CargarAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al vincular la factura sugerida: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task DesligarFacturaAsync(OperacionFacturadaDto operacion)
        {
            if (operacion == null)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _facturaService.DesvincularFacturaOperacionAsync(operacion.IdOperacion);

                SuccessMessage = string.IsNullOrWhiteSpace(resultado.Mensaje)
                    ? $"Factura desligada de la operación #{operacion.IdOperacion}. La factura se conserva."
                    : resultado.Mensaje;
                await CargarAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al desligar la factura: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CargarXmlParaOperacionAsync(nint windowHandle, XamlRoot xamlRoot, OperacionSinFacturaDto operacion)
        {
            if (operacion == null)
            {
                return;
            }

            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".xml");

            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            await ProcesarArchivoXmlAsync(file, operacion);
        }

        /// <summary>
        /// Parsea y vincula un XML de factura ya obtenido (desde el FilePicker o desde un
        /// arrastrar-y-soltar) a la operación indicada.
        /// </summary>
        public async Task ProcesarArchivoXmlAsync(StorageFile file, OperacionSinFacturaDto operacion)
        {
            if (file == null || operacion == null)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var xmlContent = await FileIO.ReadTextAsync(file);
                var request = CfdiXmlParser.ParseXmlToRequest(xmlContent);
                request.IdOperacion = operacion.IdOperacion;

                var result = await _facturaService.GuardarFacturaAsync(request);
                if (!result.Success && !string.Equals(result.Accion, "existente", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? $"No se pudo vincular la factura {file.Name} a la operación."
                        : result.Message;
                    return;
                }

                SuccessMessage = $"Factura {file.Name} vinculada a la operación #{operacion.IdOperacion}.";
                await CargarAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el XML de factura: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CancelarFacturaAsync(OperacionFacturadaDto operacion)
        {
            if (operacion == null)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                await _facturaService.CancelarFacturaOperacionAsync(operacion.IdOperacion);

                SuccessMessage = $"Factura desvinculada de la operación #{operacion.IdOperacion}.";
                await CargarAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cancelar la factura: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
