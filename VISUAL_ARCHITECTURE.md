# Visual Architecture: Google Maps Implementation

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Ubicaciones Page (UI)                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │             Ubicaciones.xaml (XAML View)                   │ │
│  │  - Header with Title & Refresh Button                      │ │
│  │  - Loading Indicator (ProgressRing)                        │ │
│  │  - Error Display (InfoBar)                                 │ │
│  │  - WebView2 Control (Google Maps)                          │ │
│  └────────────────────────────────────────────────────────────┘ │
│                              ↕ Data Binding                      │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │       UbicacionesViewModel (MVVM ViewModel)                │ │
│  │  Properties:                                               │ │
│  │    • MapsConfig (GoogleMapsConfigDto)                      │ │
│  │    • Areas (ObservableCollection<GoogleMapsAreaDto>)       │ │
│  │    • IsLoading, ErrorMessage, IsMapInitialized             │ │
│  │  Methods:                                                  │ │
│  │    • InitializeAsync()                                     │ │
│  │    • LoadConfigurationAsync()                              │ │
│  │    • LoadAreasAsync()                                      │ │
│  │    • RefreshAreasAsync()                                   │ │
│  │    • ValidatePointAsync()                                  │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              ↕ Service Calls
┌─────────────────────────────────────────────────────────────────┐
│                         Service Layer                            │
│  ┌──────────────────────────────┐  ┌────────────────────────┐  │
│  │  GoogleMapsConfigService     │  │    AreasService        │  │
│  │  • GetApiKeyAsync()          │  │  • GetAreasAsync()     │  │
│  │  • GetConfigAsync()          │  │  • GetAreasForGoogle   │  │
│  └──────────────────────────────┘  │    MapsAsync()         │  │
│                                     │  • ValidatePointAsync()│  │
│                                     └────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↕ HTTP + JWT Auth
┌─────────────────────────────────────────────────────────────────┐
│                    HTTP Client Pipeline                          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │         AuthenticatedHttpHandler (Middleware)              │ │
│  │  - Adds JWT Bearer Token to requests                       │ │
│  │  - Handles 401 with token refresh                          │ │
│  │  - Retries failed requests after refresh                   │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              ↕ API Calls
┌─────────────────────────────────────────────────────────────────┐
│                         Backend API                              │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Endpoints:                                                │ │
│  │    GET /api/GoogleMapsConfig                               │ │
│  │    GET /api/GoogleMapsConfig/api-key                       │ │
│  │    GET /api/Areas/googlemaps?activo=true                   │ │
│  │    GET /api/Areas/validate-point?lat=X&lng=Y               │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

```
┌──────────────┐
│  User Opens  │
│  Ubicaciones │
└──────┬───────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ 1. OnNavigatedTo() triggered                                     │
│    └─→ ViewModel.InitializeAsync()                              │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ 2. Load Configuration                                            │
│    └─→ GoogleMapsConfigService.GetConfigAsync()                 │
│        └─→ GET /api/GoogleMapsConfig                            │
│            └─→ Returns: { apiKey, defaultCenter, defaultZoom }  │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ 3. Load Areas                                                    │
│    └─→ AreasService.GetAreasForGoogleMapsAsync(activo: true)   │
│        └─→ GET /api/Areas/googlemaps?activo=true               │
│            └─→ Returns: Array of GoogleMapsAreaDto             │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ 4. Generate HTML                                                 │
│    └─→ GenerateMapHtml() with:                                  │
│        • API Key                                                 │
│        • Center coordinates (lat, lng)                           │
│        • Zoom level                                              │
│        • Serialized areas JSON                                   │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ 5. Load in WebView2                                             │
│    └─→ MapWebView.NavigateToString(html)                       │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ 6. JavaScript Execution                                          │
│    └─→ initMap()                                                │
│        ├─→ Create Google Maps instance                          │
│        ├─→ Create InfoWindow                                    │
│        └─→ renderAreas()                                        │
│            ├─→ For each area:                                   │
│            │   ├─→ Parse JSON (path, options, etc.)            │
│            │   ├─→ Create shape (Polygon/Circle/etc.)          │
│            │   ├─→ Add to map                                  │
│            │   ├─→ Add click listener → showAreaInfo()         │
│            │   └─→ Add hover listeners                         │
│            └─→ All areas rendered                              │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│ 7. User Interaction Ready                                        │
│    ├─→ Click area → InfoWindow shows details                   │
│    ├─→ Hover area → Visual feedback (opacity/border change)    │
│    └─→ Click refresh → Reload from step 3                      │
└──────────────────────────────────────────────────────────────────┘
```

## Geometry Type Rendering

```
┌─────────────────────────────────────────────────────────────────┐
│                      Polygon Rendering                           │
│  Input: { type: "Polygon", path: "[{lat,lng}...]" }            │
│    ↓                                                             │
│  Parse path array                                                │
│    ↓                                                             │
│  google.maps.Polygon({ paths: [...], options })                │
│    ↓                                                             │
│  ┌────────────┐                                                 │
│  │    ╱╲      │  Multi-point area on map                       │
│  │   ╱  ╲     │  with fill color and border                    │
│  │  ╱____╲    │                                                 │
│  └────────────┘                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                       Circle Rendering                           │
│  Input: { type: "Circle", center: "{lat,lng}", radius: 500 }   │
│    ↓                                                             │
│  Parse center and radius                                         │
│    ↓                                                             │
│  google.maps.Circle({ center: {...}, radius, options })        │
│    ↓                                                             │
│  ┌────────────┐                                                 │
│  │    ███     │  Circular area on map                          │
│  │  ███████   │  with fill color and border                    │
│  │    ███     │  radius in meters                              │
│  └────────────┘                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     Rectangle Rendering                          │
│  Input: { type: "Rectangle", bounds: "{n,e,s,w}" }             │
│    ↓                                                             │
│  Parse bounds                                                    │
│    ↓                                                             │
│  google.maps.Rectangle({ bounds: {...}, options })             │
│    ↓                                                             │
│  ┌────────────┐                                                 │
│  │ ██████████ │  Rectangular area on map                       │
│  │ ██████████ │  defined by corner coordinates                 │
│  │ ██████████ │                                                 │
│  └────────────┘                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      Polyline Rendering                          │
│  Input: { type: "Polyline", path: "[{lat,lng}...]" }           │
│    ↓                                                             │
│  Parse path array                                                │
│    ↓                                                             │
│  google.maps.Polyline({ path: [...], options })                │
│    ↓                                                             │
│  ┌────────────┐                                                 │
│  │    ╱       │  Multi-point line on map                       │
│  │   ╱        │  with color and width                          │
│  │  ╱─────    │  (no fill, just stroke)                        │
│  └────────────┘                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## InfoWindow Display

```
┌─────────────────────────────────────────────────────────────────┐
│                    User clicks on area                           │
└──────┬──────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│  shape.click event fires                                         │
│    └─→ showAreaInfo(area, position) called                      │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│  Generate InfoWindow HTML content:                               │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  ┌──────────────────────────────────────────────────────┐  │ │
│  │  │  Zona Centro                           [X]           │  │ │
│  │  │  ─────────────────────────────────────────────────   │  │ │
│  │  │  Tipo: Polygon                                       │  │ │
│  │  │  ID: 1                                               │  │ │
│  │  │  Radio: 500m    (if circle)                         │  │ │
│  │  └──────────────────────────────────────────────────────┘  │ │
│  │  Styled, non-blocking popup                                │ │
│  └────────────────────────────────────────────────────────────┘ │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│  infoWindow.setContent(html)                                     │
│  infoWindow.setPosition(position)                                │
│  infoWindow.open(map)                                            │
└──────────────────────────────────────────────────────────────────┘
```

## Error Handling Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                     Any Operation                                │
└──────┬──────────────────────────────────────────────────────────┘
       │
       ↓
    [Success?] ──Yes→ Update UI with data
       │
       No
       ↓
┌──────────────────────────────────────────────────────────────────┐
│  Exception Caught                                                │
│    ├─→ Log error details (LoggingService)                       │
│    ├─→ Set ErrorMessage property                                │
│    └─→ Set IsLoading = false                                    │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────────────────────────┐
│  UI Updates                                                      │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  ⚠ Error al cargar el mapa                                │ │
│  │  Por favor, intente nuevamente.                           │ │
│  │                                                  [Dismiss] │ │
│  └────────────────────────────────────────────────────────────┘ │
│  InfoBar displays error to user                                 │
└──────────────────────────────────────────────────────────────────┘
```

## State Management

```
Application States:
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│  INITIAL STATE                                                   │
│  • IsLoading = false                                            │
│  • IsMapInitialized = false                                     │
│  • ErrorMessage = null                                          │
│  • Areas = empty collection                                     │
│                                                                  │
├──────────────────────────────────────────────────────────────────┤
│                          ↓ InitializeAsync()                     │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  LOADING STATE                                                   │
│  • IsLoading = true      ← Shows ProgressRing                   │
│  • IsMapInitialized = false                                     │
│  • ErrorMessage = null                                          │
│                                                                  │
├──────────────────────────────────────────────────────────────────┤
│                          ↓ Success                               │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  SUCCESS STATE                                                   │
│  • IsLoading = false                                            │
│  • IsMapInitialized = true   ← Shows WebView2                   │
│  • ErrorMessage = null                                          │
│  • MapsConfig = loaded                                          │
│  • Areas = populated collection                                 │
│                                                                  │
├──────────────────────────────────────────────────────────────────┤
│                          ↓ Error                                 │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ERROR STATE                                                     │
│  • IsLoading = false                                            │
│  • IsMapInitialized = false                                     │
│  • ErrorMessage = "Error message"  ← Shows InfoBar              │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

## Legend

```
Symbols Used:
  ┌─┐   Box/Container
  │ │   Vertical connection
  ─     Horizontal connection
  ↓     Data flow / Sequence
  ├─→   Branch / Decision
  └─→   End of branch
  ↕     Bidirectional flow
  ███   Filled area (map visualization)
  ╱╲    Lines (map visualization)
```

---

This visual architecture demonstrates the complete implementation of the Google Maps viewer on the Ubicaciones page, showing all components, data flows, and interactions in a clear, hierarchical manner.
