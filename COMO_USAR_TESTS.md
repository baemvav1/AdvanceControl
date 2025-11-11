# ⚡ Cómo Usar los Tests - Guía Rápida

## 🚀 Inicio Rápido (Quick Start)

### Ejecutar Todos los Tests

```bash
cd "C:\ruta\a\AdvanceControl"
dotnet test
```

### Comandos Más Comunes

```bash
# 1. Ejecutar todos los tests
dotnet test

# 2. Ver detalles de cada test
dotnet test --verbosity normal

# 3. Ejecutar solo tests de autenticación
dotnet test --filter "FullyQualifiedName~Auth"

# 4. Ejecutar solo tests del LoginViewModel
dotnet test --filter "FullyQualifiedName~LoginViewModel"

# 5. Listar todos los tests sin ejecutarlos
dotnet test --list-tests
```

## 📁 ¿Dónde Están los Tests?

Los tests se encuentran en la carpeta:
```
Advance Control.Tests/
├── Services/
│   └── AuthServiceTests.cs           # Tests del servicio de autenticación
└── ViewModels/
    ├── LoginViewModelTests.cs        # Tests del ViewModel de login
    └── CustomersViewModelTests.cs    # Tests del ViewModel de clientes
```

## 🎯 ¿Qué Tests Existen?

### AuthServiceTests (7 tests)
- ✅ Autenticación con credenciales válidas
- ✅ Validación de usuario vacío
- ✅ Validación de contraseña vacía
- ✅ Manejo de credenciales inválidas
- ✅ Obtención de token de acceso
- ✅ Limpieza de tokens
- ✅ Renovación de token (refresh)

### LoginViewModelTests
- ✅ Tests del proceso de login
- ✅ Validación de formulario
- ✅ Manejo de errores

### CustomersViewModelTests
- ✅ Tests del ViewModel de clientes
- ✅ Operaciones CRUD

## 🖥️ Usar Visual Studio

1. Abrir `Advance Control.sln`
2. Ir a **Ver** → **Explorador de pruebas** (Ctrl+E, T)
3. Hacer clic en ▶️ "Ejecutar Todas"

### Atajos de Teclado
- `Ctrl+R, A` - Ejecutar todas las pruebas
- `Ctrl+R, T` - Ejecutar pruebas en el contexto actual
- `Ctrl+R, Ctrl+T` - Depurar pruebas

## 💡 Ejemplos Prácticos

### Ejecutar Tests de un Servicio Específico
```bash
# Solo tests de AuthService
dotnet test --filter "AuthServiceTests"

# Solo tests de CustomersViewModel
dotnet test --filter "CustomersViewModelTests"
```

### Ver Información Detallada
```bash
# Ver qué tests se ejecutan y sus resultados
dotnet test --verbosity normal

# Ver información muy detallada (útil para debugging)
dotnet test --verbosity detailed
```

### Ejecutar Solo un Test
```bash
# Ejecutar un test específico por nombre
dotnet test --filter "AuthenticateAsync_WithValidCredentials_ReturnsTrue"
```

## ❓ Preguntas Frecuentes

### ¿Necesito Windows para ejecutar los tests?
Sí, el proyecto usa WinUI 3 que requiere Windows 10/11 para compilar.

### ¿Qué framework de testing usa el proyecto?
- **xUnit 2.9.2** - Framework de pruebas
- **Moq 4.20.72** - Para crear mocks

### ¿Cuántos tests hay actualmente?
Aproximadamente 40+ tests unitarios que cubren:
- Servicios de autenticación
- ViewModels principales
- Operaciones CRUD básicas

### ¿Cómo agrego nuevos tests?
Ver la **[GUIA_PRUEBAS.md](./GUIA_PRUEBAS.md)** para instrucciones detalladas.

## 🔗 Más Información

Para una guía completa y detallada, consulta:
- **[GUIA_PRUEBAS.md](./GUIA_PRUEBAS.md)** - Guía completa de testing (400+ líneas)

## 🆘 Problemas Comunes

### "No tests found"
```bash
# Solución: Limpiar y recompilar
dotnet clean
dotnet restore
dotnet build
dotnet test
```

### "Test host process crashed"
```bash
# Solución: Ver más detalles del error
dotnet test --verbosity detailed
```

### Tests muy lentos
```bash
# Limitar threads si hay problemas de recursos
dotnet test -- xUnit.MaxParallelThreads=1
```

---

## ✅ Resumen

**Para ejecutar los tests simplemente:**
```bash
dotnet test
```

**Para más información:**
- Lee la [GUIA_PRUEBAS.md](./GUIA_PRUEBAS.md) completa
- Revisa el código en `Advance Control.Tests/`
- Usa Visual Studio para una experiencia visual

---

**Última Actualización:** Noviembre 2025  
**Versión:** 1.0
