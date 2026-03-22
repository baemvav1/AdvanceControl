using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs
{
    public sealed class LevantamientoFallaDialog : ContentDialog
    {
        private readonly TextBox _descripcionTextBox;
        private readonly InfoBar _estadoInfoBar;

        public string DescripcionCapturada { get; private set; } = string.Empty;

        public LevantamientoFallaDialog(string tituloComponente, string? descripcionInicial, XamlRoot xamlRoot)
        {
            XamlRoot = xamlRoot;
            Title = tituloComponente;
            PrimaryButtonText = "Guardar";
            CloseButtonText = "Cancelar";
            DefaultButton = ContentDialogButton.Primary;
            MaxWidth = 680;

            var descripcionTituloTextBlock = new TextBlock
            {
                Text = "Describe la falla detectada en el componente seleccionado.",
                TextWrapping = TextWrapping.WrapWholeWords
            };

            _descripcionTextBox = new TextBox
            {
                AcceptsReturn = true,
                Height = 180,
                PlaceholderText = "Describe la falla detectada en este componente.",
                Text = descripcionInicial ?? string.Empty,
                TextWrapping = TextWrapping.Wrap
            };
            ScrollViewer.SetVerticalScrollBarVisibility(_descripcionTextBox, ScrollBarVisibility.Auto);

            _estadoInfoBar = new InfoBar
            {
                IsClosable = false,
                IsOpen = false,
                Severity = InfoBarSeverity.Error
            };

            Content = new StackPanel
            {
                Width = 600,
                Spacing = 12,
                Children =
                {
                    descripcionTituloTextBlock,
                    _descripcionTextBox,
                    _estadoInfoBar
                }
            };

            PrimaryButtonClick += OnPrimaryButtonClick;
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            _estadoInfoBar.IsOpen = false;

            var descripcion = _descripcionTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                _estadoInfoBar.Message = "Captura una descripcion de la falla antes de guardar.";
                _estadoInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            DescripcionCapturada = descripcion;
        }
    }
}
