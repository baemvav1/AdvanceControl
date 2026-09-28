# Pendientes — retomar más adelante

Reportados en vivo el 2026-09-28, justo después de liberar el nuevo visor de factura
(Complementos/Pagos, "Consolidar Ingresos Manuales", fix de complementos en facturas sin serie).

## 1. Excepción no controlada al cambiar de tab "Pagos" → "Complementos"

En `ProFinancieroPage` (visor de factura), cambiar del tab **Pagos** al tab **Complementos**
lanza una excepción no controlada. No se investigó el stack trace todavía (no se pudo reproducir
desde aquí, no hay forma de ejercer la UI de WinUI en este entorno).

**Hipótesis a revisar primero:**
- `Pivot` puede estar re-realizando el `ItemsRepeater` de Complementos (`ComplementosRepeater`)
  al volver a seleccionarlo, y algo en su `DataTemplate` (`x:DataType="models:ComplementoPagoRelacionadoDto"`)
  falla con los datos ya cargados por `ViewModel.ComplementosRelacionados`.
- Revisar si el problema es específico del `Pivot` nuevo (agregado en el commit "Pestañas
  Complementos/Pagos en el visor de factura") o si ya existía antes y solo se hizo visible al
  encapsular la lista en tabs.

**Archivos:** `Advance Control/Views/Pages/ProFinancieroPage.xaml` (los dos `PivotItem`),
`Advance Control/Views/Pages/ProFinancieroPage.xaml.cs`.

**Para retomar:** reproducir localmente con el depurador de Visual Studio adjunto (Output ›
Excepciones no controladas) para tener el stack trace real antes de tocar nada.

## 2. "Generar complemento de pago" en factura 1032 (id interno 699) no timbra

Al confirmar el diálogo de generar complemento para la factura 1032, no pasa nada visible en la
UI. Preocupación inicial: que se hubiera generado algo a medias. **Ya descartado.**

**Investigado (2026-09-28):**
- El log de la API confirma que **Bilkon rechazó el timbrado** antes de generar nada:
  ```
  FEL Bilkon rechazó el timbrado del complemento de pago: CRP20104
  AdvanceApi.Services.FelBilkonException: El valor del campo Moneda debe ser "XXX".
     en AdvanceApi.Services.FacturaService.GenerarComplementoPagoAsync
  ```
- Verificado en base de datos (VPS, 2026-09-28 ~12:00 UTC): **cero** filas nuevas en `facturas`
  con `tipo_de_comprobante='P'` y **cero** filas nuevas en `complemento_pago_pagos` en la última
  hora. El rechazo fue limpio — no quedó nada a medias, la factura 1032 no se tocó.
- **Causa probable:** `CfdiPagoBuilderService` (`AdvanceControlApi/AdvanceApi/Services/CfdiPagoBuilderService.cs`)
  no está fijando `Moneda = "XXX"` a nivel de Comprobante para los CFDI tipo Complemento de Pago —
  regla obligatoria del SAT para Pagos 2.0 (el importe real vive en el nodo `<Pago>`, la moneda del
  comprobante siempre debe ser "XXX" independientemente de la moneda real del pago).
- **Segundo problema encontrado (UI):** aunque el error sí debería llegar de vuelta al cliente
  (`ProFinancieroViewModel.GenerarComplementoPagoAsync` actualiza `ErrorMessage` cuando
  `!resultado.Success`), en pantalla "no pasa nada" visible. Revisar si el `TextBlock` de error en
  `ProFinancieroPage.xaml` queda tapado por el visor de PDF (comparten el mismo `Grid.Row`), o si
  el mensaje no se está propagando por alguna otra razón.

**Para retomar:**
1. Corregir `CfdiPagoBuilderService` para fijar `Moneda="XXX"` en el nodo Comprobante de los
   Complementos de Pago.
2. Probar de nuevo con la factura 1032 (ya confirmado que no hay nada pendiente/huérfano que
   limpiar primero).
3. Asegurar que un rechazo de Bilkon se vea claramente en la UI de `ProFinancieroPage` (no
   silenciosamente).

**Archivos:** `AdvanceControlApi/AdvanceApi/Services/CfdiPagoBuilderService.cs`,
`AdvanceControlApi/AdvanceApi/Services/FacturaService.cs` (`GenerarComplementoPagoAsync`),
`Advance Control/Views/Pages/ProFinancieroPage.xaml` (visualización de `ErrorMessage`).
