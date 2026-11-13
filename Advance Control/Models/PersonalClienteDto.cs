using System.Linq;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Usuario asignado a un cliente como Técnico, Supervisor o Agente (api/personal_cliente),
    /// o candidato para un rol (api/personal_cliente/candidatos).
    /// </summary>
    public class PersonalClienteDto
    {
        public const string RolTecnico = "tecnico";
        public const string RolSupervisor = "supervisor";
        public const string RolAgente = "agente";

        [JsonPropertyName("id_cliente")] public int IdCliente { get; set; }
        [JsonPropertyName("credencial_id")] public long CredencialId { get; set; }
        [JsonPropertyName("rol")] public string? Rol { get; set; }
        [JsonPropertyName("usuario")] public string? Usuario { get; set; }
        [JsonPropertyName("nivel")] public int? Nivel { get; set; }
        [JsonPropertyName("contacto_id")] public long? ContactoId { get; set; }
        [JsonPropertyName("tratamiento")] public string? Tratamiento { get; set; }
        [JsonPropertyName("nombre")] public string? Nombre { get; set; }
        [JsonPropertyName("correo")] public string? Correo { get; set; }
        [JsonPropertyName("telefono")] public string? Telefono { get; set; }

        /// <summary>"Tec. Javier" (tratamiento + primer nombre), igual que los contactos.</summary>
        public string NombreCorto
        {
            get
            {
                var primero = (Nombre ?? Usuario ?? string.Empty).Split(' ', System.StringSplitOptions.RemoveEmptyEntries) is { Length: > 0 } p ? p[0] : string.Empty;
                return string.IsNullOrWhiteSpace(Tratamiento) ? primero : $"{Tratamiento.Trim()} {primero}";
            }
        }

        public string NombreCompleto => string.IsNullOrWhiteSpace(Tratamiento)
            ? Nombre ?? Usuario ?? string.Empty
            : $"{Tratamiento.Trim()} {Nombre ?? Usuario}";

        public string Detalle => string.Join(" · ", new[] { Usuario, Correo }.Where(s => !string.IsNullOrWhiteSpace(s)));

        public static string RolTexto(string rol) => rol switch
        {
            RolTecnico => "técnico",
            RolSupervisor => "supervisor",
            _ => "administrativo"
        };
    }
}
