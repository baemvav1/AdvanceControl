using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>Diálogos cortos del flujo de aprobaciones del cliente.</summary>
    public static class AprobacionDialogs
    {
        /// <summary>
        /// Registrar la aprobación del cliente desde Advance Control (por teléfono, en papel…):
        /// solo un comentario opcional; el login del técnico es la evidencia.
        /// </summary>
        /// <returns>(true, comentario) si confirmó; (false, null) si canceló.</returns>
        public static async Task<(bool Ok, string? Comentario)> RegistrarAprobacionAsync(XamlRoot xamlRoot, string queSeAprueba)
        {
            var comentario = new TextBox
            {
                PlaceholderText = "Comentario opcional (p. ej. \"Aprobó por teléfono\", \"Firmó la hoja en sitio\")",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                MaxLength = 2000
            };

            var dialogo = new ContentDialog
            {
                Title = "Registrar aprobación del cliente",
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Registra que el cliente aprobó {queSeAprueba}. Quedará a tu nombre, con fecha y hora.",
                            TextWrapping = TextWrapping.Wrap
                        },
                        comentario
                    }
                },
                PrimaryButtonText = "Aprobar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            return await dialogo.ShowAsync() == ContentDialogResult.Primary
                ? (true, string.IsNullOrWhiteSpace(comentario.Text) ? null : comentario.Text.Trim())
                : (false, null);
        }

        /// <summary>Monto antes de IVA que cubre la orden de compra (debe coincidir con la cotización).</summary>
        public static async Task<decimal?> SubtotalOrdenCompraAsync(XamlRoot xamlRoot, decimal? subtotalCotizacion)
        {
            var monto = new NumberBox
            {
                Header = "Monto que cubre la orden de compra (antes de IVA)",
                PlaceholderText = "0.00",
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
                Minimum = 0,
                NumberFormatter = new global::Windows.Globalization.NumberFormatting.DecimalFormatter { FractionDigits = 2, IsGrouped = true }
            };

            var texto = subtotalCotizacion.HasValue
                ? $"Debe coincidir exactamente con el subtotal de la cotización vigente ({subtotalCotizacion.Value:C2}). Si coincide, la orden de compra aprueba la cotización."
                : "Debe coincidir exactamente con el subtotal de la cotización vigente. Si coincide, la orden de compra aprueba la cotización.";

            var dialogo = new ContentDialog
            {
                Title = "Orden de compra",
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children = { new TextBlock { Text = texto, TextWrapping = TextWrapping.Wrap }, monto }
                },
                PrimaryButtonText = "Continuar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            if (await dialogo.ShowAsync() != ContentDialogResult.Primary || double.IsNaN(monto.Value) || monto.Value <= 0)
                return null;
            return System.Math.Round((decimal)monto.Value, 2);
        }
    }
}
