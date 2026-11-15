using System;
using System.Globalization;

namespace Advance_Control.Models
{
    /// <summary>
    /// Fila plana del listado de movimientos pendientes de Conciliación.
    /// </summary>
    public class ConciliacionMovimientoFila
    {
        public int IdMovimiento { get; set; }
        /// <summary>Posición de carga (fecha, id); desempate estable al ordenar en la tabla.</summary>
        public int Orden { get; set; }
        public decimal Abono { get; set; }

        /// <summary>Parte del abono aún sin aplicar a facturas (menor al abono si ya se usó en parte).</summary>
        public decimal MontoRestante { get; set; }
        public string Metadatos { get; set; } = string.Empty;

        /// <summary>RFC del emisor; en cheques, el folio del cheque (metadato FOLIO_CHEQUE).</summary>
        public string RfcReferencia { get; set; } = string.Empty;

        public string Banco { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }

        /// <summary>Metadatos uno por línea, para el tooltip de la celda.</summary>
        public string MetadatosTooltip { get; set; } = string.Empty;

        /// <summary>Movimiento y estado de cuenta originales, para abrirlos en el visor.</summary>
        public EstadoCuentaGrupoDetalleDto Grupo { get; set; } = null!;
        public EstadoCuentaResumenDto EstadoCuenta { get; set; } = null!;

        public string AbonoTexto => Abono.ToString("C2", new CultureInfo("es-MX"));
        public string MontoRestanteTexto => MontoRestante.ToString("C2", new CultureInfo("es-MX"));
        /// <summary>Valor del filtro y del portapapeles de la columna Abono (ordena como número).</summary>
        public Views.Helpers.MontoFiltro MontoRestanteFiltro => new(decimal.Round(MontoRestante, 2));
        public bool TieneAbonoParcial => MontoRestante < Abono;

        /// <summary>
        /// La columna Abono del grid muestra lo que queda disponible; si ya se aplicó en parte,
        /// el tooltip muestra el abono original del estado de cuenta y lo ya aplicado.
        /// </summary>
        public string? AbonoTooltip => TieneAbonoParcial
            ? $"Abono original: {AbonoTexto}" + System.Environment.NewLine
              + $"Ya aplicado: {(Abono - MontoRestante).ToString("C2", new CultureInfo("es-MX"))}"
            : null;
        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
    }
}
