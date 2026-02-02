# Funcionalidad: Botón "Ver en Mapa" en Vista de Equipos

## Resumen

Se ha implementado una nueva funcionalidad que permite a los usuarios navegar desde la vista de equipos directamente a la página de ubicaciones para visualizar una ubicación específica en el mapa.

## Cambios Implementados

### 1. Vista de Equipos (EquiposView.xaml)

Se agregó un nuevo botón "Ver en Mapa" en el pivot de ubicaciones, que aparece cuando un equipo tiene una ubicación asignada.

**Ubicación**: Líneas 429-438

**Características del botón**:
- **Icono**: Symbol="Map" (icono de mapa)
- **Texto**: "Ver en Mapa"
- **Visibilidad**: Solo visible cuando `Ubicacion` no es null (mismo comportamiento que el botón "Editar Ubicación")
- **Estilo**: Fondo negro con borde gris oscuro, texto blanco

### 2. Código de Vista de Equipos (EquiposView.xaml.cs)

Se implementó el manejador de eventos `VerEnMapaButton_Click` que:

**Ubicación**: Líneas 618-631

**Funcionalidad**:
1. Obtiene el equipo desde el Tag del botón
2. Verifica que hay una ubicación asignada
3. Navega a la página de Ubicaciones pasando el IdUbicacion como parámetro

```csharp
Frame.Navigate(typeof(Ubicaciones), equipo.IdUbicacion.Value);
```

### 3. Página de Ubicaciones (Ubicaciones.xaml.cs)

Se realizaron dos cambios principales:

#### a) Actualización del método OnNavigatedTo (Líneas 228-232)

Se agregó lógica para detectar si se pasó un parámetro de navegación (IdUbicacion) y, si es así, seleccionar y centrar esa ubicación:

```csharp
// Si se pasó un ID de ubicación como parámetro, seleccionarla y centrar el mapa
if (e.Parameter is int idUbicacion)
{
    await _loggingService.LogInformationAsync($"Navegación con parámetro: IdUbicacion = {idUbicacion}", "Ubicaciones", "OnNavigatedTo");
    await SelectAndCenterUbicacionAsync(idUbicacion);
}
```

#### b) Nuevo método SelectAndCenterUbicacionAsync (Líneas 1170-1208)

Este método:
1. Busca la ubicación en la lista de ubicaciones del ViewModel usando LINQ
2. Si encuentra la ubicación:
   - La selecciona en el ListView (ViewModel.SelectedUbicacion)
   - Espera 500ms para asegurar que el mapa esté inicializado
   - Centra el mapa en la ubicación usando el método existente `CenterMapOnUbicacion`
3. Registra todo el proceso usando el servicio de logging

## Flujo de Usuario

1. Usuario navega a la vista de Equipos
2. Usuario expande un equipo que tiene una ubicación asignada
3. En el pivot "Ubicacion", se muestran los detalles de la ubicación
4. Usuario hace clic en el botón "Ver en Mapa"
5. La aplicación navega a la página de Ubicaciones
6. La ubicación correspondiente se selecciona automáticamente en la lista
7. El mapa se centra en la ubicación seleccionada con zoom nivel 15

## Beneficios

- **Navegación intuitiva**: Los usuarios pueden visualizar rápidamente una ubicación en el mapa sin buscarla manualmente
- **Experiencia mejorada**: Integración fluida entre las vistas de equipos y ubicaciones
- **Consistencia**: Usa los patrones de navegación existentes en la aplicación
- **Logging completo**: Toda la operación está registrada para facilitar el debugging

## Archivos Modificados

1. `Advance Control/Views/Pages/EquiposView.xaml`
   - Agregado botón "Ver en Mapa" (10 líneas)

2. `Advance Control/Views/Pages/EquiposView.xaml.cs`
   - Agregado método `VerEnMapaButton_Click` (14 líneas)

3. `Advance Control/Views/Pages/Ubicaciones.xaml.cs`
   - Actualizado método `OnNavigatedTo` para aceptar parámetros (7 líneas)
   - Agregado método `SelectAndCenterUbicacionAsync` (38 líneas)

**Total**: 69 líneas de código agregadas, 0 líneas modificadas

## Pruebas Recomendadas

1. **Caso normal**: 
   - Abrir un equipo con ubicación asignada
   - Hacer clic en "Ver en Mapa"
   - Verificar que la página de Ubicaciones se abre y la ubicación está seleccionada

2. **Caso sin ubicación**: 
   - Verificar que el botón no aparece cuando el equipo no tiene ubicación

3. **Caso de error**: 
   - Navegar con un IdUbicacion que no existe
   - Verificar que no hay errores y se registra en los logs
