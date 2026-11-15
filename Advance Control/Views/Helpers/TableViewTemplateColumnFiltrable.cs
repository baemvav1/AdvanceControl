using System;
using System.Globalization;
using WinUI.TableView;

namespace Advance_Control.Views.Helpers
{
    /// <summary>
    /// Columna con plantilla que sí se puede filtrar. TableViewTemplateColumn de WinUI.TableView
    /// devuelve null en GetCellContent, así que su filtro de encabezado solo listaba "(vacío)".
    /// Aquí el valor del filtro es el de ClipboardContentBinding.
    /// </summary>
    public partial class TableViewTemplateColumnFiltrable : TableViewTemplateColumn
    {
        public override object? GetCellContent(object? dataItem) => GetClipboardContent(dataItem);
    }

    /// <summary>
    /// Monto para filtros y portapapeles: se ordena como número y se muestra como moneda.
    /// </summary>
    public readonly record struct MontoFiltro(decimal Valor) : IComparable<MontoFiltro>, IComparable
    {
        private static readonly CultureInfo CulturaMx = new("es-MX");

        public int CompareTo(MontoFiltro other) => Valor.CompareTo(other.Valor);

        public int CompareTo(object? obj) => obj is MontoFiltro otro ? CompareTo(otro) : 1;

        public override string ToString() => Valor.ToString("C2", CulturaMx);
    }
}
