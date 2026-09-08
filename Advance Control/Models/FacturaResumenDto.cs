using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class FacturaResumenDto
    {
        public int IdFactura { get; set; }
        public string VersionXml { get; set; } = "4.0";
        public string? Serie { get; set; }
        public string? Folio { get; set; }
        public DateTime Fecha { get; set; }
        public string? FormaPago { get; set; }
        public string? NoCertificado { get; set; }
        public string? CondicionesDePago { get; set; }
        public decimal SubTotal { get; set; }
        public string Moneda { get; set; } = "MXN";
        public decimal Total { get; set; }
        public string? TipoDeComprobante { get; set; }
        public string? Exportacion { get; set; }
        public string? MetodoPago { get; set; }
        public string? LugarExpedicion { get; set; }
        public decimal TotalImpuestosTrasladados { get; set; }
        public string? EmisorRfc { get; set; }
        public string? EmisorNombre { get; set; }
        public string? EmisorRegimenFiscal { get; set; }
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public string? ReceptorDomicilioFiscal { get; set; }
        public string? ReceptorRegimenFiscal { get; set; }
        public string? ReceptorUsoCfdi { get; set; }
        public string? Uuid { get; set; }
        public DateTime? FechaTimbrado { get; set; }
        public string? RfcProvCertif { get; set; }
        public string? NoCertificadoSat { get; set; }
        public bool? Finiquito { get; set; }
        public decimal TotalAbonado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int NumeroAbonos { get; set; }
        public DateTime? FechaUltimoAbono { get; set; }
        public int? IdOperacion { get; set; }
        public bool Cancelada { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public string? MotivoCancelacion { get; set; }
        public string? UuidSustitucion { get; set; }

        /// <summary>Solo viene poblado al consultar el detalle de UNA factura, no en el listado.</summary>
        public string? Sello { get; set; }
        public string? SelloCfd { get; set; }
        public string? SelloSat { get; set; }
        public string? AcuseCancelacionXml { get; set; }

        public string FolioTitulo => string.IsNullOrWhiteSpace(Serie) ? $"{Folio}" : $"{Serie}{Folio}";

        public bool EsComplementoPago => string.Equals(TipoDeComprobante, "P", StringComparison.OrdinalIgnoreCase);
        public bool EsConSerie => !EsComplementoPago && !string.IsNullOrWhiteSpace(Serie);
        public bool EsSinSerie => !EsComplementoPago && string.IsNullOrWhiteSpace(Serie);
        public string TipoFacturaTexto => EsComplementoPago
            ? "Complemento de pago"
            : EsConSerie
                ? "Serie + folio"
                : "Folio";

        /// <summary>
        /// Solo las facturas generadas por el software (Serie A + folio propio) se gestionan aquí.
        /// Las de folio suelto y los complementos de pago se hicieron/hacen desde el portal de Bilkon;
        /// se cancelan y se les capturan pagos allá, para no perder el hilo de esos movimientos.
        /// </summary>
        public bool PermiteGestionInterna => EsConSerie;

        public string TooltipCapturarPagoTexto => PermiteGestionInterna
            ? "Agregar complemento de pago"
            : "Esta factura no fue generada por el software; captura su pago desde el portal de Bilkon";

        /// <summary>Solo se puede cancelar una factura propia (Serie+Folio), con UUID timbrado y que no esté ya cancelada.</summary>
        public bool PuedeCancelarCfdi => PermiteGestionInterna && !Cancelada && !string.IsNullOrWhiteSpace(Uuid);

        public string TooltipCancelarTexto => Cancelada
            ? "Esta factura ya está cancelada"
            : PermiteGestionInterna
                ? "Cancelar CFDI ante el SAT"
                : "Esta factura no fue generada por el software; cancélala desde el portal de Bilkon";

        public string EstadoCancelacionTexto => Cancelada
            ? $"Cancelada ({FechaCancelacion:dd/MM/yyyy})"
            : "Vigente";

        public string SugerenciaTexto
        {
            get
            {
                var uuidCorto = string.IsNullOrWhiteSpace(Uuid)
                    ? "sin UUID"
                    : (Uuid.Length > 8 ? Uuid[..8] + "…" : Uuid);
                return $"{FolioTitulo} · {ReceptorNombre ?? "Sin receptor"} · {uuidCorto}";
            }
        }

        public string FechaTexto => Fecha == default ? string.Empty : Fecha.ToString("dd/MM/yyyy HH:mm");
        public string EmisorReceptorTexto => $"{EmisorNombre ?? "Sin emisor"} -> {ReceptorNombre ?? "Sin receptor"}";
        public string RfcTexto => $"{ReceptorRfc ?? "Sin RFC"}";
        public string TotalesTexto => $"Subtotal {SubTotalTexto} · IVA/Impuestos {TotalImpuestosTexto} · Total {TotalTexto}";
        public string MetodoFormaPagoTexto => $"{MetodoPago ?? "Sin metodo"} · {FormaPago ?? "Sin forma"}";
        public string UuidTexto => string.IsNullOrWhiteSpace(Uuid) ? "Sin UUID" : $"UUID: {Uuid}";
        public string SubTotalTexto => SubTotal.ToString("C2", new CultureInfo("es-MX"));
        public string TotalTexto => Total.ToString("C2", new CultureInfo("es-MX"));
        public string TotalImpuestosTexto => TotalImpuestosTrasladados.ToString("C2", new CultureInfo("es-MX"));
        public string LugarExpedicionTexto => string.IsNullOrWhiteSpace(LugarExpedicion) ? "Sin lugar de expedicion" : $"CP {LugarExpedicion}";
        public string TotalAbonadoTexto => TotalAbonado.ToString("C2", new CultureInfo("es-MX"));
        public string SaldoPendienteTexto => SaldoPendiente.ToString("C2", new CultureInfo("es-MX"));
        public string EstadoPagoTexto => Finiquito == true ? "Pagada" : SaldoPendiente <= 0 ? "Sin saldo" : "Pendiente";
        public string FechaUltimoAbonoTexto => FechaUltimoAbono.HasValue ? FechaUltimoAbono.Value.ToString("dd/MM/yyyy HH:mm") : "Sin abonos";
    }
}
