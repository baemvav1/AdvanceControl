# Contratos de Suscripción (Oro/Plata/Bronce)

Reporte de la funcionalidad de contratos de suscripción implementada en Advance Control: generación de contratos de mantenimiento recurrente, firma digital del proceso documental, y timbrado de la iguala mensual sin salir de la app.

## Contexto / motivación

El cargo recurrente mensual de clientes como "Servicios Externos para Hoteles" ($46,906.99/mes) es la **iguala** de un contrato de mantenimiento que hasta ahora no tenía ningún respaldo en el sistema: se facturaba a mano en el portal externo de Bilkon, sin contrato firmado registrado, sin PDF, sin nada trazable.

Esta funcionalidad cubre el ciclo completo:

```
Seleccionar nivel (Oro/Plata/Bronce)
        ↓
Llenar formulario (equipos, monto, vigencia, firmante)
        ↓
Finalizar → guarda en DB + genera PDF + sube al VPS
        ↓
Cargar documento firmado escaneado
        ↓
Timbrar la factura de la iguala mensual (reutiliza FEL Bilkon)
```

## Migraciones aplicadas en producción

| Migración | Contenido |
|---|---|
| `123_equipos_datos_tecnicos.sql` | `equipos` gana 5 columnas: `controlador`, `tipo_puerta`, `velocidad`, `tipo_maquina`, `operador` (para cubrir la tabla de equipos del machote) |
| `124_contratos_suscripcion.sql` | Tablas `contratos_suscripcion` y `contrato_suscripcion_equipos`; funciones `fn_contratos_suscripcion_gestionar` y `fn_contrato_suscripcion_marcar_firmado` |
| `125_facturas_iguala.sql` | `facturas` gana `id_contrato_suscripcion` y `periodo_iguala` + índice único parcial (evita timbrar la iguala dos veces el mismo mes); `fn_facturas_guardar_completa` extendida |

Las 3 se aplicaron y verificaron contra la base de producción del VPS (187.124.243.107) el 2026-09-18, registradas en `migration_history`.

## Modelo de datos

**`contratos_suscripcion`**: `id_cliente`, `nivel` (Oro/Plata/Bronce), `numero_contrato`, `direccion_instalacion`, `monto_mensual`, `numero_unidades`, `vigencia_inicio/fin`, `nombre_firmante`, `telefono_firmante`, `fecha_firma`, `estatus` (`generado` → `firmado`), `pdf_generado_url`, `pdf_firmado_url`.

**`contrato_suscripcion_equipos`**: tabla puente — un contrato es **consolidado por cliente** y puede cubrir equipos de varias ubicaciones del mismo cliente.

**`facturas`**: una factura de iguala es una factura normal con `id_operacion = NULL`, marcada con `id_contrato_suscripcion` + `periodo_iguala` (mes). El índice único impide duplicados.

## Backend (AdvanceControlApi)

- `Controllers/ContratosSuscripcionController.cs` — CRUD de contratos (`GET/POST/PUT /api/contratos-suscripcion`, `POST .../{id}/marcar-firmado`).
- `Services/ContratoSuscripcionService.cs` / `IContratoSuscripcionService.cs`.
- `Controllers/UploadsController.cs` — nueva sección `contratos`: `POST/GET api/uploads/contratos/{idContrato}?tipo=generado|firmado`, guarda en `storage/contratos/{idContrato}/{idContrato}_Generado.pdf` / `..._Firmado.pdf`.
- `Controllers/EquipoCrudController.cs`, `Services/EquipoService.cs`, `Clases/Equipo.cs`, `DTOs/EquipoQueryDto.cs` — extendidos con los 5 campos técnicos nuevos.
- `Services/FacturaService.cs` — nuevo método `TimbrarIgualaMensualAsync(idContrato, request, periodo)`, endpoint `POST api/factura/contrato-suscripcion/{idContrato}/timbrar-iguala?periodo=YYYY-MM`. Reutiliza tal cual `ICfdiBuilderService.ConstruirYSellarAsync` → `IFelBilkonClient.TimbrarCfdiAsync` → `fn_facturas_guardar_completa`, el mismo pipeline que ya usa el timbrado directo de operaciones.

## Cliente (WinUI)

- **Pivot "Suscripción"** en la ficha de cada cliente (`Views/Pages/ClientesPage.xaml`):
  - Sin contrato → botón **"Generar contrato"**.
  - Contrato generado → resumen (nivel, monto, vigencia, estatus), link al PDF, botón **"Cargar documento firmado"**.
  - Contrato firmado → además botón **"Generar factura de iguala del mes"**.
- **`SeleccionarNivelSuscripcionUserControl`** — modal con 3 `RadioButton` (Oro/Plata/Bronce).
- **`GenerarContratoUserControl`** — formulario: número de contrato, dirección de instalación, selector múltiple de equipos (checklist con buscador), monto mensual, número de unidades (autocalculado, editable), vigencia inicio/fin, nombre/teléfono del firmante, fecha de firma. Botón "Finalizar" orquesta: guardar contrato → generar PDF → subir al VPS → guardar la URL.
- **`ContratoPdfService`** (QuestPDF) + **`ContratoTextosNivel`** — reproducen el texto legal **exacto** de los 3 machotes (`Machotes contratos\Machote_{Oro|Plata|Bronce}_Modificado.md`): cláusulas 1ª–11ª, tabla de equipos, condiciones contractuales, horarios, firma. Solo cambian por nivel la Cláusula Cuarta (cobertura de refacciones), la Cláusula Quinta (Bronce no incluye "Conferencias Educativas Gratuitas") y la mención del número de póliza en la Cláusula Novena (solo Bronce).
- **`NumeroALetrasHelper`** — convierte montos y unidades a letras para el texto legal (p. ej. "$46,906.99 (CUARENTA Y SEIS MIL NOVECIENTOS SEIS PESOS 99/100) MN").
- **`IContratoDocumentoService`/`RemoteContratoDocumentoService`** — sube generado/firmado al VPS (patrón Local+Remote ya usado para imágenes de operación).
- **`TimbrarIgualaWindow`** — ventana ligera (un solo concepto, a diferencia de `FacturarDirectoWindow` que maneja varios) para timbrar la iguala: muestra emisor/receptor, deja elegir clave SAT de producto/servicio y unidad (buscador contra el catálogo SAT), forma/método de pago, y timbra con un clic reutilizando FEL Bilkon.

## Decisiones tomadas con el usuario

1. Solo el **mecanismo manual** de timbrado (botón); no hay automatización/cron mensual en esta fase.
2. El formulario del contrato **selecciona equipos ya existentes** del cliente (se muestra el listado completo de equipos activos con buscador, no hay un vínculo cliente↔equipo confiable en el esquema actual para pre-filtrar).
3. Un contrato es **consolidado por cliente** (no uno por ubicación/propiedad).
4. Texto legal **fijo por nivel**, tal cual los machotes — el formulario solo llena los campos variables.

## Estado

- Ambos repos compilan sin errores (`dotnet build`, 0 errores).
- Migraciones 123-125 aplicadas y verificadas en producción.
- Commits locales creados en ambos repos (API: `55ead09`; cliente: `80b958d`) — **sin push**.
- Pendiente: build de release / deploy del binario de la API al VPS para que los endpoints nuevos queden activos en producción (el schema ya está listo, pero el proceso corriendo sigue siendo el binario anterior hasta el próximo deploy).
- Prueba manual end-to-end en la app (generar contrato de prueba, subir firmado, timbrar iguala) queda pendiente — no se puede ejercer la UI de WinUI desde aquí.
