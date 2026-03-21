using System.Xml.Linq;
using Advance_Control.ViewModels;

namespace Advance_Control.Tests.ViewModels
{
    public class EsCuentaViewModelNamespaceTests
    {
        [Fact]
        public void ObtenerNamespaceEstadoCuenta_UsaNamespaceDelRootAunqueSeaPrefijado()
        {
            var xml = """
                <ns0:estadoCuenta xmlns:ns0="http://www.afirme.com/estado-cuenta/v2" version="2.0">
                  <ns0:informacionGeneral>
                    <ns0:cuenta>
                      <ns0:numero>000143132927</ns0:numero>
                      <ns0:clabe>062540001431329271</ns0:clabe>
                    </ns0:cuenta>
                  </ns0:informacionGeneral>
                </ns0:estadoCuenta>
                """;

            var raiz = XDocument.Parse(xml).Root!;
            var ns = EsCuentaViewModel.ObtenerNamespaceEstadoCuenta(raiz);
            var cuenta = raiz.Element(ns + "informacionGeneral")?.Element(ns + "cuenta");

            Assert.Equal("http://www.afirme.com/estado-cuenta/v2", ns.NamespaceName);
            Assert.Equal("000143132927", cuenta?.Element(ns + "numero")?.Value);
            Assert.Equal("062540001431329271", cuenta?.Element(ns + "clabe")?.Value);
        }

        [Fact]
        public void LeerMontosExplicitos_AceptaNodosEnSingular()
        {
            var montos = XElement.Parse("""
                <montos>
                  <deposito>0.00</deposito>
                  <retiro>3189.93</retiro>
                  <saldo>0.00</saldo>
                </montos>
                """);

            var resultado = EsCuentaViewModel.LeerMontosExplicitos(montos);

            Assert.NotNull(resultado);
            Assert.Null(resultado.Value.Abono);
            Assert.Equal(3189.93m, resultado.Value.Cargo);
            Assert.Equal(0.00m, resultado.Value.Saldo);
        }
    }
}
