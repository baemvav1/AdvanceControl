namespace AdvanceControl.Services
{
    // POCO para configurar parámetros de token en el cliente.
    // Debe coincidir con lo que configure el backend.
    public class TokenOptions
    {
        public string Issuer { get; set; } = "advance";
        public string Audience { get; set; } = "advance_client";
        // Clave simétrica para validar tokens localmente. En producción, valida con la misma clave que el backend usa.
        public string SigningKey { get; set; } = "change_this_to_strong_secret_at_runtime_which_is_long_enough";
        public int ExpiryMinutes { get; set; } = 60;
    }
}