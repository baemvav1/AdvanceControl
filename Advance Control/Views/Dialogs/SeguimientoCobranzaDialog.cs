using System;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Diálogo "Agregar seguimiento" de cobranza (tipo de contacto, nota y promesa de pago).
    /// Lo usan Detalle Clientes y Cobranza; el RFC del cliente lo pone quien lo llama.
    /// </summary>
    public static class SeguimientoCobranzaDialog
    {
        public static readonly string[] TiposContacto = { "Llamada", "Visita", "Correo", "WhatsApp", "Otro" };

        /// <summary>Devuelve el seguimiento capturado, o null si se canceló o los datos no son válidos.</summary>
        public static async Task<CobranzaSeguimientoCreateDto?> MostrarAsync(XamlRoot xamlRoot, INotificacionService notificacionService)
        {
            var tipoComboBox = new ComboBox
            {
                PlaceholderText = "Selecciona un tipo",
                Margin = new Thickness(0, 0, 0, 8),
                ItemsSource = TiposContacto
            };

            var notaTextBox = new TextBox
            {
                PlaceholderText = "Nota del contacto...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                MaxHeight = 160,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var promesaMontoTextBox = new TextBox
            {
                PlaceholderText = "Monto de la promesa (opcional)",
                InputScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var promesaFechaPicker = new DatePicker();

            var dialogContent = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Tipo de contacto:" },
                    tipoComboBox,
                    new TextBlock { Text = "Nota:" },
                    notaTextBox,
                    new TextBlock { Text = "Promesa de pago (opcional):" },
                    promesaMontoTextBox,
                    promesaFechaPicker
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Agregar seguimiento",
                Content = dialogContent,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            if (tipoComboBox.SelectedItem is not string tipoContacto)
            {
                await notificacionService.MostrarAsync("Campo requerido", "Selecciona un tipo de contacto.");
                return null;
            }

            decimal? promesaMonto = null;
            if (!string.IsNullOrWhiteSpace(promesaMontoTextBox.Text))
            {
                if (!decimal.TryParse(promesaMontoTextBox.Text, out var monto))
                {
                    await notificacionService.MostrarAsync("Monto inválido", "El monto de la promesa no es un número válido.");
                    return null;
                }
                promesaMonto = monto;
            }

            // El DatePicker siempre trae una fecha seleccionada (hoy por defecto) aunque el usuario
            // no lo haya tocado, así que la fecha de promesa solo cuenta si además se capturó un monto.
            return new CobranzaSeguimientoCreateDto
            {
                TipoContacto = tipoContacto,
                Nota = string.IsNullOrWhiteSpace(notaTextBox.Text) ? null : notaTextBox.Text,
                PromesaPagoMonto = promesaMonto,
                PromesaPagoFecha = promesaMonto.HasValue ? promesaFechaPicker.SelectedDate?.Date : null
            };
        }
    }
}
