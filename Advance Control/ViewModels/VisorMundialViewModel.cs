using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Areas;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.RelacionUsuarioArea;
using Advance_Control.Services.Reportes;
using Advance_Control.Services.VisorMundial;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Advance_Control.ViewModels
{
    public enum VisorMundialModo
    {
        Ubicaciones = 0,
        ColaboradoresPorArea = 1
    }

    /// <summary>Qué se está mostrando actualmente en el panel derecho (detalle de un clic de pin).</summary>
    public enum VisorMundialPanelSeleccion
    {
        Ninguno = 0,
        Ubicacion = 1,
        Colaborador = 2
    }

    public class VisorMundialViewModel : ViewModelBase
    {
        private readonly IGoogleMapsConfigService _googleMapsConfigService;
        private readonly Services.VisorMundial.IVisorMundialService _visorMundialService;
        private readonly IClienteService _clienteService;
        private readonly IEquipoService _equipoService;
        private readonly IContactoService _contactoService;
        private readonly IReporteFinancieroFacturacionService _reporteFinancieroService;
        private readonly IAreasService _areasService;
        private readonly IRelacionUsuarioAreaService _relacionUsuarioAreaService;
        private readonly IOperacionService _operacionService;

        private GoogleMapsConfigDto? _mapsConfig;
        private ObservableCollection<VisorMundialUbicacionDto> _ubicaciones;
        private ObservableCollection<CustomerDto> _clientes;
        private ObservableCollection<EquipoDto> _equipos;
        private ObservableCollection<ContactoDto> _tecnicos;
        private ObservableCollection<ReporteFinancieroFacturacionCabeceraDto> _topClientesSaldo;
        private ObservableCollection<VisorMundialEquipoDto> _equiposUbicacionSeleccionada;
        private ObservableCollection<VisorMundialEquipoCardItem> _equipoCards;
        private ObservableCollection<VisorMundialColaboradorPuntoDto> _colaboradoresPuntos;
        private VisorMundialUbicacionDto? _ubicacionSeleccionada;
        private VisorMundialColaboradorPuntoDto? _colaboradorSeleccionado;
        private ObservableCollection<DetalleClienteTreeItem> _colaboradorTreeItems;
        private VisorMundialPanelSeleccion _panelSeleccion;
        private Dictionary<string, (decimal Facturado, decimal Abonado, int DiasVencidoMax)> _cobranzaPorRfc = new(StringComparer.OrdinalIgnoreCase);
        private bool _isLoading;
        private bool _isLoadingDetalle;
        private string? _errorMessage;
        private bool _isMapInitialized;
        private bool _isPanelOpen = true;
        private int _modoIndex;
        private bool _colaboradoresDataLoaded;
        private decimal _totalFacturadoGlobal;
        private decimal _totalAbonadoGlobal;

        public VisorMundialViewModel(
            IGoogleMapsConfigService googleMapsConfigService,
            Services.VisorMundial.IVisorMundialService visorMundialService,
            IClienteService clienteService,
            IEquipoService equipoService,
            IContactoService contactoService,
            IReporteFinancieroFacturacionService reporteFinancieroService,
            IAreasService areasService,
            IRelacionUsuarioAreaService relacionUsuarioAreaService,
            IOperacionService operacionService)
        {
            _googleMapsConfigService = googleMapsConfigService ?? throw new ArgumentNullException(nameof(googleMapsConfigService));
            _visorMundialService = visorMundialService ?? throw new ArgumentNullException(nameof(visorMundialService));
            _clienteService = clienteService ?? throw new ArgumentNullException(nameof(clienteService));
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _contactoService = contactoService ?? throw new ArgumentNullException(nameof(contactoService));
            _reporteFinancieroService = reporteFinancieroService ?? throw new ArgumentNullException(nameof(reporteFinancieroService));
            _areasService = areasService ?? throw new ArgumentNullException(nameof(areasService));
            _relacionUsuarioAreaService = relacionUsuarioAreaService ?? throw new ArgumentNullException(nameof(relacionUsuarioAreaService));
            _operacionService = operacionService ?? throw new ArgumentNullException(nameof(operacionService));

            _ubicaciones = new ObservableCollection<VisorMundialUbicacionDto>();
            _clientes = new ObservableCollection<CustomerDto>();
            _equipos = new ObservableCollection<EquipoDto>();
            _tecnicos = new ObservableCollection<ContactoDto>();
            _topClientesSaldo = new ObservableCollection<ReporteFinancieroFacturacionCabeceraDto>();
            _equiposUbicacionSeleccionada = new ObservableCollection<VisorMundialEquipoDto>();
            _equipoCards = new ObservableCollection<VisorMundialEquipoCardItem>();
            _colaboradoresPuntos = new ObservableCollection<VisorMundialColaboradorPuntoDto>();
            _colaboradorTreeItems = new ObservableCollection<DetalleClienteTreeItem>();
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

        public ObservableCollection<VisorMundialEquipoCardItem> EquipoCards
        {
            get => _equipoCards;
            set => SetProperty(ref _equipoCards, value);
        }

        public VisorMundialColaboradorPuntoDto? ColaboradorSeleccionado
        {
            get => _colaboradorSeleccionado;
            set => SetProperty(ref _colaboradorSeleccionado, value);
        }

        public ObservableCollection<DetalleClienteTreeItem> ColaboradorTreeItems
        {
            get => _colaboradorTreeItems;
            set => SetProperty(ref _colaboradorTreeItems, value);
        }

        public VisorMundialPanelSeleccion PanelSeleccion
        {
            get => _panelSeleccion;
            set
            {
                if (SetProperty(ref _panelSeleccion, value))
                {
                    OnPropertyChanged(nameof(MostrandoUbicacion));
                    OnPropertyChanged(nameof(MostrandoColaborador));
                    OnPropertyChanged(nameof(SinSeleccion));
                }
            }
        }

        public bool MostrandoUbicacion => PanelSeleccion == VisorMundialPanelSeleccion.Ubicacion;
        public bool MostrandoColaborador => PanelSeleccion == VisorMundialPanelSeleccion.Colaborador;
        public bool SinSeleccion => PanelSeleccion == VisorMundialPanelSeleccion.Ninguno;

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

        /// <summary>0 = Ubicaciones, 1 = Colaboradores por área. Respaldo del ComboBox de modo.</summary>
        public int ModoIndex
        {
            get => _modoIndex;
            set
            {
                if (SetProperty(ref _modoIndex, value))
                {
                    OnPropertyChanged(nameof(Modo));
                    OnPropertyChanged(nameof(ResumenUbicaciones));
                }
            }
        }

        public VisorMundialModo Modo => (VisorMundialModo)_modoIndex;

        public ObservableCollection<VisorMundialColaboradorPuntoDto> ColaboradoresPuntos
        {
            get => _colaboradoresPuntos;
            set => SetProperty(ref _colaboradoresPuntos, value);
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

        public string ResumenUbicaciones => Modo == VisorMundialModo.ColaboradoresPorArea
            ? $"{ColaboradoresPuntos.Count} colaborador(es) en el mapa"
            : $"{Ubicaciones.Count} ubicación(es) en el mapa";

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

            _cobranzaPorRfc = cabeceras
                .Where(c => !string.IsNullOrWhiteSpace(c.ReceptorRfc))
                .GroupBy(c => c.ReceptorRfc!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => (Facturado: g.Sum(c => c.TotalFacturado), Abonado: g.Sum(c => c.TotalAbonadoMovimientos), DiasVencidoMax: g.Max(c => c.DiasVencidoMax)),
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
        /// Carga (una sola vez, salvo forceReload) los colaboradores agrupados por área,
        /// con sus posiciones ya calculadas en abanico para que no se solapen los pines
        /// de quienes comparten área.
        /// </summary>
        public async Task EnsureColaboradoresDataAsync(bool forceReload = false)
        {
            if (_colaboradoresDataLoaded && !forceReload)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var areas = await _areasService.GetAreasAsync(activo: true);
                var areasConCentro = areas
                    .Where(a => a.CentroLatitud.HasValue && a.CentroLongitud.HasValue)
                    .ToList();

                var relacionesTasks = areasConCentro
                    .Select(a => _relacionUsuarioAreaService.GetRelacionesPorAreaAsync(a.IdArea))
                    .ToList();
                await Task.WhenAll(relacionesTasks);

                var contactosPorCredencial = Tecnicos
                    .Where(c => c.CredencialId.HasValue)
                    .GroupBy(c => c.CredencialId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var puntos = new List<VisorMundialColaboradorPuntoDto>();
                for (var i = 0; i < areasConCentro.Count; i++)
                {
                    var area = areasConCentro[i];
                    var personasEnArea = relacionesTasks[i].Result
                        .Where(r => r.Activo && contactosPorCredencial.ContainsKey(r.CredencialId))
                        .Select(r => contactosPorCredencial[r.CredencialId])
                        .Distinct()
                        .ToList();

                    puntos.AddRange(CalcularPuntosFan(personasEnArea, area));
                }

                ColaboradoresPuntos = new ObservableCollection<VisorMundialColaboradorPuntoDto>(puntos);
                _colaboradoresDataLoaded = true;
                OnPropertyChanged(nameof(ResumenUbicaciones));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar colaboradores por área: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private const double MetrosPorGradoLat = 111_320.0;

        /// <summary>
        /// Calcula la posición final de cada colaborador de un área: si hay uno solo va exactamente
        /// en el centro del área; si hay varios se reparten en abanico (círculo) alrededor del centro
        /// para que sus pines no se encimen.
        /// </summary>
        private static IEnumerable<VisorMundialColaboradorPuntoDto> CalcularPuntosFan(List<ContactoDto> personas, AreaDto area)
        {
            var centroLat = (double)area.CentroLatitud!.Value;
            var centroLng = (double)area.CentroLongitud!.Value;
            var n = personas.Count;

            if (n == 0)
            {
                yield break;
            }

            if (n == 1)
            {
                yield return BuildPuntoColaborador(personas[0], area, centroLat, centroLng);
                yield break;
            }

            // El radio crece un poco con la cantidad de personas para que los pines no se toquen,
            // pero se limita para no derivar hacia el territorio de un área vecina.
            var radioMetros = Math.Clamp(8.0 * n, 20.0, 60.0);
            var metrosPorGradoLng = MetrosPorGradoLat * Math.Cos(centroLat * Math.PI / 180.0);

            for (var i = 0; i < n; i++)
            {
                var anguloRad = (2 * Math.PI / n) * i;
                var deltaLat = (radioMetros * Math.Cos(anguloRad)) / MetrosPorGradoLat;
                var deltaLng = (radioMetros * Math.Sin(anguloRad)) / metrosPorGradoLng;

                yield return BuildPuntoColaborador(personas[i], area, centroLat + deltaLat, centroLng + deltaLng);
            }
        }

        private static VisorMundialColaboradorPuntoDto BuildPuntoColaborador(ContactoDto contacto, AreaDto area, double lat, double lng)
        {
            var inicial1 = !string.IsNullOrEmpty(contacto.Nombre) ? contacto.Nombre[0].ToString() : string.Empty;
            var inicial2 = !string.IsNullOrEmpty(contacto.Apellido) ? contacto.Apellido[0].ToString() : string.Empty;

            return new VisorMundialColaboradorPuntoDto
            {
                CredencialId = contacto.CredencialId ?? 0,
                NombreCompleto = contacto.NombreCompleto,
                Cargo = contacto.Cargo,
                Glyph = (inicial1 + inicial2).ToUpperInvariant(),
                IdArea = area.IdArea,
                NombreArea = area.Nombre,
                Latitud = (decimal)lat,
                Longitud = (decimal)lng
            };
        }

        // Mismo degradado rojo-amarillo-verde que DetalleClientesViewModel.ObtenerColorSalud,
        // para que el color de un pin coincida con el de la tarjeta del cliente en "Detalle clientes".
        private static readonly (byte R, byte G, byte B) ColorSaludBaja = (0xEA, 0x43, 0x35); // rojo
        private static readonly (byte R, byte G, byte B) ColorSaludMedia = (0xFB, 0xBC, 0x04); // amarillo
        private static readonly (byte R, byte G, byte B) ColorSaludAlta = (0x34, 0xA8, 0x53); // verde

        /// <summary>
        /// Color hexadecimal para una ubicación según la salud de cobranza (abonado/facturado) de sus clientes.
        /// </summary>
        public string ObtenerColorUbicacion(VisorMundialUbicacionDto ubicacion)
        {
            var rfcs = ubicacion.ClientesRfcList;
            if (rfcs.Count == 0)
            {
                return "#9AA0A6"; // gris: sin cliente asociado
            }

            var facturado = 0m;
            var abonado = 0m;
            var diasVencidoMax = 0;
            foreach (var rfc in rfcs)
            {
                if (_cobranzaPorRfc.TryGetValue(rfc, out var totales))
                {
                    facturado += totales.Facturado;
                    abonado += totales.Abonado;
                    diasVencidoMax = Math.Max(diasVencidoMax, totales.DiasVencidoMax);
                }
            }

            return ObtenerColorSalud(facturado, abonado, diasVencidoMax);
        }

        /// <summary>
        /// Mismo criterio que DetalleClientesViewModel.ObtenerColorSalud: si hay alguna factura
        /// vencida 90+ días, el vencimiento manda y el color va directo a rojo.
        /// </summary>
        private static string ObtenerColorSalud(decimal totalFacturado, decimal totalAbonado, int diasVencidoMax = 0)
        {
            if (diasVencidoMax > 90)
            {
                return $"#{ColorSaludBaja.R:X2}{ColorSaludBaja.G:X2}{ColorSaludBaja.B:X2}";
            }

            var proporcion = totalFacturado <= 0
                ? 1.0
                : Math.Clamp((double)(totalAbonado / totalFacturado), 0.0, 1.0);

            return proporcion <= 0.5
                ? InterpolarColorHex(ColorSaludBaja, ColorSaludMedia, proporcion / 0.5)
                : InterpolarColorHex(ColorSaludMedia, ColorSaludAlta, (proporcion - 0.5) / 0.5);
        }

        private static string InterpolarColorHex((byte R, byte G, byte B) desde, (byte R, byte G, byte B) hasta, double t)
        {
            byte Interpolar(byte a, byte b) => (byte)Math.Round(a + (b - a) * t);
            return $"#{Interpolar(desde.R, hasta.R):X2}{Interpolar(desde.G, hasta.G):X2}{Interpolar(desde.B, hasta.B):X2}";
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
                ColaboradorSeleccionado = null;
                PanelSeleccion = VisorMundialPanelSeleccion.Ubicacion;
                IsPanelOpen = true;

                var equipos = await _visorMundialService.ObtenerEquiposPorUbicacionAsync(ubicacion.IdUbicacion);
                EquiposUbicacionSeleccionada = new ObservableCollection<VisorMundialEquipoDto>(equipos);

                var cards = await Task.WhenAll(equipos.Select(ConstruirEquipoCardAsync));
                EquipoCards = new ObservableCollection<VisorMundialEquipoCardItem>(cards);
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

        /// <summary>
        /// Arma la tarjeta de un equipo: sus facturas propias (vía operaciones.id_equipo, nunca por
        /// RFC de cliente porque un mismo equipo puede tener varios clientes ligados), sus operaciones,
        /// y el color de salud calculado con el mismo criterio que el resto de la app.
        /// </summary>
        private async Task<VisorMundialEquipoCardItem> ConstruirEquipoCardAsync(VisorMundialEquipoDto equipo)
        {
            List<VisorMundialEquipoFacturaDto> facturas;
            List<OperacionDto> operaciones;
            try
            {
                var facturasTask = _visorMundialService.ObtenerFacturasPorEquipoAsync(equipo.IdEquipo);
                var operacionesTask = _operacionService.GetOperacionesAsync(new OperacionQueryDto { IdEquipo = equipo.IdEquipo });
                await Task.WhenAll(facturasTask, operacionesTask);
                facturas = facturasTask.Result;
                operaciones = operacionesTask.Result;
            }
            catch (Exception)
            {
                facturas = new List<VisorMundialEquipoFacturaDto>();
                operaciones = new List<OperacionDto>();
            }

            var totalFacturado = facturas.Sum(f => f.Total);
            var totalAbonado = facturas.Sum(f => f.Abono);
            var diasVencidoMax = facturas.Count == 0 ? 0 : facturas.Max(f => f.DiasVencido);
            var color = ColorFromHex(ObtenerColorSalud(totalFacturado, totalAbonado, diasVencidoMax));

            var treeItems = new ObservableCollection<DetalleClienteTreeItem>
            {
                ConstruirRamaFacturadoEquipo(facturas),
                ConstruirRamaOperacionesEquipo(operaciones),
                ConstruirRamaAdeudoEquipo(facturas)
            };

            return new VisorMundialEquipoCardItem(equipo, color, treeItems, totalFacturado, totalAbonado, diasVencidoMax);
        }

        private static DetalleClienteTreeItem ConstruirRamaFacturadoEquipo(List<VisorMundialEquipoFacturaDto> facturas)
        {
            var raiz = new DetalleClienteTreeItem("Facturado", facturas.Count.ToString());
            foreach (var factura in facturas.OrderByDescending(f => f.FechaTimbrado))
            {
                raiz.Hijos.Add(new DetalleClienteTreeItem(factura.FolioTexto, $"{factura.TotalTexto} · {factura.FechaTimbradoTexto} · {factura.EstadoTexto}", nivel: 1)
                {
                    IdFactura = factura.IdFactura,
                    IdOperacion = factura.IdOperacion
                });
            }
            return raiz;
        }

        private static DetalleClienteTreeItem ConstruirRamaAdeudoEquipo(List<VisorMundialEquipoFacturaDto> facturas)
        {
            var pendientes = facturas.Where(f => !f.Finiquito).OrderByDescending(f => f.FechaTimbrado).ToList();
            var raiz = new DetalleClienteTreeItem("Adeudo", pendientes.Count.ToString());
            foreach (var factura in pendientes)
            {
                raiz.Hijos.Add(new DetalleClienteTreeItem(factura.FolioTexto, $"{factura.TotalTexto} · {factura.DiasVencidoTexto}", nivel: 1)
                {
                    IdFactura = factura.IdFactura,
                    IdOperacion = factura.IdOperacion
                });
            }
            return raiz;
        }

        private static DetalleClienteTreeItem ConstruirRamaOperacionesEquipo(List<OperacionDto> operaciones)
        {
            var abiertas = operaciones.Where(o => o.FechaFinal == null && !o.TFinalizado).ToList();
            var pendientesCierre = operaciones.Where(o => o.FechaFinal == null && o.TFinalizado).ToList();
            var finalizadas = operaciones.Where(o => o.FechaFinal != null).ToList();

            var raiz = new DetalleClienteTreeItem("Operaciones", operaciones.Count.ToString());
            raiz.Hijos.Add(ConstruirGrupoOperacionesEquipo("Abiertas", abiertas));
            raiz.Hijos.Add(ConstruirGrupoOperacionesEquipo("Trabajo finalizado, pendiente de cierre", pendientesCierre));
            raiz.Hijos.Add(ConstruirGrupoOperacionesEquipo("Finalizadas", finalizadas));
            return raiz;
        }

        private static DetalleClienteTreeItem ConstruirGrupoOperacionesEquipo(string etiqueta, List<OperacionDto> operaciones)
        {
            var grupo = new DetalleClienteTreeItem($"{etiqueta}: {operaciones.Count}", null, nivel: 1);
            foreach (var operacion in operaciones.OrderByDescending(o => o.FechaInicio))
            {
                grupo.Hijos.Add(new DetalleClienteTreeItem($"{operacion.TipoMantenimiento} · {operacion.Atiende}", operacion.FechaInicioCorta, nivel: 2)
                {
                    IdOperacion = operacion.IdOperacion
                });
            }
            return grupo;
        }

        private static Color ColorFromHex(string hex)
        {
            hex = hex.TrimStart('#');
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(0xFF, r, g, b);
        }

        /// <summary>
        /// Detalle de un colaborador al hacer clic en su pin: área(s) asignada(s), equipos que atiende
        /// (derivados de sus operaciones, cruzados contra el catálogo de equipos ya cargado), las
        /// operaciones mismas, y las ubicaciones de esos equipos.
        /// </summary>
        public async Task CargarDetalleColaboradorAsync(VisorMundialColaboradorPuntoDto colaborador)
        {
            if (colaborador == null)
            {
                return;
            }

            try
            {
                IsLoadingDetalle = true;
                ColaboradorSeleccionado = colaborador;
                UbicacionSeleccionada = null;
                PanelSeleccion = VisorMundialPanelSeleccion.Colaborador;
                IsPanelOpen = true;

                var operacionesTask = _operacionService.GetOperacionesAsync(new OperacionQueryDto { IdAtiende = (int)colaborador.CredencialId });
                var areasTask = _relacionUsuarioAreaService.GetRelacionesPorUsuarioAsync(colaborador.CredencialId);
                await Task.WhenAll(operacionesTask, areasTask);

                var operaciones = operacionesTask.Result;
                var areas = areasTask.Result;

                var identificadoresEquipos = operaciones
                    .Select(o => o.Identificador)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var equiposAtiende = Equipos
                    .Where(e => !string.IsNullOrWhiteSpace(e.Identificador) && identificadoresEquipos.Contains(e.Identificador!, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                var idsUbicacion = equiposAtiende
                    .Where(e => e.IdUbicacion.HasValue)
                    .Select(e => e.IdUbicacion!.Value)
                    .Distinct()
                    .ToList();

                var ubicacionesAtiende = Ubicaciones.Where(u => idsUbicacion.Contains(u.IdUbicacion)).ToList();

                ColaboradorTreeItems = ConstruirArbolColaborador(areas, equiposAtiende, operaciones, ubicacionesAtiende);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el detalle del colaborador: {ex.Message}";
            }
            finally
            {
                IsLoadingDetalle = false;
            }
        }

        private static ObservableCollection<DetalleClienteTreeItem> ConstruirArbolColaborador(
            List<RelacionUsuarioAreaDto> areas,
            List<EquipoDto> equipos,
            List<OperacionDto> operaciones,
            List<VisorMundialUbicacionDto> ubicaciones)
        {
            var ramaArea = new DetalleClienteTreeItem("Área asignada", areas.Count.ToString());
            foreach (var area in areas)
            {
                ramaArea.Hijos.Add(new DetalleClienteTreeItem(area.NombreArea ?? "Sin nombre", null, nivel: 1));
            }

            var ramaEquipos = new DetalleClienteTreeItem("Equipos que atiende", equipos.Count.ToString());
            foreach (var equipo in equipos.OrderBy(e => e.Identificador, StringComparer.OrdinalIgnoreCase))
            {
                ramaEquipos.Hijos.Add(new DetalleClienteTreeItem(equipo.Identificador ?? "Sin identificador", equipo.Marca, nivel: 1));
            }

            var ramaOperaciones = ConstruirRamaOperacionesEquipo(operaciones);

            var ramaUbicaciones = new DetalleClienteTreeItem("Ubicaciones que atiende", ubicaciones.Count.ToString());
            foreach (var ubicacion in ubicaciones.OrderBy(u => u.Nombre, StringComparer.OrdinalIgnoreCase))
            {
                ramaUbicaciones.Hijos.Add(new DetalleClienteTreeItem(ubicacion.Nombre ?? "Sin nombre", ubicacion.DireccionCompleta, nivel: 1));
            }

            return new ObservableCollection<DetalleClienteTreeItem>
            {
                ramaArea,
                ramaEquipos,
                ramaOperaciones,
                ramaUbicaciones
            };
        }
    }

    /// <summary>
    /// Tarjeta de un equipo dentro del panel de detalle de una ubicación: color de salud de cobranza
    /// (propio del equipo, no del cliente) + árbol de facturas/operaciones/adeudo de ese equipo.
    /// </summary>
    public class VisorMundialEquipoCardItem
    {
        public VisorMundialEquipoCardItem(
            VisorMundialEquipoDto equipo,
            Color colorSalud,
            ObservableCollection<DetalleClienteTreeItem> treeItems,
            decimal totalFacturado,
            decimal totalAbonado,
            int diasVencidoMax)
        {
            Equipo = equipo;
            ColorSaludBrush = new SolidColorBrush(colorSalud);
            TreeItems = treeItems;
            TotalFacturado = totalFacturado;
            TotalAbonado = totalAbonado;
            DiasVencidoMax = diasVencidoMax;
        }

        public VisorMundialEquipoDto Equipo { get; }
        public Brush ColorSaludBrush { get; }
        public ObservableCollection<DetalleClienteTreeItem> TreeItems { get; }
        public decimal TotalFacturado { get; }
        public decimal TotalAbonado { get; }
        public int DiasVencidoMax { get; }

        public int IdEquipo => Equipo.IdEquipo;
        public string Identificador => Equipo.Identificador ?? "Sin identificador";
        public string SubtituloTexto => $"{(string.IsNullOrWhiteSpace(Equipo.Marca) ? "Sin marca" : Equipo.Marca)} · {Equipo.ClientesTexto}";
        public string SaldoTexto => (TotalFacturado - TotalAbonado).ToString("C2", new CultureInfo("es-MX"));
        public string EstadoOperacionTexto => Equipo.EstadoTexto;
    }
}
