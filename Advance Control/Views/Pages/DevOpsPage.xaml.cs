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
                "También se borrarán todos los archivos físicos asociados (imágenes y PDFs de " +
                "prefacturas, hojas de servicio, órdenes de compra y facturas).\n\n" +
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
                "También se borrarán todos los archivos físicos de operaciones (imágenes y PDFs de " +
                "prefacturas, hojas de servicio, órdenes de compra y facturas).\n\n" +
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

        private async void OnLimpiarUbicacionesClick(object sender, RoutedEventArgs e)
        {
            if (await ConfirmarLimpieza("Ubicaciones y Areas",
                "Se eliminarán TODAS las areas, coordenadas, marcadores, ubicaciones y relaciones usuario-area.\n\n" +
                "Los equipos perderán su ubicacion asignada.\n\n" +
                "Esta acción NO se puede deshacer."))
            {
                await ViewModel.EjecutarLimpiezaAsync("ubicaciones");
            }
        }

        private async void OnLimpiarPermisosClick(object sender, RoutedEventArgs e)
        {
            if (await ConfirmarLimpieza("Permisos",
                "Se eliminarán TODOS los permisos de módulo y acción de la base de datos " +
                "y se regenerarán automáticamente desde la aplicación.\n\n" +
                "⚠️ Todos los niveles configurados manualmente se perderán y volverán al valor " +
                "por defecto (nivel 8 – más restrictivo). Deberás re-ajustarlos después.\n\n" +
                "Esta acción NO se puede deshacer."))
            {
                await ViewModel.EjecutarLimpiezaAsync("permisos");
            }
        }

        private async void OnLimpiarConciliacionRangoClick(object sender, RoutedEventArgs e)
        {
            var fechaInicioLabel = new TextBlock { Text = "Fecha inicio:", Margin = new Thickness(0, 0, 0, 4) };
            var pickerInicio = new CalendarDatePicker
            {
                PlaceholderText = "Selecciona fecha de inicio",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 12)
            };
            var fechaFinLabel = new TextBlock { Text = "Fecha fin:", Margin = new Thickness(0, 0, 0, 4) };
            var pickerFin = new CalendarDatePicker
            {
                PlaceholderText = "Selecciona fecha de fin",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var advertencia = new TextBlock
            {
                Text = "⚠️ Se eliminarán TODAS las facturas, estados de cuenta, movimientos y " +
                       "datos de conciliación dentro del rango. Esta acción NO se puede deshacer.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 16, 0, 0),
                Opacity = 0.8
            };

            var panel = new StackPanel { Width = 320 };
            panel.Children.Add(fechaInicioLabel);
            panel.Children.Add(pickerInicio);
            panel.Children.Add(fechaFinLabel);
            panel.Children.Add(pickerFin);
            panel.Children.Add(advertencia);

            var dialog = new ContentDialog
            {
                Title = "Borrar Conciliación por Rango de Fechas",
                Content = panel,
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary) return;

            if (pickerInicio.Date == null || pickerFin.Date == null)
            {
                await MostrarError("Debes seleccionar ambas fechas.");
                return;
            }

            var fechaInicio = pickerInicio.Date.Value.DateTime;
            var fechaFin = pickerFin.Date.Value.DateTime;

            if (fechaFin < fechaInicio)
            {
                await MostrarError("La fecha de fin debe ser mayor o igual a la fecha de inicio.");
                return;
            }

            await ViewModel.EjecutarLimpiezaRangoAsync(fechaInicio, fechaFin);
        }

        private async System.Threading.Tasks.Task MostrarError(string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = "Error de validación",
                Content = mensaje,
                CloseButtonText = "Aceptar",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
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
