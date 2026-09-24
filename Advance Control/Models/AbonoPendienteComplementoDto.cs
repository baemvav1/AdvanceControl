using System;
using System.Globalization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Un abono de una factura PPD propia que todavía no entró a ningún
    /// Complemento de Pago real. Fuente del checklist en GenerarComplementoPagoDialog.
    /// </summary>
    public class AbonoPendienteComplementoDto
    {
        public int IdAbonoFactura { get; set; }
        public int IdFactura { get; set; }
        public string? Serie { get; set; }
        public string? Folio { get; set; }
        public string? Uuid { get; set; }
        public string Moneda { get; set; } = "MXN";
        public decimal Total { get; set; }
        public DateTime FechaAbono { get; set; }
        public decimal MontoAbono { get; set; }
        public int? IdMovimiento { get; set; }
        public string? Referencia { get; set; }
        public int NumParcialidad { get; set; }
        public decimal ImpSaldoAnt { get; set; }
        public decimal ImpSaldoInsoluto { get; set; }

        public string FolioTitulo => string.IsNullOrWhiteSpace(Serie) ? $"{Folio}" : $"{Serie}{Folio}";
        public string MontoAbonoTexto => MontoAbono.ToString("C2", new CultureInfo("es-MX"));
        public string FechaAbonoTexto => FechaAbono.ToString("dd/MM/yyyy");
        public string DescripcionTexto => $"Factura {FolioTitulo} · Parcialidad {NumParcialidad} · {MontoAbonoTexto} · {FechaAbonoTexto}";
    }
}
