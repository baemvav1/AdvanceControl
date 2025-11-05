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

        /// <summary>
        /// Shows a dialog with the specified UserControl content and returns a result of type T
        /// </summary>
        public async Task<DialogResult<T>> ShowDialogAsync<T>(
            UserControl content,
            string title = "",
            string primaryButtonText = "OK",
            string secondaryButtonText = "Cancel",
            object? parameters = null)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (_xamlRoot == null)
                throw new InvalidOperationException("XamlRoot must be set before showing a dialog. Call SetXamlRoot first.");

            // Set parameters as DataContext if provided
            if (parameters != null)
            {
                content.DataContext = parameters;
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                XamlRoot = _xamlRoot
            };

            var result = await dialog.ShowAsync();

            // Return the result with the DataContext as the result value
            // The calling code can cast the result to the expected type
            return new DialogResult<T>(
                isConfirmed: result == ContentDialogResult.Primary,
                result: content.DataContext is T typedResult ? typedResult : default);
        }

        /// <summary>
        /// Shows a dialog with the specified UserControl content without returning a typed result
        /// </summary>
        public async Task<bool> ShowDialogAsync(
            UserControl content,
            string title = "",
            string primaryButtonText = "OK",
            string secondaryButtonText = "Cancel",
            object? parameters = null)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (_xamlRoot == null)
                throw new InvalidOperationException("XamlRoot must be set before showing a dialog. Call SetXamlRoot first.");

            // Set parameters as DataContext if provided
            if (parameters != null)
            {
                content.DataContext = parameters;
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                XamlRoot = _xamlRoot
            };

            var result = await dialog.ShowAsync();

            // Return true if primary button was clicked
            return result == ContentDialogResult.Primary;
        }
    }
}
