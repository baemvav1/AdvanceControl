namespace Advance_Control.Models
{
    /// <summary>Renglón del buscador de lista genérico (clientes, contactos…).</summary>
    public class BuscadorListaItem
    {
        public BuscadorListaItem(object dato, string titulo, string? subtitulo, string busqueda)
        {
            Dato = dato;
            Titulo = titulo;
            Subtitulo = subtitulo ?? string.Empty;
            Busqueda = Advance_Control.Utilities.TextoBusqueda.Normalizar(busqueda);
        }

        /// <summary>El objeto real (CustomerDto, ContactoDto…).</summary>
        public object Dato { get; }
        public string Titulo { get; }
        public string Subtitulo { get; }
        public bool TieneSubtitulo => !string.IsNullOrWhiteSpace(Subtitulo);

        /// <summary>Texto donde se busca, en minúsculas y sin acentos.</summary>
        public string Busqueda { get; }
    }
}
