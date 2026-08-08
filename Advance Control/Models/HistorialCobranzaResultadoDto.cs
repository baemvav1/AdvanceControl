using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class HistorialCobranzaResultadoDto
    {
        public string CarpetaHistorial { get; set; } = string.Empty;
        public int OperacionesFinalizadas { get; set; }
        public int OperacionesFinalizadasSinFacturar { get; set; }
        public int OperacionesPendientes { get; set; }
        public int OperacionesOmitidas { get; set; }
        public bool ReporteCobranzaGenerado { get; set; }
        public List<string> Errores { get; set; } = new();

        public int TotalOperaciones => OperacionesFinalizadas + OperacionesFinalizadasSinFacturar + OperacionesPendientes;

        public string ResumenTexto =>
            $"Finalizadas: {OperacionesFinalizadas} | Sin facturar: {OperacionesFinalizadasSinFacturar} | " +
            $"Pendientes: {OperacionesPendientes} | Omitidas: {OperacionesOmitidas}";
    }
}
