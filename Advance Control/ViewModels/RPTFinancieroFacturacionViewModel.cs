using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Email;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Reportes;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// Cobranza como mesa de trabajo por cliente: listado de clientes con saldo a la izquierda y,
    /// del cliente seleccionado, su historial (seguimientos y documentos generados), sus facturas
    /// y los documentos que se le pueden generar o enviar. Todos los documentos son de un solo
    /// cliente y cada uno deja un seguimiento "Documento: ..." con quién lo generó.
    /// </summary>
    public class RPTFinancieroFacturacionViewModel : ViewModelBase
    {
        /// <summary>Prefijo de los seguimientos que se registran solos al generar o enviar un documento.</summary>
        public const string PrefijoDocumento = "Documento: ";

        private static readonly CultureInfo CulturaMx = new("es-MX");

        private readonly IReporteFinancieroFacturacionService _reporteService;
        private readonly IReporteFinancieroFacturacionExportService _exportService;
        private readonly IHistorialCobranzaService _historialService;
        private readonly IEmailService _emailService;
        private readonly IClienteService _clienteService;
        private readonly IOperacionService _operacionService;
        private readonly List<ReporteFinancieroFacturacionCabeceraDto> _cabecerasBase = new();
        private readonly List<ReporteFinancieroFacturacionDetalleDto> _detallesBase = new();
        private ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> _clientes = new();
        private ReporteFinancieroFacturacionCabeceraDto? _clienteSeleccionado;
        private ObservableCollection<ReporteFinancieroFacturacionDetalleDto> _facturasCliente = new();
        private ObservableCollection<CobranzaSeguimientoDto> _seguimientosCliente = new();
        private bool _isLoading;
        private bool _isCargandoSeguimientos;
        private string _operacionesAbiertasTexto = "—";
        private string _operacionesTFinalizadoTexto = "—";
        private string? _errorMessage;
        private string? _successMessage;
        private string? _busquedaCliente;
        private bool _soloConSaldo = true;
        private bool _soloVencidos;
        private int _filtroEstadoFacturas;
        private string? _busquedaFactura;
        private string? _referenciaFiltro;
        private DateTimeOffset? _fechaInicioFiltro;
        private DateTimeOffset? _fechaFinFiltro;
        private int _estadoDocumentos;
        // Los movimientos no conciliados no se incluyen en los reportes de Cobranza.
        private const bool MostrarMovimientosNoConciliados = false;
        private bool _isGenerandoHistorial;
        private string? _historialProgresoTexto;
        private HistorialCobranzaResultadoDto? _historialResultado;
        private bool _historialAbiertas = true;
        private bool _historialAbiertasSinOrden = true;
        private bool _historialFinalizadaConOrden = true;
        private bool _historialFinalizadaSinOrden = true;
        private bool _historialPendientes = true;

        public RPTFinancieroFacturacionViewModel(
            IReporteFinancieroFacturacionService reporteService,
            IReporteFinancieroFacturacionExportService exportService,
            IHistorialCobranzaService historialService,
            IEmailService emailService,
            IClienteService clienteService,
            IOperacionService operacionService)
        {
            _reporteService = reporteService ?? throw new ArgumentNullException(nameof(reporteService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _historialService = historialService ?? throw new ArgumentNullException(nameof(historialService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _operacionService = operacionService ?? throw new ArgumentNullException(nameof(operacionService));
        }

        // ---------------- Estado general ----------------

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(PuedeGenerarDocumentos));
                }
            }
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

        // ---------------- Listado de clientes ----------------

        public ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> Clientes
        {
            get => _clientes;
            private set
            {
                if (SetProperty(ref _clientes, value))
                {
                    OnPropertyChanged(nameof(ResumenClientesTexto));
                }
            }
        }

        public string? BusquedaCliente
        {
            get => _busquedaCliente;
            set { if (SetProperty(ref _busquedaCliente, value)) AplicarFiltroClientes(); }
        }

        public bool SoloConSaldo
        {
            get => _soloConSaldo;
            set { if (SetProperty(ref _soloConSaldo, value)) AplicarFiltroClientes(); }
        }

        public bool SoloVencidos
        {
            get => _soloVencidos;
            set { if (SetProperty(ref _soloVencidos, value)) AplicarFiltroClientes(); }
        }

        public string ResumenClientesTexto =>
            $"{Clientes.Count} cliente(s) · saldo {Clientes.Sum(c => c.Saldo).ToString("C2", CulturaMx)} · vencido {Clientes.Sum(c => c.TotalVencido).ToString("C2", CulturaMx)}";

        // ---------------- Cliente seleccionado ----------------

        public ReporteFinancieroFacturacionCabeceraDto? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                if (SetProperty(ref _clienteSeleccionado, value))
                {
                    OnPropertyChanged(nameof(TieneClienteSeleccionado));
                    OnPropertyChanged(nameof(NoTieneClienteSeleccionado));
                    OnPropertyChanged(nameof(PuedeGenerarDocumentos));
                    AplicarFiltroFacturas();
                    _ = CargarSeguimientosAsync();
                    _ = CargarOperacionesAsync();
                }
            }
        }

        public bool TieneClienteSeleccionado => ClienteSeleccionado != null;
        public bool NoTieneClienteSeleccionado => ClienteSeleccionado == null;
        public bool PuedeGenerarDocumentos => ClienteSeleccionado != null && !IsLoading;

        public ObservableCollection<ReporteFinancieroFacturacionDetalleDto> FacturasCliente
        {
            get => _facturasCliente;
            private set
            {
                if (SetProperty(ref _facturasCliente, value))
                {
                    OnPropertyChanged(nameof(ResumenFacturasTexto));
                }
            }
        }

        /// <summary>0 = todas, 1 = pendientes, 2 = finiquitadas.</summary>
        public int FiltroEstadoFacturas
        {
            get => _filtroEstadoFacturas;
            set { if (SetProperty(ref _filtroEstadoFacturas, value)) AplicarFiltroFacturas(); }
        }

        public string? BusquedaFactura
        {
            get => _busquedaFactura;
            set { if (SetProperty(ref _busquedaFactura, value)) AplicarFiltroFacturas(); }
        }

        public string ResumenFacturasTexto => $"{FacturasCliente.Count} factura(s)";

        public ObservableCollection<CobranzaSeguimientoDto> SeguimientosCliente
        {
            get => _seguimientosCliente;
            private set
            {
                if (SetProperty(ref _seguimientosCliente, value))
                {
                    OnPropertyChanged(nameof(UltimoDocumentoTexto));
                    OnPropertyChanged(nameof(SinSeguimientos));
                }
            }
        }

        public bool IsCargandoSeguimientos
        {
            get => _isCargandoSeguimientos;
            private set => SetProperty(ref _isCargandoSeguimientos, value);
        }

        public bool SinSeguimientos => SeguimientosCliente.Count == 0;

        /// <summary>"Último reporte: quién · cuándo · qué", a partir del seguimiento automático más reciente.</summary>
        public string UltimoDocumentoTexto
        {
            get
            {
                var ultimo = SeguimientosCliente.FirstOrDefault(s =>
                    s.TipoContacto?.StartsWith(PrefijoDocumento, StringComparison.OrdinalIgnoreCase) == true);
                return ultimo == null
                    ? "Último reporte: ninguno registrado"
                    : $"Último reporte: {ultimo.RegistradoPorTexto} · {ultimo.FechaSeguimientoTexto} · {ultimo.TipoContacto![PrefijoDocumento.Length..]}";
            }
        }

        /// <summary>Operaciones sin factura y sin T-Finalizado (con cuántas traen orden de compra).</summary>
        public string OperacionesAbiertasTexto
        {
            get => _operacionesAbiertasTexto;
            private set => SetProperty(ref _operacionesAbiertasTexto, value);
        }

        /// <summary>Operaciones con T-Finalizado que todavía no tienen factura (por facturar).</summary>
        public string OperacionesTFinalizadoTexto
        {
            get => _operacionesTFinalizadoTexto;
            private set => SetProperty(ref _operacionesTFinalizadoTexto, value);
        }

        // ---------------- Opciones de documentos ----------------

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

        /// <summary>Qué facturas entran a los documentos: 0 = todas, 1 = pendientes, 2 = finiquitadas.</summary>
        public int EstadoDocumentos
        {
            get => _estadoDocumentos;
            set => SetProperty(ref _estadoDocumentos, value);
        }

        // ---------------- Carga ----------------

        /// <summary>Carga una sola vez todas las cabeceras y facturas; los filtros se aplican en memoria.</summary>
        public async Task CargarReporteAsync()
        {
            var rfcSeleccionado = ClienteSeleccionado?.ReceptorRfc;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _reporteService.ObtenerReporteAsync(null, null, null, null, null);

                _cabecerasBase.Clear();
                _cabecerasBase.AddRange(resultado.Cabeceras);
                _detallesBase.Clear();
                _detallesBase.AddRange(resultado.Detalles.OrderByDescending(item => item.FechaTimbrado).ThenBy(item => item.FolioTexto));

                AplicarFiltroClientes();

                ClienteSeleccionado = rfcSeleccionado == null
                    ? null
                    : _cabecerasBase.FirstOrDefault(c => MismoRfc(c.ReceptorRfc, rfcSeleccionado));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar la cobranza: {ex.Message}";
                Clientes = new ObservableCollection<ReporteFinancieroFacturacionCabeceraDto>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AplicarFiltroClientes()
        {
            var texto = BusquedaCliente?.Trim();
            var filtrados = _cabecerasBase
                .Where(c => !SoloConSaldo || c.Saldo > 0.005m)
                .Where(c => !SoloVencidos || c.TieneVencido)
                .Where(c => string.IsNullOrEmpty(texto)
                    || c.ReceptorRfcTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || c.ReceptorNombreTexto.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.TotalVencido)
                .ThenByDescending(c => c.Saldo)
                .ThenBy(c => c.ReceptorNombreTexto);

            Clientes = new ObservableCollection<ReporteFinancieroFacturacionCabeceraDto>(filtrados);
        }

        private void AplicarFiltroFacturas()
        {
            if (ClienteSeleccionado == null)
            {
                FacturasCliente = new ObservableCollection<ReporteFinancieroFacturacionDetalleDto>();
                return;
            }

            var texto = BusquedaFactura?.Trim();
            var facturas = DetallesDe(ClienteSeleccionado.ReceptorRfc)
                .Where(f => FiltroEstadoFacturas switch
                {
                    1 => f.Finiquito != true,
                    2 => f.Finiquito == true,
                    _ => true
                })
                .Where(f => string.IsNullOrEmpty(texto)
                    || f.FolioTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || f.ReferenciaTexto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || (f.ConceptosTexto?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false));

            FacturasCliente = new ObservableCollection<ReporteFinancieroFacturacionDetalleDto>(facturas);
        }

        private IEnumerable<ReporteFinancieroFacturacionDetalleDto> DetallesDe(string? rfc) =>
            _detallesBase.Where(d => MismoRfc(d.ReceptorRfc, rfc));

        private static bool MismoRfc(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

        public async Task CargarSeguimientosAsync()
        {
            var rfc = ClienteSeleccionado?.ReceptorRfc;
            if (string.IsNullOrWhiteSpace(rfc))
            {
                SeguimientosCliente = new ObservableCollection<CobranzaSeguimientoDto>();
                return;
            }

            try
            {
                IsCargandoSeguimientos = true;
                var seguimientos = await _reporteService.ObtenerSeguimientosAsync(rfc);

                // Si mientras tanto se eligió otro cliente, esta respuesta ya no aplica.
                if (MismoRfc(ClienteSeleccionado?.ReceptorRfc, rfc))
                {
                    SeguimientosCliente = new ObservableCollection<CobranzaSeguimientoDto>(seguimientos.OrderByDescending(s => s.FechaSeguimiento));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo cargar el historial del cliente: {ex.Message}";
            }
            finally
            {
                IsCargandoSeguimientos = false;
            }
        }

        /// <summary>
        /// Cuenta las operaciones del cliente que aún no tienen factura, con la misma regla que el
        /// Historial de cobranza: abiertas = sin T-Finalizado; T-Finalizado = trabajo terminado
        /// y por facturar. Las operaciones se ligan por id de cliente, que se resuelve por RFC.
        /// </summary>
        public async Task CargarOperacionesAsync()
        {
            var rfc = ClienteSeleccionado?.ReceptorRfc;
            OperacionesAbiertasTexto = "…";
            OperacionesTFinalizadoTexto = "…";
            if (string.IsNullOrWhiteSpace(rfc))
            {
                return;
            }

            try
            {
                var clientes = await _clienteService.GetClientesAsync(new ClienteQueryDto { Rfc = rfc });
                var cliente = clientes?.FirstOrDefault(c => MismoRfc(c.Rfc, rfc)) ?? clientes?.FirstOrDefault();

                // Sin IncluirFacturadas la API ya excluye las operaciones con factura.
                var operaciones = cliente == null
                    ? new List<OperacionDto>()
                    : await _operacionService.GetOperacionesAsync(new OperacionQueryDto { IdCliente = cliente.IdCliente, IncluirFinalizadas = true });

                if (!MismoRfc(ClienteSeleccionado?.ReceptorRfc, rfc))
                {
                    return;
                }

                if (cliente == null)
                {
                    OperacionesAbiertasTexto = OperacionesTFinalizadoTexto = "Sin cliente en catálogo";
                    return;
                }

                var abiertas = operaciones.Where(o => !o.TFinalizado).ToList();
                var tFinalizadas = operaciones.Where(o => o.TFinalizado).ToList();
                OperacionesAbiertasTexto = $"{abiertas.Count} ({abiertas.Count(o => o.CkOrdenCompraCargadaApi)} con OC)";
                OperacionesTFinalizadoTexto = $"{tFinalizadas.Count} ({tFinalizadas.Count(o => o.CkOrdenCompraCargadaApi)} con OC)";
            }
            catch (Exception ex)
            {
                if (MismoRfc(ClienteSeleccionado?.ReceptorRfc, rfc))
                {
                    OperacionesAbiertasTexto = OperacionesTFinalizadoTexto = "Error";
                    ErrorMessage = $"No se pudieron contar las operaciones del cliente: {ex.Message}";
                }
            }
        }

        public async Task AgregarSeguimientoAsync(CobranzaSeguimientoCreateDto dto)
        {
            var cliente = ClienteSeleccionado ?? throw new InvalidOperationException("Selecciona un cliente.");
            dto.ReceptorRfc = cliente.ReceptorRfc;

            try
            {
                var creado = await _reporteService.RegistrarSeguimientoAsync(dto);
                if (creado != null && MismoRfc(ClienteSeleccionado?.ReceptorRfc, cliente.ReceptorRfc))
                {
                    SeguimientosCliente.Insert(0, creado);
                    OnPropertyChanged(nameof(UltimoDocumentoTexto));
                    OnPropertyChanged(nameof(SinSeguimientos));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"No se pudo guardar el seguimiento: {ex.Message}";
            }
        }

        /// <summary>
        /// Anota en la bitácora del cliente quién generó o envió un documento. Si falla, el documento
        /// ya se generó: solo se avisa.
        /// </summary>
        private async Task RegistrarDocumentoAsync(ReporteFinancieroFacturacionCabeceraDto cliente, string documento, string nota)
        {
            try
            {
                await AgregarSeguimientoAsync(new CobranzaSeguimientoCreateDto
                {
                    ReceptorRfc = cliente.ReceptorRfc,
                    TipoContacto = PrefijoDocumento + documento,
                    Nota = nota
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"El documento se generó, pero no se pudo anotar en el historial: {ex.Message}";
            }
        }

        // ---------------- Documentos del cliente seleccionado ----------------

        private ReporteFinancieroFacturacionCabeceraDto ClienteParaDocumento()
        {
            var cliente = ClienteSeleccionado ?? throw new InvalidOperationException("Selecciona un cliente.");
            if (string.IsNullOrWhiteSpace(cliente.ReceptorRfc))
            {
                throw new InvalidOperationException("El cliente seleccionado no tiene RFC.");
            }

            if (FechaInicioFiltro.HasValue && FechaFinFiltro.HasValue && FechaFinFiltro.Value.Date < FechaInicioFiltro.Value.Date)
            {
                throw new InvalidOperationException("La fecha fin no puede ser menor que la fecha inicio.");
            }

            return cliente;
        }

        private bool? FiniquitoDocumentos => EstadoDocumentos switch
        {
            1 => false,
            2 => true,
            _ => null
        };

        private string? ReferenciaDocumentos => string.IsNullOrWhiteSpace(ReferenciaFiltro) ? null : ReferenciaFiltro.Trim();

        /// <summary>Texto para la nota del historial: periodo, facturas incluidas y referencia.</summary>
        private string DescribirOpcionesDocumento()
        {
            var periodo = (FechaInicioFiltro, FechaFinFiltro) switch
            {
                ({ } ini, { } fin) => $"{ini:dd/MM/yyyy} al {fin:dd/MM/yyyy}",
                ({ } ini, null) => $"desde {ini:dd/MM/yyyy}",
                (null, { } fin) => $"hasta {fin:dd/MM/yyyy}",
                _ => "todas las fechas"
            };
            var estado = EstadoDocumentos switch { 1 => "pendientes", 2 => "finiquitadas", _ => "todas" };
            var nota = $"Periodo: {periodo} · Facturas: {estado}";
            return ReferenciaDocumentos == null ? nota : $"{nota} · Referencia: {ReferenciaDocumentos}";
        }

        public Task<string> GenerarReporteAsync() =>
            GenerarReporteFinancieroAsync(simplificado: false);

        public Task<string> GenerarReporteSimplificadoAsync() =>
            GenerarReporteFinancieroAsync(simplificado: true);

        private async Task<string> GenerarReporteFinancieroAsync(bool simplificado)
        {
            var cliente = ClienteParaDocumento();
            var nombreDocumento = simplificado ? "Reporte simplificado" : "Reporte";

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var resultado = await _reporteService.ObtenerReporteAsync(
                    cliente.ReceptorRfc, FiniquitoDocumentos, ReferenciaDocumentos, FechaInicioFiltro, FechaFinFiltro);

                var cabeceras = resultado.Cabeceras.Where(c => MismoRfc(c.ReceptorRfc, cliente.ReceptorRfc)).ToList();
                var detalles = resultado.Detalles.Where(d => MismoRfc(d.ReceptorRfc, cliente.ReceptorRfc))
                    .OrderByDescending(d => d.FechaTimbrado).ThenBy(d => d.FolioTexto).ToList();

                if (detalles.Count == 0)
                {
                    throw new InvalidOperationException("El cliente no tiene facturas con las opciones elegidas.");
                }

                var rutaArchivo = simplificado
                    ? await _exportService.GenerarReporteSimplificadoPdfAsync(
                        cabeceras, detalles, cliente.ReceptorRfc, ReferenciaDocumentos, FechaInicioFiltro, FechaFinFiltro, FiniquitoDocumentos,
                        resultado.MovimientosNoConciliadosCount, resultado.MovimientosNoConciliadosTotal, MostrarMovimientosNoConciliados)
                    : await _exportService.GenerarReportePdfAsync(
                        cabeceras, detalles, cliente.ReceptorRfc, ReferenciaDocumentos, FechaInicioFiltro, FechaFinFiltro, FiniquitoDocumentos,
                        resultado.MovimientosNoConciliadosCount, resultado.MovimientosNoConciliadosTotal, MostrarMovimientosNoConciliados);

                SuccessMessage = $"{nombreDocumento} generado: {rutaArchivo}";
                await RegistrarDocumentoAsync(cliente, nombreDocumento, DescribirOpcionesDocumento());
                return rutaArchivo;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el {nombreDocumento.ToLowerInvariant()}: {ex.Message}";
                SuccessMessage = null;
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<string> GenerarReporteCobranzaAsync()
        {
            var cliente = ClienteParaDocumento();

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var items = await _reporteService.ObtenerReporteCobranzaAsync(
                    cliente.ReceptorRfc, FiniquitoDocumentos, ReferenciaDocumentos, FechaInicioFiltro, FechaFinFiltro);

                if (items.Count == 0)
                {
                    throw new InvalidOperationException("El cliente no tiene facturas con las opciones elegidas para el reporte de cobranza.");
                }

                var rutaArchivo = await _exportService.GenerarReporteCobranzaPdfAsync(
                    items, cliente.ReceptorRfc, ReferenciaDocumentos, FechaInicioFiltro, FechaFinFiltro, FiniquitoDocumentos);

                SuccessMessage = $"Reporte de cobranza generado: {rutaArchivo}";
                await RegistrarDocumentoAsync(cliente, "Reporte de cobranza", DescribirOpcionesDocumento());
                return rutaArchivo;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el reporte de cobranza: {ex.Message}";
                SuccessMessage = null;
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<string> EnviarAlertaCobranzaAsync(IReadOnlyList<string> destinatarios)
        {
            var cliente = ClienteParaDocumento();

            if (destinatarios == null || destinatarios.Count == 0)
            {
                throw new InvalidOperationException("Selecciona al menos un contacto con correo para enviar la alerta.");
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var items = await _reporteService.ObtenerReporteCobranzaAsync(cliente.ReceptorRfc, null, null, null, null);
                if (items.Count == 0)
                {
                    throw new InvalidOperationException("El cliente no tiene facturas pendientes para incluir en la alerta de cobranza.");
                }

                var rutaPdf = await _exportService.GenerarReporteCobranzaPdfAsync(items, cliente.ReceptorRfc, null, null, null, null);
                var pdfBytes = await File.ReadAllBytesAsync(rutaPdf);

                var mensaje = new EmailMessage
                {
                    Para = destinatarios.ToList(),
                    Asunto = $"Alerta de cobranza - {cliente.ReceptorNombreTexto}",
                    CuerpoTexto =
                        $"Estimado(a),{Environment.NewLine}{Environment.NewLine}" +
                        $"Le compartimos el estado de cuenta de facturación pendiente de {cliente.ReceptorNombreTexto} (RFC {cliente.ReceptorRfcTexto}).{Environment.NewLine}{Environment.NewLine}" +
                        $"Saldo pendiente: {cliente.SaldoTexto}{Environment.NewLine}" +
                        $"Facturas pendientes: {cliente.NumeroFacturasNoFiniquitadas}{Environment.NewLine}{Environment.NewLine}" +
                        "Se adjunta el detalle. Quedamos atentos.",
                    Adjuntos = { (Path.GetFileName(rutaPdf), pdfBytes) },
                    CarpetaCliente = cliente.ReceptorNombreTexto
                };

                await _emailService.SendEmailAsync(mensaje);

                SuccessMessage = $"Alerta de cobranza enviada a {destinatarios.Count} contacto(s) de {cliente.ReceptorNombreTexto}.";
                await RegistrarDocumentoAsync(cliente, "Alerta por correo", $"Enviada a: {string.Join(", ", destinatarios)}");
                return rutaPdf;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al enviar la alerta de cobranza: {ex.Message}";
                SuccessMessage = null;
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ---------------- Historial de cobranza (carpeta de documentos) ----------------

        public bool IsGenerandoHistorial
        {
            get => _isGenerandoHistorial;
            set
            {
                if (SetProperty(ref _isGenerandoHistorial, value))
                {
                    OnPropertyChanged(nameof(CanGenerarHistorial));
                }
            }
        }

        public string? HistorialProgresoTexto
        {
            get => _historialProgresoTexto;
            set => SetProperty(ref _historialProgresoTexto, value);
        }

        public HistorialCobranzaResultadoDto? HistorialResultado
        {
            get => _historialResultado;
            private set
            {
                if (SetProperty(ref _historialResultado, value))
                {
                    OnPropertyChanged(nameof(HistorialResumenTexto));
                    OnPropertyChanged(nameof(HistorialErrores));
                    OnPropertyChanged(nameof(HistorialTieneErrores));
                }
            }
        }

        public string HistorialResumenTexto => HistorialResultado?.ResumenTexto ?? string.Empty;

        public ObservableCollection<string> HistorialErrores => HistorialResultado?.Errores.Count > 0
            ? new ObservableCollection<string>(HistorialResultado.Errores)
            : new ObservableCollection<string>();

        public bool HistorialTieneErrores => HistorialResultado?.Errores.Count > 0;

        /// <summary>Abiertas: sin T-Finalizado, sin factura, con orden de compra.</summary>
        public bool HistorialAbiertas
        {
            get => _historialAbiertas;
            set { if (SetProperty(ref _historialAbiertas, value)) NotificarEstadosHistorial(); }
        }

        /// <summary>Abiertas S/Orden: sin T-Finalizado, sin factura, sin orden de compra.</summary>
        public bool HistorialAbiertasSinOrden
        {
            get => _historialAbiertasSinOrden;
            set { if (SetProperty(ref _historialAbiertasSinOrden, value)) NotificarEstadosHistorial(); }
        }

        /// <summary>Finalizada C/Orden: T-Finalizado, sin factura, con orden de compra.</summary>
        public bool HistorialFinalizadaConOrden
        {
            get => _historialFinalizadaConOrden;
            set { if (SetProperty(ref _historialFinalizadaConOrden, value)) NotificarEstadosHistorial(); }
        }

        /// <summary>Finalizada S/Orden: T-Finalizado, sin factura, sin orden de compra.</summary>
        public bool HistorialFinalizadaSinOrden
        {
            get => _historialFinalizadaSinOrden;
            set { if (SetProperty(ref _historialFinalizadaSinOrden, value)) NotificarEstadosHistorial(); }
        }

        /// <summary>Pendientes: facturadas sin pago o no cubiertas en su totalidad.</summary>
        public bool HistorialPendientes
        {
            get => _historialPendientes;
            set { if (SetProperty(ref _historialPendientes, value)) NotificarEstadosHistorial(); }
        }

        private HistorialCobranzaEstadosDto EstadosHistorial => new()
        {
            Abiertas = HistorialAbiertas,
            AbiertasSinOrden = HistorialAbiertasSinOrden,
            FinalizadaConOrden = HistorialFinalizadaConOrden,
            FinalizadaSinOrden = HistorialFinalizadaSinOrden,
            Pendientes = HistorialPendientes
        };

        /// <summary>Texto del dropdown: cuántos estados están marcados.</summary>
        public string HistorialEstadosTexto
        {
            get
            {
                var marcados = new[] { HistorialAbiertas, HistorialAbiertasSinOrden, HistorialFinalizadaConOrden, HistorialFinalizadaSinOrden, HistorialPendientes }
                    .Count(marcado => marcado);
                return marcados switch
                {
                    5 => "Estados: todos",
                    0 => "Estados: ninguno",
                    _ => $"Estados: {marcados} de 5"
                };
            }
        }

        private void NotificarEstadosHistorial()
        {
            OnPropertyChanged(nameof(CanGenerarHistorial));
            OnPropertyChanged(nameof(HistorialEstadosTexto));
        }

        /// <summary>
        /// Solo se bloquea mientras se genera: lo que falte (fechas, estados) lo explica el botón al
        /// presionarlo, en vez de quedarse deshabilitado sin decir por qué.
        /// </summary>
        public bool CanGenerarHistorial => !IsGenerandoHistorial;

        public bool HistorialTieneEstadosMarcados => EstadosHistorial.AlgunoMarcado;

        /// <summary>
        /// Genera la carpeta de historial de cobranza del cliente seleccionado con el periodo de
        /// Documentos. idCliente y nombreCliente se resuelven en la página a partir del RFC.
        /// </summary>
        public async Task GenerarHistorialAsync(int idCliente, string nombreCliente, string? dirigidoA)
        {
            var cliente = ClienteParaDocumento();

            if (!FechaInicioFiltro.HasValue || !FechaFinFiltro.HasValue)
            {
                throw new InvalidOperationException("Selecciona un rango de fechas para generar el historial.");
            }

            if (idCliente <= 0)
            {
                throw new InvalidOperationException("El cliente seleccionado no es válido.");
            }

            try
            {
                IsGenerandoHistorial = true;
                ErrorMessage = null;
                SuccessMessage = null;
                HistorialResultado = null;
                HistorialProgresoTexto = "Iniciando...";

                var progreso = new Progress<string>(texto => HistorialProgresoTexto = texto);

                var resultado = await _historialService.GenerarHistorialAsync(
                    cliente.ReceptorRfc!.Trim().ToUpperInvariant(),
                    nombreCliente,
                    idCliente,
                    FechaInicioFiltro.Value,
                    FechaFinFiltro.Value,
                    dirigidoA,
                    EstadosHistorial,
                    progreso);

                HistorialResultado = resultado;
                SuccessMessage = $"Historial generado en: {resultado.CarpetaHistorial} ({resultado.ResumenTexto})";

                var nota = $"Periodo: {FechaInicioFiltro.Value:dd/MM/yyyy} al {FechaFinFiltro.Value:dd/MM/yyyy} · {HistorialEstadosTexto} · {resultado.ResumenTexto}";
                if (!string.IsNullOrWhiteSpace(dirigidoA))
                {
                    nota += $" · Dirigido a: {dirigidoA}";
                }
                await RegistrarDocumentoAsync(cliente, "Historial de cobranza", nota);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al generar el historial de cobranza: {ex.Message}";
                SuccessMessage = null;
                throw;
            }
            finally
            {
                IsGenerandoHistorial = false;
                HistorialProgresoTexto = null;
            }
        }
    }
}
