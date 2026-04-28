namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Script JS inyectado en los WebView2 que cargan Google Maps. Captura errores
    /// no controlados, errores de autenticación de Google Maps (gm_authFailure)
    /// y los reenvía al host .NET vía window.chrome.webview.postMessage.
    /// El handler C# debe reconocer los tipos: jsError, jsConsoleError, jsUnhandledRejection y gmAuthFailure.
    /// </summary>
    internal static class WebView2DiagnosticsScript
    {
        public const string JS = @"
(function(){
    if (window.__acDiagnosticsInstalled) return;
    window.__acDiagnosticsInstalled = true;
    function send(payload){
        try { window.chrome.webview.postMessage(JSON.stringify(payload)); } catch(e) {}
    }
    window.addEventListener('error', function(ev){
        send({
            type: 'jsError',
            message: ev && ev.message ? String(ev.message) : 'unknown',
            source: ev && ev.filename ? String(ev.filename) : '',
            line: ev && ev.lineno ? ev.lineno : 0,
            col: ev && ev.colno ? ev.colno : 0,
            stack: ev && ev.error && ev.error.stack ? String(ev.error.stack) : ''
        });
    });
    window.addEventListener('unhandledrejection', function(ev){
        var reason = ev && ev.reason ? ev.reason : 'unknown';
        send({
            type: 'jsUnhandledRejection',
            message: (typeof reason === 'string') ? reason : (reason.message || String(reason)),
            stack: (reason && reason.stack) ? String(reason.stack) : ''
        });
    });
    var origConsoleError = console.error;
    console.error = function(){
        try {
            var parts = [];
            for (var i=0;i<arguments.length;i++){
                var a = arguments[i];
                parts.push(typeof a === 'string' ? a : (a && a.stack ? a.stack : JSON.stringify(a)));
            }
            send({ type: 'jsConsoleError', message: parts.join(' ') });
        } catch(e) {}
        origConsoleError.apply(console, arguments);
    };
    // Hook oficial de Google Maps cuando la API key falla (clave inválida, billing apagado, refer no permitido, etc.)
    window.gm_authFailure = function(){
        send({ type: 'gmAuthFailure', message: 'Google Maps reportó fallo de autenticación. Revisa: API key, billing/facturación habilitada, restricciones de refer/HTTP, APIs habilitadas (Maps JavaScript API, Geocoding, Places).' });
    };
})();
";
    }
}
