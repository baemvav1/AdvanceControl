using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Una empresa (Cliente) vinculada a un Contacto, con si está marcada
    /// como visible en su Portal de Cliente. Usado en ClientesPage
    /// (vincular/desvincular) y UsuarioEditorWindow (selección múltiple de
    /// empresas del portal).
    /// </summary>
    public class ClientePortalSeleccionDto
    {
        [JsonPropertyName("idCliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        [JsonPropertyName("visiblePortal")]
        public bool VisiblePortal { get; set; }
    }
}
