# Implementación del Buscador de Google Maps en Ubicaciones

## Resumen
Se ha implementado un buscador de ubicaciones para el mapa de Google Maps en la página de **Ubicaciones**. Este buscador permite a los usuarios buscar cualquier ubicación utilizando el API de Google Places y posicionar automáticamente el mapa en los resultados encontrados.

## Características Implementadas

### 1. Interfaz de Usuario (UI)
**Ubicación**: `Advance Control/Views/Pages/Ubicaciones.xaml`

Se agregó un cuadro de búsqueda flotante en la parte superior del mapa con los siguientes elementos:

```xaml
<!-- Search Box for Map -->
<Grid 
    Grid.Column="1" 
    VerticalAlignment="Top" 
    Margin="12,12,12,0" 
    Padding="12"
    Background="{ThemeResource LayerFillColorDefaultBrush}"
    BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
    BorderThickness="1"
    CornerRadius="8"
    ZIndex="1000">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>
    <TextBox 
        x:Name="MapSearchBox"
        Grid.Column="0"
        PlaceholderText="Buscar ubicación en el mapa..."
        VerticalAlignment="Center"
        Margin="0,0,8,0" />
    <Button 
        x:Name="SearchButton"
        Grid.Column="1"
        Click="SearchButton_Click"
        Style="{StaticResource AccentButtonStyle}">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <SymbolIcon Symbol="Find" />
            <TextBlock Text="Buscar" />
        </StackPanel>
    </Button>
</Grid>
```

**Características del UI:**
- TextBox con placeholder "Buscar ubicación en el mapa..."
- Botón de búsqueda con icono de lupa
- Diseño flotante sobre el mapa (ZIndex="1000")
- Estilo consistente con el tema de la aplicación

### 2. Manejador de Eventos en C#
**Ubicación**: `Advance Control/Views/Pages/Ubicaciones.xaml.cs`

Se agregó el método `SearchButton_Click` para manejar las búsquedas:

```csharp
/// <summary>
/// Maneja el clic en el botón de búsqueda del mapa
/// </summary>
private async void SearchButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var searchQuery = MapSearchBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await ShowMessageDialogAsync("Búsqueda", "Por favor ingrese una ubicación para buscar");
            return;
        }

        await _loggingService.LogInformationAsync($"Buscando ubicación: {searchQuery}", "Ubicaciones", "SearchButton_Click");

        if (MapWebView?.CoreWebView2 != null)
        {
            // Use proper JavaScript encoding to prevent XSS attacks
            var encodedQuery = System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(searchQuery);
            var script = $"searchLocation('{encodedQuery}');";
            await MapWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
    }
    catch (Exception ex)
    {
        await _loggingService.LogErrorAsync("Error al buscar ubicación", ex, "Ubicaciones", "SearchButton_Click");
        await ShowMessageDialogAsync("Error", "Ocurrió un error al buscar la ubicación");
    }
}
```

**Funcionalidades del Manejador:**
- Valida que el campo de búsqueda no esté vacío
- Registra la búsqueda en los logs del sistema
- Usa `JavaScriptEncoder.Default.Encode` para prevenir ataques XSS de manera segura y completa
- Ejecuta JavaScript en el WebView2 para realizar la búsqueda
- Maneja errores con mensajes informativos al usuario

### 3. Integración con Google Places API
**Ubicación**: `Advance Control/Views/Pages/Ubicaciones.xaml.cs` (método `GenerateMapHtml`)

#### 3.1 Carga de la Librería Places
Se modificó la carga del script de Google Maps para incluir la librería Places:

```javascript
<script src='https://maps.googleapis.com/maps/api/js?key={apiKey}&libraries=places'></script>
```

#### 3.2 Constantes de Configuración
Se agregaron constantes para mejorar la mantenibilidad del código:

```javascript
const SEARCH_MARKER_ICON = 'https://maps.google.com/mapfiles/ms/icons/blue-dot.png';
const EDIT_MARKER_ICON = 'https://maps.google.com/mapfiles/ms/icons/red-dot.png';
const MARKER_ICON_SIZE = 40;
```

#### 3.3 Variable para Marcador de Búsqueda
Se agregó una variable global para rastrear el marcador de búsqueda:

```javascript
let searchMarker = null;
```

#### 3.4 Función de Búsqueda
Se implementó la función `searchLocation` que utiliza el servicio Places de Google Maps:

```javascript
function searchLocation(query) {
    if (!query || query.trim() === '') {
        return;
    }

    const request = {
        query: query,
        fields: ['name', 'geometry', 'formatted_address']
    };

    const service = new google.maps.places.PlacesService(map);
    
    service.findPlaceFromQuery(request, (results, status) => {
        if (status === google.maps.places.PlacesServiceStatus.OK && results && results.length > 0) {
            const place = results[0];
            
            // Remove previous search marker if exists
            if (searchMarker) {
                searchMarker.setMap(null);
            }

            // Center map on the found location
            if (place.geometry && place.geometry.location) {
                map.setCenter(place.geometry.location);
                map.setZoom(15);

                // Add a marker for the search result
                searchMarker = new google.maps.Marker({
                    position: place.geometry.location,
                    map: map,
                    title: place.name,
                    icon: {
                        url: SEARCH_MARKER_ICON,
                        scaledSize: new google.maps.Size(MARKER_ICON_SIZE, MARKER_ICON_SIZE)
                    },
                    animation: google.maps.Animation.DROP
                });

                // Show info window with search result
                const content = `
                    <div style='padding: 8px; min-width: 200px;'>
                        <h3 style='margin: 0 0 8px 0; color: #1a73e8; font-size: 16px;'>${place.name || 'Ubicación encontrada'}</h3>
                        <div style='color: #5f6368; font-size: 14px;'>
                            ${place.formatted_address ? `<p style='margin: 4px 0;'>${place.formatted_address}</p>` : ''}
                        </div>
                    </div>
                `;
                
                infoWindow.setContent(content);
                infoWindow.open(map, searchMarker);
            }
        } else {
            // Show error to user via InfoWindow
            const errorMessages = {
                'ZERO_RESULTS': 'No se encontraron resultados para la búsqueda.',
                'OVER_QUERY_LIMIT': 'Se ha excedido el límite de consultas. Intente más tarde.',
                'REQUEST_DENIED': 'La solicitud fue denegada.',
                'INVALID_REQUEST': 'La solicitud no es válida.',
                'UNKNOWN_ERROR': 'Ocurrió un error desconocido. Intente nuevamente.'
            };
            
            const errorMessage = errorMessages[status] || errorMessages['UNKNOWN_ERROR'];
            
            const errorContent = `
                <div style='padding: 8px; min-width: 200px;'>
                    <h3 style='margin: 0 0 8px 0; color: #d93025; font-size: 16px;'>Error en la búsqueda</h3>
                    <div style='color: #5f6368; font-size: 14px;'>
                        <p style='margin: 4px 0;'>${errorMessage}</p>
                    </div>
                </div>
            `;
            
            infoWindow.setContent(errorContent);
            infoWindow.setPosition(map.getCenter());
            infoWindow.open(map);
            
            console.error('Error en búsqueda de ubicación. Query:', query, 'Status:', status);
        }
    });
}
```

**Características de la Función de Búsqueda:**
- Valida que la consulta no esté vacía
- Utiliza `PlacesService.findPlaceFromQuery` para buscar ubicaciones
- Solicita campos específicos: nombre, geometría y dirección formateada
- Elimina marcadores de búsqueda anteriores antes de crear uno nuevo
- Centra el mapa en la ubicación encontrada con zoom 15
- Crea un marcador azul distintivo usando la constante `SEARCH_MARKER_ICON`
- Aplica animación DROP al marcador para mejor experiencia visual
- Muestra un InfoWindow con información de la ubicación
- **Manejo de errores mejorado**: Muestra mensajes de error amigables al usuario mediante InfoWindow
- Mapea códigos de error de Google Places a mensajes en español
- Posiciona el InfoWindow de error en el centro del mapa
- Registra errores en la consola para debugging
- Los errores son visibles para el usuario, no solo en la consola del navegador

## Flujo de Funcionamiento

1. **Usuario ingresa texto**: El usuario escribe el nombre de una ubicación, dirección o lugar de interés en el campo de búsqueda
2. **Usuario presiona Buscar**: Al hacer clic en el botón, se ejecuta `SearchButton_Click`
3. **Validación**: Se valida que el campo no esté vacío
4. **Codificación segura**: Se codifica la consulta usando `JavaScriptEncoder.Default.Encode` para prevenir XSS
5. **Llamada a JavaScript**: Se ejecuta la función `searchLocation` en el WebView2
6. **Consulta a Google Places**: El API de Places busca la ubicación
7. **Resultado encontrado**: Si se encuentra:
   - Se elimina cualquier marcador de búsqueda anterior
   - Se centra el mapa en la nueva ubicación
   - Se crea un marcador azul distintivo
   - Se muestra un InfoWindow con detalles
8. **Resultado no encontrado**: Si falla:
   - Se muestra un InfoWindow con mensaje de error en español
   - Se mapea el código de error a un mensaje amigable
   - Se registra el error en la consola para debugging

## Consideraciones de Seguridad

1. **Protección XSS mejorada**: Se utiliza `System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode` que es el método recomendado por Microsoft para codificar cadenas antes de insertarlas en JavaScript. Este encoder maneja correctamente todos los caracteres especiales y secuencias peligrosas.
2. **Validación de entrada**: Se valida que el campo no esté vacío antes de procesar
3. **Manejo de errores**: Se capturan y registran excepciones de manera segura
4. **Logging**: Todas las búsquedas se registran en el sistema de logging para auditoría
5. **Constantes**: URLs de iconos y tamaños se definen como constantes para prevenir modificaciones accidentales

## Diferencias con el API de Advance

Esta funcionalidad **NO** interactúa con el API de Advance Control. En su lugar:
- Utiliza directamente el API de Google Places
- No almacena resultados en la base de datos
- No afecta las ubicaciones guardadas en el sistema
- Es solo para navegación y exploración del mapa
- No requiere autenticación adicional más allá de la API Key de Google Maps

## Beneficios de la Implementación

1. **Búsqueda rápida**: Los usuarios pueden encontrar cualquier ubicación sin necesidad de conocer coordenadas exactas
2. **Experiencia mejorada**: Navegación intuitiva del mapa con búsqueda de texto
3. **Integración nativa**: Utiliza el servicio oficial de Google Places
4. **Visual distintivo**: Marcadores azules para diferenciar búsquedas de ubicaciones guardadas
5. **Información contextual**: InfoWindows con detalles de la ubicación encontrada
6. **No invasivo**: No interfiere con las funcionalidades existentes del sistema

## Tipos de Búsquedas Soportadas

El API de Google Places permite buscar:
- **Nombres de lugares**: "Torre Eiffel", "Estadio Azteca"
- **Direcciones**: "Avenida Reforma 123, Ciudad de México"
- **Ciudades**: "Guadalajara, Jalisco"
- **Códigos postales**: "06600"
- **Puntos de interés**: "restaurantes cerca de mí", "gasolineras"
- **Coordenadas**: "19.4326,-99.1332"

## Mantenimiento y Extensiones Futuras

### Posibles Mejoras
1. **Autocompletado**: Agregar sugerencias mientras el usuario escribe
2. **Historial de búsquedas**: Guardar búsquedas recientes del usuario
3. **Búsqueda avanzada**: Filtros por tipo de lugar (restaurantes, hoteles, etc.)
4. **Múltiples resultados**: Mostrar lista de resultados cuando hay varias coincidencias
5. **Integración con ubicaciones**: Botón para guardar ubicación encontrada como nueva ubicación en el sistema

### Consideraciones de Costos
- El API de Google Places tiene límites de uso gratuito
- Monitorear el uso para evitar cargos inesperados
- Considerar implementar caché de búsquedas frecuentes

## Archivos Modificados

1. **Advance Control/Views/Pages/Ubicaciones.xaml**
   - Agregado: Cuadro de búsqueda UI

2. **Advance Control/Views/Pages/Ubicaciones.xaml.cs**
   - Agregado: Método `SearchButton_Click`
   - Modificado: Método `GenerateMapHtml` para incluir librería Places y función de búsqueda

## Testing

### Casos de Prueba Sugeridos
1. Buscar una ciudad conocida (ej: "Ciudad de México")
2. Buscar una dirección específica
3. Buscar un punto de interés (ej: "Zócalo")
4. Buscar con campo vacío (debe mostrar mensaje de error)
5. Buscar ubicación inexistente (debe registrar error en consola)
6. Realizar múltiples búsquedas consecutivas (debe limpiar marcadores anteriores)

### Verificación Visual
- El marcador de búsqueda debe ser azul (diferente a otros marcadores)
- El InfoWindow debe mostrar nombre y dirección
- El mapa debe centrarse en la ubicación encontrada
- La animación DROP debe verse fluida

## Conclusión

La implementación del buscador de Google Maps en la página de Ubicaciones proporciona una herramienta poderosa y fácil de usar para que los usuarios naveguen el mapa sin necesidad de conocer coordenadas exactas. La solución es robusta, segura y no interfiere con las funcionalidades existentes del sistema.
