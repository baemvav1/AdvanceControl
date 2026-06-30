using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.Services.Notificacion;
using Advance_Control.Models;

namespace Advance_Control.Views.Pages
{
    public sealed partial class UbicacionesPage : Page
    {
        public UbicacionesViewModel ViewModel { get; }
        private readonly ILoggingService _loggingService;
        private readonly IActivityService _activityService;
        private readonly INotificacionService _notificacionService;

        private bool _isEditMode = false;
        private int? _editingUbicacionId = null;
        private bool _isCenteringMap = false;

        public UbicacionesPage()
        {
            ViewModel = AppServices.Get<UbicacionesViewModel>();
            _loggingService = AppServices.Get<ILoggingService>();
            _activityService = AppServices.Get<IActivityService>();
            _notificacionService = AppServices.Get<INotificacionService>();

            this.InitializeComponent();
            ButtonClickLogger.Attach(this, _loggingService, nameof(UbicacionesPage));
            this.DataContext = ViewModel;
            this.Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync();
                await MapWebView.EnsureCoreWebView2Async(env);

                if (MapWebView.CoreWebView2 == null)
                {
                    SetStatus(InfoBarSeverity.Error, "No se pudo inicializar el mapa.");
                    return;
                }

                // Si InitializeAsync ya terminó, cargamos el mapa aquí.
                // Si aún está corriendo, OnNavigatedTo lo cargará cuando termine.
                if (ViewModel.MapsConfig != null)
                    await LoadMapAsync(MapWebView.CoreWebView2);
            }
            catch (Exception ex)
            {
                SetStatus(InfoBarSeverity.Error, $"Error al inicializar mapa: {ex.Message}");
                await _loggingService.LogErrorAsync("Error al inicializar mapa", ex, "UbicacionesPage", "OnPageLoaded");
            }
        }

        private async Task LoadMapAsync(CoreWebView2 core)
        {
            var apiKey = ViewModel.MapsConfig?.ApiKey ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                SetStatus(InfoBarSeverity.Error, "ApiKey de Google Maps no disponible — revisa /api/GoogleMapsConfig");
                return;
            }

            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Advance Control", "map_cache");
            Directory.CreateDirectory(cacheDir);

            core.SetVirtualHostNameToFolderMapping(
                "ac-maps-local", cacheDir,
                CoreWebView2HostResourceAccessKind.Allow);
            var html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>html,body,#map{{margin:0;height:100%;width:100%}}</style>
</head>
<body>
  <div id='map'></div>
  <script>
    function initMap() {{
      new google.maps.Map(document.getElementById('map'), {{
        center: {{lat: 19.4326, lng: -99.1332}},
        zoom: 12
      }});
    }}
  </script>
  <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&callback=initMap' defer></script>
</body>
</html>";

            var mapFile = Path.Combine(cacheDir, "map.html");
            await File.WriteAllTextAsync(mapFile, html, Encoding.UTF8);

            MapWebView.NavigationCompleted += (s, a) =>
            {
                SetStatus(a.IsSuccess ? InfoBarSeverity.Success : InfoBarSeverity.Error,
                    a.IsSuccess ? "Mapa cargado" : $"Error al cargar mapa: {a.WebErrorStatus}");
            };

            core.Navigate("https://ac-maps-local/map.html");
            SetStatus(InfoBarSeverity.Informational, "Navegando al mapa…");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                await ViewModel.InitializeAsync();

                // Si el WebView2 ya terminó de inicializarse, cargamos el mapa aquí.
                // Si aún está iniciando, OnPageLoaded lo cargará cuando termine.
                if (MapWebView.CoreWebView2 != null)
                    await LoadMapAsync(MapWebView.CoreWebView2);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al inicializar Ubicaciones", ex, "UbicacionesPage", "OnNavigatedTo");
            }
        }

        private void SetStatus(Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, string message)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                StatusBar.Severity = severity;
                StatusBar.Message = message;
                StatusBar.IsOpen = true;
            });
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.LoadUbicacionesAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al refrescar", ex, "UbicacionesPage", "RefreshButton_Click");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            _editingUbicacionId = null;
            FormTitle.Text = "Nueva Ubicación";
            ClearForm();
            LocationForm.Visibility = Visibility.Visible;
        }

        private void UbicacionesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isCenteringMap) return;
            // sin mapa por ahora — placeholder para cuando se integre
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int idUbicacion)
            {
                var ubicacion = ViewModel.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == idUbicacion);
                if (ubicacion != null)
                {
                    _isEditMode = true;
                    _editingUbicacionId = idUbicacion;
                    FormTitle.Text = "Editar Ubicación";
                    LoadUbicacionToForm(ubicacion);
                    LocationForm.Visibility = Visibility.Visible;
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int idUbicacion)
            {
                var dialog = new ContentDialog
                {
                    Title = "Confirmar eliminación",
                    Content = "¿Está seguro de que desea eliminar esta ubicación?",
                    PrimaryButtonText = "Eliminar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        var response = await ViewModel.DeleteUbicacionAsync(idUbicacion);
                        if (response.Success)
                        {
                            _activityService.Registrar("Ubicaciones", "Ubicación eliminada");
                            await _notificacionService.MostrarAsync("Éxito", "Ubicación eliminada correctamente");
                        }
                        else
                        {
                            await _notificacionService.MostrarAsync("Error", response.Message ?? "Error al eliminar la ubicación");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync("Error al eliminar ubicación", ex, "UbicacionesPage", "DeleteButton_Click");
                        await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la ubicación");
                    }
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
                {
                    await _notificacionService.MostrarAsync("Validación", "El nombre es requerido");
                    return;
                }

                if (!decimal.TryParse(LatTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var lat) ||
                    !decimal.TryParse(LngTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var lng))
                {
                    await _notificacionService.MostrarAsync("Validación", "Latitud y Longitud deben ser números válidos (ej: 19.4326, -99.1332)");
                    return;
                }

                var ubicacion = new UbicacionDto
                {
                    Nombre = NombreTextBox.Text,
                    Descripcion = DescripcionTextBox.Text,
                    Latitud = lat,
                    Longitud = lng,
                    Activo = true
                };

                ApiResponse response;
                if (_isEditMode && _editingUbicacionId.HasValue)
                {
                    ubicacion.IdUbicacion = _editingUbicacionId.Value;
                    response = await ViewModel.UpdateUbicacionAsync(ubicacion);
                }
                else
                {
                    response = await ViewModel.CreateUbicacionAsync(ubicacion);
                }

                if (response.Success)
                {
                    var wasEditMode = _isEditMode;
                    _activityService.Registrar("Ubicaciones", wasEditMode ? "Ubicación modificada" : "Ubicación creada");
                    LocationForm.Visibility = Visibility.Collapsed;
                    ClearForm();
                    await _notificacionService.MostrarAsync("Éxito", wasEditMode ? "Ubicación actualizada correctamente" : "Ubicación creada correctamente");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", response.Message ?? "Error al guardar la ubicación");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al guardar ubicación", ex, "UbicacionesPage", "SaveButton_Click");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al guardar la ubicación");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LocationForm.Visibility = Visibility.Collapsed;
            ClearForm();
        }

        private void ClearForm()
        {
            NombreTextBox.Text = string.Empty;
            DescripcionTextBox.Text = string.Empty;
            LatTextBox.Text = string.Empty;
            LngTextBox.Text = string.Empty;
            _isEditMode = false;
            _editingUbicacionId = null;
        }

        private void LoadUbicacionToForm(UbicacionDto ubicacion)
        {
            NombreTextBox.Text = ubicacion.Nombre ?? string.Empty;
            DescripcionTextBox.Text = ubicacion.Descripcion ?? string.Empty;
            LatTextBox.Text = ubicacion.Latitud?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            LngTextBox.Text = ubicacion.Longitud?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}
