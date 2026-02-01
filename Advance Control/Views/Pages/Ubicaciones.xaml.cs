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

                // Parsear el centro del mapa
                var centerParts = ViewModel.MapsConfig.DefaultCenter.Split(',');
                var lat = centerParts.Length > 0 ? centerParts[0].Trim() : DEFAULT_LATITUDE;
                var lng = centerParts.Length > 1 ? centerParts[1].Trim() : DEFAULT_LONGITUDE;

                // Serializar las áreas como JSON
                var areasJson = JsonSerializer.Serialize(ViewModel.Areas);

                // Crear el HTML con Google Maps
                var html = GenerateMapHtml(
                    ViewModel.MapsConfig.ApiKey,
                    lat,
                    lng,
                    ViewModel.MapsConfig.DefaultZoom,
                    areasJson);

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
        private string GenerateMapHtml(string apiKey, string lat, string lng, int zoom, string areasJson)
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
        let shapes = [];
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
                await _loggingService.LogInformationAsync("Refrescando áreas del mapa", "Ubicaciones", "RefreshButton_Click");
                
                await ViewModel.RefreshAreasAsync();
                await LoadMapAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al refrescar áreas", ex, "Ubicaciones", "RefreshButton_Click");
            }
        }
    }
}
