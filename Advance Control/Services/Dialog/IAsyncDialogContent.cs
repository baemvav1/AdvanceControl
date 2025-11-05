using System.Threading.Tasks;

namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Interface for UserControls that need to perform async operations before the dialog closes
    /// </summary>
    public interface IAsyncDialogContent
    {
        /// <summary>
        /// Called when the primary button is clicked, before the dialog closes
        /// </summary>
        /// <returns>True if the dialog should close, false to keep it open</returns>
        Task<bool> OnPrimaryButtonClickAsync();
    }
}
