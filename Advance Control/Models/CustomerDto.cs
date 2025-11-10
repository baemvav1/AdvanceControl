using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class CustomerDto
    {
        [JsonPropertyName("id_cliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("tipo_persona")]
        public int TipoPersona { get; set; }

        [JsonPropertyName("rfc")]
        public string Rfc { get; set; } = string.Empty;

        [JsonPropertyName("razon_social")]
        public string RazonSocial { get; set; } = string.Empty;

        [JsonPropertyName("nombre_comercial")]
        public string NombreComercial { get; set; } = string.Empty;

        [JsonPropertyName("curp")]
        public string? Curp { get; set; }

        [JsonPropertyName("regimen_fiscal")]
        public string RegimenFiscal { get; set; } = string.Empty;

        [JsonPropertyName("uso_cfdi")]
        public string UsoCfdi { get; set; } = string.Empty;

        [JsonPropertyName("dias_credito")]
        public int? DiasCredito { get; set; }

        [JsonPropertyName("limite_credito")]
        public decimal? LimiteCredito { get; set; }

        [JsonPropertyName("prioridad")]
        public int Prioridad { get; set; }

        [JsonPropertyName("estatus")]
        public bool Estatus { get; set; }

        [JsonPropertyName("credencial_id")]
        public int? CredencialId { get; set; }

        [JsonPropertyName("notas")]
        public string Notas { get; set; } = string.Empty;

        [JsonPropertyName("creado_en")]
        public DateTime CreadoEn { get; set; }

        [JsonPropertyName("actualizado_en")]
        public DateTime? ActualizadoEn { get; set; }

        [JsonPropertyName("id_usuario_creador")]
        public int IdUsuarioCreador { get; set; }

        [JsonPropertyName("id_usuaio_act")]
        public int? IdUsuarioAct { get; set; }
    }
}
