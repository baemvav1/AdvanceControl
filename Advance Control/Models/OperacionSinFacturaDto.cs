using System;
using System.Collections.Generic;
using System.Globalization;

namespace Advance_Control.Models
{
    public class OperacionSinFacturaDto
    {
        public int IdOperacion { get; set; }
        public string? TipoMantenimiento { get; set; }
        public string? RazonSocial { get; set; }
        public string? RfcCliente { get; set; }
        public string? Identificador { get; set; }
        public string? Atiende { get; set; }
        public double Monto { get; set; }
        public DateTime? FechaFinal { get; set; }

        /// <summary>Calculado en el cliente por FacturaOperacionMatchingEngine, no viene de la API.</summary>
        public List<FacturaResumenDto> Sugerencias { get; set; } = new();

        public string MontoTexto => Monto.ToString("C2", new CultureInfo("es-MX"));
        public string FechaFinalTexto => FechaFinal.HasValue ? FechaFinal.Value.ToString("dd/MM/yyyy") : "Sin fecha";
        public string AtiendeTexto => string.IsNullOrWhiteSpace(Atiende) ? "Sin asignar" : Atiende;
        public string IdOperacionTexto => $"Operación #{IdOperacion}";
        public bool TieneSugerencias => Sugerencias.Count > 0;
        public string SugerenciasTexto => Sugerencias.Count == 1
            ? "1 factura sugerida"
            : $"{Sugerencias.Count} facturas sugeridas";
    }
}
