# Implementación del Visor de Google Maps en Ubicaciones

## Resumen
Se ha implementado un visor de Google Maps completamente funcional en la página de **Ubicaciones** del sistema AdvanceControl. Esta implementación permite visualizar áreas geográficas (polígonos, círculos, rectángulos y polilíneas) en un mapa interactivo de Google Maps.

## Arquitectura de la Solución

### 1. Modelos de Datos (Models/)
Se crearon 5 modelos DTOs para manejar los datos del API:

#### GoogleMapsConfigDto.cs
- **ApiKey**: Clave de API de Google Maps
- **DefaultCenter**: Coordenadas del centro predeterminado (formato: "latitud,longitud")
- **DefaultZoom**: Nivel de zoom predeterminado (valor por defecto: 15)

#### AreaDto.cs
Modelo completo de área geográfica con 24 propiedades incluyendo:
- Identificación: IdArea, Nombre, Descripcion
- Visualización: ColorMapa, Opacidad, ColorBorde, AnchoBorde
- Geometría: TipoGeometria, CentroLatitud, CentroLongitud, Radio
- Bounding Box: BoundingBoxNE_Lat, BoundingBoxNE_Lng, BoundingBoxSW_Lat, BoundingBoxSW_Lng
- Etiquetas: EtiquetaMostrar, EtiquetaTexto
- Metadatos: FechaCreacion, FechaModificacion, UsuarioCreacion, UsuarioModificacion

#### GoogleMapsAreaDto.cs
Modelo optimizado para renderizado en Google Maps con formato JSON serializado:
- **IdArea**: Identificador único
- **Nombre**: Nombre del área
- **Type**: Tipo de geometría (Polygon, Circle, Rectangle, Polyline)
- **Path**: JSON con array de coordenadas
- **Options**: JSON con opciones de estilo (fillColor, fillOpacity, strokeColor, strokeWeight)
- **Center**: JSON con punto central
- **Bounds**: JSON con bounding box
- **Radius**: Radio en metros (solo para círculos)

#### CoordinateDto.cs
- **Lat**: Latitud (decimal)
- **Lng**: Longitud (decimal)

#### AreaValidationResultDto.cs
Resultado de validación de punto en área:
- **IdArea**: Identificador del área
- **Nombre**: Nombre del área
- **TipoGeometria**: Tipo de geometría
- **DentroDelArea**: Booleano indicando si el punto está dentro

### 2. Servicios (Services/)

#### GoogleMapsConfigService
**Ubicación**: `Services/GoogleMaps/GoogleMapsConfigService.cs`

**Métodos implementados**:
- `GetApiKeyAsync()`: Obtiene solo la clave de API
- `GetConfigAsync()`: Obtiene configuración completa (API key, centro, zoom)

**Endpoints consumidos**:
- `GET /api/GoogleMapsConfig/api-key`
- `GET /api/GoogleMapsConfig`

#### AreasService
**Ubicación**: `Services/Areas/AreasService.cs`

**Métodos implementados**:
- `GetAreasAsync()`: Obtiene áreas con filtros opcionales (idArea, nombre, activo, tipoGeometria)
- `GetAreasForGoogleMapsAsync()`: Obtiene áreas en formato optimizado para Google Maps
- `ValidatePointAsync()`: Valida si un punto está dentro de áreas

**Endpoints consumidos**:
- `GET /api/Areas`
- `GET /api/Areas/googlemaps`
- `GET /api/Areas/validate-point`

**Características**:
- Manejo robusto de errores con logging
- Soporte para cancelación de operaciones (CancellationToken)
- Construcción dinámica de query strings
- Integración con AuthenticatedHttpHandler para JWT Bearer tokens

### 3. ViewModel (ViewModels/)

#### UbicacionesViewModel
**Ubicación**: `ViewModels/UbicacionesViewModel.cs`

**Propiedades**:
- `MapsConfig`: Configuración de Google Maps
- `Areas`: Colección observable de áreas para el mapa
- `IsLoading`: Indicador de carga
- `ErrorMessage`: Mensaje de error
- `HasError`: Booleano calculado para mostrar errores
- `IsMapInitialized`: Indica si el mapa está inicializado

**Métodos**:
- `InitializeAsync()`: Inicializa mapa y carga configuración y áreas
- `LoadConfigurationAsync()`: Carga configuración de Google Maps
- `LoadAreasAsync()`: Carga áreas activas desde el API
- `ValidatePointAsync()`: Valida coordenadas contra áreas
- `RefreshAreasAsync()`: Recarga áreas del mapa

**Patrón MVVM**:
- Hereda de ViewModelBase para INotifyPropertyChanged
- Usa ObservableCollection para actualización automática de UI
- Manejo de estado (IsLoading, ErrorMessage)
- Logging extensivo de operaciones

### 4. Vista (Views/Pages/)

#### Ubicaciones.xaml
**Características de la UI**:
- Header con título y botón de refresh
- Indicador de carga (ProgressRing)
- InfoBar para mostrar errores
- WebView2 para renderizar Google Maps
- Bindings x:Bind para mejor rendimiento
- Uso de BooleanToVisibilityConverter para visibilidad condicional

**Estructura**:
```xml
<Grid Padding="24">
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto" />  <!-- Header -->
    <RowDefinition Height="*" />     <!-- Content -->
  </Grid.RowDefinitions>
  
  <!-- Header con título y botón refresh -->
  <!-- StackPanel de carga (visible cuando IsLoading=true) -->
  <!-- InfoBar de error (visible cuando HasError=true) -->
  <!-- WebView2 (visible cuando IsMapInitialized=true) -->
</Grid>
```

#### Ubicaciones.xaml.cs
**Funcionalidades**:
- Resolución de ViewModel desde DI (Dependency Injection)
- Inicialización automática al navegar (OnNavigatedTo)
- Generación dinámica de HTML para Google Maps
- Manejo de eventos del WebView2
- Botón de refresh para recargar áreas

**Método GenerateMapHtml()**:
Genera HTML completo con:
- Google Maps API script
- Inicialización del mapa con coordenadas y zoom
- Renderizado de áreas según tipo:
  - **Polygon**: google.maps.Polygon con paths
  - **Circle**: google.maps.Circle con center y radius
  - **Rectangle**: google.maps.Rectangle con bounds
  - **Polyline**: google.maps.Polyline con path
- Event listeners para clicks en áreas
- Alert modal con información del área al hacer click

### 5. Registro en Dependency Injection (App.xaml.cs)

Se agregaron los siguientes registros:

```csharp
// Services
services.AddHttpClient<IGoogleMapsConfigService, GoogleMapsConfigService>(...)
    .AddHttpMessageHandler<AuthenticatedHttpHandler>();

services.AddHttpClient<IAreasService, AreasService>(...)
    .AddHttpMessageHandler<AuthenticatedHttpHandler>();

// ViewModel
services.AddTransient<ViewModels.UbicacionesViewModel>();
```

**Configuración**:
- HttpClient con BaseAddress desde IApiEndpointProvider
- Timeout configurable según modo desarrollo
- AuthenticatedHttpHandler para agregar JWT token automáticamente
- Soporte para refresh token automático en caso de 401

## Flujo de Trabajo

### Inicialización del Mapa
1. Usuario navega a página Ubicaciones
2. `OnNavigatedTo()` se ejecuta
3. `ViewModel.InitializeAsync()` se llama:
   - Carga configuración (API key, centro, zoom)
   - Carga áreas activas
4. `LoadMapAsync()` genera HTML con Google Maps
5. HTML se carga en WebView2
6. JavaScript inicializa mapa y renderiza áreas

### Interacción del Usuario
1. **Click en área**: Muestra alert con información (nombre, tipo, ID)
2. **Botón Refresh**: Recarga áreas desde API y re-renderiza mapa
3. **Error handling**: Muestra InfoBar con mensaje de error

## Tipos de Geometría Soportados

### 1. Polygon (Polígono)
- Renderizado: `google.maps.Polygon`
- Datos requeridos: Array de coordenadas (path)
- Opciones de estilo: fillColor, fillOpacity, strokeColor, strokeWeight

### 2. Circle (Círculo)
- Renderizado: `google.maps.Circle`
- Datos requeridos: Centro (lat, lng) y radio (metros)
- Opciones de estilo: fillColor, fillOpacity, strokeColor, strokeWeight

### 3. Rectangle (Rectángulo)
- Renderizado: `google.maps.Rectangle`
- Datos requeridos: Bounds (north, east, south, west)
- Opciones de estilo: fillColor, fillOpacity, strokeColor, strokeWeight

### 4. Polyline (Polilínea)
- Renderizado: `google.maps.Polyline`
- Datos requeridos: Array de coordenadas (path)
- Opciones de estilo: strokeColor, strokeWeight

## Seguridad

### Autenticación
- Todas las peticiones requieren JWT Bearer Token
- Token se agrega automáticamente via AuthenticatedHttpHandler
- Refresh automático de token en caso de expiración (401)

### Manejo de API Key
- API Key de Google Maps se obtiene del servidor (no hardcodeada)
- Se carga dinámicamente al inicializar el mapa
- Se pasa de forma segura al HTML generado

## Logging

Todos los componentes implementan logging extensivo:
- Inicialización y carga de datos
- Errores de red y de deserialización
- Navegación del WebView2
- Acciones del usuario (refresh, etc.)

## Manejo de Errores

### Niveles de Error
1. **Nivel de Servicio**: Try-catch con logging y throw de InvalidOperationException
2. **Nivel de ViewModel**: Try-catch con logging y asignación a ErrorMessage
3. **Nivel de Vista**: Try-catch con logging para eventos UI

### Visualización de Errores
- InfoBar en la parte superior del contenido
- Mensaje descriptivo al usuario
- ErrorMessage se limpia al recargar exitosamente

## Características Técnicas

### Performance
- Uso de x:Bind en lugar de Binding para mejor rendimiento
- Compilación one-way por defecto
- ObservableCollection para actualización incremental
- Lazy loading de áreas

### Responsividad
- CancellationToken en todas las operaciones async
- IsLoading para feedback visual
- ProgressRing durante operaciones largas

### Extensibilidad
- Interfaces bien definidas (IGoogleMapsConfigService, IAreasService)
- Separación clara de responsabilidades (MVVM)
- Fácil agregar nuevos tipos de geometría
- Posibilidad de agregar herramientas de dibujo en el futuro

## Mejoras Futuras Posibles

1. **Herramientas de Dibujo**:
   - Agregar herramientas para crear nuevas áreas
   - Implementar POST /api/Areas desde la UI
   - Edición de áreas existentes

2. **Búsqueda y Filtros**:
   - Filtro por nombre de área
   - Filtro por tipo de geometría
   - Búsqueda de direcciones

3. **Información Detallada**:
   - InfoWindow en lugar de alert
   - Mostrar propiedades completas del área
   - Historial de cambios

4. **Validación de Ubicación**:
   - Implementar UI para ValidatePointAsync
   - Click en mapa para validar punto
   - Highlight de áreas que contienen el punto

5. **Exportación**:
   - Exportar áreas como GeoJSON
   - Importar áreas desde archivo
   - Imprimir mapa

## Testing

Para probar la implementación:

1. **Configurar API**:
   - Asegurar que el servidor API esté corriendo
   - Verificar que `/api/GoogleMapsConfig` retorna configuración válida
   - Verificar que `/api/Areas/googlemaps?activo=true` retorna áreas

2. **Configurar Google Maps API Key**:
   - API Key debe estar configurada en el servidor
   - Debe tener permisos para Maps JavaScript API

3. **Navegar a Ubicaciones**:
   - Login en la aplicación
   - Navegar a página "Ubicaciones"
   - Verificar que el mapa se carga correctamente
   - Verificar que las áreas se muestran

4. **Interacción**:
   - Click en un área para ver información
   - Click en botón Refresh para recargar
   - Verificar manejo de errores (desconectar red, etc.)

## Archivos Creados/Modificados

### Archivos Nuevos (13)
1. `Models/GoogleMapsConfigDto.cs`
2. `Models/AreaDto.cs`
3. `Models/GoogleMapsAreaDto.cs`
4. `Models/CoordinateDto.cs`
5. `Models/AreaValidationResultDto.cs`
6. `Services/GoogleMaps/IGoogleMapsConfigService.cs`
7. `Services/GoogleMaps/GoogleMapsConfigService.cs`
8. `Services/Areas/IAreasService.cs`
9. `Services/Areas/AreasService.cs`
10. `ViewModels/UbicacionesViewModel.cs`
11. `Views/Pages/Ubicaciones.xaml` (actualizado)
12. `Views/Pages/Ubicaciones.xaml.cs` (actualizado)
13. `GOOGLE_MAPS_IMPLEMENTATION.md` (este archivo)

### Archivos Modificados (1)
1. `App.xaml.cs` - Agregado registro de servicios y ViewModel

## Conclusión

La implementación del visor de Google Maps en la página Ubicaciones está completa y sigue las mejores prácticas de:
- Arquitectura MVVM
- Dependency Injection
- Manejo de errores robusto
- Logging extensivo
- Seguridad (JWT tokens)
- Performance (x:Bind, ObservableCollection)

El sistema está listo para:
- Visualizar áreas geográficas de cualquier tipo
- Interactuar con las áreas en el mapa
- Refrescar datos desde el servidor
- Expandirse con nuevas funcionalidades

La solución es mantenible, testeable y extensible para futuras mejoras.
