using AdvanceApi.Models;

namespace AdvanceApi.Services
{
    /// <summary>
    /// Implementación del servicio de contacto de usuario
    /// NOTA: Esta es una implementación temporal con datos mock.
    /// En producción, esto debe conectarse a una base de datos real.
    /// </summary>
    public class ContactoUsuarioService : IContactoUsuarioService
    {
        private readonly ILogger<ContactoUsuarioService> _logger;

        // Mock data - En producción esto vendría de la base de datos
        private readonly Dictionary<string, UserInfoDto> _mockUsers = new()
        {
            {
                "baemvav", new UserInfoDto
                {
                    CredencialId = 1,
                    NombreCompleto = "Braulio Emiliano Vazquez Valdez",
                    Correo = "baemvav@gmail.com",
                    Telefono = "5655139308",
                    Nivel = 6,
                    TipoUsuario = "Devs"
                }
            },
            {
                "admin", new UserInfoDto
                {
                    CredencialId = 2,
                    NombreCompleto = "Usuario Admin",
                    Correo = "admin@example.com",
                    Telefono = "1234567890",
                    Nivel = 10,
                    TipoUsuario = "Admin"
                }
            },
            {
                "usuario_ejemplo", new UserInfoDto
                {
                    CredencialId = 3,
                    NombreCompleto = "Usuario Ejemplo",
                    Correo = "usuario@example.com",
                    Telefono = "9876543210",
                    Nivel = 3,
                    TipoUsuario = "Usuario"
                }
            }
        };

        public ContactoUsuarioService(ILogger<ContactoUsuarioService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserInfoDto?> GetContactoUsuarioAsync(string username)
        {
            try
            {
                _logger.LogInformation("Buscando información del usuario: {Username}", username);

                // En producción, esto sería una consulta a la base de datos
                // Por ahora, usamos datos mock para probar el endpoint
                await Task.Delay(10); // Simula una llamada asíncrona

                if (_mockUsers.TryGetValue(username, out var userInfo))
                {
                    _logger.LogInformation("Usuario encontrado: {Username}", username);
                    return userInfo;
                }

                _logger.LogWarning("Usuario no encontrado: {Username}", username);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información del usuario {Username}", username);
                throw new InvalidOperationException($"Error al obtener información del usuario: {username}", ex);
            }
        }
    }
}
