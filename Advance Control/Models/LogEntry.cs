using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Representa una entrada de log que será enviada al servidor
    /// </summary>
    public class LogEntry
    {
        public string? Id { get; set; }
        public LogLevel Level { get; set; }
        public string? Message { get; set; }
        public string? Exception { get; set; }
        public string? StackTrace { get; set; }
        /// <summary>Nombre de la clase/servicio que generó el log</summary>
        public string? Source { get; set; }
        public string? Method { get; set; }
        /// <summary>Nombre de usuario (texto)</summary>
        public string? Username { get; set; }
        public string? MachineName { get; set; }
        public string? AppVersion { get; set; }
        public DateTime Timestamp { get; set; }
        public string? AdditionalData { get; set; }

        // ── Campos nuevos ─────────────────────────────────────────────────────

        /// <summary>
        /// ID numérico de la credencial del usuario autenticado.
        /// Se auto-popula desde IUserSessionService en LoggingService.
        /// </summary>
        public int? CredencialId { get; set; }

        /// <summary>
        /// Categoría de negocio para filtrado en el dashboard.
        /// Valores: Operacion | Mantenimiento | Cliente | Equipo | Proveedor | Autenticacion | Sistema
        /// </summary>
        public string? Categoria { get; set; }

        /// <summary>
        /// Nombre de la vista/página donde se originó el log.
        /// Ej: "OperacionesView", "MttoView", "ClientesView"
        /// </summary>
        public string? Page { get; set; }
    }

    /// <summary>
    /// Niveles de severidad para los logs
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }
}

