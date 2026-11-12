using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Muestra un control como modal: Popup con velo de pantalla completa y el contenido al centro.
    /// Se usa en vez de ContentDialog porque WinUI solo permite un ContentDialog abierto a la vez,
    /// y estos modales se abren encima de otros diálogos (y unos encima de otros: buscador → nuevo equipo).
    /// </summary>
    public static class ModalPopup
    {
        /// <summary>
        /// Abre <paramref name="contenido"/> como modal. Devuelve la acción para cerrarlo.
        /// <paramref name="alEscape"/> se llama con Esc (normalmente para cancelar).
        /// </summary>
        public static Action Abrir(XamlRoot xamlRoot, UIElement contenido, Action? alEscape = null)
        {
            var velo = new Grid
            {
                Width = xamlRoot.Size.Width,
                Height = xamlRoot.Size.Height,
                Background = (Brush)Application.Current.Resources["SmokeFillColorDefaultBrush"],
                Children = { contenido }
            };

            var popup = new Popup { Child = velo, XamlRoot = xamlRoot, IsLightDismissEnabled = false };

            void Ajustar(XamlRoot sender, XamlRootChangedEventArgs args)
            {
                velo.Width = sender.Size.Width;
                velo.Height = sender.Size.Height;
            }

            var cerrado = false;
            void Cerrar()
            {
                if (cerrado) return;
                cerrado = true;
                xamlRoot.Changed -= Ajustar;
                popup.IsOpen = false;
            }

            xamlRoot.Changed += Ajustar;
            velo.KeyDown += (_, e) =>
            {
                if (e.Key == VirtualKey.Escape && alEscape is not null)
                {
                    e.Handled = true;
                    alEscape();
                }
            };

            popup.IsOpen = true;
            return Cerrar;
        }

        /// <summary>
        /// Marco con la apariencia de ContentDialog (fondo, borde, esquinas, sombra, título y padding 24)
        /// para contenidos que no traen el suyo.
        /// </summary>
        public static Border Marco(string titulo, UIElement contenido, double? maxAlto = null)
        {
            var res = Application.Current.Resources;
            return new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = (Brush)res["SolidBackgroundFillColorBaseBrush"],
                BorderBrush = (Brush)res["SurfaceStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(24),
                MaxHeight = maxAlto ?? double.PositiveInfinity,
                Translation = new System.Numerics.Vector3(0, 0, 32),
                Shadow = new ThemeShadow(),
                Child = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new StackPanel
                    {
                        Spacing = 16,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = titulo,
                                FontSize = 20,
                                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                            },
                            contenido
                        }
                    }
                }
            };
        }
    }
}
