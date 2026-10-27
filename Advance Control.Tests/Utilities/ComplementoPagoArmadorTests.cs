using System;
using System.Collections.Generic;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.Utilities;

namespace Advance_Control.Tests.Utilities
{
    public class ComplementoPagoArmadorTests
    {
        private static readonly DateTime Dia = new(2026, 10, 20, 6, 0, 0);

        [Fact]
        public void Armar_UsaLaFormaDePagoDeCadaAbono()
        {
            var grupos = ComplementoPagoArmador.Armar(new[] { Abono(1, "cheque", Dia), Abono(2, "spei", Dia) }, null, null);

            Assert.Equal(2, grupos.Count);
            Assert.Contains(grupos, g => g.FormaPago == "02" && g.Doctos.Single().IdAbonoFactura == 1);
            Assert.Contains(grupos, g => g.FormaPago == "03" && g.Doctos.Single().IdAbonoFactura == 2);
        }

        [Fact]
        public void Armar_MismoDiaYForma_UnSoloPago()
        {
            var grupos = ComplementoPagoArmador.Armar(new[] { Abono(1, "transferencia", Dia), Abono(2, "spei", Dia.AddHours(3)) }, null, null);

            var pago = Assert.Single(grupos);
            Assert.Equal(Dia.Date, pago.FechaPago);
            Assert.Equal(2, pago.Doctos.Count);
        }

        [Fact]
        public void Armar_AbonoSinForma_UsaElCombo()
        {
            var pago = Assert.Single(ComplementoPagoArmador.Armar(new[] { Abono(1, null, Dia) }, "03", null));

            Assert.Equal("03", pago.FormaPago);
        }

        [Fact]
        public void Validar_AbonoSinFormaYSinCombo_Rechaza()
        {
            Assert.NotNull(ComplementoPagoArmador.Validar(new[] { Abono(1, null, Dia) }, null));
            Assert.Null(ComplementoPagoArmador.Validar(new[] { Abono(1, "cheque", Dia) }, null));
            Assert.NotNull(ComplementoPagoArmador.Validar(Array.Empty<AbonoPendienteComplementoDto>(), "03"));
        }

        [Fact]
        public void Armar_NumOperacion_TomaLaReferenciaDelAbonoSiNoSeCaptura()
        {
            var abono = Abono(1, "cheque", Dia, referencia: "CH-4521");

            Assert.Equal("CH-4521", ComplementoPagoArmador.Armar(new[] { abono }, null, "  ").Single().NumOperacion);
            Assert.Equal("OP-9", ComplementoPagoArmador.Armar(new[] { abono }, null, "OP-9").Single().NumOperacion);
        }

        private static AbonoPendienteComplementoDto Abono(int id, string? forma, DateTime fecha, string? referencia = null) => new()
        {
            IdAbonoFactura = id,
            IdFactura = 10,
            FechaAbono = fecha,
            MontoAbono = 100m,
            FormaPago = forma,
            Referencia = referencia,
            NumParcialidad = id,
            ImpSaldoAnt = 1000m,
            ImpSaldoInsoluto = 900m
        };
    }
}
