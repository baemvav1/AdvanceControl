# Sesión "salvandolosmapas" — recuperación

Última actualización: 2026-09-08. Repo: `AdvanceControl`, rama `main`.

## Problema original

Los mapas (`AreasPage`, `VisorMundialPage`, `UbicacionesPage`) mostraban error de
carga en el WebView2 ("no se encuentra el archivo").

## Lo ya hecho (commiteado en `main`)

1. `estados.json`/`municipios.json` no llegaban como `Content` suelto tras instalar
   el MSIX → se incrustaron como `EmbeddedResource` en el `.dll`, con extracción a
   `%LOCALAPPDATA%\Advance Control\geo` en runtime.
   Commit: `c46071f Correcciones mapas` (mergeado con `a4300b3`).
2. Eso no resolvió el error real: `areas.html` fallaba en la pestaña Network del
   WebView2 con `(failed)` sin status HTTP → se aplicó cache-busting en
   `Navigate()` en las 3 páginas, y se corrigió un leak de `NavigationCompleted`
   en `UbicacionesPage` (se suscribía con `+=` en cada `LoadMapAsync` sin
   desuscribirse antes).
3. Con eso tampoco se arregló → se agregó mostrar el `WebErrorStatus` real en el
   InfoBar de `AreasPage` (antes solo iba al log remoto). Resultado obtenido:
   **`ConnectionAborted`** (`net::ERR_CONNECTION_ABORTED`), en el 100% de las
   cargas.
4. Se descartó antivirus/EDR corporativo (el usuario confirmó que no hay).
5. Prueba de aislamiento: desde la consola de DevTools del mismo WebView2 se
   ejecutó `window.location = 'https://www.google.com'` → **cargó
   correctamente**. Esto descarta que sea un problema general del WebView2
   Runtime/entorno (SSL, proxy, etc.) — el fallo es específico de los virtual
   hosts internos de la app (`https://ac-maps-local/...`,
   `https://ac-visor-mundial-local/...`).

## Estado del working tree (sin commitear)

`Advance Control/Views/Pages/AreasPage.xaml.cs` tiene un diff sin commitear
(diagnóstico agregado en esta sesión, línea ~1347-1350):

```csharp
else
{
    ShowDiag(Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
        $"El mapa no cargó. WebErrorStatus: {args.WebErrorStatus}");
    await _loggingService.LogErrorAsync($"WebView2 navegación falló. Status: {args.WebErrorStatus}", null, "AreasPage", "MapWebView_NavigationCompleted");
}
```

Este cambio es el que permitió ver `ConnectionAborted` en pantalla. Se puede
dejar o quitar una vez resuelto el bug real.

## Hipótesis de la Visibility — probada y descartada (2026-09-08, tarde)

Se aplicó el fix descrito abajo (mover `IsMapInitialized = true` a
`MapWebView_NavigationCompleted`, tras el `NavigationCompleted`, para que
nunca coincida con la navegación en vuelo). **Se compiló y probó en local:
el error persiste, mismo `ConnectionAborted`.** Esta hipótesis queda
descartada. El código de ese cambio se dejó (es inofensivo y correcto en sí
mismo — evita una fuga potencial de handlers duplicados — pero no era la
causa raíz).

## Fix aplicado (2026-09-08, tarde) — servir el HTML desde memoria, no desde disco

Evidencia nueva de un screenshot de Network: **dos peticiones a `areas.html`**
en la misma carga, ambas sin status HTTP y 0 B — señal de navegación
duplicada/abortada por el propio motor, no de un 404 real.

En vez de seguir depurando por qué `SetVirtualHostNameToFolderMapping` +
escritura a disco (`%LOCALAPPDATA%\Advance Control\map_cache\areas.html`,
redirigido por el sandboxing MSIX a
`AppData\Local\Packages\<PFN>\LocalCache\Local\Advance Control\map_cache\`)
fallaba con `ERR_CONNECTION_ABORTED` de forma consistente, se eliminó esa
ruta de código por completo para `areas.html`:

- Se agregó `_webView2Environment` (guarda el `CoreWebView2Environment` de
  `EnsureCoreWebView2Async`) y `_currentAreasMapHtml` (el HTML generado, en
  memoria) como campos de `AreasPage`.
- En `EnsureWebView2InitializedAsync`: se quitó
  `SetVirtualHostNameToFolderMapping("ac-maps-local", mapCacheDir, ...)` y se
  reemplazó por `AddWebResourceRequestedFilter("https://ac-maps-local/areas.html", ...Document)`
  + `WebResourceRequested += CoreWebView2_AreasHtmlRequested`.
- Nuevo handler `CoreWebView2_AreasHtmlRequested`: responde la petición
  directamente con `_currentAreasMapHtml` desde un `MemoryStream`
  (`.AsRandomAccessStream()`) vía `CreateWebResourceResponse`, sin tocar
  disco.
- En `LoadMapAsync`: ya no escribe `areas.html` a disco; solo hace
  `_currentAreasMapHtml = html;` antes de `Navigate()`.
- El virtual host `geo-assets` (GeoJSON de estados/municipios, estático, vía
  `fetch()` desde JS) **no se tocó** — sigue usando
  `SetVirtualHostNameToFolderMapping`. Si el problema resulta ser genérico
  del mecanismo de virtual-host-to-folder en este entorno (no específico de
  documentos regenerados en cada carga), `geo-assets` podría fallar también
  y necesitaría el mismo tratamiento (sería la próxima pista si esto no
  resuelve el error).

Compila limpio (Debug, sin Platform y con `-p:Platform=x64`, salvo el error
preexistente de empaquetado MSIX/AnyCPU no relacionado).

**Confirmado por el usuario: el mapa ya carga.** Esto confirma que el
problema SÍ era genérico de `SetVirtualHostNameToFolderMapping`/carpeta
virtual en este equipo, no algo específico de `areas.html` regenerado en
cada carga.

## Fix aplicado #2 (2026-09-08, tarde) — botones Estado/Municipio (geo-assets)

Con el mapa cargando, los botones "Estado"/"Municipio" seguían sin
funcionar — usan `fetch('https://geo-assets/estados.json')` y
`municipios.json`, servidos por el mismo mecanismo
(`SetVirtualHostNameToFolderMapping("geo-assets", geoFolder, ...)`) que ya
se sabía poco confiable. Se aplicó el mismo patrón que para `areas.html`:

- Nuevos campos `_estadosGeoJsonBytes` / `_municipiosGeoJsonBytes` (byte[]
  en memoria).
- `EnsureGeoAssetsExtractedAsync()` (extraía a disco en
  `%LOCALAPPDATA%\Advance Control\geo`) reemplazado por
  `LoadGeoAssetsIntoMemory()` (lee los recursos embebidos directo a
  `MemoryStream`/`byte[]`, sin tocar disco).
- En `EnsureWebView2InitializedAsync`: se quitó el
  `SetVirtualHostNameToFolderMapping("geo-assets", ...)` y se reemplazó por
  `AddWebResourceRequestedFilter("https://geo-assets/*", ...All)` +
  `WebResourceRequested += CoreWebView2_GeoAssetsRequested`.
- Nuevo handler `CoreWebView2_GeoAssetsRequested`: responde
  `estados.json`/`municipios.json` desde los `byte[]` en memoria.
- **Importante**: como `WebResourceRequested` dispara TODOS los handlers
  suscritos en cuanto CUALQUIER filtro matchea (no aísla por handler), se le
  agregó a `CoreWebView2_AreasHtmlRequested` una verificación explícita de
  `args.Request.Uri` — si no, respondía con el HTML del mapa también a
  peticiones de `geo-assets`.

Compila limpio. **Usuario probó: los botones seguían sin funcionar.**

## Fix aplicado #3 (2026-09-08, tarde) — CORS + error silencioso

Causa encontrada: `SetVirtualHostNameToFolderMapping` con
`CoreWebView2HostResourceAccessKind.Allow` permitía automáticamente CORS
entre virtual hosts; al construir la respuesta a mano con
`CreateWebResourceResponse` en `CoreWebView2_GeoAssetsRequested` **no se
agregó el header `Access-Control-Allow-Origin`**, así que el
`fetch('https://geo-assets/...')` hecho desde el origen
`https://ac-maps-local` (otro origen) era bloqueado por CORS.

Además, el error resultante (capturado en JS y reenviado como mensaje
`geoClickError`) solo se logueaba de forma remota
(`CoreWebView2_WebMessageReceived`, case `geoClickError`) — nunca se
mostraba en pantalla, así que el fallo era invisible para quien probaba
("no pasa nada al hacer clic").

Cambios:
- `CoreWebView2_GeoAssetsRequested`: headers de la respuesta ahora incluyen
  `Access-Control-Allow-Origin: *` además de `Content-Type`.
- `CoreWebView2_WebMessageReceived`, case `geoClickError`: ahora también
  hace `ShowDiag(...Error, ...)` (antes solo logueaba), para que cualquier
  fallo futuro en los botones Estado/Municipio se vea en el InfoBar en vez
  de quedar silencioso.

Compila limpio. **Confirmado: el usuario probó y los botones Estado/Municipio
funcionan.** Resultó ser exactamente lo esperado (Municipio se habilita al
clickear un estado específico en el mapa, no con solo activar el botón
"Estado") — el usuario confundió el flujo, no había bug ahí.

## Fix aplicado #4 (2026-09-08, tarde) — replicado a VisorMundialPage y UbicacionesPage

Mismo patrón (servir el HTML desde memoria vía `WebResourceRequested` en vez
de `SetVirtualHostNameToFolderMapping` + disco) aplicado a las otras 2
pantallas con mapa:

- **`VisorMundialPage.xaml.cs`**: agregado `_webView2Environment` y
  `_currentVisorMundialHtml`; reemplazado
  `SetVirtualHostNameToFolderMapping("ac-visor-mundial-local", ...)` por
  `AddWebResourceRequestedFilter` + `WebResourceRequested +=
  CoreWebView2_VisorMundialHtmlRequested`; `LoadMapAsync` ya no escribe
  `visor_mundial.html` a disco. No usa `geo-assets`, así que no necesitó ese
  segundo fix. No tenía riesgo de doble-navegación (un solo call site de
  `Navigate()`, protegido por el mismo lock que ya tenía para
  `EnsureWebView2InitializedAsync`).
- **`UbicacionesPage.xaml.cs`**: mismo cambio para `map.html`
  (`CoreWebView2_MapHtmlRequested`). Además, esta página SÍ tenía riesgo real
  de doble-navegación: `LoadMapAsync` se llamaba desde dos sitios
  (`OnPageLoaded` y `OnNavigatedTo`), cada uno cubriendo el caso de que el
  otro corriera primero, sin ningún guard — se agregó `_mapLoadStarted` +
  `_loadMapLock` (SemaphoreSlim) para que solo la primera llamada navegue de
  verdad. También se corrigió el leak ya identificado en la sesión original:
  `MapWebView.NavigationCompleted += (lambda)` se re-suscribía en cada
  llamada a `LoadMapAsync`; ahora es un método nombrado
  (`MapWebView_NavigationCompleted`) suscrito una sola vez.

Compila limpio (Debug, con y sin `-p:Platform=x64`, salvo el error
preexistente de empaquetado MSIX/AnyCPU no relacionado).

**Pendiente de que el usuario pruebe VisorMundialPage y UbicacionesPage con
F5 en Visual Studio.**

Si el error de `ConnectionAborted` reapareciera en alguna de estas 2
pantallas a pesar del fix: el siguiente sospechoso es que el
problema no sea específico de `areas.html` sino del **runtime/entorno de
WebView2 en este equipo para *cualquier* dominio que no sea el real de
internet** — a pesar de que `window.location = 'https://www.google.com'`
cargó bien, valdría la pena repetir esa prueba apuntando a un dominio
inventado pero de apariencia real (`https://algo-que-no-existe-real.com`)
para diferenciar "falla todo lo no-Google" de "falla todo lo que no sea DNS
público real", y revisar si hay un Firewall/Windows Defender Application
Guard/Network Isolation bloqueando específicamente hosts sin TLD real o
resolución DNS (los virtual hosts de WebView2 no usan DNS real, son
interceptados internamente — si algo intercepta a nivel de socket/Winsock
LSP antes de que WebView2 pueda resolverlos internamente, ambos mecanismos
—folder mapping y WebResourceRequested— fallarían igual, porque
`WebResourceRequested` también depende de que la petición llegue al proceso
de red de Chromium con ese host).

## Hipótesis de Visibility — texto original (para referencia histórica)

En `AreasPage.xaml.cs`, método `LoadMapAsync()` (líneas ~490-558):

```csharp
MapWebView.CoreWebView2.Navigate("https://ac-maps-local/areas.html");  // línea 549
ViewModel.IsMapInitialized = true;                                      // línea 550
```

`IsMapInitialized` está bindeado (`x:Bind ... Mode=OneWay`) a la
`Visibility` del `WebView2` en `AreasPage.xaml:98`
(`Visibility="{x:Bind ViewModel.IsMapInitialized, ..., Converter=...}"`,
`Collapsed` → `Visible`).

**Hipótesis:** cambiar la `Visibility` del control WebView2 justo después de
`Navigate()`, mientras la navegación sigue en vuelo, puede abortarla
(`ERR_CONNECTION_ABORTED`). Esto explicaría por qué falla en el 100% de las
cargas: el cambio de visibilidad ocurre siempre inmediatamente después del
`Navigate()`, nunca antes ni con delay.

**Aún sin verificar/aplicar**: mover `ViewModel.IsMapInitialized = true` para
que se dispare *después* de que la navegación complete (por ejemplo, dentro del
handler `MapWebView_NavigationCompleted`), en vez de inmediatamente después de
`Navigate()`. O alternativamente, hacer el WebView2 visible *antes* de navegar
para que el cambio de visibilidad no coincida con la navegación en vuelo.

### Diferencia entre las 3 páginas (revisado, no concluido)

- **`AreasPage.xaml.cs`** (líneas 549-550): `Navigate()` seguido inmediatamente
  de `ViewModel.IsMapInitialized = true` en el mismo método — coincide
  exactamente con la hipótesis.
- **`VisorMundialPage.xaml.cs`** (línea 203): `Navigate()` en `LoadMapAsync()`
  del code-behind, **pero** `IsMapInitialized = true` se setea en
  `VisorMundialViewModel.cs:302`, dentro de otro método (`InitializeAsync` del
  ViewModel, después de cargar datos vía `Task.WhenAll` de varios servicios) —
  no está confirmado si ese `IsMapInitialized = true` ocurre antes, durante o
  después de que la navegación del WebView2 complete. Falta trazar el orden de
  llamadas entre `LoadMapAsync()` (code-behind) e `InitializeAsync()`
  (ViewModel) para saber si aquí aplica la misma carrera.
- **`UbicacionesPage.xaml.cs`** (líneas 274-281): aquí el patrón es distinto —
  ya tiene un handler `NavigationCompleted` que llama `SetStatus(...)` y
  **no** depende de `IsMapInitialized` para la visibilidad en ese archivo
  (`IsMapInitialized` no aparece en `UbicacionesPage.xaml.cs`, solo se setea en
  `UbicacionesViewModel.cs:177`, al final de `InitializeAsync()`, después de
  `LoadAreasAsync`/`LoadUbicacionesAsync` — probablemente ya tarde respecto a la
  navegación, hay que confirmarlo mirando el XAML si `Visibility` de su
  `WebView2` también depende de `IsMapInitialized`).

**Quedó pendiente**: leer `AreasViewModel.cs` (línea 100-127, donde vive la
propiedad `IsMapInitialized` de `AreasPage`) para ver si el setter dispara
algo más (side effects) aparte de `OnPropertyChanged`, y confirmar en el XAML
de `UbicacionesPage` si su `WebView2` tiene un binding de `Visibility` similar.

## Próximo paso concreto al retomar

1. Confirmar la hipótesis del `Visibility`/`Navigate()` en carrera: en
   `AreasPage.xaml.cs`, mover `ViewModel.IsMapInitialized = true;` (línea 550)
   fuera de `LoadMapAsync()` y dispararlo desde
   `MapWebView_NavigationCompleted` (línea ~1342) cuando `args.IsSuccess` sea
   verdadero, en vez de inmediatamente tras `Navigate()`.
2. Compilar y probar en local (F5 desde Visual Studio) si el `ConnectionAborted`
   desaparece.
3. Si se confirma, aplicar el mismo patrón (mover el `= true` a después de
   `NavigationCompleted`) en `VisorMundialViewModel.cs` y
   `UbicacionesViewModel.cs`, revisando primero si sus respectivos XAML atan
   `Visibility` del `WebView2` a `IsMapInitialized` (falta confirmar para
   `UbicacionesPage`, ver sección anterior).
4. Una vez resuelto, decidir si dejar o quitar el diagnóstico agregado en
   `AreasPage.xaml.cs` (diff sin commitear, ver arriba) y hacer commit.

## Cómo retomar

Decirle a Claude: "recupera la conversación salvandolosmapas" y luego pedirle
que siga desde este archivo (`docs/salvandolosmapas-recovery.md`).
