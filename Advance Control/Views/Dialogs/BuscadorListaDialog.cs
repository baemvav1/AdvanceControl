using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Microsoft.UI.Xaml;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Abre <see cref="BuscadorListaUserControl"/> con una lista ya cargada (visible completa sin
    /// texto) en un <see cref="ModalPopup"/> y devuelve el <see cref="BuscadorListaItem.Dato"/> elegido,
    /// o null si se cancela.
    /// </summary>
    public static class BuscadorListaDialog
    {
        public static Task<object?> ElegirAsync(XamlRoot xamlRoot, string titulo, string placeholder, string vacio,
            IEnumerable<BuscadorListaItem> items)
        {
            var lista = items.ToList();
            var tcs = new TaskCompletionSource<object?>();
            Action? cerrar = null;
            var buscador = new BuscadorListaUserControl(titulo, placeholder, vacio, null,
                () => Task.FromResult(lista), mostrarTodoSinTexto: true);
            void Terminar(object? r) { if (tcs.Task.IsCompleted) return; cerrar?.Invoke(); tcs.TrySetResult(r); }
            buscador.Elegido += (_, item) => Terminar(item.Dato);
            buscador.Cancelado += (_, _) => Terminar(null);
            cerrar = ModalPopup.Abrir(xamlRoot, buscador, alEscape: () => Terminar(null));
            return tcs.Task;
        }
    }
}
