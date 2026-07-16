using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class OperacionSinFacturaDto
    {
        public int IdOperacion { get; set; }
        public string? TipoMantenimiento { get; set; }
        public string? RazonSocial { get; set; }
        public string? Identificador { get; set; }
        public string? Atiende { get; set; }
        public double Monto { get; set; }
        public DateTime? FechaFinal { get; set; }

        public string MontoTexto => Monto.ToString("C2", new CultureInfo("es-MX"));
        public string FechaFinalTexto => FechaFinal.HasValue ? FechaFinal.Value.ToString("dd/MM/yyyy") : "Sin fecha";
        public string AtiendeTexto => string.IsNullOrWhiteSpace(Atiende) ? "Sin asignar" : Atiende;
        public string IdOperacionTexto => $"Operación #{IdOperacion}";
    }
}
