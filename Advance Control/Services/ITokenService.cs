using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace AdvanceControl.Services
{
    // Abstracción para manejo de JWT en el cliente.
    // El backend emitirá tokens; el cliente puede validar, parsear y mantenerlos.
    public interface ITokenService
    {
        // Generar un token (uso local/test). En producción el backend emitirá el token.
        string GenerateToken(Guid userId, IEnumerable<string>? roles = null);

        // Validar la estructura y firma del token, retorna false si inválido.
        bool ValidateToken(string token);

        // Extraer claims (user id, roles, exp, etc.). Retorna null si inválido.
        ClaimsPrincipal? GetPrincipalFromToken(string token);

        // Extraer user id del token (si existe), null si no está presente o inválido.
        Guid? GetUserId(string token);

        // Extraer roles (si hay).
        IEnumerable<string> GetRoles(string token);
    }
}