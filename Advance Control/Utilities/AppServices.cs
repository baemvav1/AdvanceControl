using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// Acceso estático al contenedor de inyección de dependencias.
    /// Elimina el patrón repetido ((App)Application.Current).Host.Services.GetRequiredService&lt;T&gt;()
    /// en todos los constructores de Views.
    /// </summary>
    public static class AppServices
    {
        /// <summary>Resuelve un servicio registrado en el contenedor DI de la aplicación.</summary>
        public static T Get<T>() where T : class
            => ((App)Application.Current).Host.Services.GetRequiredService<T>();
    }
}
