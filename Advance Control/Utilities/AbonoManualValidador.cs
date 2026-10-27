using System;
using Advance_Control.Models;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Validación de "Registrar abono" compartida por RegistrarComplementoPagoDialog y la ventana de
    /// detalle de factura. La BD rechaza igual monto/saldo/cancelada (fn_facturas_registrar_abono);
    /// validarlo aquí deja corregir sin cerrar el diálogo.
    /// </summary>
    public static class AbonoManualValidador
    {
        /// <summary>Mensaje para el usuario si el abono no se puede registrar; null si está bien.</summary>
        public static string? Validar(FacturaResumenDto factura, decimal monto, DateTimeOffset? fecha, string? formaPago, DateTime hoy)
        {
            if (!factura.PuedeCapturarPagoManual)
                return factura.TooltipCapturarPagoTexto;

            if (monto <= 0m)
                return "Captura un monto válido mayor que cero.";

            // abonos_factura y el complemento (ImpPagado) usan 2 decimales; la BD redondearía en silencio.
            if (decimal.Round(monto, 2) != monto)
                return "El monto admite máximo 2 decimales.";

            if (factura.SaldoPendiente <= 0m)
                return "Esta factura ya no tiene saldo pendiente.";

            if (monto > factura.SaldoPendiente)
                return $"El monto no puede exceder el saldo pendiente ({factura.SaldoPendienteTexto}).";

            if (fecha == null)
                return "Selecciona la fecha del pago.";

            // La fecha del abono es la FechaPago del Complemento de Pago: no puede ser futura.
            if (fecha.Value.Date > hoy.Date)
                return "La fecha del pago no puede ser futura.";

            if (FormaPagoAbonoManualCatalogo.ClaveSat(formaPago) == null)
                return "Selecciona cómo llegó el pago (efectivo, transferencia, SPEI o cheque).";

            return null;
        }

        /// <summary>
        /// Mediodía del día elegido: el VPS guarda/lee en UTC y la PC muestra en hora de México
        /// (-6 h); con la hora real, un abono capturado de madrugada (o una fecha elegida a las
        /// 00:00) regresaría con el día anterior y así saldría en el Complemento de Pago.
        /// </summary>
        public static DateTime FechaParaGuardar(DateTimeOffset fecha) => fecha.Date.AddHours(12);
    }
}
