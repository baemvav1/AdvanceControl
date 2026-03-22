namespace Advance_Control.Services.AccessControl
{
    /// <summary>
    /// Singleton que gestiona el nivel de acceso efectivo de la app.
    /// Cuando no hay sesión cargada, la app se mantiene en nivel 8 para bloquear
    /// navegación sensible desde la UI.
    /// </summary>
    public class AccessControlService : IAccessControlService
    {
        /// <summary>
        /// Instancia estática para uso desde converters y helpers donde DI no está disponible.
        /// </summary>
        public static IAccessControlService Current { get; private set; } = new AccessControlService();

        public int NivelUsuario { get; private set; } = 8;

        public AccessControlService()
        {
            Current = this;
        }

        public void SetNivel(int nivel)
        {
            NivelUsuario = nivel > 0 ? nivel : 8;
        }

        /// <inheritdoc/>
        public bool CanAccess(int requiredLevel)
        {
            return NivelUsuario <= requiredLevel;
        }
    }
}
