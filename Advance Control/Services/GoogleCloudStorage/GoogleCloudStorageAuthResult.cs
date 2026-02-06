namespace Advance_Control.Services.GoogleCloudStorage
{
    /// <summary>
    /// Representa el resultado de una operación de autenticación con Google Cloud Storage
    /// </summary>
    public class GoogleCloudStorageAuthResult
    {
        /// <summary>
        /// Indica si la autenticación fue exitosa
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Código de error de OAuth (si aplica)
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Mensaje de error detallado
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Mensaje de error amigable para mostrar al usuario
        /// </summary>
        public string? UserFriendlyMessage { get; set; }

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static GoogleCloudStorageAuthResult Succeeded()
        {
            return new GoogleCloudStorageAuthResult { Success = true };
        }

        /// <summary>
        /// Crea un resultado fallido con información del error
        /// </summary>
        /// <param name="errorCode">Código de error de OAuth (ej: access_denied, org_internal)</param>
        /// <param name="errorMessage">Mensaje de error descriptivo</param>
        /// <param name="userFriendlyMessage">Mensaje amigable para mostrar al usuario. Si es null, se genera automáticamente basado en el código de error.</param>
        public static GoogleCloudStorageAuthResult Failed(string errorCode, string errorMessage, string? userFriendlyMessage = null)
        {
            return new GoogleCloudStorageAuthResult
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                UserFriendlyMessage = userFriendlyMessage ?? GetUserFriendlyMessage(errorCode)
            };
        }

        /// <summary>
        /// Obtiene un mensaje amigable basado en el código de error
        /// </summary>
        private static string GetUserFriendlyMessage(string errorCode)
        {
            return errorCode switch
            {
                "access_denied" => "El usuario denegó el acceso. Por favor, autorice la aplicación para continuar.",
                "org_internal" => "El cliente OAuth está configurado solo para uso interno de la organización. " +
                                  "El administrador debe cambiar el tipo de usuario a 'Externo' en Google Cloud Console " +
                                  "(APIs y servicios > Pantalla de consentimiento OAuth).",
                "invalid_client" => "La configuración del cliente OAuth es inválida. Verifique el ClientId y ClientSecret.",
                "invalid_grant" => "El código de autorización ha expirado o ya fue utilizado. Intente nuevamente.",
                "invalid_scope" => "Los permisos solicitados no son válidos para esta aplicación.",
                "unauthorized_client" => "El cliente no está autorizado para usar este flujo de autenticación.",
                "cancelled" => "La autenticación fue cancelada por el usuario.",
                "timeout" => "La autenticación excedió el tiempo de espera. Por favor, intente nuevamente.",
                "state_mismatch" => "Error de seguridad: el estado de la solicitud no coincide. Intente nuevamente.",
                "no_code" => "No se recibió el código de autorización de Google.",
                _ => $"Error de autenticación: {errorCode}. Por favor, intente nuevamente."
            };
        }
    }
}
