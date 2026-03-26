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
using global::Windows.Foundation;
using global::Windows.Foundation.Collections;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.Models;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página de ubicaciones con visualización de Google Maps
    /// </summary>
    public sealed partial class UbicacionesPage : Page
    {
        // Default coordinates for Mexico City if configuration is not available
        private const string DEFAULT_LATITUDE = "19.4326";
        private const string DEFAULT_LONGITUDE = "-99.1332";
        
        // Delay in milliseconds to ensure map is initialized before centering
        private const int MAP_INITIALIZATION_DELAY_MS = 500;



        public UbicacionesViewModel ViewModel { get; }
        private readonly ILoggingService _loggingService;
        private readonly IActivityService _activityService;
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
        

        
        // WebView2 initialization state
        // Volatile ensures visibility across threads
        private volatile bool _isWebView2Initialized = false;
        private readonly SemaphoreSlim _webView2InitLock = new SemaphoreSlim(1, 1);
        private bool _isDisposed = false;

        public UbicacionesPage()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<UbicacionesViewModel>();

            // Resolver el servicio de logging desde DI
            _loggingService = AppServices.Get<ILoggingService>();

            // Resolver el servicio de actividades desde DI
            _activityService = AppServices.Get<IActivityService>();

            this.InitializeComponent();
            ButtonClickLogger.Attach(this, _loggingService, nameof(UbicacionesPage));

            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;

            // Setup WebView2 message handler
            this.Loaded += UbicacionesView_Loaded;
            this.Unloaded += UbicacionesView_Unloaded;
        }

        /// <summary>
        /// Cleanup resources when page is unloaded
        /// </summary>
        private void UbicacionesView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Mark as disposed to prevent further operations
            _isDisposed = true;
            
            // Give a brief moment for any in-flight initialization to complete
            // This is a best-effort approach to avoid ObjectDisposedException
            Task.Delay(100).ContinueWith(_ =>
            {
                try
                {
                    _webView2InitLock?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Semaphore already disposed, ignore
                }
            });
        }

        /// <summary>
        /// Ensures WebView2 is initialized and message handler is registered
        /// This must be called before any map loading to ensure messages are received
        /// Thread-safe implementation using SemaphoreSlim
        /// </summary>
        private async Task EnsureWebView2InitializedAsync()
        {
            // Check if page is disposed
            if (_isDisposed)
            {
                await _loggingService.LogWarningAsync("Cannot initialize WebView2 - page is disposed", "UbicacionesPage", "EnsureWebView2InitializedAsync");
                return;
            }
            
            if (_isWebView2Initialized)
            {
                return; // Already initialized
            }

            // Use semaphore to ensure thread-safe initialization
            await _webView2InitLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_isWebView2Initialized || _isDisposed)
                {
                    return; // Already initialized by another thread or page is disposed
                }

                await _loggingService.LogInformationAsync("Initializing CoreWebView2 and message handler", "UbicacionesPage", "EnsureWebView2InitializedAsync");
                
                await MapWebView.EnsureCoreWebView2Async();
                MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                
                _isWebView2Initialized = true;
                
                await _loggingService.LogInformationAsync("CoreWebView2 and message handler initialized successfully", "UbicacionesPage", "EnsureWebView2InitializedAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al inicializar WebView2 message handler", ex, "UbicacionesPage", "EnsureWebView2InitializedAsync");
                // Don't set _isWebView2Initialized to true so it can be retried
                throw; // Re-throw to notify caller of initialization failure
            }
            finally
            {
                _webView2InitLock.Release();
            }
        }

        private async void UbicacionesView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure WebView2 is initialized when page loads
                await EnsureWebView2InitializedAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al configurar WebView2 en Loaded event", ex, "UbicacionesPage", "UbicacionesView_Loaded");
            }
        }

        private async void CoreWebView2_WebMessageReceived(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = args.TryGetWebMessageAsString();
                await _loggingService.LogInformationAsync($"Mensaje recibido de WebView2: {message}", "UbicacionesPage", "CoreWebView2_WebMessageReceived");

                // Parse the JSON message
                var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                
                if (jsonDoc != null && jsonDoc.TryGetValue("type", out var typeElement))
                {
                    var messageType = typeElement.GetString();

                    if (messageType == "markerMoved" || messageType == "placeSelected")
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

                                // Extraer nombre del Place si es placeSelected
                                string? placeName = null;
                                if (messageType == "placeSelected" && 
                                    jsonDoc.TryGetValue("placeName", out var placeNameElement) &&
                                    placeNameElement.ValueKind == JsonValueKind.String)
                                {
                                    placeName = placeNameElement.GetString();
                                }

                                // Actualizar campos del formulario en el hilo de UI
                                var addressToDisplay = _currentDireccionCompleta;
                                var placeNameToDisplay = placeName;
                                var enqueued = this.DispatcherQueue.TryEnqueue(() =>
                                {
                                    try
                                    {
                                        // Siempre actualizar el buscador con la dirección
                                        if (MapSearchBox != null && !string.IsNullOrWhiteSpace(addressToDisplay))
                                        {
                                            MapSearchBox.Text = addressToDisplay;
                                        }

                                        // Auto-llenar Descripción con la dirección completa
                                        if (DescripcionTextBox != null && !string.IsNullOrWhiteSpace(addressToDisplay))
                                        {
                                            DescripcionTextBox.Text = addressToDisplay;
                                        }

                                        // Si es un Place, auto-llenar Nombre con el nombre del lugar
                                        if (NombreTextBox != null && !string.IsNullOrWhiteSpace(placeNameToDisplay))
                                        {
                                            NombreTextBox.Text = placeNameToDisplay;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _ = _loggingService.LogErrorAsync("Error al actualizar campos del formulario", ex, "UbicacionesPage", "CoreWebView2_WebMessageReceived");
                                    }
                                });
                                
                                if (!enqueued)
                                {
                                    await _loggingService.LogWarningAsync("No se pudo encolar la actualización de los campos del formulario", "UbicacionesPage", "CoreWebView2_WebMessageReceived");
                                }

                                var logMessage = $"Ubicación actualizada ({messageType}): Lat={lat}, Lng={lng}, Ciudad={_currentCiudad}, Estado={_currentEstado}, País={_currentPais}";
                                if (!string.IsNullOrWhiteSpace(_currentDireccionCompleta))
                                    logMessage += $", Dirección={_currentDireccionCompleta}";
                                if (!string.IsNullOrWhiteSpace(placeName))
                                    logMessage += $", Place={placeName}";
                                
                                await _loggingService.LogInformationAsync(
                                    logMessage, 
                                    "UbicacionesPage", 
                                    "CoreWebView2_WebMessageReceived");
                            }
                            else
                            {
                                await _loggingService.LogWarningAsync("Coordenadas recibidas no son números válidos", "UbicacionesPage", "CoreWebView2_WebMessageReceived");
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al procesar mensaje de WebView2", ex, "UbicacionesPage", "CoreWebView2_WebMessageReceived");
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                await _loggingService.LogInformationAsync("Navegando a página de Ubicaciones", "UbicacionesPage", "OnNavigatedTo");

                // Ensure CoreWebView2 is initialized and message handler is registered BEFORE loading any map
                await EnsureWebView2InitializedAsync();

                // Inicializar el mapa y cargar datos
                await ViewModel.InitializeAsync();

                // Cargar el HTML del mapa en el WebView2
                await LoadMapAsync();

                // Si se pasó un ID de ubicación como parámetro, seleccionarla y centrar el mapa
                if (e.Parameter is int idUbicacion)
                {
                    await _loggingService.LogInformationAsync($"Navegación con parámetro: IdUbicacion = {idUbicacion}", "UbicacionesPage", "OnNavigatedTo");
                    await SelectAndCenterUbicacionAsync(idUbicacion);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al navegar a Ubicaciones", ex, "UbicacionesPage", "OnNavigatedTo");
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
                    await _loggingService.LogWarningAsync("No hay configuración de Google Maps disponible", "UbicacionesPage", "LoadMapAsync");
                    return;
                }

                await _loggingService.LogInformationAsync("Cargando mapa de Google Maps", "UbicacionesPage", "LoadMapAsync");

                // Ensure CoreWebView2 is initialized before using NavigateToString
                await MapWebView.EnsureCoreWebView2Async();

                // Parsear el centro del mapa
                var centerParts = ViewModel.MapsConfig.DefaultCenter.Split(',');
                var lat = centerParts.Length > 0 ? centerParts[0].Trim() : DEFAULT_LATITUDE;
                var lng = centerParts.Length > 1 ? centerParts[1].Trim() : DEFAULT_LONGITUDE;

                // Serializar las ubicaciones como JSON
                var ubicacionesJson = JsonSerializer.Serialize(ViewModel.Ubicaciones);

                // Crear el HTML con Google Maps
                var html = GenerateMapHtml(
                    ViewModel.MapsConfig.ApiKey,
                    lat,
                    lng,
                    ViewModel.MapsConfig.DefaultZoom,
                    ubicacionesJson);

                // Cargar el HTML en el WebView2
                MapWebView.NavigateToString(html);

                await _loggingService.LogInformationAsync("Mapa cargado exitosamente", "UbicacionesPage", "LoadMapAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el mapa", ex, "UbicacionesPage", "LoadMapAsync");
            }
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
                await _loggingService.LogErrorAsync($"Error al ejecutar script en mapa: {script}", ex, "UbicacionesPage", "ExecuteMapScriptAsync");
            }
        }

        /// <summary>
        /// Genera el HTML para el mapa de Google Maps
        /// </summary>
        private string GenerateMapHtml(string apiKey, string lat, string lng, int zoom, string ubicacionesJson)
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
        @keyframes marker-drop {{
            0% {{ transform: translateY(-200px); opacity: 0; }}
            60% {{ transform: translateY(10px); opacity: 1; }}
            80% {{ transform: translateY(-5px); }}
            100% {{ transform: translateY(0); }}
        }}
        @keyframes marker-bounce {{
            0%, 100% {{ transform: translateY(0); }}
            25% {{ transform: translateY(-20px); }}
            50% {{ transform: translateY(0); }}
            75% {{ transform: translateY(-10px); }}
        }}
        .marker-drop {{ animation: marker-drop 0.5s ease-out; }}
        .marker-bounce {{ animation: marker-bounce 0.6s ease-in-out infinite; }}
    </style>
</head>
<body>
    <div id='map'></div>
    
    <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=places,marker'></script>
    <script>
        // Pin colors for different marker types
        const PIN_COLORS = {{
            search:   {{ background: '#4285F4', border: '#1a73e8', glyph: '#FFFFFF' }},
            edit:     {{ background: '#EA4335', border: '#d93025', glyph: '#FFFFFF' }},
            selected: {{ background: '#34A853', border: '#0d652d', glyph: '#FFFFFF' }},
            default:  {{ background: '#FF6B6B', border: '#CC5555', glyph: '#FFFFFF' }}
        }};

        let map;
        let ubicaciones = {ubicacionesJson};
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

        // Crea un PinElement con colores personalizados
        function createPinContent(colorKey) {{
            const colors = PIN_COLORS[colorKey] || PIN_COLORS.default;
            const pin = new google.maps.marker.PinElement({{
                background: colors.background,
                borderColor: colors.border,
                glyphColor: colors.glyph
            }});
            return pin.element;
        }}

        // Crea un elemento DOM con imagen para iconos personalizados
        function createIconContent(iconUrl) {{
            const img = document.createElement('img');
            img.src = iconUrl;
            img.style.width = '40px';
            img.style.height = '40px';
            return img;
        }}

        // Aplica animación CSS al content de un marker
        function animateMarker(marker, animClass) {{
            if (marker && marker.content) {{
                marker.content.classList.add(animClass);
                // Remover clase después de la animación para poder re-aplicar
                if (animClass === 'marker-drop') {{
                    setTimeout(() => marker.content.classList.remove(animClass), 500);
                }}
            }}
        }}

        // Remueve un AdvancedMarkerElement del mapa
        function removeMarker(marker) {{
            if (marker) {{
                marker.map = null;
            }}
        }}

        function initMap() {{
            // Crear el mapa con Map ID para AdvancedMarkerElement
            map = new google.maps.Map(document.getElementById('map'), {{
                center: {{ lat: {lat}, lng: {lng} }},
                zoom: {zoom},
                mapTypeId: 'roadmap',
                mapId: '3457a32dcb6331583ad98107'
            }});

            // Crear InfoWindow global
            infoWindow = new google.maps.InfoWindow();

            // Crear geocoder para reverse geocoding
            geocoder = new google.maps.Geocoder();

            // Renderizar las ubicaciones (markers)
            renderUbicaciones();

            // Click listener: detectar click normal o click sobre un Place/POI
            map.addListener('click', (event) => {{
                if (!isFormVisible) return;

                if (event.placeId) {{
                    // Click sobre un negocio/lugar (POI) — obtener nombre del Place
                    event.stop(); // Evitar que se abra el infoWindow default de Google
                    handlePlaceClick(event.placeId, event.latLng);
                }} else {{
                    // Click normal en el mapa — colocar pin sin nombre
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
                        removeMarker(searchMarker);
                    }}

                    // Center map on the found location
                    if (place.geometry && place.geometry.location) {{
                        map.setCenter(place.geometry.location);
                        map.setZoom(15);

                        // Add a marker for the search result
                        searchMarker = new google.maps.marker.AdvancedMarkerElement({{
                            position: place.geometry.location,
                            map: map,
                            title: place.name,
                            content: createPinContent('search')
                        }});
                        animateMarker(searchMarker, 'marker-drop');

                        // Auto-colocar el pin de edición en la ubicación del Place encontrado
                        if (isFormVisible) {{
                            placeMarkerFromPlace(place);
                        }}

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
                        infoWindow.open({{ anchor: searchMarker, map: map }});
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

        // Maneja click sobre un Place/POI del mapa — consulta detalles y coloca pin
        function handlePlaceClick(placeId, latLng) {{
            const service = new google.maps.places.PlacesService(map);
            service.getDetails({{
                placeId: placeId,
                fields: ['name', 'geometry', 'formatted_address']
            }}, (place, status) => {{
                if (status === google.maps.places.PlacesServiceStatus.OK && place) {{
                    const location = place.geometry ? place.geometry.location : latLng;
                    
                    // Construir un objeto compatible con placeMarkerFromPlace
                    const placeData = {{
                        name: place.name,
                        formatted_address: place.formatted_address,
                        geometry: {{ location: location }}
                    }};
                    placeMarkerFromPlace(placeData);
                }} else {{
                    // Si falla Place Details, colocar pin normal
                    placeMarker(latLng);
                }}
            }});
        }}

        function placeMarker(location) {{
            // Remove existing edit marker
            if (editMarker) {{
                removeMarker(editMarker);
            }}

            // Create new marker
            editMarker = new google.maps.marker.AdvancedMarkerElement({{
                position: location,
                map: map,
                gmpDraggable: true,
                title: 'Nueva ubicación',
                content: createPinContent('edit')
            }});
            animateMarker(editMarker, 'marker-drop');

            // Update form with coordinates
            updateFormWithLocation(location);

            // Add drag end listener
            editMarker.addEventListener('gmp-dragend', () => {{
                const pos = editMarker.position;
                updateFormWithLocation(new google.maps.LatLng(pos.lat, pos.lng));
            }});
        }}

        // Coloca el pin de edición desde un resultado de Google Places (incluye nombre del Place)
        function placeMarkerFromPlace(place) {{
            if (!place.geometry || !place.geometry.location) return;

            const location = place.geometry.location;

            // Remove existing edit marker
            if (editMarker) {{
                removeMarker(editMarker);
            }}

            // Create new draggable marker
            editMarker = new google.maps.marker.AdvancedMarkerElement({{
                position: location,
                map: map,
                gmpDraggable: true,
                title: place.name || 'Nueva ubicación',
                content: createPinContent('edit')
            }});
            animateMarker(editMarker, 'marker-drop');

            // Enviar datos del Place a C# (incluye nombre)
            const lat = location.lat();
            const lng = location.lng();

            geocoder.geocode({{ location: location }}, (results, status) => {{
                let addressData = {{}};
                
                if (status === 'OK' && results && results[0]) {{
                    addressData.formatted = results[0].formatted_address;
                    addressData.place_id = results[0].place_id;
                    
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

                const message = JSON.stringify({{
                    type: 'placeSelected',
                    lat: lat,
                    lng: lng,
                    placeName: place.name || '',
                    address: addressData
                }});

                if (window.chrome && window.chrome.webview) {{
                    window.chrome.webview.postMessage(message);
                }}
            }});

            // Add drag end listener (al arrastrar pierde el nombre del place)
            editMarker.addEventListener('gmp-dragend', () => {{
                const pos = editMarker.position;
                updateFormWithLocation(new google.maps.LatLng(pos.lat, pos.lng));
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
                removeMarker(editMarker);
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

        function renderUbicaciones() {{
            // Limpiar markers existentes
            markers.forEach(marker => removeMarker(marker));
            markers = [];

            // Renderizar cada ubicación como marker
            ubicaciones.forEach(ubicacion => {{
                try {{
                    if (!ubicacion.latitud || !ubicacion.longitud) {{
                        return;
                    }}

                    const content = ubicacion.icono 
                        ? createIconContent(ubicacion.icono) 
                        : createPinContent('default');

                    const marker = new google.maps.marker.AdvancedMarkerElement({{
                        position: {{ 
                            lat: parseFloat(ubicacion.latitud), 
                            lng: parseFloat(ubicacion.longitud) 
                        }},
                        map: map,
                        title: ubicacion.nombre,
                        content: content
                    }});

                    markers.push(marker);

                    // Agregar listener para click con InfoWindow
                    marker.addEventListener('gmp-click', () => {{
                        showUbicacionInfo(ubicacion, marker.position);
                    }});
                }} catch (error) {{
                    console.error('Error al renderizar ubicación', ubicacion.nombre, error);
                }}
            }});
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
                removeMarker(selectedLocationMarker);
            }}

            // Create a new marker for the selected location with distinctive green color
            selectedLocationMarker = new google.maps.marker.AdvancedMarkerElement({{
                position: position,
                map: map,
                title: ubicacion.nombre,
                content: createPinContent('selected'),
                zIndex: 9999
            }});
            animateMarker(selectedLocationMarker, 'marker-bounce');

            // Stop bouncing after 2 seconds
            selectedMarkerTimeout = setTimeout(() => {{
                if (selectedLocationMarker && selectedLocationMarker.content) {{
                    selectedLocationMarker.content.classList.remove('marker-bounce');
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
        /// Maneja el evento de navegación completada del WebView2
        /// </summary>
        private async void MapWebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                await _loggingService.LogInformationAsync("WebView2 navegación completada exitosamente", "UbicacionesPage", "MapWebView_NavigationCompleted");
            }
            else
            {
                await _loggingService.LogErrorAsync($"WebView2 navegación falló. Status: {args.WebErrorStatus}", null, "UbicacionesPage", "MapWebView_NavigationCompleted");
            }
        }

        /// <summary>
        /// Maneja el clic en el botón de refrescar
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _loggingService.LogInformationAsync("Refrescando ubicaciones del mapa", "UbicacionesPage", "RefreshButton_Click");
                
                await ViewModel.LoadUbicacionesAsync();
                await LoadMapAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al refrescar", ex, "UbicacionesPage", "RefreshButton_Click");
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

                await _loggingService.LogInformationAsync($"Buscando ubicación: {searchQuery}", "UbicacionesPage", "SearchButton_Click");

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
                await _loggingService.LogErrorAsync("Error al buscar ubicación", ex, "UbicacionesPage", "SearchButton_Click");
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
                    await _loggingService.LogErrorAsync("Error al mostrar ubicación en el mapa", ex, "UbicacionesPage", "UbicacionesList_SelectionChanged");
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
                            _activityService.Registrar("Ubicaciones", "Ubicación eliminada");
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
                        await _loggingService.LogErrorAsync("Error al eliminar ubicación", ex, "UbicacionesPage", "DeleteButton_Click");
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
                    var wasEditMode = _isEditMode;
                    _activityService.Registrar("Ubicaciones", wasEditMode ? "Ubicación modificada" : "Ubicación creada");
                    _isFormVisible = false;
                    LocationForm.Visibility = Visibility.Collapsed;
                    ClearForm();
                    await NotifyMapFormVisibility(false);
                    await LoadMapAsync();
                    await ShowMessageDialogAsync("Éxito", wasEditMode ? "Ubicación actualizada correctamente" : "Ubicación creada correctamente");
                }
                else
                {
                    await ShowMessageDialogAsync("Error", response.Message ?? "Error al guardar la ubicación");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al guardar ubicación", ex, "UbicacionesPage", "SaveButton_Click");
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
                await _loggingService.LogErrorAsync("Error al notificar visibilidad del formulario al mapa", ex, "UbicacionesPage", "NotifyMapFormVisibility");
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
                await _loggingService.LogErrorAsync("Error al cargar marker en el mapa", ex, "UbicacionesPage", "LoadMarkerOnMap");
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
                await _loggingService.LogErrorAsync("Error al centrar el mapa en la ubicación", ex, "UbicacionesPage", "CenterMapOnUbicacion");
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
                await _loggingService.LogInformationAsync($"Buscando ubicación con ID: {idUbicacion}", "UbicacionesPage", "SelectAndCenterUbicacionAsync");

                // Buscar la ubicación en la lista de ubicaciones
                var ubicacion = ViewModel.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == idUbicacion);

                if (ubicacion != null)
                {
                    await _loggingService.LogInformationAsync($"Ubicación encontrada: {ubicacion.Nombre}", "UbicacionesPage", "SelectAndCenterUbicacionAsync");

                    // Seleccionar la ubicación en el ListView
                    ViewModel.SelectedUbicacion = ubicacion;

                    // Esperar un momento para asegurar que el mapa esté inicializado
                    await Task.Delay(MAP_INITIALIZATION_DELAY_MS);

                    // Centrar el mapa en la ubicación
                    await CenterMapOnUbicacion(ubicacion);

                    await _loggingService.LogInformationAsync("Mapa centrado en la ubicación seleccionada", "UbicacionesPage", "SelectAndCenterUbicacionAsync");
                }
                else
                {
                    await _loggingService.LogWarningAsync($"No se encontró ubicación con ID: {idUbicacion}", "UbicacionesPage", "SelectAndCenterUbicacionAsync");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al seleccionar y centrar ubicación", ex, "UbicacionesPage", "SelectAndCenterUbicacionAsync");
            }
        }
    }
}

