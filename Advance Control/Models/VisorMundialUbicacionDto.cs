using System;
using System.Collections.Generic;
using System.Linq;

namespace Advance_Control.Models
{
    public class VisorMundialUbicacionDto
    {
        public int IdUbicacion { get; set; }
        public string? Nombre { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public string? DireccionCompleta { get; set; }
        public int? IdArea { get; set; }
        public int CantidadEquipos { get; set; }
        public bool TieneOperacionAbierta { get; set; }
        public string? ClientesRfc { get; set; }

        public IReadOnlyList<string> ClientesRfcList =>
            string.IsNullOrWhiteSpace(ClientesRfc)
                ? Array.Empty<string>()
                : ClientesRfc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
