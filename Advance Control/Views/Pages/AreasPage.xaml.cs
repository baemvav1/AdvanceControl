using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Globalization;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página standalone de gestión de áreas geográficas con Google Maps
    /// </summary>
    public sealed partial class AreasPage : Page
    {
        // Default coordinates for Mexico City if configuration is not available
        private const string DEFAULT_LATITUDE = "22.1497";
        private const string DEFAULT_LONGITUDE = "-100.975";
        private const int DEFAULT_ZOOM = 12;
        
        // Fallback radius in meters for areas without geometry data
        private const int FALLBACK_CIRCLE_RADIUS_METERS = 100;
        
        public AreasViewModel ViewModel { get; }
        private readonly ILoggingService _loggingService;
        private readonly INotificacionService _notificacionService;
        private readonly IActivityService _activityService;
        private bool _isEditMode = false;
        private int? _editingAreaId = null;
        private bool _isFormVisible = false;
        private string? _pendingGeoNombre = null;   // nombre del estado/municipio para pre-llenar el formulario
        private string? _pendingGeoType = null;    // "Estado" o "Municipio"
        private string? _pendingEstadoNombre = null; // para municipios: nombre del estado padre
        
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
        
        // Flag to prevent multiple simultaneous map centering operations (0 = not centering, 1 = centering)
        private int _isCenteringMapInt = 0;
        
        public AreasPage()
        {
            ViewModel = AppServices.Get<AreasViewModel>();
            _loggingService = AppServices.Get<ILoggingService>();
            _notificacionService = AppServices.Get<INotificacionService>();
            _activityService = AppServices.Get<IActivityService>();

            this.InitializeComponent();
            ButtonClickLogger.Attach(this, _loggingService, nameof(AreasPage));

            this.DataContext = ViewModel;

            this.Loaded += AreasView_Loaded;
            this.Unloaded += AreasView_Unloaded;
        }
        
        /// <summary>
        /// Truncates a string for logging to avoid log bloat
        /// </summary>
        private static string TruncateForLog(string? value, int maxLength = 150)
        {
            if (string.IsNullOrEmpty(value))
                return "(null or empty)";
            
            if (value.Length <= maxLength)
                return value;
            
            return $"{value[..maxLength]}... (total: {value.Length} chars)";
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
                await _loggingService.LogWarningAsync("Cannot initialize WebView2 - page is disposed", "AreasPage", "EnsureWebView2InitializedAsync");
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

                await _loggingService.LogInformationAsync("Initializing CoreWebView2 and message handler", "AreasPage", "EnsureWebView2InitializedAsync");

                if (MapWebView == null)
                {
                    await _loggingService.LogWarningAsync("MapWebView aún no está disponible en el árbol visual", "AreasPage", "EnsureWebView2InitializedAsync");
                    return;
                }

                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
                await MapWebView.EnsureCoreWebView2Async(env);

                if (_isDisposed)
                {
                    await _loggingService.LogWarningAsync("La página se descargó durante la inicialización de WebView2", "AreasPage", "EnsureWebView2InitializedAsync");
                    return;
                }

                var coreWebView2 = MapWebView.CoreWebView2;
                if (coreWebView2 == null)
                {
                    await _loggingService.LogErrorAsync(
                        "CoreWebView2 es null tras EnsureCoreWebView2Async. Verifica que el runtime de Microsoft Edge WebView2 (Evergreen) esté instalado en este equipo.",
                        new InvalidOperationException("CoreWebView2 no se inicializó correctamente"),
                        "AreasPage",
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

                // Virtual host para servir GeoJSON localmente sin peticiones de red
                var geoFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "geo");
                if (Directory.Exists(geoFolder))
                    coreWebView2.SetVirtualHostNameToFolderMapping(
                        "geo-assets", geoFolder,
                        Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                _isWebView2Initialized = true;

                await _loggingService.LogInformationAsync("CoreWebView2 and message handler initialized successfully", "AreasPage", "EnsureWebView2InitializedAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al inicializar WebView2 message handler", ex, "AreasPage", "EnsureWebView2InitializedAsync");
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
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al configurar WebView2 en Loaded event", ex, "AreasPage", "AreasView_Loaded");
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational, "Inicializando WebView2…");
                await _loggingService.LogInformationAsync("Navegando a página de Áreas", "AreasPage", "OnNavigatedTo");

                await EnsureWebView2InitializedAsync();
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational, $"WebView2 listo. CoreWebView2 = {(MapWebView?.CoreWebView2 != null ? "OK" : "NULL")}");

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
                    $"MapsConfig: {(hasCfg ? "recibida" : "NULL (API no respondió o devolvió 404)")}. ApiKey={keyPreview}. Centro={ViewModel.MapsConfig?.DefaultCenter ?? "?"}");

                await LoadMapAsync();
            }
            catch (Exception ex)
            {
                ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, $"Excepción en OnNavigatedTo: {ex.GetType().Name}: {ex.Message}");
                await _loggingService.LogErrorAsync("Error al navegar a Áreas", ex, "AreasPage", "OnNavigatedTo");
            }
        }

        private void OpenFormForGeoSelection(string nombre, string geoType, string? estadoNombre = null)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _isEditMode = false;
                _editingAreaId = null;
                FormTitle.Text = "Nueva Área";
                NombreTextBox.Text = nombre;
                DescripcionTextBox.Text = geoType == "Municipio" && estadoNombre != null
                    ? $"Municipio del estado de {estadoNombre}"
                    : string.Empty;
                ColorComboBox.SelectedIndex = 0;
                ActivoCheckBox.IsChecked = true;
                AreaForm.Visibility = Visibility.Visible;
                _isFormVisible = true;
            });
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

        private async void CoreWebView2_WebMessageReceived(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                // Soportar mensajes posteados como string (JSON.stringify) o como objeto
                var rawString = args.TryGetWebMessageAsString();
                var message = !string.IsNullOrEmpty(rawString) ? rawString : args.WebMessageAsJson.ToString();
                await _loggingService.LogInformationAsync($"Mensaje recibido de WebView2: {message}", "AreasPage", "CoreWebView2_WebMessageReceived");

                var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                
                if (jsonDoc != null && jsonDoc.TryGetValue("type", out var typeElement))
                {
                    var messageType = typeElement.GetString();

                    if (messageType == "debug")
                    {
                        var debugMsg = jsonDoc.TryGetValue("message", out var dbgEl) ? dbgEl.GetString() : "unknown";
                        await _loggingService.LogInformationAsync($"[JS DEBUG] {debugMsg}", "AreasPage", "CoreWebView2_WebMessageReceived");
                    }
                    else if (messageType == "jsError" || messageType == "jsConsoleError" ||
                             messageType == "jsUnhandledRejection" || messageType == "gmAuthFailure")
                    {
                        var errorMsg = jsonDoc.TryGetValue("message", out var errEl) ? errEl.GetString() : "unknown";
                        var errorStack = jsonDoc.TryGetValue("stack", out var stackEl) ? stackEl.GetString() : "";
                        var errorSource = jsonDoc.TryGetValue("source", out var srcEl) ? srcEl.GetString() : "";
                        var fullMsg = $"[{messageType}] {errorMsg}" +
                                      (string.IsNullOrEmpty(errorSource) ? "" : $" @ {errorSource}") +
                                      (string.IsNullOrEmpty(errorStack) ? "" : $"\nStack: {errorStack}");
                        ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, $"[{messageType}] {errorMsg}");
                        await _loggingService.LogErrorAsync(fullMsg, new System.InvalidOperationException(messageType ?? "jsError"), "AreasPage", "CoreWebView2_WebMessageReceived");
                    }
                    else if (messageType == "shapeDrawn" || messageType == "shapeEdited")
                    {
                        if (jsonDoc.TryGetValue("shapeType", out var shapeTypeElement))
                        {
                            _currentShapeType = shapeTypeElement.GetString();
                        }

                        if (jsonDoc.TryGetValue("path", out var pathElement))
                        {
                            _currentShapePath = pathElement.GetRawText();
                            await _loggingService.LogInformationAsync(
                                $"[DATA_FLOW] Step 1 - Path received: {TruncateForLog(_currentShapePath)}",
                                "AreasPage",
                                "CoreWebView2_WebMessageReceived");
                        }
                        else
                        {
                            await _loggingService.LogWarningAsync(
                                "[DATA_FLOW] Step 1 - Path not found in shape message",
                                "AreasPage",
                                "CoreWebView2_WebMessageReceived");
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

                        // Pre-llenar nombre cuando viene de "Crear desde estado/municipio"
                        if (jsonDoc.TryGetValue("estadoNombre", out var estadoNombreElement))
                        {
                            var geoNombre = estadoNombreElement.GetString();
                            if (!string.IsNullOrWhiteSpace(geoNombre))
                            {
                                _pendingGeoNombre = geoNombre;
                                // Abrir el formulario automáticamente, igual que si el usuario hiciera click en "Agregar"
                                _isEditMode = false;
                                _editingAreaId = null;
                                FormTitle.Text = "Nueva Área";
                                NombreTextBox.Text = geoNombre;
                                DescripcionTextBox.Text = string.Empty;
                                ColorComboBox.SelectedIndex = 0;
                                ActivoCheckBox.IsChecked = true;
                                AreaForm.Visibility = Visibility.Visible;
                                _isFormVisible = true;
                            }
                        }

                        await _loggingService.LogInformationAsync(
                            $"Shape {messageType}: Type={_currentShapeType}, " +
                            $"Path={(string.IsNullOrEmpty(_currentShapePath) ? "EMPTY" : "SET")}, " +
                            $"Center={(string.IsNullOrEmpty(_currentShapeCenter) ? "EMPTY" : "SET")}, " +
                            $"Radius={(_currentShapeRadius.HasValue ? _currentShapeRadius.Value.ToString() : "NULL")}, " +
                            $"Bounds={(string.IsNullOrEmpty(_currentShapeBounds) ? "EMPTY" : "SET")}",
                            "AreasPage",
                            "CoreWebView2_WebMessageReceived");
                    }
                    else if (messageType == "estadoSelected")
                    {
                        var nombre = jsonDoc.TryGetValue("nombre", out var nEl) ? nEl.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(nombre))
                        {
                            _pendingGeoNombre = nombre;
                            _pendingGeoType = "Estado";
                            _pendingEstadoNombre = nombre;
                            _currentShapeType = "Estado";
                            OpenFormForGeoSelection(nombre, "Estado");
                        }
                    }
                    else if (messageType == "municipioSelected")
                    {
                        var nombre = jsonDoc.TryGetValue("nombre", out var nEl) ? nEl.GetString() : null;
                        var estado = jsonDoc.TryGetValue("estado", out var eEl) ? eEl.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(nombre))
                        {
                            _pendingGeoNombre = nombre;
                            _pendingGeoType = "Municipio";
                            _pendingEstadoNombre = estado;
                            _currentShapeType = "Municipio";
                            OpenFormForGeoSelection(nombre, "Municipio", estado);
                        }
                    }
                    else if (messageType == "geoClickError")
                    {
                        if (jsonDoc.TryGetValue("message", out var errMsgElement))
                        {
                            await _loggingService.LogErrorAsync(
                                $"Error JS en click de capa geo: {errMsgElement.GetString()}",
                                null!, "AreasPage", "CoreWebView2_WebMessageReceived");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al procesar mensaje de forma", ex, "AreasPage", "CoreWebView2_WebMessageReceived");
            }
        }

        private async Task LoadMapAsync()
        {
            try
            {
                // CoreWebView2 listo siempre, incluso si vamos a mostrar mensaje de diagnóstico
                await EnsureWebView2InitializedAsync();
                if (_isDisposed || MapWebView?.CoreWebView2 == null)
                {
                    await _loggingService.LogWarningAsync("No se puede cargar el mapa: CoreWebView2 no está inicializado. Verifica que el runtime de Microsoft Edge WebView2 esté instalado.", "AreasPage", "LoadMapAsync");
                    return;
                }

                if (ViewModel.MapsConfig == null)
                {
                    ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, "No se pudo cargar la configuración del mapa. Verifica que /api/GoogleMapsConfig responda.");
                    await _loggingService.LogWarningAsync("No hay configuración de Google Maps disponible (endpoint /api/GoogleMapsConfig devolvió null o 404)", "AreasPage", "LoadMapAsync");
                    MapWebView.NavigateToString(WebView2DiagnosticHtml.Build(
                        "No se pudo cargar la configuración del mapa",
                        "El endpoint <code>/api/GoogleMapsConfig</code> no devolvió configuración. Verifica que la API esté en línea y que <code>GoogleMaps:ApiKey</code> esté definida en <code>appsettings.json</code> de la API."));
                    return;
                }

                if (string.IsNullOrWhiteSpace(ViewModel.MapsConfig.ApiKey))
                {
                    ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error, "ApiKey de Google Maps vacía en la configuración recibida del API.");
                    await _loggingService.LogWarningAsync("ApiKey de Google Maps vacía", "AreasPage", "LoadMapAsync");
                    MapWebView.NavigateToString(WebView2DiagnosticHtml.Build(
                        "Clave de Google Maps no configurada",
                        "La API devolvió configuración pero <code>ApiKey</code> está vacía. Define <code>GoogleMaps:ApiKey</code> en <code>appsettings.json</code> de la API y reinicia el servicio."));
                    return;
                }

                await _loggingService.LogInformationAsync($"Cargando mapa. ApiKey={ViewModel.MapsConfig.ApiKey?.Substring(0, Math.Min(10, ViewModel.MapsConfig.ApiKey?.Length ?? 0))}..., Center={ViewModel.MapsConfig.DefaultCenter}, Zoom={ViewModel.MapsConfig.DefaultZoom}", "AreasPage", "LoadMapAsync");
                await _loggingService.LogInformationAsync("CoreWebView2 listo", "AreasPage", "LoadMapAsync");

                // Parsear el centro del mapa
                var centerParts = ViewModel.MapsConfig.DefaultCenter?.Split(',') ?? Array.Empty<string>();
                var centerLat = centerParts.Length > 0 ? centerParts[0].Trim() : DEFAULT_LATITUDE;
                var centerLng = centerParts.Length > 1 ? centerParts[1].Trim() : DEFAULT_LONGITUDE;
                var zoom = ViewModel.MapsConfig.DefaultZoom;

                var areasJson = PrepareAreasJson();
                await _loggingService.LogInformationAsync($"AreasJSON generado: {areasJson.Length} chars, Areas count: {ViewModel.Areas.Count}", "AreasPage", "LoadMapAsync");

                // GeoJSON de estados se carga bajo demanda via fetch desde virtual host geo-assets
                // para evitar exceder el límite de ~2MB de NavigateToString

                var html = GenerateAreasMapHtml(ViewModel.MapsConfig.ApiKey, centerLat, centerLng, zoom, areasJson);
                await _loggingService.LogInformationAsync($"HTML generado: {html.Length} chars ({html.Length / 1024} KB)", "AreasPage", "LoadMapAsync");

                // Escribir a disco y navegar con origen https:// real.
                // NavigateToString crea origen null que Google Maps rechaza.
                var mapCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Advance Control", "map_cache");
                Directory.CreateDirectory(mapCacheDir);
                var mapFile = Path.Combine(mapCacheDir, "areas.html");
                await System.IO.File.WriteAllTextAsync(mapFile, html, System.Text.Encoding.UTF8);
                MapWebView.CoreWebView2.Navigate("https://ac-maps-local/areas.html");
                ViewModel.IsMapInitialized = true;

                await _loggingService.LogInformationAsync("Mapa cargado: https://ac-maps-local/areas.html", "AreasPage", "LoadMapAsync");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el mapa de áreas", ex, "AreasPage", "LoadMapAsync");
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
                        type = string.IsNullOrEmpty(a.TipoGeometria) ? "Polygon" : a.TipoGeometria,
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
                _ = _loggingService.LogErrorAsync("Error al preparar JSON de áreas", ex, "AreasPage", "PrepareAreasJson");
                return "[]";
            }
        }

        private object? ParsePathJson(AreaDto area)
        {
            // First try to get path from MetadataJSON
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
                catch (JsonException)
                {
                    _ = _loggingService.LogWarningAsync(
                        $"Error parsing path JSON for area {area.IdArea}",
                        "AreasPage",
                        "ParsePathJson");
                }
            }

            // Fallback: Create path from bounding box coordinates if available
            if (area.BoundingBoxNE_Lat.HasValue && area.BoundingBoxNE_Lng.HasValue &&
                area.BoundingBoxSW_Lat.HasValue && area.BoundingBoxSW_Lng.HasValue)
            {
                // Create a rectangle path from bounding box (4 corners)
                return new[]
                {
                    new { lat = area.BoundingBoxNE_Lat.Value, lng = area.BoundingBoxSW_Lng.Value }, // NW
                    new { lat = area.BoundingBoxNE_Lat.Value, lng = area.BoundingBoxNE_Lng.Value }, // NE
                    new { lat = area.BoundingBoxSW_Lat.Value, lng = area.BoundingBoxNE_Lng.Value }, // SE
                    new { lat = area.BoundingBoxSW_Lat.Value, lng = area.BoundingBoxSW_Lng.Value }  // SW
                };
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
            return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <style>
    html, body, #map {{ margin: 0; height: 100%; width: 100%; font-family: Roboto, Arial, sans-serif; }}
    #search-container {{
      position: absolute; top: 16px; left: 50%; transform: translateX(-50%);
      z-index: 10; width: 500px; max-width: 60%;
      display: flex; align-items: center;
      background: #fff; border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.18);
      transition: box-shadow 0.2s ease;
    }}
    #search-container:focus-within {{ box-shadow: 0 4px 20px rgba(0,0,0,0.28); }}
    #search-box {{
      flex: 1; padding: 12px 10px 12px 16px; font-size: 15px;
      border: none; border-radius: 8px 0 0 8px; outline: none;
      background: transparent; min-width: 0;
    }}
    #clear-btn {{
      flex-shrink: 0; width: 36px; height: 36px; margin-right: 6px;
      display: none; align-items: center; justify-content: center;
      background: none; border: none; border-radius: 50%;
      cursor: pointer; font-size: 18px; color: #666; line-height: 1;
    }}
    #clear-btn:hover {{ background: #f0f0f0; color: #333; }}
    #clear-btn.visible {{ display: flex; }}
    .pac-container {{ border-radius: 8px; margin-top: 4px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); border-top: none; }}
    #layer-btns {{
      position: absolute; top: 16px; right: 16px; z-index: 10;
      display: flex; flex-direction: column; gap: 8px;
    }}
    #layer-btns button {{
      padding: 9px 18px; background: #fff;
      border: 1px solid #dadce0; border-radius: 4px;
      cursor: pointer; font-size: 14px; font-weight: 500; color: #3c4043;
      box-shadow: 0 2px 6px rgba(0,0,0,0.15);
      transition: background 0.15s, color 0.15s, border-color 0.15s;
      white-space: nowrap; text-align: left;
    }}
    #layer-btns button:hover:not(:disabled) {{ background: #f1f3f4; }}
    #layer-btns button:disabled {{ opacity: 0.45; cursor: default; box-shadow: none; }}
    #layer-btns button.active {{ background: #e8f0fe; color: #1a73e8; border-color: #4285F4; }}
    #loading-overlay {{
      position: absolute; bottom: 24px; left: 50%; transform: translateX(-50%);
      z-index: 10; background: rgba(0,0,0,0.65); color: #fff;
      padding: 8px 18px; border-radius: 20px; font-size: 13px;
      display: none; pointer-events: none;
    }}
  </style>
</head>
<body>
  <div id='search-container'>
    <input id='search-box' type='text' placeholder='Buscar lugar, colonia, coordenadas…' />
    <button id='clear-btn' title='Limpiar búsqueda'>&#x2715;</button>
  </div>
  <div id='layer-btns'>
    <button id='estado-btn' onclick='toggleEstado()'>Estado</button>
    <button id='municipio-btn' onclick='toggleMunicipio()' disabled>Municipio</button>
  </div>
  <div id='loading-overlay'>Cargando…</div>
  <div id='map'></div>
  <script>
    let map, marker, autocomplete, infoWindow;
    let estadoLayerActive = false;
    let municipioLayerActive = false;
    let selectedEstadoNombre = null;
    let estadosGeoJson = null;
    let municipiosGeoJson = null;

    const STYLE_ESTADO = {{
      fillColor: '#4285F4', fillOpacity: 0.07,
      strokeColor: '#4285F4', strokeWeight: 2, strokeOpacity: 0.85
    }};
    const STYLE_ESTADO_SEL = {{
      fillColor: '#1565C0', fillOpacity: 0.22,
      strokeColor: '#1565C0', strokeWeight: 2.5, strokeOpacity: 1
    }};
    const STYLE_MUNICIPIO = {{
      fillColor: '#0F9D58', fillOpacity: 0.07,
      strokeColor: '#0F9D58', strokeWeight: 1.5, strokeOpacity: 0.85
    }};
    const STYLE_MUNICIPIO_SEL = {{
      fillColor: '#1B5E20', fillOpacity: 0.25,
      strokeColor: '#1B5E20', strokeWeight: 2, strokeOpacity: 1
    }};

    async function initMap() {{
      const {{ Map }} = await google.maps.importLibrary(""maps"");
      const {{ AdvancedMarkerElement }} = await google.maps.importLibrary(""marker"");
      map = new Map(document.getElementById('map'), {{
        center: {{lat: {centerLat}, lng: {centerLng}}},
        zoom: {zoom}, mapId: 'DEMO_MAP_ID'
      }});
      infoWindow = new google.maps.InfoWindow();

      const input = document.getElementById('search-box');
      const clearBtn = document.getElementById('clear-btn');
      autocomplete = new google.maps.places.Autocomplete(input,
        {{ fields: ['geometry', 'name', 'formatted_address'] }});
      autocomplete.bindTo('bounds', map);
      autocomplete.addListener('place_changed', function() {{
        const place = autocomplete.getPlace();
        if (!place.geometry || !place.geometry.location) return;
        navigateTo(place.geometry.location.lat(), place.geometry.location.lng(),
          place.name || '', place.formatted_address || '');
      }});
      input.addEventListener('input', function() {{
        clearBtn.classList.toggle('visible', this.value.length > 0);
      }});
      input.addEventListener('keydown', function(e) {{
        if (e.key !== 'Enter') return;
        const text = this.value.trim();
        const coordMatch = text.match(/^(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)$/);
        if (coordMatch) {{
          navigateTo(parseFloat(coordMatch[1]), parseFloat(coordMatch[2]), 'Coordenadas', text);
        }} else {{ google.maps.event.trigger(autocomplete, 'place_changed'); }}
      }});
      clearBtn.addEventListener('click', function() {{
        input.value = ''; clearBtn.classList.remove('visible');
        if (marker) {{ marker.setMap(null); marker = null; }}
        infoWindow.close(); input.focus();
      }});

      map.data.addListener('click', function(event) {{
        try {{
          const feature = event.feature;
          const nombre = feature.getProperty('nombre') || '';
          const estado = feature.getProperty('estado') || nombre;
          if (!municipioLayerActive) {{
            selectedEstadoNombre = nombre;
            applyEstadoStyles();
            updateButtons();
            try {{
              window.chrome.webview.postMessage(JSON.stringify({{
                type: 'estadoSelected', nombre: nombre
              }}));
            }} catch(e) {{}}
          }} else {{
            map.data.revertStyle();
            map.data.overrideStyle(feature, STYLE_MUNICIPIO_SEL);
            try {{
              window.chrome.webview.postMessage(JSON.stringify({{
                type: 'municipioSelected', nombre: nombre, estado: estado
              }}));
            }} catch(e) {{}}
          }}
        }} catch(err) {{
          try {{ window.chrome.webview.postMessage(JSON.stringify({{ type: 'geoClickError', message: err.message }})); }} catch(e) {{}}
        }}
      }});
    }}

    function applyEstadoStyles() {{
      map.data.setStyle(function(feature) {{
        return feature.getProperty('nombre') === selectedEstadoNombre
          ? STYLE_ESTADO_SEL : STYLE_ESTADO;
      }});
    }}

    function setLoading(text) {{
      const el = document.getElementById('loading-overlay');
      if (text) {{ el.textContent = text; el.style.display = 'block'; }}
      else {{ el.style.display = 'none'; }}
    }}

    async function toggleEstado() {{
      if (estadoLayerActive) {{
        map.data.forEach(f => map.data.remove(f));
        estadoLayerActive = false;
        municipioLayerActive = false;
        selectedEstadoNombre = null;
        updateButtons();
        return;
      }}
      setLoading('Cargando estados…');
      try {{
        if (!estadosGeoJson) {{
          const resp = await fetch('https://geo-assets/estados.json');
          estadosGeoJson = await resp.json();
        }}
        map.data.forEach(f => map.data.remove(f));
        municipioLayerActive = false;
        selectedEstadoNombre = null;
        map.data.addGeoJson(estadosGeoJson);
        map.data.setStyle(STYLE_ESTADO);
        estadoLayerActive = true;
        updateButtons();
      }} catch(err) {{
        try {{ window.chrome.webview.postMessage(JSON.stringify({{ type: 'geoClickError', message: 'toggleEstado: ' + err.message }})); }} catch(e) {{}}
      }} finally {{
        setLoading(null);
      }}
    }}

    async function toggleMunicipio() {{
      if (municipioLayerActive) {{
        municipioLayerActive = false;
        map.data.forEach(f => map.data.remove(f));
        map.data.addGeoJson(estadosGeoJson);
        applyEstadoStyles();
        updateButtons();
        return;
      }}
      if (!selectedEstadoNombre) return;
      setLoading(`Cargando municipios de ${{selectedEstadoNombre}}…`);
      try {{
        if (!municipiosGeoJson) {{
          const resp = await fetch('https://geo-assets/municipios.json');
          municipiosGeoJson = await resp.json();
        }}
        const filtered = {{
          type: 'FeatureCollection',
          features: municipiosGeoJson.features.filter(
            f => f.properties && f.properties.estado === selectedEstadoNombre)
        }};
        map.data.forEach(f => map.data.remove(f));
        map.data.addGeoJson(filtered);
        map.data.setStyle(STYLE_MUNICIPIO);
        municipioLayerActive = true;
        updateButtons();
      }} catch(err) {{
        try {{ window.chrome.webview.postMessage(JSON.stringify({{ type: 'geoClickError', message: 'toggleMunicipio: ' + err.message }})); }} catch(e) {{}}
      }} finally {{
        setLoading(null);
      }}
    }}

    function updateButtons() {{
      const eBtn = document.getElementById('estado-btn');
      const mBtn = document.getElementById('municipio-btn');
      eBtn.classList.toggle('active', estadoLayerActive);
      mBtn.disabled = !selectedEstadoNombre && !municipioLayerActive;
      mBtn.classList.toggle('active', municipioLayerActive);
    }}

    function navigateTo(lat, lng, name, address) {{
      const pos = {{ lat: lat, lng: lng }};
      map.panTo(pos); map.setZoom(13);
      if (marker) marker.setMap(null);
      infoWindow.close();
      marker = new google.maps.marker.AdvancedMarkerElement({{
        map: map, position: pos, title: name
      }});
      if (name || address) {{
        const contentString = `<div style='padding: 4px; max-width: 200px;'>` +
          `${{name ? `<strong>${{name}}</strong><br/>` : ''}}` +
          `${{address ? `<span style='font-size:12px;color:#555;'>${{address}}</span>` : ''}}` +
          `</div>`;
        infoWindow.setContent(contentString);
        infoWindow.open({{ map, anchor: marker }});
      }}
    }}

    function fitBoundsToFeature(feature) {{
      try {{
        const bounds = new google.maps.LatLngBounds();
        feature.getGeometry().forEachLatLng(function(latlng) {{ bounds.extend(latlng); }});
        if (!bounds.isEmpty()) map.fitBounds(bounds);
      }} catch(e) {{}}
    }}

    async function highlightEstado(nombre) {{
      if (municipioLayerActive) {{
        municipioLayerActive = false;
        map.data.forEach(f => map.data.remove(f));
        if (estadosGeoJson) map.data.addGeoJson(estadosGeoJson);
        estadoLayerActive = true;
      }} else if (!estadoLayerActive) {{
        await toggleEstado();
      }}
      selectedEstadoNombre = nombre;
      applyEstadoStyles();
      updateButtons();
      map.data.forEach(function(feature) {{
        if (feature.getProperty('nombre') === nombre) fitBoundsToFeature(feature);
      }});
    }}

    async function highlightMunicipio(nombre, estado) {{
      if (!estadoLayerActive || selectedEstadoNombre !== estado || municipioLayerActive) {{
        if (!estadoLayerActive) await toggleEstado();
        selectedEstadoNombre = estado;
        municipioLayerActive = false;
        setLoading(`Cargando municipios de ${{estado}}…`);
        try {{
          if (!municipiosGeoJson) {{
            const resp = await fetch('https://geo-assets/municipios.json');
            municipiosGeoJson = await resp.json();
          }}
          const filtered = {{
            type: 'FeatureCollection',
            features: municipiosGeoJson.features.filter(
              f => f.properties && f.properties.estado === estado)
          }};
          map.data.forEach(f => map.data.remove(f));
          map.data.addGeoJson(filtered);
          map.data.setStyle(STYLE_MUNICIPIO);
          municipioLayerActive = true;
          updateButtons();
        }} finally {{ setLoading(null); }}
      }}
      map.data.revertStyle();
      map.data.forEach(function(feature) {{
        if (feature.getProperty('nombre') === nombre && feature.getProperty('estado') === estado) {{
          map.data.overrideStyle(feature, STYLE_MUNICIPIO_SEL);
          fitBoundsToFeature(feature);
        }}
      }});
    }}
  </script>
  <script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=places&callback=initMap' async defer></script>
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
                await _loggingService.LogErrorAsync("Error al ejecutar script en el mapa", ex, "AreasPage", "ExecuteMapScriptAsync");
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
                await _loggingService.LogErrorAsync("Error al refrescar áreas", ex, "AreasPage", "RefreshButton_Click");
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            await _loggingService.LogInformationAsync(
                $"AddButton_Click - Before clearing form: _currentShapeType={_currentShapeType ?? "NULL"}",
                "AreasPage",
                "AddButton_Click");
            
            _isEditMode = false;
            _editingAreaId = null;
            FormTitle.Text = "Nueva Área";
            
            // Si hay un nombre de geo-click pendiente, usarlo; si no, limpiar
            NombreTextBox.Text = _pendingGeoNombre ?? string.Empty;
            _pendingGeoNombre = null;
            DescripcionTextBox.Text = string.Empty;
            ColorComboBox.SelectedIndex = 0;
            ActivoCheckBox.IsChecked = true;

            await _loggingService.LogInformationAsync(
                $"AddButton_Click - After setting up form: _currentShapeType={_currentShapeType ?? "NULL"}",
                "AreasPage",
                "AddButton_Click");

            AreaForm.Visibility = Visibility.Visible;
            _isFormVisible = true;
        }

        private async void AreasList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prevent multiple simultaneous map centering operations using Interlocked for thread safety
            if (Interlocked.CompareExchange(ref _isCenteringMapInt, 1, 0) != 0)
                return;
            GoogleMapsConfigDto googleMapsConfig = new GoogleMapsConfigDto();
            try
            {
                // When an area is selected from the list, visualize it on the map
                if (ViewModel.SelectedArea != null)
                {
                    // Center the map on the selected area and highlight it
                    await CenterMapOnAreaAsync(ViewModel.SelectedArea,googleMapsConfig.DefaultZoom);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al mostrar área en el mapa", ex, "AreasPage", "AreasList_SelectionChanged");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al mostrar el área en el mapa.");
            }
            finally
            {
                Interlocked.Exchange(ref _isCenteringMapInt, 0);
            }
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
                                "AreasPage",
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
                            _activityService.Registrar("Areas", "Área eliminada");
                            await _notificacionService.MostrarAsync("Éxito", "Área eliminada correctamente.");

                            // Solo recargar el mapa si el área eliminada estaba seleccionada
                            if (ViewModel.SelectedArea?.IdArea == areaId)
                            {
                                ViewModel.SelectedArea = null;
                                await LoadMapAsync();
                            }
                        }
                        else
                        {
                            await _notificacionService.MostrarAsync("Error", deleteResult.Message ?? "Error al eliminar el área.");
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
                "AreasPage",
                "SaveButton_Click");
            
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                await _notificacionService.MostrarAsync("Validación", "El nombre del área es requerido.");
                return;
            }

            bool isGeoType = _currentShapeType == "Estado" || _currentShapeType == "Municipio";

            if (!_isEditMode && string.IsNullOrEmpty(_currentShapeType))
            {
                await _notificacionService.MostrarAsync("Validación", "Selecciona un estado o municipio en el mapa antes de guardar.");
                return;
            }

            if (!_isEditMode && !isGeoType)
            {
                bool hasValidShapeData = _currentShapeType?.ToLower() == "circle"
                    ? !string.IsNullOrEmpty(_currentShapeCenter) && _currentShapeRadius.HasValue
                    : !string.IsNullOrEmpty(_currentShapePath);

                if (!hasValidShapeData)
                {
                    await _notificacionService.MostrarAsync("Validación", "Selecciona un estado o municipio en el mapa antes de guardar.");
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
                MetadataJSON = isGeoType
                    ? JsonSerializer.Serialize(new
                      {
                          geoType = _currentShapeType,
                          nombre = _pendingGeoNombre ?? NombreTextBox.Text.Trim(),
                          estado = _pendingEstadoNombre
                      })
                    : _isEditMode && string.IsNullOrEmpty(_currentShapePath)
                        ? null
                        : JsonSerializer.Serialize(new Dictionary<string, object?>
                        {
                            ["path"] = _currentShapePath != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapePath) : (object?)null,
                            ["center"] = _currentShapeCenter != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapeCenter) : (object?)null,
                            ["bounds"] = _currentShapeBounds != null ? JsonSerializer.Deserialize<JsonElement>(_currentShapeBounds) : (object?)null,
                            ["radius"] = _currentShapeRadius
                        })
            };

            // Log the serialized MetadataJSON for debugging data flow
            await _loggingService.LogInformationAsync(
                $"[DATA_FLOW] Step 2 - MetadataJSON: {TruncateForLog(area.MetadataJSON)}",
                "AreasPage",
                "SaveButton_Click");

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
                        "AreasPage",
                        "SaveButton_Click");
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
                        "AreasPage",
                        "SaveButton_Click");
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
                await _notificacionService.MostrarAsync("Éxito", _isEditMode ? "Área actualizada correctamente." : "Área creada correctamente.");

                _activityService.Registrar("Areas", _isEditMode ? "Área modificada" : "Área creada");
                CancelButton_Click(sender, e);

                await ExecuteMapScriptAsync("clearCurrentShape();");
                await LoadMapAsync();
            }
            else
            {
                await _notificacionService.MostrarAsync("Error", result.Message ?? "Error al guardar el área.");
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
            _pendingGeoNombre = null;
            _pendingGeoType = null;
            _pendingEstadoNombre = null;

            AreasList.SelectedItem = null;
        }

        private async void MapWebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                await _loggingService.LogInformationAsync("WebView2 navegación completada exitosamente", "AreasPage", "MapWebView_NavigationCompleted");
            }
            else
            {
                await _loggingService.LogErrorAsync($"WebView2 navegación falló. Status: {args.WebErrorStatus}", null, "AreasPage", "MapWebView_NavigationCompleted");
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

                await _loggingService.LogInformationAsync($"Buscando ubicación: {searchQuery}", "AreasPage", "MapSearchBox_KeyDown");

                if (MapWebView?.CoreWebView2 != null)
                {
                    var encodedQuery = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(searchQuery);
                    var script = $"searchLocation('{encodedQuery}');";
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al buscar ubicación", ex, "AreasPage", "MapSearchBox_KeyDown");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al buscar la ubicación");
            }
        }

        /// <summary>
        /// Centra el mapa en un área seleccionada y la dibuja si no existe
        /// </summary>
        private async Task CenterMapOnAreaAsync(AreaDto area, int zoom)
        {
            try
            {
                if (MapWebView?.CoreWebView2 == null)
                {
                    await _loggingService.LogWarningAsync("MapWebView o CoreWebView2 es null", "AreasPage", "CenterMapOnAreaAsync");
                    return;
                }

                // Detectar si es un área de tipo geo (estado/municipio)
                if (!string.IsNullOrEmpty(area.MetadataJSON))
                {
                    try
                    {
                        var meta = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(area.MetadataJSON);
                        if (meta != null && meta.TryGetValue("geoType", out var geoTypeEl))
                        {
                            var geoType = geoTypeEl.GetString();
                            var nombre = meta.TryGetValue("nombre", out var nEl) ? nEl.GetString() : area.Nombre;
                            var estado = meta.TryGetValue("estado", out var eEl) ? eEl.GetString() : null;
                            var nombreJson = JsonSerializer.Serialize(nombre);
                            var script = geoType == "Estado"
                                ? $"highlightEstado({nombreJson});"
                                : $"highlightMunicipio({nombreJson}, {JsonSerializer.Serialize(estado)});";
                            await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
                            return;
                        }
                    }
                    catch { /* continuar con lógica de coordenadas */ }
                }

                // Check if area has center coordinates
                if (area.CentroLatitud.HasValue && area.CentroLongitud.HasValue)
                {
                    var latStr = area.CentroLatitud.Value.ToString("F6", CultureInfo.InvariantCulture);
                    var lngStr = area.CentroLongitud.Value.ToString("F6", CultureInfo.InvariantCulture);

                    // Prepare area data JSON for drawing on the map
                    var areaDataJson = PrepareAreaDataJsonForDrawing(area);

                    var script = $@"
                        (function() {{
                            var position = {{ lat: {latStr}, lng: {lngStr} }};
                            map.setCenter(position);
                            map.setZoom({zoom});
                            drawSelectedArea({areaDataJson});
                        }})();
                    ";
                    await MapWebView.CoreWebView2.ExecuteScriptAsync(script);

                    await _loggingService.LogInformationAsync(
                        $"Mapa centrado en área: {area.Nombre} ({latStr}, {lngStr})",
                        "AreasPage",
                        "CenterMapOnAreaAsync");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Información", "El área seleccionada no tiene coordenadas de centro. No se puede centrar en el mapa.");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al centrar el mapa en el área", ex, "AreasPage", "CenterMapOnAreaAsync");
            }
        }

        /// <summary>
        /// Prepara los datos del área en formato JSON para ser dibujada en el mapa
        /// </summary>
        private string PrepareAreaDataJsonForDrawing(AreaDto area)
        {
            try
            {
                var areaData = new
                {
                    idArea = area.IdArea,
                    nombre = area.Nombre,
                    type = string.IsNullOrEmpty(area.TipoGeometria) ? "Polygon" : area.TipoGeometria,
                    path = ParsePathJson(area),
                    center = ParseCenterJson(area),
                    radius = area.Radio,
                    fillColor = area.ColorMapa ?? "#FF0000",
                    fillOpacity = (double)(area.Opacidad ?? 0.35m)
                };

                return JsonSerializer.Serialize(areaData);
            }
            catch (Exception ex)
            {
                _ = _loggingService.LogErrorAsync("Error al preparar JSON de área para dibujo", ex, "AreasPage", "PrepareAreaDataJsonForDrawing");
                // Return minimal data to at least show the center point
                return JsonSerializer.Serialize(new
                {
                    idArea = area.IdArea,
                    nombre = area.Nombre,
                    type = "Circle",
                    center = ParseCenterJson(area),
                    radius = FALLBACK_CIRCLE_RADIUS_METERS,
                    fillColor = "#FF0000",
                    fillOpacity = 0.35
                });
            }
        }

    }
}

