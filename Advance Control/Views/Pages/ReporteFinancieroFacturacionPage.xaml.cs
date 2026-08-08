using System;
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
            await CargarClientesHistorialSeguroAsync();
        }

        private void Filtro_TextChanged(object sender, TextChangedEventArgs e)
        {
            ProgramarRecargaLive();
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
            var clienteSeleccionado = ViewModel.ClienteHistorialSeleccionado;
            if (clienteSeleccionado == null || string.IsNullOrWhiteSpace(clienteSeleccionado.ReceptorRfc))
            {
                ViewModel.ErrorMessage = "Selecciona un cliente para generar el historial.";
                ViewModel.SuccessMessage = null;
                return;
            }

            try
            {
                var clientes = await _clienteService.GetClientesAsync(new ClienteQueryDto { Rfc = clienteSeleccionado.ReceptorRfc });
                var cliente = clientes?.FirstOrDefault(c => string.Equals(c.Rfc, clienteSeleccionado.ReceptorRfc, StringComparison.OrdinalIgnoreCase))
                    ?? clientes?.FirstOrDefault();

                if (cliente == null)
                {
                    ViewModel.ErrorMessage = $"No se encontró el cliente con RFC {clienteSeleccionado.ReceptorRfc}.";
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

                await ViewModel.GenerarHistorialAsync(cliente.IdCliente, dirigidoA);

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

        private async Task CargarClientesHistorialSeguroAsync()
        {
            try
            {
                await ViewModel.CargarClientesHistorialAsync();
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo cargar el listado de clientes para historial: {ex.Message}";
                ViewModel.SuccessMessage = null;
            }
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

