using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Relaciones;
using Advance_Control.Services.Areas;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Utilities;

namespace Advance_Control.ViewModels
{
    /// <summary>Qué muestra el Visor según la tarjeta seleccionada.</summary>
    public enum ModoVisor { Vacio, Equipo, Cliente, Area, Ubicacion, CrearArea, CrearUbicacion }

    /// <summary>
    /// Estado del prototipo de Catálogos.
    ///  - Equipo: elegido con el buscador; quitarlo reinicia toda la pantalla.
    ///  - Clientes: los vinculados al equipo (relaciones_equipo_cliente); vincular con el buscador,
    ///    desvincular uno a la vez. Cada acción se guarda en el momento.
    ///  - Área → Ubicación (un solo sentido): sin ubicación, el área se busca o se crea (sin basura)
    ///    y luego la ubicación se busca o se crea dentro de esa área. Con ubicación, el área queda
    ///    bloqueada y solo se puede cambiar la ubicación dentro de la misma área. Elegir o crear una
    ///    ubicación se la asigna al equipo en el momento.
    ///  - Visor: según la tarjeta seleccionada (equipo → resumen de operaciones, fase 4;
    ///    cliente → su ficha y contactos; área → mapa con contorno y ubicaciones; ubicación → su punto).
    /// </summary>
    public class ProCatalogosViewModel : ViewModelBase
    {
        private static readonly CultureInfo CulturaMx = new("es-MX");

        private readonly IRelacionService _relacionService;
        private readonly IClienteService _clienteService;
        private readonly IContactoService _contactoService;
        private readonly ILoggingService _logger;
        private readonly IAreasService _areasService;
        private readonly IUbicacionService _ubicacionService;
        private readonly IEquipoService _equipoService;

        private List<AreaDto> _areas = new();
        private List<UbicacionDto> _ubicaciones = new();
        private AreaDto? _area;

        private EquipoBusquedaDto? _equipo;
        private RelacionClienteDto? _clienteSeleccionado;
        private CustomerDto? _clienteDetalle;
        private ContactoDto? _contactoSeleccionado;
        private ModoVisor _modo = ModoVisor.Vacio;
        private bool _cargandoClientes;
        private bool _cargandoCliente;

        public ProCatalogosViewModel(IRelacionService relacionService, IClienteService clienteService,
            IContactoService contactoService, ILoggingService logger, IAreasService areasService,
            IUbicacionService ubicacionService, IEquipoService equipoService)
        {
            _areasService = areasService;
            _ubicacionService = ubicacionService;
            _equipoService = equipoService;
            _relacionService = relacionService;
            _clienteService = clienteService;
            _contactoService = contactoService;
            _logger = logger;
        }

        public ObservableCollection<RelacionClienteDto> Clientes { get; } = new();
        public ObservableCollection<ContactoDto> Contactos { get; } = new();

        // ── Equipo ─────────────────────────────────────────────────────────

        public EquipoBusquedaDto? Equipo
        {
            get => _equipo;
            set
            {
                if (!SetProperty(ref _equipo, value)) return;
                Reiniciar();
                Modo = value is null ? ModoVisor.Vacio : ModoVisor.Equipo;
                OnPropertyChanged(nameof(HayEquipo));
                OnPropertyChanged(nameof(SinEquipo));
                if (value is not null)
                {
                    _ = CargarClientesAsync();
                    _ = PrepararAreaUbicacionAsync();
                    _ = CargarResumenAsync();
                }
            }
        }

        public bool HayEquipo => _equipo is not null;
        public bool SinEquipo => _equipo is null;

        /// <summary>Basura del equipo: limpia la tarjeta y reinicia toda la pantalla.</summary>
        public void LimpiarEquipo() => Equipo = null;

        private void Reiniciar()
        {
            Resumen = null;
            ErrorResumen = null;
            _area = null;
            NotificarAreaUbicacion();
            Clientes.Clear();
            Contactos.Clear();
            _clienteSeleccionado = null;
            _clienteDetalle = null;
            _contactoSeleccionado = null;
            OnPropertyChanged(nameof(ClienteSeleccionado));
            OnPropertyChanged(nameof(ClienteDetalle));
            OnPropertyChanged(nameof(ContactoSeleccionado));
            NotificarClientes();
            NotificarCliente();
        }

        // ── Visor ──────────────────────────────────────────────────────────

        public ModoVisor Modo
        {
            get => _modo;
            set
            {
                if (!SetProperty(ref _modo, value)) return;
                OnPropertyChanged(nameof(VisorVacio));
                OnPropertyChanged(nameof(VisorEquipo));
                OnPropertyChanged(nameof(VisorCliente));
                OnPropertyChanged(nameof(VisorTapaMapa));
                OnPropertyChanged(nameof(VisorCrearArea));
                OnPropertyChanged(nameof(VisorCrearUbicacion));
            }
        }

        public bool VisorVacio => _modo == ModoVisor.Vacio;
        public bool VisorEquipo => _modo == ModoVisor.Equipo;
        public bool VisorCliente => _modo == ModoVisor.Cliente;
        /// <summary>Equipo y cliente se muestran encima del mapa (que sigue cargado debajo).</summary>
        public bool VisorTapaMapa => _modo is ModoVisor.Equipo or ModoVisor.Cliente;
        public bool VisorCrearArea => _modo == ModoVisor.CrearArea;
        public bool VisorCrearUbicacion => _modo == ModoVisor.CrearUbicacion;

        /// <summary>Tocar la tarjeta del equipo: el Visor muestra su resumen.</summary>
        public void MostrarEquipoEnVisor()
        {
            if (HayEquipo) Modo = ModoVisor.Equipo;
        }

        // ── Resumen de operaciones del equipo (Visor) ─────────────────────

        private EquipoResumenDto? _resumen;
        private bool _cargandoResumen;
        private string? _errorResumen;

        public EquipoResumenDto? Resumen
        {
            get => _resumen;
            private set { if (SetProperty(ref _resumen, value)) NotificarResumen(); }
        }

        public bool CargandoResumen
        {
            get => _cargandoResumen;
            private set { if (SetProperty(ref _cargandoResumen, value)) NotificarResumen(); }
        }

        public string? ErrorResumen
        {
            get => _errorResumen;
            private set { if (SetProperty(ref _errorResumen, value)) NotificarResumen(); }
        }

        public bool HayResumen => _resumen is not null;
        public bool HayErrorResumen => _errorResumen is not null;

        public async Task CargarResumenAsync()
        {
            if (_equipo is null) return;
            var id = _equipo.IdEquipo;
            CargandoResumen = true;
            ErrorResumen = null;
            try
            {
                var resumen = await _equipoService.GetResumenOperacionesAsync(id);
                if (_equipo?.IdEquipo == id) Resumen = resumen;
            }
            catch (Exception ex)
            {
                ErrorResumen = ex.Message;
                await _logger.LogErrorAsync("Error al cargar el resumen de operaciones", ex, nameof(ProCatalogosViewModel), nameof(CargarResumenAsync));
            }
            finally
            {
                CargandoResumen = false;
            }
        }

        private void NotificarResumen()
        {
            OnPropertyChanged(nameof(HayResumen));
            OnPropertyChanged(nameof(HayErrorResumen));
        }

        // ── Clientes del equipo ───────────────────────────────────────────

        public bool CargandoClientes
        {
            get => _cargandoClientes;
            private set { if (SetProperty(ref _cargandoClientes, value)) NotificarClientes(); }
        }

        public bool SinClientes => HayEquipo && !_cargandoClientes && Clientes.Count == 0;

        public RelacionClienteDto? ClienteSeleccionado
        {
            get => _clienteSeleccionado;
            set
            {
                if (!SetProperty(ref _clienteSeleccionado, value)) return;
                OnPropertyChanged(nameof(HayClienteSeleccionado));
                if (value is not null)
                {
                    Modo = ModoVisor.Cliente;
                    _ = CargarClienteAsync(value.IdCliente);
                }
                else if (_modo == ModoVisor.Cliente)
                {
                    Modo = HayEquipo ? ModoVisor.Equipo : ModoVisor.Vacio;
                }
            }
        }

        public bool HayClienteSeleccionado => _clienteSeleccionado is not null;

        public async Task CargarClientesAsync(int? seleccionarIdCliente = null)
        {
            if (_equipo is null) return;
            var identificador = _equipo.Identificador;
            CargandoClientes = true;
            try
            {
                var relaciones = await _relacionService.GetRelacionesAsync(identificador);
                if (_equipo?.Identificador != identificador) return; // se cambió el equipo mientras cargaba
                Clientes.Clear();
                foreach (var r in relaciones.OrderBy(r => r.RazonSocial, StringComparer.CurrentCultureIgnoreCase))
                    Clientes.Add(r);
                if (seleccionarIdCliente is { } id)
                    ClienteSeleccionado = Clientes.FirstOrDefault(c => c.IdCliente == id);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar clientes del equipo", ex, nameof(ProCatalogosViewModel), nameof(CargarClientesAsync));
            }
            finally
            {
                CargandoClientes = false;
            }
        }

        /// <summary>Vincula el cliente al equipo (se guarda en el momento) y lo deja seleccionado.</summary>
        public async Task<string?> VincularClienteAsync(CustomerDto cliente)
        {
            if (_equipo is null) return "No hay equipo seleccionado.";
            if (Clientes.Any(c => c.IdCliente == cliente.IdCliente))
                return "El cliente ya está vinculado a este equipo.";
            var ok = await _relacionService.CreateRelacionAsync(_equipo.Identificador, cliente.IdCliente, null);
            if (!ok) return "No se pudo vincular el cliente.";
            await CargarClientesAsync(seleccionarIdCliente: cliente.IdCliente);
            return null;
        }

        /// <summary>Desvincula el cliente seleccionado del equipo (solo la vinculación).</summary>
        public async Task<string?> DesvincularClienteSeleccionadoAsync()
        {
            if (_equipo is null || _clienteSeleccionado is null) return null;
            var ok = await _relacionService.DeleteRelacionAsync(_equipo.Identificador, _clienteSeleccionado.IdCliente);
            if (!ok) return "No se pudo desvincular el cliente.";
            ClienteSeleccionado = null;
            await CargarClientesAsync();
            return null;
        }

        private void NotificarClientes()
        {
            OnPropertyChanged(nameof(SinClientes));
            OnPropertyChanged(nameof(HayClienteSeleccionado));
        }

        // ── Cliente seleccionado: ficha y contactos ───────────────────────

        public CustomerDto? ClienteDetalle
        {
            get => _clienteDetalle;
            private set { if (SetProperty(ref _clienteDetalle, value)) NotificarCliente(); }
        }

        public bool CargandoCliente
        {
            get => _cargandoCliente;
            private set { if (SetProperty(ref _cargandoCliente, value)) NotificarCliente(); }
        }

        public bool SinContactos => _clienteDetalle is not null && !_cargandoCliente && Contactos.Count == 0;

        public ContactoDto? ContactoSeleccionado
        {
            get => _contactoSeleccionado;
            set { if (SetProperty(ref _contactoSeleccionado, value)) OnPropertyChanged(nameof(HayContactoSeleccionado)); }
        }

        public bool HayContactoSeleccionado => _contactoSeleccionado is not null;

        // Textos de la cabecera del cliente
        public string ClienteRazonSocial => _clienteDetalle?.RazonSocial ?? string.Empty;
        public string ClienteNombreComercial => Texto(_clienteDetalle?.NombreComercial);
        public string ClienteRfc => Texto(_clienteDetalle?.Rfc);
        public string ClienteRegimen => Texto(_clienteDetalle?.RegimenFiscal);
        public string ClienteUsoCfdi => Texto(_clienteDetalle?.UsoCfdi);
        public string ClienteCodigoPostal => Texto(_clienteDetalle?.CodigoPostal);
        public string ClienteDiasCredito => _clienteDetalle?.DiasCredito is { } d ? $"{d} días" : "—";
        public string ClienteLimiteCredito => _clienteDetalle?.LimiteCredito is { } l ? l.ToString("C", CulturaMx) : "—";
        public string ClientePrioridad => _clienteDetalle is null ? "—" : _clienteDetalle.Prioridad.ToString();
        public string ClienteEstatus => _clienteDetalle is null ? "—" : _clienteDetalle.Estatus ? "Activo" : "Inactivo";
        public string ClienteNotas => Texto(_clienteDetalle?.Notas);

        private static string Texto(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s!;

        private async Task CargarClienteAsync(int idCliente)
        {
            CargandoCliente = true;
            Contactos.Clear();
            ContactoSeleccionado = null;
            try
            {
                var detalleTask = _clienteService.GetClienteByIdAsync(idCliente);
                var contactosTask = _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = idCliente });
                await Task.WhenAll(detalleTask, contactosTask);
                if (_clienteSeleccionado?.IdCliente != idCliente) return; // cambió la selección

                ClienteDetalle = detalleTask.Result.FirstOrDefault();
                foreach (var c in contactosTask.Result.OrderBy(c => c.NombreCompleto, StringComparer.CurrentCultureIgnoreCase))
                    Contactos.Add(c);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar el cliente", ex, nameof(ProCatalogosViewModel), nameof(CargarClienteAsync));
            }
            finally
            {
                CargandoCliente = false;
            }
        }

        /// <summary>Vincula el contacto al cliente seleccionado (se guarda en el momento).</summary>
        public async Task<string?> VincularContactoAsync(ContactoDto contacto)
        {
            if (_clienteSeleccionado is null) return "No hay cliente seleccionado.";
            if (Contactos.Any(c => c.ContactoId == contacto.ContactoId))
                return "El contacto ya está vinculado a este cliente.";
            var respuesta = await _contactoService.VincularClienteAsync(contacto.ContactoId, _clienteSeleccionado.IdCliente);
            if (!respuesta.Success) return respuesta.Message ?? "No se pudo vincular el contacto.";
            Contactos.Add(contacto);
            ContactoSeleccionado = contacto;
            OnPropertyChanged(nameof(SinContactos));
            return null;
        }

        /// <summary>Desvincula el contacto seleccionado del cliente (solo la vinculación).</summary>
        public async Task<string?> DesvincularContactoSeleccionadoAsync()
        {
            if (_clienteSeleccionado is null || _contactoSeleccionado is null) return null;
            var contacto = _contactoSeleccionado;
            var respuesta = await _contactoService.DesvincularClienteAsync(contacto.ContactoId, _clienteSeleccionado.IdCliente);
            if (!respuesta.Success) return respuesta.Message ?? "No se pudo desvincular el contacto.";
            Contactos.Remove(contacto);
            ContactoSeleccionado = null;
            OnPropertyChanged(nameof(SinContactos));
            return null;
        }

        /// <summary>Refleja en la lista los cambios hechos al contacto desde su detalle.</summary>
        public void ReemplazarContacto(ContactoDto actualizado)
        {
            var i = Contactos.ToList().FindIndex(c => c.ContactoId == actualizado.ContactoId);
            if (i < 0) return;
            Contactos[i] = actualizado;
            ContactoSeleccionado = actualizado;
        }

        // ── Área y ubicación ──────────────────────────────────────────────

        /// <summary>Área elegida (o la de la ubicación del equipo).</summary>
        public AreaDto? Area => _area;
        public bool HayArea => _area is not null;

        /// <summary>El equipo ya tiene ubicación con área: el área no se puede cambiar.</summary>
        public bool AreaBloqueada => _equipo?.IdUbicacion is not null && _equipo.IdArea is not null
            && _area is not null && _area.IdArea == _equipo.IdArea;
        public bool AreaEditable => HayEquipo && !AreaBloqueada;

        /// <summary>La ubicación se desbloquea al haber área.</summary>
        public bool UbicacionHabilitada => HayEquipo && _area is not null;
        public bool UbicacionBloqueada => HayEquipo && _area is null;

        public string AreaTexto => _area?.Nombre ?? "Sin área seleccionada";
        public bool TieneUbicacion => _equipo?.IdUbicacion is not null;
        public string UbicacionNombre => _equipo?.IdUbicacion is null ? "Sin ubicación asignada" : _equipo.UbicacionTexto;
        public string UbicacionDireccion => string.Join(" · ", new[] { _equipo?.Direccion, _equipo?.CiudadEstadoTexto }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        /// <summary>Contorno del área elegida (para el mapa y para validar ubicaciones nuevas).</summary>
        public List<(double Lat, double Lng)> ContornoArea => GeoContornos.PathDeMetadata(_area?.MetadataJSON);

        /// <summary>Ubicaciones activas del área elegida.</summary>
        public List<UbicacionDto> UbicacionesDelArea => _area is null ? new()
            : _ubicaciones.Where(u => u.IdArea == _area.IdArea && u.Activo != false && u.Latitud is not null && u.Longitud is not null)
                .OrderBy(u => u.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList();

        public IReadOnlyList<AreaDto> Areas => _areas;

        /// <summary>Al elegir equipo: carga áreas/ubicaciones y, si tiene ubicación, fija su área.</summary>
        private async Task PrepararAreaUbicacionAsync()
        {
            var equipo = _equipo;
            await CargarCatalogosAsync();
            if (!ReferenceEquals(equipo, _equipo) || equipo is null) return;
            if (equipo.IdArea is { } idArea)
                _area = _areas.FirstOrDefault(a => a.IdArea == idArea);
            NotificarAreaUbicacion();
        }

        public async Task CargarCatalogosAsync()
        {
            try
            {
                var areasTask = _areasService.GetAreasAsync(activo: true);
                var ubicacionesTask = _ubicacionService.GetUbicacionesAsync();
                await Task.WhenAll(areasTask, ubicacionesTask);
                _areas = areasTask.Result.OrderBy(a => a.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList();
                _ubicaciones = ubicacionesTask.Result;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar áreas y ubicaciones", ex, nameof(ProCatalogosViewModel), nameof(CargarCatalogosAsync));
            }
        }

        /// <summary>Elige el área (solo si el equipo no tiene ubicación) y muestra su mapa.</summary>
        public void ElegirArea(AreaDto area)
        {
            if (!AreaEditable) return;
            _area = area;
            NotificarAreaUbicacion();
            Modo = ModoVisor.Area;
        }

        public void MostrarAreaEnVisor()
        {
            if (_area is not null) Modo = ModoVisor.Area;
        }

        public void MostrarUbicacionEnVisor()
        {
            if (_equipo?.IdUbicacion is not null) Modo = ModoVisor.Ubicacion;
            else if (_area is not null) Modo = ModoVisor.Area;
        }

        public void IniciarCrearArea()
        {
            if (AreaEditable) Modo = ModoVisor.CrearArea;
        }

        public void IniciarCrearUbicacion()
        {
            if (UbicacionHabilitada) Modo = ModoVisor.CrearUbicacion;
        }

        /// <summary>Cancela la creación y regresa al mapa del área (o al estado anterior).</summary>
        public void CancelarCreacion() =>
            Modo = _area is not null ? ModoVisor.Area : HayEquipo ? ModoVisor.Equipo : ModoVisor.Vacio;

        /// <summary>
        /// Crea el área a partir de un estado o municipio (contorno del GeoJSON) y la deja elegida.
        /// Devuelve el error, o null si salió bien.
        /// </summary>
        public async Task<string?> CrearAreaAsync(string geoType, string geoNombre, string? geoEstado, string nombre)
        {
            if (!AreaEditable) return "El área no se puede cambiar: el equipo ya tiene ubicación.";
            if (string.IsNullOrWhiteSpace(nombre)) return "Escribe el nombre del área.";
            var contorno = GeoContornos.Obtener(geoType, geoNombre, geoEstado);
            if (contorno is null) return $"No se encontró el contorno de {geoNombre}.";

            var area = new AreaDto
            {
                Nombre = nombre.Trim(),
                Descripcion = geoType == "Municipio" && geoEstado is not null ? $"Municipio del estado de {geoEstado}" : null,
                ColorMapa = "#1a73e8",
                ColorBorde = "#1a73e8",
                Opacidad = 0.35m,
                AnchoBorde = 2,
                Activo = true,
                TipoGeometria = geoType,
                MetadataJSON = System.Text.Json.JsonSerializer.Serialize(new
                {
                    geoType,
                    nombre = geoNombre,
                    estado = geoEstado,
                    path = contorno.Path.Select(p => new { lat = p.Lat, lng = p.Lng })
                }),
                BoundingBoxNE_Lat = contorno.Norte,
                BoundingBoxNE_Lng = contorno.Este,
                BoundingBoxSW_Lat = contorno.Sur,
                BoundingBoxSW_Lng = contorno.Oeste,
                CentroLatitud = Math.Round((contorno.Norte + contorno.Sur) / 2m, 6),
                CentroLongitud = Math.Round((contorno.Este + contorno.Oeste) / 2m, 6)
            };

            var respuesta = await _areasService.CreateAreaAsync(area);
            if (!respuesta.Success) return respuesta.Message ?? "No se pudo crear el área.";

            await CargarCatalogosAsync();
            var creada = _areas.Where(a => string.Equals(a.Nombre, area.Nombre, StringComparison.CurrentCultureIgnoreCase))
                .OrderByDescending(a => a.IdArea).FirstOrDefault();
            if (creada is null) return "El área se creó, pero no se pudo recargar.";
            _area = creada;
            NotificarAreaUbicacion();
            Modo = ModoVisor.Area;
            return null;
        }

        /// <summary>Asigna al equipo una ubicación existente del área (se guarda en el momento).</summary>
        public async Task<string?> AsignarUbicacionAsync(int idUbicacion)
        {
            if (_equipo is null || _area is null) return "Primero elige un área.";
            var ubicacion = _ubicaciones.FirstOrDefault(u => u.IdUbicacion == idUbicacion);
            if (ubicacion is null) return "No se encontró la ubicación.";
            if (ubicacion.IdArea != _area.IdArea) return "La ubicación no pertenece al área elegida.";

            var ok = await _equipoService.UpdateEquipoAsync(_equipo.IdEquipo, new EquipoQueryDto { IdUbicacion = idUbicacion });
            if (!ok) return "No se pudo asignar la ubicación al equipo.";

            ActualizarUbicacionDelEquipo(ubicacion);
            Modo = ModoVisor.Ubicacion;
            return null;
        }

        /// <summary>
        /// Crea una ubicación dentro del área (el punto debe caer en su contorno) y se la asigna al equipo.
        /// </summary>
        public async Task<string?> CrearUbicacionAsync(string nombre, string descripcion, string direccion,
            string ciudad, string estado, double lat, double lng)
        {
            if (_equipo is null || _area is null) return "Primero elige un área.";
            if (new[] { nombre, descripcion, direccion, ciudad, estado }.Any(string.IsNullOrWhiteSpace))
                return "Todos los campos son obligatorios: nombre, descripción, dirección, ciudad y estado.";
            var contorno = ContornoArea;
            if (contorno.Count >= 3 && !GeoContornos.PuntoEnPoligono(lat, lng, contorno))
                return $"El punto está fuera del área {_area.Nombre}. Muévelo dentro del contorno.";

            var (exito, mensaje, id) = await _ubicacionService.CrearUbicacionConIdAsync(new UbicacionDto
            {
                Nombre = nombre.Trim(),
                Descripcion = descripcion.Trim(),
                DireccionCompleta = direccion.Trim(),
                Ciudad = ciudad.Trim(),
                Estado = estado.Trim(),
                Pais = "México",
                Latitud = Math.Round((decimal)lat, 8),
                Longitud = Math.Round((decimal)lng, 8),
                IdArea = _area.IdArea,
                Activo = true
            });
            if (!exito || id is null) return mensaje ?? "No se pudo crear la ubicación.";

            await CargarCatalogosAsync();
            return await AsignarUbicacionAsync(id.Value);
        }

        /// <summary>Refleja en la tarjeta del equipo la ubicación asignada (sin reiniciar la pantalla).</summary>
        private void ActualizarUbicacionDelEquipo(UbicacionDto u)
        {
            if (_equipo is null) return;
            _equipo.IdUbicacion = u.IdUbicacion;
            _equipo.Ubicacion = u.Nombre;
            _equipo.Direccion = u.DireccionCompleta;
            _equipo.Ciudad = u.Ciudad;
            _equipo.Estado = u.Estado;
            _equipo.Latitud = u.Latitud;
            _equipo.Longitud = u.Longitud;
            _equipo.IdArea = _area?.IdArea;
            _equipo.Area = _area?.Nombre;
            // La tarjeta del equipo se enlaza a propiedades del objeto: forzar el refresco.
            var equipo = _equipo;
            _equipo = null;
            OnPropertyChanged(nameof(Equipo));
            _equipo = equipo;
            OnPropertyChanged(nameof(Equipo));
            NotificarAreaUbicacion();
        }

        private void NotificarAreaUbicacion()
        {
            foreach (var p in new[] { nameof(Area), nameof(HayArea), nameof(AreaBloqueada), nameof(AreaEditable),
                nameof(UbicacionHabilitada), nameof(UbicacionBloqueada), nameof(AreaTexto), nameof(TieneUbicacion),
                nameof(UbicacionNombre), nameof(UbicacionDireccion) })
                OnPropertyChanged(p);
        }

        private void NotificarCliente()
        {
            OnPropertyChanged(nameof(SinContactos));
            foreach (var p in new[] { nameof(ClienteRazonSocial), nameof(ClienteNombreComercial), nameof(ClienteRfc),
                nameof(ClienteRegimen), nameof(ClienteUsoCfdi), nameof(ClienteCodigoPostal), nameof(ClienteDiasCredito),
                nameof(ClienteLimiteCredito), nameof(ClientePrioridad), nameof(ClienteEstatus), nameof(ClienteNotas) })
                OnPropertyChanged(p);
        }
    }
}
