using System.Collections.Generic;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Catálogo SAT c_FormaPago, con una descripción abreviada (para no ocupar demasiado
    /// espacio en listas y PDFs) en vez del texto oficial completo.
    /// </summary>
    public static class SatFormaPagoCatalogo
    {
        private static readonly Dictionary<string, string> Descripciones = new()
        {
            ["01"] = "Efectivo",
            ["02"] = "Cheque nominativo",
            ["03"] = "Transferencia",
            ["04"] = "Tarjeta de crédito",
            ["05"] = "Monedero electrónico",
            ["06"] = "Dinero electrónico",
            ["08"] = "Vales de despensa",
            ["12"] = "Dación en pago",
            ["13"] = "Subrogación",
            ["14"] = "Consignación",
            ["15"] = "Condonación",
            ["17"] = "Compensación",
            ["23"] = "Novación",
            ["24"] = "Confusión",
            ["25"] = "Remisión de deuda",
            ["26"] = "Prescripción/caducidad",
            ["27"] = "A satisfacción del acreedor",
            ["28"] = "Tarjeta de débito",
            ["29"] = "Tarjeta de servicios",
            ["30"] = "Aplicación de anticipos",
            ["31"] = "Intermediario de pagos",
            ["99"] = "Por definir",
        };

        /// <summary>Descripción abreviada de la clave, o la clave tal cual si no está en el catálogo.</summary>
        public static string Describir(string? clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
            {
                return "-";
            }

            return Descripciones.TryGetValue(clave.Trim(), out var descripcion) ? descripcion : clave;
        }
    }
}
