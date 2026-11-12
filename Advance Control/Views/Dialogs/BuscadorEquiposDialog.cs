using System.Threading.Tasks;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Abre el buscador de equipos como modal y devuelve el equipo elegido (o null si se cancela).
    /// Es un Popup con velo y no un ContentDialog porque WinUI solo permite un ContentDialog
    /// abierto a la vez, y el buscador se abre desde diálogos (nueva orden de servicio,
    /// relación refacción-equipo, operación recuperada).
    /// </summary>
    public static class BuscadorEquiposDialog
    {
        public static Task<EquipoBusquedaDto?> MostrarAsync(XamlRoot xamlRoot)
        {
            var tcs = new TaskCompletionSource<EquipoBusquedaDto?>();
            var buscador = new BuscadorEquiposUserControl();

            // Velo de pantalla completa con el buscador al centro
            var velo = new Grid
            {
                Width = xamlRoot.Size.Width,
                Height = xamlRoot.Size.Height,
                Background = (Brush)Application.Current.Resources["SmokeFillColorDefaultBrush"],
                Children = { buscador }
            };

            var popup = new Popup
            {
                Child = velo,
                XamlRoot = xamlRoot,
                IsLightDismissEnabled = false
            };

            void Ajustar(XamlRoot sender, XamlRootChangedEventArgs args)
            {
                velo.Width = sender.Size.Width;
                velo.Height = sender.Size.Height;
            }

            void Cerrar(EquipoBusquedaDto? resultado)
            {
                if (tcs.Task.IsCompleted) return;
                xamlRoot.Changed -= Ajustar;
                popup.IsOpen = false;
                tcs.TrySetResult(resultado);
            }

            xamlRoot.Changed += Ajustar;
            buscador.EquipoSeleccionado += (_, equipo) => Cerrar(equipo);
            buscador.Cancelado += (_, _) => Cerrar(null);
            velo.KeyDown += (_, e) =>
            {
                if (e.Key == VirtualKey.Escape)
                {
                    e.Handled = true;
                    Cerrar(null);
                }
            };
            popup.Closed += (_, _) => Cerrar(null);

            popup.IsOpen = true;
            return tcs.Task;
        }
    }
}
