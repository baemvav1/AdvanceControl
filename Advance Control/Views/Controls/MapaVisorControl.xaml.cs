using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Advance_Control.Services.GoogleMaps;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Advance_Control.Views.Controls
{
    /// <summary>Marcador del mapa (una ubicación).</summary>
    public sealed record MarcadorMapa(int Id, string Nombre, double Lat, double Lng, bool Resaltado);

    /// <summary>Qué dibujar: contorno de un área, marcadores y a dónde mirar.</summary>
    public sealed class VistaMapa
    {
        public List<(double Lat, double Lng)> Contorno { get; init; } = new();
        public List<MarcadorMapa> Marcadores { get; init; } = new();
        public (double Lat, double Lng)? Centro { get; init; }
        public int? Zoom { get; init; }
        /// <summary>Encuadra el contorno (si hay) o los marcadores.</summary>
        public bool Encuadrar { get; init; } = true;
    }

    public sealed record GeoElegido(string GeoType, string Nombre, string? Estado);

    public sealed record PuntoElegido(double Lat, double Lng, string? Direccion, string? Ciudad, string? Estado);

    /// <summary>
    /// Mapa del Visor: Google Maps en WebView2, con el mismo esquema que AreasPage (HTML y GeoJSON
    /// servidos desde memoria vía WebResourceRequested, para tener origen https real sin depender
    /// del disco ni del sandbox de MSIX).
    /// Modos: vista normal (contorno + marcadores; clic en marcador → <see cref="MarcadorClic"/>),
    /// crear área (capas Estado/Municipio; clic → <see cref="GeoClic"/>) y crear ubicación
    /// (búsqueda de dirección + clic/arrastre del punto → <see cref="PuntoClic"/>).
    /// Las órdenes que llegan antes de que el mapa esté listo se encolan.
    /// </summary>
    public sealed partial class MapaVisorControl : UserControl
    {
        private const string UrlHtml = "https://ac-visor-local/visor.html";

        private readonly ILoggingService _logger;
        private readonly List<string> _pendientes = new();
        private CoreWebView2Environment? _env;
        private string? _html;
        private bool _iniciado;
        private bool _listo;

        public event EventHandler<int>? MarcadorClic;
        public event EventHandler<GeoElegido>? GeoClic;
        public event EventHandler<PuntoElegido>? PuntoClic;

        public MapaVisorControl()
        {
            _logger = AppServices.Get<ILoggingService>();
            InitializeComponent();
            Loaded += async (_, _) => await IniciarAsync();
        }

        // ── API para la página ───────────────────────────────────────────

        public void MostrarVista(VistaMapa vista)
        {
            var datos = new
            {
                contorno = vista.Contorno.Select(p => new { lat = p.Lat, lng = p.Lng }),
                marcadores = vista.Marcadores.Select(m => new { id = m.Id, nombre = m.Nombre, lat = m.Lat, lng = m.Lng, resaltado = m.Resaltado }),
                centro = vista.Centro is { } c ? new { lat = c.Lat, lng = c.Lng } : null,
                zoom = vista.Zoom,
                encuadrar = vista.Encuadrar
            };
            Ejecutar($"vista({JsonSerializer.Serialize(datos)});");
        }

        public void ModoCrearArea(bool activo) => Ejecutar($"modoCrearArea({(activo ? "true" : "false")});");

        public void ModoCrearUbicacion(bool activo) => Ejecutar($"modoCrearUbicacion({(activo ? "true" : "false")});");

        // ── WebView2 ─────────────────────────────────────────────────────

        private void Ejecutar(string script)
        {
            if (_listo && Web.CoreWebView2 is not null)
                _ = Web.CoreWebView2.ExecuteScriptAsync(script);
            else
                _pendientes.Add(script);
        }

        private async Task IniciarAsync()
        {
            if (_iniciado) return;
            _iniciado = true;
            try
            {
                var config = await AppServices.Get<IGoogleMapsConfigService>().GetConfigAsync();
                if (config is null || string.IsNullOrWhiteSpace(config.ApiKey))
                {
                    EstadoText.Text = "No se pudo cargar la configuración de Google Maps.";
                    return;
                }

                var partes = (config.DefaultCenter ?? string.Empty).Split(',');
                var lat = partes.Length > 0 && double.TryParse(partes[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var la) ? la : 22.1497;
                var lng = partes.Length > 1 && double.TryParse(partes[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ln) ? ln : -100.975;
                _html = Html(config.ApiKey, lat, lng, config.DefaultZoom > 0 ? config.DefaultZoom : 5);

                _env = await CoreWebView2Environment.CreateAsync();
                await Web.EnsureCoreWebView2Async(_env);
                var core = Web.CoreWebView2;
                if (core is null)
                {
                    EstadoText.Text = "No se pudo iniciar el mapa (WebView2).";
                    return;
                }

                core.Settings.AreDevToolsEnabled = true;
                core.WebMessageReceived += Core_WebMessageReceived;
                await core.AddScriptToExecuteOnDocumentCreatedAsync(Advance_Control.Views.Pages.WebView2DiagnosticsScript.JS);
                core.AddWebResourceRequestedFilter("https://ac-visor-local/*", CoreWebView2WebResourceContext.All);
                core.AddWebResourceRequestedFilter("https://geo-assets/*", CoreWebView2WebResourceContext.All);
                core.WebResourceRequested += Core_WebResourceRequested;
                core.Navigate(UrlHtml);
            }
            catch (Exception ex)
            {
                EstadoText.Text = "No se pudo iniciar el mapa.";
                await _logger.LogErrorAsync("Error al iniciar el mapa del Visor", ex, nameof(MapaVisorControl), nameof(IniciarAsync));
            }
        }

        private void Core_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            if (_env is null) return;
            var uri = args.Request.Uri;
            if (uri == UrlHtml && _html is not null)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(_html);
                args.Response = _env.CreateWebResourceResponse(new MemoryStream(bytes).AsRandomAccessStream(), 200, "OK",
                    "Content-Type: text/html; charset=utf-8");
                return;
            }

            byte[]? geo = uri switch
            {
                "https://geo-assets/estados.json" => GeoContornos.Estados,
                "https://geo-assets/municipios.json" => GeoContornos.Municipios,
                _ => null
            };
            if (geo is not null)
                args.Response = _env.CreateWebResourceResponse(new MemoryStream(geo).AsRandomAccessStream(), 200, "OK",
                    "Content-Type: application/json\r\nAccess-Control-Allow-Origin: *");
        }

        private void Core_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                using var doc = JsonDocument.Parse(args.TryGetWebMessageAsString());
                var raiz = doc.RootElement;
                string? Texto(string p) => raiz.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

                switch (Texto("type"))
                {
                    case "listo":
                        _listo = true;
                        EstadoText.Text = string.Empty;
                        foreach (var s in _pendientes) _ = sender.ExecuteScriptAsync(s);
                        _pendientes.Clear();
                        break;
                    case "marcador":
                        if (raiz.TryGetProperty("id", out var id) && id.TryGetInt32(out var idUbicacion))
                            MarcadorClic?.Invoke(this, idUbicacion);
                        break;
                    case "geo":
                        var nombre = Texto("nombre");
                        if (!string.IsNullOrWhiteSpace(nombre))
                            GeoClic?.Invoke(this, new GeoElegido(Texto("geoType") ?? "Estado", nombre!, Texto("estado")));
                        break;
                    case "punto":
                        if (raiz.TryGetProperty("lat", out var la) && raiz.TryGetProperty("lng", out var ln))
                            PuntoClic?.Invoke(this, new PuntoElegido(la.GetDouble(), ln.GetDouble(),
                                Texto("direccion"), Texto("ciudad"), Texto("estado")));
                        break;
                    case "error":
                    case "jsError":
                        _ = _logger.LogWarningAsync($"Mapa del Visor: {args.TryGetWebMessageAsString()}", nameof(MapaVisorControl), "JS");
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = _logger.LogWarningAsync($"Mensaje del mapa no válido: {ex.Message}", nameof(MapaVisorControl), nameof(Core_WebMessageReceived));
            }
        }

        // ── HTML del mapa ────────────────────────────────────────────────

        private static string Html(string apiKey, double lat, double lng, int zoom)
        {
            var inv = CultureInfo.InvariantCulture;
            return $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
  html, body, #map { margin: 0; height: 100%; width: 100%; font-family: 'Segoe UI', Roboto, Arial, sans-serif; }
  .panel { position: absolute; z-index: 10; display: none; }
  #busqueda { top: 12px; left: 50%; transform: translateX(-50%); width: 60%; }
  #busqueda input { width: 100%; box-sizing: border-box; padding: 10px 14px; font-size: 14px; border: none;
    border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,.2); outline: none; }
  #capas { top: 12px; right: 12px; flex-direction: column; gap: 6px; }
  #capas button { padding: 8px 14px; background: #fff; border: 1px solid #dadce0; border-radius: 4px; cursor: pointer;
    font-size: 13px; box-shadow: 0 2px 6px rgba(0,0,0,.15); }
  #capas button.activo { background: #e8f0fe; color: #1a73e8; border-color: #4285F4; }
  #capas button:disabled { opacity: .45; cursor: default; }
  #aviso { bottom: 16px; left: 50%; transform: translateX(-50%); background: rgba(0,0,0,.7); color: #fff;
    padding: 6px 14px; border-radius: 16px; font-size: 12px; pointer-events: none; }
</style>
</head>
<body>
<div id="busqueda" class="panel"><input id="buscar" type="text" placeholder="Buscar dirección…"></div>
<div id="capas" class="panel">
  <button id="btnEstado" onclick="capaEstados()">Estado</button>
  <button id="btnMunicipio" onclick="capaMunicipios()" disabled>Municipio</button>
</div>
<div id="aviso" class="panel"></div>
<div id="map"></div>
<script>
  let map, poligono = null, marcadores = [], punto = null, autocomplete = null, geocoder = null;
  let modoArea = false, modoUbicacion = false, estadoSel = null, enMunicipios = false;
  let estadosGeo = null, municipiosGeo = null;
  const enviar = o => { try { window.chrome.webview.postMessage(JSON.stringify(o)); } catch (e) {} };
  const aviso = t => { const a = document.getElementById('aviso'); a.textContent = t || ''; a.style.display = t ? 'block' : 'none'; };

  async function initMap() {
    const { Map } = await google.maps.importLibrary('maps');
    await google.maps.importLibrary('marker');
    map = new Map(document.getElementById('map'), {
      center: { lat: {{lat.ToString(inv)}}, lng: {{lng.ToString(inv)}} }, zoom: {{zoom}}, mapId: 'DEMO_MAP_ID',
      streetViewControl: false, mapTypeControl: true
    });
    geocoder = new google.maps.Geocoder();

    const input = document.getElementById('buscar');
    autocomplete = new google.maps.places.Autocomplete(input, { fields: ['geometry', 'formatted_address', 'address_components'] });
    autocomplete.bindTo('bounds', map);
    autocomplete.addListener('place_changed', () => {
      const p = autocomplete.getPlace();
      if (!p.geometry || !p.geometry.location) return;
      map.panTo(p.geometry.location); map.setZoom(17);
      ponerPunto(p.geometry.location.lat(), p.geometry.location.lng(), p);
    });

    map.addListener('click', e => { if (modoUbicacion) ponerPunto(e.latLng.lat(), e.latLng.lng(), null); });

    map.data.addListener('click', e => {
      if (!modoArea) return;
      const nombre = e.feature.getProperty('nombre') || '';
      const estado = e.feature.getProperty('estado') || nombre;
      if (!enMunicipios) {
        estadoSel = nombre;
        map.data.setStyle(f => f.getProperty('nombre') === estadoSel ? ESTILO_SEL : ESTILO);
        document.getElementById('btnMunicipio').disabled = false;
        enviar({ type: 'geo', geoType: 'Estado', nombre: nombre });
      } else {
        map.data.revertStyle(); map.data.overrideStyle(e.feature, ESTILO_SEL);
        enviar({ type: 'geo', geoType: 'Municipio', nombre: nombre, estado: estado });
      }
    });
    enviar({ type: 'listo' });
  }

  const ESTILO = { fillColor: '#4285F4', fillOpacity: .06, strokeColor: '#4285F4', strokeWeight: 1.5 };
  const ESTILO_SEL = { fillColor: '#1565C0', fillOpacity: .25, strokeColor: '#1565C0', strokeWeight: 2.5 };

  function limpiarVista() {
    if (poligono) { poligono.setMap(null); poligono = null; }
    marcadores.forEach(m => m.map = null); marcadores = [];
  }

  // Contorno del área + marcadores de ubicaciones; encuadra lo que haya.
  function vista(d) {
    limpiarVista();
    const bounds = new google.maps.LatLngBounds();
    if (d.contorno && d.contorno.length > 2) {
      poligono = new google.maps.Polygon({ paths: d.contorno, map, fillColor: '#1a73e8', fillOpacity: .12,
        strokeColor: '#1a73e8', strokeWeight: 2, clickable: false });
      d.contorno.forEach(p => bounds.extend(p));
    }
    (d.marcadores || []).forEach(m => {
      const pin = new google.maps.marker.PinElement(m.resaltado
        ? { background: '#d93025', borderColor: '#a50e0e', glyphColor: '#fff', scale: 1.25 }
        : { background: '#1a73e8', borderColor: '#0b57d0', glyphColor: '#fff' });
      const mk = new google.maps.marker.AdvancedMarkerElement({ map, position: { lat: m.lat, lng: m.lng }, title: m.nombre, content: pin.element, gmpClickable: true });
      mk.addListener('click', () => enviar({ type: 'marcador', id: m.id }));
      marcadores.push(mk);
      if (!(d.contorno && d.contorno.length > 2)) bounds.extend({ lat: m.lat, lng: m.lng });
    });
    if (d.centro) { map.setCenter(d.centro); if (d.zoom) map.setZoom(d.zoom); }
    else if (d.encuadrar && !bounds.isEmpty()) {
      map.fitBounds(bounds, 40);
      if (!(d.contorno && d.contorno.length > 2) && (d.marcadores || []).length === 1) map.setZoom(16);
    }
  }

  async function capaEstados() {
    try {
      if (!estadosGeo) estadosGeo = await (await fetch('https://geo-assets/estados.json')).json();
      quitarCapas(); map.data.addGeoJson(estadosGeo); map.data.setStyle(ESTILO);
      enMunicipios = false; estadoSel = null;
      document.getElementById('btnEstado').classList.add('activo');
      document.getElementById('btnMunicipio').classList.remove('activo');
      document.getElementById('btnMunicipio').disabled = true;
      aviso('Haz clic en un estado; luego puedes pasar a sus municipios');
    } catch (e) { enviar({ type: 'error', message: 'estados: ' + e.message }); }
  }

  async function capaMunicipios() {
    if (!estadoSel) return;
    try {
      if (!municipiosGeo) municipiosGeo = await (await fetch('https://geo-assets/municipios.json')).json();
      quitarCapas();
      map.data.addGeoJson({ type: 'FeatureCollection', features: municipiosGeo.features.filter(f => f.properties && f.properties.estado === estadoSel) });
      map.data.setStyle(ESTILO); enMunicipios = true;
      document.getElementById('btnMunicipio').classList.add('activo');
      document.getElementById('btnEstado').classList.remove('activo');
      const b = new google.maps.LatLngBounds(); map.data.forEach(f => f.getGeometry().forEachLatLng(ll => b.extend(ll)));
      if (!b.isEmpty()) map.fitBounds(b);
      aviso('Haz clic en un municipio');
    } catch (e) { enviar({ type: 'error', message: 'municipios: ' + e.message }); }
  }

  function quitarCapas() { map.data.forEach(f => map.data.remove(f)); }

  function modoCrearArea(on) {
    modoArea = on;
    document.getElementById('capas').style.display = on ? 'flex' : 'none';
    if (on) capaEstados(); else { quitarCapas(); aviso(''); }
  }

  function modoCrearUbicacion(on) {
    modoUbicacion = on;
    document.getElementById('busqueda').style.display = on ? 'block' : 'none';
    if (on) aviso('Busca la dirección y ajusta el punto con un clic (o arrastrándolo)');
    else { aviso(''); if (punto) { punto.map = null; punto = null; } document.getElementById('buscar').value = ''; }
  }

  function componente(comps, tipo) {
    const c = (comps || []).find(x => x.types.includes(tipo)); return c ? c.long_name : null;
  }

  function enviarPunto(lat, lng, lugar) {
    const mandar = (dir, comps) => enviar({ type: 'punto', lat, lng, direccion: dir,
      ciudad: componente(comps, 'locality') || componente(comps, 'administrative_area_level_2'),
      estado: componente(comps, 'administrative_area_level_1') });
    if (lugar && lugar.address_components) { mandar(lugar.formatted_address || null, lugar.address_components); return; }
    geocoder.geocode({ location: { lat, lng } })
      .then(r => { const g = r.results && r.results[0]; mandar(g ? g.formatted_address : null, g ? g.address_components : null); })
      .catch(() => mandar(null, null));
  }

  function ponerPunto(lat, lng, lugar) {
    if (!punto) {
      const pin = new google.maps.marker.PinElement({ background: '#188038', borderColor: '#0d652d', glyphColor: '#fff', scale: 1.3 });
      punto = new google.maps.marker.AdvancedMarkerElement({ map, position: { lat, lng }, content: pin.element, gmpDraggable: true });
      punto.addListener('dragend', () => { const p = punto.position; enviarPunto(typeof p.lat === 'function' ? p.lat() : p.lat, typeof p.lng === 'function' ? p.lng() : p.lng, null); });
    } else {
      punto.position = { lat, lng };
    }
    enviarPunto(lat, lng, lugar);
  }
</script>
<script src="https://maps.googleapis.com/maps/api/js?key={{apiKey}}&libraries=places&callback=initMap" async defer></script>
</body>
</html>
""";
        }
    }
}
