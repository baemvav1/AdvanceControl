using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Services.Security
{
    public interface ISecureStorage
    {
        /// <summary>
        /// Guarda un secreto identificado por key (sobrescribe si existe).
        /// </summary>
        Task SetAsync(string key, string value);

        /// <summary>
        /// Recupera el secreto por key. Devuelve null si no existe.
        /// </summary>
        Task<string?> GetAsync(string key);

        /// <summary>
        /// Elimina el secreto identificado por key.
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Elimina todos los secretos gestionados (si procede).
        /// </summary>
        Task ClearAsync();
    }
}
