using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página de herramientas DevOps para limpieza de datos por módulo.
    /// </summary>
    public sealed partial class DevOpsPage : Page
    {
        public DevOpsViewModel ViewModel { get; }

        public DevOpsPage()
        {
            var services = ((App)Application.Current).Host.Services;
            ViewModel = services.GetRequiredService<DevOpsViewModel>();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private async void OnLimpiarFinancieroClick(object sender, RoutedEventArgs e)
        {
            if (await ConfirmarLimpieza("Financiero",
                "Se eliminarán TODAS las facturas, conceptos, traslados, abonos, " +
                "bitácora de conciliación, estados de cuenta y movimientos.\n\n" +
                "Esta acción NO se puede deshacer."))
            {
                await ViewModel.EjecutarLimpiezaAsync("financiero");
            }
        }

        private async void OnLimpiarOperacionesClick(object sender, RoutedEventArgs e)
        {
            if (await ConfirmarLimpieza("Operaciones",
                "Se eliminarán TODAS las operaciones, cargos, checks y relaciones.\n\n" +
                "Esta acción NO se puede deshacer."))
            {
                await ViewModel.EjecutarLimpiezaAsync("operaciones");
            }
        }

        private async void OnLimpiarMantenimientoClick(object sender, RoutedEventArgs e)
        {
            if (await ConfirmarLimpieza("Mantenimiento",
                "Se eliminarán TODOS los mantenimientos y las operaciones que dependen de ellos.\n\n" +
                "Los tipos de mantenimiento (catálogo) se preservan.\n\n" +
                "Esta acción NO se puede deshacer."))
            {
                await ViewModel.EjecutarLimpiezaAsync("mantenimiento");
            }
        }

        private async void OnLimpiarLevantamientosClick(object sender, RoutedEventArgs e)
        {
            if (await ConfirmarLimpieza("Levantamientos",
                "Se eliminarán TODOS los levantamientos y sus nodos.\n\n" +
                "Esta acción NO se puede deshacer."))
            {
                await ViewModel.EjecutarLimpiezaAsync("levantamientos");
            }
        }

        private async void OnLimpiarServiciosClick(object sender, RoutedEventArgs e)
        {
            if (await ConfirmarLimpieza("Servicios",
                "Se eliminarán TODOS los servicios registrados.\n\n" +
                "Esta acción NO se puede deshacer."))
            {
                await ViewModel.EjecutarLimpiezaAsync("servicios");
            }
        }

        private async void OnLimpiarLogsClick(object sender, RoutedEventArgs e)
        {
            if (await ConfirmarLimpieza("Logs y Actividad",
                "Se eliminarán TODOS los logs, actividades, notificaciones y errores.\n\n" +
                "Esta acción NO se puede deshacer."))
            {
                await ViewModel.EjecutarLimpiezaAsync("logs");
            }
        }

        private async void OnCargarEstadisticasClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarEstadisticasAsync();
        }

        /// <summary>
        /// Muestra un diálogo de confirmación antes de ejecutar una limpieza.
        /// </summary>
        private async System.Threading.Tasks.Task<bool> ConfirmarLimpieza(string modulo, string detalle)
        {
            var dialog = new ContentDialog
            {
                Title = $"⚠️ Confirmar borrado: {modulo}",
                Content = detalle,
                PrimaryButtonText = "Eliminar todo",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
    }
}
