using System;
using Advance_Control.Models;
using Advance_Control.Utilities;

namespace Advance_Control.Tests.Utilities
{
    public class AbonoManualValidadorTests
    {
        private static readonly DateTime Hoy = new(2026, 10, 27);

        [Fact]
        public void Validar_AbonoCorrecto_SinError()
        {
            Assert.Null(AbonoManualValidador.Validar(Factura(), 500m, Hoy, "cheque", Hoy));
        }

        [Fact]
        public void Validar_FacturaCancelada_Rechaza()
        {
            Assert.NotNull(AbonoManualValidador.Validar(Factura(cancelada: true), 500m, Hoy, "spei", Hoy));
        }

        [Theory]
        [InlineData(0)]          // monto cero
        [InlineData(1000.01)]    // excede saldo
        [InlineData(10.555)]     // 3 decimales
        public void Validar_MontoInvalido_Rechaza(double monto)
        {
            Assert.NotNull(AbonoManualValidador.Validar(Factura(), (decimal)monto, Hoy, "spei", Hoy));
        }

        [Fact]
        public void Validar_FacturaSinSaldo_Rechaza()
        {
            Assert.NotNull(AbonoManualValidador.Validar(Factura(saldo: 0m), 1m, Hoy, "spei", Hoy));
        }

        [Fact]
        public void Validar_FechaFutura_Rechaza()
        {
            Assert.NotNull(AbonoManualValidador.Validar(Factura(), 500m, Hoy.AddDays(1), "spei", Hoy));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("tarjeta")]
        public void Validar_FormaPagoFueraDeCatalogo_Rechaza(string? forma)
        {
            Assert.NotNull(AbonoManualValidador.Validar(Factura(), 500m, Hoy, forma, Hoy));
        }

        [Fact]
        public void FechaParaGuardar_DevuelveMediodiaDelMismoDia()
        {
            var elegida = new DateTimeOffset(2026, 10, 5, 0, 0, 0, TimeSpan.FromHours(-6));

            Assert.Equal(new DateTime(2026, 10, 5, 12, 0, 0), AbonoManualValidador.FechaParaGuardar(elegida));
        }

        private static FacturaResumenDto Factura(decimal saldo = 1000m, bool cancelada = false) => new()
        {
            IdFactura = 1,
            Total = 1000m,
            SaldoPendiente = saldo,
            Cancelada = cancelada
        };
    }
}
