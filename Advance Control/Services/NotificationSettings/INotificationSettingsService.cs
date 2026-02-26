using System.Collections.Generic;
using System.Threading.Tasks;

namespace Advance_Control.Services.NotificationSettings
{
    /// <summary>
    /// Gestiona la lista de categorías de notificaciones permitidas en el dashboard.
    /// Los registros se persisten localmente (ApplicationData.Current.LocalFolder)
    /// y se crean automáticamente conforme el usuario accede a las distintas áreas de la app.
    /// </summary>
    public interface INotificationSettingsService
    {
        /// <summary>
        /// Si la categoría no existe en la lista, la agrega con Habilitada=true y persiste el archivo.
        /// </summary>
        Task EnsureCategoryRegisteredAsync(string? categoria, string? page = null);

        /// <summary>
        /// Activa o desactiva una categoría de notificaciones y persiste el cambio.
        /// </summary>
        Task SetCategoryEnabledAsync(string categoria, bool enabled);

        /// <summary>
        /// Devuelve true si el item debe mostrarse en el dashboard:
        /// categoría nula/vacía → siempre visible; de lo contrario debe existir y tener Habilitada=true.
        /// </summary>
        bool IsCategoryAllowed(string? categoria);

        /// <summary>
        /// Retorna una copia de todas las entradas registradas (para futuro panel de gestión).
        /// </summary>
        IReadOnlyList<NotificacionPermitidaEntry> GetAll();
    }

    /// <summary>
    /// Entrada individual en el listado de notificaciones permitidas.
    /// </summary>
    public class NotificacionPermitidaEntry
    {
        public string Categoria { get; set; } = string.Empty;
        /// <summary>Página de origen del primer log que generó esta entrada.</summary>
        public string? Page { get; set; }
        /// <summary>Habilita o deshabilita la notificación en el dashboard.</summary>
        public bool Habilitada { get; set; } = true;
    }
}
