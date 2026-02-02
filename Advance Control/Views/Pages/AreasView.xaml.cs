using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Logging;
using Advance_Control.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página standalone de gestión de áreas geográficas con Google Maps
    /// </summary>
    public sealed partial class AreasView : Page
    {
        // Default coordinates for Mexico City if configuration is not available
        private const string DEFAULT_LATITUDE = "19.4326";
        private const string DEFAULT_LONGITUDE = "-99.1332";
        private const int DEFAULT_ZOOM = 12;
        
        public AreasViewModel ViewModel { get; }
        private readonly ILoggingService _loggingService;
        private bool _isEditMode = false;
        private int? _editingAreaId = null;
        private bool _isFormVisible = false;
        
        // Store polygon/shape data from Google Maps Drawing Manager
        private string? _currentShapeType = null;
        private string? _currentShapePath = null;
        private string? _currentShapeCenter = null;
        private decimal? _currentShapeRadius = null;
        private string? _currentShapeBounds = null;

        // WebView2 initialization state
        private volatile bool _isWebView2Initialized = false;
        private readonly SemaphoreSlim _webView2InitLock = new SemaphoreSlim(1, 1);
        private bool _isDisposed = false;

        public AreasView()
        {
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<AreasViewModel>();
            _loggingService = ((App)Application.Current).Host.Services.GetRequiredService<ILoggingService>();

            this.InitializeComponent();

            this.DataContext = ViewModel;

            this.Loaded += AreasView_Loaded;
            this.Unloaded += AreasView_Unloaded;
        }

        private void AreasView_Unloaded(object sender, RoutedEventArgs e)
        {
            _isDisposed = true;
            
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

        private async Task EnsureWebView2InitializedAsync()
        {
            if (_isDisposed)
            {
                await _loggingService.LogWarningAsync("Cannot initialize WebView2 - page is disposed", "AreasView", "EnsureWebView2InitializedAsync");
                return;
            }
            
            if (_isWebView2Initialized)
            {
                return;
            }

            await _webView2InitLock.WaitAsync();
            try
            {
                if (_isWebView2Initialized || _isDisposed)
                {
                    return;
                }

                await _loggingService.LogInformationAsync("Initializing CoreWebView2 and message handler", "AreasView", "EnsureWebView2InitializedAsync");
                
                await MapWebView.EnsureCoreWebView2Async();
                MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                
                _isWebView2Initialized = true;
                
                await _loggingService.LogInformationAsync("CoreWebView2 and message handler initialized successfully", "AreasView", "EnsureWebView2InitializedAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al inicializar WebView2 message handler", ex, "AreasView", "EnsureWebView2InitializedAsync");
                throw;
            }
            finally
            {
                _webView2InitLock.Release();
            }
        }

        private async void AreasView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await EnsureWebView2InitializedAsync();
                await ViewModel.InitializeAsync();
                await LoadMapAsync();
                await _loggingService.LogInformationAsync("Página de Áreas cargada", "AreasView", "AreasView_Loaded");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar página de Áreas", ex, "AreasView", "AreasView_Loaded");
            }
        }

        private async void CoreWebView2_WebMessageReceived(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = args.TryGetWebMessageAsString();
                await _loggingService.LogInformationAsync($"Mensaje recibido de WebView2: {message}", "AreasView", "CoreWebView2_WebMessageReceived");

                var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                
                if (jsonDoc != null && jsonDoc.TryGetValue("type", out var typeElement))
                {
                    var messageType = typeElement.GetString();

                    if (messageType == "shapeDrawn" || messageType == "shapeEdited")
                    {
                        if (jsonDoc.TryGetValue("shapeType", out var shapeTypeElement))
                        {
                            _currentShapeType = shapeTypeElement.GetString();
                        }

                        if (jsonDoc.TryGetValue("path", out var pathElement))
                        {
                            _currentShapePath = pathElement.GetRawText();
                        }

                        if (jsonDoc.TryGetValue("center", out var centerElement))
                        {
                            _currentShapeCenter = centerElement.GetRawText();
                        }

                        if (jsonDoc.TryGetValue("radius", out var radiusElement) && radiusElement.ValueKind == JsonValueKind.Number)
                        {
                            _currentShapeRadius = radiusElement.GetDecimal();
                        }

                        if (jsonDoc.TryGetValue("bounds", out var boundsElement))
                        {
                            _currentShapeBounds = boundsElement.GetRawText();
                        }

                        await _loggingService.LogInformationAsync(
                            $"Shape {messageType}: Type={_currentShapeType}, " +
                            $"Path={(string.IsNullOrEmpty(_currentShapePath) ? "EMPTY" : "SET")}, " +
                            $"Center={(string.IsNullOrEmpty(_currentShapeCenter) ? "EMPTY" : "SET")}, " +
                            $"Radius={(_currentShapeRadius.HasValue ? _currentShapeRadius.Value.ToString() : "NULL")}, " +
                            $"Bounds={(string.IsNullOrEmpty(_currentShapeBounds) ? "EMPTY" : "SET")}",
                            "AreasView",
                            "CoreWebView2_WebMessageReceived");
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al procesar mensaje de forma", ex, "AreasView", "CoreWebView2_WebMessageReceived");
            }
        }

        private async Task LoadMapAsync()
        {
            try
            {
                await EnsureWebView2InitializedAsync();

                var apiKey = App.GetConfigurationValue("GoogleMapsApiKey");
                if (string.IsNullOrEmpty(apiKey))
                {
                    await _loggingService.LogErrorAsync("Google Maps API Key no configurada", null, "AreasView", "LoadMapAsync");
                    return;
                }

                var centerLat = App.GetConfigurationValue("DefaultLatitude", DEFAULT_LATITUDE);
                var centerLng = App.GetConfigurationValue("DefaultLongitude", DEFAULT_LONGITUDE);
                var zoom = int.Parse(App.GetConfigurationValue("DefaultZoom", DEFAULT_ZOOM.ToString()));

                var areasJson = PrepareAreasJson();
                var html = GenerateAreasMapHtml(apiKey, centerLat, centerLng, zoom, areasJson);

                MapWebView.NavigateToString(html);
                ViewModel.IsMapInitialized = true;

                await _loggingService.LogInformationAsync("Mapa de áreas cargado exitosamente", "AreasView", "LoadMapAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el mapa de áreas", ex, "AreasView", "LoadMapAsync");
            }
        }

        private string PrepareAreasJson()
        {
            try
            {
                var areas = ViewModel.Areas
                    .Where(a => a.Activo == true)
                    .Select(a => new
                    {
                        idArea = a.IdArea,
                        nombre = a.Nombre,
                        type = a.TipoGeometria ?? "Polygon",
                        path = ParsePathJson(a),
                        center = ParseCenterJson(a),
                        radius = a.Radio,
                        options = new
                        {
                            fillColor = a.ColorMapa ?? "#FF0000",
                            fillOpacity = (double)(a.Opacidad ?? 0.35m),
                            strokeColor = a.ColorBorde ?? "#FF0000",
                            strokeWeight = a.AnchoBorde ?? 2,
                            editable = false,
                            draggable = false
                        }
                    })
                    .ToList();

                return JsonSerializer.Serialize(areas);
            }
            catch (Exception ex)
            {
                _ = _loggingService.LogErrorAsync("Error al preparar JSON de áreas", ex, "AreasView", "PrepareAreasJson");
                return "[]";
            }
        }

        private object? ParsePathJson(AreaDto area)
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
                catch (JsonException ex)
                {
                    _ = _loggingService.LogWarningAsync(
                        $"Error parsing path JSON for area {area.IdArea}",
                        "AreasView",
                        "ParsePathJson",
                        ex);
                }
            }
            return null;
        }

        private object? ParseCenterJson(AreaDto area)
        {
            if (area.CentroLatitud.HasValue && area.CentroLongitud.HasValue)
            {
                return new { lat = area.CentroLatitud.Value, lng = area.CentroLongitud.Value };
            }
            return null;
        }

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
        const SEARCH_MARKER_ICON = 'https://maps.google.com/mapfiles/ms/icons/blue-dot.png';
        const MARKER_ICON_SIZE = 40;

        let map;
        let drawingManager;
        let currentShape = null;
        let existingShapes = [];
        let searchMarker = null;
        let infoWindow;

        function escapeHtml(text) {{
            if (!text) return '';
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }}

        function initMap() {{
            map = new google.maps.Map(document.getElementById('map'), {{
                center: {{ lat: {centerLat}, lng: {centerLng} }},
                zoom: {zoom},
                mapTypeId: 'roadmap'
            }});

            infoWindow = new google.maps.InfoWindow();

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

            google.maps.event.addListener(drawingManager, 'overlaycomplete', function(event) {{
                if (currentShape) {{
                    currentShape.setMap(null);
                }}

                currentShape = event.overlay;
                
                drawingManager.setDrawingMode(null);

                const shapeData = extractShapeData(event.type, event.overlay);
                
                window.chrome.webview.postMessage({{
                    type: 'shapeDrawn',
                    shapeType: event.type,
                    ...shapeData
                }});

                addShapeEditListeners(event.type, event.overlay);
            }});

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
                    
                    if (searchMarker) {{
                        searchMarker.setMap(null);
                    }}

                    if (place.geometry && place.geometry.location) {{
                        map.setCenter(place.geometry.location);
                        map.setZoom(15);

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

        window.clearCurrentShape = clearCurrentShape;
        window.searchLocation = searchLocation;
    </script>
</body>
</html>";
        }

        private async Task ExecuteMapScriptAsync(string script)
        {
            try
            {
                if (_isWebView2Initialized && MapWebView.CoreWebView2 != null)
                {
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al ejecutar script en el mapa", ex, "AreasView", "ExecuteMapScriptAsync");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.RefreshAreasAsync();
                await LoadMapAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al refrescar áreas", ex, "AreasView", "RefreshButton_Click");
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            await _loggingService.LogInformationAsync(
                $"AddButton_Click - Before clearing form: _currentShapeType={_currentShapeType ?? "NULL"}",
                "AreasView",
                "AddButton_Click");
            
            _isEditMode = false;
            _editingAreaId = null;
            FormTitle.Text = "Nueva Área";
            
            NombreTextBox.Text = string.Empty;
            DescripcionTextBox.Text = string.Empty;
            ColorComboBox.SelectedIndex = 0;
            ActivoCheckBox.IsChecked = true;

            await _loggingService.LogInformationAsync(
                $"AddButton_Click - After setting up form: _currentShapeType={_currentShapeType ?? "NULL"}",
                "AreasView",
                "AddButton_Click");

            AreaForm.Visibility = Visibility.Visible;
            _isFormVisible = true;
        }

        private void AreasList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle area selection - could zoom to area on map
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int areaId)
            {
                var area = ViewModel.Areas.FirstOrDefault(a => a.IdArea == areaId);
                if (area != null)
                {
                    _isEditMode = true;
                    _editingAreaId = areaId;
                    FormTitle.Text = "Editar Área";

                    NombreTextBox.Text = area.Nombre;
                    DescripcionTextBox.Text = area.Descripcion;
                    ActivoCheckBox.IsChecked = area.Activo ?? true;

                    for (int i = 0; i < ColorComboBox.Items.Count; i++)
                    {
                        if (ColorComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == area.ColorMapa)
                        {
                            ColorComboBox.SelectedIndex = i;
                            break;
                        }
                    }

                    AreaForm.Visibility = Visibility.Visible;
                    _isFormVisible = true;

                    _currentShapeType = area.TipoGeometria;
                    
                    if (area.CentroLatitud.HasValue && area.CentroLongitud.HasValue)
                    {
                        _currentShapeCenter = JsonSerializer.Serialize(new Dictionary<string, decimal>
                        {
                            ["lat"] = area.CentroLatitud.Value,
                            ["lng"] = area.CentroLongitud.Value
                        });
                    }
                    
                    if (area.Radio.HasValue)
                    {
                        _currentShapeRadius = area.Radio.Value;
                    }
                    
                    if (area.BoundingBoxNE_Lat.HasValue && area.BoundingBoxNE_Lng.HasValue &&
                        area.BoundingBoxSW_Lat.HasValue && area.BoundingBoxSW_Lng.HasValue)
                    {
                        _currentShapeBounds = JsonSerializer.Serialize(new Dictionary<string, decimal>
                        {
                            ["north"] = area.BoundingBoxNE_Lat.Value,
                            ["east"] = area.BoundingBoxNE_Lng.Value,
                            ["south"] = area.BoundingBoxSW_Lat.Value,
                            ["west"] = area.BoundingBoxSW_Lng.Value
                        });
                    }
                    
                    if (!string.IsNullOrEmpty(area.MetadataJSON))
                    {
                        try
                        {
                            var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(area.MetadataJSON);
                            if (metadata != null && metadata.TryGetValue("path", out var pathElement))
                            {
                                _currentShapePath = pathElement.GetRawText();
                            }
                        }
                        catch (JsonException ex)
                        {
                            _ = _loggingService.LogErrorAsync(
                                "Error al parsear MetadataJSON del área",
                                ex,
                                "AreasView",
                                "EditButton_Click");
                        }
                    }
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int areaId)
            {
                var area = ViewModel.Areas.FirstOrDefault(a => a.IdArea == areaId);
                if (area != null)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Confirmar eliminación",
                        Content = $"¿Está seguro de que desea eliminar el área '{area.Nombre}'?",
                        PrimaryButtonText = "Eliminar",
                        CloseButtonText = "Cancelar",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        var deleteResult = await ViewModel.DeleteAreaAsync(areaId);

                        if (deleteResult.Success)
                        {
                            var successDialog = new ContentDialog
                            {
                                Title = "Éxito",
                                Content = "Área eliminada correctamente.",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            };
                            await successDialog.ShowAsync();

                            await LoadMapAsync();
                        }
                        else
                        {
                            var errorDialog = new ContentDialog
                            {
                                Title = "Error",
                                Content = deleteResult.Message ?? "Error al eliminar el área.",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                    }
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await _loggingService.LogInformationAsync(
                $"SaveButton_Click - _isEditMode={_isEditMode}, _currentShapeType={_currentShapeType ?? "NULL"}, " +
                $"_currentShapePath={(string.IsNullOrEmpty(_currentShapePath) ? "EMPTY" : "SET")}, " +
                $"_currentShapeCenter={(string.IsNullOrEmpty(_currentShapeCenter) ? "EMPTY" : "SET")}, " +
                $"_currentShapeRadius={(_currentShapeRadius.HasValue ? _currentShapeRadius.Value.ToString() : "NULL")}",
                "AreasView",
                "SaveButton_Click");
            
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                var dialog = new ContentDialog
                {
                    Title = "Validación",
                    Content = "El nombre del área es requerido.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            if (!_isEditMode && string.IsNullOrEmpty(_currentShapeType))
            {
                await _loggingService.LogErrorAsync(
                    "Validation failed: _currentShapeType is null or empty when creating a new area.",
                    null,
                    "AreasView",
                    "SaveButton_Click");
                
                var dialog = new ContentDialog
                {
                    Title = "Validación",
                    Content = "Debe dibujar un área en el mapa antes de guardar.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            if (!_isEditMode)
            {
                bool hasValidShapeData = false;
                
                if (_currentShapeType?.ToLower() == "circle")
                {
                    hasValidShapeData = !string.IsNullOrEmpty(_currentShapeCenter) && _currentShapeRadius.HasValue;
                }
                else
                {
                    hasValidShapeData = !string.IsNullOrEmpty(_currentShapePath);
                }

                if (!hasValidShapeData)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Validación",
                        Content = "Debe dibujar un área en el mapa antes de guardar.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                    return;
                }
            }

            string selectedColor = "#FF0000";
            if (ColorComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                selectedColor = selectedItem.Tag.ToString() ?? "#FF0000";
            }

            var area = new AreaDto
            {
                IdArea = _editingAreaId ?? 0,
                Nombre = NombreTextBox.Text.Trim(),
                Descripcion = DescripcionTextBox.Text?.Trim(),
                ColorMapa = selectedColor,
                Opacidad = 0.35m,
                ColorBorde = selectedColor,
                AnchoBorde = 2,
                Activo = ActivoCheckBox.IsChecked,
                TipoGeometria = _currentShapeType ?? "Polygon",
                MetadataJSON = _isEditMode && string.IsNullOrEmpty(_currentShapePath)
                    ? null
                    : JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        ["path"] = _currentShapePath != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapePath) : (object?)null,
                        ["center"] = _currentShapeCenter != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapeCenter) : (object?)null,
                        ["bounds"] = _currentShapeBounds != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapeBounds) : (object?)null,
                        ["radius"] = _currentShapeRadius
                    })
            };

            if (!string.IsNullOrEmpty(_currentShapeCenter))
            {
                try
                {
                    var center = JsonSerializer.Deserialize<Dictionary<string, decimal>>(_currentShapeCenter);
                    if (center != null)
                    {
                        area.CentroLatitud = center.GetValueOrDefault("lat");
                        area.CentroLongitud = center.GetValueOrDefault("lng");
                    }
                }
                catch (JsonException ex)
                {
                    _ = _loggingService.LogWarningAsync(
                        "Error parsing center JSON",
                        "AreasView",
                        "SaveButton_Click",
                        ex);
                }
            }

            if (!string.IsNullOrEmpty(_currentShapeBounds))
            {
                try
                {
                    var bounds = JsonSerializer.Deserialize<Dictionary<string, decimal>>(_currentShapeBounds);
                    if (bounds != null)
                    {
                        area.BoundingBoxNE_Lat = bounds.GetValueOrDefault("north");
                        area.BoundingBoxNE_Lng = bounds.GetValueOrDefault("east");
                        area.BoundingBoxSW_Lat = bounds.GetValueOrDefault("south");
                        area.BoundingBoxSW_Lng = bounds.GetValueOrDefault("west");
                    }
                }
                catch (JsonException ex)
                {
                    _ = _loggingService.LogWarningAsync(
                        "Error parsing bounds JSON",
                        "AreasView",
                        "SaveButton_Click",
                        ex);
                }
            }

            if (_currentShapeRadius.HasValue)
            {
                area.Radio = _currentShapeRadius.Value;
            }

            ApiResponse result;
            if (_isEditMode && _editingAreaId.HasValue)
            {
                result = await ViewModel.UpdateAreaAsync(area);
            }
            else
            {
                result = await ViewModel.CreateAreaAsync(area);
            }

            if (result.Success)
            {
                var successDialog = new ContentDialog
                {
                    Title = "Éxito",
                    Content = _isEditMode ? "Área actualizada correctamente." : "Área creada correctamente.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();

                CancelButton_Click(sender, e);

                await ExecuteMapScriptAsync("clearCurrentShape();");
                await LoadMapAsync();
            }
            else
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = result.Message ?? "Error al guardar el área.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            AreaForm.Visibility = Visibility.Collapsed;
            _isFormVisible = false;

            _isEditMode = false;
            _editingAreaId = null;
            _currentShapeType = null;
            _currentShapePath = null;
            _currentShapeCenter = null;
            _currentShapeRadius = null;
            _currentShapeBounds = null;

            AreasList.SelectedItem = null;
        }

        private async void MapWebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                await _loggingService.LogInformationAsync("WebView2 navegación completada exitosamente", "AreasView", "MapWebView_NavigationCompleted");
            }
            else
            {
                await _loggingService.LogErrorAsync($"WebView2 navegación falló. Status: {args.WebErrorStatus}", null, "AreasView", "MapWebView_NavigationCompleted");
            }
        }
    }
}
