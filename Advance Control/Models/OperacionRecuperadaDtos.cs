using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Alta de una operación antigua ("Op Recuperada"), sin orden de servicio
    /// (POST api/Operaciones/recuperada, migración 142).
    /// </summary>
    public class OperacionRecuperadaCreateDto
    {
        /// <summary>Número original de la operación (alfanumérico). Se muestra como "R" + este valor.</summary>
        public string IdTrasladado { get; set; } = string.Empty;
        public int IdTipo { get; set; }
        public int IdCliente { get; set; }
        /// <summary>Equipo o inmueble: exactamente uno de los dos mayor a 0.</summary>
        public int IdEquipo { get; set; }
        public int IdInmueble { get; set; }
        /// <summary>Credencial de quien atendió; 0 = sin asignar.</summary>
        public long IdAtiende { get; set; }
        public string? Nota { get; set; }
        public DateTimeOffset? FechaInicio { get; set; }
        /// <summary>Si viene, la operación queda finalizada.</summary>
        public DateTimeOffset? FechaFinal { get; set; }
    }

    public class OperacionRecuperadaResultadoDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int IdOperacion { get; set; }
        public string? FolioOperacion { get; set; }
    }
}
