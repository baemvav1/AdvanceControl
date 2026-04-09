using System;
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

        public RPTFinancieroFacturacionViewModel ViewModel { get; }

        public ReporteFinancieroFacturacionPage()
        {
            InitializeComponent();
            ViewModel = AppServices.Get<RPTFinancieroFacturacionViewModel>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await CargarReporteSeguroAsync();
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

