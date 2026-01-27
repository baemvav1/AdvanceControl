using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Advance_Control.Models
{
    public class CustomerDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        [JsonPropertyName("idCliente")]
        public int IdCliente { get; set; }

        [JsonPropertyName("rfc")]
        public string Rfc { get; set; } = string.Empty;

        [JsonPropertyName("razonSocial")]
        public string RazonSocial { get; set; } = string.Empty;

        [JsonPropertyName("nombreComercial")]
        public string NombreComercial { get; set; } = string.Empty;

        [JsonPropertyName("curp")]
        public string? Curp { get; set; }

        [JsonPropertyName("regimenFiscal")]
        public string RegimenFiscal { get; set; } = string.Empty;

        [JsonPropertyName("usoCfdi")]
        public string UsoCfdi { get; set; } = string.Empty;

        [JsonPropertyName("diasCredito")]
        public int? DiasCredito { get; set; }

        [JsonPropertyName("limiteCredito")]
        public decimal? LimiteCredito { get; set; }

        [JsonPropertyName("prioridad")]
        public int Prioridad { get; set; }

        [JsonPropertyName("estatus")]
        public bool Estatus { get; set; }

        [JsonPropertyName("credencialId")]
        public int? CredencialId { get; set; }

        [JsonPropertyName("notas")]
        public string Notas { get; set; } = string.Empty;

        [JsonPropertyName("creadoEn")]
        public DateTime CreadoEn { get; set; }

        [JsonPropertyName("actualizadoEn")]
        public DateTime? ActualizadoEn { get; set; }

        [JsonPropertyName("idUsuarioCreador")]
        public int IdUsuarioCreador { get; set; }

        [JsonPropertyName("idUsuarioAct")]
        public int? IdUsuarioAct { get; set; }

        private bool _expand = false;

        /// <summary>
        /// Propiedad interna para controlar el estado de expansión en la UI.
        /// No se deserializa desde el endpoint.
        /// </summary>
        public bool Expand
        {
            get => _expand;
            set
            {
                if (_expand != value)
                {
                    _expand = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
