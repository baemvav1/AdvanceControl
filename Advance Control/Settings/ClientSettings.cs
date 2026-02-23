namespace Advance_Control.Settings
{
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
