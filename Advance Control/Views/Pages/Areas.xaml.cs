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
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página de gestión de áreas geográficas con Google Maps
    /// </summary>
    public sealed partial class Areas : Page
    {
        // Default coordinates for Mexico City if configuration is not available
        private const string DEFAULT_LATITUDE = "19.4326";
        private const string DEFAULT_LONGITUDE = "-99.1332";
        
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

        public Areas()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<AreasViewModel>();

            // Resolver el servicio de logging desde DI
            _loggingService = ((App)Application.Current).Host.Services.GetRequiredService<ILoggingService>();

            this.InitializeComponent();

            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;

            // Setup WebView2 message handler
            this.Loaded += Areas_Loaded;
        }

        private async void Areas_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async();
                MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al configurar WebView2 message handler", ex, "Areas", "Areas_Loaded");
            }
        }

        private async void CoreWebView2_WebMessageReceived(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = args.TryGetWebMessageAsString();
                await _loggingService.LogInformationAsync($"Mensaje recibido de WebView2: {message}", "Areas", "CoreWebView2_WebMessageReceived");

                // Parse the JSON message
                var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                
                if (jsonDoc != null && jsonDoc.TryGetValue("type", out var typeElement))
                {
                    var messageType = typeElement.GetString();

                    if (messageType == "shapeDrawn")
                    {
                        // Extract shape data
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
                            $"Shape dibujado: Type={_currentShapeType}, Path={_currentShapePath?.Substring(0, Math.Min(100, _currentShapePath?.Length ?? 0))}",
                            "Areas",
                            "CoreWebView2_WebMessageReceived");

                        // Mostrar mensaje al usuario
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            // Aquí podríamos mostrar un InfoBar o actualizar la UI
                        });
                    }
                    else if (messageType == "shapeEdited")
                    {
                        // Handle shape editing
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

                        await _loggingService.LogInformationAsync("Shape editado", "Areas", "CoreWebView2_WebMessageReceived");
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al procesar mensaje de WebView2", ex, "Areas", "CoreWebView2_WebMessageReceived");
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await InitializeMapAsync();
        }

        private async Task InitializeMapAsync()
        {
            try
            {
                await _loggingService.LogInformationAsync("Inicializando mapa de áreas", "Areas", "InitializeMapAsync");

                // Initialize ViewModel
                await ViewModel.InitializeAsync();

                // Get configuration
                var config = ViewModel.MapsConfig;
                
                string apiKey = config?.ApiKey ?? "YOUR_API_KEY";
                string centerLat = config?.DefaultCenter?.Split(',')[0]?.Trim() ?? DEFAULT_LATITUDE;
                string centerLng = config?.DefaultCenter?.Split(',')[1]?.Trim() ?? DEFAULT_LONGITUDE;
                int zoom = config?.DefaultZoom ?? 12;

                // Load areas for display
                var areas = ViewModel.Areas;
                var areasJson = JsonSerializer.Serialize(areas.Select(a => new
                {
                    idArea = a.IdArea,
                    nombre = a.Nombre,
                    type = a.TipoGeometria,
                    path = ParsePathJson(a),
                    center = ParseCenterJson(a),
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

                // Create HTML for Google Maps with Drawing Manager
                var html = CreateMapHtml(apiKey, centerLat, centerLng, zoom, areasJson);

                // Navigate to HTML
                MapWebView.NavigateToString(html);

                await _loggingService.LogInformationAsync("Mapa de áreas inicializado", "Areas", "InitializeMapAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al inicializar mapa", ex, "Areas", "InitializeMapAsync");
            }
        }

        private object? ParsePathJson(AreaDto area)
        {
            // Si el área tiene MetadataJSON, intentar extraer el path de allí
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
                catch
                {
                    // Ignore parsing errors
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

        private string CreateMapHtml(string apiKey, string centerLat, string centerLng, int zoom, string areasJson)
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
    
    <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=drawing,geometry&callback=initMap' async defer></script>
    <script>
        let map;
        let drawingManager;
        let currentShape = null;
        let existingShapes = [];

        function initMap() {{
            // Initialize map
            map = new google.maps.Map(document.getElementById('map'), {{
                center: {{ lat: {centerLat.Replace(",", ".")}, lng: {centerLng.Replace(",", ".")} }},
                zoom: {zoom},
                mapTypeId: 'roadmap'
            }});

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

        private async void MapWebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                await _loggingService.LogInformationAsync("Mapa cargado exitosamente", "Areas", "MapWebView_NavigationCompleted");
            }
            else
            {
                await _loggingService.LogErrorAsync($"Error al cargar mapa. HttpStatusCode: {args.HttpStatusCode}", null, "Areas", "MapWebView_NavigationCompleted");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.RefreshAreasAsync();
            await InitializeMapAsync();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            _editingAreaId = null;
            FormTitle.Text = "Nueva Área";
            
            // Clear form
            NombreTextBox.Text = string.Empty;
            DescripcionTextBox.Text = string.Empty;
            ColorComboBox.SelectedIndex = 0;
            ActivoCheckBox.IsChecked = true;

            // Show form
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

                    // Populate form
                    NombreTextBox.Text = area.Nombre;
                    DescripcionTextBox.Text = area.Descripcion;
                    ActivoCheckBox.IsChecked = area.Activo ?? true;

                    // Set color
                    for (int i = 0; i < ColorComboBox.Items.Count; i++)
                    {
                        if (ColorComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == area.ColorMapa)
                        {
                            ColorComboBox.SelectedIndex = i;
                            break;
                        }
                    }

                    // Show form
                    AreaForm.Visibility = Visibility.Visible;
                    _isFormVisible = true;

                    // TODO: Load area shape on map for editing
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
                    // Show confirmation dialog
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
                            // Show success message
                            var successDialog = new ContentDialog
                            {
                                Title = "Éxito",
                                Content = "Área eliminada correctamente.",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            };
                            await successDialog.ShowAsync();

                            // Reload map
                            await InitializeMapAsync();
                        }
                        else
                        {
                            // Show error message
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
            // Validate form
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

            if (string.IsNullOrEmpty(_currentShapeType) || string.IsNullOrEmpty(_currentShapePath))
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

            // Get selected color
            string selectedColor = "#FF0000";
            if (ColorComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                selectedColor = selectedItem.Tag.ToString() ?? "#FF0000";
            }

            // Create or update area
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
                MetadataJSON = JsonSerializer.Serialize(new
                {
                    path = _currentShapePath != null ? JsonSerializer.Deserialize<object>(_currentShapePath) : null,
                    center = _currentShapeCenter != null ? JsonSerializer.Deserialize<object>(_currentShapeCenter) : null,
                    bounds = _currentShapeBounds != null ? JsonSerializer.Deserialize<object>(_currentShapeBounds) : null,
                    radius = _currentShapeRadius
                })
            };

            // Parse center from JSON
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
                catch { }
            }

            // Parse bounds from JSON
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
                catch { }
            }

            // Set radius for circles
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
                // Show success message
                var successDialog = new ContentDialog
                {
                    Title = "Éxito",
                    Content = _isEditMode ? "Área actualizada correctamente." : "Área creada correctamente.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();

                // Hide form and reset
                CancelButton_Click(sender, e);

                // Clear current shape
                await MapWebView.CoreWebView2.ExecuteScriptAsync("clearCurrentShape();");

                // Reload map
                await InitializeMapAsync();
            }
            else
            {
                // Show error message
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
            // Hide form
            AreaForm.Visibility = Visibility.Collapsed;
            _isFormVisible = false;

            // Reset state
            _isEditMode = false;
            _editingAreaId = null;
            _currentShapeType = null;
            _currentShapePath = null;
            _currentShapeCenter = null;
            _currentShapeRadius = null;
            _currentShapeBounds = null;

            // Clear selection
            AreasList.SelectedItem = null;
        }
    }
}
