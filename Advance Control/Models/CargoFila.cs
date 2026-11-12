using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Advance_Control.Models
{
    /// <summary>
    /// Fila plana del listado de cargos del visor de operaciones. Propiedades simples para
    /// que TableView ordene y filtre por valor (igual que las filas de Conciliación).
    /// Cantidad, Unitario y Nota se editan en la tabla; <see cref="Aplicar"/> avisa del cambio.
    /// </summary>
    public class CargoFila : INotifyPropertyChanged
    {
        public const int TipoServicio = 2;
        private static readonly CultureInfo CulturaMx = new("es-MX");

        public event PropertyChangedEventHandler? PropertyChanged;

        public int IdCargo { get; set; }
        public int? IdTipoCargo { get; set; }
        public int? IdRelacionCargo { get; set; }
        /// <summary>En servicios la cantidad solo puede ser entera.</summary>
        public bool EsServicio => IdTipoCargo == TipoServicio;
        public string Tipo { get; set; } = string.Empty;
        /// <summary>Título del detalle (marca en refacciones, concepto en servicios).</summary>
        public string Detalle { get; set; } = string.Empty;
        /// <summary>Descripción (refacciones; si no hay, la serie). Vacía en servicios.</summary>
        public string Descripcion { get; set; } = string.Empty;
        public bool TieneDescripcion => !string.IsNullOrWhiteSpace(Descripcion);
        /// <summary>Encabezado de sus fotos en la galería: "título descripción" (sin título, el tipo).</summary>
        public string TituloGaleria
        {
            get
            {
                var texto = string.Join(" ", new[] { Detalle, Descripcion }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
                return string.IsNullOrEmpty(texto) ? Tipo : texto;
            }
        }
        public double Cantidad { get; private set; }
        public double Unitario { get; private set; }
        public double Monto { get; private set; }
        public string Nota { get; private set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;

        public string CantidadTexto => Cantidad.ToString(CulturaMx);
        public string UnitarioTexto => Unitario.ToString("C2", CulturaMx);
        public string MontoTexto => Monto.ToString("C2", CulturaMx);

        /// <summary>Valor que se pone en la celda al empezar a editarla (sin formato de moneda).</summary>
        public string TextoParaEditar(string campo) => campo switch
        {
            nameof(Cantidad) => Cantidad.ToString(CulturaMx),
            nameof(Unitario) => Unitario.ToString("0.##", CulturaMx),
            _ => Nota
        };

        /// <summary>Cambia los valores editables, recalcula el monto y refresca las celdas.</summary>
        public void Aplicar(double cantidad, double unitario, string nota)
        {
            Cantidad = cantidad;
            Unitario = unitario;
            Monto = cantidad * unitario;
            Nota = nota;
            foreach (var propiedad in new[] { nameof(Cantidad), nameof(Unitario), nameof(Monto), nameof(Nota), nameof(CantidadTexto), nameof(UnitarioTexto), nameof(MontoTexto) })
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
            }
        }

        public static CargoFila Desde(CargoDto cargo) => new()
        {
            IdCargo = cargo.IdCargo,
            IdTipoCargo = cargo.IdTipoCargo,
            IdRelacionCargo = cargo.IdRelacionCargo,
            Tipo = cargo.TipoCargo ?? string.Empty,
            Detalle = cargo.DetalleLinea1 ?? string.Empty,
            Descripcion = !string.IsNullOrWhiteSpace(cargo.DetalleLinea3) ? cargo.DetalleLinea3! : cargo.DetalleLinea2 ?? string.Empty,
            Cantidad = cargo.Cantidad ?? 0d,
            Unitario = cargo.Unitario ?? 0d,
            Monto = cargo.Monto ?? 0d,
            Nota = cargo.Nota ?? string.Empty,
            Proveedor = cargo.Proveedor ?? string.Empty
        };
    }
}
