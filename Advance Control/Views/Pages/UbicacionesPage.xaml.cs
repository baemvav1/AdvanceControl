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
using Advance_Control.Services.Notificacion;
using Advance_Control.Models;
using System.Globalization;
using System.Diagnostics;
using System.Net.Http;
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
        private readonly INotificacionService _notificacionService;
        private readonly Services.Areas.IAreasService _areasService;
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
        private volatile bool _isWebView2Initialized = false;
        private readonly SemaphoreSlim _webView2InitLock = new SemaphoreSlim(1, 1);
        private bool _isDisposed = false;
        private string? _pendingEstadosGeoJson = null;
        // Guarda el parámetro de navegación para procesarlo en Loaded
        private object? _navigationParameter = null;
        // Evita doble inicialización si Loaded se dispara más de una vez
        private bool _pageInitialized = false;

        public UbicacionesPage()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<UbicacionesViewModel>();

            // Resolver el servicio de logging desde DI
            _loggingService = AppServices.Get<ILoggingService>();

            // Resolver el servicio de actividades desde DI
            _activityService = AppServices.Get<IActivityService>();

            // Resolver el servicio de notificaciones
            _notificacionService = AppServices.Get<INotificacionService>();

            // Resolver el servicio de áreas para validación
            _areasService = AppServices.Get<Services.Areas.IAreasService>();

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

                if (MapWebView == null)
                {
                    await _loggingService.LogWarningAsync("MapWebView aún no está disponible en el árbol visual", "UbicacionesPage", "EnsureWebView2InitializedAsync");
                    return;
                }

                await MapWebView.EnsureCoreWebView2Async();

                // Tras el await, la página puede haberse descargado o el runtime de WebView2 puede no estar instalado
                if (_isDisposed)
                {
                    await _loggingService.LogWarningAsync("La página se descargó durante la inicialización de WebView2", "UbicacionesPage", "EnsureWebView2InitializedAsync");
                    return;
                }

                var coreWebView2 = MapWebView.CoreWebView2;
                if (coreWebView2 == null)
                {
                    await _loggingService.LogErrorAsync(
                        "CoreWebView2 es null tras EnsureCoreWebView2Async. Verifica que el runtime de Microsoft Edge WebView2 (Evergreen) esté instalado en este equipo.",
                        new InvalidOperationException("CoreWebView2 no se inicializó correctamente"),
                        "UbicacionesPage",
                        "EnsureWebView2InitializedAsync");
                    return;
                }

                coreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // Habilitar DevTools y menú contextual para diagnóstico (clic derecho → Inspeccionar)
                coreWebView2.Settings.AreDevToolsEnabled = true;
                coreWebView2.Settings.AreDefaultContextMenusEnabled = true;

                // Capturar errores JS y de Google Maps Auth y reenviarlos al log .NET
                await coreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(WebView2DiagnosticsScript.JS);

                // Virtual host para servir el HTML del mapa — da origen https:// a Google Maps
                var mapCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Advance Control", "map_cache");
                Directory.CreateDirectory(mapCacheDir);
                coreWebView2.SetVirtualHostNameToFolderMapping(
                    "ac-maps-local", mapCacheDir,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                
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
            // Evitar doble inicialización (Loaded puede dispararse más de una vez)
            if (_pageInitialized || _isDisposed) return;
            _pageInitialized = true;

            try
            {
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational, "Inicializando WebView2 (Loaded)…");
                await _loggingService.LogInformationAsync("Loaded event: inicializando WebView2", "UbicacionesPage", "UbicacionesView_Loaded");

                // Inicializar WebView2 AQUÍ (control ya está en el árbol visual)
                await EnsureWebView2InitializedAsync();
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational,
                    $"WebView2 listo. CoreWebView2={(MapWebView?.CoreWebView2 != null ? "OK" : "NULL")}");

                // Cargar datos del ViewModel
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational, "Cargando configuración de Google Maps desde la API…");
                await ViewModel.InitializeAsync();

                var hasCfg = ViewModel.MapsConfig != null;
                var keyPreview = hasCfg && !string.IsNullOrEmpty(ViewModel.MapsConfig!.ApiKey)
                    ? ViewModel.MapsConfig.ApiKey.Substring(0, Math.Min(8, ViewModel.MapsConfig.ApiKey.Length)) + "…"
                    : "(vacía)";
                ShowDiag(
                    hasCfg && !string.IsNullOrWhiteSpace(ViewModel.MapsConfig!.ApiKey)
                        ? Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational
                        : Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                    $"MapsConfig: {(hasCfg ? "recibida" : "NULL")}. ApiKey={keyPreview}. Centro={ViewModel.MapsConfig?.DefaultCenter ?? "?"}");

                // Cargar el mapa
                await LoadMapAsync();

                // Procesar parámetro de navegación
                if (_navigationParameter is int idUbicacion)
                {
                    await _loggingService.LogInformationAsync($"Parámetro de nav: IdUbicacion={idUbicacion}", "UbicacionesPage", "UbicacionesView_Loaded");
                    await SelectAndCenterUbicacionAsync(idUbicacion);
                }
            }
            catch (Exception ex)
            {
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, $"Error en Loaded: {ex.GetType().Name}: {ex.Message}");
                await _loggingService.LogErrorAsync("Error en Loaded event", ex, "UbicacionesPage", "UbicacionesView_Loaded");
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

                    // Diagnóstico: errores JS / fallos de autenticación de Google Maps inyectados por WebView2DiagnosticsScript
                    if (messageType == "jsError" || messageType == "jsConsoleError" ||
                        messageType == "jsUnhandledRejection" || messageType == "gmAuthFailure")
                    {
                        var jsMsg = jsonDoc.TryGetValue("message", out var mEl) ? mEl.GetString() : "";
                        var jsStack = jsonDoc.TryGetValue("stack", out var sEl) ? sEl.GetString() : "";
                        var jsSource = jsonDoc.TryGetValue("source", out var srcEl) ? srcEl.GetString() : "";
                        var fullMsg = $"[{messageType}] {jsMsg}" +
                                      (string.IsNullOrEmpty(jsSource) ? "" : $" @ {jsSource}") +
                                      (string.IsNullOrEmpty(jsStack) ? "" : $"\nStack: {jsStack}");
                        ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, $"[{messageType}] {jsMsg}");
                        await _loggingService.LogErrorAsync(fullMsg, new System.InvalidOperationException(messageType ?? "jsError"), "UbicacionesPage", "CoreWebView2_WebMessageReceived");
                        return;
                    }

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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Guardar parámetro; toda la init se hace en Loaded donde el WebView2 ya está en el árbol visual
            _navigationParameter = e.Parameter;
            _pageInitialized = false; // permitir re-inicialización si se navega de vuelta
        }

        /// <summary>
        /// Muestra un mensaje de diagnóstico visible en la página, sin depender del logger ni del notificador.
        /// </summary>
        private void ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, string message)
        {
            try
            {
                if (DiagInfoBar == null) return;
                void Apply()
                {
                    DiagInfoBar.Severity = severity;
                    DiagInfoBar.Message = message;
                    DiagInfoBar.IsOpen = true;
                }
                if (this.DispatcherQueue.HasThreadAccess) Apply();
                else this.DispatcherQueue.TryEnqueue(Apply);
            }
            catch { /* nunca debe romper el flujo del mapa */ }
        }

        /// <summary>
        /// Carga el mapa de Google Maps en el WebView2
        /// </summary>
        private async System.Threading.Tasks.Task LoadMapAsync()
        {
            try
            {
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational, "LoadMapAsync: entrando…");

                // WebView2 ya fue inicializado en Loaded — solo verificar estado, no reinicializar
                if (_isDisposed || !_isWebView2Initialized || MapWebView?.CoreWebView2 == null)
                {
                    ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                        $"WebView2 no listo: disposed={_isDisposed}, initialized={_isWebView2Initialized}, CoreWebView2={MapWebView?.CoreWebView2 != null}");
                    await _loggingService.LogWarningAsync("No se puede cargar el mapa: CoreWebView2 no está inicializado.", "UbicacionesPage", "LoadMapAsync");
                    return;
                }

                if (ViewModel.MapsConfig == null)
                {
                    ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, "No se pudo cargar la configuración del mapa. Verifica que /api/GoogleMapsConfig responda.");
                    await _loggingService.LogWarningAsync("No hay configuración de Google Maps disponible (endpoint /api/GoogleMapsConfig devolvió null o 404)", "UbicacionesPage", "LoadMapAsync");
                    MapWebView.NavigateToString(WebView2DiagnosticHtml.Build(
                        "No se pudo cargar la configuración del mapa",
                        "El endpoint <code>/api/GoogleMapsConfig</code> no devolvió configuración. Verifica que la API esté en línea y que <code>GoogleMaps:ApiKey</code> esté definida en <code>appsettings.json</code> de la API."));
                    return;
                }

                if (string.IsNullOrWhiteSpace(ViewModel.MapsConfig.ApiKey))
                {
                    ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, "ApiKey de Google Maps vacía en la configuración recibida del API.");
                    await _loggingService.LogWarningAsync("ApiKey de Google Maps vacía", "UbicacionesPage", "LoadMapAsync");
                    MapWebView.NavigateToString(WebView2DiagnosticHtml.Build(
                        "Clave de Google Maps no configurada",
                        "La API devolvió una configuración pero <code>ApiKey</code> está vacía. Define <code>GoogleMaps:ApiKey</code> en <code>appsettings.json</code> de la API y reinicia el servicio."));
                    return;
                }

                await _loggingService.LogInformationAsync("Cargando mapa de Google Maps", "UbicacionesPage", "LoadMapAsync");

                // ── DIAGNÓSTICO DE RED ────────────────────────────────────────────────────
                var diagLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Advance Control", "maps_diag.txt");
                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var testUrl = "https://maps.googleapis.com/maps/api/geocode/json?latlng=19.4326,-99.1332&key=" + ViewModel.MapsConfig.ApiKey;
                    var response = await httpClient.GetAsync(testUrl);
                    var body = await response.Content.ReadAsStringAsync();
                    var statusCode = (int)response.StatusCode;
                    var resumen = "HTTP " + statusCode + "\r\n" + body[..Math.Min(500, body.Length)];
                    await File.WriteAllTextAsync(diagLog, resumen);
                }
                catch (Exception diagEx)
                {
                    var errorMsg = "EXCEPCION: " + diagEx.GetType().Name + "\r\n" + diagEx.Message;
                    try { await File.WriteAllTextAsync(diagLog, errorMsg); } catch { }
                }
                // ── FIN DIAGNÓSTICO ───────────────────────────────────────────────────────

                // Parsear el centro del mapa
                var centerParts = (ViewModel.MapsConfig.DefaultCenter ?? string.Empty).Split(',');
                var lat = centerParts.Length > 0 ? centerParts[0].Trim() : DEFAULT_LATITUDE;
                var lng = centerParts.Length > 1 ? centerParts[1].Trim() : DEFAULT_LONGITUDE;

                var ubicacionesJson = JsonSerializer.Serialize(ViewModel.Ubicaciones);

                var html = GenerateMapHtml(
                    ViewModel.MapsConfig.ApiKey,
                    lat, lng,
                    ViewModel.MapsConfig.DefaultZoom,
                    ubicacionesJson);

                // Escribir el HTML a disco y navegar con origen https:// real.
                // NavigateToString crea origen null que Google Maps rechaza.
                var mapCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Advance Control", "map_cache");
                Directory.CreateDirectory(mapCacheDir);
                var mapFile = Path.Combine(mapCacheDir, "ubicaciones.html");
                await System.IO.File.WriteAllTextAsync(mapFile, html, System.Text.Encoding.UTF8);

                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational,
                    $"Cargando mapa ({html.Length:N0} chars)…");

                MapWebView.CoreWebView2.Navigate("https://ac-maps-local/ubicaciones.html");

                await _loggingService.LogInformationAsync("Mapa cargado exitosamente", "UbicacionesPage", "LoadMapAsync");
            }
            catch (Exception ex)
            {
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                    $"Excepción en LoadMapAsync: {ex.GetType().Name}: {ex.Message}");
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

        private string GenerateMapHtml(string apiKey, string lat, string lng, int zoom, string ubicacionesJson)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body, html {{ margin:0; padding:0; height:100%; width:100%; overflow:hidden; }}
        #map {{ height:100%; width:100%; }}
        #status {{ position:fixed; top:8px; left:50%; transform:translateX(-50%);
                   background:rgba(0,0,0,.75); color:#fff; padding:6px 14px;
                   border-radius:4px; font:13px sans-serif; z-index:9999;
                   max-width:90%; text-align:center; }}
        #status.ok  {{ background:rgba(0,120,0,.8); }}
        #status.err {{ background:rgba(180,0,0,.85); }}
        @keyframes marker-drop {{
            0%   {{ transform:translateY(-180px); opacity:0; }}
            60%  {{ transform:translateY(8px);   opacity:1; }}
            80%  {{ transform:translateY(-4px); }}
            100% {{ transform:translateY(0); }}
        }}
        @keyframes marker-bounce {{
            0%, 100% {{ transform:translateY(0); }}
            25%       {{ transform:translateY(-18px); }}
            75%       {{ transform:translateY(-8px); }}
        }}
        .marker-drop   {{ animation:marker-drop   0.45s ease-out; }}
        .marker-bounce {{ animation:marker-bounce 0.7s  ease-in-out infinite; }}
    </style>
</head>
<body>
    <div id='map'></div>
    <div id='status'>Cargando Google Maps…</div>
    <script>
        // ─── diagnóstico ─────────────────────────────────────────────────────────
        var _gmLoaded = false;
        var _statusEl = document.getElementById('status');

        function setStatus(msg, isErr) {{
            if (_statusEl) {{
                _statusEl.textContent = msg;
                _statusEl.className = isErr ? 'err' : 'ok';
                setTimeout(() => {{ if (_statusEl) _statusEl.style.display='none'; }}, isErr ? 8000 : 3000);
            }}
        }}

        function postToHost(obj) {{
            try {{ if (window.chrome?.webview) window.chrome.webview.postMessage(JSON.stringify(obj)); }} catch(e) {{}}
        }}

        // Hook oficial de auth failure de Google Maps
        window.gm_authFailure = function() {{
            var msg = 'gm_authFailure: API key inválida, billing deshabilitado, o restricción de HTTP Referrer. Verifica Google Cloud Console: (1) Maps JS API habilitada, (2) Billing activo, (3) Restricciones de key incluyen https://ac-maps-local/*.';
            setStatus(msg, true);
            postToHost({{ type:'gmAuthFailure', message: msg }});
        }};

        // Timeout de 12s: si initMap no se llamó, reportar
        setTimeout(function() {{
            if (!_gmLoaded) {{
                var gmExists = typeof window.google !== 'undefined' && typeof window.google.maps !== 'undefined';
                var msg = gmExists
                    ? 'Timeout 12s: google.maps existe pero initMap nunca se llamó. Posible error de Map ID o AdvancedMarkerElement.'
                    : 'Timeout 12s: google.maps NO existe. El script de Google Maps no se descargó. Verifica: internet, API key válida, billing activo.';
                setStatus(msg, true);
                postToHost({{ type:'jsError', message: msg }});
            }}
        }}, 12000);

        // ─── estado global ───────────────────────────────────────────────────────
        const PIN = {{
            search:   {{ background:'#4285F4', borderColor:'#1a73e8', glyphColor:'#fff' }},
            edit:     {{ background:'#EA4335', borderColor:'#d93025', glyphColor:'#fff' }},
            selected: {{ background:'#34A853', borderColor:'#0d652d', glyphColor:'#fff' }},
            default:  {{ background:'#FF6B6B', borderColor:'#CC5555', glyphColor:'#fff' }}
        }};
        const ubicaciones = {ubicacionesJson};

        let map, infoWindow, geocoder;
        let markers = [];
        let editMarker = null, searchMarker = null;
        let selectedMarker = null, selectedMarkerTimer = null;
        let isFormVisible = false;

        // ─── helpers ─────────────────────────────────────────────────────────────
        function esc(t) {{
            if (!t) return '';
            const d = document.createElement('div');
            d.textContent = String(t);
            return d.innerHTML;
        }}

        function makePin(key) {{
            const p = new google.maps.marker.PinElement(PIN[key] || PIN.default);
            return p.element;
        }}

        function dropIn(marker) {{
            if (!marker?.content) return;
            marker.content.classList.add('marker-drop');
            setTimeout(() => marker.content?.classList.remove('marker-drop'), 500);
        }}

        function clearMarker(m) {{ if (m) m.map = null; }}

        // ─── initMap ─────────────────────────────────────────────────────────────
        function initMap() {{
            try {{
                _gmLoaded = true;
                setStatus('Google Maps cargado OK', false);
                postToHost({{ type:'debug', message:'initMap ejecutado OK' }});

                map = new google.maps.Map(document.getElementById('map'), {{
                    center: {{ lat: {lat}, lng: {lng} }},
                    zoom: {zoom},
                    mapId: '3457a32dcb6331583ad98107'
                }});
                infoWindow = new google.maps.InfoWindow();
                geocoder   = new google.maps.Geocoder();
                renderAll();

                map.addListener('click', evt => {{
                    if (!isFormVisible) return;
                    if (evt.placeId) {{ evt.stop(); placeFromPoiId(evt.placeId, evt.latLng); }}
                    else             {{ placeEditPin(evt.latLng); }}
                }});

                postToHost({{ type:'debug', message:'initMap completo, markers=' + ubicaciones.length }});
            }} catch(err) {{
                var msg = 'Error en initMap: ' + err.message;
                setStatus(msg, true);
                postToHost({{ type:'jsError', message: msg, stack: err.stack || '' }});
            }}
        }}

        // ─── renderizado ─────────────────────────────────────────────────────────
        function renderAll() {{
            markers.forEach(clearMarker);
            markers = [];
            ubicaciones.forEach(u => {{
                if (u.Latitud == null || u.Longitud == null) return;
                const marker = new google.maps.marker.AdvancedMarkerElement({{
                    position: {{ lat: Number(u.Latitud), lng: Number(u.Longitud) }},
                    map,
                    title: u.Nombre ?? '',
                    content: u.Icono
                        ? (() => {{ const img = document.createElement('img'); img.src=u.Icono; img.style.cssText='width:40px;height:40px'; return img; }})()
                        : makePin('default')
                }});
                marker.addEventListener('gmp-click', () => showInfo(u, marker.position));
                markers.push(marker);
            }});
        }}

        function showInfo(u, pos) {{
            geocoder.geocode({{ location: pos }}, (res, st) => {{
                const dir = st === 'OK' && res?.[0] ? esc(res[0].formatted_address) : esc(u.DireccionCompleta);
                infoWindow.setContent(`<div style='padding:8px;min-width:240px'>
                    <h3 style='margin:0 0 6px;color:#1a73e8'>${{esc(u.Nombre)}}</h3>
                    ${{u.Descripcion ? `<p style='margin:3px 0;color:#5f6368'>${{esc(u.Descripcion)}}</p>` : ''}}
                    ${{dir ? `<p style='margin:3px 0;font-size:13px'>${{dir}}</p>` : ''}}
                    <p style='margin:3px 0;font-size:11px;color:#888'>Lat ${{u.Latitud}}, Lng ${{u.Longitud}}</p>
                </div>`);
                infoWindow.setPosition(pos);
                infoWindow.open(map);
            }});
        }}

        // ─── pin de edición ───────────────────────────────────────────────────────
        function placeEditPin(latLng) {{
            clearMarker(editMarker);
            editMarker = new google.maps.marker.AdvancedMarkerElement({{
                position: latLng, map, gmpDraggable: true,
                title: 'Nueva ubicación', content: makePin('edit')
            }});
            dropIn(editMarker);
            geocodeAndNotify(latLng, 'markerMoved', null);
            editMarker.addEventListener('gmp-dragend', () => {{
                const p = editMarker.position;
                geocodeAndNotify(new google.maps.LatLng(p.lat, p.lng), 'markerMoved', null);
            }});
        }}

        function placeFromPoiId(placeId, fallbackLatLng) {{
            const svc = new google.maps.places.PlacesService(map);
            svc.getDetails({{ placeId, fields:['name','geometry','formatted_address'] }}, (place, st) => {{
                if (st === google.maps.places.PlacesServiceStatus.OK && place) placeFromResult(place);
                else placeEditPin(fallbackLatLng);
            }});
        }}

        function placeFromResult(place) {{
            if (!place.geometry?.location) return;
            const loc = place.geometry.location;
            clearMarker(editMarker);
            editMarker = new google.maps.marker.AdvancedMarkerElement({{
                position: loc, map, gmpDraggable: true,
                title: place.name ?? '', content: makePin('edit')
            }});
            dropIn(editMarker);
            geocodeAndNotify(loc, 'placeSelected', place.name ?? '');
            editMarker.addEventListener('gmp-dragend', () => {{
                const p = editMarker.position;
                geocodeAndNotify(new google.maps.LatLng(p.lat, p.lng), 'markerMoved', null);
            }});
        }}

        function geocodeAndNotify(latLng, type, placeName) {{
            const lat = typeof latLng.lat === 'function' ? latLng.lat() : latLng.lat;
            const lng = typeof latLng.lng === 'function' ? latLng.lng() : latLng.lng;
            geocoder.geocode({{ location: latLng }}, (res, st) => {{
                const addr = {{}};
                if (st === 'OK' && res?.[0]) {{
                    addr.formatted = res[0].formatted_address;
                    addr.place_id  = res[0].place_id;
                    res[0].address_components.forEach(c => {{
                        if (c.types.includes('locality'))                    addr.city    = c.long_name;
                        if (c.types.includes('administrative_area_level_1')) addr.state   = c.long_name;
                        if (c.types.includes('country'))                     addr.country = c.long_name;
                    }});
                }}
                const msg = {{ type, lat, lng, address: addr }};
                if (placeName != null) msg.placeName = placeName;
                postToHost(msg);
            }});
        }}

        // ─── funciones llamadas desde C# ─────────────────────────────────────────
        function setFormVisibility(visible) {{
            isFormVisible = visible;
            if (!visible) {{ clearMarker(editMarker); editMarker = null; }}
        }}

        function loadExistingMarker(lat, lng) {{
            const loc = new google.maps.LatLng(lat, lng);
            placeEditPin(loc);
            map.setCenter(loc);
            map.setZoom(15);
        }}

        function showSelectedLocationMarker(ubicacion, position) {{
            if (selectedMarkerTimer) {{ clearTimeout(selectedMarkerTimer); selectedMarkerTimer = null; }}
            clearMarker(selectedMarker);
            selectedMarker = new google.maps.marker.AdvancedMarkerElement({{
                position, map, title: ubicacion.Nombre ?? '',
                content: makePin('selected'), zIndex: 9999
            }});
            if (selectedMarker.content) {{
                selectedMarker.content.classList.add('marker-bounce');
                selectedMarkerTimer = setTimeout(() => {{
                    selectedMarker?.content?.classList.remove('marker-bounce');
                }}, 2000);
            }}
            showInfo(ubicacion, position);
        }}

        function searchLocation(query) {{
            if (!query?.trim()) return;
            const svc = new google.maps.places.PlacesService(map);
            svc.findPlaceFromQuery(
                {{ query, fields: ['name','geometry','formatted_address'] }},
                (results, status) => {{
                    if (status !== google.maps.places.PlacesServiceStatus.OK || !results?.length) return;
                    const place = results[0];
                    if (!place.geometry?.location) return;
                    clearMarker(searchMarker);
                    map.setCenter(place.geometry.location);
                    map.setZoom(15);
                    searchMarker = new google.maps.marker.AdvancedMarkerElement({{
                        position: place.geometry.location, map,
                        title: place.name, content: makePin('search')
                    }});
                    dropIn(searchMarker);
                    infoWindow.setContent(`<div style='padding:8px'><b>${{esc(place.name)}}</b><br><span style='font-size:13px;color:#5f6368'>${{esc(place.formatted_address)}}</span></div>`);
                    infoWindow.open({{ anchor: searchMarker, map }});
                    if (isFormVisible) placeFromResult(place);
                }}
            );
        }}
    </script>
    <script
        src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=places,marker&callback=initMap'
        defer
        onerror='setStatus(""Error: no se pudo descargar el script de Google Maps (maps.googleapis.com). Verifica internet y que la API key sea válida."", true); postToHost({{type:""jsError"",message:""Script de Google Maps no descargado""}});'>
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

                // Si tenemos GeoJSON pendiente que era demasiado grande para embeber en NavigateToString,
                // lo inyectamos ahora vía ExecuteScriptAsync (sin límite práctico de tamaño).
                if (!string.IsNullOrEmpty(_pendingEstadosGeoJson) && sender?.CoreWebView2 != null)
                {
                    try
                    {
                        // _pendingEstadosGeoJson YA es JSON válido, así que lo asignamos directo a la variable global.
                        var script = "window.ESTADOS_DATA = " + _pendingEstadosGeoJson + ";";
                        await sender.CoreWebView2.ExecuteScriptAsync(script);
                        ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success,
                            $"Mapa cargado. GeoJSON de estados inyectado ({_pendingEstadosGeoJson.Length:N0} caracteres).");
                        _pendingEstadosGeoJson = null;
                    }
                    catch (Exception exGeo)
                    {
                        ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                            $"Mapa cargado, pero falló inyectar GeoJSON: {exGeo.Message}");
                    }
                }
                else
                {
                    ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success, "Mapa cargado.");
                }
            }
            else
            {
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                    $"WebView2 navegación falló. Status: {args.WebErrorStatus}");
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
        /// Maneja la tecla Enter en el campo de búsqueda del mapa
        /// </summary>
        private async void MapSearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != global::Windows.System.VirtualKey.Enter) return;
            e.Handled = true;

            try
            {
                var searchQuery = MapSearchBox.Text?.Trim();
                
                if (string.IsNullOrWhiteSpace(searchQuery))
                    return;

                await _loggingService.LogInformationAsync($"Buscando ubicación: {searchQuery}", "UbicacionesPage", "MapSearchBox_KeyDown");

                if (MapWebView?.CoreWebView2 != null)
                {
                    var encodedQuery = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(searchQuery);
                    var script = $"searchLocation('{encodedQuery}');";
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al buscar ubicación", ex, "UbicacionesPage", "MapSearchBox_KeyDown");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al buscar la ubicación");
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
                    await _notificacionService.MostrarAsync("Validación", "El nombre es requerido");
                    return;
                }

                // Validate that coordinates were set from the map
                if (!_currentLatitud.HasValue || !_currentLongitud.HasValue)
                {
                    await _notificacionService.MostrarAsync("Validación", "Por favor, haz clic en el mapa para seleccionar una ubicación");
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

                // Validar que las coordenadas caigan dentro de un área definida
                try
                {
                    var areas = await _areasService.ValidatePointAsync(_currentLatitud.Value, _currentLongitud.Value);
                    if (areas == null || !areas.Any(a => a.DentroDelArea))
                    {
                        await _notificacionService.MostrarAsync("Ubicación sin área",
                            "La ubicación seleccionada no se encuentra dentro de ningún área definida. " +
                            "Primero debe crear un área que cubra esta zona en la página de Áreas.");
                        return;
                    }
                }
                catch (Exception areaEx)
                {
                    await _loggingService.LogWarningAsync(
                        $"No se pudo validar área para coordenadas ({_currentLatitud}, {_currentLongitud}): {areaEx.Message}",
                        "UbicacionesPage", "SaveButton_Click");
                    await _notificacionService.MostrarAsync("Advertencia",
                        "No se pudo verificar si la ubicación pertenece a un área. Verifique su conexión e intente de nuevo.");
                    return;
                }

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

