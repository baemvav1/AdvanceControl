using System.Globalization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Fila plana del listado de cargos del prototipo de operaciones. Propiedades simples para
    /// que TableView ordene y filtre por valor (igual que las filas de Conciliación).
    /// </summary>
    public class ProCargoFila
    {
        private static readonly CultureInfo CulturaMx = new("es-MX");

        public int IdCargo { get; set; }
        /// <summary>Posición de carga; desempate estable al ordenar en la tabla.</summary>
        public int Orden { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public double Cantidad { get; set; }
        public double Unitario { get; set; }
        public double Monto { get; set; }
        public string Nota { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;

        /// <summary>Renglón en blanco del final: doble clic abre "Agregar cargo".</summary>
        public bool EsNuevo { get; private init; }

        public string CantidadTexto => EsNuevo ? string.Empty : Cantidad.ToString(CulturaMx);
        public string UnitarioTexto => EsNuevo ? string.Empty : Unitario.ToString("C2", CulturaMx);
        public string MontoTexto => EsNuevo ? string.Empty : Monto.ToString("C2", CulturaMx);

        public static ProCargoFila Nuevo(int orden) => new() { Orden = orden, EsNuevo = true };

        public static ProCargoFila Desde(CargoDto cargo, int orden) => new()
        {
            IdCargo = cargo.IdCargo,
            Orden = orden,
            Tipo = cargo.TipoCargo ?? string.Empty,
            Detalle = cargo.DetalleLinea1 ?? string.Empty,
            Cantidad = cargo.Cantidad ?? 0d,
            Unitario = cargo.Unitario ?? 0d,
            Monto = cargo.Monto ?? 0d,
            Nota = cargo.Nota ?? string.Empty,
            Proveedor = cargo.Proveedor ?? string.Empty
        };
    }
}
