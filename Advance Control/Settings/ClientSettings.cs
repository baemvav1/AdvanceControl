using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Settings
{
    public class ClientSettings
    {
        public string? Theme { get; set; }
        public string? Language { get; set; }
        public bool RememberLogin { get; set; }
        public int DefaultTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Timeout para operaciones de autenticación en segundos
        /// </summary>
        public int AuthTimeoutSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Configuración para el modo de desarrollo
    /// </summary>
    public class DevelopmentModeOptions
    {
        /// <summary>
        /// Indica si el modo de desarrollo está habilitado
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Desactiva los timeouts de autenticación (expiración de tokens)
        /// </summary>
        public bool DisableAuthTimeouts { get; set; } = false;

        /// <summary>
        /// Desactiva los timeouts de HTTP
        /// </summary>
        public bool DisableHttpTimeouts { get; set; } = false;
    }
}
