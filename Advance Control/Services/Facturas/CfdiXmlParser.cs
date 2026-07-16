using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Advance_Control.Models;

namespace Advance_Control.Services.Facturas
{
    /// <summary>
    /// Parseo de XML CFDI compartido entre el módulo Facturas (importación global)
    /// y el módulo Facturación (vinculación XML ↔ operación).
    /// </summary>
    public static class CfdiXmlParser
    {
        public static GuardarFacturaRequestDto ParseXmlToRequest(string xmlContent)
        {
            var doc = XDocument.Parse(xmlContent);
            var comprobante = doc.Root ?? throw new InvalidOperationException("El XML no contiene el nodo Comprobante.");
            var emisor = ElementByLocalName(comprobante, "Emisor");
            var receptor = ElementByLocalName(comprobante, "Receptor");
            var impuestos = ElementByLocalName(comprobante, "Impuestos");
            var timbre = ElementByLocalName(ElementByLocalName(comprobante, "Complemento"), "TimbreFiscalDigital");

            var conceptos = comprobante
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Conceptos", StringComparison.OrdinalIgnoreCase))?
                .Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Concepto", StringComparison.OrdinalIgnoreCase))
                .Select((concepto, index) => CrearConcepto(concepto, index + 1))
                .ToList() ?? new List<FacturaConceptoDto>();

            var trasladosGlobales = impuestos?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Traslados", StringComparison.OrdinalIgnoreCase))?
                .Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Traslado", StringComparison.OrdinalIgnoreCase))
                .Select((traslado, index) => CrearTraslado(traslado, index + 1))
                .ToList() ?? new List<FacturaTrasladoDto>();

            return new GuardarFacturaRequestDto
            {
                VersionXml = GetStringAttr(comprobante, "Version") ?? "4.0",
                Folio = GetStringAttr(comprobante, "Folio"),
                Fecha = GetDateTimeAttr(comprobante, "Fecha") ?? DateTime.Now,
                FormaPago = GetStringAttr(comprobante, "FormaPago"),
                NoCertificado = GetStringAttr(comprobante, "NoCertificado"),
                Certificado = GetStringAttr(comprobante, "Certificado"),
                Sello = GetStringAttr(comprobante, "Sello"),
                CondicionesDePago = GetStringAttr(comprobante, "CondicionesDePago"),
                SubTotal = GetDecimalAttr(comprobante, "SubTotal"),
                Moneda = GetStringAttr(comprobante, "Moneda") ?? "MXN",
                Total = GetDecimalAttr(comprobante, "Total"),
                TipoDeComprobante = GetStringAttr(comprobante, "TipoDeComprobante"),
                Exportacion = GetStringAttr(comprobante, "Exportacion"),
                MetodoPago = GetStringAttr(comprobante, "MetodoPago"),
                LugarExpedicion = GetStringAttr(comprobante, "LugarExpedicion"),
                TotalImpuestosTrasladados = GetDecimalAttr(impuestos, "TotalImpuestosTrasladados"),
                EmisorRfc = GetStringAttr(emisor, "Rfc"),
                EmisorNombre = GetStringAttr(emisor, "Nombre"),
                EmisorRegimenFiscal = GetStringAttr(emisor, "RegimenFiscal"),
                ReceptorRfc = GetStringAttr(receptor, "Rfc"),
                ReceptorNombre = GetStringAttr(receptor, "Nombre"),
                ReceptorDomicilioFiscal = GetStringAttr(receptor, "DomicilioFiscalReceptor"),
                ReceptorRegimenFiscal = GetStringAttr(receptor, "RegimenFiscalReceptor"),
                ReceptorUsoCfdi = GetStringAttr(receptor, "UsoCFDI"),
                Uuid = GetStringAttr(timbre, "UUID"),
                FechaTimbrado = GetDateTimeAttr(timbre, "FechaTimbrado"),
                RfcProvCertif = GetStringAttr(timbre, "RfcProvCertif"),
                NoCertificadoSat = GetStringAttr(timbre, "NoCertificadoSAT"),
                SelloCfd = GetStringAttr(timbre, "SelloCFD"),
                SelloSat = GetStringAttr(timbre, "SelloSAT"),
                XmlContenido = xmlContent,
                Conceptos = conceptos,
                TrasladosGlobales = trasladosGlobales
            };
        }

        private static FacturaConceptoDto CrearConcepto(XElement concepto, int orden)
        {
            var traslados = concepto
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Impuestos", StringComparison.OrdinalIgnoreCase))?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Traslados", StringComparison.OrdinalIgnoreCase))?
                .Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Traslado", StringComparison.OrdinalIgnoreCase))
                .Select((traslado, index) => CrearTraslado(traslado, index + 1))
                .ToList() ?? new List<FacturaTrasladoDto>();

            return new FacturaConceptoDto
            {
                Orden = orden,
                ClaveProdServ = GetStringAttr(concepto, "ClaveProdServ"),
                Cantidad = GetDecimalAttr(concepto, "Cantidad"),
                ClaveUnidad = GetStringAttr(concepto, "ClaveUnidad"),
                Unidad = GetStringAttr(concepto, "Unidad"),
                Descripcion = GetStringAttr(concepto, "Descripcion") ?? string.Empty,
                ValorUnitario = GetDecimalAttr(concepto, "ValorUnitario"),
                Importe = GetDecimalAttr(concepto, "Importe"),
                ObjetoImp = GetStringAttr(concepto, "ObjetoImp"),
                Traslados = traslados
            };
        }

        private static FacturaTrasladoDto CrearTraslado(XElement traslado, int orden)
        {
            return new FacturaTrasladoDto
            {
                Orden = orden,
                Base = GetDecimalAttr(traslado, "Base"),
                Impuesto = GetStringAttr(traslado, "Impuesto"),
                TipoFactor = GetStringAttr(traslado, "TipoFactor"),
                TasaOCuota = GetDecimalAttr(traslado, "TasaOCuota"),
                Importe = GetDecimalAttr(traslado, "Importe")
            };
        }

        internal static XElement? ElementByLocalName(XElement? parent, string localName)
            => parent?
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));

        private static string? GetStringAttr(XElement? element, string attributeName)
            => element?.Attribute(attributeName)?.Value;

        internal static decimal GetDecimalAttr(XElement? element, string attributeName)
        {
            var value = GetStringAttr(element, attributeName);
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;
        }

        private static DateTime? GetDateTimeAttr(XElement? element, string attributeName)
        {
            var value = GetStringAttr(element, attributeName);
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : null;
        }
    }
}
