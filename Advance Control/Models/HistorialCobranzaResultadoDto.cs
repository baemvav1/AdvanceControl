using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class HistorialCobranzaResultadoDto
    {
        public string CarpetaHistorial { get; set; } = string.Empty;
        public int OperacionesAbiertas { get; set; }
        public int OperacionesAbiertasSinOrden { get; set; }
        public int OperacionesFinalizadasConOrden { get; set; }
        public int OperacionesFinalizadasSinOrden { get; set; }
        public int OperacionesFacturadasPendientes { get; set; }

        /// <summary>Del rango de fechas, pero fuera de los checks marcados (o de ninguna categoría).</summary>
        public int OperacionesExcluidas { get; set; }

        /// <summary>Fallaron al generar sus documentos.</summary>
        public int OperacionesOmitidas { get; set; }

        public bool ReporteCobranzaGenerado { get; set; }
        public List<string> Errores { get; set; } = new();

        public int TotalOperaciones => OperacionesAbiertas + OperacionesAbiertasSinOrden
            + OperacionesFinalizadasConOrden + OperacionesFinalizadasSinOrden + OperacionesFacturadasPendientes;

        public string ResumenTexto =>
            $"Abiertas: {OperacionesAbiertas} | Abiertas S/Orden: {OperacionesAbiertasSinOrden} | " +
            $"Finalizada C/Orden: {OperacionesFinalizadasConOrden} | Finalizada S/Orden: {OperacionesFinalizadasSinOrden} | " +
            $"Pendientes: {OperacionesFacturadasPendientes} | " +
            $"Excluidas: {OperacionesExcluidas} | Omitidas: {OperacionesOmitidas}";
    }
}
