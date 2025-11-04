using System;

namespace Advance_Control.Models
{
    /// <summary>
    /// Representa una entrada de log que será enviada al servidor
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Identificador único del log (generado en cliente)
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Nivel de severidad del log
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Mensaje descriptivo del log
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Información de la excepción si existe
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Stack trace de la excepción
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Nombre de la clase o componente que generó el log
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Nombre del método que generó el log
        /// </summary>
        public string? Method { get; set; }

        /// <summary>
        /// Usuario que estaba usando la aplicación (si está autenticado)
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Identificador de la máquina/dispositivo
        /// </summary>
        public string? MachineName { get; set; }

        /// <summary>
        /// Versión de la aplicación
        /// </summary>
        public string? AppVersion { get; set; }

        /// <summary>
        /// Fecha y hora UTC de creación del log
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Datos adicionales en formato JSON
        /// </summary>
        public string? AdditionalData { get; set; }
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
