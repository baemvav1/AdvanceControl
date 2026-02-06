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

    /// <summary>
    /// Configuración para Google Cloud Storage OAuth 2.0
    /// </summary>
    public class GoogleCloudStorageOptions
    {
        /// <summary>
        /// ID del cliente OAuth 2.0 de Google
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Secreto del cliente OAuth 2.0 de Google
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del bucket de Google Cloud Storage
        /// </summary>
        public string BucketName { get; set; } = "advance-control-cargo-images";

        /// <summary>
        /// URI de redirección para el flujo OAuth
        /// </summary>
        public string RedirectUri { get; set; } = "http://127.0.0.1:8484/";
    }
}
