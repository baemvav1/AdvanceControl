using Advance_Control.ViewModels;

namespace Advance_Control.Tests.ViewModels
{
    public class EsCuentaViewModelParsingTests
    {
        [Fact]
        public void ExtraerValoresMonetarios_ReconoceSignoFinalNegativo()
        {
            var valores = EsCuentaViewModel.ExtraerValoresMonetarios("ENVIO SPEI $ 24,400.00 $ 28,489.95-");

            Assert.Equal(2, valores.Count);
            Assert.Equal(24400.00m, valores[0]);
            Assert.Equal(-28489.95m, valores[1]);
        }

        [Fact]
        public void InferirMontosDesdeDescripcion_ClasificaSpeiRecibidoComoAbono()
        {
            const decimal saldoAnterior = 535.88m;
            const string descripcion = "SPEI RECIBIDO DE 044-SCOTIABANK 3 $ 24,592.00 $ 25,127.88 CUENTA:0441800010762";

            var resultado = EsCuentaViewModel.InferirMontosDesdeDescripcion("SPEI_RECIBIDO", descripcion, saldoAnterior);

            Assert.Equal(24592.00m, resultado.MontoAbono);
            Assert.Null(resultado.MontoCargo);
            Assert.Equal(25127.88m, resultado.SaldoResultante);
        }

        [Fact]
        public void InferirMontosDesdeDescripcion_ClasificaEnvioSpeiComoCargo_AunConSaldoAtipico()
        {
            const decimal saldoAnterior = 4089.95m;
            const string descripcion = "ENVIO SPEI (AFIRMENET) A 012-BBVA MEXICO 12 $ 24,400.00 $ 28,489.95- CUENTA:012540004656663099";

            var resultado = EsCuentaViewModel.InferirMontosDesdeDescripcion("ENVIO_SPEI", descripcion, saldoAnterior);

            Assert.Null(resultado.MontoAbono);
            Assert.Equal(24400.00m, resultado.MontoCargo);
            Assert.Equal(-20310.05m, resultado.SaldoResultante);
        }
    }
}
