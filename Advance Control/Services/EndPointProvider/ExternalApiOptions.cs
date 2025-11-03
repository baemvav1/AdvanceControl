using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.EndPointProvider
{
    /// <summary>
    /// Opciones de configuración para la API externa.
    /// Se carga desde la sección "ExternalApi" en appsettings.json.
    /// </summary>
    public class ExternalApiOptions
    {
        /// <summary>
        /// URL base de la API externa.
        /// Ejemplo: "https://api.example.com/"
        /// </summary>
        public string BaseUrl { get; set; }
        
        /// <summary>
        /// Clave de API para autenticación (opcional).
        /// </summary>
        public string ApiKey { get; set; }
    }
}
