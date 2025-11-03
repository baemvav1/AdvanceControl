# Advance Control - Cliente (WinUI)

Cliente de escritorio para Advance Control desarrollado con WinUI 3 siguiendo el patrón MVVM (Model-View-ViewModel).

## Descripción

Aplicación de escritorio para Windows que consume una API externa y proporciona funcionalidad de gestión de clientes con autenticación JWT.

## Tecnologías

- **.NET 8.0** - Framework de desarrollo
- **WinUI 3** - Framework de interfaz de usuario moderno para Windows
- **MVVM Pattern** - Arquitectura de separación de responsabilidades
- **CommunityToolkit.Mvvm** - Toolkit para simplificar implementación MVVM
- **Microsoft.Extensions.Hosting** - Inyección de dependencias y configuración
- **Microsoft.Extensions.Http** - Cliente HTTP con soporte para DI

## Requisitos del Sistema

- Windows 10 versión 1809 (build 17763) o superior
- .NET 8.0 SDK
- Visual Studio 2022 (recomendado) con:
  - Carga de trabajo "Desarrollo de la Plataforma universal de Windows"
  - Carga de trabajo ".NET Desktop Development"

## Instalación

### Instalar Dependencias

```bash
dotnet restore "Advance Control/Advance Control.csproj"
```

O instalar paquetes individuales:

```bash
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Http
dotnet add package CommunityToolkit.Mvvm
```

### Compilar

```bash
dotnet build "Advance Control/Advance Control.csproj"
```

### Ejecutar

Desde Visual Studio: Presionar F5

Desde línea de comandos:
```bash
dotnet run --project "Advance Control/Advance Control.csproj"
```

## Configuración

La aplicación utiliza `appsettings.json` para configuración. Ejemplo de configuración:

```json
{
  "ExternalApi": {
    "BaseUrl": "https://api.example.com/",
    "ApiKey": "your-api-key-here"
  }
}
```

## Estructura del Proyecto

```
Advance Control/
├── Assets/              # Recursos de la aplicación (iconos, imágenes)
├── Converters/          # Conversores para data binding XAML
├── Helpers/             # Clases auxiliares y utilidades
├── Models/              # Modelos de datos (DTOs)
├── Navigation/          # Servicios de navegación
├── Services/            # Servicios de lógica de negocio
│   ├── Auth/           # Autenticación y autorización
│   ├── EndPointProvider/ # Proveedor de endpoints de API
│   ├── Http/           # Manejo de HTTP
│   ├── OnlineCheck/    # Verificación de conectividad
│   └── Security/       # Almacenamiento seguro de secretos
├── Settings/            # Configuraciones de la aplicación
├── ViewModels/          # ViewModels (lógica de presentación)
├── Views/               # Vistas XAML (interfaz de usuario)
├── App.xaml[.cs]       # Punto de entrada de la aplicación
└── appsettings.json    # Archivo de configuración
```

## Estado de Implementación

### Componentes Implementados ✅

- **OnlineCheck Service**: Verificación de conectividad con la API
- **ApiEndpointProvider**: Construcción de URLs de endpoints
- **BooleanToVisibilityConverter**: Conversor para visibilidad en XAML

### Componentes Pendientes 🚧

Los siguientes archivos están creados pero requieren implementación:

**Autenticación:**
- `IAuthService.cs` - Interfaz del servicio de autenticación
- `AuthService.cs` - Implementación del servicio de autenticación
- `AuthServiceStub.cs` - Stub para pruebas sin API real

**Seguridad:**
- `ISecretStorage.cs` - Interfaz para almacenamiento seguro
- `SecretStorageWindows.cs` - Implementación usando Windows Credential Manager

**HTTP:**
- `AuthenticatedHttpHandler.cs` - Handler para añadir JWT a peticiones HTTP

**Modelos:**
- `CustomerDto.cs` - Modelo de datos de cliente
- `TokenDto.cs` - Modelo de datos de token JWT

**Navegación:**
- `INavigationService.cs` - Interfaz del servicio de navegación

**Helpers:**
- `JwtUtils.cs` - Utilidades para manejo de tokens JWT

**ViewModels:**
- `ViewModelBase.cs` - Clase base para ViewModels
- `MainViewModel.cs` - ViewModel de la ventana principal
- `CustomersViewModel.cs` - ViewModel para gestión de clientes

**Settings:**
- `ClientSettings.cs` - Configuración del cliente

## Documentación Adicional

📖 **[DOCUMENTATION_INDEX.md](./DOCUMENTATION_INDEX.md)** - Índice completo de documentación

- [ARCHITECTURE.md](./ARCHITECTURE.md) - Documentación de arquitectura
- [EMPTY_FILES.md](./EMPTY_FILES.md) - Lista detallada de archivos pendientes de implementación
- [API.md](./API.md) - Documentación de servicios implementados
- [DEVELOPER_GUIDE.md](./DEVELOPER_GUIDE.md) - Guía para desarrolladores
- [EMPTY_FILES_SUMMARY.md](./EMPTY_FILES_SUMMARY.md) - Resumen rápido de archivos pendientes

## Contribuir

1. Revisar la lista de componentes pendientes en [EMPTY_FILES.md](./EMPTY_FILES.md)
2. Implementar la funcionalidad siguiendo los patrones existentes
3. Agregar XML documentation comments
4. Mantener consistencia con el estilo de código existente

## Licencia

[Especificar licencia del proyecto]
