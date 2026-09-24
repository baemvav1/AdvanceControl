# Sesión "complementos" — resumen para retomar

Repos involucrados:
- Cliente WinUI 3: `C:\Users\baemv\Documents\GitHub\AdvanceControl`
- API .NET 8: `C:\Users\baemv\Documents\GitHub\AdvanceControlApi`
- VPS producción: `187.124.243.107` (ver memoria `vps_access` para credenciales/comandos)

## Pendiente inmediato (por dónde retomar)

**El usuario va a hacer el deploy del API él mismo** (código ya listo y compilando, migración
130 ya aplicada en producción). Cuando avise que ya desplegó:

1. Abrir Conciliación → botón **"Complementos"** (nuevo, junto a "Conciliación de Abonos").
2. Debe proponer los 3 complementos de la factura 962 (id interno 623) contra sus movimientos
   bancarios reales:
   - Complemento folio 963 (docto id 57) — $372,383.00 — movimiento id 943 (2026-06-10)
   - Complemento folio 998 (docto id 53) — $150,000.01
   - Complemento folio 1142 (docto id 98) — $150,000.01 (parcialidad 3, deja saldo insoluto $72,382.55)
3. Aprobar y confirmar que:
   - `abonos_factura` gana 3 filas nuevas con `id_movimiento` correcto.
   - `complemento_pago_doctos.id_abono_factura` queda apuntando a esos abonos.
   - El prototipo Financiero (`ProFinancieroPage`, factura 962 hardcodeada) muestra
     `Pagado`/`Restante` coherentes.
   - "Deshacer último"/"Deshacer todo" siguen funcionando sobre estos abonos nuevos
     (bitácora con `tipo_operacion = 'complemento'`).

## Qué se construyó esta sesión (cronológico, resumen)

### 1. Fix de bug en producción (equipos/suscripciones)
Migración `126_fix_varchar_text_mismatch_equipos_contratos.sql` — mismatch varchar/text en
`fn_equipos_gestionar`/`fn_contratos_suscripcion_gestionar` que rompía la carga de equipos
disponibles al crear una suscripción.

### 2. Botón de editar en Equipos
Se agregó pese a que "sin editar" era política deliberada documentada — el usuario pidió
explícitamente revertir esa política para Equipos. Incluye scroll view (el diálogo no dejaba ver
el botón Guardar).

### 3. Overhaul de catálogos (UX)
- Todos los catálogos: expandir solo por botón (no por tap en la card).
- Paginación client-side (`PaginadorViewModel<T>`, `PaginacionFooterControl`, nuevos, reusables)
  con leyenda de rango/total.
- Autosuggest en filtros (`AutoSuggestHelper`, nuevo, reusable).
- Clientes → grupo Catálogos; Entidades → grupo Financiero (navbar).
- Dashboard: sección "Noticias" (RichTextBlock) con resumen de cambios de las últimas 3 semanas.

### 4. Complementos de Pago — generación real (CFDI Pagos 2.0)
Investigación + implementación completa de timbrado real de Complementos de Pago vía FEL Bilkon,
cubriendo los 4 casos de negocio (1 pago/1 factura, varios pagos/misma factura, parcialidad,
un pago cubre varias facturas). Piezas clave:
- `AdvanceControlApi/migrations/127_complementos_pago.sql` — tablas `complemento_pago_pagos`/
  `complemento_pago_doctos`, funciones de registro/consulta.
- `AdvanceApi/Services/CfdiPagoBuilderService.cs` — construcción/timbrado del XML (decompilado el
  paquete NuGet real con ilspycmd para verificar la forma exacta de la API).
- Cliente: `GenerarComplementoPagoDialog`, botón "Generar complemento de pago" en `FacturasPage`
  (solo visible si `PuedeGenerarComplementoPago`), renombrado el flujo viejo de "Agregar
  complemento" a **"Registrar abono"** (control interno, no timbra) para no confundir ambos
  conceptos.

### 5. Auditoría e histórico de Complementos de Pago
El negocio timbraba complementos a mano en el portal de Bilkon antes de esta feature; 73
complementos históricos nunca se parsearon a `complemento_pago_pagos/doctos` (el importador de
XML nunca leía `<pago20:Pagos>`).
- `128_complemento_pago_backfill.sql` — relaja FKs a nullable (`id_abono_factura`/
  `id_factura_pagada`), agrega columnas crudas (`uuid_docto_relacionado`, IVA), función de
  backfill idempotente, `fn_complementos_pago_listar` (con `matched` bool), `fn_factura_
  complementos_relacionados` (inversa).
- Backfill de los 73 históricos: parseo con Python (`xml.etree.ElementTree`, namespaces
  correctos — NO usar `xpath()` en PL/pgSQL, falla en silencio), verificado dos veces antes de
  aplicar directo a producción dentro de una transacción.
- Cliente: página `ComplementosPagoAuditoriaPage` (nav "Complementos de Pago" en Financiero),
  sección "Complementos de pago relacionados" en `DetailFacturaWindow`, PDF de complemento
  (`ComplementoPagoPdfService`).

### 6. Prototipos por grupo de navbar (`Pro{Grupo}`)
A petición del usuario: una página "Prototipo" por cada grupo del navbar (`ProCatalogos`,
`ProFinanciero`, etc.), visible solo nivel 1 (devs) vía el sistema de permisos ya existente
(escaneo automático, nivel_requerido se ajusta manualmente en Administración — **ojo**: nivel 8
= menos restrictivo (todos pueden entrar), nivel 1 = más restrictivo (solo devs); las páginas
nuevas se sincronizan con `nivel_requerido = 8` por defecto, hay que bajarlas a mano).

### 7. Prototipo Financiero — visor de factura (candidato a reemplazar FacturasPage/DetailFacturaWindow)
`ProFinancieroPage` + `ProFinancieroViewModel`: visor de PDF embebido (extraído a
`Utilities/PdfPreviewRenderer.cs`, reusa el mecanismo de `CotizacionVisorDialog` con
`Windows.Data.Pdf`), mecanismo genérico por serie+folio pero hardcodeado a la **factura 962**
(cambiada desde A7 tras encontrar un caso real con complementos). Panel derecho: KPIs
Total/Pagado/Restante (Pagado/Restante ahora se calculan desde los Complementos de Pago
ligados cuando existen, no solo desde `abonos_factura`), botones Registrar Abono/Generar
Complemento/Enviar Historial (deshabilitado, sin backend)/Cancelar Factura, lista de
Complementos de pago relacionados con botón PDF y "eliminar" (en realidad cancela el CFDI del
complemento, reusando `CancelarCfdiDialog`).

### 8. Rediseño del PDF de factura (aplicado a `FacturaPdfService` y `ComplementoPagoPdfService`)
Investigación de qué es legalmente obligatorio en una representación impresa de CFDI (CFF
Art. 29-A + Regla 2.7.1.7 RMF + Anexo 20) para decidir qué recortar. Cambios:
- Logo (`Assets/Logos/AdvanceElevadoresLogo.png`) a la izquierda, datos fiscales (Folio, UUID,
  certificados, fechas, lugar de expedición) a la derecha alineados a la derecha, etiquetas en
  azul (`Colors.Blue.Darken2`).
- Direcciones completas de Emisor/Receptor vía `IEntidadService` (no vienen en el XML del CFDI,
  es domicilio registrado en el catálogo de Entidades) + régimen fiscal/uso CFDI con descripción
  real (`ISatCatalogoService`), en vez del código pelón.
- Forma de pago legible (`Utilities/SatFormaPagoCatalogo.cs`, catálogo SAT c_FormaPago abreviado).
- Se quitaron duplicados: RFC proveedor de certificación, banner de Folio fiscal, Tipo de
  comprobante, Fecha emisión/timbrado repetidas — reordenado: Emisor/Receptor → Conceptos →
  Totales → Traslados → Datos generales (reducido) → Timbre fiscal (reducido).
- Se quitó el texto "Página X de Y" que aparecía en el visor en pantalla (no en el PDF impreso)
  tanto en `PdfPreviewRenderer.cs` como en `CotizacionVisorDialog.xaml.cs`.

### 9. Bug crítico de producción: todas las cargas de facturas fallaban
Diagnosticado en vivo (no era el VPS, pese a que el usuario lo reinició): la migración 125
(`facturas_iguala`) pisó mal la función `fn_facturas_guardar_completa` sin `DROP FUNCTION`
previo (lección ya documentada en `feedback_pg_overload_drop` pero no aplicada), dejando DOS
sobrecargas ambiguas en producción. El API llamaba con parámetros que no calzaban con ninguna.
Fix: `129_fix_overload_guardar_factura_completa.sql` — elimina ambas sobrecargas vía catálogo
(`pg_proc`), crea una sola función fusionada. Verificado en producción con `BEGIN/ROLLBACK`
antes de commitear.

### 10. Backfill de 15 complementos nuevos (llegaron con la recarga de FEL tras el fix)
Mismo mecanismo del punto 5, aplicado a los 15 complementos nuevos (folios 1136–1200) que
trajo la recarga de FEL una vez arreglado el bug de la 129. Uno de ellos (folio 1142, id 856)
es la 3ª parcialidad de la factura 962.

### 11. Feature grande: botón "Complementos" en Conciliación (ESTA ES LA ÚLTIMA, la que falta desplegar)
Liga Complementos de Pago (ya timbrados, sin abono interno registrado) a movimientos bancarios
por monto + fecha, reusando el motor de matching ya existente (`ConciliacionMatchingEngine`,
regla PPD "mismo mes o posterior"). Detalle completo en la sección "Pendiente inmediato" arriba.
Piezas:
- `AdvanceControlApi/migrations/130_complemento_pago_vincular_movimiento.sql` — **ya aplicada
  en producción y probada** (`fn_complementos_pago_sin_movimiento`, `fn_complemento_pago_
  vincular_movimiento`, esta última reusa `fn_facturas_registrar_abono`).
- API: `ComplementoPagoPendienteMovimientoDto`, `VincularComplementoMovimientoRequestDto`,
  endpoints `GET api/factura/complementos-pago/pendientes-movimiento` y
  `POST api/factura/complemento-pago/vincular-movimiento` — **código listo, falta deploy**.
- Cliente: `ConciliacionAutomaticaModo.Complementos`, `CrearPropuestasComplementosAsync` en
  `ConciliacionAutomaticaWindowViewModel.cs`, botón "Complementos" en `ConciliacionPage.xaml`,
  plantilla actualizada en `ConfirmacionConciliacionUserControl.xaml`.

## Archivos clave tocados esta sesión (no exhaustivo, los más relevantes)

**API** (`AdvanceControlApi/`):
- `migrations/126...129.sql`, `migrations/130_complemento_pago_vincular_movimiento.sql`
- `AdvanceApi/Services/FacturaService.cs`, `AdvanceApi/Controllers/FacturaController.cs`
- `AdvanceApi/Services/CfdiPagoBuilderService.cs` (nuevo)
- `AdvanceApi/DTOs/ComplementoPagoDtos.cs`, `ComplementoPagoPendienteMovimientoDto.cs` (nuevo)

**Cliente** (`Advance Control/`):
- `Views/Pages/ProFinancieroPage.xaml(.cs)`, `ViewModels/ProFinancieroViewModel.cs` (nuevos)
- `Views/Pages/ComplementosPagoAuditoriaPage.xaml(.cs)`, `ViewModels/ComplementosPagoAuditoriaViewModel.cs` (nuevos)
- `Services/Facturas/FacturaPdfService.cs`, `ComplementoPagoPdfService.cs` (rediseño cabecera)
- `Utilities/PdfPreviewRenderer.cs`, `SatFormaPagoCatalogo.cs` (nuevos)
- `ViewModels/ConciliacionAutomaticaWindowViewModel.cs` (botón Complementos)
- `Views/Pages/ConciliacionPage.xaml(.cs)`, `Views/Dialogs/ConfirmacionConciliacionUserControl.xaml`
- `Models/ConciliacionMatchPropuestaDto.cs`, `ConciliacionAutomaticaModo.cs`, `ComplementoPagoDtos.cs`
- `Assets/Logos/AdvanceElevadoresLogo.png` (nuevo)

## Notas / lecciones para no repetir errores

- **Nivel de permisos**: 8 = menos restrictivo (todos), 1 = más restrictivo (solo devs) —
  confundido una vez esta sesión, corregido.
- **Nunca `CREATE OR REPLACE FUNCTION` en Postgres sin `DROP FUNCTION IF EXISTS` previo** si la
  firma de parámetros puede diferir aunque sea un poco — crea sobrecargas ambiguas silenciosas
  (causó el incidente del punto 9).
- **WinUI + async**: nunca usar `.ConfigureAwait(false)` en ViewModels/servicios que terminan
  actualizando bindings de UI — rompe con `COMException` (hilo equivocado). Causó un bug esta
  sesión en `ProFinancieroViewModel`/`PdfPreviewRenderer`.
- Direcciones completas de Entidades/complementos no vienen en el XML del CFDI (el SAT solo
  exige CP) — es dato del catálogo interno de Entidades, no del comprobante.
