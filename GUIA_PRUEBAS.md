# 🧪 Guía de Pruebas (Tests) - Advance Control

## 📋 Tabla de Contenidos

1. [Introducción](#introducción)
2. [Requisitos Previos](#requisitos-previos)
3. [Estructura de Pruebas](#estructura-de-pruebas)
4. [Cómo Ejecutar las Pruebas](#cómo-ejecutar-las-pruebas)
5. [Tipos de Pruebas](#tipos-de-pruebas)
6. [Escribir Nuevas Pruebas](#escribir-nuevas-pruebas)
7. [Mejores Prácticas](#mejores-prácticas)
8. [Solución de Problemas](#solución-de-problemas)

---

## Introducción

Este proyecto utiliza **xUnit** como framework de pruebas unitarias, junto con **Moq** para crear objetos simulados (mocks). Las pruebas se encuentran en el proyecto `Advance Control.Tests`.

### Framework de Pruebas
- **xUnit 2.9.2**: Framework principal de testing
- **Moq 4.20.72**: Librería para crear mocks
- **Microsoft.NET.Test.Sdk 17.11.1**: SDK para ejecutar pruebas
- **coverlet.collector 6.0.2**: Recolector de cobertura de código

---

## Requisitos Previos

### Software Necesario
- **Windows 10/11**: Requerido para compilar el proyecto WinUI 3
- **.NET 8.0 SDK**: [Descargar aquí](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (opcional pero recomendado):
  - Con carga de trabajo "Desarrollo de aplicaciones de escritorio con C++"
  - Con carga de trabajo "Desarrollo de la Plataforma universal de Windows"

### Verificar Instalación
```bash
# Verificar que .NET 8.0 está instalado
dotnet --version
```

---

## Estructura de Pruebas

```
Advance Control.Tests/
├── Advance Control.Tests.csproj    # Configuración del proyecto de pruebas
├── Services/                       # Pruebas de servicios
│   └── AuthServiceTests.cs        # Pruebas del servicio de autenticación
└── ViewModels/                     # Pruebas de ViewModels
    ├── CustomersViewModelTests.cs  # Pruebas del ViewModel de clientes
    └── LoginViewModelTests.cs      # Pruebas del ViewModel de login
```

### Pruebas Existentes

#### 1. **AuthServiceTests.cs**
Pruebas del servicio de autenticación:
- `AuthenticateAsync_WithValidCredentials_ReturnsTrue`: Autenticación exitosa
- `AuthenticateAsync_WithEmptyUsername_ReturnsFalse`: Validación de usuario vacío
- `AuthenticateAsync_WithEmptyPassword_ReturnsFalse`: Validación de contraseña vacía
- `AuthenticateAsync_WithInvalidCredentials_ReturnsFalse`: Credenciales inválidas
- `GetAccessTokenAsync_WithValidToken_ReturnsToken`: Obtención de token
- `ClearTokenAsync_RemovesTokens`: Limpieza de tokens
- `RefreshTokenAsync_WithValidRefreshToken_ReturnsTrue`: Renovación de token

#### 2. **CustomersViewModelTests.cs**
Pruebas del ViewModel de gestión de clientes

#### 3. **LoginViewModelTests.cs**
Pruebas del ViewModel de inicio de sesión

---

## Cómo Ejecutar las Pruebas

### Opción 1: Usando la Línea de Comandos (Recomendado)

#### Ejecutar TODAS las Pruebas
```bash
# Navegar al directorio raíz del proyecto
cd "C:\ruta\a\AdvanceControl"

# Restaurar dependencias
dotnet restore

# Ejecutar todas las pruebas
dotnet test
```

#### Ejecutar Pruebas con Información Detallada
```bash
# Ver detalles de cada prueba
dotnet test --verbosity normal

# Ver información más detallada
dotnet test --verbosity detailed
```

#### Ejecutar Pruebas de un Proyecto Específico
```bash
dotnet test "Advance Control.Tests/Advance Control.Tests.csproj"
```

#### Ejecutar una Prueba Específica
```bash
# Ejecutar solo pruebas que contengan "Authenticate" en el nombre
dotnet test --filter "FullyQualifiedName~Authenticate"

# Ejecutar solo las pruebas de AuthService
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

#### Generar Reporte de Cobertura
```bash
# Ejecutar pruebas con cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

### Opción 2: Usando Visual Studio 2022

#### Explorador de Pruebas
1. Abrir `Advance Control.sln` en Visual Studio
2. Ir a **Ver** → **Explorador de pruebas** (o presionar `Ctrl+E, T`)
3. Visual Studio descubrirá automáticamente todas las pruebas
4. Opciones disponibles:
   - **Ejecutar Todas**: Botón ▶️ verde en la parte superior
   - **Ejecutar Prueba Individual**: Clic derecho en la prueba → Ejecutar
   - **Depurar Prueba**: Clic derecho en la prueba → Depurar

#### Atajos de Teclado en Visual Studio
- `Ctrl+R, A`: Ejecutar todas las pruebas
- `Ctrl+R, T`: Ejecutar pruebas en el contexto actual
- `Ctrl+R, Ctrl+T`: Depurar pruebas en el contexto actual

### Opción 3: Usando Visual Studio Code

1. Instalar la extensión **C# Dev Kit**
2. Abrir la carpeta del proyecto
3. En la barra lateral, seleccionar el ícono de pruebas (vaso de laboratorio)
4. Ejecutar o depurar pruebas desde la interfaz

---

## Tipos de Pruebas

### Pruebas Unitarias
Las pruebas actuales son **pruebas unitarias** que verifican el comportamiento de componentes individuales de forma aislada.

#### Características:
- ✅ Rápidas de ejecutar
- ✅ Aisladas (usan mocks para dependencias)
- ✅ Prueban una sola unidad de código
- ✅ No requieren conexión a servicios externos

#### Ejemplo de Estructura:
```csharp
[Fact]
public async Task NombreMetodo_Condicion_ResultadoEsperado()
{
    // Arrange (Preparar): Configurar el escenario de prueba
    var mockService = new Mock<IService>();
    var sut = new ClassToTest(mockService.Object);
    
    // Act (Actuar): Ejecutar la acción a probar
    var result = await sut.MethodToTest();
    
    // Assert (Afirmar): Verificar el resultado
    Assert.True(result);
}
```

---

## Escribir Nuevas Pruebas

### 1. Crear una Nueva Clase de Prueba

```csharp
using Xunit;
using Moq;
using Advance_Control.Services.TuServicio;

namespace Advance_Control.Tests.Services
{
    public class TuServicioTests
    {
        // Constructor: Inicializar mocks y dependencias
        public TuServicioTests()
        {
            // Configuración inicial
        }

        [Fact]
        public void MetodoAPrueba_CuandoCondicion_EntoncesResultado()
        {
            // Arrange
            
            // Act
            
            // Assert
        }
    }
}
```

### 2. Convenciones de Nomenclatura

#### Nombres de Clases
- Formato: `{ClaseAPrueba}Tests`
- Ejemplo: `AuthServiceTests`, `CustomerServiceTests`

#### Nombres de Métodos
- Formato: `{Metodo}_{Escenario}_{ResultadoEsperado}`
- Ejemplos:
  - `Login_WithValidCredentials_ReturnsTrue`
  - `GetCustomer_WhenNotFound_ReturnsNull`
  - `SaveData_WithInvalidInput_ThrowsException`

### 3. Usar Mocks con Moq

```csharp
// Crear un mock
var mockRepository = new Mock<ICustomerRepository>();

// Configurar comportamiento del mock
mockRepository
    .Setup(x => x.GetCustomerAsync(It.IsAny<int>()))
    .ReturnsAsync(new Customer { Id = 1, Name = "Test" });

// Verificar que un método fue llamado
mockRepository.Verify(x => x.SaveAsync(It.IsAny<Customer>()), Times.Once);
```

### 4. Tipos de Aserciones Comunes

```csharp
// Verificar valores booleanos
Assert.True(result);
Assert.False(result);

// Verificar igualdad
Assert.Equal(expected, actual);
Assert.NotEqual(expected, actual);

// Verificar nulos
Assert.Null(result);
Assert.NotNull(result);

// Verificar colecciones
Assert.Empty(collection);
Assert.NotEmpty(collection);
Assert.Contains(item, collection);

// Verificar excepciones
await Assert.ThrowsAsync<ArgumentException>(() => method());
```

---

## Mejores Prácticas

### ✅ DO (Hacer)

1. **Seguir el patrón AAA (Arrange-Act-Assert)**
   ```csharp
   [Fact]
   public void Test_Method()
   {
       // Arrange: Preparar
       var data = new TestData();
       
       // Act: Actuar
       var result = sut.Process(data);
       
       // Assert: Verificar
       Assert.NotNull(result);
   }
   ```

2. **Nombres descriptivos**: Los nombres deben explicar qué se está probando
   ```csharp
   // ✅ Bueno
   [Fact]
   public void Login_WithEmptyPassword_ReturnsFalse()
   
   // ❌ Malo
   [Fact]
   public void Test1()
   ```

3. **Una aserción principal por prueba**: Enfocarse en una cosa a la vez
   ```csharp
   // ✅ Bueno
   [Fact]
   public void GetUser_WhenExists_ReturnsUser()
   {
       var result = service.GetUser(1);
       Assert.NotNull(result);
   }
   
   [Fact]
   public void GetUser_WhenExists_ReturnsCorrectUser()
   {
       var result = service.GetUser(1);
       Assert.Equal(1, result.Id);
   }
   ```

4. **Independencia**: Cada prueba debe poder ejecutarse de forma aislada
   ```csharp
   // ✅ Cada prueba crea sus propios datos
   [Fact]
   public void Test1()
   {
       var data = CreateTestData();
       // ...
   }
   ```

5. **Limpiar recursos**: Implementar IDisposable si es necesario
   ```csharp
   public class MyTests : IDisposable
   {
       public void Dispose()
       {
           // Limpiar recursos
       }
   }
   ```

### ❌ DON'T (No Hacer)

1. **No depender del orden de ejecución**
2. **No usar datos reales de producción**
3. **No hacer pruebas demasiado complejas**
4. **No probar código de librerías externas**
5. **No ignorar pruebas que fallan** (usar `[Fact(Skip = "razón")]` temporalmente)

---

## Solución de Problemas

### Error: "No tests found"

**Causa**: El proyecto no se compiló correctamente.

**Solución**:
```bash
dotnet clean
dotnet restore
dotnet build
dotnet test
```

### Error: "Could not load file or assembly"

**Causa**: Versiones incompatibles de paquetes NuGet.

**Solución**:
```bash
# Limpiar cache de NuGet
dotnet nuget locals all --clear

# Restaurar paquetes
dotnet restore --force
```

### Error: "Test host process crashed"

**Causa**: Problema en el código de prueba o dependencias faltantes.

**Solución**:
1. Verificar que todas las dependencias están instaladas
2. Revisar el código de la prueba que falla
3. Ejecutar con mayor verbosidad para ver más detalles:
   ```bash
   dotnet test --verbosity detailed
   ```

### Las Pruebas son Muy Lentas

**Solución**:
```bash
# Ejecutar pruebas en paralelo (por defecto)
dotnet test

# Limitar paralelismo si hay problemas de recursos
dotnet test -- xUnit.MaxParallelThreads=1
```

### Necesito Depurar una Prueba

**En Visual Studio**:
1. Poner un punto de interrupción en la prueba
2. Clic derecho → **Depurar pruebas**

**En VS Code**:
1. Instalar extensión C# Dev Kit
2. Usar el depurador integrado de pruebas

**Línea de comandos**:
```bash
# Agregar líneas de depuración en el código
System.Diagnostics.Debugger.Launch();
```

---

## Comandos Útiles

### Resumen de Comandos

```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar con detalles
dotnet test --verbosity normal

# Filtrar por nombre
dotnet test --filter "FullyQualifiedName~Auth"

# Ejecutar pruebas de un solo proyecto
dotnet test "Advance Control.Tests/Advance Control.Tests.csproj"

# Generar reporte de cobertura
dotnet test --collect:"XPlat Code Coverage"

# Listar todas las pruebas sin ejecutarlas
dotnet test --list-tests

# Ejecutar en modo de observación (re-ejecuta al cambiar archivos)
dotnet watch test
```

### Configuración Adicional

Para personalizar el comportamiento de las pruebas, editar el archivo `Advance Control.Tests.csproj`:

```xml
<PropertyGroup>
  <!-- Configurar cobertura de código -->
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>opencover</CoverletOutputFormat>
</PropertyGroup>
```

---

## Recursos Adicionales

### Documentación Oficial
- [xUnit.net](https://xunit.net/) - Framework de pruebas
- [Moq 4](https://github.com/moq/moq4) - Librería de mocking
- [.NET Testing](https://docs.microsoft.com/dotnet/core/testing/) - Guía oficial de Microsoft

### Tutoriales Recomendados
- [Unit Testing Best Practices](https://docs.microsoft.com/dotnet/core/testing/unit-testing-best-practices)
- [Introduction to xUnit](https://xunit.net/docs/getting-started/netfx/visual-studio)

---

## Estado Actual de las Pruebas

### Cobertura por Componente

| Componente | Archivo de Prueba | Estado |
|------------|-------------------|--------|
| AuthService | AuthServiceTests.cs | ✅ Implementado |
| LoginViewModel | LoginViewModelTests.cs | ✅ Implementado |
| CustomersViewModel | CustomersViewModelTests.cs | ✅ Implementado |
| Otros servicios | - | ⏳ Pendiente |

### Próximos Pasos

1. **Expandir cobertura**: Agregar pruebas para otros servicios
2. **Pruebas de integración**: Implementar pruebas que verifiquen la integración entre componentes
3. **Automatización CI/CD**: Configurar ejecución automática en GitHub Actions
4. **Reporte de cobertura**: Implementar herramientas de visualización de cobertura

---

**Última Actualización**: Noviembre 2025  
**Versión del Framework**: .NET 8.0 + xUnit 2.9.2
