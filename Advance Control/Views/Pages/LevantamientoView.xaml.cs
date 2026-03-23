using System;
using System.IO;
using Advance_Control.Models;
using Advance_Control.Services.ImageViewer;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página base para el módulo de levantamiento.
    /// </summary>
        public sealed partial class LevantamientoView : Page
        {
            private const bool ShowHotspotButtons = true;
            private readonly ILoggingService _logger;
            private readonly ILevantamientoImageService _imageService;
            private readonly IImageViewerService _imageViewerService;

            public LevantamientoViewModel ViewModel { get; }

        public Visibility HotspotButtonVisibility => ShowHotspotButtons
            ? Visibility.Visible
            : Visibility.Collapsed;

        public LevantamientoView()
        {
            var services = ((App)Application.Current).Host.Services;
            ViewModel = services.GetRequiredService<LevantamientoViewModel>();
            _logger = services.GetRequiredService<ILoggingService>();
            _imageService = services.GetRequiredService<ILevantamientoImageService>();
            _imageViewerService = services.GetRequiredService<IImageViewerService>();

            InitializeComponent();
            ButtonClickLogger.Attach(this, _logger, nameof(LevantamientoView));

            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeAsync();

            // Si se navego con un IdLevantamiento, cargar los datos existentes
            if (e.Parameter is int idLevantamiento && idLevantamiento > 0)
            {
                await ViewModel.CargarLevantamientoAsync(idLevantamiento);

                // Rellenar los campos de texto con los valores cargados
                if (ViewModel.IntroduccionCargada is not null)
                    ImputIntroduccion.Text = ViewModel.IntroduccionCargada;
                if (ViewModel.ConclusionCargada is not null)
                    ImputConclusion.Text = ViewModel.ConclusionCargada;

                // Seleccionar el equipo en el AutoSuggestBox
                if (ViewModel.TieneEquipoSeleccionado)
                    EquipoAutoSuggestBox.Text = $"{ViewModel.EquipoSeleccionadoIdentificador} - {ViewModel.EquipoSeleccionadoMarca}";
            }
        }

        private async void HotspotButton_Click(object sender, RoutedEventArgs e)
        {
            var hotspotTag = (sender as FrameworkElement)?.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(hotspotTag))
            {
                await ShowMessageAsync("Hotspot invalido", "No se pudo identificar el componente seleccionado.");
                return;
            }

            var hotspot = ViewModel.TryGetHotspotByVisualTag(hotspotTag);
            if (hotspot is null)
            {
                await ShowMessageAsync("Componente no configurado", $"No existe un mapeo para el tag '{hotspotTag}'.");
                return;
            }

            ViewModel.SelectHotspot(hotspot);

            var dialog = new Views.Dialogs.LevantamientoFallaDialog(hotspot.Titulo, hotspot.DescripcionFalla, XamlRoot);
            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            ViewModel.RegisterFailure(hotspot, dialog.DescripcionCapturada);
        }

        private void EliminarCaptura_Click(object sender, RoutedEventArgs e)
        {
            var clave = (sender as FrameworkElement)?.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(clave))
            {
                return;
            }

            ViewModel.RemoveFailure(clave);
        }

        #region Selector de equipo

        private void EquipoAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.FiltrarEquipos(sender.Text);
                sender.ItemsSource = ViewModel.EquiposSugeridos;
            }
        }

        private void EquipoAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedText)
            {
                ViewModel.SeleccionarEquipo(selectedText);
            }
        }

        private void EquipoAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox autoSuggestBox)
            {
                ViewModel.FiltrarEquipos(autoSuggestBox.Text);
                autoSuggestBox.ItemsSource = ViewModel.EquiposSugeridos;
                autoSuggestBox.IsSuggestionListOpen = true;
            }
        }

        private void ClearEquipo_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LimpiarSeleccionEquipo();
            EquipoAutoSuggestBox.Text = string.Empty;
        }

        #endregion

        #region Imagenes de nodos

        private async void CargarImagenNodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not LevantamientoTreeItemModel nodo)
                return;

            if (ViewModel.IdLevantamiento <= 0)
            {
                await ShowMessageAsync("Levantamiento no guardado",
                    "Debe guardar el levantamiento antes de cargar imagenes.");
                return;
            }

            try
            {
                var picker = new FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".gif");
                picker.FileTypeFilter.Add(".bmp");

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                using var stream = await file.OpenStreamForReadAsync();
                var contentType = ImageContentTypeHelper.GetContentTypeFromExtension(file.FileType);

                var result = await _imageService.SaveImageAsync(
                    ViewModel.IdLevantamiento, nodo.Clave, stream, contentType);

                nodo.AddImage(new LevantamientoImageItem
                {
                    FileName = result.FileName,
                    FilePath = result.FilePath,
                    Title = result.Title,
                    ImageNumber = result.ImageNumber
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error al cargar imagen de nodo", ex,
                    nameof(LevantamientoView), nameof(CargarImagenNodo_Click));
                await ShowMessageAsync("Error", $"No se pudo cargar la imagen: {ex.Message}");
            }
        }

        private async void VerImagenNodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not LevantamientoImageItem imagen)
                return;

            try
            {
                await _imageViewerService.ShowImageAsync(imagen.FilePath, imagen.Title);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error al abrir visor de imagen", ex,
                    nameof(LevantamientoView), nameof(VerImagenNodo_Click));
            }
        }

        private async void EliminarImagenNodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not LevantamientoImageItem imagen)
                return;

            // Buscar el nodo padre que contiene esta imagen
            var nodo = FindNodeWithImage(ViewModel.CapturedTreeItems, imagen);
            if (nodo is null) return;

            var confirmDialog = new ContentDialog
            {
                Title = "Eliminar imagen",
                Content = $"¿Desea eliminar la imagen '{imagen.Title}'?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                await _imageService.DeleteImageAsync(imagen.FilePath);
                nodo.RemoveImage(imagen);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error al eliminar imagen", ex,
                    nameof(LevantamientoView), nameof(EliminarImagenNodo_Click));
                await ShowMessageAsync("Error", $"No se pudo eliminar la imagen: {ex.Message}");
            }
        }

        private static LevantamientoTreeItemModel? FindNodeWithImage(
            System.Collections.ObjectModel.ObservableCollection<LevantamientoTreeItemModel> nodes,
            LevantamientoImageItem target)
        {
            foreach (var node in nodes)
            {
                if (node.Imagenes.Contains(target))
                    return node;

                var found = FindNodeWithImage(node.Hijos, target);
                if (found is not null) return found;
            }
            return null;
        }

        #endregion

        #region Guardar y Reporte

        private async void GuardarLevantamiento_Click(object sender, RoutedEventArgs e)
        {
            GuardarLevantamientoButton.IsEnabled = false;
            try
            {
                var introduccion = ImputIntroduccion.Text;
                var conclusion = ImputConclusion.Text;

                var (success, message) = await ViewModel.GuardarLevantamientoAsync(introduccion, conclusion);

                await ShowMessageAsync(
                    success ? "Levantamiento guardado" : "Error al guardar",
                    message);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error al guardar levantamiento", ex,
                    nameof(LevantamientoView), nameof(GuardarLevantamiento_Click));
                await ShowMessageAsync("Error", $"Error inesperado: {ex.Message}");
            }
            finally
            {
                GuardarLevantamientoButton.IsEnabled = true;
            }
        }

        private async void GenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IdLevantamiento <= 0)
            {
                await ShowMessageAsync("Levantamiento no guardado",
                    "Debe guardar el levantamiento antes de generar el reporte.");
                return;
            }

            GenerarReporteButton.IsEnabled = false;
            try
            {
                var introduccion = ImputIntroduccion.Text;
                var conclusion = ImputConclusion.Text;

                var filePath = await ViewModel.GenerarReporteAsync(introduccion, conclusion);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var storageFile = await global::Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                    await global::Windows.System.Launcher.LaunchFileAsync(storageFile);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error al generar reporte", ex,
                    nameof(LevantamientoView), nameof(GenerarReporte_Click));
                await ShowMessageAsync("Error", $"No se pudo generar el reporte: {ex.Message}");
            }
            finally
            {
                GenerarReporteButton.IsEnabled = true;
            }
        }

        #endregion

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Aceptar",
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
