using System.Collections.Generic;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Catálogo interno (NO es el c_FormaPago del SAT -- un abono manual no timbra nada) de cómo
    /// llegó un pago capturado a mano. Distingue Transferencia de SPEI a propósito aunque
    /// fiscalmente ambos sean "03 Transferencia electrónica de fondos": operativamente importa
    /// para el negocio, y un pago en Efectivo nunca va a tener un movimiento bancario real que lo
    /// respalde (se excluye de "Consolidar Ingresos Manuales", ver fn_ingresos_manuales_sin_movimiento).
    /// </summary>
    public static class FormaPagoAbonoManualCatalogo
    {
        public const string Efectivo = "efectivo";
        public const string Transferencia = "transferencia";
        public const string Spei = "spei";
        public const string Cheque = "cheque";

        private static readonly Dictionary<string, string> Descripciones = new()
        {
            [Efectivo] = "Efectivo",
            [Transferencia] = "Transferencia",
            [Spei] = "SPEI",
            [Cheque] = "Cheque",
        };

        /// <summary>Opciones para poblar un selector, en el orden en que deben mostrarse.</summary>
        public static readonly IReadOnlyList<string> Claves = new[] { Transferencia, Spei, Cheque, Efectivo };

        /// <summary>
        /// c_FormaPago del SAT para el Complemento de Pago: 01 Efectivo, 02 Cheque nominativo,
        /// 03 Transferencia (incluye SPEI). Null si la clave es vacía o no está en el catálogo.
        /// </summary>
        public static string? ClaveSat(string? clave)
        {
            switch (clave?.Trim().ToLowerInvariant())
            {
                case Efectivo: return "01";
                case Cheque: return "02";
                case Transferencia:
                case Spei: return "03";
                default: return null;
            }
        }

        /// <summary>Descripción de la clave, o la clave tal cual si no está en el catálogo. "Sin especificar" si es null/vacío.</summary>
        public static string Describir(string? clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
            {
                return "Sin especificar";
            }

            return Descripciones.TryGetValue(clave.Trim().ToLowerInvariant(), out var descripcion) ? descripcion : clave;
        }
    }
}
