using System;
using System.Globalization;
using Advance_Control.Utilities;

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

        /// <summary>Cómo llegó el pago según "Registrar abono" (efectivo/transferencia/spei/cheque); null si no se capturó.</summary>
        public string? FormaPago { get; set; }

        /// <summary>c_FormaPago del SAT que corresponde a <see cref="FormaPago"/>, o null si no se sabe.</summary>
        public string? FormaPagoSat => FormaPagoAbonoManualCatalogo.ClaveSat(FormaPago);

        public string FolioTitulo => string.IsNullOrWhiteSpace(Serie) ? $"{Folio}" : $"{Serie}{Folio}";
        public string MontoAbonoTexto => MontoAbono.ToString("C2", new CultureInfo("es-MX"));
        public string FechaAbonoTexto => FechaAbono.ToString("dd/MM/yyyy");
        public string DescripcionTexto => string.IsNullOrWhiteSpace(FormaPago)
            ? $"Factura {FolioTitulo} · Parcialidad {NumParcialidad} · {MontoAbonoTexto} · {FechaAbonoTexto}"
            : $"Factura {FolioTitulo} · Parcialidad {NumParcialidad} · {MontoAbonoTexto} · {FechaAbonoTexto} · {FormaPagoAbonoManualCatalogo.Describir(FormaPago)}";
    }
}
