using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Advance_Control.Models
{
    public sealed class ConciliacionMatchPropuestaDto
    {
        public required string Tipo { get; init; }
        public required List<FacturaResumenDto> Facturas { get; init; }
        public required ConciliacionMovimientoResumenDto Movimiento { get; init; }
        public List<ConciliacionMovimientoResumenDto> MovimientosAdicionales { get; init; } = new();
        public required string Observaciones { get; init; }
        public bool Aprobado { get; set; } = true;

        // Helpers para display en diálogo
        public bool EsAbonos => string.Equals(Tipo, "Abonos", StringComparison.OrdinalIgnoreCase);
        public bool EsUnoAUno => string.Equals(Tipo, "1 a 1", StringComparison.OrdinalIgnoreCase);
        public bool EsGenerica => !EsAbonos;
        public FacturaResumenDto? FacturaPrincipal => Facturas.FirstOrDefault();
        public string FoliosTexto => string.Join(", ", Facturas.Select(f => f.Folio ?? "-"));
        public decimal TotalFacturas => Facturas.Sum(f => f.Total);
        public string ReceptorNombre => Facturas.FirstOrDefault()?.ReceptorNombre ?? "-";
        public string FacturaObjetivoFolio => FacturaPrincipal?.Folio ?? "-";
        public string FacturaObjetivoFechaTexto => FacturaPrincipal?.FechaTexto ?? string.Empty;
        public string FacturaObjetivoSaldoTexto => FacturaPrincipal?.SaldoPendienteTexto ?? 0m.ToString("C2", new CultureInfo("es-MX"));
        public string FacturaObjetivoMetodoPagoTexto => FacturaPrincipal?.MetodoFormaPagoTexto ?? "Sin metodo";
        public string FacturaObjetivoEstadoTexto => FacturaPrincipal?.EstadoPagoTexto ?? "Sin estado";
        public string FacturaObjetivoReceptorTexto => FacturaPrincipal?.ReceptorNombre ?? "Sin receptor";
        public int CantidadMovimientos => TodosLosMovimientos.Count;
        public string CantidadMovimientosTexto => CantidadMovimientos == 1
            ? "1 abono propuesto"
            : $"{CantidadMovimientos} abonos propuestos";
        public IReadOnlyList<ConciliacionAbonoMovimientoItemDto> MovimientosDetalleAbonos => TodosLosMovimientos
            .Select(movimiento => new ConciliacionAbonoMovimientoItemDto
            {
                IdFactura = FacturaPrincipal?.IdFactura ?? 0,
                FolioFactura = FacturaObjetivoFolio,
                Movimiento = movimiento
            })
            .ToList();

        // Para abonos: suma de todos los movimientos; para 1a1/combinacional: solo el principal
        public decimal MontoAbonoTotal => Movimiento.Abono + MovimientosAdicionales.Sum(m => m.Abono);

        // Metadato concatenado de todos los movimientos del grupo
        public string MetadatosAgregados => MovimientosAdicionales.Count == 0
            ? Movimiento.MetadatosTexto
            : string.Join(" | ", new[] { Movimiento.MetadatosTexto }
                .Concat(MovimientosAdicionales.Select(m => m.MetadatosTexto)));

        public IReadOnlyList<ConciliacionMovimientoResumenDto> TodosLosMovimientos =>
            new[] { Movimiento }.Concat(MovimientosAdicionales).ToList();
    }
}
