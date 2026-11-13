using System.Threading.Tasks;
using Advance_Control.Models;
using Microsoft.UI.Xaml;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Abre el buscador de equipos como modal y devuelve el equipo elegido (o null si se cancela).
    /// Usa <see cref="ModalPopup"/> (no ContentDialog: se abre desde otros diálogos).
    /// </summary>
    public static class BuscadorEquiposDialog
    {
        public static Task<EquipoBusquedaDto?> MostrarAsync(XamlRoot xamlRoot)
        {
            var tcs = new TaskCompletionSource<EquipoBusquedaDto?>();
            var buscador = new BuscadorEquiposUserControl();
            System.Action? cerrar = null;

            void Terminar(EquipoBusquedaDto? resultado)
            {
                if (tcs.Task.IsCompleted) return;
                cerrar?.Invoke();
                tcs.TrySetResult(resultado);
            }

            buscador.EquipoSeleccionado += (_, equipo) => Terminar(equipo);
            buscador.Cancelado += (_, _) => Terminar(null);
            cerrar = ModalPopup.Abrir(xamlRoot, buscador, alEscape: () => Terminar(null));
            return tcs.Task;
        }
    }
}
