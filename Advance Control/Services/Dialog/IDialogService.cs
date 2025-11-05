using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Service for displaying generic dialogs with any UserControl and returning typed results
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Sets the XamlRoot for the dialog service (required for WinUI 3)
        /// Must be called before showing any dialogs
        /// </summary>
        void SetXamlRoot(XamlRoot xamlRoot);

        /// <summary>
        /// Shows a dialog with the specified UserControl content and returns a result of type T
        /// </summary>
        /// <typeparam name="T">The type of result to return</typeparam>
        /// <param name="content">The UserControl to display in the dialog</param>
        /// <param name="title">Optional title for the dialog</param>
        /// <param name="primaryButtonText">Optional text for primary button (default: "OK")</param>
        /// <param name="secondaryButtonText">Optional text for secondary button (default: "Cancel")</param>
        /// <returns>DialogResult containing the result and whether the dialog was confirmed</returns>
        Task<DialogResult<T>> ShowDialogAsync<T>(
            UserControl content,
            string title = "",
            string primaryButtonText = "OK",
            string secondaryButtonText = "Cancel");
    }
}
