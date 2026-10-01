using System.Collections.Generic;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Catálogo de instituciones del SPEI (Banxico): los 3 primeros dígitos de una CLABE
    /// identifican al banco. Si la clave no está en el catálogo se devuelve "Banco NNN".
    /// </summary>
    public static class BancosMexico
    {
        private static readonly Dictionary<string, string> NombresPorClave = new()
        {
            ["002"] = "Banamex",
            ["006"] = "Bancomext",
            ["009"] = "Banobras",
            ["012"] = "BBVA",
            ["014"] = "Santander",
            ["019"] = "Banjército",
            ["021"] = "HSBC",
            ["030"] = "BanBajío",
            ["036"] = "Inbursa",
            ["042"] = "Mifel",
            ["044"] = "Scotiabank",
            ["058"] = "Banregio",
            ["059"] = "Invex",
            ["060"] = "Bansi",
            ["062"] = "Afirme",
            ["072"] = "Banorte",
            ["106"] = "Bank of America",
            ["112"] = "Monex",
            ["113"] = "Ve por Más",
            ["127"] = "Banco Azteca",
            ["130"] = "Compartamos",
            ["132"] = "Multiva",
            ["133"] = "Actinver",
            ["135"] = "Nafin",
            ["136"] = "Intercam",
            ["137"] = "BanCoppel",
            ["143"] = "CIBanco",
            ["152"] = "Bancrea",
            ["156"] = "Sabadell",
            ["166"] = "Banco del Bienestar",
            ["638"] = "Nu México",
            ["646"] = "STP",
            ["722"] = "Mercado Pago",
        };

        /// <summary>Nombre del banco dueño de la CLABE, o cadena vacía si no es una CLABE válida.</summary>
        public static string ObtenerNombrePorClabe(string? clabe)
        {
            var valor = clabe?.Trim();
            if (string.IsNullOrEmpty(valor) || valor.Length != 18 || !long.TryParse(valor, out _))
            {
                return string.Empty;
            }

            var clave = valor[..3];
            return NombresPorClave.TryGetValue(clave, out var nombre) ? nombre : $"Banco {clave}";
        }
    }
}
