using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Service for displaying generic dialogs with any UserControl content
    /// </summary>
    public class DialogService : IDialogService
    {
        private XamlRoot? _xamlRoot;

        public DialogService(XamlRoot? xamlRoot = null)
        {
            _xamlRoot = xamlRoot;
        }

        /// <summary>
        /// Sets the XamlRoot for the dialog service (required for WinUI 3)
        /// </summary>
        public void SetXamlRoot(XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot ?? throw new ArgumentNullException(nameof(xamlRoot));
        }

        public async Task<DialogResult<T>> ShowDialogAsync<T>(
            UserControl content,
            string title = "",
            string primaryButtonText = "OK",
            string secondaryButtonText = "Cancel")
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (_xamlRoot == null)
                throw new InvalidOperationException("XamlRoot must be set before showing a dialog. Call SetXamlRoot first.");

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                XamlRoot = _xamlRoot
            };

            // Handle primary button click for async operations
            if (content is IAsyncDialogContent asyncContent)
            {
                dialog.PrimaryButtonClick += async (sender, args) =>
                {
                    // Defer closing to allow async operation
                    var deferral = args.GetDeferral();
                    try
                    {
                        var shouldClose = await asyncContent.OnPrimaryButtonClickAsync();
                        
                        // If operation failed, prevent dialog from closing
                        if (!shouldClose)
                        {
                            args.Cancel = true;
                        }
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                };
            }

            var result = await dialog.ShowAsync();

            // Check if the content implements IDialogContent<T> to get the result
            if (content is IDialogContent<T> dialogContent)
            {
                return new DialogResult<T>(
                    isConfirmed: result == ContentDialogResult.Primary,
                    result: dialogContent.GetResult());
            }

            // Default behavior: return default value for T
            return new DialogResult<T>(
                isConfirmed: result == ContentDialogResult.Primary,
                result: default);
        }
    }
}
