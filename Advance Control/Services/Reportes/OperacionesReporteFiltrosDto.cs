using System;

namespace Advance_Control.Services.Reportes
{
    /// <summary>
    /// Snapshot de los filtros activos en OperacionesPage al momento de generar
    /// el reporte, para dejarlos impresos y que el reporte sea auditable
    /// (qué subconjunto exacto se le exigió al operador).
    /// </summary>
    public class OperacionesReporteFiltrosDto
    {
        public string? IdOperacionFiltro { get; set; }
        public string TipoFiltro { get; set; } = "Todos";
        public string? ClienteFiltro { get; set; }
        public string? EquipoFiltro { get; set; }
        public string? AreaFiltro { get; set; }
        public string? NotaFiltro { get; set; }
        public DateTimeOffset? FechaInicialFiltro { get; set; }
        public DateTimeOffset? FechaFinalFiltro { get; set; }
        public bool MostrarAbiertas { get; set; }
        public bool MostrarTFinalizadas { get; set; }
        public bool MostrarFacturadas { get; set; }
        public bool MostrarAbiertasConOc { get; set; }
        public string? GeneradoPor { get; set; }
    }
}
