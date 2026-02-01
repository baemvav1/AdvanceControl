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

        public UbicacionesViewModel ViewModel { get; }
        private readonly ILoggingService _loggingService;
        private bool _isEditMode = false;
        private int? _editingUbicacionId = null;

        public Ubicaciones()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<UbicacionesViewModel>();

            // Resolver el servicio de logging desde DI
            _loggingService = ((App)Application.Current).Host.Services.GetRequiredService<ILoggingService>();

            this.InitializeComponent();

            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
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
    
    <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}'></script>
    <script>
        let map;
        let areas = {areasJson};
        let ubicaciones = {ubicacionesJson};
        let shapes = [];
        let markers = [];
        let infoWindow;

        function initMap() {{
            // Crear el mapa
            map = new google.maps.Map(document.getElementById('map'), {{
                center: {{ lat: {lat}, lng: {lng} }},
                zoom: {zoom},
                mapTypeId: 'roadmap'
            }});

            // Crear InfoWindow global
            infoWindow = new google.maps.InfoWindow();

            // Renderizar las áreas
            renderAreas();

            // Renderizar las ubicaciones (markers)
            renderUbicaciones();
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
            const content = `
                <div style='padding: 8px; min-width: 250px;'>
                    <h3 style='margin: 0 0 8px 0; color: #1a73e8; font-size: 16px;'>${{ubicacion.nombre}}</h3>
                    <div style='color: #5f6368; font-size: 14px;'>
                        ${{ubicacion.descripcion ? `<p style='margin: 4px 0;'>${{ubicacion.descripcion}}</p>` : ''}}
                        ${{ubicacion.direccionCompleta ? `<p style='margin: 4px 0;'><strong>Dirección:</strong> ${{ubicacion.direccionCompleta}}</p>` : ''}}
                        ${{ubicacion.telefono ? `<p style='margin: 4px 0;'><strong>Tel:</strong> ${{ubicacion.telefono}}</p>` : ''}}
                        ${{ubicacion.email ? `<p style='margin: 4px 0;'><strong>Email:</strong> ${{ubicacion.email}}</p>` : ''}}
                        <p style='margin: 4px 0; font-size: 12px;'><strong>Coordenadas:</strong> ${{ubicacion.latitud}}, ${{ubicacion.longitud}}</p>
                    </div>
                </div>
            `;
            
            infoWindow.setContent(content);
            infoWindow.setPosition(position);
            infoWindow.open(map);
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
                await _loggingService.LogInformationAsync("WebView2 navegación completada exitosamente", "Ubicaciones", "MapWebView_NavigationCompleted");
            }
            else
            {
                await _loggingService.LogErrorAsync($"WebView2 navegación falló. Status: {args.WebErrorStatus}", null, "Ubicaciones", "MapWebView_NavigationCompleted");
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
        /// Maneja el clic en el botón de agregar ubicación
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            _editingUbicacionId = null;
            FormTitle.Text = "Nueva Ubicación";
            ClearForm();
            LocationForm.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de ubicaciones
        /// </summary>
        private void UbicacionesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Opcional: Puedes hacer algo cuando se selecciona una ubicación
            // Por ejemplo, centrar el mapa en la ubicación seleccionada
        }

        /// <summary>
        /// Maneja el clic en el botón de editar ubicación
        /// </summary>
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

                if (!decimal.TryParse(LatitudTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var latitud))
                {
                    await ShowMessageDialogAsync("Validación", "La latitud debe ser un número válido");
                    return;
                }

                if (!decimal.TryParse(LongitudTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var longitud))
                {
                    await ShowMessageDialogAsync("Validación", "La longitud debe ser un número válido");
                    return;
                }

                var ubicacion = new UbicacionDto
                {
                    Nombre = NombreTextBox.Text,
                    Descripcion = DescripcionTextBox.Text,
                    Latitud = latitud,
                    Longitud = longitud,
                    DireccionCompleta = DireccionTextBox.Text,
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
                    LocationForm.Visibility = Visibility.Collapsed;
                    ClearForm();
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
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LocationForm.Visibility = Visibility.Collapsed;
            ClearForm();
        }

        /// <summary>
        /// Limpia el formulario de ubicación
        /// </summary>
        private void ClearForm()
        {
            NombreTextBox.Text = string.Empty;
            DescripcionTextBox.Text = string.Empty;
            LatitudTextBox.Text = string.Empty;
            LongitudTextBox.Text = string.Empty;
            DireccionTextBox.Text = string.Empty;
            _isEditMode = false;
            _editingUbicacionId = null;
        }

        /// <summary>
        /// Carga una ubicación en el formulario para edición
        /// </summary>
        private void LoadUbicacionToForm(UbicacionDto ubicacion)
        {
            NombreTextBox.Text = ubicacion.Nombre ?? string.Empty;
            DescripcionTextBox.Text = ubicacion.Descripcion ?? string.Empty;
            LatitudTextBox.Text = ubicacion.Latitud?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            LongitudTextBox.Text = ubicacion.Longitud?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            DireccionTextBox.Text = ubicacion.DireccionCompleta ?? string.Empty;
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
    }
}
