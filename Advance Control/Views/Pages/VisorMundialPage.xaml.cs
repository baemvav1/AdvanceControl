using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
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
        private const int DEFAULT_ZOOM = 5;

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
                else if (messageType == "colaboradorClicked" && jsonDoc.TryGetValue("credencialId", out var credencialElement))
                {
                    var credencialId = credencialElement.GetInt64();
                    var colaborador = ViewModel.ColaboradoresPuntos.FirstOrDefault(c => c.CredencialId == credencialId);
                    if (colaborador != null)
                    {
                        await ViewModel.CargarDetalleColaboradorAsync(colaborador);
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
                // Zoom fijo (no el de ViewModel.MapsConfig, que es compartido con otras páginas y
                // suele estar pensado para acercarse a una ubicación puntual): aquí siempre debe
                // verse el país completo.
                var zoom = DEFAULT_ZOOM;

                var puntosJson = ViewModel.Modo == VisorMundialModo.ColaboradoresPorArea
                    ? PrepareColaboradoresJson()
                    : PrepareUbicacionesJson();
                var html = GenerateVisorMundialMapHtml(ViewModel.MapsConfig.ApiKey!, centerLat, centerLng, zoom, puntosJson);

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
            var puntos = ViewModel.Ubicaciones
                .Select(u => new
                {
                    idUbicacion = u.IdUbicacion,
                    lat = u.Latitud,
                    lng = u.Longitud,
                    color = ViewModel.ObtenerColorUbicacion(u),
                    glyph = (string?)null,
                    title = u.Nombre + " (" + u.CantidadEquipos + " equipo(s))"
                })
                .ToList();

            return JsonSerializer.Serialize(puntos);
        }

        private string PrepareColaboradoresJson()
        {
            var puntos = ViewModel.ColaboradoresPuntos
                .Select(p => new
                {
                    idUbicacion = (int?)null,
                    idColaborador = (long?)p.CredencialId,
                    lat = p.Latitud,
                    lng = p.Longitud,
                    color = "#4285F4",
                    glyph = p.Glyph,
                    title = p.NombreCompleto + (string.IsNullOrWhiteSpace(p.Cargo) ? "" : $" ({p.Cargo})") + " — " + p.NombreArea
                })
                .ToList();

            return JsonSerializer.Serialize(puntos);
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeAsync();
            if (ViewModel.Modo == VisorMundialModo.ColaboradoresPorArea)
            {
                await ViewModel.EnsureColaboradoresDataAsync(forceReload: true);
            }
            await LoadMapAsync();
        }

        private void BtnTogglePanel_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsPanelOpen = !ViewModel.IsPanelOpen;
        }

        /// <summary>
        /// Clic derecho sobre un nodo de factura dentro del árbol de una tarjeta de equipo o del
        /// árbol de detalle de un colaborador abre "Ver factura"/"Ver operación", igual que en
        /// Detalle Clientes. Cada opción solo aparece cuando el nodo la tiene disponible.
        /// </summary>
        private void ArbolNodo_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not DetalleClienteTreeItem nodo)
            {
                return;
            }

            if (!nodo.IdFactura.HasValue && !nodo.IdOperacion.HasValue)
            {
                return;
            }

            var menu = new MenuFlyout();

            if (nodo.IdFactura.HasValue)
            {
                var verFacturaItem = new MenuFlyoutItem { Text = "Ver factura" };
                verFacturaItem.Click += (_, _) => AbrirFactura(nodo.IdFactura.Value, nodo.Etiqueta);
                menu.Items.Add(verFacturaItem);
            }

            if (nodo.IdOperacion.HasValue)
            {
                var verOperacionItem = new MenuFlyoutItem { Text = "Ver operación" };
                verOperacionItem.Click += (_, _) => OperacionVisorNavigator.Navigate(nodo.IdOperacion.Value);
                menu.Items.Add(verOperacionItem);
            }

            menu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
        }

        private void AbrirFactura(int idFactura, string folio)
        {
            var factura = new FacturaResumenDto { IdFactura = idFactura, Folio = folio };
            var ventana = new DetailFacturaWindow(factura);
            ventana.Activate();
        }

        private async void ModoVisorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isWebView2Initialized || ViewModel.MapsConfig == null)
            {
                return;
            }

            if (ViewModel.Modo == VisorMundialModo.ColaboradoresPorArea)
            {
                await ViewModel.EnsureColaboradoresDataAsync();
            }

            await LoadMapAsync();
        }

        private static string GenerateVisorMundialMapHtml(string apiKey, string centerLat, string centerLng, int zoom, string puntosJson)
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

      const puntos = {puntosJson};
      puntos.forEach(function(p) {{
        const pin = new PinElement({{
          background: p.color,
          borderColor: '#202020',
          glyphColor: '#ffffff',
          glyph: p.glyph || undefined
        }});

        const marker = new AdvancedMarkerElement({{
          map: map,
          position: {{ lat: p.lat, lng: p.lng }},
          title: p.title,
          content: pin.element
        }});

        if (p.idUbicacion !== undefined && p.idUbicacion !== null) {{
          marker.addListener('click', function() {{
            try {{
              window.chrome.webview.postMessage(JSON.stringify({{ type: 'pinClicked', idUbicacion: p.idUbicacion }}));
            }} catch(e) {{}}
          }});
        }} else if (p.idColaborador !== undefined && p.idColaborador !== null) {{
          marker.addListener('click', function() {{
            try {{
              window.chrome.webview.postMessage(JSON.stringify({{ type: 'colaboradorClicked', credencialId: p.idColaborador }}));
            }} catch(e) {{}}
          }});
        }}

        markers.push(marker);
      }});
    }}
  </script>
  <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&callback=initMap' async defer></script>
</body>
</html>";
        }
    }
}
