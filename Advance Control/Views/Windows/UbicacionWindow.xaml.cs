using Advance_Control.Models;
using Advance_Control.Services.Areas;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Utilities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics;

namespace Advance_Control.Views.Windows
{
    /// <summary>
    /// Ventana para crear una ubicación rápida desde la página de equipos.
    /// Incluye mapa para seleccionar coordenadas y validación de área.
    /// </summary>
    public sealed partial class UbicacionWindow : Window
    {
        private readonly IUbicacionService _ubicacionService;
        private readonly IAreasService _areasService;
        private readonly IGoogleMapsConfigService _mapsConfigService;
        private readonly ILoggingService _loggingService;

        private decimal? _latitud;
        private decimal? _longitud;
        private string? _direccion;
        private string? _ciudad;
        private string? _estado;
        private string? _pais;
        private string? _placeId;

        /// <summary>
        /// Ubicación creada exitosamente. Null si se canceló.
        /// </summary>
        public UbicacionDto? UbicacionCreada { get; private set; }

        public UbicacionWindow()
        {
            this.InitializeComponent();

            _ubicacionService = AppServices.Get<IUbicacionService>();
            _areasService = AppServices.Get<IAreasService>();
            _mapsConfigService = AppServices.Get<IGoogleMapsConfigService>();
            _loggingService = AppServices.Get<ILoggingService>();

            // Configurar tamaño de la ventana
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(900, 700));
            this.Title = "Crear Ubicación";

            // Inicializar mapa
            _ = InitializeMapAsync();
        }

        private async Task InitializeMapAsync()
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async();
                MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                var config = await _mapsConfigService.GetConfigAsync();
                var apiKey = config?.ApiKey ?? "";
                var zoom = config?.DefaultZoom ?? 6;

                // Parsear centro predeterminado (formato: "lat,lng")
                var lat = "19.4326";
                var lng = "-99.1332";
                if (!string.IsNullOrEmpty(config?.DefaultCenter))
                {
                    var parts = config.DefaultCenter.Split(',');
                    if (parts.Length == 2)
                    {
                        lat = parts[0].Trim();
                        lng = parts[1].Trim();
                    }
                }

                var html = GenerateMapHtml(apiKey, lat, lng, zoom);
                MapWebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al inicializar mapa en UbicacionWindow", ex,
                    "UbicacionWindow", "InitializeMapAsync");
            }
        }

        private void CoreWebView2_WebMessageReceived(
            Microsoft.Web.WebView2.Core.CoreWebView2 sender,
            Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = args.TryGetWebMessageAsString();
                var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                if (jsonDoc == null) return;

                if (!jsonDoc.TryGetValue("type", out var typeEl)) return;
                var msgType = typeEl.GetString();

                if (msgType is "markerMoved" or "placeSelected")
                {
                    if (jsonDoc.TryGetValue("lat", out var latEl) && jsonDoc.TryGetValue("lng", out var lngEl))
                    {
                        if (decimal.TryParse(latEl.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                            decimal.TryParse(lngEl.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
                        {
                            _latitud = lat;
                            _longitud = lng;

                            if (jsonDoc.TryGetValue("address", out var addrEl))
                                _direccion = addrEl.GetString();
                            if (jsonDoc.TryGetValue("city", out var cityEl))
                                _ciudad = cityEl.GetString();
                            if (jsonDoc.TryGetValue("state", out var stateEl))
                                _estado = stateEl.GetString();
                            if (jsonDoc.TryGetValue("country", out var countryEl))
                                _pais = countryEl.GetString();
                            if (jsonDoc.TryGetValue("placeId", out var placeEl))
                                _placeId = placeEl.GetString();

                            DispatcherQueue.TryEnqueue(() =>
                            {
                                LatitudTextBox.Text = lat.ToString(CultureInfo.InvariantCulture);
                                LongitudTextBox.Text = lng.ToString(CultureInfo.InvariantCulture);
                                DireccionTextBox.Text = _direccion ?? "";

                                // Auto-llenar nombre si está vacío
                                if (string.IsNullOrWhiteSpace(NombreTextBox.Text) &&
                                    jsonDoc.TryGetValue("name", out var nameEl))
                                {
                                    var placeName = nameEl.GetString();
                                    if (!string.IsNullOrWhiteSpace(placeName))
                                        NombreTextBox.Text = placeName;
                                }

                                GuardarButton.IsEnabled = true;
                                StatusInfoBar.IsOpen = false;
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _loggingService.LogErrorAsync("Error al procesar mensaje de WebView2", ex,
                    "UbicacionWindow", "CoreWebView2_WebMessageReceived");
            }
        }

        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
                {
                    MostrarError("El nombre es requerido.");
                    return;
                }

                if (!_latitud.HasValue || !_longitud.HasValue)
                {
                    MostrarError("Haz clic en el mapa para seleccionar una ubicación.");
                    return;
                }

                GuardarButton.IsEnabled = false;
                MostrarInfo("Validando área...");

                // Validar que la ubicación cae dentro de un área
                try
                {
                    var areas = await _areasService.ValidatePointAsync(_latitud.Value, _longitud.Value);
                    if (areas == null || !areas.Any(a => a.DentroDelArea))
                    {
                        MostrarError("La ubicación no se encuentra dentro de ningún área definida. " +
                                     "Primero cree un área que cubra esta zona.");
                        GuardarButton.IsEnabled = true;
                        return;
                    }
                }
                catch (Exception areaEx)
                {
                    await _loggingService.LogWarningAsync(
                        $"No se pudo validar área: {areaEx.Message}",
                        "UbicacionWindow", "GuardarButton_Click");
                    MostrarError("No se pudo verificar el área. Verifique su conexión.");
                    GuardarButton.IsEnabled = true;
                    return;
                }

                MostrarInfo("Guardando ubicación...");

                var ubicacion = new UbicacionDto
                {
                    Nombre = NombreTextBox.Text.Trim(),
                    Latitud = _latitud.Value,
                    Longitud = _longitud.Value,
                    DireccionCompleta = _direccion,
                    Ciudad = _ciudad,
                    Estado = _estado,
                    Pais = _pais,
                    PlaceId = _placeId,
                    Activo = true
                };

                var response = await _ubicacionService.CreateUbicacionAsync(ubicacion);

                if (response.Success)
                {
                    // Recuperar la ubicación con su IdArea asignado por el SP
                    var ubicaciones = await _ubicacionService.GetUbicacionesAsync();
                    var creada = ubicaciones
                        .Where(u => u.Nombre == ubicacion.Nombre)
                        .OrderByDescending(u => u.IdUbicacion)
                        .FirstOrDefault();

                    UbicacionCreada = creada ?? ubicacion;
                    this.Close();
                }
                else
                {
                    MostrarError(response.Message ?? "Error al crear la ubicación.");
                    GuardarButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al guardar ubicación", ex,
                    "UbicacionWindow", "GuardarButton_Click");
                MostrarError("Error inesperado al guardar la ubicación.");
                GuardarButton.IsEnabled = true;
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            UbicacionCreada = null;
            this.Close();
        }

        private async void MapSearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != global::Windows.System.VirtualKey.Enter) return;
            e.Handled = true;

            var query = MapSearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query)) return;

            try
            {
                var escaped = query.Replace("'", "\\'");
                await MapWebView.ExecuteScriptAsync($"searchLocation('{escaped}')");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al buscar en mapa", ex,
                    "UbicacionWindow", "MapSearchBox_KeyDown");
            }
        }

        private void MostrarError(string mensaje)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Message = mensaje;
            StatusInfoBar.IsOpen = true;
        }

        private void MostrarInfo(string mensaje)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Informational;
            StatusInfoBar.Message = mensaje;
            StatusInfoBar.IsOpen = true;
        }

        private string GenerateMapHtml(string apiKey, string lat, string lng, int zoom)
        {
            return $@"<!DOCTYPE html>
<html><head>
<meta charset='utf-8'>
<style>
    * {{ margin: 0; padding: 0; }}
    html, body, #map {{ width: 100%; height: 100%; }}
</style>
</head><body>
<div id='map'></div>
<script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=places,marker'></script>
<script>
    let map, marker, geocoder;

    function initMap() {{
        map = new google.maps.Map(document.getElementById('map'), {{
            center: {{ lat: {lat}, lng: {lng} }},
            zoom: {zoom},
            mapTypeControl: true,
            streetViewControl: false
        }});
        geocoder = new google.maps.Geocoder();

        map.addListener('click', function(e) {{
            placeMarker(e.latLng);
            geocodeLocation(e.latLng);
        }});
    }}

    function placeMarker(location) {{
        if (marker) {{
            marker.position = location;
        }} else {{
            marker = new google.maps.marker.AdvancedMarkerElement({{
                position: location,
                map: map,
                gmpDraggable: true,
                title: 'Ubicación seleccionada'
            }});
            marker.addEventListener('gmp-dragend', () => {{
                const pos = marker.position;
                geocodeLocation(new google.maps.LatLng(pos.lat, pos.lng));
            }});
        }}
    }}

    function geocodeLocation(location) {{
        geocoder.geocode({{ location: location }}, function(results, status) {{
            const message = {{
                type: 'markerMoved',
                lat: location.lat(),
                lng: location.lng()
            }};

            if (status === 'OK' && results[0]) {{
                message.address = results[0].formatted_address;
                message.placeId = results[0].place_id;

                // Buscar nombre del lugar (POI, establecimiento, etc.)
                for (const r of results) {{
                    const t = r.types || [];
                    if (t.includes('point_of_interest') || t.includes('establishment') ||
                        t.includes('premise') || t.includes('subpremise') ||
                        t.includes('neighborhood') || t.includes('sublocality')) {{
                        const nameParts = r.formatted_address.split(',');
                        if (nameParts.length > 0) message.name = nameParts[0].trim();
                        break;
                    }}
                }}

                const comps = results[0].address_components;
                for (const c of comps) {{
                    if (c.types.includes('locality')) message.city = c.long_name;
                    if (c.types.includes('administrative_area_level_1')) message.state = c.long_name;
                    if (c.types.includes('country')) message.country = c.long_name;
                }}
            }}

            window.chrome.webview.postMessage(JSON.stringify(message));
        }});
    }}

    function searchLocation(query) {{
        geocoder.geocode({{ address: query }}, function(results, status) {{
            if (status === 'OK' && results[0]) {{
                const loc = results[0].geometry.location;
                map.setCenter(loc);
                map.setZoom(15);
                placeMarker(loc);
                geocodeLocation(loc);
            }}
        }});
    }}

    initMap();
</script>
</body></html>";
        }
    }
}
