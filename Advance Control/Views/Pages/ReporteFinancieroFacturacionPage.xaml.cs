using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Advance_Control.Views.Pages
{
    public sealed partial class ReporteFinancieroFacturacionPage : Page
    {
        private readonly IClienteService _clienteService;
        private readonly IContactoService _contactoService;
        private readonly INotificacionService _notificacionService;

        public RPTFinancieroFacturacionViewModel ViewModel { get; }

        public ReporteFinancieroFacturacionPage()
        {
            InitializeComponent();
            ViewModel = AppServices.Get<RPTFinancieroFacturacionViewModel>();
            _clienteService = AppServices.Get<IClienteService>();
            _contactoService = AppServices.Get<IContactoService>();
            _notificacionService = AppServices.Get<INotificacionService>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarReporteAsync();
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarReporteAsync();
            ClientesListView.SelectedItem = ViewModel.ClienteSeleccionado;
        }

        private void ClientesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Al refiltrar el listado la selección se pierde; el cliente abierto se conserva
            // hasta que se elija otro.
            if (e.AddedItems.FirstOrDefault() is ReporteFinancieroFacturacionCabeceraDto cliente)
            {
                ViewModel.ClienteSeleccionado = cliente;
            }
        }

        private void PestanasSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            // IsSelected="True" en el XAML dispara este evento mientras se arma la página,
            // antes de que existan los paneles.
            if (PanelHistorial == null || PanelFacturas == null || PanelDocumentos == null)
                return;

            PanelHistorial.Visibility = sender.SelectedItem == PestanaHistorial ? Visibility.Visible : Visibility.Collapsed;
            PanelFacturas.Visibility = sender.SelectedItem == PestanaFacturas ? Visibility.Visible : Visibility.Collapsed;
            PanelDocumentos.Visibility = sender.SelectedItem == PestanaDocumentos ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------------- Historial ----------------

        private async void BtnAgregarSeguimiento_Click(object sender, RoutedEventArgs e)
        {
            var dto = await SeguimientoCobranzaDialog.MostrarAsync(XamlRoot, _notificacionService);
            if (dto == null)
            {
                return;
            }

            await ViewModel.AgregarSeguimientoAsync(dto);
        }

        // ---------------- Documentos ----------------

        private async void BtnGenerarReporte_Click(object sender, RoutedEventArgs e) =>
            await GenerarYAbrirAsync(ViewModel.GenerarReporteAsync);

        private async void BtnGenerarReporteSimplificado_Click(object sender, RoutedEventArgs e) =>
            await GenerarYAbrirAsync(ViewModel.GenerarReporteSimplificadoAsync);

        private async void BtnGenerarReporteCobranza_Click(object sender, RoutedEventArgs e) =>
            await GenerarYAbrirAsync(ViewModel.GenerarReporteCobranzaAsync);

        /// <summary>Genera el PDF y lo abre. El ViewModel ya deja el error en la barra si falla.</summary>
        private async Task GenerarYAbrirAsync(Func<Task<string>> generar)
        {
            try
            {
                var rutaArchivo = await generar();
                Process.Start(new ProcessStartInfo(rutaArchivo) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage ??= ex.Message;
                ViewModel.SuccessMessage = null;
            }
        }

        private async void BtnGenerarHistorial_Click(object sender, RoutedEventArgs e)
        {
            var cabecera = ViewModel.ClienteSeleccionado;
            if (cabecera == null || string.IsNullOrWhiteSpace(cabecera.ReceptorRfc))
            {
                ViewModel.ErrorMessage = "El cliente seleccionado no tiene RFC.";
                ViewModel.SuccessMessage = null;
                return;
            }

            if (!ViewModel.FechaInicioFiltro.HasValue || !ViewModel.FechaFinFiltro.HasValue)
            {
                ViewModel.ErrorMessage = "Selecciona Desde y Hasta en las opciones para generar el historial.";
                ViewModel.SuccessMessage = null;
                return;
            }

            if (ViewModel.FechaFinFiltro.Value.Date < ViewModel.FechaInicioFiltro.Value.Date)
            {
                ViewModel.ErrorMessage = "La fecha Hasta no puede ser anterior a Desde.";
                ViewModel.SuccessMessage = null;
                return;
            }

            if (!ViewModel.HistorialTieneEstadosMarcados)
            {
                ViewModel.ErrorMessage = "Marca al menos un estado en el menú \"Estados\" para generar el historial.";
                ViewModel.SuccessMessage = null;
                return;
            }

            try
            {
                var cliente = await BuscarClienteAsync(cabecera.ReceptorRfc);
                if (cliente == null)
                {
                    ViewModel.ErrorMessage = $"No se encontró el cliente con RFC {cabecera.ReceptorRfc} en el catálogo de clientes.";
                    ViewModel.SuccessMessage = null;
                    return;
                }

                string? dirigidoA = null;
                try
                {
                    var contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = cliente.IdCliente });
                    if (contactos?.Count > 0)
                    {
                        var lv = new ListView { ItemsSource = contactos, DisplayMemberPath = "NombreCompleto", SelectionMode = ListViewSelectionMode.Single, MaxHeight = 300 };
                        var sel = new ContentDialog
                        {
                            Title = "¿A quién van dirigidas las cotizaciones y reportes del historial?",
                            Content = new ScrollViewer { Content = lv, MaxHeight = 320 },
                            PrimaryButtonText = "Seleccionar",
                            SecondaryButtonText = "Omitir",
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.XamlRoot
                        };

                        if (await sel.ShowAsync() == ContentDialogResult.Primary && lv.SelectedItem is ContactoDto c)
                        {
                            dirigidoA = string.Join(" ", new[] { c.Tratamiento, c.Nombre, c.Apellido }.Where(s => !string.IsNullOrWhiteSpace(s)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewModel.ErrorMessage = $"No se pudieron cargar los contactos del cliente: {ex.Message}";
                }

                var nombreCliente = string.IsNullOrWhiteSpace(cliente.NombreComercial) ? cliente.RazonSocial : cliente.NombreComercial;
                await ViewModel.GenerarHistorialAsync(cliente.IdCliente, nombreCliente, dirigidoA);

                var carpeta = ViewModel.HistorialResultado?.CarpetaHistorial;
                if (!string.IsNullOrWhiteSpace(carpeta))
                {
                    Process.Start(new ProcessStartInfo(carpeta) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage ??= $"No se pudo generar el historial de cobranza: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }

        private async void BtnEnviarAlerta_Click(object sender, RoutedEventArgs e)
        {
            var cabecera = ViewModel.ClienteSeleccionado;
            if (cabecera == null || string.IsNullOrWhiteSpace(cabecera.ReceptorRfc))
            {
                ViewModel.ErrorMessage = "El cliente seleccionado no tiene RFC.";
                ViewModel.SuccessMessage = null;
                return;
            }

            try
            {
                var cliente = await BuscarClienteAsync(cabecera.ReceptorRfc);
                if (cliente == null)
                {
                    ViewModel.ErrorMessage = $"No se encontró el cliente con RFC {cabecera.ReceptorRfc}.";
                    ViewModel.SuccessMessage = null;
                    return;
                }

                var contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = cliente.IdCliente });
                var contactosConCorreo = (contactos ?? new List<ContactoDto>())
                    .Where(c => !string.IsNullOrWhiteSpace(c.Correo))
                    .Select(c => new ContactoAlertaItem(c.Correo!, $"{c.NombreCompleto} <{c.Correo}>"))
                    .ToList();

                if (contactosConCorreo.Count == 0)
                {
                    ViewModel.ErrorMessage = $"El cliente {cabecera.ReceptorNombreTexto} no tiene contactos con correo registrado.";
                    ViewModel.SuccessMessage = null;
                    return;
                }

                var lv = new ListView { ItemsSource = contactosConCorreo, SelectionMode = ListViewSelectionMode.Multiple, MaxHeight = 300 };
                var dialog = new ContentDialog
                {
                    Title = $"Contactos que recibirán la alerta de cobranza ({cabecera.ReceptorNombreTexto})",
                    Content = new ScrollViewer { Content = lv, MaxHeight = 320 },
                    PrimaryButtonText = "Enviar alerta",
                    SecondaryButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                {
                    return;
                }

                var destinatarios = lv.SelectedItems.Cast<ContactoAlertaItem>().Select(item => item.Correo).ToList();
                if (destinatarios.Count == 0)
                {
                    ViewModel.ErrorMessage = "Selecciona al menos un contacto para enviar la alerta.";
                    ViewModel.SuccessMessage = null;
                    return;
                }

                await ViewModel.EnviarAlertaCobranzaAsync(destinatarios);
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage ??= $"No se pudo enviar la alerta de cobranza: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }

        /// <summary>Cliente del catálogo con ese RFC (clientes.rfc puede repetirse: se toma el que coincide exacto).</summary>
        private async Task<CustomerDto?> BuscarClienteAsync(string rfc)
        {
            var clientes = await _clienteService.GetClientesAsync(new ClienteQueryDto { Rfc = rfc });
            return clientes?.FirstOrDefault(c => string.Equals(c.Rfc, rfc, StringComparison.OrdinalIgnoreCase))
                ?? clientes?.FirstOrDefault();
        }

        private sealed class ContactoAlertaItem
        {
            public ContactoAlertaItem(string correo, string texto)
            {
                Correo = correo;
                Texto = texto;
            }

            public string Correo { get; }
            public string Texto { get; }

            public override string ToString() => Texto;
        }
    }
}
