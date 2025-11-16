using System.Threading.Tasks;

namespace Advance_Control.Navigation
{
    /// <summary>
    /// Interfaz que las páginas pueden implementar para soportar la funcionalidad de recarga
    /// </summary>
    public interface IReloadable
    {
        /// <summary>
        /// Recarga los datos de la página
        /// </summary>
        Task ReloadAsync();
    }
}
