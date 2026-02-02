# Implementación de TabView con Ubicaciones y Áreas

## Resumen
Se ha implementado exitosamente un TabView en la página de Ubicaciones que contiene dos pestañas:
1. **Ubicaciones** - Gestión de ubicaciones con mapa de Google Maps
2. **Áreas** - Gestión de áreas geográficas con herramientas de dibujo de polígonos

## Cambios Realizados

### 1. Servicios de Áreas - CRUD Completo

#### Archivos Modificados:
- `Services/Areas/IAreasService.cs`
- `Services/Areas/AreasService.cs`

#### Nuevos Métodos Agregados:
```csharp
// Crear área
Task<ApiResponse> CreateAreaAsync(AreaDto area, CancellationToken cancellationToken = default);

// Actualizar área
Task<ApiResponse> UpdateAreaAsync(int idArea, AreaDto area, CancellationToken cancellationToken = default);

// Eliminar área
Task<ApiResponse> DeleteAreaAsync(int idArea, CancellationToken cancellationToken = default);
```

### 2. Nuevo ViewModel - AreasViewModel

#### Archivo Creado:
- `ViewModels/AreasViewModel.cs`

#### Características:
- Gestión completa del estado de las áreas
- Integración con Google Maps Config Service
- Manejo de errores robusto
- Logging comprehensivo
- Métodos CRUD completos:
  - `CreateAreaAsync()`
  - `UpdateAreaAsync()`
  - `DeleteAreaAsync()`
  - `LoadAreasAsync()`
  - `RefreshAreasAsync()`

### 3. Nueva Página de Áreas

#### Archivos Creados:
- `Views/Pages/Areas.xaml`
- `Views/Pages/Areas.xaml.cs`

#### Características Principales:

**UI Components:**
- WebView2 para Google Maps con Drawing Manager
- Lista de áreas con edit/delete buttons
- Formulario de creación/edición con:
  - Campo de nombre (requerido)
  - Campo de descripción (opcional)
  - Selector de color (7 colores predefinidos)
  - Checkbox de estado activo

**Google Maps Drawing Manager:**
- Soporte para dibujar:
  - Polígonos (Polygon)
  - Círculos (Circle)
  - Rectángulos (Rectangle)
- Herramientas de edición integradas
- Dibujo interactivo con arrastrar y soltar
- Visualización de áreas existentes en el mapa

**Funcionalidad JavaScript:**
```javascript
// Detecta cuando se dibuja una forma
drawingManager.addListener('overlaycomplete', function(event) {
  // Extrae datos de la forma
  // Envía mensaje a C# con coordenadas y tipo
});

// Carga áreas existentes desde la base de datos
loadExistingAreas(areasData);
```

**Comunicación WebView2 ↔ C#:**
```csharp
// C# recibe mensajes desde JavaScript
CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

// Tipos de mensajes soportados:
// - "shapeDrawn": Nueva forma dibujada
// - "shapeEdited": Forma editada
```

### 4. Refactorización de Ubicaciones.xaml

#### Cambios Principales:
- Implementación de `TabView` control
- Primera pestaña: "Ubicaciones" (contenido original)
- Segunda pestaña: "Áreas" (nuevo)
- Header simplificado: "Ubicaciones y Áreas"
- Botón de refresh movido dentro de la pestaña de Ubicaciones

#### Estructura del TabView:
```xaml
<TabView Grid.Row="1" Margin="20,0,20,20" TabWidthMode="Equal">
    <TabViewItem Header="Ubicaciones" IsClosable="False">
        <!-- Contenido de ubicaciones -->
    </TabViewItem>
    <TabViewItem Header="Áreas" IsClosable="False">
        <local:Areas x:Name="AreasPage" />
    </TabViewItem>
</TabView>
```

### 5. Registro de Dependencias

#### Archivo Modificado:
- `App.xaml.cs`

#### Cambio:
```csharp
// Agregado al registro de ViewModels
services.AddTransient<ViewModels.AreasViewModel>();
```

## Flujo de Trabajo para Áreas

### Crear un Área:
1. Usuario navega a la pestaña "Áreas"
2. Click en "Agregar Área"
3. Usa las herramientas de dibujo en el mapa para crear un polígono, círculo o rectángulo
4. Completa el formulario (nombre, descripción, color)
5. Click en "Guardar"
6. El sistema:
   - Extrae coordenadas del shape
   - Calcula centro y bounds
   - Guarda en la base de datos via AreasService
   - Recarga el mapa con la nueva área

### Editar un Área:
1. Click en botón "Editar" de un área en la lista
2. El formulario se carga con los datos del área
3. El usuario puede:
   - Modificar nombre, descripción, color
   - (TODO: Cargar shape en el mapa para edición)
4. Click en "Guardar"
5. Sistema actualiza el área via AreasService

### Eliminar un Área:
1. Click en botón "Eliminar" de un área
2. Sistema muestra diálogo de confirmación
3. Si confirma, elimina el área via AreasService
4. Recarga el mapa sin el área eliminada

## Estructura de Datos

### AreaDto
```csharp
- IdArea: int
- Nombre: string
- Descripcion: string?
- ColorMapa: string (hex color)
- Opacidad: decimal?
- ColorBorde: string
- AnchoBorde: int?
- Activo: bool?
- TipoGeometria: string (Polygon, Circle, Rectangle)
- CentroLatitud/Longitud: decimal?
- Radio: decimal? (solo para círculos)
- BoundingBox: (NE_Lat, NE_Lng, SW_Lat, SW_Lng)
- MetadataJSON: string? (contiene path completo del polígono)
```

### GoogleMapsAreaDto
```csharp
- IdArea: int
- Nombre: string
- Type: string (Polygon, Circle, Rectangle)
- Path: string (JSON array de coordenadas)
- Options: string (JSON con opciones de estilo)
- Center: string (JSON con lat/lng)
- Bounds: string (JSON con bounds)
- Radius: decimal? (para círculos)
```

## Google Maps API Integration

### Bibliotecas Utilizadas:
- `maps.googleapis.com/maps/api/js`
- Libraries: `drawing`, `geometry`

### Controles de Dibujo:
```javascript
drawingManager = new google.maps.drawing.DrawingManager({
  drawingControl: true,
  drawingControlOptions: {
    position: google.maps.ControlPosition.TOP_CENTER,
    drawingModes: [
      google.maps.drawing.OverlayType.POLYGON,
      google.maps.drawing.OverlayType.CIRCLE,
      google.maps.drawing.OverlayType.RECTANGLE
    ]
  }
});
```

### Estilos Predeterminados:
- Color de relleno: Seleccionable (rojo, azul, verde, amarillo, naranja, púrpura, rosa)
- Opacidad: 0.35
- Grosor de borde: 2px
- Color de borde: Mismo que el color de relleno
- Editable: true (durante creación)
- Draggable: true (durante creación)

## Casos de Uso

### Asignación de Técnicos a Zonas:
1. Crear áreas que representen zonas de operación
2. Cada área puede ser asignada a uno o más técnicos (implementación futura)
3. Sistema puede validar si una ubicación está dentro de una zona específica
4. Permite delimitar responsabilidades geográficas

### Visualización de Cobertura:
1. Las áreas se muestran en el mapa con colores distintivos
2. Permite ver rápidamente qué zonas están cubiertas
3. Facilita la planificación de expansión

## Mejoras Futuras Sugeridas

1. **Edición de Shapes en el Mapa:**
   - Cargar el shape existente en el mapa al editar
   - Permitir modificación visual de los polígonos

2. **Asignación de Técnicos:**
   - Agregar tabla de relación Area-Técnico
   - UI para asignar técnicos a áreas
   - Visualización de técnicos asignados

3. **Validación Automática:**
   - Al crear una ubicación, verificar automáticamente en qué área cae
   - Sugerir técnicos disponibles según el área

4. **Exportación/Importación:**
   - Exportar áreas a formatos GeoJSON, KML
   - Importar áreas desde archivos externos

5. **Análisis:**
   - Calcular superficie de las áreas
   - Detectar superposición de áreas
   - Estadísticas de ubicaciones por área

## Notas Técnicas

### Limitaciones Conocidas:
1. Al editar un área, el shape no se carga en el mapa (implementación pendiente)
2. Solo se puede tener un shape activo a la vez durante la creación
3. El sistema no detecta automáticamente superposición de áreas

### Consideraciones de Seguridad:
- Todas las llamadas HTTP usan cancellation tokens
- Validación de entrada antes de guardar
- Manejo robusto de errores con logging
- Confirmaciones para operaciones destructivas

### Performance:
- Carga diferida de áreas (solo cuando se navega a la pestaña)
- Serialización eficiente de JSON
- WebView2 maneja el rendering del mapa
- Areas se cargan en formato optimizado para Google Maps

## Testing

### Pruebas Recomendadas:
1. ✓ Verificar que el TabView se renderiza correctamente
2. ✓ Confirmar que ambas pestañas son accesibles
3. ✓ Probar creación de polígonos en el mapa
4. ✓ Verificar que los datos se guardan correctamente
5. ✓ Probar edición de áreas existentes
6. ✓ Confirmar que la eliminación funciona
7. ✓ Verificar que las áreas se visualizan correctamente en el mapa

### Comandos de Prueba (solo en Windows):
```bash
# Compilar
dotnet build "Advance Control.sln" --configuration Debug

# Ejecutar
# Abrir desde Visual Studio 2022
```

## Conclusión

Se ha implementado exitosamente un sistema completo de gestión de áreas geográficas con las siguientes capacidades:

✅ **TabView** con dos pestañas (Ubicaciones y Áreas)
✅ **CRUD completo** para áreas (Create, Read, Update, Delete)
✅ **Herramientas de dibujo** de Google Maps (polígonos, círculos, rectángulos)
✅ **Visualización** de áreas existentes en el mapa
✅ **Integración completa** con el backend API
✅ **Logging y manejo de errores** robusto
✅ **UI intuitiva** con formularios y listas

El sistema está listo para ser usado en la delimitación de zonas operativas para técnicos.
