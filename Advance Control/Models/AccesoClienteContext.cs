namespace Advance_Control.Models
{
    /// <summary>
    /// Filtro de estatus que viaja desde el dashboard de Acceso Cliente
    /// hacia la página de Operaciones para listar el subconjunto correspondiente.
    /// </summary>
    public enum AccesoClienteFiltro
    {
        Todas = 0,
        Facturadas = 1,
        Finalizadas = 2,
        SinFinalizar = 3,
    }

    /// <summary>
    /// Contexto de navegación que pasa <see cref="Views.Pages.AccesoClientePage"/>
    /// a <see cref="Views.Pages.OperacionesPage"/> para filtrar las operaciones de un cliente
    /// específico en modo solo-lectura (bypass de permisos).
    /// </summary>
    public class AccesoClienteContext
    {
        public int IdCliente { get; set; }

        public string NombreCliente { get; set; } = string.Empty;

        public AccesoClienteFiltro Filtro { get; set; } = AccesoClienteFiltro.Todas;

        /// <summary>
        /// Cuando es true, las operaciones se presentan en modo lectura
        /// y cualquier acción mutadora queda bloqueada (bypass sin permisos).
        /// </summary>
        public bool BypassAcceso { get; set; } = true;
    }
}
