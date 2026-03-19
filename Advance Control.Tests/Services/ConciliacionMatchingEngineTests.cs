using System;
using System.Collections.Generic;
using Advance_Control.Models;
using Advance_Control.Services.Conciliacion;

namespace Advance_Control.Tests.Services
{
    public class ConciliacionMatchingEngineTests
    {
        private readonly ConciliacionMatchingEngine _engine = new(new ConciliacionRulesProvider());

        [Fact]
        public void EsFacturaElegibleParaConciliacionUnoAUno_WhenFacturaSinAbonos_ReturnsTrue()
        {
            var factura = CrearFactura(total: 100m, saldoPendiente: 100m, totalAbonado: 0m, numeroAbonos: 0, finiquito: false);

            var resultado = _engine.EsFacturaElegibleParaConciliacionUnoAUno(factura);

            Assert.True(resultado);
        }

        [Fact]
        public void BuscarMovimientoCoincidente_WhenFacturaPpd_AcceptsPosteriorMonth()
        {
            var factura = CrearFactura(total: 150m, saldoPendiente: 150m, metodoPago: "PPD", fecha: new DateTime(2026, 1, 27));
            var movimientos = new List<ConciliacionMovimientoResumenDto>
            {
                CrearMovimiento(1, 150m, new DateTime(2025, 12, 31)),
                CrearMovimiento(2, 150m, new DateTime(2026, 2, 1))
            };

            var resultado = _engine.BuscarMovimientoCoincidente(movimientos, 150m, new[] { factura }, factura.Fecha);

            Assert.NotNull(resultado);
            Assert.Equal(2, resultado!.IdMovimiento);
        }

        [Fact]
        public void BuscarMovimientoCoincidente_WhenFacturaPue_RejectsPosteriorMonth()
        {
            var factura = CrearFactura(total: 150m, saldoPendiente: 150m, metodoPago: "PUE", fecha: new DateTime(2026, 1, 27));
            var movimientos = new List<ConciliacionMovimientoResumenDto>
            {
                CrearMovimiento(1, 150m, new DateTime(2026, 2, 1))
            };

            var resultado = _engine.BuscarMovimientoCoincidente(movimientos, 150m, new[] { factura }, factura.Fecha);

            Assert.Null(resultado);
        }

        [Fact]
        public void ObtenerMovimientosCandidatosParaFactura_RespectsConfiguredLimit()
        {
            var factura = CrearFactura(total: 500m, saldoPendiente: 500m, metodoPago: "PPD", fecha: new DateTime(2026, 1, 15));
            var movimientos = new List<ConciliacionMovimientoResumenDto>();

            for (var indice = 1; indice <= 20; indice++)
            {
                movimientos.Add(CrearMovimiento(indice, 10m, new DateTime(2026, 1, indice <= 28 ? indice : 28)));
            }

            var resultado = _engine.ObtenerMovimientosCandidatosParaFactura(movimientos, factura, 500m);

            Assert.Equal(15, resultado.Count);
        }

        [Fact]
        public void ObtenerMontoPendienteFactura_WhenFacturaTieneTotalCero_ReturnsZero()
        {
            var factura = CrearFactura(total: 0m, saldoPendiente: 125m);

            var resultado = _engine.ObtenerMontoPendienteFactura(factura);

            Assert.Equal(0m, resultado);
        }

        [Fact]
        public void CanRunAbonos_WhenOnlyZeroTotalInvoicesExist_ReturnsFalse()
        {
            var facturas = new List<FacturaResumenDto>
            {
                CrearFactura(idFactura: 1, total: 0m, saldoPendiente: 100m),
                CrearFactura(idFactura: 2, total: 0m, saldoPendiente: 50m)
            };
            var movimientos = new List<ConciliacionMovimientoResumenDto>
            {
                CrearMovimiento(1, 100m, new DateTime(2026, 1, 10)),
                CrearMovimiento(2, 50m, new DateTime(2026, 1, 11))
            };

            var resultado = _engine.CanRunAbonos(facturas, movimientos);

            Assert.False(resultado);
        }

        [Fact]
        public void CanRunCombinacional_WhenInvoicesHaveZeroTotal_ReturnsFalse()
        {
            var facturas = new List<FacturaResumenDto>
            {
                CrearFactura(idFactura: 1, total: 0m, saldoPendiente: 100m, receptorRfc: "XAXX010101000"),
                CrearFactura(idFactura: 2, total: 0m, saldoPendiente: 50m, receptorRfc: "XAXX010101000")
            };
            var movimientos = new List<ConciliacionMovimientoResumenDto>
            {
                CrearMovimiento(10, 150m, new DateTime(2026, 1, 22))
            };

            var resultado = _engine.CanRunCombinacional(facturas, movimientos);

            Assert.False(resultado);
        }

        [Fact]
        public void BuscarCombinacionFacturasCompatible_FindsExactGroupAndMatchingMovement()
        {
            var facturas = new List<FacturaResumenDto>
            {
                CrearFactura(idFactura: 1, total: 100m, saldoPendiente: 100m, metodoPago: "PUE", receptorRfc: "XAXX010101000", fecha: new DateTime(2026, 1, 5)),
                CrearFactura(idFactura: 2, total: 50m, saldoPendiente: 50m, metodoPago: "PPD", receptorRfc: "XAXX010101000", fecha: new DateTime(2026, 1, 20)),
                CrearFactura(idFactura: 3, total: 75m, saldoPendiente: 75m, metodoPago: "PUE", receptorRfc: "OTRO010101000", fecha: new DateTime(2026, 1, 25))
            };
            var movimientos = new List<ConciliacionMovimientoResumenDto>
            {
                CrearMovimiento(10, 150m, new DateTime(2026, 1, 22)),
                CrearMovimiento(11, 75m, new DateTime(2026, 1, 26))
            };

            var combinacion = _engine.BuscarCombinacionFacturasCompatible(facturas, movimientos, 150m, out var movimientoObjetivo);

            Assert.NotNull(combinacion);
            Assert.NotNull(movimientoObjetivo);
            Assert.Equal(new[] { 1, 2 }, combinacion!.Select(f => f.IdFactura).ToArray());
            Assert.Equal(10, movimientoObjetivo!.IdMovimiento);
        }

        private static FacturaResumenDto CrearFactura(
            int idFactura = 1,
            decimal total = 100m,
            decimal saldoPendiente = 100m,
            decimal totalAbonado = 0m,
            int numeroAbonos = 0,
            bool finiquito = false,
            string metodoPago = "PUE",
            string receptorRfc = "XAXX010101000",
            DateTime? fecha = null)
        {
            return new FacturaResumenDto
            {
                IdFactura = idFactura,
                Total = total,
                SaldoPendiente = saldoPendiente,
                TotalAbonado = totalAbonado,
                NumeroAbonos = numeroAbonos,
                Finiquito = finiquito,
                MetodoPago = metodoPago,
                ReceptorRfc = receptorRfc,
                Fecha = fecha ?? new DateTime(2026, 1, 1)
            };
        }

        private static ConciliacionMovimientoResumenDto CrearMovimiento(int idMovimiento, decimal abono, DateTime fecha)
        {
            return new ConciliacionMovimientoResumenDto
            {
                IdMovimiento = idMovimiento,
                GrupoId = $"MOV-{idMovimiento}",
                Abono = abono,
                Fecha = fecha,
                Referencia = $"REF-{idMovimiento}"
            };
        }
    }
}
