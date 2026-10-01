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
        public bool TieneAbonoParcial => MontoRestante < Abono;

        /// <summary>Tooltip del abono: solo si ya se aplicó en parte, muestra lo que queda disponible.</summary>
        public string? AbonoTooltip => TieneAbonoParcial ? $"Disponible: {MontoRestanteTexto}" : null;
        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
    }
}
