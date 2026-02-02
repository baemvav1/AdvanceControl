# Visual Guide: Ver en Mapa Button

## Button Location in UI

The "Ver en Mapa" button is located in the Equipos view, within the expanded equipment details panel.

```
┌─────────────────────────────────────────────────────────────┐
│ EQUIPOS VIEW                                                │
├─────────────────────────────────────────────────────────────┤
│ Search filters...                                           │
│                                                              │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Equipment Item (Expanded)                               │ │
│ │ ┌─────────────────────────────────────────────────────┐ │ │
│ │ │ [Detalles] [Clientes] [Ubicacion] ← Pivot Tabs     │ │ │
│ │ ├─────────────────────────────────────────────────────┤ │ │
│ │ │ Ubicación del Equipo                                │ │ │
│ │ │                                                       │ │ │
│ │ │ ┌─────────────────────────────────────────────────┐ │ │ │
│ │ │ │ Building Name                                   │ │ │ │
│ │ │ │ Main office location                            │ │ │ │
│ │ │ │ Lat: 19.4326, Lng: -99.1332                     │ │ │ │
│ │ │ └─────────────────────────────────────────────────┘ │ │ │
│ │ │                                                       │ │ │
│ │ │ ┌────────────────────┐ ┌────────────────────┐       │ │ │
│ │ │ │ ✏️ Editar Ubicación│ │ 🗺️ Ver en Mapa     │ ← NEW │ │ │
│ │ │ └────────────────────┘ └────────────────────┘       │ │ │
│ │ └─────────────────────────────────────────────────────┘ │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Button Properties

### XAML Definition
```xml
<Button
    x:Name="VerEnMapaButton" 
    Background="Black" 
    BorderThickness="1" 
    BorderBrush="DarkGray"
    Click="VerEnMapaButton_Click" 
    Foreground="WhiteSmoke"
    Tag="{x:Bind}"
    Visibility="{x:Bind Ubicacion, Converter={StaticResource NullToVisibilityConverter}, Mode=OneWay}">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <SymbolIcon Symbol="Map" />
        <TextBlock Text="Ver en Mapa" />
    </StackPanel>
</Button>
```

### Visual Characteristics
- **Background**: Black
- **Border**: 1px DarkGray
- **Text Color**: WhiteSmoke
- **Icon**: Map symbol (🗺️)
- **Layout**: Icon on left, text on right with 8px spacing

### Visibility Behavior
- **Visible**: When equipment has an assigned location (Ubicacion != null)
- **Hidden**: When equipment has no location
- Same visibility logic as "Editar Ubicación" button

## User Interaction Flow

```
┌─────────────────────────┐
│   User in Equipos View  │
└──────────┬──────────────┘
           │
           ▼
┌────────────────────────────┐
│ Expand equipment with      │
│ assigned location          │
└──────────┬─────────────────┘
           │
           ▼
┌────────────────────────────┐
│ Navigate to Ubicacion tab  │
└──────────┬─────────────────┘
           │
           ▼
┌────────────────────────────┐
│ See location details and   │
│ "Ver en Mapa" button       │
└──────────┬─────────────────┘
           │
           ▼
┌────────────────────────────┐
│ Click "Ver en Mapa" button │
└──────────┬─────────────────┘
           │
           ▼
┌────────────────────────────┐
│ Navigate to Ubicaciones    │
│ page with location ID      │
└──────────┬─────────────────┘
           │
           ▼
┌────────────────────────────┐
│ Location selected in list  │
│ and centered on map        │
└────────────────────────────┘
```

## Button States

### Normal State
```
┌────────────────────┐
│ 🗺️ Ver en Mapa    │  ← Black background, white text
└────────────────────┘
```

### Hover State
```
┌────────────────────┐
│ 🗺️ Ver en Mapa    │  ← Lighter background (system hover effect)
└────────────────────┘
```

### Pressed State
```
┌────────────────────┐
│ 🗺️ Ver en Mapa    │  ← Darker background (system pressed effect)
└────────────────────┘
```

### Hidden State (No Location)
```
(Button not visible when equipment has no location)
```

## Code Behavior

### On Click
1. Extract equipment data from button's Tag property
2. Validate equipment has location (IdUbicacion.HasValue)
3. Navigate to Ubicaciones page: `Frame.Navigate(typeof(Ubicaciones), idUbicacion)`

### Navigation Parameter
- **Type**: int (IdUbicacion)
- **Purpose**: Tell Ubicaciones page which location to display
- **Handling**: Ubicaciones.OnNavigatedTo checks for parameter and calls SelectAndCenterUbicacionAsync

### Result
- Ubicaciones page loads
- Location is found in the list using LINQ: `FirstOrDefault(u => u.IdUbicacion == idUbicacion)`
- Location is selected: `ViewModel.SelectedUbicacion = ubicacion`
- Map centers on location with zoom level 15
- User sees location details and marker on map

## Comparison with Similar Buttons

### Editar Ubicación Button
- **Purpose**: Edit location assignment for equipment
- **Action**: Opens dialog to select different location
- **Icon**: Edit (✏️)
- **Location**: Same row, left of "Ver en Mapa"

### Ver en Mapa Button (NEW)
- **Purpose**: View location on map
- **Action**: Navigates to Ubicaciones page with location selected
- **Icon**: Map (🗺️)
- **Location**: Same row, right of "Editar Ubicación"

Both buttons:
- Share same visual style (black background, white text, dark gray border)
- Only visible when location exists
- Located in Ubicacion pivot tab
- Provide complementary functionality

## Integration with Existing Features

The "Ver en Mapa" button integrates seamlessly with:

1. **Equipment Management**: Uses existing equipment data structure (EquipoDto)
2. **Location Service**: Leverages existing IUbicacionService for data
3. **Navigation System**: Uses standard WinUI Frame navigation
4. **Map Display**: Reuses existing CenterMapOnUbicacion method
5. **Logging**: Uses existing ILoggingService for diagnostics

No breaking changes to existing functionality.
