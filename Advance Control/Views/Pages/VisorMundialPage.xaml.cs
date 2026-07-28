using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Views.Pages
{
    public sealed partial class VisorMundialPage : Page
    {
        private const string DEFAULT_LATITUDE = "22.1497";
        private const string DEFAULT_LONGITUDE = "-100.975";
        private const int DEFAULT_ZOOM = 6;

        public VisorMundialViewModel ViewModel { get; }
        private readonly ILoggingService _loggingService;

        private volatile bool _isWebView2Initialized = false;
        private readonly SemaphoreSlim _webView2InitLock = new SemaphoreSlim(1, 1);
        private bool _isDisposed = false;

        public VisorMundialPage()
        {
            ViewModel = AppServices.Get<VisorMundialViewModel>();
            _loggingService = AppServices.Get<ILoggingService>();

            InitializeComponent();
            ButtonClickLogger.Attach(this, _loggingService, nameof(VisorMundialPage));
            DataContext = ViewModel;

            Loaded += VisorMundialPage_Loaded;
            Unloaded += VisorMundialPage_Unloaded;
        }

        private void VisorMundialPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _isDisposed = true;
        }

        private async void VisorMundialPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await EnsureWebView2InitializedAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al configurar WebView2 en Loaded", ex, "VisorMundialPage", "VisorMundialPage_Loaded");
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                await EnsureWebView2InitializedAsync();
                await ViewModel.InitializeAsync();
                await LoadMapAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al navegar al Visor Mundial", ex, "VisorMundialPage", "OnNavigatedTo");
            }
        }

        private async Task EnsureWebView2InitializedAsync()
        {
            if (_isDisposed || _isWebView2Initialized)
            {
                return;
            }

            await _webView2InitLock.WaitAsync();
            try
            {
                if (_isWebView2Initialized || _isDisposed || MapWebView == null)
                {
                    return;
                }

                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
                await MapWebView.EnsureCoreWebView2Async(env);

                if (_isDisposed || MapWebView.CoreWebView2 == null)
                {
                    return;
                }

                var coreWebView2 = MapWebView.CoreWebView2;
                coreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                var mapCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Advance Control", "map_cache");
                Directory.CreateDirectory(mapCacheDir);
                coreWebView2.SetVirtualHostNameToFolderMapping(
                    "ac-visor-mundial-local", mapCacheDir,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                _isWebView2Initialized = true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al inicializar WebView2", ex, "VisorMundialPage", "EnsureWebView2InitializedAsync");
            }
            finally
            {
                _webView2InitLock.Release();
            }
        }

        private async void CoreWebView2_WebMessageReceived(
            Microsoft.Web.WebView2.Core.CoreWebView2 sender,
            Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var rawString = args.TryGetWebMessageAsString();
                var message = !string.IsNullOrEmpty(rawString) ? rawString : args.WebMessageAsJson.ToString();

                var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                if (jsonDoc == null || !jsonDoc.TryGetValue("type", out var typeElement))
                {
                    return;
                }

                var messageType = typeElement.GetString();
                if (messageType == "pinClicked" && jsonDoc.TryGetValue("idUbicacion", out var idElement))
                {
                    var idUbicacion = idElement.GetInt32();
                    var ubicacion = ViewModel.Ubicaciones.FirstOrDefault(u => u.IdUbicacion == idUbicacion);
                    if (ubicacion != null)
                    {
                        await ViewModel.CargarEquiposDeUbicacionAsync(ubicacion);
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al procesar mensaje de WebView2", ex, "VisorMundialPage", "CoreWebView2_WebMessageReceived");
            }
        }

        private async Task LoadMapAsync()
        {
            try
            {
                await EnsureWebView2InitializedAsync();
                if (_isDisposed || MapWebView?.CoreWebView2 == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(ViewModel.MapsConfig?.ApiKey))
                {
                    await _loggingService.LogWarningAsync("ApiKey de Google Maps vacía, no se puede cargar el Visor Mundial", "VisorMundialPage", "LoadMapAsync");
                    return;
                }

                var centerParts = ViewModel.MapsConfig.DefaultCenter?.Split(',') ?? Array.Empty<string>();
                var centerLat = centerParts.Length > 0 ? centerParts[0].Trim() : DEFAULT_LATITUDE;
                var centerLng = centerParts.Length > 1 ? centerParts[1].Trim() : DEFAULT_LONGITUDE;
                var zoom = ViewModel.MapsConfig.DefaultZoom > 0 ? ViewModel.MapsConfig.DefaultZoom : DEFAULT_ZOOM;

                var ubicacionesJson = PrepareUbicacionesJson();
                var html = GenerateVisorMundialMapHtml(ViewModel.MapsConfig.ApiKey!, centerLat, centerLng, zoom, ubicacionesJson);

                var mapCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Advance Control", "map_cache");
                Directory.CreateDirectory(mapCacheDir);
                var mapFile = Path.Combine(mapCacheDir, "visor_mundial.html");
                await File.WriteAllTextAsync(mapFile, html, System.Text.Encoding.UTF8);
                MapWebView.CoreWebView2.Navigate("https://ac-visor-mundial-local/visor_mundial.html");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el mapa del Visor Mundial", ex, "VisorMundialPage", "LoadMapAsync");
            }
        }

        private string PrepareUbicacionesJson()
        {
            var ubicaciones = ViewModel.Ubicaciones
                .Select(u => new
                {
                    idUbicacion = u.IdUbicacion,
                    nombre = u.Nombre,
                    latitud = u.Latitud,
                    longitud = u.Longitud,
                    cantidadEquipos = u.CantidadEquipos,
                    colorOperaciones = ViewModel.ObtenerColorUbicacion(u, "operaciones"),
                    colorCobranza = ViewModel.ObtenerColorUbicacion(u, "cobranza")
                })
                .ToList();

            return JsonSerializer.Serialize(ubicaciones);
        }

        private async void ModoColoreoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModoColoreoComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string modo)
            {
                return;
            }

            ViewModel.ModoColoreo = modo;

            if (_isWebView2Initialized && MapWebView.CoreWebView2 != null)
            {
                await MapWebView.CoreWebView2.ExecuteScriptAsync($"setModo('{modo}');");
            }
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeAsync();
            await LoadMapAsync();
        }

        private void BtnTogglePanel_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsPanelOpen = !ViewModel.IsPanelOpen;
        }

        private static string GenerateVisorMundialMapHtml(string apiKey, string centerLat, string centerLng, int zoom, string ubicacionesJson)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <style>
    html, body, #map {{ margin: 0; height: 100%; width: 100%; font-family: Roboto, Arial, sans-serif; }}
  </style>
</head>
<body>
  <div id='map'></div>
  <script>
    let map;
    let markers = [];

    async function initMap() {{
      const {{ Map }} = await google.maps.importLibrary(""maps"");
      const {{ AdvancedMarkerElement, PinElement }} = await google.maps.importLibrary(""marker"");

      map = new Map(document.getElementById('map'), {{
        center: {{ lat: {centerLat}, lng: {centerLng} }},
        zoom: {zoom},
        mapId: 'DEMO_MAP_ID'
      }});

      const ubicaciones = {ubicacionesJson};
      ubicaciones.forEach(function(u) {{
        const pin = new PinElement({{
          background: u.colorOperaciones,
          borderColor: '#202020',
          glyphColor: '#ffffff'
        }});

        const marker = new AdvancedMarkerElement({{
          map: map,
          position: {{ lat: u.latitud, lng: u.longitud }},
          title: u.nombre + ' (' + u.cantidadEquipos + ' equipo(s))',
          content: pin.element
        }});

        marker._pin = pin;
        marker._data = u;
        marker.addListener('click', function() {{
          try {{
            window.chrome.webview.postMessage(JSON.stringify({{ type: 'pinClicked', idUbicacion: u.idUbicacion }}));
          }} catch(e) {{}}
        }});

        markers.push(marker);
      }});
    }}

    function setModo(modo) {{
      markers.forEach(function(m) {{
        m._pin.background = modo === 'cobranza' ? m._data.colorCobranza : m._data.colorOperaciones;
      }});
    }}
  </script>
  <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&callback=initMap' async defer></script>
</body>
</html>";
        }
    }
}
