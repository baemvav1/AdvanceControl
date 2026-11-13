using System;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>Un chat existente del usuario (GET api/Mensajes/conversaciones).</summary>
    public class ConversacionResumenDto
    {
        [JsonPropertyName("otroCredencialId")]
        public long OtroCredencialId { get; set; }

        [JsonPropertyName("ultimoMensajeId")]
        public long UltimoMensajeId { get; set; }

        [JsonPropertyName("ultimoContenido")]
        public string UltimoContenido { get; set; } = string.Empty;

        [JsonPropertyName("ultimoTipo")]
        public string UltimoTipo { get; set; } = "mensaje";

        [JsonPropertyName("ultimoEn")]
        public DateTime UltimoEn { get; set; }

        [JsonPropertyName("ultimoEsMio")]
        public bool UltimoEsMio { get; set; }

        [JsonPropertyName("ultimoEliminado")]
        public bool UltimoEliminado { get; set; }

        [JsonPropertyName("noLeidos")]
        public long NoLeidos { get; set; }

        /// <summary>Texto corto para la lista de chats ("Tú: …" si lo envió el usuario).</summary>
        public static string Resumen(string? contenido, bool eliminado, bool esMio)
        {
            var texto = eliminado ? "Mensaje eliminado" : (contenido ?? string.Empty).Replace('\n', ' ').Trim();
            return esMio ? $"Tú: {texto}" : texto;
        }

        /// <summary>Fecha a hora local (el servidor la manda en UTC).</summary>
        public static DateTime ALocal(DateTime fecha)
            => fecha.Kind == DateTimeKind.Utc ? fecha.ToLocalTime() : fecha;
    }
}
