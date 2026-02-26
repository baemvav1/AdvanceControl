namespace Advance_Control.Services.AccessControl
{
    /// <summary>
    /// Singleton que gestiona el nivel de acceso del usuario activo.
    /// NivelUsuario=1 por defecto (acceso máximo). Forzado en 1 al iniciar sesión hasta
    /// que se integre la selección real de niveles desde la tabla Nivel.
    /// </summary>
    public class AccessControlService : IAccessControlService
    {
        /// <summary>
        /// Instancia estática para uso desde converters y helpers donde DI no está disponible.
        /// </summary>
        public static IAccessControlService Current { get; private set; } = new AccessControlService();

        public int NivelUsuario { get; private set; } = 1;

        public AccessControlService()
        {
            Current = this;
        }

        public void SetNivel(int nivel)
        {
            NivelUsuario = nivel > 0 ? nivel : 1;
        }

        /// <inheritdoc/>
        public bool CanAccess(int requiredLevel)
        {
            return NivelUsuario <= requiredLevel;
        }
    }
}
