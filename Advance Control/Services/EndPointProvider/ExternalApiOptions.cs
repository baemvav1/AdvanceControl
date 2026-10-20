using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.EndPointProvider
{
    public class ExternalApiOptions
    {
        /// <summary>
        /// URL de la API en modo desarrollo (localhost)
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL de la API en modo producción (VPS remoto)
        /// </summary>
        public string ProductionUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL de la API del entorno de pruebas (se usa en lugar de ProductionUrl cuando en el
        /// login se eligió "Pruebas"; ver Utilities.EntornoApp)
        /// </summary>
        public string PruebasUrl { get; set; } = "https://advance-elevadores.mx/pruebas/";
    }
}