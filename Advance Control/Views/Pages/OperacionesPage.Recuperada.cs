using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Operaciones;
using Advance_Control.Services.OrdenServicio;
using Advance_Control.Utilities;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Botón "Op Recuperada": alta directa de una operación antigua (sin orden de servicio) que
    /// conserva su número original y se muestra como "R" + ese número (migración 142). Reutiliza
    /// NuevaOrdenServicioUserControl para tipo, equipo/inmueble, cliente relacionado y nota.
    /// </summary>
    public sealed partial class OperacionesPage
    {
        private async void OpRecuperada_Click(object sender, RoutedEventArgs e)
        {
            var operacionService = AppServices.Get<IOperacionService>();
            var ordenServicioService = AppServices.Get<IOrdenServicioService>();

            var numeroTextBox = new TextBox
            {
                Header = "Número original de la operación",
                PlaceholderText = "Ej. 1023 o A-1023",
                MaxLength = 40
            };
            var vistaPreviaText = new TextBlock
            {
                Text = "Se mostrará como: —",
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            numeroTextBox.TextChanged += (s, args) =>
            {
                var numero = numeroTextBox.Text.Trim().ToUpperInvariant();
                vistaPreviaText.Text = numero.Length == 0 ? "Se mostrará como: —" : $"Se mostrará como: R{numero}";
            };

            var ordenControl = new NuevaOrdenServicioUserControl();

            var fechaInicioPicker = new CalendarDatePicker
            {
                Header = "Fecha de inicio",
                Date = DateTimeOffset.Now.Date,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var fechaFinalPicker = new CalendarDatePicker
            {
                Header = "Fecha de cierre (vacía = sigue abierta)",
                PlaceholderText = "Sin cerrar",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var tecnicoCombo = new ComboBox
            {
                Header = "Técnico que atendió (opcional)",
                PlaceholderText = "Elige primero el equipo o inmueble",
                DisplayMemberPath = nameof(TecnicoDisponibleDto.NombreCompleto),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            string? identificadorTecnicos = null;
            tecnicoCombo.DropDownOpened += async (s, args) =>
            {
                var identificador = ordenControl.IdentificadorSeleccionado;
                if (string.IsNullOrWhiteSpace(identificador) || identificador == identificadorTecnicos) return;
                identificadorTecnicos = identificador;
                try
                {
                    var tecnicos = await ordenServicioService.GetTecnicosDisponiblesAsync(identificador, ordenControl.TipoObjetivoSeleccionado);
                    tecnicoCombo.ItemsSource = tecnicos;
                }
                catch (Exception)
                {
                    tecnicoCombo.ItemsSource = new List<TecnicoDisponibleDto>();
                }
            };

            var errorInfoBar = new InfoBar { Severity = InfoBarSeverity.Error, IsClosable = false, IsOpen = false };

            var fechasGrid = new Grid { ColumnSpacing = 12 };
            fechasGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fechasGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(fechaFinalPicker, 1);
            fechasGrid.Children.Add(fechaInicioPicker);
            fechasGrid.Children.Add(fechaFinalPicker);

            var contenido = new StackPanel { Spacing = 12, MinWidth = 520 };
            contenido.Children.Add(new TextBlock
            {
                Text = "Para operaciones antiguas que ya tienen su propio número. Se crea directo, sin orden de servicio; " +
                       "los cargos y fotos se agregan después como en cualquier operación.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            contenido.Children.Add(numeroTextBox);
            contenido.Children.Add(vistaPreviaText);
            contenido.Children.Add(ordenControl);
            contenido.Children.Add(fechasGrid);
            contenido.Children.Add(tecnicoCombo);
            contenido.Children.Add(errorInfoBar);

            var dialog = new ContentDialog
            {
                Title = "Operación recuperada",
                Content = new ScrollViewer { Content = contenido, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
                PrimaryButtonText = "Crear",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            string? folioCreado = null;

            dialog.PrimaryButtonClick += async (s, args) =>
            {
                var deferral = args.GetDeferral();
                try
                {
                    var error = ValidarRecuperada(numeroTextBox.Text, ordenControl, fechaInicioPicker.Date, fechaFinalPicker.Date);
                    if (error != null)
                    {
                        errorInfoBar.Message = error;
                        errorInfoBar.IsOpen = true;
                        args.Cancel = true;
                        return;
                    }

                    dialog.IsPrimaryButtonEnabled = false;
                    var dto = new OperacionRecuperadaCreateDto
                    {
                        IdTrasladado = numeroTextBox.Text.Trim(),
                        IdTipo = ordenControl.IdTipoMantenimiento!.Value,
                        IdCliente = ordenControl.IdCliente!.Value,
                        IdEquipo = ordenControl.IdEquipo ?? 0,
                        IdInmueble = ordenControl.IdInmueble ?? 0,
                        IdAtiende = (tecnicoCombo.SelectedItem as TecnicoDisponibleDto)?.CredencialId ?? 0,
                        Nota = ordenControl.Nota,
                        FechaInicio = fechaInicioPicker.Date?.Date,
                        FechaFinal = fechaFinalPicker.Date?.Date
                    };

                    var resultado = await operacionService.CrearRecuperadaAsync(dto);
                    if (!resultado.Success)
                    {
                        errorInfoBar.Message = resultado.Message ?? "No se pudo crear la operación recuperada.";
                        errorInfoBar.IsOpen = true;
                        args.Cancel = true;
                        dialog.IsPrimaryButtonEnabled = true;
                        return;
                    }

                    folioCreado = resultado.FolioOperacion;
                }
                finally
                {
                    deferral.Complete();
                }
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary || folioCreado == null) return;

            await ViewModel.LoadOperacionesAsync();
            await _notificacionService.MostrarAsync("Operación recuperada", $"Se creó la operación {folioCreado}. Ya puedes agregarle cargos y fotos.");
        }

        /// <summary>Devuelve el primer problema a corregir, o null si todo está completo.</summary>
        private static string? ValidarRecuperada(string numero, NuevaOrdenServicioUserControl orden, DateTimeOffset? fechaInicio, DateTimeOffset? fechaFinal)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return "Escribe el número original de la operación.";
            if (!orden.IdTipoMantenimiento.HasValue)
                return "Selecciona el tipo de mantenimiento.";
            if (!orden.IdEquipo.HasValue && !orden.IdInmueble.HasValue)
                return "Selecciona el equipo o inmueble.";
            if (!orden.IdCliente.HasValue)
                return "Selecciona el cliente.";
            if (!fechaInicio.HasValue)
                return "Indica la fecha de inicio.";
            if (fechaFinal.HasValue && fechaFinal.Value.Date < fechaInicio.Value.Date)
                return "La fecha de cierre no puede ser anterior a la de inicio.";
            return null;
        }
    }
}
