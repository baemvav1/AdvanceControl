using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>Resumen de operaciones de un equipo (GET api/equipo_crud/{id}/resumen-operaciones).</summary>
    public class EquipoResumenDto
    {
        private static readonly CultureInfo CulturaMx = new("es-MX");

        [JsonPropertyName("total_operaciones")] public int TotalOperaciones { get; set; }
        [JsonPropertyName("abiertas")] public int Abiertas { get; set; }
        [JsonPropertyName("finalizadas")] public int Finalizadas { get; set; }
        [JsonPropertyName("total_facturado")] public decimal TotalFacturado { get; set; }
        [JsonPropertyName("total_vencido")] public decimal TotalVencido { get; set; }
        [JsonPropertyName("ultimas")] public List<OperacionResumenDto> Ultimas { get; set; } = new();
        [JsonPropertyName("ultima_visita")] public VisitaResumenDto? UltimaVisita { get; set; }
        [JsonPropertyName("ordenes_pendientes")] public List<OrdenPendienteResumenDto> OrdenesPendientes { get; set; } = new();

        public string TotalFacturadoTexto => TotalFacturado.ToString("C", CulturaMx);
        public string TotalVencidoTexto => TotalVencido.ToString("C", CulturaMx);
        public bool HayVencido => TotalVencido > 0.01m;
        public string AbiertasFinalizadasTexto => $"{Abiertas} abiertas · {Finalizadas} finalizadas";
        public string UltimaVisitaTexto => UltimaVisita is null
            ? "Sin visitas finalizadas"
            : $"{UltimaVisita.Fecha?.ToLocalTime():dd/MM/yyyy} · {UltimaVisita.Tipo ?? "Operación"} {UltimaVisita.Folio} · {UltimaVisita.Tecnico ?? "Sin técnico"}";
        public bool SinOperaciones => TotalOperaciones == 0;
        public bool SinOrdenesPendientes => OrdenesPendientes.Count == 0;
    }

    public class OperacionResumenDto
    {
        private static readonly CultureInfo CulturaMx = new("es-MX");

        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("folio")] public string? Folio { get; set; }
        [JsonPropertyName("fecha")] public DateTimeOffset? Fecha { get; set; }
        [JsonPropertyName("tipo")] public string? Tipo { get; set; }
        [JsonPropertyName("finalizada")] public bool Finalizada { get; set; }
        [JsonPropertyName("monto")] public double Monto { get; set; }

        public string FolioTexto => $"#{Folio ?? Id.ToString()}";
        public string FechaTexto => Fecha?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—";
        public string TipoTexto => Tipo ?? "—";
        public string EstatusTexto => Finalizada ? "Finalizada" : "Abierta";
        public string MontoTexto => Monto.ToString("C", CulturaMx);
    }

    public class VisitaResumenDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("folio")] public string? Folio { get; set; }
        [JsonPropertyName("fecha")] public DateTimeOffset? Fecha { get; set; }
        [JsonPropertyName("tipo")] public string? Tipo { get; set; }
        [JsonPropertyName("tecnico")] public string? Tecnico { get; set; }
    }

    public class OrdenPendienteResumenDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("fecha")] public DateTimeOffset? Fecha { get; set; }
        [JsonPropertyName("tipo")] public string? Tipo { get; set; }
        [JsonPropertyName("descripcion")] public string? Descripcion { get; set; }

        public string Texto => $"#{Id} · {Fecha?.ToLocalTime():dd/MM/yyyy} · {Tipo ?? "Orden"}" +
            (string.IsNullOrWhiteSpace(Descripcion) ? "" : $" · {Descripcion}");
    }
}
