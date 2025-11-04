namespace Advance_Control.Services.EndPointProvider
{
    public interface IApiEndpointProvider
    {
        /// <summary>
        /// Devuelve la Base URL de la API, por ejemplo "https://api.example.com" (sin slash final).
        /// </summary>
        string GetApiBaseUrl();

        /// <summary>
        /// Devuelve la URI absoluta (string) para la ruta relativa provista.
        /// Ejemplos de routeRelative: "Online", "auth/login", "customers/123".
        /// </summary>
        string GetEndpoint(string routeRelative);

        /// <summary>
        /// Variante que permite pasar partes y las concatena correctamente.
        /// </summary>
        string GetEndpoint(params string[] routeParts);
    }
}

