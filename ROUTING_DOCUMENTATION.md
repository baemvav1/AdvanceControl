# Documentación de Rutas de Navegación - Advance Control

## Resumen
Esta aplicación WinUI 3 utiliza un sistema de navegación personalizado implementado con el patrón Service. Este documento explica dónde se encuentran las rutas y cómo funciona la navegación.

---

## 📍 Ubicación de los Componentes de Navegación

### 1. **Servicio de Navegación**
- **Ubicación**: `/Advance Control/Navigation/`
- **Archivos principales**:
  - `NavigationService.cs` - Implementación del servicio de navegación
  - `INavigationService.cs` - Interfaz del servicio

**Funcionalidad**:
- Gestiona la navegación entre páginas usando un `Frame` de WinUI
- Registra rutas usando el método `Configure<TPage>(string tag)`
- Navega a páginas usando el método `Navigate(string tag)`
- Soporta navegación hacia atrás con `GoBack()` y `CanGoBack`

### 2. **Configuración de Rutas**
- **Ubicación**: `/Advance Control/ViewModels/MainViewModel.cs`
- **Líneas**: 201-209

```csharp
// Configure routes for each page
_navigationService.Configure<Views.OperacionesView>("Operaciones");
_navigationService.Configure<Views.AcesoriaView>("Asesoria");
_navigationService.Configure<Views.MttoView>("Mantenimiento");
_navigationService.Configure<Views.ClientesView>("Clientes");
_navigationService.Configure<Views.EquiposView>("Equipos");
_navigationService.Configure<Views.RefaaccionView>("Refacciones");
_navigationService.Configure<Views.Servicios>("Servicios");
_navigationService.Configure<Views.ProveedoresView>("Proveedores");
```

**Nota**: Este es el lugar donde se registran todas las rutas de la aplicación. Cada ruta asocia un "tag" (etiqueta) con un tipo de página (View).

### 3. **Menú de Navegación (UI)**
- **Ubicación**: `/Advance Control/Views/MainWindow.xaml`
- **Líneas**: 71-88

```xml
<NavigationView.MenuItems>
    <NavigationViewItem Content="Operaciones" Icon="Calculator" Tag="Operaciones" />
    <NavigationViewItem Content="Asesoría" Icon="ContactInfo" Tag="Asesoria" />
    <NavigationViewItem Content="Mantenimiento" Icon="Repair" Tag="Mantenimiento" />
    <NavigationViewItem Content="Clientes" Icon="People" Tag="Clientes" />
    <NavigationViewItem Content="Equipos" Icon="AllApps" Tag="Equipos" />
    <NavigationViewItem Content="Refacciones" Icon="Setting" Tag="Refacciones" />
    <NavigationViewItem Content="Servicios" Icon="Repair" Tag="Servicios" />
    <NavigationViewItem Content="Proveedores" Icon="Contact" Tag="Proveedores" />
</NavigationView.MenuItems>
```

**Nota**: El atributo `Tag` de cada `NavigationViewItem` debe coincidir con el tag registrado en `MainViewModel.cs`.

### 4. **Manejador de Eventos de Navegación**
- **Ubicación**: `/Advance Control/ViewModels/MainViewModel.cs`
- **Método**: `OnNavigationItemInvoked` (líneas 221-231)

```csharp
public void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
{
    if (args.InvokedItemContainer is NavigationViewItem item)
    {
        var tag = item.Tag?.ToString();
        if (!string.IsNullOrEmpty(tag))
        {
            _navigationService.Navigate(tag);
        }
    }
}
```

**Funcionalidad**: Este método se invoca cuando el usuario hace clic en un elemento del menú y llama al servicio de navegación con el tag correspondiente.

### 5. **Páginas (Views)**
- **Ubicación**: `/Advance Control/Views/Pages/`
- **Archivos**:
  - `OperacionesView.xaml` / `.xaml.cs`
  - `AcesoriaView.xaml` / `.xaml.cs`
  - `MttoView.xaml` / `.xaml.cs`
  - `ClientesView.xaml` / `.xaml.cs`
  - `EquiposView.xaml` / `.xaml.cs`
  - `RefaaccionView.xaml` / `.xaml.cs`
  - `Servicios.xaml` / `.xaml.cs` ⚠️ (Nota: no sigue el patrón "*View")
  - `ProveedoresView.xaml` / `.xaml.cs`

**Namespace**: `Advance_Control.Views`

### 6. **ViewModels**
- **Ubicación**: `/Advance Control/ViewModels/`
- **Archivos relacionados con navegación**:
  - `OperacionesViewModel.cs`
  - `AcesoriaViewModel.cs`
  - `MttoViewModel.cs`
  - `CustomersViewModel.cs` (para ClientesView)
  - `EquiposViewModel.cs`
  - `RefaccionesViewModel.cs`
  - `ServiciosViewModel.cs`
  - `ProveedoresViewModel.cs`

### 7. **Registro de Dependencias (DI)**
- **Ubicación**: `/Advance Control/App.xaml.cs`
- **Línea del Servicio de Navegación**: 380
- **Líneas de ViewModels**: 388-399

```csharp
// Servicio de navegación (línea 380)
services.AddSingleton<INavigationService, NavigationService>();

// ViewModels (líneas 388-399)
services.AddTransient<ViewModels.MainViewModel>();
services.AddTransient<ViewModels.LoginViewModel>();
services.AddTransient<ViewModels.CustomersViewModel>();
services.AddTransient<ViewModels.ProveedoresViewModel>();
services.AddTransient<ViewModels.EquiposViewModel>();
services.AddTransient<ViewModels.OperacionesViewModel>();
services.AddTransient<ViewModels.AcesoriaViewModel>();
services.AddTransient<ViewModels.MttoViewModel>();
services.AddTransient<ViewModels.NuevoEquipoViewModel>();
services.AddTransient<ViewModels.RefaccionesViewModel>();
services.AddTransient<ViewModels.ServiciosViewModel>();
```

---

## 🔄 Flujo de Navegación

1. **Usuario hace clic** en un elemento del menú de navegación (`MainWindow.xaml`)
2. **Se dispara el evento** `ItemInvoked` del `NavigationView`
3. **MainViewModel** recibe el evento en `OnNavigationItemInvoked`
4. **Se extrae el Tag** del elemento seleccionado
5. **NavigationService** recibe la llamada `Navigate(tag)`
6. **NavigationService busca** el tipo de página registrado para ese tag
7. **Frame navega** a la página correspondiente
8. **OnNavigatedTo** se dispara en la página destino
9. **ViewModel carga datos** (ej: `LoadServiciosAsync()`)

---

## ✅ Problema Resuelto: Servicios

### **Problema Original**
El servicio de navegación no podía encontrar la ruta para "ServiciosView" porque:
- ✅ La página existe: `Servicios.xaml`
- ✅ El ViewModel existe: `ServiciosViewModel.cs`
- ✅ El menú tiene el elemento: `Tag="Servicios"`
- ❌ **Faltaba**: La configuración de la ruta en `MainViewModel.cs`

### **Solución Aplicada**
Se agregó la siguiente línea en `MainViewModel.cs` (línea 208):
```csharp
_navigationService.Configure<Views.Servicios>("Servicios");
```

Esto registra la página `Servicios` con el tag "Servicios", permitiendo que el sistema de navegación la encuentre cuando el usuario hace clic en el menú.

---

## 📋 Checklist para Agregar una Nueva Página

Si necesitas agregar una nueva página con navegación, sigue estos pasos:

1. ✅ **Crear la View** en `/Advance Control/Views/Pages/`
   - Archivo `.xaml` (interfaz)
   - Archivo `.xaml.cs` (code-behind)
   - Namespace: `Advance_Control.Views`

2. ✅ **Crear el ViewModel** en `/Advance Control/ViewModels/`
   - Heredar de `ViewModelBase`
   - Implementar lógica de negocio

3. ✅ **Registrar el ViewModel en DI** en `App.xaml.cs`
   ```csharp
   services.AddTransient<ViewModels.NuevoViewModel>();
   ```

4. ✅ **Agregar ruta en MainViewModel.cs** (líneas 201-209)
   ```csharp
   _navigationService.Configure<Views.NuevaView>("NuevaRuta");
   ```

5. ✅ **Agregar elemento al menú** en `MainWindow.xaml` (líneas 71-88)
   ```xml
   <NavigationViewItem Content="Nueva Sección" Icon="Document" Tag="NuevaRuta" />
   ```

6. ✅ **Verificar** que el `Tag` del menú coincida con el tag de configuración

---

## 🔍 Convenciones de Nomenclatura

- **Views**: Generalmente terminan en "View" (ej: `ClientesView`, `EquiposView`)
  - **Excepción**: `Servicios` no sigue este patrón
- **ViewModels**: Terminan en "ViewModel" (ej: `ClientesViewModel`, `ServiciosViewModel`)
- **Tags de Navegación**: Normalmente el nombre sin el sufijo "View" (ej: "Clientes", "Equipos", "Servicios")

---

## 🛠️ Mantenimiento

Para mantener el sistema de navegación funcionando correctamente:

1. **Sincronizar tags**: Asegúrate de que los tags en `MainWindow.xaml` coincidan con los tags en `MainViewModel.cs`
2. **Registrar ViewModels**: Todos los ViewModels deben estar registrados en el contenedor DI
3. **Convenciones de nombres**: Trata de seguir el patrón "*View" para las páginas (aunque `Servicios` es una excepción)
4. **Documentar cambios**: Actualiza esta documentación cuando agregues nuevas rutas

---

## 📞 Referencias de Código

| Componente | Archivo | Líneas Clave |
|------------|---------|--------------|
| Servicio de Navegación | `Navigation/NavigationService.cs` | Todo el archivo |
| Configuración de Rutas | `ViewModels/MainViewModel.cs` | 201-209 |
| Menú de Navegación | `Views/MainWindow.xaml` | 71-88 |
| Manejador de Eventos | `ViewModels/MainViewModel.cs` | 221-231 |
| Registro DI | `App.xaml.cs` | 380, 388-399 |

---

**Fecha de Creación**: 2026-01-28  
**Última Actualización**: 2026-01-28  
**Versión**: 1.0
