# Funcionalidad de Marcador en Mapa para Auto-rellenar Ubicaciones

## Resumen
Se ha implementado una funcionalidad que permite a los usuarios colocar un marcador rojo en el mapa de Google Maps para auto-rellenar automáticamente los campos de ubicación al agregar o editar ubicaciones.

## Características Principales

### 1. Marcador Draggable (Arrastrable)
- **Color**: Marcador rojo (red pin) distintivo de los marcadores de ubicaciones guardadas
- **Tamaño**: 40x40 píxeles para fácil identificación
- **Animación**: Efecto de caída (DROP) al colocarse
- **Arrastrable**: El usuario puede arrastrar el marcador para ajustar la posición
- **Visibilidad**: Solo aparece cuando el formulario de agregar/editar está visible

### 2. Colocación del Marcador
- **Modo Agregar**: Cuando el usuario hace clic en "Agregar Ubicación", el mapa se activa para recibir clics
- **Modo Editar**: Al editar una ubicación existente, el marcador se coloca automáticamente en las coordenadas guardadas
- **Clic en Mapa**: El usuario puede hacer clic en cualquier parte del mapa para colocar el marcador
- **Reposicionamiento**: Hacer clic en una nueva ubicación mueve el marcador a esa posición

### 3. Auto-rellenado de Campos

#### Campos Actualizados Automáticamente:
1. **Latitud**: Se actualiza con precisión de 6 decimales
2. **Longitud**: Se actualiza con precisión de 6 decimales
3. **Dirección Completa**: Se obtiene mediante reverse geocoding de Google Maps

#### Proceso de Actualización:
1. Usuario coloca/arrastra el marcador
2. JavaScript captura las coordenadas
3. Se ejecuta reverse geocoding para obtener la dirección
4. Los datos se envían a C# mediante WebView2 messaging
5. Los campos del formulario se actualizan automáticamente

### 4. Reverse Geocoding
- **API Utilizada**: Google Maps Geocoding API
- **Información Obtenida**:
  - Dirección formateada completa
  - Ciudad (locality)
  - Estado (administrative_area_level_1)
  - País (country)
- **Manejo de Errores**: Si el geocoding falla, solo se actualizan las coordenadas

## Flujo de Trabajo del Usuario

### Agregar Nueva Ubicación:
1. Usuario hace clic en "Agregar Ubicación"
2. Se muestra el formulario con un InfoBar informativo:
   > "Haz clic en el mapa para colocar un marcador rojo y rellenar automáticamente las coordenadas y dirección."
3. Usuario hace clic en el mapa donde desea la ubicación
4. Aparece un marcador rojo en esa posición
5. Los campos de Latitud, Longitud y Dirección se rellenan automáticamente
6. Usuario puede:
   - Arrastrar el marcador para ajustar la posición
   - Editar manualmente cualquier campo
   - Agregar información adicional (nombre, descripción, etc.)
7. Usuario hace clic en "Guardar" para crear la ubicación

### Editar Ubicación Existente:
1. Usuario hace clic en el botón "Editar" de una ubicación
2. Se muestra el formulario con los datos existentes
3. Si la ubicación tiene coordenadas, el marcador rojo se coloca automáticamente
4. El mapa se centra en las coordenadas de la ubicación
5. Usuario puede:
   - Arrastrar el marcador a una nueva posición
   - Hacer clic en el mapa para mover el marcador
   - Editar manualmente cualquier campo
6. Usuario hace clic en "Guardar" para actualizar la ubicación

### Cancelar Operación:
1. Usuario hace clic en "Cancelar"
2. El formulario se oculta
3. El marcador rojo desaparece del mapa
4. El mapa vuelve a mostrar solo las ubicaciones guardadas

## Implementación Técnica

### Arquitectura de Comunicación

#### JavaScript (Frontend)
- **Variables globales**:
  - `editMarker`: Referencia al marcador rojo
  - `geocoder`: Instancia del servicio de geocoding
  - `isFormVisible`: Estado de visibilidad del formulario

- **Funciones principales**:
  - `placeMarker(location)`: Crea/actualiza el marcador rojo
  - `updateFormWithLocation(location)`: Actualiza formulario con coordenadas y dirección
  - `setFormVisibility(visible)`: Controla si el mapa acepta clics
  - `loadExistingMarker(lat, lng)`: Carga marcador de ubicación existente

#### C# (Backend)
- **Handlers de eventos**:
  - `CoreWebView2_WebMessageReceived`: Recibe mensajes de JavaScript
  - `Ubicaciones_Loaded`: Configura el message handler del WebView2

- **Métodos auxiliares**:
  - `NotifyMapFormVisibility(bool)`: Notifica al mapa sobre visibilidad del formulario
  - `LoadMarkerOnMap(decimal, decimal)`: Carga marcador con coordenadas específicas

### Protocolo de Mensajes

#### JavaScript → C#
```json
{
  "type": "markerMoved",
  "lat": 19.432608,
  "lng": -99.133209,
  "address": {
    "formatted": "Av. Paseo de la Reforma 222, Ciudad de México",
    "city": "Ciudad de México",
    "state": "Ciudad de México",
    "country": "México"
  }
}
```

#### C# → JavaScript
```javascript
// Notificar visibilidad del formulario
setFormVisibility(true);
setFormVisibility(false);

// Cargar marcador existente
loadExistingMarker(19.432608, -99.133209);
```

### Manejo de Concurrencia
- Los mensajes de JavaScript se procesan de forma asíncrona
- Los campos del formulario se actualizan en el UI thread usando `DispatcherQueue.TryEnqueue()`
- El logging de operaciones mantiene un registro completo de eventos

### Seguridad
- La API Key de Google Maps se obtiene de forma segura del servidor
- No se exponen credenciales en el código cliente
- Validación de coordenadas en C# antes de guardar (-90 a 90 para latitud, -180 a 180 para longitud)

## Elementos UI Agregados

### InfoBar Informativo
```xml
<InfoBar
    IsOpen="True"
    IsClosable="False"
    Severity="Informational"
    Message="Haz clic en el mapa para colocar un marcador rojo y rellenar automáticamente las coordenadas y dirección."
    Margin="0,4,0,4" />
```

### Estilos del Marcador
- **URL del icono**: `https://maps.google.com/mapfiles/ms/icons/red-dot.png`
- **Tamaño escalado**: 40x40 píxeles
- **Animación**: `google.maps.Animation.DROP`
- **Draggable**: true

## Logging y Diagnóstico

### Eventos Registrados
1. Configuración del WebView2 message handler
2. Recepción de mensajes de JavaScript
3. Actualización de campos del formulario
4. Notificación de visibilidad al mapa
5. Carga de marcadores existentes
6. Errores de procesamiento de mensajes

### Ubicación de Logs
- Componente: "Ubicaciones"
- Métodos: "CoreWebView2_WebMessageReceived", "NotifyMapFormVisibility", "LoadMarkerOnMap"

## Mejoras Futuras Posibles

1. **Búsqueda de Direcciones**
   - Agregar un campo de búsqueda
   - Al encontrar una dirección, colocar el marcador automáticamente

2. **Información de Área**
   - Al colocar el marcador, mostrar en qué área geográfica se encuentra (si está dentro de alguna)
   - Usar el endpoint `ValidatePointAsync` del ViewModel

3. **Historial de Ubicaciones**
   - Guardar un historial de ubicaciones recientes
   - Permitir selección rápida de ubicaciones frecuentes

4. **Validación Visual**
   - Mostrar un indicador visual si la ubicación está dentro/fuera de áreas permitidas
   - Advertir al usuario antes de guardar ubicaciones fuera de áreas válidas

5. **Importación desde Archivo**
   - Permitir cargar múltiples ubicaciones desde un archivo CSV o GeoJSON
   - Cada ubicación se representaría con un marcador en el mapa

6. **Street View**
   - Integrar Google Street View
   - Permitir ver la ubicación a nivel de calle antes de guardar

## Compatibilidad

- **Plataforma**: Windows 10/11 (WinUI 3)
- **WebView2**: Requiere Microsoft Edge WebView2 Runtime
- **Google Maps API**: Requiere API Key válida con Maps JavaScript API habilitado
- **Geocoding API**: Debe estar habilitado en el proyecto de Google Cloud

## Testing

### Casos de Prueba
1. **Agregar ubicación con mapa**:
   - Verificar que el marcador aparece al hacer clic
   - Verificar que los campos se rellenan correctamente
   - Verificar que el marcador es arrastrable

2. **Editar ubicación existente**:
   - Verificar que el marcador se carga en la posición correcta
   - Verificar que se puede mover el marcador
   - Verificar que los cambios se guardan correctamente

3. **Cancelar operación**:
   - Verificar que el marcador desaparece
   - Verificar que el formulario se limpia

4. **Reverse geocoding**:
   - Verificar que se obtiene la dirección correcta
   - Verificar manejo de errores cuando no hay dirección disponible

5. **Validación de coordenadas**:
   - Verificar que no se pueden guardar coordenadas inválidas
   - Verificar mensajes de error apropiados

## Archivos Modificados

### 1. `Views/Pages/Ubicaciones.xaml`
- Agregado InfoBar informativo en el formulario de ubicación
- No se modificó la estructura general de la página

### 2. `Views/Pages/Ubicaciones.xaml.cs`
- Agregada variable `_isFormVisible` para rastrear estado del formulario
- Agregado handler `Ubicaciones_Loaded` para configurar WebView2 messaging
- Agregado handler `CoreWebView2_WebMessageReceived` para procesar mensajes
- Modificado `AddButton_Click` para notificar visibilidad al mapa
- Modificado `EditButton_Click` para cargar marcador existente
- Modificado `CancelButton_Click` para ocultar marcador
- Modificado `SaveButton_Click` para limpiar estado del formulario
- Agregado método `NotifyMapFormVisibility` para comunicación con JavaScript
- Agregado método `LoadMarkerOnMap` para cargar marcadores existentes
- Actualizado `GenerateMapHtml` con toda la funcionalidad del marcador

## Conclusión

Esta implementación proporciona una experiencia de usuario intuitiva y eficiente para agregar y editar ubicaciones. El uso de Google Maps con marcadores draggables y reverse geocoding elimina la necesidad de que los usuarios busquen y escriban manualmente las coordenadas, reduciendo errores y mejorando la precisión de los datos de ubicación.

La arquitectura de comunicación bidireccional entre JavaScript y C# mediante WebView2 es robusta y extensible, permitiendo futuras mejoras sin cambios significativos en la estructura del código.
