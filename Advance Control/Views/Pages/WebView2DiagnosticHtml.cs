namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Genera páginas HTML de diagnóstico que se cargan en el WebView2 cuando
    /// no se puede inicializar el mapa. Evita dejar el WebView completamente en blanco
    /// y comunica al usuario y al soporte la causa exacta del problema.
    /// </summary>
    internal static class WebView2DiagnosticHtml
    {
        public static string Build(string titulo, string detalleHtml)
        {
            return $@"<!DOCTYPE html>
<html lang='es'><head><meta charset='utf-8'><title>{System.Net.WebUtility.HtmlEncode(titulo)}</title>
<style>
  html,body{{margin:0;padding:0;height:100%;font-family:'Segoe UI',Roboto,Arial,sans-serif;background:#f5f6f8;color:#202124;}}
  .wrap{{display:flex;align-items:center;justify-content:center;height:100%;padding:24px;box-sizing:border-box;}}
  .card{{background:#fff;border:1px solid #e0e0e0;border-radius:10px;padding:32px;max-width:560px;box-shadow:0 4px 16px rgba(0,0,0,.06);}}
  h1{{margin:0 0 12px 0;font-size:20px;color:#c5221f;}}
  p{{margin:0 0 12px 0;line-height:1.5;font-size:14px;}}
  code{{background:#f1f3f4;padding:2px 6px;border-radius:4px;font-family:Consolas,monospace;font-size:13px;}}
  ul{{margin:8px 0 0 18px;padding:0;font-size:13px;line-height:1.6;}}
  .ico{{font-size:42px;line-height:1;margin-bottom:8px;}}
</style></head>
<body><div class='wrap'><div class='card'>
  <div class='ico'>⚠️</div>
  <h1>{System.Net.WebUtility.HtmlEncode(titulo)}</h1>
  <p>{detalleHtml}</p>
  <p><strong>Pasos sugeridos:</strong></p>
  <ul>
    <li>Verificar que la API esté en línea y responda correctamente.</li>
    <li>Confirmar que <code>GoogleMaps:ApiKey</code> esté configurada en <code>appsettings.json</code>.</li>
    <li>Confirmar que la clave esté activa en Google Cloud Console (APIs habilitadas, billing activo, restricciones correctas).</li>
    <li>Reiniciar el servicio de la API después de cambiar la configuración.</li>
  </ul>
</div></div></body></html>";
        }
    }
}
