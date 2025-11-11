# Advance Control - Cliente (WinUI 3)

Sistema de gestión empresarial con arquitectura MVVM para Windows.

## 📋 Descripción

Advance Control es una aplicación de escritorio moderna desarrollada con WinUI 3 que implementa un sistema cliente para gestión empresarial con los siguientes módulos:
- **Operaciones**: Gestión de operaciones del negocio
- **Asesoría**: Sistema de asesoramiento a clientes
- **Mantenimiento**: Control de mantenimientos
- **Clientes**: Administración de clientes

## 🏗️ Arquitectura

- **Framework:** .NET 8.0 + WinUI 3
- **Patrón:** MVVM (Model-View-ViewModel)
- **Inyección de Dependencias:** Microsoft.Extensions.Hosting
- **Autenticación:** JWT con auto-refresh
- **Almacenamiento Seguro:** Windows PasswordVault

## 🚀 Inicio Rápido

### Requisitos Previos
- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (con carga de trabajo de desarrollo de Windows App SDK)

### Compilar el Proyecto

```bash
# Restaurar paquetes
dotnet restore

# Compilar
dotnet build "Advance Control.sln"
```

### Ejecutar las Pruebas

```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar con información detallada
dotnet test --verbosity normal

# Ejecutar una prueba específica
dotnet test --filter "FullyQualifiedName~AuthService"
```

Para más información sobre cómo usar las pruebas, consulta la **[GUIA_PRUEBAS.md](./GUIA_PRUEBAS.md)**.

## 📚 Documentación

### Documentación Completa del Proyecto

- **[RESUMEN_EJECUTIVO.md](./RESUMEN_EJECUTIVO.md)** - Resumen general del estado del proyecto (¡EMPIEZA AQUÍ!)
- **[ARQUITECTURA_Y_ESTADO.md](./ARQUITECTURA_Y_ESTADO.md)** - Documentación técnica completa de arquitectura
- **[DIAGRAMA_FLUJO_SISTEMA.md](./DIAGRAMA_FLUJO_SISTEMA.md)** - Diagramas de flujo de todos los procesos
- **[LISTA_ERRORES_Y_MEJORAS.md](./LISTA_ERRORES_Y_MEJORAS.md)** - Lista priorizada de errores y mejoras
- **[GUIA_PRUEBAS.md](./GUIA_PRUEBAS.md)** - Guía completa para ejecutar y escribir pruebas

## 📊 Estado Actual

### Completitud: 67.5%

| Componente | Estado | Completitud |
|------------|--------|-------------|
| Infraestructura Base | ✅ | 100% |
| Sistema de Autenticación | ✅ | 100% |
| Sistema de Navegación | ✅ | 100% |
| Logging | ✅ | 100% |
| Módulos de Negocio | 🔄 | 25% |
| Testing | 🔄 | 30% |

### Servicios Implementados ✅

- **AuthService**: Autenticación JWT con auto-refresh
- **NavigationService**: Navegación entre páginas
- **LoggingService**: Logging centralizado al servidor
- **OnlineCheck**: Verificación de conectividad
- **DialogService**: Sistema de diálogos flexible
- **SecretStorageWindows**: Almacenamiento seguro

## 🛠️ Desarrollo

Ver **[ARQUITECTURA_Y_ESTADO.md](./ARQUITECTURA_Y_ESTADO.md)** para guía completa de desarrollo.

## 🎯 Próximos Pasos

Ver **[RESUMEN_EJECUTIVO.md](./RESUMEN_EJECUTIVO.md)** para el roadmap completo.

---

**Estado del Proyecto:** ✅ INFRAESTRUCTURA COMPLETA - LISTO PARA DESARROLLO DE MÓDULOS  
**Última Actualización:** 2025-11-06  
**Calificación:** 8.5/10
