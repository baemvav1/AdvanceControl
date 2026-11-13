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

            // "Copiar producción a Pruebas" solo existe en el entorno de pruebas (el API de
            // producción además lo rechaza con 403).
            if (Advance_Control.Utilities.EntornoApp.EsPruebas)
            {
                RefrescoPruebasPanel.Visibility = Visibility.Visible;
                Loaded += async (_, _) => await RetomarRefrescoSiCorreAsync();
            }
        }

        private bool _siguiendoRefresco;

        private async void OnRefrescarPruebasClick(object sender, RoutedEventArgs e)
        {
            if (!Advance_Control.Utilities.EntornoApp.EsPruebas) return;

            var dialogo = new ContentDialog
            {
                Title = "Copiar producción a Pruebas",
                Content = "Se BORRA todo lo que hay en la base de Pruebas y se reemplaza con una copia " +
                          "fresca de producción (anonimizada), junto con sus fotos y documentos.\n\n" +
                          "Tarda un par de minutos. Al terminar se reinicia el API de pruebas y todos " +
                          "tendrán que volver a iniciar sesión en Pruebas.\n\n" +
                          "Producción no se modifica.",
                PrimaryButtonText = "Copiar ahora",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            if (await dialogo.ShowAsync() != ContentDialogResult.Primary) return;

            var servicio = ((App)Application.Current).Host.Services.GetRequiredService<Services.DevOps.IDevOpsService>();
            try
            {
                MostrarRefresco(true, "Iniciando…");
                await servicio.IniciarRefrescoPruebasAsync();
            }
            catch (Exception ex)
            {
                MostrarRefresco(false, $"No se pudo iniciar: {ex.Message}");
                return;
            }
            await SeguirRefrescoAsync(servicio);
        }

        /// <summary>Si se abrió la página con una copia en curso, sigue mostrando su avance.</summary>
        private async System.Threading.Tasks.Task RetomarRefrescoSiCorreAsync()
        {
            var servicio = ((App)Application.Current).Host.Services.GetRequiredService<Services.DevOps.IDevOpsService>();
            try
            {
                var estado = await servicio.ObtenerEstadoRefrescoPruebasAsync();
                if (estado.Corriendo) await SeguirRefrescoAsync(servicio);
                else if (estado.Estado == "ok" && estado.Fin is DateTime fin)
                    RefrescoEstadoText.Text = $"Última copia: {fin.ToLocalTime():dd/MM/yyyy HH:mm}";
                else if (estado.Estado == "error")
                    RefrescoEstadoText.Text = $"La última copia falló en \"{estado.Paso}\": {estado.Mensaje}";
            }
            catch { /* sin estado: no se muestra nada */ }
        }

        /// <summary>Consulta el avance cada 3 s hasta que termine (el API se reinicia al final).</summary>
        private async System.Threading.Tasks.Task SeguirRefrescoAsync(Services.DevOps.IDevOpsService servicio)
        {
            if (_siguiendoRefresco) return;
            _siguiendoRefresco = true;
            try
            {
                var limite = DateTime.Now.AddMinutes(30);
                while (DateTime.Now < limite)
                {
                    await System.Threading.Tasks.Task.Delay(3000);
                    Services.DevOps.DevOpsRefrescoEstado estado;
                    try { estado = await servicio.ObtenerEstadoRefrescoPruebasAsync(); }
                    catch { MostrarRefresco(true, "Esperando al servidor (se está reiniciando)…"); continue; }

                    if (estado.Corriendo)
                    {
                        MostrarRefresco(true, string.IsNullOrEmpty(estado.Paso) ? "Copiando…" : estado.Paso + "…");
                        continue;
                    }

                    if (estado.Estado == "ok")
                    {
                        MostrarRefresco(false, "Listo: Pruebas tiene datos frescos de producción.");
                        await new ContentDialog
                        {
                            Title = "Copia terminada",
                            Content = "Pruebas ya tiene una copia fresca de producción.\n\nCierra sesión y vuelve a entrar para seguir trabajando en Pruebas.",
                            CloseButtonText = "Aceptar",
                            XamlRoot = XamlRoot
                        }.ShowAsync();
                    }
                    else
                    {
                        MostrarRefresco(false, $"Falló en \"{estado.Paso}\": {estado.Mensaje}");
                    }
                    return;
                }
                MostrarRefresco(false, "La copia sigue sin terminar después de 30 minutos; revisa el servidor.");
            }
            finally
            {
                _siguiendoRefresco = false;
            }
        }

        private void MostrarRefresco(bool corriendo, string texto)
        {
            BtnRefrescarPruebas.IsEnabled = !corriendo;
            RefrescoProgreso.IsActive = corriendo;
            RefrescoProgreso.Visibility = corriendo ? Visibility.Visible : Visibility.Collapsed;
            RefrescoEstadoText.Text = texto;
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

        private async void OnDesvincularFacturasClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirmar desvinculación de facturas",
                Content = "Se desvincularán TODAS las facturas de sus operaciones.\n\n" +
                    "Las facturas NO se borran: se conservan y vuelven a aparecer en \"Sin operación\" " +
                    "para poder volver a vincularse (manual o con \"Vincular facturas automáticamente\" " +
                    "en Facturación). Las operaciones correspondientes vuelven a quedar pendientes " +
                    "(no finalizadas).",
                PrimaryButtonText = "Desvincular todas",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DesvincularFacturasAsync();
            }
        }

        private async void OnCargarEstadisticasClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.CargarEstadisticasAsync();
        }

        private async void OnEnviarMensajePruebaClick(object sender, RoutedEventArgs e)
        {
            var emisorBox = new NumberBox
            {
                Header = "ID Emisor (credencial)",
                Value = ViewModel.CredencialIdActual,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Minimum = 1,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var receptorBox = new NumberBox
            {
                Header = "ID Receptor (credencial)",
                PlaceholderText = "Ej: 2",
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Minimum = 1,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var mensajeBox = new TextBox
            {
                Header = "Mensaje",
                PlaceholderText = "Escribe el mensaje de prueba...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 100
            };

            var panel = new StackPanel { Width = 360 };
            panel.Children.Add(emisorBox);
            panel.Children.Add(receptorBox);
            panel.Children.Add(mensajeBox);

            var dialog = new ContentDialog
            {
                Title = "Enviar Mensaje de Prueba",
                Content = panel,
                PrimaryButtonText = "Enviar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary) return;

            if (double.IsNaN(emisorBox.Value) || emisorBox.Value < 1)
            {
                await MostrarError("Debes ingresar un ID de emisor válido.");
                return;
            }

            if (double.IsNaN(receptorBox.Value) || receptorBox.Value < 1)
            {
                await MostrarError("Debes ingresar un ID de receptor válido.");
                return;
            }

            var mensaje = mensajeBox.Text?.Trim();
            if (string.IsNullOrEmpty(mensaje))
            {
                await MostrarError("El mensaje no puede estar vacío.");
                return;
            }

            await ViewModel.EnviarMensajePruebaAsync((long)emisorBox.Value, (long)receptorBox.Value, mensaje);
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
