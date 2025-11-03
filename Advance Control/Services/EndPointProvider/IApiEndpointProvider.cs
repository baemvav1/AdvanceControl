using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.EndPointProvider
{
    public interface IApiEndpointProvider
    {
        /// <summary>
        /// Devuelve la URI absoluta (string) para la ruta relativa provista.
        /// Ejemplos de rutaRelative: "Online", "auth/login", "customers/123".
        /// </summary>
        string GetEndpoint(string routeRelative);

        /// <summary>
        /// Variante que permite pasar partes y las concatena correctamente.
        /// </summary>
        string GetEndpoint(params string[] routeParts);
    }
}
