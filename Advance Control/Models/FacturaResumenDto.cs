using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class FacturaResumenDto
    {
        public int IdFactura { get; set; }
        public string VersionXml { get; set; } = "4.0";
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

        public string FolioTitulo => string.IsNullOrWhiteSpace(Folio)
            ? $"Factura {Uuid ?? "sin folio"}"
            : $"Factura {Folio}";

        public string FechaTexto => Fecha == default ? string.Empty : Fecha.ToString("dd/MM/yyyy HH:mm");
        public string EmisorReceptorTexto => $"{EmisorNombre ?? "Sin emisor"} -> {ReceptorNombre ?? "Sin receptor"}";
        public string RfcTexto => $"{EmisorRfc ?? "Sin RFC"} / {ReceptorRfc ?? "Sin RFC"}";
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
