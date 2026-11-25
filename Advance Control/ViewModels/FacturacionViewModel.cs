using System;
using System.Collections.Generic;
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

        private readonly Services.Entidades.IEntidadService _entidadService;

        /// <summary>RFC de cliente -> entidades de facturación de los clientes con ese RFC.</summary>
        private Dictionary<string, HashSet<int>> _entidadesPorRfcCliente = new(StringComparer.OrdinalIgnoreCase);
        private int? _idEntidadPredeterminada;

        public FacturacionViewModel(IFacturaService facturaService, FacturaOperacionMatchingEngine matchingEngine, Services.Entidades.IEntidadService entidadService)
        {
            _entidadService = entidadService ?? throw new ArgumentNullException(nameof(entidadService));
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _matchingEngine = matchingEngine ?? throw new ArgumentNullException(nameof(matchingEngine));
            _operacionesSinFactura = new ObservableCollection<OperacionSinFacturaDto>();
            _operacionesFacturadas = new ObservableCollection<OperacionFacturadaDto>();
        }

        // ---------------- Entidad (razón social) sobre la que se trabaja ----------------

        public List<EntidadFiltroItem> EntidadesFiltro { get; private set; } = new();

        private EntidadFiltroItem? _entidadFiltro;
        public EntidadFiltroItem? EntidadFiltro
        {
            get => _entidadFiltro;
            set => SetProperty(ref _entidadFiltro, value);
        }

        private async Task AsegurarEntidadesFiltroAsync()
        {
            if (EntidadesFiltro.Count > 0) return;
            try
            {
                var (opciones, inicial) = await EntidadFiltroItem.CargarAsync(_entidadService, incluirTodas: true);
                EntidadesFiltro = opciones;
                EntidadFiltro ??= inicial;
                _idEntidadPredeterminada = opciones.FirstOrDefault(o => o.Entidad?.EsPredeterminada == true)?.IdEntidad;
            }
            catch
            {
                // Sin entidades no se filtra: se ve todo, como antes.
            }
        }

        private async Task CargarEntidadesClientesAsync()
        {
            try
            {
                _entidadesPorRfcCliente = (await _entidadService.GetEntidadesFacturacionClientesAsync())
                    .Where(c => !string.IsNullOrWhiteSpace(c.Rfc) && c.IdEntidad.HasValue)
                    .GroupBy(c => c.Rfc!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Select(c => c.IdEntidad!.Value).ToHashSet(), StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                _entidadesPorRfcCliente = new(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>Operación sin factura: es de la entidad de facturación de su cliente (o de la predeterminada).</summary>
        private bool SinFacturaEsDeEntidad(OperacionSinFacturaDto operacion)
        {
            if (EntidadFiltro is not { EsTodas: false } entidad) return true;
            var rfc = operacion.RfcCliente?.Trim();
            if (!string.IsNullOrEmpty(rfc) && _entidadesPorRfcCliente.TryGetValue(rfc, out var entidades))
                return entidades.Contains(entidad.IdEntidad!.Value);
            return entidad.IdEntidad == _idEntidadPredeterminada;
        }

        /// <summary>Facturas sin operación de la entidad elegida (candidatas para vincular).</summary>
        private List<FacturaResumenDto> FacturasSinOperacionDeEntidad(IEnumerable<FacturaResumenDto> todas)
            => todas
                .Where(factura => !factura.IdOperacion.HasValue || factura.IdOperacion.Value <= 0)
                .Where(factura => EntidadFiltro?.CoincideRfc(factura.EmisorRfc) ?? true)
                .ToList();

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

                await AsegurarEntidadesFiltroAsync();
                await CargarEntidadesClientesAsync();

                var todasLasFacturas = await _facturaService.ObtenerFacturasAsync();
                var emisorPorFactura = todasLasFacturas.ToDictionary(f => f.IdFactura, f => f.EmisorRfc);

                // Sin factura: por la entidad del cliente. Facturadas: por quien emitió la factura.
                var sinFactura = (await _facturaService.ObtenerOperacionesSinFacturaAsync())
                    .Where(SinFacturaEsDeEntidad)
                    .ToList();
                var facturadas = (await _facturaService.ObtenerOperacionesFacturadasAsync())
                    .Where(o => EntidadFiltro?.CoincideRfc(emisorPorFactura.GetValueOrDefault(o.IdFactura)) ?? true)
                    .ToList();

                var facturasSinOperacion = FacturasSinOperacionDeEntidad(todasLasFacturas);

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

                SuccessMessage = $"Factura {factura.Folio} vinculada a la operación #{operacion.FolioTexto}.";
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

        /// <summary>
        /// Calcula, para cada operación sin factura, las facturas ya cargadas (sin operación)
        /// que coinciden en RFC y monto antes de IVA. Solo devuelve operaciones con al menos
        /// un candidato -- las que no tienen ninguno no se muestran en el selector. La factura
        /// que queda seleccionada por defecto en cada propuesta se saca del pool compartido,
        /// asi que dos propuestas del mismo lote nunca terminan apuntando a la misma factura.
        /// </summary>
        public async Task<IReadOnlyList<FacturaOperacionPropuestaDto>> CargarPropuestasVinculacionAsync()
        {
            await AsegurarEntidadesFiltroAsync();
            await CargarEntidadesClientesAsync();
            var sinFactura = (await _facturaService.ObtenerOperacionesSinFacturaAsync())
                .Where(SinFacturaEsDeEntidad)
                .ToList();
            var todasLasFacturas = await _facturaService.ObtenerFacturasAsync();
            var facturasSinOperacion = FacturasSinOperacionDeEntidad(todasLasFacturas);

            var propuestas = new List<FacturaOperacionPropuestaDto>();
            foreach (var operacion in sinFactura)
            {
                var candidatas = _matchingEngine.ObtenerCandidatos(operacion, facturasSinOperacion).ToList();
                if (candidatas.Count > 0)
                {
                    var propuesta = new FacturaOperacionPropuestaDto { Operacion = operacion, Candidatas = candidatas };
                    propuestas.Add(propuesta);

                    // Saca del pool compartido la factura que quedo seleccionada por defecto para
                    // esta propuesta, para que otras operaciones del mismo lote no la vuelvan a
                    // proponer (evita que dos propuestas aprobadas intenten vincular la misma
                    // factura y la segunda falle en fn_facturas_vincular_operacion).
                    facturasSinOperacion.RemoveAll(factura => factura.IdFactura == propuesta.FacturaSeleccionada.IdFactura);
                }
            }

            return propuestas;
        }

        /// <summary>
        /// Vincula, una por una, las propuestas aprobadas. fn_facturas_vincular_operacion ya
        /// marca la operación como finalizada al vincular, no hace falta un paso extra.
        /// Si una propuesta falla (p.ej. la factura ya se vinculo desde otra propuesta del mismo
        /// lote) se sigue con las demas y se reporta el error al final.
        /// </summary>
        public async Task<(int Vinculadas, List<string> Errores)> AplicarPropuestasVinculacionAprobadasAsync(
            IReadOnlyList<FacturaOperacionPropuestaDto> aprobadas)
        {
            var vinculadas = 0;
            var errores = new List<string>();

            foreach (var propuesta in aprobadas)
            {
                try
                {
                    await _facturaService.VincularFacturaOperacionAsync(
                        propuesta.FacturaSeleccionada.IdFactura,
                        propuesta.Operacion.IdOperacion);
                    vinculadas++;
                }
                catch (Exception ex)
                {
                    errores.Add($"Operación #{propuesta.Operacion.FolioTexto} ({propuesta.Operacion.RazonSocial}): {ex.Message}");
                }
            }

            if (vinculadas > 0)
            {
                await CargarAsync();
            }

            return (vinculadas, errores);
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
                    ? $"Factura desligada de la operación #{operacion.FolioTexto}. La factura se conserva."
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

                SuccessMessage = $"Factura {file.Name} vinculada a la operación #{operacion.FolioTexto}.";
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

                SuccessMessage = $"Factura desvinculada de la operación #{operacion.FolioTexto}.";
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
