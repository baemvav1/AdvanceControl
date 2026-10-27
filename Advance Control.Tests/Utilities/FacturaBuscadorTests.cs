using System;
using System.Collections.Generic;
using Advance_Control.Models;
using Advance_Control.Utilities;

namespace Advance_Control.Tests.Utilities
{
    public class FacturaBuscadorTests
    {
        private static readonly Dictionary<string, string> Nombres = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SCO7505142JA"] = "Constructora Salazar"
        };

        private static readonly FacturaResumenDto Salazar = Factura("A", "1250", "SCO7505142JA", "SALAZAR CONSTRUCCIONES SA DE CV");
        private static readonly FacturaResumenDto SinCatalogo = Factura(null, "125", "GKI071109C82", "GRUPO KIMBERLEY");

        [Theory]
        [InlineData("1250", true)]
        [InlineData("A1250", true)]
        [InlineData("a-1250", true)]
        [InlineData("125", false)]   // folio exacto: 125 no es 1250
        [InlineData("B1250", false)]
        public void Folio_EsExacto(string texto, bool esperado)
        {
            Assert.Equal(esperado, FacturaBuscador.Coincide(Salazar, CampoBusquedaFactura.Folio, texto, Nombres));
        }

        [Fact]
        public void NombreComercial_UsaElCatalogoYSinCatalogoElReceptor()
        {
            Assert.True(FacturaBuscador.Coincide(Salazar, CampoBusquedaFactura.NombreComercial, "constructora", Nombres));
            Assert.False(FacturaBuscador.Coincide(Salazar, CampoBusquedaFactura.NombreComercial, "1250", Nombres));
            Assert.True(FacturaBuscador.Coincide(SinCatalogo, CampoBusquedaFactura.NombreComercial, "kimberley", Nombres));
        }

        [Fact]
        public void NombreComercial_IgnoraAcentos()
        {
            var factura = Factura(null, "1", "XAXX010101000", "ELEVACIÓN Y MÁS");

            Assert.True(FacturaBuscador.Coincide(factura, CampoBusquedaFactura.NombreComercial, "elevacion y mas", Nombres));
        }

        [Fact]
        public void Rfc_ParcialYSoloEnElRfc()
        {
            Assert.True(FacturaBuscador.Coincide(Salazar, CampoBusquedaFactura.Rfc, "sco750", Nombres));
            Assert.False(FacturaBuscador.Coincide(Salazar, CampoBusquedaFactura.Rfc, "salazar", Nombres));
        }

        [Fact]
        public void Sugerencias_DevuelveValoresDistintosDelCampo()
        {
            var facturas = new[] { Salazar, Factura("A", "1251", "SCO7505142JA", "SALAZAR CONSTRUCCIONES SA DE CV"), SinCatalogo };

            Assert.Equal(new[] { "Constructora Salazar" }, FacturaBuscador.Sugerencias(facturas, CampoBusquedaFactura.NombreComercial, "salaz", Nombres));
            Assert.Equal(new[] { "A1250", "A1251", "125" }, FacturaBuscador.Sugerencias(facturas, CampoBusquedaFactura.Folio, "125", Nombres));
        }

        private static FacturaResumenDto Factura(string? serie, string folio, string rfc, string receptor) => new()
        {
            Serie = serie,
            Folio = folio,
            ReceptorRfc = rfc,
            ReceptorNombre = receptor
        };
    }
}
