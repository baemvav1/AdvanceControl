# Reorganización del Layout de la Página Ubicaciones

## Resumen de Cambios

Se ha reorganizado exitosamente la página de Ubicaciones según los requerimientos especificados, dividiendo el layout en dos renglones principales:

### Estructura Anterior
- Renglón 0: Header simple
- Renglón 1: TabView con dos tabs, cada uno conteniendo su propio mapa y formularios

### Nueva Estructura

#### Renglón 0: Header y Buscador
- **StackPanel Vertical** que contiene:
  1. **Header de página**: TextBlock con el texto "Ubicaciones" (32pt, Bold)
  2. **Buscador en mapa**: Grid con TextBox y Button para buscar ubicaciones o áreas en el mapa

#### Renglón 1: Mapa y Formularios
- **Dos columnas**:
  1. **Columna Izquierda (*)**: Mapa compartido (WebView2)
     - Muestra el mapa de Google Maps
     - Cambia de contenido según la pestaña activa
  2. **Columna Derecha (400px)**: TabView con formularios
     - **Tab "Ubicaciones"**: Lista y formulario de ubicaciones
     - **Tab "Áreas"**: Lista y formulario de áreas

## Cambios Detallados

### 1. Ubicaciones.xaml

#### Cambios en la Estructura
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />  <!-- Header y Búsqueda -->
        <RowDefinition Height="*" />      <!-- Mapa y TabView -->
    </Grid.RowDefinitions>
    
    <!-- Row 0: Header y Buscador -->
    <StackPanel Grid.Row="0" Spacing="12">
        <TextBlock Text="Ubicaciones" FontSize="32" FontWeight="Bold" />
        <Grid> <!-- Buscador --> </Grid>
    </StackPanel>
    
    <!-- Row 1: Mapa y TabView -->
    <Grid Grid.Row="1">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />      <!-- Mapa -->
            <ColumnDefinition Width="400" />    <!-- TabView -->
        </Grid.ColumnDefinitions>
        
        <!-- Mapa compartido -->
        <Grid Grid.Column="0">
            <WebView2 x:Name="MapWebView" />
        </Grid>
        
        <!-- TabView con formularios -->
        <TabView Grid.Column="1">
            <TabViewItem Header="Ubicaciones">...</TabViewItem>
            <TabViewItem Header="Áreas">
                <local:Areas x:Name="AreasPage" />
            </TabViewItem>
        </TabView>
    </Grid>
</Grid>
```

#### Beneficios
- Buscador siempre visible en la parte superior
- Mapa compartido entre ambas pestañas (eficiencia de recursos)
- TabView más compacto enfocado en los formularios

### 2. Areas.xaml

Se simplificó completamente eliminando:
- Header propio (ahora está en la página padre)
- Mapa propio (ahora usa el mapa compartido de Ubicaciones)
- Grid con múltiples columnas

Ahora contiene solo:
```xml
<ScrollViewer>
    <Grid>
        <!-- Lista de áreas -->
        <!-- Formulario de áreas -->
    </Grid>
</ScrollViewer>
```

### 3. Ubicaciones.xaml.cs

#### Métodos Agregados

##### `LoadAreasMapAsync()`
```csharp
private async Task LoadAreasMapAsync()
```
- Carga el mapa específico para Áreas con herramientas de dibujo
- Obtiene datos del AreasViewModel
- Genera HTML con Google Maps Drawing Manager

##### `GenerateAreasMapHtml()`
```csharp
private string GenerateAreasMapHtml(string apiKey, string centerLat, string centerLng, int zoom, string areasJson)
```
- Genera el HTML completo para el mapa de Áreas
- Incluye:
  - Google Maps API con libraries: `drawing`, `geometry`
  - Drawing Manager para polígonos, círculos y rectángulos
  - Funciones JavaScript para extraer datos de formas
  - Event listeners para `shapeDrawn` y `shapeEdited`
  - Carga de áreas existentes desde la base de datos

##### `TabView_SelectionChanged()`
```csharp
private async void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
```
- Detecta cambio de pestaña
- Recarga el mapa según la pestaña activa:
  - **Ubicaciones**: Llama a `LoadMapAsync()` (mapa con marcadores)
  - **Áreas**: Llama a `LoadAreasMapAsync()` (mapa con herramientas de dibujo)

##### Métodos Auxiliares
```csharp
private object? ParseAreaPath(AreaDto area)
private object? ParseAreaCenter(AreaDto area)
```
- Extraen información de geometría desde AreaDto
- Convierten JSON almacenado en objetos serializables

#### Actualización de `CoreWebView2_WebMessageReceived()`
```csharp
else if (messageType == "shapeDrawn" || messageType == "shapeEdited")
{
    // Reenviar mensajes de formas a la página de Áreas
    if (AreasPage != null)
    {
        await AreasPage.HandleShapeMessageAsync(jsonDoc);
    }
}
```

### 4. Areas.xaml.cs

#### Métodos Agregados

##### `HandleShapeMessageAsync()`
```csharp
public async Task HandleShapeMessageAsync(Dictionary<string, JsonElement> jsonDoc)
```
- Método público llamado por Ubicaciones cuando se reciben mensajes del mapa
- Procesa mensajes `shapeDrawn` y `shapeEdited`
- Extrae y almacena datos de formas: tipo, path, centro, radio, bounds

#### Métodos Comentados (Ya No Usados)
- `CoreWebView2_WebMessageReceived()` - Ahora manejado por Ubicaciones
- `OnNavigatedTo()` - Ya no necesita cargar el mapa
- `InitializeMapAsync()` - El mapa lo inicializa Ubicaciones
- `CreateMapHtml()` - HTML generado por Ubicaciones
- `MapWebView_NavigationCompleted()` - Ya no tiene WebView2 propio

#### Actualizaciones
- `Areas_Loaded()`: Ya no configura WebView2, solo inicializa el ViewModel
- `RefreshButton_Click()`: Solo refresca datos, no recarga el mapa
- Línea comentada en `SaveButton_Click()`: Limpieza de formas pendiente de implementar

## Funcionalidad del Mapa Compartido

### Mapa de Ubicaciones (Tab "Ubicaciones")
- Muestra marcadores rojos para cada ubicación
- Al hacer clic, coloca un marcador rojo draggable
- Reverse geocoding para obtener dirección
- Info windows con detalles de ubicación
- Muestra áreas de fondo (si existen)

### Mapa de Áreas (Tab "Áreas")
- Drawing Manager con tres modos:
  1. **Polígono**: Dibujar formas personalizadas
  2. **Círculo**: Dibujar áreas circulares
  3. **Rectángulo**: Dibujar áreas rectangulares
- Formas editables y draggables durante creación
- Visualización de áreas existentes con sus colores
- Envía datos de formas a C# mediante WebView2 messages

## Flujo de Trabajo

### Al Cargar la Página
1. `OnNavigatedTo()` en Ubicaciones se ejecuta
2. Se inicializa el ViewModel de Ubicaciones
3. Se carga el mapa de Ubicaciones por defecto
4. TabView muestra la pestaña "Ubicaciones" seleccionada

### Al Cambiar a Tab "Áreas"
1. Se dispara `TabView_SelectionChanged()`
2. Se detecta que la pestaña es "Áreas"
3. Se llama a `LoadAreasMapAsync()`
4. Se carga el AreasViewModel si es necesario
5. Se genera nuevo HTML con Drawing Manager
6. WebView2 navega al nuevo HTML
7. Mapa se reinicializa con herramientas de dibujo

### Al Dibujar una Forma
1. Usuario dibuja polígono/círculo/rectángulo
2. JavaScript extrae datos de la forma
3. Se envía mensaje via `window.chrome.webview.postMessage()`
4. `CoreWebView2_WebMessageReceived()` en Ubicaciones recibe el mensaje
5. Se reenvía a `HandleShapeMessageAsync()` en Areas
6. Areas almacena los datos en campos privados
7. Usuario completa el formulario y guarda

### Al Buscar en el Mapa
1. Usuario ingresa texto en el buscador del Row 0
2. Presiona botón "Buscar"
3. Se ejecuta geocoding via Google Maps API
4. Mapa se centra en la ubicación encontrada
5. Funciona para ambas pestañas (Ubicaciones y Áreas)

## Consideraciones Técnicas

### Ventajas
- **Un solo WebView2**: Mejor rendimiento y uso de memoria
- **Código centralizado**: Map loading logic en un solo lugar
- **Búsqueda compartida**: Funciona para ambos contextos
- **Layout consistente**: Header y buscador siempre visibles
- **Separación de responsabilidades**: Ubicaciones maneja el mapa, Areas maneja su lógica de negocio

### Limitaciones Conocidas
1. **Limpieza de formas**: `clearCurrentShape()` no se ejecuta desde Areas después de guardar (comentado)
2. **Recarga de mapa**: RefreshButton en Areas no recarga el mapa automáticamente
3. **Sincronización**: Cambios en Areas no actualizan automáticamente el mapa de Ubicaciones

### Mejoras Futuras Sugeridas
1. **Método público en Ubicaciones** para que Areas pueda solicitar recarga del mapa
2. **Evento personalizado** cuando Areas guarda/elimina una forma
3. **Estado de tab actual** para que búsqueda sepa en qué contexto está
4. **Cache de HTML** para evitar regenerar al cambiar de tab frecuentemente
5. **Transiciones suaves** entre los dos tipos de mapa

## Testing Recomendado

### Pruebas de UI
- [ ] Verificar que header "Ubicaciones" se muestra correctamente
- [ ] Verificar que buscador está siempre visible
- [ ] Verificar que mapa ocupa toda la columna izquierda
- [ ] Verificar que TabView está en columna derecha con ancho fijo
- [ ] Verificar tamaños responsive

### Pruebas Funcionales - Tab Ubicaciones
- [ ] Cargar página y verificar mapa de ubicaciones se muestra
- [ ] Hacer clic en mapa y verificar marcador rojo aparece
- [ ] Crear nueva ubicación y verificar se guarda
- [ ] Editar ubicación existente
- [ ] Eliminar ubicación
- [ ] Buscar ubicación en el buscador

### Pruebas Funcionales - Tab Áreas
- [ ] Cambiar a tab "Áreas" y verificar mapa recarga con herramientas
- [ ] Dibujar un polígono y verificar se captura
- [ ] Dibujar un círculo y verificar se captura
- [ ] Dibujar un rectángulo y verificar se captura
- [ ] Completar formulario y guardar área
- [ ] Editar área existente
- [ ] Eliminar área
- [ ] Buscar área en el buscador

### Pruebas de Integración
- [ ] Cambiar entre tabs múltiples veces
- [ ] Verificar que no hay memory leaks
- [ ] Verificar que WebView2 messages se procesan correctamente
- [ ] Verificar que datos persisten en la base de datos

## Archivos Modificados

1. **Advance Control/Views/Pages/Ubicaciones.xaml**
   - Reorganización completa del layout
   - Header y buscador en Row 0
   - Mapa y TabView en Row 1

2. **Advance Control/Views/Pages/Ubicaciones.xaml.cs**
   - `LoadAreasMapAsync()` agregado
   - `GenerateAreasMapHtml()` agregado
   - `TabView_SelectionChanged()` agregado
   - `ParseAreaPath()` y `ParseAreaCenter()` agregados
   - `CoreWebView2_WebMessageReceived()` actualizado

3. **Advance Control/Views/Pages/Areas.xaml**
   - Eliminado header y mapa
   - Solo contiene lista y formulario

4. **Advance Control/Views/Pages/Areas.xaml.cs**
   - `HandleShapeMessageAsync()` agregado
   - Referencias a MapWebView comentadas
   - Métodos de mapa comentados
   - `Areas_Loaded()` simplificado
   - `RefreshButton_Click()` actualizado

## Conclusión

La reorganización de la página Ubicaciones ha sido completada exitosamente siguiendo los requerimientos especificados:

✅ **Renglón 0**: Header + Buscador en StackPanel vertical
✅ **Renglón 1**: Dos columnas (Mapa izquierda, TabView derecha)
✅ **Mapa compartido**: Se recarga al cambiar de tab
✅ **Tabs**: Ubicaciones y Áreas con sus formularios

La implementación proporciona una experiencia de usuario más coherente y eficiente, con un mapa compartido que se adapta al contexto de la pestaña activa.
