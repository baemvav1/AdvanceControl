using System;
using System.Globalization;

namespace Advance_Control.Models
{
    public class OperacionFacturadaDto
    {
        public int IdOperacion { get; set; }
        public string? TipoMantenimiento { get; set; }
        public string? RazonSocial { get; set; }
        public string? Identificador { get; set; }
        public string? Atiende { get; set; }
        public double Monto { get; set; }
        public DateTime? FechaFinal { get; set; }
        public int IdFactura { get; set; }
        public string? Folio { get; set; }
        public string? Uuid { get; set; }
        public decimal Total { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? FechaTimbrado { get; set; }
        public decimal TotalAbonado { get; set; }
        public long NumeroAbonos { get; set; }

        public string TotalTexto => Total.ToString("C2", new CultureInfo("es-MX"));
        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
        public string UuidTexto => string.IsNullOrWhiteSpace(Uuid) ? "Sin UUID" : $"UUID: {Uuid}";
        public string AtiendeTexto => string.IsNullOrWhiteSpace(Atiende) ? "Sin asignar" : Atiende;
        public string IdOperacionTexto => $"Operación #{IdOperacion}";
        public bool TieneAbonos => NumeroAbonos > 0;
    }
}
