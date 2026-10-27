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
        [InlineData("A1250", true)]
        [InlineData("a-1250", true)]
        [InlineData("A 1250", true)]
        [InlineData("1250", false)]  // sin serie no encuentra la A1250
        [InlineData("A125", false)]
        [InlineData("B1250", false)]
        public void Folio_EsSerieMasFolioExacto(string texto, bool esperado)
        {
            Assert.Equal(esperado, FacturaBuscador.Coincide(Salazar, CampoBusquedaFactura.Folio, texto, Nombres));
        }

        [Fact]
        public void Folio_SinSerie_SoloConElFolio()
        {
            Assert.True(FacturaBuscador.Coincide(SinCatalogo, CampoBusquedaFactura.Folio, "125", Nombres));
            Assert.False(FacturaBuscador.Coincide(SinCatalogo, CampoBusquedaFactura.Folio, "A125", Nombres));
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
            Assert.Equal(new[] { "A1250", "A1251" }, FacturaBuscador.Sugerencias(facturas, CampoBusquedaFactura.Folio, "A125", Nombres));
            Assert.Equal(new[] { "125" }, FacturaBuscador.Sugerencias(facturas, CampoBusquedaFactura.Folio, "125", Nombres));
        }

        [Theory]
        [InlineData("cb07bb5d", true)]
        [InlineData("CB07BB5D-1877-4827-8613-B06FF7B27A57", true)]
        [InlineData("cb07bb5d1877", true)]  // sin guiones también
        [InlineData("B06FF7B27A57", true)]
        [InlineData("A1250", false)]
        public void Uuid_ParcialSinGuiones(string texto, bool esperado)
        {
            var factura = Factura("A", "1250", "SCO7505142JA", "SALAZAR CONSTRUCCIONES SA DE CV");
            factura.Uuid = "cb07bb5d-1877-4827-8613-b06ff7b27a57";

            Assert.Equal(esperado, FacturaBuscador.Coincide(factura, CampoBusquedaFactura.Uuid, texto, Nombres));
            Assert.False(FacturaBuscador.Coincide(SinCatalogo, CampoBusquedaFactura.Uuid, texto, Nombres));
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
