using System;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Controls;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Lienzo de prototipos del grupo Catálogos. Visible solo para nivel 1 (devs)
    /// vía el sistema de permisos UI. Sin funcionalidad fija: se construye libremente aquí.
    /// Prototipo actual: equipo → clientes → (área → ubicación), con el Visor según la tarjeta.
    /// </summary>
    public sealed partial class ProCatalogosPage : Page
    {
        public ProCatalogosViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;

        public ProCatalogosPage()
        {
            ViewModel = AppServices.Get<ProCatalogosViewModel>();
            _notificacionService = AppServices.Get<INotificacionService>();
            InitializeComponent();
            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ProCatalogosViewModel.Modo) or nameof(ProCatalogosViewModel.Area)
                    or nameof(ProCatalogosViewModel.UbicacionNombre))
                    ActualizarMapa();
            };
        }

        // ── Mapa del Visor ─────────────────────────────────────────────────

        /// <summary>Dibuja en el mapa lo que corresponde al modo del Visor.</summary>
        private void ActualizarMapa()
        {
            // El mapa solo existe con equipo elegido (x:Load); sin equipo no hay nada que dibujar.
            if (Mapa is null) return;
            var modo = ViewModel.Modo;
            Mapa.ModoCrearArea(modo == ModoVisor.CrearArea);
            Mapa.ModoCrearUbicacion(modo == ModoVisor.CrearUbicacion);

            switch (modo)
            {
                case ModoVisor.Vacio:
                case ModoVisor.CrearArea:
                    Mapa.MostrarVista(new VistaMapa { Encuadrar = false });
                    break;
                case ModoVisor.Area:
                case ModoVisor.CrearUbicacion:
                    Mapa.MostrarVista(VistaDelArea(centrarEnEquipo: false));
                    break;
                case ModoVisor.Ubicacion:
                    Mapa.MostrarVista(VistaDelArea(centrarEnEquipo: true));
                    break;
                // Equipo y Cliente se muestran encima del mapa: se deja como estaba.
            }
        }

        private VistaMapa VistaDelArea(bool centrarEnEquipo)
        {
            var equipo = ViewModel.Equipo;
            (double, double)? centro = null;
            if (centrarEnEquipo && equipo?.Latitud is { } la && equipo.Longitud is { } ln)
                centro = ((double)la, (double)ln);

            return new VistaMapa
            {
                Contorno = ViewModel.ContornoArea,
                Marcadores = ViewModel.UbicacionesDelArea
                    .Select(u => new MarcadorMapa(u.IdUbicacion ?? 0, u.Nombre ?? "", (double)u.Latitud!.Value,
                        (double)u.Longitud!.Value, u.IdUbicacion == equipo?.IdUbicacion))
                    .ToList(),
                Centro = centro,
                Zoom = centro is null ? null : 17
            };
        }

        /// <summary>Clic en un marcador: asignar esa ubicación al equipo (con confirmación si ya tenía otra).</summary>
        private async void Mapa_MarcadorClic(object? sender, int idUbicacion)
        {
            if (ViewModel.Modo is not (ModoVisor.Area or ModoVisor.Ubicacion) || !ViewModel.UbicacionHabilitada) return;
            await AsignarUbicacionAsync(idUbicacion);
        }

        // ── Área ───────────────────────────────────────────────────────────

        private GeoElegido? _geoElegido;

        private void TarjetaArea_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel.MostrarAreaEnVisor();

        private async void BuscarArea_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.AreaEditable) return;
            if (ViewModel.Areas.Count == 0) await ViewModel.CargarCatalogosAsync();
            var area = await ElegirDeListaAsync("Buscador áreas...", "Buscar área...", "No hay áreas; crea una con +",
                ViewModel.Areas.Select(a => new BuscadorListaItem(a, a.Nombre, a.Descripcion ?? a.TipoGeometria, $"{a.Nombre} {a.Descripcion}")));
            if (area is AreaDto elegida)
                ViewModel.ElegirArea(elegida);
        }

        private void CrearArea_Click(object sender, RoutedEventArgs e)
        {
            _geoElegido = null;
            GeoElegidoText.Text = "Elige un estado o municipio en el mapa.";
            NombreAreaTextBox.Text = string.Empty;
            ErrorAreaText.Visibility = Visibility.Collapsed;
            ViewModel.IniciarCrearArea();
        }

        private void Mapa_GeoClic(object? sender, GeoElegido geo)
        {
            if (ViewModel.Modo != ModoVisor.CrearArea) return;
            var anterior = _geoElegido;
            _geoElegido = geo;
            GeoElegidoText.Text = geo.GeoType == "Municipio"
                ? $"Municipio: {geo.Nombre} ({geo.Estado})"
                : $"Estado: {geo.Nombre}";
            // Proponer el nombre si está vacío o si era el propuesto antes
            if (string.IsNullOrWhiteSpace(NombreAreaTextBox.Text) || NombreAreaTextBox.Text == anterior?.Nombre)
                NombreAreaTextBox.Text = geo.Nombre;
            ErrorAreaText.Visibility = Visibility.Collapsed;
        }

        private async void GuardarArea_Click(object sender, RoutedEventArgs e)
        {
            if (_geoElegido is null)
            {
                MostrarError(ErrorAreaText, "Elige un estado o municipio en el mapa.");
                return;
            }
            GuardarAreaButton.IsEnabled = false;
            try
            {
                var error = await ViewModel.CrearAreaAsync(_geoElegido.GeoType, _geoElegido.Nombre, _geoElegido.Estado, NombreAreaTextBox.Text);
                if (error is not null) MostrarError(ErrorAreaText, error);
            }
            finally
            {
                GuardarAreaButton.IsEnabled = true;
            }
        }

        // ── Ubicación ──────────────────────────────────────────────────────

        private PuntoElegido? _punto;

        private void TarjetaUbicacion_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel.MostrarUbicacionEnVisor();

        private async void BuscarUbicacion_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.UbicacionHabilitada) return;
            var ubicacion = await ElegirDeListaAsync("Buscador ubicaciones...", "Buscar ubicación del área...",
                "El área no tiene ubicaciones; crea una con +",
                ViewModel.UbicacionesDelArea.Select(u => new BuscadorListaItem(u, u.Nombre ?? "", u.DireccionCompleta,
                    $"{u.Nombre} {u.DireccionCompleta} {u.Ciudad}")));
            if (ubicacion is UbicacionDto elegida && elegida.IdUbicacion is { } id)
                await AsignarUbicacionAsync(id);
        }

        private void CrearUbicacion_Click(object sender, RoutedEventArgs e)
        {
            _punto = null;
            PuntoText.Text = "Busca la dirección arriba del mapa y ajusta el punto con un clic.";
            foreach (var t in new[] { NombreUbicacionTextBox, DescripcionUbicacionTextBox, DireccionUbicacionTextBox,
                         CiudadUbicacionTextBox, EstadoUbicacionTextBox })
                t.Text = string.Empty;
            ErrorUbicacionText.Visibility = Visibility.Collapsed;
            ViewModel.IniciarCrearUbicacion();
        }

        private void Mapa_PuntoClic(object? sender, PuntoElegido punto)
        {
            if (ViewModel.Modo != ModoVisor.CrearUbicacion) return;
            _punto = punto;
            if (!string.IsNullOrWhiteSpace(punto.Direccion)) DireccionUbicacionTextBox.Text = punto.Direccion;
            if (!string.IsNullOrWhiteSpace(punto.Ciudad)) CiudadUbicacionTextBox.Text = punto.Ciudad;
            if (!string.IsNullOrWhiteSpace(punto.Estado)) EstadoUbicacionTextBox.Text = punto.Estado;

            var contorno = ViewModel.ContornoArea;
            var dentro = contorno.Count < 3 || GeoContornos.PuntoEnPoligono(punto.Lat, punto.Lng, contorno);
            PuntoText.Text = $"Punto: {punto.Lat:0.000000}, {punto.Lng:0.000000}" +
                (dentro ? "  ✓ dentro del área" : "  ✗ FUERA del área: muévelo dentro del contorno");
            ErrorUbicacionText.Visibility = Visibility.Collapsed;
        }

        private async void GuardarUbicacion_Click(object sender, RoutedEventArgs e)
        {
            if (_punto is null)
            {
                MostrarError(ErrorUbicacionText, "Marca el punto en el mapa (busca la dirección o haz clic).");
                return;
            }
            GuardarUbicacionButton.IsEnabled = false;
            try
            {
                var error = await ViewModel.CrearUbicacionAsync(NombreUbicacionTextBox.Text, DescripcionUbicacionTextBox.Text,
                    DireccionUbicacionTextBox.Text, CiudadUbicacionTextBox.Text, EstadoUbicacionTextBox.Text, _punto.Lat, _punto.Lng);
                if (error is not null) MostrarError(ErrorUbicacionText, error);
            }
            finally
            {
                GuardarUbicacionButton.IsEnabled = true;
            }
        }

        private void CancelarCreacion_Click(object sender, RoutedEventArgs e) => ViewModel.CancelarCreacion();

        private async Task AsignarUbicacionAsync(int idUbicacion)
        {
            var equipo = ViewModel.Equipo;
            if (equipo is null || equipo.IdUbicacion == idUbicacion) return;
            var nueva = ViewModel.UbicacionesDelArea.FirstOrDefault(u => u.IdUbicacion == idUbicacion);
            var mensaje = equipo.IdUbicacion is null
                ? $"¿Asignar la ubicación {nueva?.Nombre} al equipo {equipo.Identificador}?"
                : $"¿Cambiar la ubicación del equipo {equipo.Identificador} de {equipo.UbicacionTexto} a {nueva?.Nombre}?";
            if (!await ConfirmarAsync("Asignar ubicación", mensaje, "Asignar")) return;
            await Avisar(await ViewModel.AsignarUbicacionAsync(idUbicacion));
        }

        /// <summary>Buscador de lista con la lista completa visible (áreas, ubicaciones del área).</summary>
        private Task<object?> ElegirDeListaAsync(string titulo, string placeholder, string vacio, System.Collections.Generic.IEnumerable<BuscadorListaItem> items)
        {
            var lista = items.ToList();
            var tcs = new TaskCompletionSource<object?>();
            Action? cerrar = null;
            var buscador = new BuscadorListaUserControl(titulo, placeholder, vacio, null,
                () => Task.FromResult(lista), mostrarTodoSinTexto: true);
            void Terminar(object? r) { if (tcs.Task.IsCompleted) return; cerrar?.Invoke(); tcs.TrySetResult(r); }
            buscador.Elegido += (_, item) => Terminar(item.Dato);
            buscador.Cancelado += (_, _) => Terminar(null);
            cerrar = ModalPopup.Abrir(XamlRoot, buscador, alEscape: () => Terminar(null));
            return tcs.Task;
        }

        private static void MostrarError(TextBlock destino, string mensaje)
        {
            destino.Text = mensaje;
            destino.Visibility = Visibility.Visible;
        }

        // ── Equipo ─────────────────────────────────────────────────────────

        private async void AgregarEquipo_Click(object sender, RoutedEventArgs e)
        {
            var equipo = await BuscadorEquiposDialog.MostrarAsync(XamlRoot);
            if (equipo is not null)
                ViewModel.Equipo = equipo;
        }

        private void LimpiarEquipo_Click(object sender, RoutedEventArgs e) => ViewModel.LimpiarEquipo();

        private void TarjetaEquipo_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel.MostrarEquipoEnVisor();

        /// <summary>Clic en una operación del resumen: se abre en su propia ventana.</summary>
        private void OperacionResumen_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is OperacionResumenDto operacion)
                Advance_Control.Views.Windows.OperacionVisorWindow.Open(operacion.Id);
        }

        // ── Clientes ───────────────────────────────────────────────────────

        private async void AgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            var cliente = await BuscadorClientesDialog.MostrarAsync(XamlRoot, ViewModel.Clientes.Select(c => c.IdCliente));
            if (cliente is null) return;
            await Avisar(await ViewModel.VincularClienteAsync(cliente));
        }

        private async void DesvincularCliente_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ClienteSeleccionado is not { } cliente || ViewModel.Equipo is not { } equipo) return;
            if (!await ConfirmarAsync("Desvincular cliente",
                    $"¿Desvincular a {cliente.RazonSocial} del equipo {equipo.Identificador}? El cliente no se borra, solo deja de estar ligado a este equipo."))
                return;
            await Avisar(await ViewModel.DesvincularClienteSeleccionadoAsync());
        }

        // ── Contactos del cliente ─────────────────────────────────────────

        private async void AgregarContacto_Click(object sender, RoutedEventArgs e)
        {
            var contacto = await ContactosDialogs.BuscarAsync(XamlRoot, ViewModel.Contactos.Select(c => c.ContactoId));
            if (contacto is null) return;
            await Avisar(await ViewModel.VincularContactoAsync(contacto));
        }

        private async void DesvincularContacto_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ContactoSeleccionado is not { } contacto || ViewModel.ClienteSeleccionado is not { } cliente) return;
            if (!await ConfirmarAsync("Desvincular contacto",
                    $"¿Desvincular a {contacto.NombreCompleto} de {cliente.RazonSocial}? El contacto no se borra, solo deja de estar ligado a este cliente."))
                return;
            await Avisar(await ViewModel.DesvincularContactoSeleccionadoAsync());
        }

        /// <summary>Clic en un contacto: queda seleccionado y se abre su detalle (editable).</summary>
        private async void Contacto_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not ContactoDto contacto) return;
            ViewModel.ContactoSeleccionado = contacto;
            var actualizado = await ContactosDialogs.DetalleAsync(XamlRoot, contacto);
            if (actualizado is not null)
                ViewModel.ReemplazarContacto(actualizado);
        }

        // ── Personal asignado: técnicos, supervisores y administrativos ────

        /// <summary>+ de una lista de personal: el rol viene en Tag (tecnico, supervisor, agente).</summary>
        private async void AgregarPersonal_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not string rol || ViewModel.ClienteSeleccionado is null) return;
            var rolTexto = PersonalClienteDto.RolTexto(rol);

            System.Collections.Generic.List<PersonalClienteDto> candidatos;
            try
            {
                candidatos = await ViewModel.CandidatosAsync(rol);
            }
            catch (Exception ex)
            {
                await Avisar(ex.Message);
                return;
            }

            var elegido = await ElegirDeListaAsync($"Asignar {rolTexto}", "Buscar por nombre o usuario", $"No hay más usuarios disponibles para {rolTexto}.",
                candidatos.Select(c => new BuscadorListaItem(c, c.NombreCompleto, c.Detalle, $"{c.Nombre} {c.Usuario} {c.Correo}")));
            if (elegido is PersonalClienteDto usuario)
                await Avisar(await ViewModel.AsignarPersonalAsync(rol, usuario));
        }

        /// <summary>Basura de un renglón de personal: quita al usuario del rol en este cliente.</summary>
        private async void QuitarPersonal_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not PersonalClienteDto usuario || ViewModel.ClienteSeleccionado is not { } cliente) return;
            if (!await ConfirmarAsync("Quitar personal",
                    $"¿Quitar a {usuario.NombreCompleto} como {PersonalClienteDto.RolTexto(usuario.Rol ?? "")} de {cliente.RazonSocial}?", "Quitar"))
                return;
            await Avisar(await ViewModel.QuitarPersonalAsync(usuario));
        }

        // ── Utilidades ────────────────────────────────────────────────────

        private async Task<bool> ConfirmarAsync(string titulo, string mensaje, string textoBoton = "Desvincular")
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = new TextBlock { Text = mensaje, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = textoBoton,
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private Task Avisar(string? error) =>
            error is null ? Task.CompletedTask : _notificacionService.MostrarNotificacionAsync("Error", error);
    }
}
