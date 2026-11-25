using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>Cuenta bancaria propia de una entidad; liga los estados de cuenta con su razón social.</summary>
    public class EntidadCuentaBancariaDto
    {
        [JsonPropertyName("idCuenta")]
        public int IdCuenta { get; set; }

        [JsonPropertyName("idEntidad")]
        public int IdEntidad { get; set; }

        [JsonPropertyName("numeroCuenta")]
        public string NumeroCuenta { get; set; } = string.Empty;

        [JsonPropertyName("clabe")]
        public string? Clabe { get; set; }

        [JsonPropertyName("banco")]
        public string? Banco { get; set; }

        /// <summary>Estados de cuenta ya cargados de esta cuenta.</summary>
        [JsonPropertyName("estadosCuenta")]
        public int EstadosCuenta { get; set; }

        /// <summary>
        /// Número comparable: solo dígitos y sin ceros a la izquierda (igual que
        /// fn_cuenta_normalizada en la BD), para cruzar con el número de un estado de cuenta.
        /// </summary>
        public static string Normalizar(string? cuenta)
        {
            if (string.IsNullOrWhiteSpace(cuenta)) return string.Empty;
            var digitos = new System.Text.StringBuilder();
            foreach (var c in cuenta)
                if (char.IsDigit(c)) digitos.Append(c);
            return digitos.ToString().TrimStart('0');
        }
    }

    /// <summary>Entidad con la que se factura a un cliente (la última usada, o la predeterminada).</summary>
    public class EntidadFacturacionClienteDto
    {
        [JsonPropertyName("idCliente")]
        public int? IdCliente { get; set; }

        /// <summary>RFC del cliente (solo en el listado de todos los clientes).</summary>
        [JsonPropertyName("rfc")]
        public string? Rfc { get; set; }

        [JsonPropertyName("idEntidad")]
        public int? IdEntidad { get; set; }

        /// <summary>True si viene del cliente (ya se le timbró antes con esa entidad).</summary>
        [JsonPropertyName("guardada")]
        public bool Guardada { get; set; }
    }
}
