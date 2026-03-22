using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class PermisoAccionModuloDto
    {
        [JsonPropertyName("idPermisoAccionModulo")]
        public int IdPermisoAccionModulo { get; set; }

        [JsonPropertyName("idPermisoModulo")]
        public int IdPermisoModulo { get; set; }

        [JsonPropertyName("claveAccion")]
        public string ClaveAccion { get; set; } = string.Empty;

        [JsonPropertyName("nombreAccion")]
        public string NombreAccion { get; set; } = string.Empty;

        [JsonPropertyName("tipoAccion")]
        public string TipoAccion { get; set; } = string.Empty;

        [JsonPropertyName("controlKey")]
        public string? ControlKey { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("nivelRequerido")]
        public int NivelRequerido { get; set; }

        [JsonPropertyName("ordenAccion")]
        public int OrdenAccion { get; set; }

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }
    }

    public class PermisoModuloDto
    {
        [JsonPropertyName("idPermisoModulo")]
        public int IdPermisoModulo { get; set; }

        [JsonPropertyName("claveModulo")]
        public string ClaveModulo { get; set; } = string.Empty;

        [JsonPropertyName("grupoModulo")]
        public string GrupoModulo { get; set; } = string.Empty;

        [JsonPropertyName("tagNavegacion")]
        public string TagNavegacion { get; set; } = string.Empty;

        [JsonPropertyName("nombreModulo")]
        public string NombreModulo { get; set; } = string.Empty;

        [JsonPropertyName("nombreView")]
        public string NombreView { get; set; } = string.Empty;

        [JsonPropertyName("rutaView")]
        public string RutaView { get; set; } = string.Empty;

        [JsonPropertyName("nivelRequerido")]
        public int NivelRequerido { get; set; }

        [JsonPropertyName("ordenGrupo")]
        public int OrdenGrupo { get; set; }

        [JsonPropertyName("ordenModulo")]
        public int OrdenModulo { get; set; }

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        [JsonPropertyName("acciones")]
        public List<PermisoAccionModuloDto> Acciones { get; set; } = new();
    }

    public class PermisoUiSyncRequestDto
    {
        [JsonPropertyName("modulos")]
        public List<PermisoModuloSyncDto> Modulos { get; set; } = new();
    }

    public class PermisoModuloSyncDto
    {
        [JsonPropertyName("claveModulo")]
        public string ClaveModulo { get; set; } = string.Empty;

        [JsonPropertyName("grupoModulo")]
        public string GrupoModulo { get; set; } = string.Empty;

        [JsonPropertyName("tagNavegacion")]
        public string TagNavegacion { get; set; } = string.Empty;

        [JsonPropertyName("nombreModulo")]
        public string NombreModulo { get; set; } = string.Empty;

        [JsonPropertyName("nombreView")]
        public string NombreView { get; set; } = string.Empty;

        [JsonPropertyName("rutaView")]
        public string RutaView { get; set; } = string.Empty;

        [JsonPropertyName("ordenGrupo")]
        public int OrdenGrupo { get; set; }

        [JsonPropertyName("ordenModulo")]
        public int OrdenModulo { get; set; }

        [JsonPropertyName("acciones")]
        public List<PermisoAccionModuloSyncDto> Acciones { get; set; } = new();
    }

    public class PermisoAccionModuloSyncDto
    {
        [JsonPropertyName("claveAccion")]
        public string ClaveAccion { get; set; } = string.Empty;

        [JsonPropertyName("nombreAccion")]
        public string NombreAccion { get; set; } = string.Empty;

        [JsonPropertyName("tipoAccion")]
        public string TipoAccion { get; set; } = string.Empty;

        [JsonPropertyName("controlKey")]
        public string? ControlKey { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("ordenAccion")]
        public int OrdenAccion { get; set; }
    }

    public class PermisoUiSyncResultDto
    {
        [JsonPropertyName("modulosProcesados")]
        public int ModulosProcesados { get; set; }

        [JsonPropertyName("accionesProcesadas")]
        public int AccionesProcesadas { get; set; }
    }

    public class PermisoModuloNivelUpdateDto
    {
        [JsonPropertyName("idPermisoModulo")]
        public int IdPermisoModulo { get; set; }

        [JsonPropertyName("nivelRequerido")]
        public int NivelRequerido { get; set; }
    }

    public class PermisoAccionNivelUpdateDto
    {
        [JsonPropertyName("idPermisoAccionModulo")]
        public int IdPermisoAccionModulo { get; set; }

        [JsonPropertyName("nivelRequerido")]
        public int NivelRequerido { get; set; }
    }
}
