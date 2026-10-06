using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Espejo de AdvanceApi.DTOs.OperacionAprobacionDto: contacto dirigido,
    /// cotización cerrada y versión vigente de la cotización.
    /// </summary>
    public class OperacionAprobacionDto
    {
        public int IdOperacion { get; set; }
        public int? IdCliente { get; set; }
        public bool CotFinalizada { get; set; }
        public DateTime? CotFinalizadaEn { get; set; }
        public long? IdContactoDirigido { get; set; }
        public string? DirigidoNombre { get; set; }
        public string? DirigidoCorreo { get; set; }
        public bool DirigidoTieneLogin { get; set; }
        public long? IdVersion { get; set; }
        public int? Version { get; set; }

        /// <summary>Pendiente | Aprobada | CambiosSolicitados | Rechazada | Reemplazada.</summary>
        public string? Estado { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Iva { get; set; }
        public decimal? Total { get; set; }
        public DateTime? CerradaEn { get; set; }
        public DateTime? RespondidaEn { get; set; }
        public string? RespuestaComentario { get; set; }

        /// <summary>portal | advance | orden_compra | migracion.</summary>
        public string? AprobadaVia { get; set; }
        public string? RespondidaPorNombre { get; set; }
        public DateTime? NotificadaEn { get; set; }
        public string? NotificadaA { get; set; }

        public bool PorResponder => Estado is "Pendiente" or "CambiosSolicitados";

        public string EstadoTexto
        {
            get
            {
                if (!CotFinalizada)
                    return Estado == "Reemplazada" ? "Cotización abierta (la versión aprobada quedó reemplazada)" : "Cotización abierta";

                var version = Version > 1 ? $" (v{Version})" : string.Empty;
                return Estado switch
                {
                    "Pendiente" => $"Cotización cerrada{version} · pendiente de aprobación",
                    "CambiosSolicitados" => $"Cotización{version} · el cliente solicitó cambios: {RespuestaComentario}",
                    "Rechazada" => $"Cotización{version} · rechazada por el cliente: {RespuestaComentario}",
                    "Aprobada" => $"Cotización{version} · aprobada{Via}{Por}{Cuando}",
                    _ => $"Cotización cerrada{version}"
                };
            }
        }

        private string Via => AprobadaVia switch
        {
            "portal" => " en el portal",
            "advance" => " (registrada por el técnico)",
            "orden_compra" => " con orden de compra",
            "migracion" => " (operación anterior al sistema de aprobaciones)",
            _ => string.Empty
        };

        private string Por => string.IsNullOrWhiteSpace(RespondidaPorNombre) ? string.Empty : $" por {RespondidaPorNombre}";
        private string Cuando => RespondidaEn.HasValue ? $" el {RespondidaEn.Value.ToLocalTime():dd/MM/yyyy HH:mm}" : string.Empty;

        public string NotificacionTexto => NotificadaEn.HasValue
            ? $"Notificada el {NotificadaEn.Value.ToLocalTime():dd/MM/yyyy HH:mm} a {NotificadaA}"
            : "Sin notificar al cliente";
    }

    /// <summary>Espejo de AdvanceApi.DTOs.HojaAprobacionDto.</summary>
    public class HojaAprobacionDto
    {
        public long Id { get; set; }
        public int IdOperacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public long? IdContactoDirigido { get; set; }
        public string? DirigidoNombre { get; set; }
        public string? DirigidoCorreo { get; set; }
        public bool DirigidoTieneLogin { get; set; }
        public string? AprobadaVia { get; set; }
        public string? AprobadaPor { get; set; }
        public string? AprobacionComentario { get; set; }
        public DateTime? FirmadaEn { get; set; }
        public DateTime? NotificadaEn { get; set; }
        public string? NotificadaA { get; set; }

        public string NotificacionTexto => NotificadaEn.HasValue
            ? $"Notificada el {NotificadaEn.Value.ToLocalTime():dd/MM/yyyy HH:mm} a {NotificadaA}"
            : "Sin notificar al cliente";
    }
}
