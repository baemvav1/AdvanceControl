# Estado de Cuenta - Funcionalidad de Carga de XML

## Descripción General

Se ha implementado la funcionalidad para cargar archivos XML de estados de cuenta bancarios en la vista `EsCuentaView`. Esta implementación incluye clases modelo, un ViewModel con lógica de parseo XML, y una interfaz de usuario completa.

## Componentes Implementados

### 1. Modelos de Datos

#### `EstadoCuenta.cs`
Clase modelo para representar un estado de cuenta completo:
- **NumeroCuenta**: Número de cuenta bancaria
- **Titular**: Nombre del titular de la cuenta
- **Banco**: Nombre del banco emisor
- **Sucursal**: Sucursal del banco
- **Periodo**: Período del estado de cuenta
- **FechaInicio** y **FechaFin**: Fechas del período
- **SaldoInicial** y **SaldoFinal**: Saldos del período
- **TotalCargos** y **TotalAbonos**: Totales de cargos y abonos
- **Transacciones**: Lista de transacciones del período

#### `Transaccion.cs`
Clase modelo para representar una transacción individual:
- **Fecha**: Fecha de la transacción
- **Descripcion**: Descripción de la transacción
- **Monto**: Monto de la transacción (positivo o negativo)
- **Tipo**: Tipo de transacción (Cargo o Abono)
- **Saldo**: Saldo después de la transacción
- **Referencia**: Número de referencia de la transacción

### 2. ViewModel

#### `EsCuentaViewModel.cs`
ViewModel que maneja la lógica de la vista:
- **CargarArchivoXmlAsync()**: Método para abrir el selector de archivos y cargar un XML
- **ParsearEstadoCuentaXml()**: Método privado que parsea el contenido XML y extrae los datos
- Propiedades observables para binding con la UI (EstadoCuenta, Transacciones, IsLoading, etc.)

### 3. Vista

#### `EsCuentaView.xaml`
Interfaz de usuario que incluye:
- Botón "Cargar Archivo XML" con atajo de teclado (Ctrl+O)
- Indicador de carga (ProgressRing)
- Mensajes de error y éxito (InfoBar)
- Sección de información general del estado de cuenta
- Lista de transacciones con formato detallado

### 4. Converters

Se crearon dos nuevos converters para la UI:
- **BoolNegationConverter**: Invierte valores booleanos
- **NullToBoolConverter**: Convierte null a false y no-null a true

## Formato del XML Esperado

El XML debe tener la siguiente estructura:

```xml
<?xml version="1.0" encoding="utf-8"?>
<EstadoCuenta>
    <NumeroCuenta>1234567890</NumeroCuenta>
    <Titular>Juan Pérez López</Titular>
    <Banco>Banco Ejemplo</Banco>
    <Sucursal>Sucursal Centro</Sucursal>
    <Periodo>Enero 2024</Periodo>
    <FechaInicio>2024-01-01</FechaInicio>
    <FechaFin>2024-01-31</FechaFin>
    <SaldoInicial>10000.00</SaldoInicial>
    <SaldoFinal>8500.50</SaldoFinal>
    <TotalCargos>2500.00</TotalCargos>
    <TotalAbonos>1000.50</TotalAbonos>
    <Transacciones>
        <Transaccion>
            <Fecha>2024-01-05</Fecha>
            <Descripcion>Compra en supermercado</Descripcion>
            <Monto>-500.00</Monto>
            <Tipo>Cargo</Tipo>
            <Saldo>9500.00</Saldo>
            <Referencia>REF123456</Referencia>
        </Transaccion>
        <Transaccion>
            <Fecha>2024-01-10</Fecha>
            <Descripcion>Depósito en sucursal</Descripcion>
            <Monto>1000.50</Monto>
            <Tipo>Abono</Tipo>
            <Saldo>10500.50</Saldo>
            <Referencia>REF789012</Referencia>
        </Transaccion>
    </Transacciones>
</EstadoCuenta>
```

## Cómo Usar

1. **Navegar a la vista Estado de Cuenta** en la aplicación
2. **Hacer clic en "Cargar Archivo XML"** o presionar `Ctrl+O`
3. **Seleccionar un archivo XML** con el formato esperado
4. El sistema parseará automáticamente el XML y mostrará:
   - Información general del estado de cuenta
   - Lista completa de transacciones

## Características Técnicas

- **Parseo XML Robusto**: Usa `System.Xml.Linq` para parseo eficiente
- **Manejo de Errores**: Captura y muestra errores de parseo al usuario
- **UI Responsiva**: Indicadores de carga y mensajes de estado
- **Binding MVVM**: Implementación completa del patrón MVVM
- **Validación de Datos**: Conversión segura de tipos (strings a decimales)

## Personalización

### Modificar el Formato XML

Si el formato del XML bancario es diferente, editar el método `ParsearEstadoCuentaXml()` en `EsCuentaViewModel.cs`:

```csharp
// Ejemplo: Si el XML usa nombres de elementos diferentes
estadoCuenta.NumeroCuenta = raiz.Element("AccountNumber")?.Value;
estadoCuenta.Titular = raiz.Element("AccountHolder")?.Value;
```

### Agregar Nuevos Campos

1. Agregar la propiedad en `EstadoCuenta.cs` o `Transaccion.cs`
2. Agregar la lógica de parseo en `ParsearEstadoCuentaXml()`
3. Actualizar la UI en `EsCuentaView.xaml` para mostrar el nuevo campo

## Pruebas

Se ha verificado la lógica de parseo XML con un archivo de prueba que incluye:
- ✓ Información general del estado de cuenta
- ✓ Múltiples transacciones
- ✓ Parseo correcto de valores decimales
- ✓ Manejo de campos opcionales

## Notas Importantes

- El archivo XML debe estar codificado en UTF-8
- Los valores decimales deben usar punto (.) como separador decimal
- El sistema es tolerante a campos faltantes (retorna null si no existen)
- La UI se actualiza automáticamente gracias al patrón MVVM
