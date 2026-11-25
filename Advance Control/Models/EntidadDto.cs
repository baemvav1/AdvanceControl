using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO para representar una entidad
    /// </summary>
    public class EntidadDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonPropertyName("idEntidad")]
        public int IdEntidad { get; set; }

        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }

        [JsonPropertyName("razonSocial")]
        public string? RazonSocial { get; set; }

        [JsonPropertyName("rfc")]
        public string? RFC { get; set; }

        [JsonPropertyName("cp")]
        public string? CP { get; set; }

        [JsonPropertyName("estado")]
        public string? Estado { get; set; }

        [JsonPropertyName("ciudad")]
        public string? Ciudad { get; set; }

        [JsonPropertyName("pais")]
        public string? Pais { get; set; }

        [JsonPropertyName("calle")]
        public string? Calle { get; set; }

        [JsonPropertyName("numExt")]
        public string? NumExt { get; set; }

        [JsonPropertyName("numInt")]
        public string? NumInt { get; set; }

        [JsonPropertyName("colonia")]
        public string? Colonia { get; set; }

        [JsonPropertyName("apoderado")]
        public string? Apoderado { get; set; }

        [JsonPropertyName("estatus")]
        public bool? Estatus { get; set; }

        [JsonPropertyName("regimenFiscal")]
        public string? RegimenFiscal { get; set; }

        /// <summary>Emisor por omisión (siempre hay exactamente una).</summary>
        [JsonPropertyName("esPredeterminada")]
        public bool EsPredeterminada { get; set; }

        /// <summary>Serie de los CFDI que timbra la app para esta entidad.</summary>
        [JsonPropertyName("serieFacturas")]
        public string? SerieFacturas { get; set; }

        [JsonPropertyName("ultimoFolio")]
        public long UltimoFolio { get; set; }

        /// <summary>Usuario de timbrado Bilkon (la contraseña nunca llega al cliente).</summary>
        [JsonPropertyName("felUsuario")]
        public string? FelUsuario { get; set; }

        [JsonPropertyName("felConfigurada")]
        public bool FelConfigurada { get; set; }

        /// <summary>Texto para ComboBox de entidad: "ADVANCE ELEVADORES (AEC210405PS8)".</summary>
        [JsonIgnore]
        public string Descripcion => $"{(string.IsNullOrWhiteSpace(RazonSocial) ? NombreComercial : RazonSocial)} ({RFC})";

        [JsonIgnore]
        public string SerieFolioTexto => $"Serie {SerieFacturas ?? "A"} · último folio {UltimoFolio}";

        [JsonIgnore]
        public string FelIndicadorTexto => FelConfigurada
            ? $"Bilkon: {FelUsuario}"
            : (EsPredeterminada ? "Bilkon: cuenta del servidor" : "Sin cuenta Bilkon");

        [JsonIgnore]
        public bool FelLista => FelConfigurada || EsPredeterminada;

        private int _cuentasBancarias;
        /// <summary>Cuentas bancarias registradas (lo calcula EntidadesViewModel).</summary>
        [JsonIgnore]
        public int CuentasBancarias
        {
            get => _cuentasBancarias;
            set
            {
                if (_cuentasBancarias != value)
                {
                    _cuentasBancarias = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CuentasIndicadorTexto));
                    OnPropertyChanged(nameof(TieneCuentas));
                }
            }
        }

        [JsonIgnore]
        public bool TieneCuentas => CuentasBancarias > 0;

        [JsonIgnore]
        public string CuentasIndicadorTexto => CuentasBancarias switch
        {
            0 => "Sin cuentas bancarias",
            1 => "1 cuenta bancaria",
            _ => $"{CuentasBancarias} cuentas bancarias"
        };

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

        // --- Indicadores de CSD/PFX, calculados por EntidadesViewModel cruzando el RFC de
        // esta entidad con el RFC del CSD/FIEL cargado. No se deserializan desde el endpoint. ---

        private bool _csdCargado;
        public bool CsdCargado
        {
            get => _csdCargado;
            set
            {
                if (_csdCargado != value)
                {
                    _csdCargado = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CsdIndicadorTexto));
                }
            }
        }

        private bool _csdVigente;
        public bool CsdVigente
        {
            get => _csdVigente;
            set
            {
                if (_csdVigente != value)
                {
                    _csdVigente = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CsdIndicadorTexto));
                }
            }
        }

        public string CsdIndicadorTexto => !CsdCargado ? "Sin CSD" : (CsdVigente ? "CSD vigente" : "CSD vencido");

        private bool _pfxCargado;
        public bool PfxCargado
        {
            get => _pfxCargado;
            set
            {
                if (_pfxCargado != value)
                {
                    _pfxCargado = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PfxIndicadorTexto));
                }
            }
        }

        private bool _pfxVigente;
        public bool PfxVigente
        {
            get => _pfxVigente;
            set
            {
                if (_pfxVigente != value)
                {
                    _pfxVigente = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PfxIndicadorTexto));
                }
            }
        }

        public string PfxIndicadorTexto => !PfxCargado ? "Sin PFX" : (PfxVigente ? "PFX vigente" : "PFX vencido");
    }
}
