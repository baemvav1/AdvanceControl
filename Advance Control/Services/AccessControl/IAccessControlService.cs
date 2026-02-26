namespace Advance_Control.Services.AccessControl
{
    public interface IAccessControlService
    {
        /// <summary>
        /// Nivel del usuario activo (1 = máximo acceso, mayor número = menor acceso).
        /// </summary>
        int NivelUsuario { get; }

        /// <summary>
        /// Establece el nivel del usuario (llamar tras cargar la sesión).
        /// </summary>
        void SetNivel(int nivel);

        /// <summary>
        /// Retorna true si el usuario puede acceder a un recurso con el nivel requerido.
        /// Regla: el usuario accede si su NivelUsuario es menor o igual al nivel requerido.
        /// Ej: usuario nivel 1 accede a todo; usuario nivel 3 solo accede a recursos de nivel ≥ 3.
        /// </summary>
        bool CanAccess(int requiredLevel);
    }
}
