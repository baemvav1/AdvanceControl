using System;
using System.Threading.Tasks;

namespace AdvanceControl.Services
{
    // Servicio que abstrae el proceso de login/logout para la UI.
    // En producción hará POST al backend para obtener token y lo almacenará (ej. SecureStorage).
    public class AuthenticationService
    {
        private readonly ITokenService _tokenService;

        public AuthenticationService(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        // Este método es solo un placeholder local para pruebas: en producción se debe reemplazar
        // por un llamado HTTP que devuelva token desde el servidor.
        public Task<string> LoginLocalAsync(Guid userId)
        {
            // Genera token localmente con JwtService. Backend debería ser el emisor.
            var token = _tokenService.GenerateToken(userId, new[] { "User" });
            // TODO: Guardar token en almacenamiento seguro
            return Task.FromResult(token);
        }

        // Validar token guardado
        public bool IsTokenValid(string token) => _tokenService.ValidateToken(token);

        // Obtener user id desde token
        public Guid? GetCurrentUserId(string token) => _tokenService.GetUserId(token);
    }
}