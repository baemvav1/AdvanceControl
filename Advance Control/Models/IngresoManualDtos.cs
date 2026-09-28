using System;
using System.Globalization;
using Advance_Control.Utilities;

namespace Advance_Control.Models
{
    /// <summary>
    /// Un abono manual (capturado a mano vía "Registrar abono") que todavía no tiene ningún
    /// movimiento bancario real vinculado -- candidato a ligar a un movimiento real desde
    /// "Consolidar Ingresos Manuales". Nunca incluye abonos en efectivo (ver backend).
    /// </summary>
    public class IngresoManualPendienteMovimientoDto
    {
        public int IdAbonoFactura { get; set; }
        public int IdFactura { get; set; }
        public string? FacturaSerie { get; set; }
        public string? FacturaFolio { get; set; }
        public string? ReceptorRfc { get; set; }
        public string? ReceptorNombre { get; set; }
        public DateTime FechaAbono { get; set; }
        public decimal MontoAbono { get; set; }
        public string? Referencia { get; set; }
        public string? Observaciones { get; set; }
        public string? FormaPago { get; set; }

        public string FacturaFolioTitulo => string.IsNullOrWhiteSpace(FacturaSerie) ? $"{FacturaFolio}" : $"{FacturaSerie}{FacturaFolio}";
        public string MontoAbonoTexto => MontoAbono.ToString("C2", new CultureInfo("es-MX"));
        public string FormaPagoTexto => FormaPagoAbonoManualCatalogo.Describir(FormaPago);
        public string ReferenciaTexto => string.IsNullOrWhiteSpace(Referencia)
            ? $"Ingreso manual ({FormaPagoTexto})"
            : $"Ingreso manual ({FormaPagoTexto}) · Ref: {Referencia}";
    }

    /// <summary>Solicitud para ligar un abono manual a un movimiento bancario.</summary>
    public class VincularIngresoManualMovimientoRequestDto
    {
        public int IdAbonoFactura { get; set; }
        public int IdMovimiento { get; set; }
    }
}
