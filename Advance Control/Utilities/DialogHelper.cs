using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Threading.Tasks;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Fábrica de ContentDialogs de uso frecuente.
    /// Elimina la duplicación de bloques de ContentDialog en las vistas.
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// Muestra el diálogo "Ya existe [tipo] — Abrir / Regenerar / Cancelar".
        /// Devuelve Primary=Abrir, Secondary=Regenerar, None=Cancelar.
        /// </summary>
        public static Task<ContentDialogResult> MostrarExisteAsync(
            XamlRoot xamlRoot,
            string tipo,
            string rutaArchivo)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = $"Ya existe {tipo}",
                Content = $"Ya existe {tipo} para esta operación:\n\n{Path.GetFileName(rutaArchivo)}\n\n¿Qué desea hacer?",
                PrimaryButtonText = "Abrir",
                SecondaryButtonText = "Regenerar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };
            return ShowDialogAsync(dialog);
        }

        /// <summary>
        /// Muestra un diálogo informativo con un solo botón "Cerrar".
        /// </summary>
        public static Task MostrarInfoAsync(XamlRoot xamlRoot, string titulo, string contenido)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = titulo,
                Content = contenido,
                CloseButtonText = "Cerrar"
            };
            return ShowDialogAsync(dialog);
        }

        /// <summary>
        /// Muestra un diálogo de confirmación "Sí / No".
        /// Devuelve true si el usuario presionó "Sí".
        /// </summary>
        public static async Task<bool> ConfirmarAsync(XamlRoot xamlRoot, string titulo, string contenido,
            string textSi = "Sí", string textNo = "No")
        {
            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = titulo,
                Content = contenido,
                PrimaryButtonText = textSi,
                CloseButtonText = textNo,
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await ShowDialogAsync(dialog).ConfigureAwait(false);
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// Muestra un ContentDialog y retorna el resultado vía TaskCompletionSource,
        /// evitando la necesidad de await sobre IAsyncOperation de WinRT.
        /// </summary>
        private static Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
        {
            var tcs = new TaskCompletionSource<ContentDialogResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var clicked = ContentDialogResult.None;
            dialog.PrimaryButtonClick += (_, _) => clicked = ContentDialogResult.Primary;
            dialog.SecondaryButtonClick += (_, _) => clicked = ContentDialogResult.Secondary;
            dialog.Closed += (_, _) => tcs.TrySetResult(clicked);

            // Inicia el diálogo sin await; el resultado se captura vía eventos
            _ = dialog.ShowAsync();
            return tcs.Task;
        }
    }
}
