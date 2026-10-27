using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Advance_Control.Models;

namespace Advance_Control.Utilities
{
    public enum CampoBusquedaFactura
    {
        Rfc,
        NombreComercial,
        Folio
    }

    /// <summary>
    /// Búsqueda del listado de facturas por un solo campo elegido en el selector:
    /// RFC (parcial), nombre comercial (parcial, sin acentos) o folio (exacto, con o sin serie).
    /// </summary>
    public static class FacturaBuscador
    {
        /// <summary>
        /// Nombre comercial del cliente (catálogo, por RFC). Si el cliente no está en el catálogo
        /// o no tiene nombre comercial, se usa el nombre del receptor del CFDI.
        /// </summary>
        public static string? NombreComercial(FacturaResumenDto factura, IReadOnlyDictionary<string, string> nombresPorRfc)
        {
            if (!string.IsNullOrWhiteSpace(factura.ReceptorRfc)
                && nombresPorRfc.TryGetValue(factura.ReceptorRfc.Trim().ToUpperInvariant(), out var nombre)
                && !string.IsNullOrWhiteSpace(nombre))
            {
                return nombre.Trim();
            }

            return factura.ReceptorNombre?.Trim();
        }

        public static bool Coincide(FacturaResumenDto factura, CampoBusquedaFactura campo, string? texto, IReadOnlyDictionary<string, string> nombresPorRfc)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            return campo switch
            {
                CampoBusquedaFactura.Rfc => NormalizarClave(factura.ReceptorRfc).Contains(NormalizarClave(texto), StringComparison.Ordinal),
                CampoBusquedaFactura.NombreComercial => NormalizarNombre(NombreComercial(factura, nombresPorRfc)).Contains(NormalizarNombre(texto), StringComparison.Ordinal),
                CampoBusquedaFactura.Folio => CoincideFolio(factura, texto),
                _ => false
            };
        }

        /// <summary>Valores distintos del campo elegido que empiezan con (folio) o contienen (RFC/nombre) el texto.</summary>
        public static List<string> Sugerencias(IEnumerable<FacturaResumenDto> facturas, CampoBusquedaFactura campo, string? texto, IReadOnlyDictionary<string, string> nombresPorRfc, int maximo = 8)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return new List<string>();
            }

            IEnumerable<string?> valores = campo switch
            {
                CampoBusquedaFactura.Rfc => facturas.Where(f => Coincide(f, campo, texto, nombresPorRfc)).Select(f => f.ReceptorRfc?.Trim().ToUpperInvariant()),
                CampoBusquedaFactura.NombreComercial => facturas.Where(f => Coincide(f, campo, texto, nombresPorRfc)).Select(f => NombreComercial(f, nombresPorRfc)),
                _ => facturas.Where(f => NormalizarClave(f.FolioTitulo).StartsWith(NormalizarClave(texto), StringComparison.Ordinal)
                        || NormalizarClave(f.Folio).StartsWith(NormalizarClave(texto), StringComparison.Ordinal))
                    .Select(f => f.FolioTitulo)
            };

            return valores
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maximo)
                .ToList();
        }

        /// <summary>"123", "A123", "A-123" y "a 123" encuentran la factura A123; "12" no encuentra la 123.</summary>
        private static bool CoincideFolio(FacturaResumenDto factura, string texto)
        {
            var buscado = NormalizarClave(texto);
            var folio = NormalizarClave(factura.Folio);
            if (folio.Length == 0)
            {
                return false;
            }

            return buscado == folio || buscado == NormalizarClave(factura.Serie) + folio;
        }

        private static string NormalizarClave(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(valor.Length);
            foreach (var c in valor)
            {
                if (!char.IsWhiteSpace(c) && c != '-')
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
            }

            return sb.ToString();
        }

        private static string NormalizarNombre(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            var descompuesto = valor.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(descompuesto.Length);
            foreach (var c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
            }

            return string.Join(' ', sb.ToString().Normalize(NormalizationForm.FormC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
