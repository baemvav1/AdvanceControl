using Advance_Control.Models;
using Advance_Control.Utilities;
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
            try
            {
                await ViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MensajesPage.OnNavigatedTo: {ex}");
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            try
            {
                await ViewModel.CleanupAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MensajesPage.OnNavigatedFrom: {ex}");
            }
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
            try { await ViewModel.NotificarEscribiendoAsync(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error en MensajeTextBox_TextChanged: {ex}"); }
        }

        private void BuscadorUsuarios_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                try { ViewModel.FiltrarUsuarios(sender.Text); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error en BuscadorUsuarios_TextChanged: {ex}"); }
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

            try
            {
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ChatArea_Drop: {ex}");
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

        private async void BurbujaPdfButton_Click(object? sender, RoutedEventArgs e)
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

        // Helpers para x:Bind del header (evitan chaining nullable que puede fallar en WinUI 3)

        public string GetUsuarioIniciales(UsuarioChatDto? user) => user?.Iniciales ?? "";
        public string GetUsuarioNombre(UsuarioChatDto? user) => user?.NombreVisible ?? "";
        public Visibility GetUsuarioEnLineaVisibility(UsuarioChatDto? user)
            => (user?.EstaEnLinea ?? false) ? Visibility.Visible : Visibility.Collapsed;

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

        private void BurbujaOperacionReferencia_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MensajeDto mensaje)
                OperacionVisorNavigator.Navigate(mensaje);
        }
    }
}
