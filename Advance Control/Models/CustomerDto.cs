using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private ObservableCollection<ContactoDto> _contactos = new ObservableCollection<ContactoDto>();

        /// <summary>
        /// Colección de contactos para este cliente.
        /// Se carga desde el endpoint de contactos cuando se expande el item.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<ContactoDto> Contactos
        {
            get => _contactos;
            set
            {
                if (_contactos != value)
                {
                    _contactos = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowNoContactosMessage));
                }
            }
        }

        private bool _contactosLoaded = false;

        /// <summary>
        /// Indica si los contactos ya han sido cargados para este cliente.
        /// </summary>
        [JsonIgnore]
        public bool ContactosLoaded
        {
            get => _contactosLoaded;
            set
            {
                if (_contactosLoaded != value)
                {
                    _contactosLoaded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowNoContactosMessage));
                }
            }
        }

        private bool _isLoadingContactos = false;

        /// <summary>
        /// Indica si los contactos están siendo cargados.
        /// </summary>
        [JsonIgnore]
        public bool IsLoadingContactos
        {
            get => _isLoadingContactos;
            set
            {
                if (_isLoadingContactos != value)
                {
                    _isLoadingContactos = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indica si se debe mostrar el mensaje de que no hay contactos.
        /// True cuando ContactosLoaded es true y Contactos está vacía.
        /// </summary>
        [JsonIgnore]
        public bool ShowNoContactosMessage => ContactosLoaded && Contactos.Count == 0;

        /// <summary>
        /// Actualiza el estado del mensaje de no contactos.
        /// Debe llamarse después de modificar Contactos o ContactosLoaded.
        /// </summary>
        public void NotifyNoContactosMessageChanged()
        {
            OnPropertyChanged(nameof(ShowNoContactosMessage));
        }
    }
}
