using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Advance_Control.Models
{
    /// <summary>
    /// Fila del buscador de equipos (GET api/equipo_crud/buscador): equipo + clientes + ubicación + área + técnicos.
    /// </summary>
    public class EquipoBusquedaDto
    {
        public int IdEquipo { get; set; }
        public string Identificador { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Descripcion { get; set; }
        public int? AnioFabricacion { get; set; }
        public int? Paradas { get; set; }
        public int? Kilogramos { get; set; }
        public int? Personas { get; set; }
        public string? Controlador { get; set; }
        public string? TipoPuerta { get; set; }
        public string? Velocidad { get; set; }
        public string? TipoMaquina { get; set; }
        public string? Operador { get; set; }
        public int? IdUbicacion { get; set; }
        public string? Ubicacion { get; set; }
        public string? Direccion { get; set; }
        public string? Ciudad { get; set; }
        public string? Estado { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public int? IdArea { get; set; }
        public string? Area { get; set; }
        public List<int> IdsClientes { get; set; } = new();
        public List<string> Clientes { get; set; } = new();
        public List<string> Tecnicos { get; set; } = new();

        // ── Textos para la vista ────────────────────────────────────────────

        public string MarcaTexto => string.IsNullOrWhiteSpace(Marca) ? "Sin marca" : Marca!;
        public string DescripcionTexto => Descripcion ?? string.Empty;
        public bool TieneDescripcion => !string.IsNullOrWhiteSpace(Descripcion);
        public string ClientesTexto => Clientes.Count == 0 ? "Sin cliente" : string.Join(" · ", Clientes);
        public bool SinCliente => Clientes.Count == 0;
        public string UbicacionTexto => string.IsNullOrWhiteSpace(Ubicacion) ? "Sin ubicación" : Ubicacion!;
        public string AreaTexto => string.IsNullOrWhiteSpace(Area) ? "Sin área" : Area!;
        public string UbicacionAreaTexto => IdUbicacion is null ? "Sin ubicación" : $"{UbicacionTexto} · {AreaTexto}";
        public string CiudadEstadoTexto => string.Join(", ", new[] { Ciudad, Estado }.Where(s => !string.IsNullOrWhiteSpace(s)));
        public string DireccionTexto => Direccion ?? string.Empty;
        public bool TieneDireccion => !string.IsNullOrWhiteSpace(Direccion) || !string.IsNullOrWhiteSpace(CiudadEstadoTexto);
        public string TecnicosTexto => Tecnicos.Count == 0 ? "Sin técnicos asignados al área" : string.Join("\n", Tecnicos);

        /// <summary>"16 paradas · 1,000 kg · 13 personas" con lo que haya.</summary>
        public string CapacidadTexto
        {
            get
            {
                var partes = new List<string>();
                if (Paradas is > 0) partes.Add($"{Paradas} paradas");
                if (Kilogramos is > 0) partes.Add($"{Kilogramos.Value.ToString("N0", CulturaMx)} kg");
                if (Personas is > 0) partes.Add($"{Personas} personas");
                return string.Join(" · ", partes);
            }
        }

        public string AnioTexto => AnioFabricacion?.ToString() ?? "—";
        public string ParadasTexto => Paradas?.ToString() ?? "—";
        public string KilogramosTexto => Kilogramos is null ? "—" : $"{Kilogramos.Value.ToString("N0", CulturaMx)} kg";
        public string PersonasTexto => Personas?.ToString() ?? "—";
        public string VelocidadTexto => Texto(Velocidad);
        public string ControladorTexto => Texto(Controlador);
        public string TipoMaquinaTexto => Texto(TipoMaquina);
        public string TipoPuertaTexto => Texto(TipoPuerta);
        public string OperadorTexto => Texto(Operador);

        // ── Búsqueda ───────────────────────────────────────────────────────

        private string? _textoBusqueda;

        /// <summary>Todo lo buscable en minúsculas y sin acentos.</summary>
        public string TextoBusqueda => _textoBusqueda ??= Normalizar(string.Join(" ", new[]
        {
            Identificador, Marca, Descripcion, Controlador, TipoMaquina, TipoPuerta, Velocidad, Operador,
            Ubicacion, Direccion, Ciudad, Estado, Area, AnioFabricacion?.ToString()
        }.Concat(Clientes).Concat(Tecnicos).Where(s => !string.IsNullOrWhiteSpace(s))));

        private static readonly CultureInfo CulturaMx = new("es-MX");

        private static string Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? "—" : valor!;

        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(descompuesto.Length);
            foreach (var c in descompuesto)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
