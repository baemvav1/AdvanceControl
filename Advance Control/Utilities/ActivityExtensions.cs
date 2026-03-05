using Advance_Control.Services.Activity;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Métodos de extensión para IActivityService.
    /// Elimina el patrón repetido "_ = _activityService.CrearActividadAsync(...)" en todas las vistas.
    /// </summary>
    public static class ActivityExtensions
    {
        /// <summary>
        /// Registra una actividad de usuario de forma fire-and-forget (no bloquea el hilo de UI).
        /// </summary>
        public static void Registrar(this IActivityService service, string origen, string titulo)
            => _ = service.CrearActividadAsync(origen, titulo);
    }
}
