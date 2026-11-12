using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Relaciones;

namespace Advance_Control.ViewModels
{
    /// <summary>Qué muestra el Visor según la tarjeta seleccionada.</summary>
    public enum ModoVisor { Vacio, Equipo, Cliente }

    /// <summary>
    /// Estado del prototipo de Catálogos.
    ///  - Equipo: elegido con el buscador; quitarlo reinicia toda la pantalla.
    ///  - Clientes: los vinculados al equipo (relaciones_equipo_cliente); vincular con el buscador,
    ///    desvincular uno a la vez. Cada acción se guarda en el momento.
    ///  - Visor: según la tarjeta seleccionada (equipo → resumen de operaciones, fase 4;
    ///    cliente → su ficha completa y sus contactos).
    /// </summary>
    public class ProCatalogosViewModel : ViewModelBase
    {
        private static readonly CultureInfo CulturaMx = new("es-MX");

        private readonly IRelacionService _relacionService;
        private readonly IClienteService _clienteService;
        private readonly IContactoService _contactoService;
        private readonly ILoggingService _logger;

        private EquipoBusquedaDto? _equipo;
        private RelacionClienteDto? _clienteSeleccionado;
        private CustomerDto? _clienteDetalle;
        private ContactoDto? _contactoSeleccionado;
        private ModoVisor _modo = ModoVisor.Vacio;
        private bool _cargandoClientes;
        private bool _cargandoCliente;

        public ProCatalogosViewModel(IRelacionService relacionService, IClienteService clienteService,
            IContactoService contactoService, ILoggingService logger)
        {
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
                    _ = CargarClientesAsync();
            }
        }

        public bool HayEquipo => _equipo is not null;
        public bool SinEquipo => _equipo is null;

        /// <summary>Basura del equipo: limpia la tarjeta y reinicia toda la pantalla.</summary>
        public void LimpiarEquipo() => Equipo = null;

        private void Reiniciar()
        {
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
            }
        }

        public bool VisorVacio => _modo == ModoVisor.Vacio;
        public bool VisorEquipo => _modo == ModoVisor.Equipo;
        public bool VisorCliente => _modo == ModoVisor.Cliente;

        /// <summary>Tocar la tarjeta del equipo: el Visor muestra su resumen.</summary>
        public void MostrarEquipoEnVisor()
        {
            if (HayEquipo) Modo = ModoVisor.Equipo;
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
