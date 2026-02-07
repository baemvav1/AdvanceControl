using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para representar un contacto del sistema
    /// </summary>
    public class ContactoDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// ID del contacto
        /// </summary>
        [JsonPropertyName("contactoId")]
        public long ContactoId { get; set; }

        /// <summary>
        /// ID de credencial asociada
        /// </summary>
        [JsonPropertyName("credencialId")]
        public long? CredencialId { get; set; }

        /// <summary>
        /// Nombre del contacto
        /// </summary>
        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        /// <summary>
        /// Apellido del contacto
        /// </summary>
        [JsonPropertyName("apellido")]
        public string? Apellido { get; set; }

        /// <summary>
        /// Correo electrónico del contacto
        /// </summary>
        [JsonPropertyName("correo")]
        public string? Correo { get; set; }

        /// <summary>
        /// Teléfono del contacto
        /// </summary>
        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        /// <summary>
        /// Departamento del contacto
        /// </summary>
        [JsonPropertyName("departamento")]
        public string? Departamento { get; set; }

        /// <summary>
        /// Código interno del contacto
        /// </summary>
        [JsonPropertyName("codigoInterno")]
        public string? CodigoInterno { get; set; }

        /// <summary>
        /// Indica si el contacto está activo
        /// </summary>
        [JsonPropertyName("activo")]
        public bool? Activo { get; set; }

        /// <summary>
        /// Notas adicionales
        /// </summary>
        [JsonPropertyName("notas")]
        public string? Notas { get; set; }

        /// <summary>
        /// Fecha de creación
        /// </summary>
        [JsonPropertyName("creadoEn")]
        public DateTime? CreadoEn { get; set; }

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        [JsonPropertyName("actualizadoEn")]
        public DateTime? ActualizadoEn { get; set; }

        /// <summary>
        /// ID del proveedor asociado
        /// </summary>
        [JsonPropertyName("idProveedor")]
        public int? IdProveedor { get; set; }

        /// <summary>
        /// Cargo del contacto
        /// </summary>
        [JsonPropertyName("cargo")]
        public string? Cargo { get; set; }

        /// <summary>
        /// ID del cliente asociado
        /// </summary>
        [JsonPropertyName("idCliente")]
        public int? IdCliente { get; set; }


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

        /// <summary>
        /// Nombre completo del contacto (Nombre + Apellido)
        /// </summary>
        public string NombreCompleto => $"{Nombre ?? ""} {Apellido ?? ""}".Trim();
    }
}
