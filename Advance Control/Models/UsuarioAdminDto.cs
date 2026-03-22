using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    public class UsuarioAdminDto : INotifyPropertyChanged
    {
        private string _usuario = string.Empty;
        private bool _estaActiva;
        private int _nivel;
        private long? _contactoId;
        private string? _nombre;
        private string? _apellido;
        private string? _correo;
        private string? _telefono;
        private string? _departamento;
        private string? _codigoInterno;
        private string? _notas;
        private int? _idProveedor;
        private string? _cargo;
        private int? _idCliente;
        private string? _tratamiento;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        [JsonPropertyName("credencialId")]
        public long CredencialId { get; set; }

        [JsonPropertyName("usuario")]
        public string Usuario
        {
            get => _usuario;
            set => SetProperty(ref _usuario, value);
        }

        [JsonPropertyName("estaActiva")]
        public bool EstaActiva
        {
            get => _estaActiva;
            set => SetProperty(ref _estaActiva, value);
        }

        [JsonPropertyName("creadoEn")]
        public DateTime? CreadoEn { get; set; }

        [JsonPropertyName("actualizadoEn")]
        public DateTime? ActualizadoEn { get; set; }

        [JsonPropertyName("nivel")]
        public int Nivel
        {
            get => _nivel;
            set => SetProperty(ref _nivel, value);
        }

        [JsonPropertyName("tipoUsuario")]
        public TipoUsuarioDto? TipoUsuario { get; set; }

        [JsonPropertyName("contactoId")]
        public long? ContactoId
        {
            get => _contactoId;
            set => SetProperty(ref _contactoId, value);
        }

        [JsonPropertyName("nombre")]
        public string? Nombre
        {
            get => _nombre;
            set => SetProperty(ref _nombre, value);
        }

        [JsonPropertyName("apellido")]
        public string? Apellido
        {
            get => _apellido;
            set => SetProperty(ref _apellido, value);
        }

        [JsonPropertyName("correo")]
        public string? Correo
        {
            get => _correo;
            set => SetProperty(ref _correo, value);
        }

        [JsonPropertyName("telefono")]
        public string? Telefono
        {
            get => _telefono;
            set => SetProperty(ref _telefono, value);
        }

        [JsonPropertyName("departamento")]
        public string? Departamento
        {
            get => _departamento;
            set => SetProperty(ref _departamento, value);
        }

        [JsonPropertyName("codigoInterno")]
        public string? CodigoInterno
        {
            get => _codigoInterno;
            set => SetProperty(ref _codigoInterno, value);
        }

        [JsonPropertyName("contactoActivo")]
        public bool? ContactoActivo { get; set; }

        [JsonPropertyName("notas")]
        public string? Notas
        {
            get => _notas;
            set => SetProperty(ref _notas, value);
        }

        [JsonPropertyName("idProveedor")]
        public int? IdProveedor
        {
            get => _idProveedor;
            set => SetProperty(ref _idProveedor, value);
        }

        [JsonPropertyName("cargo")]
        public string? Cargo
        {
            get => _cargo;
            set => SetProperty(ref _cargo, value);
        }

        [JsonPropertyName("idCliente")]
        public int? IdCliente
        {
            get => _idCliente;
            set => SetProperty(ref _idCliente, value);
        }

        [JsonPropertyName("tratamiento")]
        public string? Tratamiento
        {
            get => _tratamiento;
            set => SetProperty(ref _tratamiento, value);
        }
    }

    public class UsuarioAdminEditDto
    {
        public string? Usuario { get; set; }
        public string? Password { get; set; }
        public int? Nivel { get; set; }
        public bool? EstaActiva { get; set; }
        public long? ContactoId { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Departamento { get; set; }
        public string? CodigoInterno { get; set; }
        public string? Cargo { get; set; }
        public int? IdProveedor { get; set; }
        public bool LimpiarIdProveedor { get; set; }
        public int? IdCliente { get; set; }
        public bool LimpiarIdCliente { get; set; }
        public string? Tratamiento { get; set; }
        public string? Notas { get; set; }
    }

    public class UsuarioAdminQueryDto
    {
        public long CredencialId { get; set; }
        public string? Usuario { get; set; }
        public bool? EstaActiva { get; set; }
        public int Nivel { get; set; }
    }

    public class UsuarioAdminOperationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("credencialId")]
        public long CredencialId { get; set; }

        [JsonPropertyName("contactoId")]
        public long? ContactoId { get; set; }
    }
}
