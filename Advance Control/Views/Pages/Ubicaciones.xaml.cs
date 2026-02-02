using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Logging;
using Advance_Control.Models;
using System.Globalization;
using System.Threading.Tasks;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página de ubicaciones con visualización de Google Maps
    /// </summary>
    public sealed partial class Ubicaciones : Page
    {
        // Default coordinates for Mexico City if configuration is not available
        private const string DEFAULT_LATITUDE = "19.4326";
        private const string DEFAULT_LONGITUDE = "-99.1332";
        
        // Delay in milliseconds to ensure map is initialized before centering
        private const int MAP_INITIALIZATION_DELAY_MS = 500;

        // Tab header names for TabView
        private const string TAB_UBICACIONES = "Ubicaciones";
        private const string TAB_AREAS = "Áreas";

        public UbicacionesViewModel ViewModel { get; }
        private readonly ILoggingService _loggingService;
        private bool _isEditMode = false;
        private int? _editingUbicacionId = null;
        private bool _isFormVisible = false;
        private bool _isCenteringMap = false;
        
        // Store location data extracted from Google Maps Geocoding API
        private decimal? _currentLatitud = null;
        private decimal? _currentLongitud = null;
        private string? _currentDireccionCompleta = null;
        private string? _currentCiudad = null;
        private string? _currentEstado = null;
        private string? _currentPais = null;
        private string? _currentPlaceId = null;
        
        // Store pending shape messages if AreasPage is not yet initialized
        private Dictionary<string, JsonElement>? _pendingShapeMessage = null;

        public Ubicaciones()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<UbicacionesViewModel>();

            // Resolver el servicio de logging desde DI
            _loggingService = ((App)Application.Current).Host.Services.GetRequiredService<ILoggingService>();

            this.InitializeComponent();

            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;

            // Setup WebView2 message handler
            this.Loaded += Ubicaciones_Loaded;
        }

        private async void Ubicaciones_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async();
                MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                
                // Set parent reference for Areas page
                if (AreasPage != null)
                {
                    AreasPage.ParentUbicacionesPage = this;
                    
                    // Forward any pending shape messages now that AreasPage is available
                    if (_pendingShapeMessage != null)
                    {
                        await _loggingService.LogInformationAsync(
                            "Forwarding pending shape message to AreasPage (on page load)", 
                            "Ubicaciones", 
                            "Ubicaciones_Loaded");
                        await AreasPage.HandleShapeMessageAsync(_pendingShapeMessage);
                        _pendingShapeMessage = null; // Clear after forwarding
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al configurar WebView2 message handler", ex, "Ubicaciones", "Ubicaciones_Loaded");
            }
        }

        private async void CoreWebView2_WebMessageReceived(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = args.TryGetWebMessageAsString();
                await _loggingService.LogInformationAsync($"Mensaje recibido de WebView2: {message}", "Ubicaciones", "CoreWebView2_WebMessageReceived");

                // Parse the JSON message
                var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                
                if (jsonDoc != null && jsonDoc.TryGetValue("type", out var typeElement))
                {
                    var messageType = typeElement.GetString();

                    if (messageType == "markerMoved")
                    {
                        // Extract coordinates
                        if (jsonDoc.TryGetValue("lat", out var latElement) && 
                            jsonDoc.TryGetValue("lng", out var lngElement))
                        {
                            // Safely parse decimal values
                            if (latElement.ValueKind == JsonValueKind.Number && 
                                lngElement.ValueKind == JsonValueKind.Number)
                            {
                                var lat = latElement.GetDecimal();
                                var lng = lngElement.GetDecimal();

                                // Store coordinates in private fields
                                _currentLatitud = lat;
                                _currentLongitud = lng;

                                // Get address information if available
                                if (jsonDoc.TryGetValue("address", out var addressElement))
                                {
                                    var addressData = addressElement.Deserialize<Dictionary<string, JsonElement>>();
                                    
                                    if (addressData != null)
                                    {
                                         // Extract formatted address
                                        if (addressData.TryGetValue("formatted", out var formattedElement) && 
                                            formattedElement.ValueKind == JsonValueKind.String)
                                        {
                                            _currentDireccionCompleta = formattedElement.GetString();
                                        }
                                        
                                        // Extract city
                                        if (addressData.TryGetValue("city", out var cityElement) && 
                                            cityElement.ValueKind == JsonValueKind.String)
                                        {
                                            _currentCiudad = cityElement.GetString();
                                        }
                                        
                                        // Extract state
                                        if (addressData.TryGetValue("state", out var stateElement) && 
                                            stateElement.ValueKind == JsonValueKind.String)
                                        {
                                            _currentEstado = stateElement.GetString();
                                        }
                                        
                                        // Extract country
                                        if (addressData.TryGetValue("country", out var countryElement) && 
                                            countryElement.ValueKind == JsonValueKind.String)
                                        {
                                            _currentPais = countryElement.GetString();
                                        }
                                        
                                        // Extract place_id
                                        if (addressData.TryGetValue("place_id", out var placeIdElement) && 
                                            placeIdElement.ValueKind == JsonValueKind.String)
                                        {
                                            _currentPlaceId = placeIdElement.GetString();
                                        }
                                    }
                                }

                                // Update search box with the formatted address for visual validation
                                // This provides the user with immediate feedback about the selected location
                                var searchBoxUpdated = false;
                                if (!string.IsNullOrWhiteSpace(_currentDireccionCompleta))
                                {
                                    var addressToDisplay = _currentDireccionCompleta;
                                    var enqueued = this.DispatcherQueue.TryEnqueue(() =>
                                    {
                                        try
                                        {
                                            if (MapSearchBox != null)
                                            {
                                                MapSearchBox.Text = addressToDisplay;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // Fire-and-forget logging to avoid blocking the UI thread
                                            // We don't await here since this is a UI thread callback
                                            _ = _loggingService.LogErrorAsync("Error al actualizar campo de búsqueda", ex, "Ubicaciones", "CoreWebView2_WebMessageReceived");
                                        }
                                    });
                                    
                                    searchBoxUpdated = enqueued;
                                    
                                    if (!enqueued)
                                    {
                                        await _loggingService.LogWarningAsync("No se pudo encolar la actualización del campo de búsqueda", "Ubicaciones", "CoreWebView2_WebMessageReceived");
                                    }
                                }

                                var logMessage = $"Ubicación actualizada: Lat={lat}, Lng={lng}, Ciudad={_currentCiudad}, Estado={_currentEstado}, País={_currentPais}";
                                if (!string.IsNullOrWhiteSpace(_currentDireccionCompleta))
                                {
                                    logMessage += $", Dirección={_currentDireccionCompleta}";
                                    if (searchBoxUpdated)
                                    {
                                        logMessage += ". Campo de búsqueda actualizado con la dirección";
                                    }
                                }
                                
                                await _loggingService.LogInformationAsync(
                                    logMessage, 
                                    "Ubicaciones", 
                                    "CoreWebView2_WebMessageReceived");
                            }
                            else
                            {
                                await _loggingService.LogWarningAsync("Coordenadas recibidas no son números válidos", "Ubicaciones", "CoreWebView2_WebMessageReceived");
                            }
                        }
                    }
                    else if (messageType == "shapeDrawn" || messageType == "shapeEdited")
                    {
                        // Forward shape messages to Areas page if it exists
                        if (AreasPage != null)
                        {
                            await AreasPage.HandleShapeMessageAsync(jsonDoc);
                        }
                        else
                        {
                            // Store the shape message to forward later when AreasPage is initialized
                            _pendingShapeMessage = jsonDoc;
                            await _loggingService.LogInformationAsync(
                                "Shape message stored for later forwarding (AreasPage not yet initialized)", 
                                "Ubicaciones", 
                                "CoreWebView2_WebMessageReceived");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al procesar mensaje de WebView2", ex, "Ubicaciones", "CoreWebView2_WebMessageReceived");
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                await _loggingService.LogInformationAsync("Navegando a página de Ubicaciones", "Ubicaciones", "OnNavigatedTo");

                // Inicializar el mapa y cargar datos
                await ViewModel.InitializeAsync();

                // Cargar el HTML del mapa en el WebView2
                await LoadMapAsync();

                // Si se pasó un ID de ubicación como parámetro, seleccionarla y centrar el mapa
                if (e.Parameter is int idUbicacion)
                {
                    await _loggingService.LogInformationAsync($"Navegación con parámetro: IdUbicacion = {idUbicacion}", "Ubicaciones", "OnNavigatedTo");
                    await SelectAndCenterUbicacionAsync(idUbicacion);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al navegar a Ubicaciones", ex, "Ubicaciones", "OnNavigatedTo");
            }
        }

        /// <summary>
        /// Carga el mapa de Google Maps en el WebView2
        /// </summary>
        private async System.Threading.Tasks.Task LoadMapAsync()
        {
            try
            {
                if (ViewModel.MapsConfig == null)
                {
                    await _loggingService.LogWarningAsync("No hay configuración de Google Maps disponible", "Ubicaciones", "LoadMapAsync");
                    return;
                }

                await _loggingService.LogInformationAsync("Cargando mapa de Google Maps", "Ubicaciones", "LoadMapAsync");

                // Ensure CoreWebView2 is initialized before using NavigateToString
                await MapWebView.EnsureCoreWebView2Async();

                // Parsear el centro del mapa
                var centerParts = ViewModel.MapsConfig.DefaultCenter.Split(',');
                var lat = centerParts.Length > 0 ? centerParts[0].Trim() : DEFAULT_LATITUDE;
                var lng = centerParts.Length > 1 ? centerParts[1].Trim() : DEFAULT_LONGITUDE;

                // Serializar las áreas como JSON
                var areasJson = JsonSerializer.Serialize(ViewModel.Areas);

                // Serializar las ubicaciones como JSON
                var ubicacionesJson = JsonSerializer.Serialize(ViewModel.Ubicaciones);

                // Crear el HTML con Google Maps
                var html = GenerateMapHtml(
                    ViewModel.MapsConfig.ApiKey,
                    lat,
                    lng,
                    ViewModel.MapsConfig.DefaultZoom,
                    areasJson,
                    ubicacionesJson);

                // Cargar el HTML en el WebView2
                MapWebView.NavigateToString(html);

                await _loggingService.LogInformationAsync("Mapa cargado exitosamente", "Ubicaciones", "LoadMapAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el mapa", ex, "Ubicaciones", "LoadMapAsync");
            }
        }

        /// <summary>
        /// Carga el mapa de Google Maps para Áreas con herramientas de dibujo
        /// </summary>
        private async Task LoadAreasMapAsync()
        {
            try
            {
                if (ViewModel.MapsConfig == null)
                {
                    await _loggingService.LogWarningAsync("No hay configuración de Google Maps disponible", "Ubicaciones", "LoadAreasMapAsync");
                    return;
                }

                await _loggingService.LogInformationAsync("Cargando mapa de Google Maps para Áreas", "Ubicaciones", "LoadAreasMapAsync");

                // Ensure CoreWebView2 is initialized before using NavigateToString
                await MapWebView.EnsureCoreWebView2Async();

                // Parse coordinates
                decimal parsedLat = decimal.TryParse(ViewModel.MapsConfig.DefaultCenter?.Split(',')[0]?.Trim() ?? DEFAULT_LATITUDE, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) 
                    ? lat 
                    : decimal.Parse(DEFAULT_LATITUDE, CultureInfo.InvariantCulture);
                decimal parsedLng = decimal.TryParse(ViewModel.MapsConfig.DefaultCenter?.Split(',')[1]?.Trim() ?? DEFAULT_LONGITUDE, NumberStyles.Any, CultureInfo.InvariantCulture, out var lng) 
                    ? lng 
                    : decimal.Parse(DEFAULT_LONGITUDE, CultureInfo.InvariantCulture);
                
                string centerLat = parsedLat.ToString(CultureInfo.InvariantCulture);
                string centerLng = parsedLng.ToString(CultureInfo.InvariantCulture);
                int zoom = ViewModel.MapsConfig.DefaultZoom;

                // Get Areas data from AreasPage if available
                var areasJson = "[]";
                if (AreasPage != null && AreasPage.ViewModel != null)
                {
                    // Load areas if not already loaded
                    if (AreasPage.ViewModel.Areas.Count == 0)
                    {
                        await AreasPage.ViewModel.LoadAreasAsync();
                    }

                    var areas = AreasPage.ViewModel.Areas;
                    areasJson = JsonSerializer.Serialize(areas.Select(a => new
                    {
                        idArea = a.IdArea,
                        nombre = a.Nombre,
                        type = a.TipoGeometria,
                        path = ParseAreaPath(a),
                        center = ParseAreaCenter(a),
                        radius = a.Radio,
                        options = new
                        {
                            fillColor = a.ColorMapa,
                            fillOpacity = a.Opacidad ?? 0.35m,
                            strokeColor = a.ColorBorde,
                            strokeWeight = a.AnchoBorde ?? 2,
                            editable = false,
                            clickable = true
                        }
                    }));
                }

                // Generate HTML for Areas map with drawing tools
                var html = GenerateAreasMapHtml(
                    ViewModel.MapsConfig.ApiKey,
                    centerLat,
                    centerLng,
                    zoom,
                    areasJson);

                // Load HTML in WebView2
                MapWebView.NavigateToString(html);

                await _loggingService.LogInformationAsync("Mapa de Áreas cargado exitosamente", "Ubicaciones", "LoadAreasMapAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el mapa de áreas", ex, "Ubicaciones", "LoadAreasMapAsync");
            }
        }

        /// <summary>
        /// Método público para que la página de Áreas pueda solicitar recarga del mapa
        /// </summary>
        public async Task ReloadAreasMapAsync()
        {
            await LoadAreasMapAsync();
        }

        /// <summary>
        /// Método público para ejecutar script en el WebView2
        /// Útil para limpiar formas dibujadas, etc.
        /// </summary>
        public async Task ExecuteMapScriptAsync(string script)
        {
            try
            {
                if (MapWebView?.CoreWebView2 != null)
                {
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error al ejecutar script en mapa: {script}", ex, "Ubicaciones", "ExecuteMapScriptAsync");
            }
        }

        /// <summary>
        /// Extrae el path (coordenadas) de un área desde su MetadataJSON
        /// </summary>
        /// <param name="area">DTO del área con MetadataJSON</param>
        /// <returns>Objeto con array de coordenadas, o null si no se encuentra</returns>
        private object? ParseAreaPath(AreaDto area)
        {
            if (!string.IsNullOrEmpty(area.MetadataJSON))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(area.MetadataJSON);
                    if (metadata != null && metadata.TryGetValue("path", out var pathElement))
                    {
                        return JsonSerializer.Deserialize<object>(pathElement.GetRawText());
                    }
                }
                catch (Exception ex)
                {
                    // Log parsing error but continue - area may not have valid path data
                    _ = _loggingService.LogWarningAsync($"Error al parsear path de área {area.IdArea}: {ex.Message}", "Ubicaciones", "ParseAreaPath");
                }
            }
            return null;
        }

        /// <summary>
        /// Extrae las coordenadas del centro de un área
        /// </summary>
        /// <param name="area">DTO del área con CentroLatitud y CentroLongitud</param>
        /// <returns>Objeto con lat/lng, o null si no están disponibles</returns>
        private object? ParseAreaCenter(AreaDto area)
        {
            if (area.CentroLatitud.HasValue && area.CentroLongitud.HasValue)
            {
                return new { lat = area.CentroLatitud.Value, lng = area.CentroLongitud.Value };
            }
            return null;
        }

        /// <summary>
        /// Genera el HTML para el mapa de Google Maps
        /// </summary>
        private string GenerateMapHtml(string apiKey, string lat, string lng, int zoom, string areasJson, string ubicacionesJson)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Google Maps - Ubicaciones</title>
    <style>
        body, html {{
            margin: 0;
            padding: 0;
            height: 100%;
            width: 100%;
            overflow: hidden;
        }}
        #map {{
            height: 100%;
            width: 100%;
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    
    <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=places'></script>
    <script>
        // Constants
        const SEARCH_MARKER_ICON = 'https://maps.google.com/mapfiles/ms/icons/blue-dot.png';
        const EDIT_MARKER_ICON = 'https://maps.google.com/mapfiles/ms/icons/red-dot.png';
        const SELECTED_MARKER_ICON = 'https://maps.google.com/mapfiles/ms/icons/green-dot.png';
        const MARKER_ICON_SIZE = 40;

        let map;
        let areas = {areasJson};
        let ubicaciones = {ubicacionesJson};
        let shapes = [];
        let markers = [];
        let infoWindow;
        let editMarker = null;
        let geocoder = null;
        let isFormVisible = false;
        let searchMarker = null;
        let selectedLocationMarker = null;
        let selectedMarkerTimeout = null;

        // HTML encoding function for defense-in-depth security
        // Encodes HTML special characters to prevent XSS by leveraging
        // the browser's built-in encoding when setting textContent
        function escapeHtml(text) {{
            if (!text) return '';
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }}

        function initMap() {{
            // Crear el mapa
            map = new google.maps.Map(document.getElementById('map'), {{
                center: {{ lat: {lat}, lng: {lng} }},
                zoom: {zoom},
                mapTypeId: 'roadmap'
            }});

            // Crear InfoWindow global
            infoWindow = new google.maps.InfoWindow();

            // Crear geocoder para reverse geocoding
            geocoder = new google.maps.Geocoder();

            // Renderizar las áreas
            renderAreas();

            // Renderizar las ubicaciones (markers)
            renderUbicaciones();

            // Add map click listener for placing marker when form is visible
            map.addListener('click', (event) => {{
                if (isFormVisible) {{
                    placeMarker(event.latLng);
                }}
            }});
        }}

        function searchLocation(query) {{
            if (!query || query.trim() === '') {{
                return;
            }}

            const request = {{
                query: query,
                fields: ['name', 'geometry', 'formatted_address']
            }};

            const service = new google.maps.places.PlacesService(map);
            
            service.findPlaceFromQuery(request, (results, status) => {{
                if (status === google.maps.places.PlacesServiceStatus.OK && results && results.length > 0) {{
                    const place = results[0];
                    
                    // Remove previous search marker if exists
                    if (searchMarker) {{
                        searchMarker.setMap(null);
                    }}

                    // Center map on the found location
                    if (place.geometry && place.geometry.location) {{
                        map.setCenter(place.geometry.location);
                        map.setZoom(15);

                        // Add a marker for the search result
                        searchMarker = new google.maps.Marker({{
                            position: place.geometry.location,
                            map: map,
                            title: place.name,
                            icon: {{
                                url: SEARCH_MARKER_ICON,
                                scaledSize: new google.maps.Size(MARKER_ICON_SIZE, MARKER_ICON_SIZE)
                            }},
                            animation: google.maps.Animation.DROP
                        }});

                        // Show info window with search result
                        const safeName = escapeHtml(place.name || 'Ubicación encontrada');
                        const safeAddress = place.formatted_address ? escapeHtml(place.formatted_address) : '';
                        
                        const content = `
                            <div style='padding: 8px; min-width: 200px;'>
                                <h3 style='margin: 0 0 8px 0; color: #1a73e8; font-size: 16px;'>${{safeName}}</h3>
                                <div style='color: #5f6368; font-size: 14px;'>
                                    ${{safeAddress ? `<p style='margin: 4px 0;'>${{safeAddress}}</p>` : ''}}
                                </div>
                            </div>
                        `;
                        
                        infoWindow.setContent(content);
                        infoWindow.open(map, searchMarker);
                    }}
                }} else {{
                    // Show error to user via InfoWindow
                    const errorMessages = {{
                        'ZERO_RESULTS': 'No se encontraron resultados para la búsqueda.',
                        'OVER_QUERY_LIMIT': 'Se ha excedido el límite de consultas. Intente más tarde.',
                        'REQUEST_DENIED': 'La solicitud fue denegada.',
                        'INVALID_REQUEST': 'La solicitud no es válida.',
                        'UNKNOWN_ERROR': 'Ocurrió un error desconocido. Intente nuevamente.'
                    }};
                    
                    const errorMessage = errorMessages[status] || errorMessages.UNKNOWN_ERROR;
                    const safeErrorMessage = escapeHtml(errorMessage);
                    
                    const errorContent = `
                        <div style='padding: 8px; min-width: 200px;'>
                            <h3 style='margin: 0 0 8px 0; color: #d93025; font-size: 16px;'>Error en la búsqueda</h3>
                            <div style='color: #5f6368; font-size: 14px;'>
                                <p style='margin: 4px 0;'>${{safeErrorMessage}}</p>
                            </div>
                        </div>
                    `;
                    
                    infoWindow.setContent(errorContent);
                    infoWindow.setPosition(map.getCenter());
                    infoWindow.open(map);
                    
                    // Log status only, not user input
                    console.error('Error en búsqueda de ubicación. Status:', status);
                }}
            }});
        }}

        function placeMarker(location) {{
            // Remove existing edit marker
            if (editMarker) {{
                editMarker.setMap(null);
            }}

            // Create new marker
            editMarker = new google.maps.Marker({{
                position: location,
                map: map,
                draggable: true,
                icon: {{
                    url: EDIT_MARKER_ICON,
                    scaledSize: new google.maps.Size(MARKER_ICON_SIZE, MARKER_ICON_SIZE)
                }},
                title: 'Nueva ubicación',
                animation: google.maps.Animation.DROP
            }});

            // Update form with coordinates
            updateFormWithLocation(location);

            // Add drag end listener
            editMarker.addListener('dragend', (event) => {{
                updateFormWithLocation(event.latLng);
            }});
        }}

        function updateFormWithLocation(location) {{
            const lat = location.lat();
            const lng = location.lng();

            // Use geocoder to get address
            geocoder.geocode({{ location: location }}, (results, status) => {{
                let addressData = {{}};
                
                if (status === 'OK' && results && results[0]) {{
                    addressData.formatted = results[0].formatted_address;
                    addressData.place_id = results[0].place_id;
                    
                    // Extract address components
                    results[0].address_components.forEach(component => {{
                        if (component.types.includes('locality')) {{
                            addressData.city = component.long_name;
                        }}
                        if (component.types.includes('administrative_area_level_1')) {{
                            addressData.state = component.long_name;
                        }}
                        if (component.types.includes('country')) {{
                            addressData.country = component.long_name;
                        }}
                    }});
                }}

                // Send message to C# app
                const message = JSON.stringify({{
                    type: 'markerMoved',
                    lat: lat,
                    lng: lng,
                    address: addressData
                }});

                if (window.chrome && window.chrome.webview) {{
                    window.chrome.webview.postMessage(message);
                }}
            }});
        }}

        function setFormVisibility(visible) {{
            isFormVisible = visible;
            
            // Remove marker if form is hidden
            if (!visible && editMarker) {{
                editMarker.setMap(null);
                editMarker = null;
            }}
        }}

        function loadExistingMarker(lat, lng) {{
            if (lat && lng) {{
                const location = new google.maps.LatLng(parseFloat(lat), parseFloat(lng));
                placeMarker(location);
                map.setCenter(location);
            }}
        }}

        function renderAreas() {{
            // Limpiar shapes existentes
            shapes.forEach(shape => shape.setMap(null));
            shapes = [];

            // Renderizar cada área
            areas.forEach(area => {{
                try {{
                    let shape = null;
                    let position = null;

                    if (area.type === 'Polygon') {{
                        const path = JSON.parse(area.path);
                        const options = JSON.parse(area.options);
                        
                        shape = new google.maps.Polygon({{
                            paths: path,
                            ...options
                        }});
                        
                        // Calcular centro del polígono para el InfoWindow
                        if (area.center) {{
                            position = JSON.parse(area.center);
                        }}
                    }} else if (area.type === 'Circle') {{
                        const center = JSON.parse(area.center);
                        const options = JSON.parse(area.options);
                        
                        shape = new google.maps.Circle({{
                            center: center,
                            radius: area.radius,
                            ...options
                        }});
                        
                        position = center;
                    }} else if (area.type === 'Rectangle') {{
                        const bounds = JSON.parse(area.bounds);
                        const options = JSON.parse(area.options);
                        
                        shape = new google.maps.Rectangle({{
                            bounds: bounds,
                            ...options
                        }});
                        
                        // Calcular centro del rectángulo
                        position = {{
                            lat: (bounds.north + bounds.south) / 2,
                            lng: (bounds.east + bounds.west) / 2
                        }};
                    }} else if (area.type === 'Polyline') {{
                        const path = JSON.parse(area.path);
                        const options = JSON.parse(area.options);
                        
                        shape = new google.maps.Polyline({{
                            path: path,
                            ...options
                        }});
                        
                        // Usar primer punto como posición
                        if (path && path.length > 0) {{
                            position = path[0];
                        }}
                    }}

                    if (shape) {{
                        shape.setMap(map);
                        shapes.push(shape);

                        // Agregar listener para click con InfoWindow
                        shape.addListener('click', (event) => {{
                            const clickPosition = position || event.latLng;
                            showAreaInfo(area, clickPosition);
                        }});

                        // Agregar hover effect
                        shape.addListener('mouseover', () => {{
                            if (area.type === 'Polygon' || area.type === 'Rectangle' || area.type === 'Circle') {{
                                const options = JSON.parse(area.options);
                                shape.setOptions({{
                                    fillOpacity: (options.fillOpacity || 0.5) * 1.3,
                                    strokeWeight: (options.strokeWeight || 2) + 1
                                }});
                            }}
                        }});

                        shape.addListener('mouseout', () => {{
                            if (area.type === 'Polygon' || area.type === 'Rectangle' || area.type === 'Circle') {{
                                const options = JSON.parse(area.options);
                                shape.setOptions(options);
                            }}
                        }});
                    }}
                }} catch (error) {{
                    console.error('Error al renderizar área', area.nombre, error);
                }}
            }});
        }}

        function renderUbicaciones() {{
            // Limpiar markers existentes
            markers.forEach(marker => marker.setMap(null));
            markers = [];

            // Renderizar cada ubicación como marker
            ubicaciones.forEach(ubicacion => {{
                try {{
                    if (!ubicacion.latitud || !ubicacion.longitud) {{
                        return;
                    }}

                    const marker = new google.maps.Marker({{
                        position: {{ 
                            lat: parseFloat(ubicacion.latitud), 
                            lng: parseFloat(ubicacion.longitud) 
                        }},
                        map: map,
                        title: ubicacion.nombre,
                        icon: ubicacion.icono || undefined
                    }});

                    markers.push(marker);

                    // Agregar listener para click con InfoWindow
                    marker.addListener('click', () => {{
                        showUbicacionInfo(ubicacion, marker.getPosition());
                    }});
                }} catch (error) {{
                    console.error('Error al renderizar ubicación', ubicacion.nombre, error);
                }}
            }});
        }}

        function showAreaInfo(area, position) {{
            const content = `
                <div style='padding: 8px; min-width: 200px;'>
                    <h3 style='margin: 0 0 8px 0; color: #1a73e8; font-size: 16px;'>${{area.nombre}}</h3>
                    <div style='color: #5f6368; font-size: 14px;'>
                        <p style='margin: 4px 0;'><strong>Tipo:</strong> ${{area.type}}</p>
                        <p style='margin: 4px 0;'><strong>ID:</strong> ${{area.idArea}}</p>
                        ${{area.radius ? `<p style='margin: 4px 0;'><strong>Radio:</strong> ${{area.radius.toFixed(0)}}m</p>` : ''}}
                    </div>
                </div>
            `;
            
            infoWindow.setContent(content);
            infoWindow.setPosition(position);
            infoWindow.open(map);
        }}

        function showUbicacionInfo(ubicacion, position) {{
            // Use reverse geocoding to get current address information
            geocoder.geocode({{ location: position }}, (results, status) => {{
                let direccionActual = escapeHtml(ubicacion.direccionCompleta || '');
                
                // If geocoding is successful, use the current address
                if (status === 'OK' && results && results[0]) {{
                    direccionActual = escapeHtml(results[0].formatted_address);
                }}
                
                // Create info window content with geocoded address
                const content = `
                    <div style='padding: 8px; min-width: 250px;'>
                        <h3 style='margin: 0 0 8px 0; color: #1a73e8; font-size: 16px;'>${{escapeHtml(ubicacion.nombre)}}</h3>
                        <div style='color: #5f6368; font-size: 14px;'>
                            ${{ubicacion.descripcion ? `<p style='margin: 4px 0;'>${{escapeHtml(ubicacion.descripcion)}}</p>` : ''}}
                            ${{direccionActual ? `<p style='margin: 4px 0;'><strong>Dirección:</strong> ${{direccionActual}}</p>` : ''}}
                            ${{ubicacion.telefono ? `<p style='margin: 4px 0;'><strong>Tel:</strong> ${{escapeHtml(ubicacion.telefono)}}</p>` : ''}}
                            ${{ubicacion.email ? `<p style='margin: 4px 0;'><strong>Email:</strong> ${{escapeHtml(ubicacion.email)}}</p>` : ''}}
                            <p style='margin: 4px 0; font-size: 12px;'><strong>Coordenadas:</strong> ${{escapeHtml(String(ubicacion.latitud))}}, ${{escapeHtml(String(ubicacion.longitud))}}</p>
                        </div>
                    </div>
                `;
                
                infoWindow.setContent(content);
                infoWindow.setPosition(position);
                infoWindow.open(map);
            }});
        }}

        function showSelectedLocationMarker(ubicacion, position) {{
            // Clear any existing timeout to prevent race conditions
            if (selectedMarkerTimeout) {{
                clearTimeout(selectedMarkerTimeout);
                selectedMarkerTimeout = null;
            }}

            // Remove previous selected location marker if exists
            if (selectedLocationMarker) {{
                selectedLocationMarker.setMap(null);
            }}

            // Create a new marker for the selected location with distinctive green color
            selectedLocationMarker = new google.maps.Marker({{
                position: position,
                map: map,
                title: ubicacion.nombre,
                icon: {{
                    url: SELECTED_MARKER_ICON,
                    scaledSize: new google.maps.Size(MARKER_ICON_SIZE, MARKER_ICON_SIZE)
                }},
                animation: google.maps.Animation.BOUNCE,
                zIndex: 9999 // Ensure it's on top
            }});

            // Stop bouncing after 2 seconds, storing the timeout reference
            selectedMarkerTimeout = setTimeout(() => {{
                if (selectedLocationMarker) {{
                    selectedLocationMarker.setAnimation(null);
                }}
                selectedMarkerTimeout = null;
            }}, 2000);

            // Show info window for the selected location
            showUbicacionInfo(ubicacion, position);
        }}

        // Inicializar el mapa cuando cargue la página
        window.onload = initMap;
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Genera el HTML para el mapa de Áreas con herramientas de dibujo
        /// </summary>
        private string GenerateAreasMapHtml(string apiKey, string centerLat, string centerLng, int zoom, string areasJson)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Áreas Map</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        html, body {{
            height: 100%;
            width: 100%;
            overflow: hidden;
        }}
        #map {{
            height: 100%;
            width: 100%;
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    
    <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=drawing,geometry,places&callback=initMap' async defer></script>
    <script>
        // Constants
        const SEARCH_MARKER_ICON = 'https://maps.google.com/mapfiles/ms/icons/blue-dot.png';
        const MARKER_ICON_SIZE = 40;

        let map;
        let drawingManager;
        let currentShape = null;
        let existingShapes = [];
        let searchMarker = null;
        let infoWindow;

        // HTML encoding function for defense-in-depth security
        function escapeHtml(text) {{
            if (!text) return '';
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }}

        function initMap() {{
            // Initialize map
            map = new google.maps.Map(document.getElementById('map'), {{
                center: {{ lat: {centerLat}, lng: {centerLng} }},
                zoom: {zoom},
                mapTypeId: 'roadmap'
            }});

            // Create InfoWindow for search results
            infoWindow = new google.maps.InfoWindow();

            // Initialize Drawing Manager
            drawingManager = new google.maps.drawing.DrawingManager({{
                drawingMode: null,
                drawingControl: true,
                drawingControlOptions: {{
                    position: google.maps.ControlPosition.TOP_CENTER,
                    drawingModes: [
                        google.maps.drawing.OverlayType.POLYGON,
                        google.maps.drawing.OverlayType.CIRCLE,
                        google.maps.drawing.OverlayType.RECTANGLE
                    ]
                }},
                polygonOptions: {{
                    fillColor: '#FF0000',
                    fillOpacity: 0.35,
                    strokeWeight: 2,
                    strokeColor: '#FF0000',
                    editable: true,
                    draggable: true
                }},
                circleOptions: {{
                    fillColor: '#FF0000',
                    fillOpacity: 0.35,
                    strokeWeight: 2,
                    strokeColor: '#FF0000',
                    editable: true,
                    draggable: true
                }},
                rectangleOptions: {{
                    fillColor: '#FF0000',
                    fillOpacity: 0.35,
                    strokeWeight: 2,
                    strokeColor: '#FF0000',
                    editable: true,
                    draggable: true
                }}
            }});

            drawingManager.setMap(map);

            // Handle shape complete
            google.maps.event.addListener(drawingManager, 'overlaycomplete', function(event) {{
                // Remove previous shape if exists
                if (currentShape) {{
                    currentShape.setMap(null);
                }}

                currentShape = event.overlay;
                
                // Set drawing mode to null to allow editing
                drawingManager.setDrawingMode(null);

                // Extract shape data
                const shapeData = extractShapeData(event.type, event.overlay);
                
                // Send message to C#
                window.chrome.webview.postMessage({{
                    type: 'shapeDrawn',
                    shapeType: event.type,
                    ...shapeData
                }});

                // Add listeners for shape edits
                addShapeEditListeners(event.type, event.overlay);
            }});

            // Load existing areas
            loadExistingAreas({areasJson});
        }}

        function extractShapeData(type, shape) {{
            const data = {{}};

            if (type === 'polygon') {{
                const path = shape.getPath();
                data.path = [];
                for (let i = 0; i < path.getLength(); i++) {{
                    const point = path.getAt(i);
                    data.path.push({{ lat: point.lat(), lng: point.lng() }});
                }}
                
                // Calculate bounds and center
                const bounds = new google.maps.LatLngBounds();
                data.path.forEach(point => bounds.extend(point));
                const center = bounds.getCenter();
                data.center = {{ lat: center.lat(), lng: center.lng() }};
                data.bounds = {{
                    north: bounds.getNorthEast().lat(),
                    east: bounds.getNorthEast().lng(),
                    south: bounds.getSouthWest().lat(),
                    west: bounds.getSouthWest().lng()
                }};
            }} 
            else if (type === 'circle') {{
                const center = shape.getCenter();
                data.center = {{ lat: center.lat(), lng: center.lng() }};
                data.radius = shape.getRadius();
                
                const bounds = shape.getBounds();
                data.bounds = {{
                    north: bounds.getNorthEast().lat(),
                    east: bounds.getNorthEast().lng(),
                    south: bounds.getSouthWest().lat(),
                    west: bounds.getSouthWest().lng()
                }};
            }}
            else if (type === 'rectangle') {{
                const bounds = shape.getBounds();
                const ne = bounds.getNorthEast();
                const sw = bounds.getSouthWest();
                
                data.path = [
                    {{ lat: ne.lat(), lng: sw.lng() }},
                    {{ lat: ne.lat(), lng: ne.lng() }},
                    {{ lat: sw.lat(), lng: ne.lng() }},
                    {{ lat: sw.lat(), lng: sw.lng() }}
                ];
                
                const center = bounds.getCenter();
                data.center = {{ lat: center.lat(), lng: center.lng() }};
                data.bounds = {{
                    north: ne.lat(),
                    east: ne.lng(),
                    south: sw.lat(),
                    west: sw.lng()
                }};
            }}

            return data;
        }}

        function addShapeEditListeners(type, shape) {{
            if (type === 'polygon') {{
                google.maps.event.addListener(shape.getPath(), 'set_at', function() {{
                    const shapeData = extractShapeData(type, shape);
                    window.chrome.webview.postMessage({{
                        type: 'shapeEdited',
                        shapeType: type,
                        ...shapeData
                    }});
                }});
                
                google.maps.event.addListener(shape.getPath(), 'insert_at', function() {{
                    const shapeData = extractShapeData(type, shape);
                    window.chrome.webview.postMessage({{
                        type: 'shapeEdited',
                        shapeType: type,
                        ...shapeData
                    }});
                }});
            }} 
            else if (type === 'circle') {{
                google.maps.event.addListener(shape, 'center_changed', function() {{
                    const shapeData = extractShapeData(type, shape);
                    window.chrome.webview.postMessage({{
                        type: 'shapeEdited',
                        shapeType: type,
                        ...shapeData
                    }});
                }});
                
                google.maps.event.addListener(shape, 'radius_changed', function() {{
                    const shapeData = extractShapeData(type, shape);
                    window.chrome.webview.postMessage({{
                        type: 'shapeEdited',
                        shapeType: type,
                        ...shapeData
                    }});
                }});
            }}
            else if (type === 'rectangle') {{
                google.maps.event.addListener(shape, 'bounds_changed', function() {{
                    const shapeData = extractShapeData(type, shape);
                    window.chrome.webview.postMessage({{
                        type: 'shapeEdited',
                        shapeType: type,
                        ...shapeData
                    }});
                }});
            }}
        }}

        function loadExistingAreas(areasData) {{
            if (!areasData || areasData.length === 0) return;

            areasData.forEach(area => {{
                let shape;

                if (area.type === 'Polygon' && area.path) {{
                    shape = new google.maps.Polygon({{
                        paths: area.path,
                        ...area.options
                    }});
                }} 
                else if (area.type === 'Circle' && area.center && area.radius) {{
                    shape = new google.maps.Circle({{
                        center: area.center,
                        radius: area.radius,
                        ...area.options
                    }});
                }}
                else if (area.type === 'Rectangle' && area.path) {{
                    const bounds = new google.maps.LatLngBounds();
                    area.path.forEach(point => bounds.extend(point));
                    shape = new google.maps.Rectangle({{
                        bounds: bounds,
                        ...area.options
                    }});
                }}

                if (shape) {{
                    shape.setMap(map);
                    existingShapes.push({{ id: area.idArea, shape: shape }});
                }}
            }});
        }}

        function searchLocation(query) {{
            if (!query || query.trim() === '') {{
                return;
            }}

            const request = {{
                query: query,
                fields: ['name', 'geometry', 'formatted_address']
            }};

            const service = new google.maps.places.PlacesService(map);
            
            service.findPlaceFromQuery(request, (results, status) => {{
                if (status === google.maps.places.PlacesServiceStatus.OK && results && results.length > 0) {{
                    const place = results[0];
                    
                    // Remove previous search marker if exists
                    if (searchMarker) {{
                        searchMarker.setMap(null);
                    }}

                    // Center map on the found location
                    if (place.geometry && place.geometry.location) {{
                        map.setCenter(place.geometry.location);
                        map.setZoom(15);

                        // Add a marker for the search result
                        searchMarker = new google.maps.Marker({{
                            position: place.geometry.location,
                            map: map,
                            title: place.name,
                            icon: {{
                                url: SEARCH_MARKER_ICON,
                                scaledSize: new google.maps.Size(MARKER_ICON_SIZE, MARKER_ICON_SIZE)
                            }},
                            animation: google.maps.Animation.DROP
                        }});

                        // Show info window with search result
                        const safeName = escapeHtml(place.name || 'Ubicación encontrada');
                        const safeAddress = place.formatted_address ? escapeHtml(place.formatted_address) : '';
                        
                        const content = `
                            <div style='padding: 8px; min-width: 200px;'>
                                <h3 style='margin: 0 0 8px 0; color: #1a73e8; font-size: 16px;'>${{safeName}}</h3>
                                <div style='color: #5f6368; font-size: 14px;'>
                                    ${{safeAddress ? `<p style='margin: 4px 0;'>${{safeAddress}}</p>` : ''}}
                                </div>
                            </div>
                        `;
                        
                        infoWindow.setContent(content);
                        infoWindow.open(map, searchMarker);
                    }}
                }} else {{
                    // Show error to user via InfoWindow
                    const errorMessages = {{
                        'ZERO_RESULTS': 'No se encontraron resultados para la búsqueda.',
                        'OVER_QUERY_LIMIT': 'Se ha excedido el límite de consultas. Intente más tarde.',
                        'REQUEST_DENIED': 'La solicitud fue denegada.',
                        'INVALID_REQUEST': 'La solicitud no es válida.',
                        'UNKNOWN_ERROR': 'Ocurrió un error desconocido. Intente nuevamente.'
                    }};
                    
                    const errorMessage = errorMessages[status] || errorMessages.UNKNOWN_ERROR;
                    const safeErrorMessage = escapeHtml(errorMessage);
                    
                    const errorContent = `
                        <div style='padding: 8px; min-width: 200px;'>
                            <h3 style='margin: 0 0 8px 0; color: #d93025; font-size: 16px;'>Error en la búsqueda</h3>
                            <div style='color: #5f6368; font-size: 14px;'>
                                <p style='margin: 4px 0;'>${{safeErrorMessage}}</p>
                            </div>
                        </div>
                    `;
                    
                    infoWindow.setContent(errorContent);
                    infoWindow.setPosition(map.getCenter());
                    infoWindow.open(map);
                    
                    // Log status only, not user input
                    console.error('Error en búsqueda de ubicación. Status:', status);
                }}
            }});
        }}

        function clearCurrentShape() {{
            if (currentShape) {{
                currentShape.setMap(null);
                currentShape = null;
            }}
        }}

        // Expose function to C#
        window.clearCurrentShape = clearCurrentShape;
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Maneja el evento de navegación completada del WebView2
        /// </summary>
        private async void MapWebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                await _loggingService.LogInformationAsync("WebView2 navegación completada exitosamente", "Ubicaciones", "MapWebView_NavigationCompleted");
            }
            else
            {
                await _loggingService.LogErrorAsync($"WebView2 navegación falló. Status: {args.WebErrorStatus}", null, "Ubicaciones", "MapWebView_NavigationCompleted");
            }
        }

        /// <summary>
        /// Maneja el cambio de pestaña en el TabView
        /// </summary>
        private async void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is TabView tabView && tabView.SelectedItem is TabViewItem selectedTab)
                {
                    var tabHeader = selectedTab.Header?.ToString() ?? "";
                    await _loggingService.LogInformationAsync($"Tab seleccionada: {tabHeader}", "Ubicaciones", "TabView_SelectionChanged");

                    // Recargar el mapa basado en la pestaña seleccionada
                    if (tabHeader == TAB_UBICACIONES)
                    {
                        // Recargar el mapa para ubicaciones con marcadores
                        await ViewModel.LoadUbicacionesAsync();
                        await LoadMapAsync();
                    }
                    else if (tabHeader == TAB_AREAS)
                    {
                        // Ensure AreasViewModel is initialized before loading the map
                        var areasViewModel = AreasPage?.ViewModel;
                        if (areasViewModel != null && !areasViewModel.IsMapInitialized)
                        {
                            await areasViewModel.InitializeAsync();
                        }
                        
                        // Forward any pending shape messages now that AreasPage is available
                        if (AreasPage != null && _pendingShapeMessage != null)
                        {
                            await _loggingService.LogInformationAsync(
                                "Forwarding pending shape message to AreasPage", 
                                "Ubicaciones", 
                                "TabView_SelectionChanged");
                            await AreasPage.HandleShapeMessageAsync(_pendingShapeMessage);
                            _pendingShapeMessage = null; // Clear after forwarding
                        }
                        
                        // Recargar el mapa para áreas con herramientas de dibujo
                        await LoadAreasMapAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cambiar de pestaña", ex, "Ubicaciones", "TabView_SelectionChanged");
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de refrescar
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _loggingService.LogInformationAsync("Refrescando ubicaciones y áreas del mapa", "Ubicaciones", "RefreshButton_Click");
                
                await ViewModel.RefreshAreasAsync();
                await ViewModel.LoadUbicacionesAsync();
                await LoadMapAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al refrescar", ex, "Ubicaciones", "RefreshButton_Click");
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de búsqueda del mapa
        /// </summary>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var searchQuery = MapSearchBox.Text?.Trim();
                
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    await ShowMessageDialogAsync("Búsqueda", "Por favor ingrese una ubicación para buscar");
                    return;
                }

                await _loggingService.LogInformationAsync($"Buscando ubicación: {searchQuery}", "Ubicaciones", "SearchButton_Click");

                if (MapWebView?.CoreWebView2 != null)
                {
                    // Use proper JavaScript encoding to prevent XSS attacks
                    var encodedQuery = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(searchQuery);
                    var script = $"searchLocation('{encodedQuery}');";
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al buscar ubicación", ex, "Ubicaciones", "SearchButton_Click");
                await ShowMessageDialogAsync("Error", "Ocurrió un error al buscar la ubicación");
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de agregar ubicación
        /// </summary>
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            _editingUbicacionId = null;
            _isFormVisible = true;
            FormTitle.Text = "Nueva Ubicación";
            ClearForm();
            LocationForm.Visibility = Visibility.Visible;

            // Notify map that form is visible
            await NotifyMapFormVisibility(true);
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de ubicaciones
        /// </summary>
        private async void UbicacionesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prevent multiple simultaneous map centering operations
            if (_isCenteringMap)
                return;

            // When a location is selected from the list, clear search box and show location on map
            if (ViewModel.SelectedUbicacion != null && 
                ViewModel.SelectedUbicacion.Latitud.HasValue && 
                ViewModel.SelectedUbicacion.Longitud.HasValue)
            {
                try
                {
                    _isCenteringMap = true;

                    // Clear the search box
                    MapSearchBox.Text = string.Empty;

                    // Center the map on the selected location and show info
                    await CenterMapOnUbicacion(ViewModel.SelectedUbicacion);
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al mostrar ubicación en el mapa", ex, "Ubicaciones", "UbicacionesList_SelectionChanged");
                }
                finally
                {
                    _isCenteringMap = false;
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de editar ubicación
        /// </summary>
        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int idUbicacion)
            {
                var ubicacion = ViewModel.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == idUbicacion);
                if (ubicacion != null)
                {
                    _isEditMode = true;
                    _editingUbicacionId = idUbicacion;
                    _isFormVisible = true;
                    FormTitle.Text = "Editar Ubicación";
                    LoadUbicacionToForm(ubicacion);
                    LocationForm.Visibility = Visibility.Visible;

                    // Notify map that form is visible
                    await NotifyMapFormVisibility(true);

                    // Load existing marker if coordinates exist
                    if (ubicacion.Latitud.HasValue && ubicacion.Longitud.HasValue)
                    {
                        await LoadMarkerOnMap(ubicacion.Latitud.Value, ubicacion.Longitud.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de eliminar ubicación
        /// </summary>
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

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        var response = await ViewModel.DeleteUbicacionAsync(idUbicacion);
                        
                        if (response.Success)
                        {
                            await LoadMapAsync();
                            await ShowMessageDialogAsync("Éxito", "Ubicación eliminada correctamente");
                        }
                        else
                        {
                            await ShowMessageDialogAsync("Error", response.Message ?? "Error al eliminar la ubicación");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _loggingService.LogErrorAsync("Error al eliminar ubicación", ex, "Ubicaciones", "DeleteButton_Click");
                        await ShowMessageDialogAsync("Error", "Ocurrió un error al eliminar la ubicación");
                    }
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de guardar
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar campos requeridos
                if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
                {
                    await ShowMessageDialogAsync("Validación", "El nombre es requerido");
                    return;
                }

                // Validate that coordinates were set from the map
                if (!_currentLatitud.HasValue || !_currentLongitud.HasValue)
                {
                    await ShowMessageDialogAsync("Validación", "Por favor, haz clic en el mapa para seleccionar una ubicación");
                    return;
                }

                var ubicacion = new UbicacionDto
                {
                    Nombre = NombreTextBox.Text,
                    Descripcion = DescripcionTextBox.Text,
                    Latitud = _currentLatitud.Value,
                    Longitud = _currentLongitud.Value,
                    DireccionCompleta = _currentDireccionCompleta,
                    Ciudad = _currentCiudad,
                    Estado = _currentEstado,
                    Pais = _currentPais,
                    PlaceId = _currentPlaceId,
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
                    _isFormVisible = false;
                    LocationForm.Visibility = Visibility.Collapsed;
                    ClearForm();
                    await NotifyMapFormVisibility(false);
                    await LoadMapAsync();
                    await ShowMessageDialogAsync("Éxito", _isEditMode ? "Ubicación actualizada correctamente" : "Ubicación creada correctamente");
                }
                else
                {
                    await ShowMessageDialogAsync("Error", response.Message ?? "Error al guardar la ubicación");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al guardar ubicación", ex, "Ubicaciones", "SaveButton_Click");
                await ShowMessageDialogAsync("Error", "Ocurrió un error al guardar la ubicación");
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de cancelar
        /// </summary>
        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isFormVisible = false;
            LocationForm.Visibility = Visibility.Collapsed;
            ClearForm();

            // Notify map that form is hidden
            await NotifyMapFormVisibility(false);
        }

        /// <summary>
        /// Limpia el formulario de ubicación
        /// </summary>
        private void ClearForm()
        {
            NombreTextBox.Text = string.Empty;
            DescripcionTextBox.Text = string.Empty;
            _currentLatitud = null;
            _currentLongitud = null;
            _currentDireccionCompleta = null;
            _currentCiudad = null;
            _currentEstado = null;
            _currentPais = null;
            _currentPlaceId = null;
            _isEditMode = false;
            _editingUbicacionId = null;
        }

        /// <summary>
        /// Notifica al mapa JavaScript sobre la visibilidad del formulario
        /// </summary>
        private async System.Threading.Tasks.Task NotifyMapFormVisibility(bool isVisible)
        {
            try
            {
                if (MapWebView?.CoreWebView2 != null)
                {
                    var visibilityParam = isVisible ? "true" : "false";
                    var script = $"setFormVisibility({visibilityParam});";
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al notificar visibilidad del formulario al mapa", ex, "Ubicaciones", "NotifyMapFormVisibility");
            }
        }

        /// <summary>
        /// Carga un marker en el mapa con las coordenadas especificadas
        /// </summary>
        private async System.Threading.Tasks.Task LoadMarkerOnMap(decimal lat, decimal lng)
        {
            try
            {
                if (MapWebView?.CoreWebView2 != null)
                {
                    var latStr = lat.ToString("F6", CultureInfo.InvariantCulture);
                    var lngStr = lng.ToString("F6", CultureInfo.InvariantCulture);
                    var script = $"loadExistingMarker({latStr}, {lngStr});";
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar marker en el mapa", ex, "Ubicaciones", "LoadMarkerOnMap");
            }
        }

        /// <summary>
        /// Carga una ubicación en el formulario para edición
        /// </summary>
        private void LoadUbicacionToForm(UbicacionDto ubicacion)
        {
            NombreTextBox.Text = ubicacion.Nombre ?? string.Empty;
            DescripcionTextBox.Text = ubicacion.Descripcion ?? string.Empty;
            
            // Load location data from existing ubicacion
            _currentLatitud = ubicacion.Latitud;
            _currentLongitud = ubicacion.Longitud;
            _currentDireccionCompleta = ubicacion.DireccionCompleta;
            _currentCiudad = ubicacion.Ciudad;
            _currentEstado = ubicacion.Estado;
            _currentPais = ubicacion.Pais;
            _currentPlaceId = ubicacion.PlaceId;
        }

        /// <summary>
        /// Centra el mapa en una ubicación y muestra su información
        /// </summary>
        private async Task CenterMapOnUbicacion(UbicacionDto ubicacion)
        {
            try
            {
                if (MapWebView?.CoreWebView2 != null)
                {
                    var latStr = ubicacion.Latitud.Value.ToString("F6", CultureInfo.InvariantCulture);
                    var lngStr = ubicacion.Longitud.Value.ToString("F6", CultureInfo.InvariantCulture);
                    
                    // Serialize the ubicacion for the JavaScript function
                    var ubicacionJson = JsonSerializer.Serialize(ubicacion);
                    // Convert to base64 to safely pass JSON data to JavaScript
                    var base64Json = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ubicacionJson));
                    
                    var script = $@"
                        (function() {{
                            var ubicacionJson = atob('{base64Json}');
                            var ubicacion = JSON.parse(ubicacionJson);
                            var position = {{ lat: {latStr}, lng: {lngStr} }};
                            map.setCenter(position);
                            map.setZoom(15);
                            showSelectedLocationMarker(ubicacion, position);
                        }})();
                    ";
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al centrar el mapa en la ubicación", ex, "Ubicaciones", "CenterMapOnUbicacion");
            }
        }

        /// <summary>
        /// Muestra un diálogo de mensaje
        /// </summary>
        private async System.Threading.Tasks.Task ShowMessageDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Selecciona una ubicación por su ID y centra el mapa en ella
        /// </summary>
        private async Task SelectAndCenterUbicacionAsync(int idUbicacion)
        {
            try
            {
                await _loggingService.LogInformationAsync($"Buscando ubicación con ID: {idUbicacion}", "Ubicaciones", "SelectAndCenterUbicacionAsync");

                // Buscar la ubicación en la lista de ubicaciones
                var ubicacion = ViewModel.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == idUbicacion);

                if (ubicacion != null)
                {
                    await _loggingService.LogInformationAsync($"Ubicación encontrada: {ubicacion.Nombre}", "Ubicaciones", "SelectAndCenterUbicacionAsync");

                    // Seleccionar la ubicación en el ListView
                    ViewModel.SelectedUbicacion = ubicacion;

                    // Esperar un momento para asegurar que el mapa esté inicializado
                    await Task.Delay(MAP_INITIALIZATION_DELAY_MS);

                    // Centrar el mapa en la ubicación
                    await CenterMapOnUbicacion(ubicacion);

                    await _loggingService.LogInformationAsync("Mapa centrado en la ubicación seleccionada", "Ubicaciones", "SelectAndCenterUbicacionAsync");
                }
                else
                {
                    await _loggingService.LogWarningAsync($"No se encontró ubicación con ID: {idUbicacion}", "Ubicaciones", "SelectAndCenterUbicacionAsync");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al seleccionar y centrar ubicación", ex, "Ubicaciones", "SelectAndCenterUbicacionAsync");
            }
        }
    }
}
