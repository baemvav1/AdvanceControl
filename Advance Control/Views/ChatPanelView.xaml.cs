using Advance_Control.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Advance_Control.Models;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Advance_Control.Views
{
    public sealed partial class ChatPanelView : UserControl
    {
        private static readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".pdf" };
        private bool _isSynchronizingUserListSelection;

        public ChatPanelViewModel ViewModel { get; }

        public ChatPanelView()
        {
            var services = ((App)Application.Current).Host.Services;
            ViewModel = services.GetRequiredService<ChatPanelViewModel>();
            this.InitializeComponent();
        }

        /// <summary>
        /// Inicializa el panel (debe llamarse después de que el UserControl esté cargado).
        /// </summary>
        public async void Initialize()
        {
            ViewModel.SetDispatcher(DispatcherQueue);
            try
            {
                await ViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ChatPanelView.Initialize: {ex}");
            }
        }

        private void ToggleUserList_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsUserListVisible = !ViewModel.IsUserListVisible;

            if (ViewModel.IsUserListVisible)
            {
                BuscadorUsuarios.Text = string.Empty;
                ViewModel.FiltrarUsuarios(string.Empty);
                SincronizarUsuarioSeleccionadoEnLista();
                _ = DispatcherQueue.TryEnqueue(() => BuscadorUsuarios.Focus(FocusState.Programmatic));
            }
        }

        private void BuscadorUsuarios_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                try
                {
                    ViewModel.FiltrarUsuarios(sender.Text);
                    SincronizarUsuarioSeleccionadoEnLista();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error en ChatPanel.BuscadorUsuarios_TextChanged: {ex}"); }
            }
        }

        private void UsuariosListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSynchronizingUserListSelection)
                return;

            if (UsuariosListView.SelectedItem is not UsuarioChatDto usuarioSeleccionado)
                return;

            if (ViewModel.UsuarioSeleccionado?.CredencialId == usuarioSeleccionado.CredencialId)
            {
                ViewModel.IsUserListVisible = false;
                return;
            }

            ViewModel.AbrirConversacion(usuarioSeleccionado);
            SincronizarUsuarioSeleccionadoEnLista();
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
                System.Diagnostics.Debug.WriteLine($"Error en ChatPanel.EnviarButton_Click: {ex}");
            }
        }

        private async void MensajeTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == global::Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                try { await ViewModel.EnviarMensajeAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error en ChatPanel.MensajeTextBox_KeyDown: {ex}"); }
            }
        }

        private async void MensajeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { await ViewModel.NotificarEscribiendoAsync(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error en ChatPanel.MensajeTextBox_TextChanged: {ex}"); }
        }

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
                System.Diagnostics.Debug.WriteLine($"Error en ChatPanel.ChatArea_Drop: {ex}");
            }
        }

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

        private async void BurbujaPdfButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url && !string.IsNullOrEmpty(url))
            {
                try { await global::Windows.System.Launcher.LaunchUriAsync(new Uri(url)); }
                catch { }
            }
        }

        private static bool EsArchivoValido(StorageFile file)
        {
            var ext = Path.GetExtension(file.Name)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(ext) && _extensionesPermitidas.Contains(ext);
        }

        private void SincronizarUsuarioSeleccionadoEnLista()
        {
            _isSynchronizingUserListSelection = true;
            try
            {
                var usuarioActual = ViewModel.UsuarioSeleccionado;
                UsuariosListView.SelectedItem = usuarioActual == null
                    ? null
                    : ViewModel.Usuarios.FirstOrDefault(u => u.CredencialId == usuarioActual.CredencialId);
            }
            finally
            {
                _isSynchronizingUserListSelection = false;
            }
        }

        // Helpers para x:Bind

        public string GetUsuarioIniciales(UsuarioChatDto? user) => user?.Iniciales ?? "";
        public string GetUsuarioNombre(UsuarioChatDto? user) => user?.NombreVisible ?? "";

        public Visibility GetUsuarioEnLineaVisibility(UsuarioChatDto? user)
            => (user?.EstaEnLinea ?? false) ? Visibility.Visible : Visibility.Collapsed;

        public Microsoft.UI.Xaml.Media.SolidColorBrush GetConnectionColor(bool connected)
        {
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(
                connected
                    ? global::Windows.UI.Color.FromArgb(255, 16, 185, 129)
                    : global::Windows.UI.Color.FromArgb(255, 239, 68, 68));
        }

    }
}
