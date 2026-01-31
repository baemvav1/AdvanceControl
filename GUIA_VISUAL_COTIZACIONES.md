# Guía Visual: Generador de Cotizaciones

## Vista Previa del Botón "Generar Cotización"

### Ubicación en la Interfaz

El botón se encuentra en la vista de **Operaciones**, dentro del pivot de **Cargos**:

```
┌─────────────────────────────────────────────────────────────┐
│  Operaciones                                                 │
│  Gestión de operaciones del sistema                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  [Filtros de búsqueda...]                                   │
│                                                              │
│  ┌───────────────────────────────────────────────────────┐ │
│  │ [▼] Operación - Cliente ABC - Equipo XYZ              │ │
│  │                                                         │ │
│  │  ┌─────────────────────────────────────────────────┐  │ │
│  │  │ [ Información ] [ Nota ] [ Acciones ] [ Cargos ]│  │ │
│  │  ├─────────────────────────────────────────────────┤  │ │
│  │  │                                                   │  │ │
│  │  │  [+ Agregar Cargo] [📄 Generar Cotización]      │  │ │
│  │  │                                                   │  │ │
│  │  │  ┌──────────────────────────────────────────┐   │  │ │
│  │  │  │ Tipo | Detalle | Proveedor | Nota | Monto│   │  │ │
│  │  │  ├──────────────────────────────────────────┤   │  │ │
│  │  │  │ Refacción | Tornillo M8 | ABC | ... | $50│   │  │ │
│  │  │  │ Servicio | Instalación | XYZ | ... |$200│   │  │ │
│  │  │  └──────────────────────────────────────────┘   │  │ │
│  │  │                                                   │  │ │
│  │  │  Total: $250.00                                  │  │ │
│  │  └───────────────────────────────────────────────┘  │ │
│  └───────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Características del Botón

**Aspecto Visual**:
- Icono: 📄 (Símbolo de documento)
- Texto: "Generar Cotización"
- Color: Acento secundario claro
- Ubicación: Junto al botón "Agregar Cargo"

**Estados**:
- Activo: Cuando hay cargos en la operación
- El botón está siempre visible, pero muestra error si no hay cargos

## Flujo de Uso

### 1. Estado Inicial
```
Usuario navega a la vista de Operaciones
         ↓
Expande una operación específica
         ↓
Selecciona el pivot "Cargos"
         ↓
Ve la lista de cargos y el botón "Generar Cotización"
```

### 2. Generación de Cotización
```
Usuario hace clic en "Generar Cotización"
         ↓
Sistema valida que existan cargos
         ↓
[SI HAY CARGOS]                    [NO HAY CARGOS]
         ↓                                  ↓
Genera PDF en background          Muestra notificación
         ↓                         "No hay cargos"
Muestra diálogo de éxito
con ruta del archivo
         ↓
[Usuario elige "Abrir"]            [Usuario elige "Cerrar"]
         ↓                                  ↓
Abre PDF con visor             Cierra diálogo
predeterminado                          ↓
                               Muestra notificación de éxito
```

### 3. Resultado Final
```
PDF generado en:
📁 Mis Documentos
  └─ 📁 Advance Control
      └─ 📁 Cotizaciones
          └─ 📄 Cotizacion_ClienteABC_20260131_220500.pdf
```

## Diálogos del Sistema

### Diálogo de Éxito
```
┌─────────────────────────────────────────────┐
│ Cotización generada                         │
├─────────────────────────────────────────────┤
│                                             │
│ La cotización se ha generado exitosamente  │
│ en:                                         │
│                                             │
│ C:\Users\...\Documents\Advance Control\    │
│ Cotizaciones\Cotizacion_ClienteABC_...pdf  │
│                                             │
│ ¿Desea abrir el archivo?                   │
│                                             │
│         [  Abrir  ]      [ Cerrar ]         │
└─────────────────────────────────────────────┘
```

### Notificación de Éxito
```
┌──────────────────────────────┐
│ ℹ Cotización generada        │
│                              │
│ La cotización PDF se ha      │
│ generado correctamente.      │
└──────────────────────────────┘
```

### Error - Sin Cargos
```
┌──────────────────────────────┐
│ ⚠ No hay cargos              │
│                              │
│ No se puede generar una      │
│ cotización porque no hay     │
│ cargos asociados a esta      │
│ operación.                   │
└──────────────────────────────┘
```

### Error - Fallo en Generación
```
┌──────────────────────────────┐
│ ❌ Error                      │
│                              │
│ Ocurrió un error al generar  │
│ la cotización. Por favor,    │
│ intente nuevamente.          │
└──────────────────────────────┘
```

## Estructura del PDF Generado

```
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║            ADVANCE CONTROL                                    ║
║            Cotización de Servicio                             ║
║                                                               ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  Información del Cliente          Información de la Operación║
║  • Cliente: ABC Corporation       • Fecha: 31/01/2026        ║
║  • Equipo: Compresor XYZ-123      • Atendido por: Juan Pérez ║
║                                   • Tipo: Correctivo          ║
║                                                               ║
║  Desglose de Cargos                                          ║
║  ┌───────────────────────────────────────────────────────┐  ║
║  │ Tipo      │ Detalle      │ Proveedor │ Nota  │ Monto │  ║
║  ├───────────────────────────────────────────────────────┤  ║
║  │ Refacción │ Tornillo M8  │ ABC Corp  │ -     │ $50.00│  ║
║  │ Servicio  │ Instalación  │ XYZ Ltd   │ Urgente│$200.00│  ║
║  │ Refacción │ Filtro aire  │ ABC Corp  │ -     │$150.00│  ║
║  └───────────────────────────────────────────────────────┘  ║
║                                                               ║
║                                        TOTAL: $400.00         ║
║                                                               ║
║  Notas Adicionales                                           ║
║  • Servicio realizado con prioridad alta                     ║
║  • Cliente requiere factura electrónica                      ║
║                                                               ║
╠═══════════════════════════════════════════════════════════════╣
║                      Página 1 de 1                            ║
╚═══════════════════════════════════════════════════════════════╝
```

## Características del PDF

### Formato y Diseño
- **Tamaño**: Letter (8.5" x 11")
- **Márgenes**: 2 cm en todos los lados
- **Fuente**: Sans-serif, tamaño 11pt
- **Colores**: 
  - Encabezado: Fondo azul claro con texto azul oscuro
  - Tabla: Filas con bordes grises claros
  - Total: Destacado en azul

### Contenido Incluido
✅ Nombre de la empresa (ADVANCE CONTROL)  
✅ Título del documento (Cotización de Servicio)  
✅ Información del cliente  
✅ Información del equipo  
✅ Fecha de la operación  
✅ Personal que atiende  
✅ Tipo de operación  
✅ Tabla detallada de cargos  
✅ Suma total  
✅ Notas adicionales  
✅ Paginación  

### Seguridad del Archivo
- ✅ Nombre sanitizado (sin caracteres especiales)
- ✅ Ubicación segura (carpeta del usuario)
- ✅ Sin información sensible expuesta
- ✅ Solo lectura para el usuario

## Ejemplo de Nomenclatura

### Formato del Nombre de Archivo
```
Cotizacion_[ClienteNombre]_[YYYYMMDD]_[HHMMSS].pdf
```

### Ejemplos Reales
```
Cotizacion_ABC_Corporation_20260131_220530.pdf
Cotizacion_XYZ_Servicios_20260131_143022.pdf
Cotizacion_Cliente_Sin_Nombre_20260131_091545.pdf
```

### Sanitización de Nombres
```
Cliente Original          →  Nombre Sanitizado
──────────────────────────────────────────────
"ABC/Corporation"        →  "ABC_Corporation"
"Client*Name?"           →  "Client_Name_"
"Test:Company"           →  "Test_Company"
"Normal Name"            →  "Normal_Name"
```

## Integración con el Sistema

### Servicios Utilizados
```
[OperacionesView] (UI)
        ↓
[OperacionesViewModel] (Lógica)
        ↓
[QuoteService] (Generación PDF)
        ↓
[LoggingService] (Registro)
```

### Flujo de Datos
```
1. Usuario → Click en botón
2. View → Valida operación
3. View → Llama ViewModel.GenerateQuoteAsync()
4. ViewModel → Valida cargos
5. ViewModel → Llama QuoteService.GenerateQuotePdfAsync()
6. QuoteService → Crea PDF con QuestPDF
7. QuoteService → Guarda archivo
8. QuoteService → Retorna ruta
9. ViewModel → Retorna resultado
10. View → Muestra diálogo
11. View → Muestra notificación
```

## Pruebas Recomendadas

### Casos de Prueba
1. ✅ Operación con múltiples cargos
2. ✅ Operación con un solo cargo
3. ✅ Operación sin cargos (debe fallar)
4. ✅ Cliente con nombre especial (caracteres)
5. ✅ Operación con notas adicionales
6. ✅ Operación sin notas adicionales
7. ✅ Generación múltiple (misma operación)
8. ✅ Apertura del PDF generado

### Validaciones
- ✅ PDF se crea en la ubicación correcta
- ✅ Nombre de archivo es válido
- ✅ Contenido del PDF es correcto
- ✅ Total calculado correctamente
- ✅ Formato profesional
- ✅ Sin errores en consola

## Solución de Problemas Comunes

### El botón no aparece
**Causa**: Vista no actualizada  
**Solución**: Cerrar y reabrir la aplicación

### "No hay cargos"
**Causa**: Operación sin cargos asociados  
**Solución**: Agregar cargos antes de generar cotización

### PDF no se abre
**Causa**: No hay lector de PDF instalado  
**Solución**: Instalar Adobe Reader o similar

### Error al generar
**Causa**: Permisos de escritura  
**Solución**: Verificar permisos en carpeta Documentos

---

**Nota**: Esta guía visual complementa la documentación técnica en `IMPLEMENTACION_GENERADOR_COTIZACIONES.md`
