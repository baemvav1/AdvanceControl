using Advance_Control.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Pages
{
    public sealed partial class MensajesPage : Page
    {
        private static readonly string[] _extensionesImagen = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        private static readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".pdf" };

        public MensajesViewModel ViewModel { get; }

        public MensajesPage()
        {
            var services = ((App)Application.Current).Host.Services;
            ViewModel = services.GetRequiredService<MensajesViewModel>();
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.SetDispatcher(DispatcherQueue);
            await ViewModel.InitializeAsync();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            await ViewModel.CleanupAsync();
        }

        private async void EnviarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.EnviarMensajeAsync();
                MensajeTextBox.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EnviarButton_Click: {ex}");
            }
        }

        private async void MensajeTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == global::Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                try
                {
                    await ViewModel.EnviarMensajeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en MensajeTextBox_KeyDown: {ex}");
                }
            }
        }

        private async void MensajeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await ViewModel.NotificarEscribiendoAsync();
        }

        private void BuscadorUsuarios_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var filter = sender.Text?.ToLower() ?? "";
                foreach (var usuario in ViewModel.Usuarios)
                {
                    // Filtrado visual simple
                }
            }
        }

        // ── Drag & Drop de imágenes ──

        private void ChatArea_DragOver(object sender, DragEventArgs e)
        {
            if (ViewModel.UsuarioSeleccionado == null || !ViewModel.EstaConectado) return;

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Enviar archivo";
                e.DragUIOverride.IsGlyphVisible = true;
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        private async void ChatArea_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.UsuarioSeleccionado == null || !ViewModel.EstaConectado) return;
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            var items = await e.DataView.GetStorageItemsAsync();
            foreach (var item in items)
            {
                if (item is StorageFile file && EsArchivoValido(file))
                {
                    using var stream = await file.OpenStreamForReadAsync();
                    await ViewModel.EnviarArchivoAsync(stream, file.Name, file.ContentType);
                }
            }
        }

        // ── Botón adjuntar ──

        private async void AdjuntarButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.UsuarioSeleccionado == null) return;

            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            foreach (var ext in _extensionesPermitidas)
                picker.FileTypeFilter.Add(ext);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            using var stream = await file.OpenStreamForReadAsync();
            await ViewModel.EnviarArchivoAsync(stream, file.Name, file.ContentType);
        }

        private static bool EsArchivoValido(StorageFile file)
        {
            var ext = Path.GetExtension(file.Name)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(ext) && _extensionesPermitidas.Contains(ext);
        }

        private async void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    await global::Windows.System.Launcher.LaunchUriAsync(new Uri(url));
                }
                catch { }
            }
        }

        // Funciones helper para x:Bind

        public Microsoft.UI.Xaml.Media.SolidColorBrush GetConnectionColor(bool connected)
        {
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(
                connected
                    ? global::Windows.UI.Color.FromArgb(255, 16, 185, 129)
                    : global::Windows.UI.Color.FromArgb(255, 239, 68, 68));
        }

        public Visibility GetEmptyVisibility(bool hayUsuarioSeleccionado)
        {
            return hayUsuarioSeleccionado ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
