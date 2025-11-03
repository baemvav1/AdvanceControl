using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.OnlineCheck
{
    /// <summary>
    /// Representa el resultado de una verificación de disponibilidad de API.
    /// </summary>
    public class OnlineCheckResult
    {
        /// <summary>
        /// Indica si la API está disponible y respondiendo correctamente.
        /// </summary>
        public bool IsOnline { get; set; }
        
        /// <summary>
        /// Código de estado HTTP recibido de la API (si aplica).
        /// Null si no se pudo establecer conexión HTTP.
        /// </summary>
        public int? StatusCode { get; set; }
        
        /// <summary>
        /// Mensaje de error descriptivo si la verificación falló.
        /// Null si la verificación fue exitosa.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Crea un resultado exitoso indicando que la API está disponible.
        /// </summary>
        /// <returns>OnlineCheckResult con IsOnline = true y StatusCode = 200.</returns>
        public static OnlineCheckResult Success() =>
            new OnlineCheckResult { IsOnline = true, StatusCode = 200, ErrorMessage = null };

        /// <summary>
        /// Crea un resultado basado en un código de estado HTTP.
        /// </summary>
        /// <param name="statusCode">Código de estado HTTP recibido.</param>
        /// <param name="errorMessage">Mensaje descriptivo del error (opcional).</param>
        /// <returns>
        /// OnlineCheckResult con IsOnline = true si el código está entre 200-299,
        /// IsOnline = false en caso contrario.
        /// </returns>
        public static OnlineCheckResult FromHttpStatus(int statusCode, string errorMessage = null) =>
            new OnlineCheckResult { IsOnline = statusCode >= 200 && statusCode <= 299, StatusCode = statusCode, ErrorMessage = errorMessage };

        /// <summary>
        /// Crea un resultado de error basado en un mensaje de excepción.
        /// </summary>
        /// <param name="message">Mensaje de error de la excepción.</param>
        /// <returns>OnlineCheckResult con IsOnline = false, StatusCode = null y el mensaje de error.</returns>
        public static OnlineCheckResult FromException(string message) =>
            new OnlineCheckResult { IsOnline = false, StatusCode = null, ErrorMessage = message };
    }
}
