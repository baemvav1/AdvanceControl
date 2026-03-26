using Microsoft.UI.Xaml;

namespace Advance_Control.Services.Theme
{
    /// <summary>
    /// Servicio para gestionar el tema visual de la aplicación (Claro, Oscuro, Sistema).
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Tema actual aplicado.
        /// </summary>
        ElementTheme CurrentTheme { get; }

        /// <summary>
        /// Inicializa el servicio: lee la preferencia guardada y la aplica al elemento raíz.
        /// Debe llamarse una vez al cargar la ventana principal.
        /// </summary>
        /// <param name="rootElement">El FrameworkElement raíz (ej. RootGrid).</param>
        void Initialize(FrameworkElement rootElement);

        /// <summary>
        /// Cambia el tema y persiste la preferencia.
        /// </summary>
        /// <param name="theme">ElementTheme.Default (Sistema), Light (Claro), Dark (Oscuro).</param>
        void SetTheme(ElementTheme theme);
    }
}
