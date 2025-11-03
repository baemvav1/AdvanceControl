using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.OnlineCheck
{
    public interface IOnlineCheck
    {
        /// <summary>
        /// Comprueba la conexión contra la URL configurada de la API.
        /// Devuelve OnlineCheckResult.IsOnline = true si la respuesta HTTP es 2xx.
        /// En caso contrario devuelve IsOnline = false y StatusCode o ErrorMessage con detalles.
        /// </summary>
        Task<OnlineCheckResult> CheckAsync(CancellationToken cancellationToken = default);
    }
}
