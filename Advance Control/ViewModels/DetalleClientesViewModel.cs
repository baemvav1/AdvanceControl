using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.Reportes;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Advance_Control.ViewModels
{
    public class DetalleClientesViewModel : ViewModelBase
    {
        private static readonly Color ColorSaludBaja = Color.FromArgb(0xFF, 0xEA, 0x43, 0x35); // rojo
        private static readonly Color ColorSaludMedia = Color.FromArgb(0xFF, 0xFB, 0xBC, 0x04); // amarillo
        private static readonly Color ColorSaludAlta = Color.FromArgb(0xFF, 0x34, 0xA8, 0x53); // verde
        private static readonly Color ColorGris = Color.FromArgb(0xFF, 0x9A, 0xA0, 0xA6); // operación cerrada
        private const string ColorEquipoHex = "#4285F4"; // azul, puntos de la rama Equipos

        private readonly IClienteService _clienteService;
        private readonly IReporteFinancieroFacturacionService _reporteFinancieroService;
        private readonly IOperacionService _operacionService;
        private readonly ILoggingService _logger;

        private ObservableCollection<ClienteSaludCardItem> _clientes;
        private ObservableCollection<DetalleClienteTreeItem> _detalleTreeItems;
        private ObservableCollection<TimelineFila> _timelineFilas;
        private ObservableCollection<KpiItem> _kpisActuales;
        private DetalleClienteTreeItem? _nodoSeleccionado;
        private CustomerDto? _clienteSeleccionado;
        private bool _isLoading;
        private bool _isLoadingDetalle;
        private string? _errorMessage;

        // Datos crudos cacheados del cliente seleccionado, para poder reconstruir cualquier
        // timeline/KPI al cambiar de rama en el árbol sin volver a golpear la API.
        private List<ReporteFinancieroFacturacionDetalleDto> _clienteDetalles = new();
        private List<OperacionDto> _clienteOperaciones = new();
        private List<ReporteCobranzaItemDto> _clienteCobranza = new();
        private ReporteFinancieroFacturacionCabeceraDto? _clienteCabecera;

        // Lista completa sin filtrar, para poder buscar en vivo sin volver a golpear la API.
        private List<ClienteSaludCardItem> _todosLosClientes = new();
        private string _busquedaTexto = string.Empty;

        // Cobranza de toda la cartera (sin filtrar por cliente), para los KPIs de cartera completa.
        private List<ReporteCobranzaItemDto> _globalCobranza = new();

        public DetalleClientesViewModel(
            IClienteService clienteService,
            IReporteFinancieroFacturacionService reporteFinancieroService,
            IOperacionService operacionService,
            ILoggingService logger)
        {
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _reporteFinancieroService = reporteFinancieroService ?? throw new ArgumentNullException(nameof(reporteFinancieroService));
            _operacionService = operacionService ?? throw new ArgumentNullException(nameof(operacionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _clientes = new ObservableCollection<ClienteSaludCardItem>();
            _detalleTreeItems = new ObservableCollection<DetalleClienteTreeItem>();
            _timelineFilas = new ObservableCollection<TimelineFila>();
            _kpisActuales = new ObservableCollection<KpiItem>();
        }

        public ObservableCollection<ClienteSaludCardItem> Clientes
        {
            get => _clientes;
            private set => SetProperty(ref _clientes, value);
        }

        public string BusquedaTexto
        {
            get => _busquedaTexto;
            set
            {
                if (SetProperty(ref _busquedaTexto, value))
                {
                    AplicarFiltro();
                }
            }
        }

        public ObservableCollection<DetalleClienteTreeItem> DetalleTreeItems
        {
            get => _detalleTreeItems;
            private set => SetProperty(ref _detalleTreeItems, value);
        }

        public CustomerDto? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            private set
            {
                if (SetProperty(ref _clienteSeleccionado, value))
                {
                    OnPropertyChanged(nameof(TieneClienteSeleccionado));
                }
            }
        }

        public bool TieneClienteSeleccionado => ClienteSeleccionado != null;

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public bool IsLoadingDetalle
        {
            get => _isLoadingDetalle;
            private set => SetProperty(ref _isLoadingDetalle, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public DetalleClienteTreeItem? NodoSeleccionado
        {
            get => _nodoSeleccionado;
            private set
            {
                if (SetProperty(ref _nodoSeleccionado, value))
                {
                    OnPropertyChanged(nameof(TimelineTitulo));
                }
            }
        }

        public ObservableCollection<TimelineFila> TimelineFilas
        {
            get => _timelineFilas;
            private set
            {
                if (SetProperty(ref _timelineFilas, value))
                {
                    OnPropertyChanged(nameof(TieneTimeline));
                }
            }
        }

        public bool TieneTimeline => TimelineFilas.Count > 0;

        public ObservableCollection<KpiItem> KpisActuales
        {
            get => _kpisActuales;
            private set => SetProperty(ref _kpisActuales, value);
        }

        public string TimelineTitulo => NodoSeleccionado?.Tipo switch
        {
            DetalleNodoTipo.Facturado => "Facturado — fecha de factura vs. fecha de pago",
            DetalleNodoTipo.Operaciones => "Operaciones — finalizadas vs. pendientes",
            DetalleNodoTipo.Adeudo => "Adeudo — fecha de factura vs. tiempo transcurrido",
            DetalleNodoTipo.EquiposTodos => "Equipos — fechas de factura por equipo",
            DetalleNodoTipo.Equipo => $"Equipo {NodoSeleccionado?.EquipoIdentificador} — fechas de factura",
            _ => "Selecciona una rama del árbol para ver su timeline"
        };

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var clientesTask = _clienteService.GetClientesAsync();
                var reporteTask = _reporteFinancieroService.ObtenerReporteAsync(null, null, null, null, null);
                var cobranzaTask = _reporteFinancieroService.ObtenerReporteCobranzaAsync(null, null, null, null, null);

                await Task.WhenAll(clientesTask, reporteTask, cobranzaTask);

                _globalCobranza = cobranzaTask.Result;

                var clientesPorRfc = clientesTask.Result
                    .Where(c => !string.IsNullOrWhiteSpace(c.Rfc))
                    .GroupBy(c => c.Rfc.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // El listado se extrapola de las facturas (cabeceras del reporte financiero), no del catálogo
                // de clientes: si una factura trae un RFC que no existe (o no coincide) en el catálogo, igual
                // se muestra su card usando el nombre del receptor tal como aparece en la factura.
                var items = reporteTask.Result.Cabeceras
                    .Where(c => !string.IsNullOrWhiteSpace(c.ReceptorRfc))
                    .GroupBy(c => c.ReceptorRfc!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g =>
                    {
                        var rfc = g.Key;
                        var facturado = g.Sum(c => c.TotalFacturado);
                        var abonado = g.Sum(c => c.TotalAbonadoMovimientos);
                        var diasVencidoMax = g.Max(c => c.DiasVencidoMax);

                        var cliente = clientesPorRfc.TryGetValue(rfc, out var match)
                            ? match
                            : new CustomerDto { Rfc = rfc, RazonSocial = g.First().ReceptorNombreTexto };

                        return new ClienteSaludCardItem(cliente, ObtenerColorSalud(facturado, abonado, diasVencidoMax), facturado, abonado, diasVencidoMax);
                    })
                    .OrderBy(item => item.RazonSocial, StringComparer.OrdinalIgnoreCase);

                _todosLosClientes = items.ToList();
                AplicarFiltro();
                ActualizarKpis();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el listado de clientes: {ex.Message}";
                await _logger.LogErrorAsync("Error al cargar el listado de clientes", ex, nameof(DetalleClientesViewModel), nameof(InitializeAsync));
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task SeleccionarClienteAsync(CustomerDto cliente)
        {
            if (cliente == null)
            {
                return;
            }

            try
            {
                IsLoadingDetalle = true;
                ErrorMessage = null;
                ClienteSeleccionado = cliente;

                var reporteTask = _reporteFinancieroService.ObtenerReporteAsync(cliente.Rfc, null, null, null, null);
                var cobranzaTask = _reporteFinancieroService.ObtenerReporteCobranzaAsync(cliente.Rfc, null, null, null, null);

                // IdCliente == 0 significa "sin filtro" para el servicio de operaciones: solo se consulta
                // cuando el RFC de la factura sí tiene un cliente coincidente en el catálogo.
                var operacionesTask = cliente.IdCliente > 0
                    ? _operacionService.GetOperacionesAsync(new OperacionQueryDto
                    {
                        IdCliente = cliente.IdCliente,
                        IncluirFinalizadas = true
                    })
                    : Task.FromResult(new List<OperacionDto>());

                await Task.WhenAll(reporteTask, cobranzaTask, operacionesTask);

                var cabecera = reporteTask.Result.Cabeceras.FirstOrDefault();

                _clienteDetalles = reporteTask.Result.Detalles;
                _clienteOperaciones = operacionesTask.Result;
                _clienteCobranza = cobranzaTask.Result;
                _clienteCabecera = cabecera;

                var ramaFacturado = ConstruirRamaFacturado(cabecera, _clienteDetalles);
                var ramaOperaciones = ConstruirRamaOperaciones(_clienteOperaciones);
                var ramaAdeudo = ConstruirRamaAdeudo(cabecera, _clienteCobranza);
                var ramaEquipos = ConstruirRamaEquipos(_clienteCobranza, _clienteOperaciones);

                DetalleTreeItems = new ObservableCollection<DetalleClienteTreeItem>
                {
                    ramaFacturado,
                    ramaOperaciones,
                    ramaAdeudo,
                    ramaEquipos
                };

                // Auto-selecciona Facturado para que el director vea un timeline de inmediato.
                SeleccionarNodoDetalle(ramaFacturado);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el detalle del cliente: {ex.Message}";
                await _logger.LogErrorAsync("Error al cargar el detalle del cliente", ex, nameof(DetalleClientesViewModel), nameof(SeleccionarClienteAsync));
            }
            finally
            {
                IsLoadingDetalle = false;
            }
        }

        /// <summary>
        /// Construye el timeline correspondiente al nodo del árbol seleccionado (sin llamadas HTTP,
        /// usa los datos ya cacheados del cliente actual).
        /// </summary>
        public void SeleccionarNodoDetalle(DetalleClienteTreeItem? nodo)
        {
            NodoSeleccionado = nodo;

            TimelineFilas = nodo?.Tipo switch
            {
                DetalleNodoTipo.Facturado => ConstruirTimelineFacturado(),
                DetalleNodoTipo.Operaciones => ConstruirTimelineOperaciones(),
                DetalleNodoTipo.Adeudo => ConstruirTimelineAdeudo(),
                DetalleNodoTipo.EquiposTodos => ConstruirTimelineEquipos(null),
                DetalleNodoTipo.Equipo => ConstruirTimelineEquipos(nodo?.EquipoIdentificador),
                _ => new ObservableCollection<TimelineFila>()
            };

            ActualizarKpis();
        }

        /// <summary>
        /// Recalcula las KPIs mostradas en el row "Kpis": resumen de cartera completa cuando no hay
        /// cliente seleccionado, o contextual a la rama del árbol seleccionada del cliente actual.
        /// No dispara llamadas HTTP, usa datos ya cacheados.
        /// </summary>
        private void ActualizarKpis()
        {
            if (ClienteSeleccionado == null)
            {
                KpisActuales = ConstruirKpisCartera();
                return;
            }

            KpisActuales = NodoSeleccionado?.Tipo switch
            {
                DetalleNodoTipo.Operaciones => ConstruirKpisOperaciones(),
                DetalleNodoTipo.Adeudo => ConstruirKpisAdeudo(),
                DetalleNodoTipo.EquiposTodos => ConstruirKpisEquipos(null),
                DetalleNodoTipo.Equipo => ConstruirKpisEquipos(NodoSeleccionado?.EquipoIdentificador),
                _ => ConstruirKpisCliente()
            };
        }

        private ObservableCollection<KpiItem> ConstruirKpisCartera()
        {
            var facturadoTotal = _todosLosClientes.Sum(c => c.TotalFacturado);
            var abonadoTotal = _todosLosClientes.Sum(c => c.TotalAbonado);
            var saldo = facturadoTotal - abonadoTotal;

            var sanos = _todosLosClientes.Count(c => c.TotalFacturado <= 0 || (c.TotalAbonado / c.TotalFacturado) >= 0.5m);
            var porcentajeSanos = _todosLosClientes.Count > 0 ? (double)sanos / _todosLosClientes.Count * 100 : 0;

            var diasVencidoMaxCartera = _todosLosClientes.Count > 0 ? _todosLosClientes.Max(c => c.DiasVencidoMax) : 0;
            var vencidas90 = _globalCobranza.Count(c => c.DiasVencido >= 90);

            return new ObservableCollection<KpiItem>
            {
                new KpiItem("Saldo total de cartera", FormatearMoneda(saldo), ObtenerColorSalud(facturadoTotal, abonadoTotal, diasVencidoMaxCartera)),
                new KpiItem("Clientes en zona sana", $"{porcentajeSanos:0}%"),
                new KpiItem("Facturas vencidas +90 días", vencidas90.ToString())
            };
        }

        private ObservableCollection<KpiItem> ConstruirKpisCliente()
        {
            var facturado = _clienteCabecera?.TotalFacturado ?? 0m;
            var abonado = _clienteCabecera?.TotalAbonadoMovimientos ?? 0m;
            var saldo = facturado - abonado;
            var porcentajeCobrado = facturado > 0 ? (double)(abonado / facturado) * 100 : 100;
            var operacionesAbiertas = _clienteOperaciones.Count(o => o.FechaFinal == null && !o.TFinalizado);

            return new ObservableCollection<KpiItem>
            {
                new KpiItem("Total facturado", FormatearMoneda(facturado)),
                new KpiItem("Saldo pendiente", FormatearMoneda(saldo), ObtenerColorSalud(facturado, abonado, _clienteCabecera?.DiasVencidoMax ?? 0)),
                new KpiItem("% cobrado", $"{porcentajeCobrado:0}%"),
                new KpiItem("Operaciones abiertas", operacionesAbiertas.ToString())
            };
        }

        private ObservableCollection<KpiItem> ConstruirKpisOperaciones()
        {
            var abiertas = _clienteOperaciones.Count(o => o.FechaFinal == null && !o.TFinalizado);
            var pendientesCierre = _clienteOperaciones.Count(o => o.FechaFinal == null && o.TFinalizado);
            var hoy = DateTime.Today;
            var diasAbiertas = _clienteOperaciones
                .Where(o => o.FechaFinal == null && o.FechaInicio.HasValue)
                .Select(o => (hoy - o.FechaInicio!.Value).TotalDays)
                .ToList();
            var promedioDias = diasAbiertas.Count > 0 ? diasAbiertas.Average() : 0;

            return new ObservableCollection<KpiItem>
            {
                new KpiItem("Abiertas", abiertas.ToString(), ColorSaludAlta),
                new KpiItem("Pendientes de cierre", pendientesCierre.ToString(), ColorSaludMedia),
                new KpiItem("Tiempo promedio abierto", $"{promedioDias:0} días")
            };
        }

        private ObservableCollection<KpiItem> ConstruirKpisAdeudo()
        {
            var hoy = DateTime.Today;
            var pendientes = _clienteCobranza.Where(c => !c.Pagada && !c.EsSinFacturar).ToList();
            var saldo = (_clienteCabecera?.TotalFacturado ?? 0m) - (_clienteCabecera?.TotalAbonadoMovimientos ?? 0m);
            var antiguedadPromedio = pendientes.Count > 0 ? pendientes.Average(c => (hoy - c.FechaFactura).TotalDays) : 0;
            var monto90 = pendientes.Where(c => c.DiasVencido >= 90).Sum(c => c.Total);

            return new ObservableCollection<KpiItem>
            {
                new KpiItem("Saldo pendiente", FormatearMoneda(saldo), ObtenerColorSalud(_clienteCabecera?.TotalFacturado ?? 0m, _clienteCabecera?.TotalAbonadoMovimientos ?? 0m, _clienteCabecera?.DiasVencidoMax ?? 0)),
                new KpiItem("Facturas pendientes", pendientes.Count.ToString()),
                new KpiItem("Antigüedad promedio", $"{antiguedadPromedio:0} días"),
                new KpiItem("Monto +90 días", FormatearMoneda(monto90), monto90 > 0 ? ColorSaludBaja : ColorSaludAlta)
            };
        }

        private ObservableCollection<KpiItem> ConstruirKpisEquipos(string? identificadorFiltro)
        {
            IEnumerable<string> identificadores = _clienteOperaciones.Select(o => o.Identificador)
                .Concat(_clienteCobranza.Select(c => c.OperacionIdentificador))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(identificadorFiltro))
            {
                var facturasEquipo = _clienteCobranza
                    .Where(c => !c.EsSinFacturar && string.Equals(c.OperacionIdentificador, identificadorFiltro, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var total = facturasEquipo.Sum(c => c.Total);
                var ultima = facturasEquipo.Count > 0 ? facturasEquipo.Max(c => c.FechaFactura) : (DateTime?)null;

                return new ObservableCollection<KpiItem>
                {
                    new KpiItem("Facturas de este equipo", facturasEquipo.Count.ToString()),
                    new KpiItem("Total facturado", FormatearMoneda(total)),
                    new KpiItem("Última factura", ultima.HasValue ? ultima.Value.ToString("dd/MM/yyyy") : "Sin facturas")
                };
            }

            var todasFacturasEquipos = _clienteCobranza.Where(c => !c.EsSinFacturar && !string.IsNullOrWhiteSpace(c.OperacionIdentificador)).ToList();
            var equipoTop = todasFacturasEquipos
                .GroupBy(c => c.OperacionIdentificador!.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return new ObservableCollection<KpiItem>
            {
                new KpiItem("Equipos con actividad", identificadores.Count().ToString()),
                new KpiItem("Equipo con más facturas", equipoTop != null ? $"{equipoTop.Key} ({equipoTop.Count()})" : "N/A"),
                new KpiItem("Total facturado a equipos", FormatearMoneda(todasFacturasEquipos.Sum(c => c.Total)))
            };
        }

        private static DetalleClienteTreeItem ConstruirRamaFacturado(
            ReporteFinancieroFacturacionCabeceraDto? cabecera,
            List<ReporteFinancieroFacturacionDetalleDto> detalles)
        {
            var totalFacturado = cabecera?.TotalFacturado ?? 0m;
            var raiz = new DetalleClienteTreeItem("Facturado", FormatearMoneda(totalFacturado));

            raiz.Hijos.Add(new DetalleClienteTreeItem("Facturas finiquitadas", (cabecera?.NumeroFacturasFiniquitadas ?? 0).ToString(), nivel: 1));
            raiz.Hijos.Add(new DetalleClienteTreeItem("Facturas no finiquitadas", (cabecera?.NumeroFacturasNoFiniquitadas ?? 0).ToString(), nivel: 1));

            var historial = new DetalleClienteTreeItem("Historial de facturas", detalles.Count.ToString(), nivel: 1);
            foreach (var factura in detalles.OrderByDescending(f => f.FechaTimbrado))
            {
                historial.Hijos.Add(new DetalleClienteTreeItem(factura.FolioTexto, $"{factura.TotalTexto} · {factura.FechaTimbradoTexto}", nivel: 2));
            }
            raiz.Hijos.Add(historial);

            raiz.EtiquetarTipo(DetalleNodoTipo.Facturado);
            return raiz;
        }

        private static DetalleClienteTreeItem ConstruirRamaOperaciones(List<OperacionDto> operaciones)
        {
            var abiertas = operaciones.Where(o => o.FechaFinal == null && !o.TFinalizado).ToList();
            var pendientesCierre = operaciones.Where(o => o.FechaFinal == null && o.TFinalizado).ToList();
            var finalizadas = operaciones.Where(o => o.FechaFinal != null).ToList();

            var raiz = new DetalleClienteTreeItem("Operaciones", operaciones.Count.ToString());

            raiz.Hijos.Add(ConstruirGrupoOperaciones("Abiertas", abiertas, ColorSaludAlta));
            raiz.Hijos.Add(ConstruirGrupoOperaciones("Trabajo finalizado, pendiente de cierre", pendientesCierre, ColorSaludMedia));
            raiz.Hijos.Add(ConstruirGrupoOperaciones("Finalizadas", finalizadas, ColorGris));

            raiz.EtiquetarTipo(DetalleNodoTipo.Operaciones);
            return raiz;
        }

        private static DetalleClienteTreeItem ConstruirGrupoOperaciones(string etiqueta, List<OperacionDto> operaciones, Color color)
        {
            var grupo = new DetalleClienteTreeItem($"{etiqueta}: {operaciones.Count}", null, color, nivel: 1);
            foreach (var operacion in operaciones.OrderByDescending(o => o.FechaInicio))
            {
                var detalleTexto = $"{operacion.TipoMantenimiento} · {operacion.Atiende} · {operacion.FechaInicioCorta}";
                grupo.Hijos.Add(new DetalleClienteTreeItem(operacion.Identificador ?? "Sin identificador", detalleTexto, nivel: 2));
            }
            return grupo;
        }

        private static DetalleClienteTreeItem ConstruirRamaAdeudo(
            ReporteFinancieroFacturacionCabeceraDto? cabecera,
            List<ReporteCobranzaItemDto> cobranza)
        {
            var totalFacturado = cabecera?.TotalFacturado ?? 0m;
            var totalAbonado = cabecera?.TotalAbonadoMovimientos ?? 0m;
            var saldo = totalFacturado - totalAbonado;

            var raiz = new DetalleClienteTreeItem("Adeudo", FormatearMoneda(saldo), ObtenerColorSalud(totalFacturado, totalAbonado, cabecera?.DiasVencidoMax ?? 0));

            var pendientes = cobranza.Where(c => !c.Pagada).OrderByDescending(c => c.FechaFactura).ToList();
            var facturasPendientes = new DetalleClienteTreeItem("Facturas sin pagar", pendientes.Count.ToString(), nivel: 1);
            foreach (var factura in pendientes)
            {
                facturasPendientes.Hijos.Add(new DetalleClienteTreeItem(factura.FolioTexto, $"{factura.TotalTexto} · {factura.FechaFacturaTexto} · {factura.DiasVencidoTexto}", nivel: 2));
            }
            raiz.Hijos.Add(facturasPendientes);

            raiz.Hijos.Add(new DetalleClienteTreeItem("Total abonado", FormatearMoneda(totalAbonado), nivel: 1));

            raiz.EtiquetarTipo(DetalleNodoTipo.Adeudo);
            return raiz;
        }

        /// <summary>
        /// Los "equipos del cliente" se derivan de los identificadores de equipo presentes en sus
        /// operaciones y facturas ya cargadas — no existe un endpoint que devuelva equipos por
        /// cliente (fn_relaciones_equipo_cliente_gestionar no expone el identificador del equipo
        /// al filtrar por idCliente), y esta derivación además solo trae equipos con actividad real.
        /// </summary>
        private static DetalleClienteTreeItem ConstruirRamaEquipos(List<ReporteCobranzaItemDto> cobranza, List<OperacionDto> operaciones)
        {
            var identificadores = operaciones.Select(o => o.Identificador)
                .Concat(cobranza.Select(c => c.OperacionIdentificador))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var raiz = new DetalleClienteTreeItem("Equipos", identificadores.Count.ToString());

            foreach (var identificador in identificadores)
            {
                var numFacturas = cobranza.Count(c =>
                    !c.EsSinFacturar &&
                    string.Equals(c.OperacionIdentificador, identificador, StringComparison.OrdinalIgnoreCase));

                var hijo = new DetalleClienteTreeItem(identificador, $"{numFacturas} factura(s)", nivel: 1)
                {
                    Tipo = DetalleNodoTipo.Equipo,
                    EquipoIdentificador = identificador
                };
                raiz.Hijos.Add(hijo);
            }

            raiz.Tipo = DetalleNodoTipo.EquiposTodos;
            return raiz;
        }

        private ObservableCollection<TimelineFila> ConstruirTimelineFacturado()
        {
            var filas = _clienteDetalles
                .Where(f => f.FechaTimbrado.HasValue)
                .OrderByDescending(f => f.FechaTimbrado)
                .Select(factura =>
                {
                    var inicio = factura.FechaTimbrado!.Value;
                    var fin = factura.Abonos.Count > 0
                        ? factura.Abonos.Max(a => a.FechaAbono)
                        : DateTime.Today;
                    var color = factura.Finiquito == true ? ColorSaludAlta : ColorSaludMedia;

                    return new TimelineFila(factura.FolioTexto, HexDe(color), inicio, fin < inicio ? inicio : fin);
                });

            return new ObservableCollection<TimelineFila>(filas);
        }

        private ObservableCollection<TimelineFila> ConstruirTimelineOperaciones()
        {
            var filas = _clienteOperaciones
                .Where(o => o.FechaInicio.HasValue)
                .OrderByDescending(o => o.FechaInicio)
                .Select(operacion =>
                {
                    var inicio = operacion.FechaInicio!.Value;
                    var fin = operacion.FechaFinal ?? DateTime.Today;
                    var color = operacion.FechaFinal != null
                        ? ColorGris
                        : (operacion.TFinalizado ? ColorSaludMedia : ColorSaludAlta);
                    var etiqueta = $"{operacion.Identificador ?? "Sin equipo"} · {operacion.TipoMantenimiento}";

                    return new TimelineFila(etiqueta, HexDe(color), inicio, fin);
                });

            return new ObservableCollection<TimelineFila>(filas);
        }

        private ObservableCollection<TimelineFila> ConstruirTimelineAdeudo()
        {
            var hoy = DateTime.Today;

            var filas = _clienteCobranza
                .Where(c => !c.Pagada && c.FechaFactura != default)
                .OrderByDescending(c => c.FechaFactura)
                .Select(factura =>
                {
                    // El trabajo finalizado sin facturar aún no es adeudo real (no hay factura que cobrar),
                    // así que se marca con un color de advertencia fijo en vez del degradado de antigüedad
                    // rojo, que solo aplica a facturas ya emitidas y pendientes de pago.
                    var color = factura.EsSinFacturar
                        ? ColorSaludMedia
                        : InterpolarColor(ColorSaludAlta, ColorSaludBaja, Math.Clamp(factura.DiasVencido / 90.0, 0.0, 1.0));

                    return new TimelineFila(factura.FolioTexto, HexDe(color), factura.FechaFactura, hoy);
                });

            return new ObservableCollection<TimelineFila>(filas);
        }

        private ObservableCollection<TimelineFila> ConstruirTimelineEquipos(string? identificadorFiltro)
        {
            IEnumerable<string> identificadores = _clienteOperaciones.Select(o => o.Identificador)
                .Concat(_clienteCobranza.Select(c => c.OperacionIdentificador))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(identificadorFiltro))
            {
                identificadores = identificadores.Where(id => string.Equals(id, identificadorFiltro, StringComparison.OrdinalIgnoreCase));
            }

            var filas = identificadores.Select(identificador =>
            {
                var fechas = _clienteCobranza
                    .Where(c => !c.EsSinFacturar && string.Equals(c.OperacionIdentificador, identificador, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.FechaFactura)
                    .Where(f => f != default)
                    .OrderBy(f => f)
                    .ToList();

                return new TimelineFila(identificador, ColorEquipoHex, puntos: fechas);
            });

            return new ObservableCollection<TimelineFila>(filas);
        }

        /// <summary>
        /// Filtra el listado de clientes por coincidencia parcial en RFC o razón social (búsqueda en vivo).
        /// </summary>
        private void AplicarFiltro()
        {
            var texto = _busquedaTexto.Trim();

            var filtrados = string.IsNullOrEmpty(texto)
                ? _todosLosClientes
                : _todosLosClientes.Where(c =>
                    c.RazonSocial.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    c.Rfc.Contains(texto, StringComparison.OrdinalIgnoreCase));

            Clientes = new ObservableCollection<ClienteSaludCardItem>(filtrados);
        }

        private static string FormatearMoneda(decimal monto) => monto.ToString("C2", new System.Globalization.CultureInfo("es-MX"));

        private static string HexDe(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        /// <summary>
        /// Color de salud de pago en degradado rojo-amarillo-verde según la proporción abonado/facturado.
        /// Sin facturación (cero deuda) se considera salud máxima (verde). Si hay alguna factura vencida
        /// 90+ días, el vencimiento manda y el color va directo a rojo sin importar la proporción.
        /// </summary>
        private static Color ObtenerColorSalud(decimal totalFacturado, decimal totalAbonado, int diasVencidoMax = 0)
        {
            if (diasVencidoMax > 90)
            {
                return ColorSaludBaja;
            }

            var proporcion = totalFacturado <= 0
                ? 1.0
                : Math.Clamp((double)(totalAbonado / totalFacturado), 0.0, 1.0);

            return proporcion <= 0.5
                ? InterpolarColor(ColorSaludBaja, ColorSaludMedia, proporcion / 0.5)
                : InterpolarColor(ColorSaludMedia, ColorSaludAlta, (proporcion - 0.5) / 0.5);
        }

        private static Color InterpolarColor(Color desde, Color hasta, double t)
        {
            byte Interpolar(byte a, byte b) => (byte)Math.Round(a + (b - a) * t);

            return Color.FromArgb(
                0xFF,
                Interpolar(desde.R, hasta.R),
                Interpolar(desde.G, hasta.G),
                Interpolar(desde.B, hasta.B));
        }
    }

    public class ClienteSaludCardItem
    {
        public ClienteSaludCardItem(CustomerDto cliente, Color colorSalud, decimal totalFacturado, decimal totalAbonado, int diasVencidoMax = 0)
        {
            Cliente = cliente;
            ColorSaludBrush = new SolidColorBrush(colorSalud);
            TotalFacturado = totalFacturado;
            TotalAbonado = totalAbonado;
            DiasVencidoMax = diasVencidoMax;
        }

        public CustomerDto Cliente { get; }

        public int IdCliente => Cliente.IdCliente;

        public string RazonSocial => Cliente.RazonSocial;

        public string Rfc => Cliente.Rfc;

        public SolidColorBrush ColorSaludBrush { get; }

        public decimal TotalFacturado { get; }

        public decimal TotalAbonado { get; }

        public int DiasVencidoMax { get; }
    }
}
