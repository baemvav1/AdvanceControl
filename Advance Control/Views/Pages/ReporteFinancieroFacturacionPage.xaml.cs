using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Contactos;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Views.Pages
{
    public sealed partial class ReporteFinancieroFacturacionPage : Page
    {
        private const int LiveFilterDelayMs = 300;
        private CancellationTokenSource? _filtrosLiveCts;
        private bool _recargaLiveEnCurso;
        private bool _recargaLivePendiente;
        private List<CustomerDto> _allClientes = new();

        private readonly IClienteService _clienteService;
        private readonly IContactoService _contactoService;

        public RPTFinancieroFacturacionViewModel ViewModel { get; }

        public ReporteFinancieroFacturacionPage()
        {
            InitializeComponent();
            ViewModel = AppServices.Get<RPTFinancieroFacturacionViewModel>();
            _clienteService = AppServices.Get<IClienteService>();
            _contactoService = AppServices.Get<IContactoService>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await CargarReporteSeguroAsync();
            await CargarClientesAutoSuggestSeguroAsync();
        }

        private void Filtro_TextChanged(object sender, TextChangedEventArgs e)
        {
            ProgramarRecargaLive();
        }

        private async Task CargarClientesAutoSuggestSeguroAsync()
        {
            try
            {
                _allClientes = await _clienteService.GetClientesAsync(null);
            }
            catch (Exception ex)
            {
                _allClientes = new List<CustomerDto>();
                ViewModel.ErrorMessage = $"No se pudo cargar el listado de clientes para el buscador: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }

        private void ClienteAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }

            var texto = sender.Text?.Trim() ?? string.Empty;
            ViewModel.ReceptorRfcFiltro = texto;

            sender.ItemsSource = string.IsNullOrWhiteSpace(texto)
                ? _allClientes.Take(10).Select(FormatClienteDisplay).ToList()
                : _allClientes
                    .Where(c =>
                        (!string.IsNullOrEmpty(c.Rfc) && c.Rfc.Contains(texto, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.NombreComercial) && c.NombreComercial.Contains(texto, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.RazonSocial) && c.RazonSocial.Contains(texto, StringComparison.OrdinalIgnoreCase)))
                    .Take(10)
                    .Select(FormatClienteDisplay)
                    .ToList();

            ProgramarRecargaLive();
        }

        private void ClienteAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox autoSuggestBox)
            {
                autoSuggestBox.ItemsSource = _allClientes.Take(10).Select(FormatClienteDisplay).ToList();
                autoSuggestBox.IsSuggestionListOpen = true;
            }
        }

        private void ClienteAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is not string selectedText)
            {
                return;
            }

            var cliente = _allClientes.FirstOrDefault(c => FormatClienteDisplay(c) == selectedText);
            if (cliente == null)
            {
                return;
            }

            sender.Text = cliente.Rfc;
            ViewModel.ReceptorRfcFiltro = cliente.Rfc;
            ProgramarRecargaLive();
        }

        private static string FormatClienteDisplay(CustomerDto cliente)
        {
            return string.IsNullOrWhiteSpace(cliente.NombreComercial)
                ? cliente.Rfc
                : $"{cliente.Rfc} — {cliente.NombreComercial}";
        }

        private void FiltroFecha_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            ProgramarRecargaLive();
        }

        private void FiltroEstado_Changed(object sender, RoutedEventArgs e)
        {
            ProgramarRecargaLive();
        }

        private async void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarFiltros();
            ClienteAutoSuggestBox.Text = string.Empty;
            ProgramarRecargaLive();
        }

        private async void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rutaArchivo = await ViewModel.GenerarReporteAsync();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rutaArchivo)
                {
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo generar el reporte financiero de facturación: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }

        private async void BtnGenerarReporteSimplificado_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rutaArchivo = await ViewModel.GenerarReporteSimplificadoAsync();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rutaArchivo)
                {
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo generar el reporte simplificado: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }

        private async void BtnGenerarReporteCobranza_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rutaArchivo = await ViewModel.GenerarReporteCobranzaAsync();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rutaArchivo)
                {
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo generar el reporte de cobranza: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }

        private async void BtnGenerarHistorial_Click(object sender, RoutedEventArgs e)
        {
            var rfcFiltro = ViewModel.ReceptorRfcFiltro;
            if (string.IsNullOrWhiteSpace(rfcFiltro))
            {
                ViewModel.ErrorMessage = "Selecciona un cliente (RFC o nombre) arriba para generar el historial.";
                ViewModel.SuccessMessage = null;
                return;
            }

            if (!ViewModel.FechaInicioFiltro.HasValue || !ViewModel.FechaFinFiltro.HasValue)
            {
                ViewModel.ErrorMessage = "Selecciona la Fecha Inicio y Fecha Fin arriba para generar el historial.";
                ViewModel.SuccessMessage = null;
                return;
            }

            try
            {
                var clientes = await _clienteService.GetClientesAsync(new ClienteQueryDto { Rfc = rfcFiltro });
                var cliente = clientes?.FirstOrDefault(c => string.Equals(c.Rfc, rfcFiltro, StringComparison.OrdinalIgnoreCase))
                    ?? clientes?.FirstOrDefault();

                if (cliente == null)
                {
                    ViewModel.ErrorMessage = $"No se encontró el cliente con RFC {rfcFiltro}. Selecciónalo desde el buscador de arriba.";
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
                ViewModel.ErrorMessage = $"No se pudo generar el historial de cobranza: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }

        private async void CabeceraItem_EnviarAlertaClick(object sender, ReporteFinancieroFacturacionCabeceraDto cabecera)
        {
            if (cabecera == null || string.IsNullOrWhiteSpace(cabecera.ReceptorRfc))
            {
                ViewModel.ErrorMessage = "El cliente seleccionado no tiene RFC.";
                ViewModel.SuccessMessage = null;
                return;
            }

            try
            {
                var clientes = await _clienteService.GetClientesAsync(new ClienteQueryDto { Rfc = cabecera.ReceptorRfc });
                var cliente = clientes?.FirstOrDefault(c => string.Equals(c.Rfc, cabecera.ReceptorRfc, StringComparison.OrdinalIgnoreCase))
                    ?? clientes?.FirstOrDefault();

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

                await ViewModel.EnviarAlertaCobranzaAsync(cabecera, destinatarios);
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo enviar la alerta de cobranza: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
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

        private void ProgramarRecargaLive()
        {
            _filtrosLiveCts?.Cancel();
            _filtrosLiveCts?.Dispose();
            _filtrosLiveCts = new CancellationTokenSource();

            _ = EjecutarRecargaLiveAsync(_filtrosLiveCts.Token);
        }

        private async Task EjecutarRecargaLiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(LiveFilterDelayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await CargarReporteLiveSerializadoAsync();
        }

        private async Task CargarReporteLiveSerializadoAsync()
        {
            if (_recargaLiveEnCurso)
            {
                _recargaLivePendiente = true;
                return;
            }

            _recargaLiveEnCurso = true;

            try
            {
                do
                {
                    _recargaLivePendiente = false;
                    await CargarReporteSeguroAsync();
                }
                while (_recargaLivePendiente);
            }
            finally
            {
                _recargaLiveEnCurso = false;
            }
        }

        private async Task CargarReporteSeguroAsync()
        {
            try
            {
                await ViewModel.CargarReporteAsync();
            }
            catch (System.Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo cargar el reporte financiero de facturación: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
        }
    }
}

