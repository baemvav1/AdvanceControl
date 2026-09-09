using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
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

        private double? _pendingLat;
        private double? _pendingLng;
        private string _pendingName = string.Empty;

        private CoreWebView2Environment? _webView2Environment;
        private string? _currentUbicacionesMapHtml;
        private bool _mapLoadStarted = false;
        private readonly SemaphoreSlim _loadMapLock = new SemaphoreSlim(1, 1);
        private bool _navigationCompletedSubscribed = false;

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
                _webView2Environment = env;
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
            // OnPageLoaded y OnNavigatedTo pueden llamar a este método casi
            // simultáneamente (cada uno intentando cubrir el caso en que el otro aún no
            // esté listo) — sin este guard, ambos navegan el mismo WebView2 casi a la vez,
            // lo que aborta la primera navegación (mismo patrón visto y corregido en
            // AreasPage: ver docs/salvandolosmapas-recovery.md).
            await _loadMapLock.WaitAsync();
            try
            {
                if (_mapLoadStarted)
                {
                    return;
                }

                var apiKey = ViewModel.MapsConfig?.ApiKey ?? string.Empty;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    SetStatus(InfoBarSeverity.Error, "ApiKey de Google Maps no disponible — revisa /api/GoogleMapsConfig");
                    return;
                }

                _mapLoadStarted = true;

                // El HTML se sirve desde memoria (no desde disco/carpeta virtual) — ver
                // CoreWebView2_MapHtmlRequested. SetVirtualHostNameToFolderMapping con un
                // archivo regenerado en cada carga producía ERR_CONNECTION_ABORTED de forma
                // consistente en algunos equipos (mismo problema resuelto en AreasPage).
                core.AddWebResourceRequestedFilter(
                    "https://ac-maps-local/map.html",
                    CoreWebView2WebResourceContext.Document);
                core.WebResourceRequested += CoreWebView2_MapHtmlRequested;

                core.WebMessageReceived -= OnMapWebMessageReceived;
                core.WebMessageReceived += OnMapWebMessageReceived;

                if (!_navigationCompletedSubscribed)
                {
                    _navigationCompletedSubscribed = true;
                    MapWebView.NavigationCompleted += MapWebView_NavigationCompleted;
                }


var html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <style>
    html, body, #map {{ margin: 0; height: 100%; width: 100%; font-family: Roboto, Arial, sans-serif; }}

    /* ── BUSCADOR ── edita SEARCH_WIDTH para cambiar el ancho */
    #search-container {{
      position: absolute;
      top: 16px;
      left: 50%;
      transform: translateX(-50%);
      z-index: 10;
      width: 500px;          /* SEARCH_WIDTH — cambia este valor a tu gusto */
      max-width: 92%;
      display: flex;
      align-items: center;
      background: #fff;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.18);
      transition: box-shadow 0.2s ease;
    }}
    #search-container:focus-within {{
      box-shadow: 0 4px 20px rgba(0,0,0,0.28);
    }}
    #search-box {{
      flex: 1;
      padding: 12px 10px 12px 16px;
      font-size: 15px;
      border: none;
      border-radius: 8px 0 0 8px;
      outline: none;
      background: transparent;
      min-width: 0;
    }}
    #clear-btn {{
      flex-shrink: 0;
      width: 36px;
      height: 36px;
      margin-right: 6px;
      display: none;
      align-items: center;
      justify-content: center;
      background: none;
      border: none;
      border-radius: 50%;
      cursor: pointer;
      font-size: 18px;
      color: #666;
      line-height: 1;
    }}
    #clear-btn:hover {{ background: #f0f0f0; color: #333; }}
    #clear-btn.visible {{ display: flex; }}
    .pac-container {{
      border-radius: 8px;
      margin-top: 4px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
      border-top: none;
    }}
  </style>
</head>
<body>
  <div id='search-container'>
    <input id='search-box' type='text' placeholder='Buscar lugar, colonia, coordenadas…' />
    <button id='clear-btn' title='Limpiar búsqueda'>&#x2715;</button>
  </div>
  <div id='map'></div>
  <script>
    let map, marker, autocomplete, infoWindow;

    async function initMap() {{
      const {{ Map }} = await google.maps.importLibrary(""maps"");
      const {{ AdvancedMarkerElement }} = await google.maps.importLibrary(""marker"");

      map = new Map(document.getElementById('map'), {{
        center: {{lat: 19.4326, lng: -99.1332}},
        zoom: 12,
        mapId: 'DEMO_MAP_ID'
      }});

      infoWindow = new google.maps.InfoWindow();

      const input   = document.getElementById('search-box');
      const clearBtn = document.getElementById('clear-btn');

      autocomplete = new google.maps.places.Autocomplete(input,
        {{ fields: ['geometry', 'name', 'formatted_address'] }});
      autocomplete.bindTo('bounds', map);

      autocomplete.addListener('place_changed', function() {{
        const place = autocomplete.getPlace();
        if (!place.geometry || !place.geometry.location) return;
        navigateTo(
          place.geometry.location.lat(),
          place.geometry.location.lng(),
          place.name || '',
          place.formatted_address || ''
        );
      }});

      input.addEventListener('input', function() {{
        clearBtn.classList.toggle('visible', this.value.length > 0);
      }});

      input.addEventListener('keydown', function(e) {{
        if (e.key !== 'Enter') return;
        const text = this.value.trim();
        const coordMatch = text.match(/^(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)$/);
        if (coordMatch) {{
          navigateTo(parseFloat(coordMatch[1]), parseFloat(coordMatch[2]), 'Coordenadas', text);
        }} else {{
          google.maps.event.trigger(autocomplete, 'place_changed');
        }}
      }});

      clearBtn.addEventListener('click', function() {{
        input.value = '';
        clearBtn.classList.remove('visible');
        if (marker) {{ marker.setMap(null); marker = null; }}
        infoWindow.close();
        input.focus();
      }});

      map.addListener('click', function(e) {{
        const lat = e.latLng.lat();
        const lng = e.latLng.lng();
        navigateTo(lat, lng, '', '');
      }});
    }}

    function navigateTo(lat, lng, name, address) {{
      const pos = {{ lat: lat, lng: lng }};
      map.panTo(pos);
      map.setZoom(16);
      
      // Limpiar marcador e InfoWindow previos
      if (marker) marker.setMap(null);
      infoWindow.close();

      // Usar la nueva API de AdvancedMarkerElement
      marker = new google.maps.marker.AdvancedMarkerElement({{
        map: map,
        position: pos,
        title: name
      }});

      // Configurar y abrir InfoWindow si hay detalles disponibles
      if (name || address) {{
        const contentString = `<div style='padding: 4px; max-width: 200px;'>` +
          `${{name ? `<strong>${{name}}</strong><br/>` : ''}}` +
          `${{address ? `<span style='font-size:12px;color:#555;'>${{address}}</span>` : ''}}` +
          `</div>`;
        
        infoWindow.setContent(contentString);
        infoWindow.open({{ map, anchor: marker }});
      }}

      // Notificar de manera segura a WebView2 / Chrome Webview
      try {{
        if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {{
          window.chrome.webview.postMessage(JSON.stringify({{
            type: 'placeSelected', lat: lat, lng: lng, name: name, address: address
          }}));
        }}
      }} catch(e) {{
        console.error(""Error enviando mensaje al WebView:"", e);
      }}
    }}
  </script>
  <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=places&callback=initMap' async defer></script>
</body>
</html>";

                _currentUbicacionesMapHtml = html;
                core.Navigate("https://ac-maps-local/map.html");
                SetStatus(InfoBarSeverity.Informational, "Cargando mapa…");
            }
            finally
            {
                _loadMapLock.Release();
            }
        }

        private void MapWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            SetStatus(args.IsSuccess ? InfoBarSeverity.Success : InfoBarSeverity.Error,
                args.IsSuccess ? "Mapa listo" : $"Error al cargar mapa: {args.WebErrorStatus}");
        }

        private void CoreWebView2_MapHtmlRequested(object sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            if (_webView2Environment == null || _currentUbicacionesMapHtml == null
                || args.Request.Uri != "https://ac-maps-local/map.html")
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(_currentUbicacionesMapHtml);
            var stream = new MemoryStream(bytes).AsRandomAccessStream();
            args.Response = _webView2Environment.CreateWebResourceResponse(
                stream, 200, "OK", "Content-Type: text/html; charset=utf-8");
        }

        private void OnMapWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var json = args.TryGetWebMessageAsString();
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.GetProperty("type").GetString() != "placeSelected") return;

                var lat = root.GetProperty("lat").GetDouble();
                var lng = root.GetProperty("lng").GetDouble();
                var name = root.GetProperty("name").GetString() ?? string.Empty;

                _pendingLat = lat;
                _pendingLng = lng;
                _pendingName = name;

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (LocationForm.Visibility == Visibility.Visible)
                    {
                        LatTextBox.Text = lat.ToString("F6", CultureInfo.InvariantCulture);
                        LngTextBox.Text = lng.ToString("F6", CultureInfo.InvariantCulture);
                        if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(NombreTextBox.Text))
                            NombreTextBox.Text = name;
                    }
                    else
                    {
                        SetStatus(InfoBarSeverity.Informational,
                            $"Lat {lat:F5}, Lng {lng:F5} — haz clic en \"Agregar Ubicación\" para guardar");
                    }
                });
            }
            catch { }
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

            if (_pendingLat.HasValue && _pendingLng.HasValue)
            {
                LatTextBox.Text = _pendingLat.Value.ToString("F6", CultureInfo.InvariantCulture);
                LngTextBox.Text = _pendingLng.Value.ToString("F6", CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(_pendingName))
                    NombreTextBox.Text = _pendingName;
            }

            LocationForm.Visibility = Visibility.Visible;
        }

        private async void UbicacionesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isCenteringMap) return;

            var ubicacion = ViewModel.SelectedUbicacion;
            if (ubicacion?.Latitud == null || ubicacion.Longitud == null) return;
            if (MapWebView.CoreWebView2 == null) return;

            var lat  = ubicacion.Latitud.Value.ToString("F6", CultureInfo.InvariantCulture);
            var lng  = ubicacion.Longitud.Value.ToString("F6", CultureInfo.InvariantCulture);
            var name = (ubicacion.Nombre ?? string.Empty).Replace("'", "\\'");

            try
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync(
                    $"navigateTo({lat}, {lng}, '{name}', '');");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al centrar mapa en ubicación", ex, "UbicacionesPage", "UbicacionesList_SelectionChanged");
            }
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
