using AdvanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AdvanceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserInfoController : ControllerBase
    {
        private readonly IContactoUsuarioService _contactoUsuarioService;
        private readonly ILogger<UserInfoController> _logger;

        public UserInfoController(IContactoUsuarioService contactoUsuarioService, ILogger<UserInfoController> logger)
        {
            _contactoUsuarioService = contactoUsuarioService ?? throw new ArgumentNullException(nameof(contactoUsuarioService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene la información del usuario actual basado en el token JWT
        /// GET /api/UserInfo/infoUsuario
        /// </summary>
        /// <remarks>
        /// Este endpoint no requiere parámetros. El nombre de usuario se extrae automáticamente del token JWT.
        /// 
        /// El token JWT debe ser enviado en el header Authorization con el formato: Bearer {token}
        /// 
        /// Retorna:
        /// - credencial_id (int): ID de la credencial del usuario
        /// - nombreCompleto (string): Nombre completo del usuario (nombre + apellido)
        /// - correo (string): Correo electrónico del usuario
        /// - telefono (string): Teléfono del usuario
        /// - nivel (int): Nivel del usuario
        /// - tipoUsuario (string): Tipo de usuario
        /// 
        /// Ejemplo de respuesta:
        /// {
        ///   "credencialId": 1,
        ///   "nombreCompleto": "Braulio Emiliano Vazquez Valdez",
        ///   "correo": "baemvav@gmail.com",
        ///   "telefono": "5655139308",
        ///   "nivel": 6,
        ///   "tipoUsuario": "Devs"
        /// }
        /// </remarks>
        /// <returns>Información del usuario autenticado</returns>
        /// <response code="200">Retorna la información del usuario</response>
        /// <response code="401">No autorizado - Token inválido o no proporcionado</response>
        /// <response code="404">Usuario no encontrado en la base de datos</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("infoUsuario")]
        public async Task<IActionResult> GetInfoUsuario()
        {
            try
            {
                // Extraer el nombre de usuario del token JWT
                var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrWhiteSpace(username))
                {
                    _logger.LogWarning("No se pudo extraer el nombre de usuario del token JWT");
                    return Unauthorized(new { message = "Token inválido o no contiene información de usuario" });
                }

                _logger.LogDebug("Obteniendo información del usuario: {Username}", username);

                var userInfo = await _contactoUsuarioService.GetContactoUsuarioAsync(username);

                if (userInfo == null)
                {
                    _logger.LogWarning("No se encontró información para el usuario {Username}", username);
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                return Ok(userInfo);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error al obtener información del usuario");
#if DEBUG
                return StatusCode(500, new { message = ex.Message, innerMessage = ex.InnerException?.Message });
#else
                return StatusCode(500, new { message = "Error al acceder a la base de datos." });
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener información del usuario");
#if DEBUG
                return StatusCode(500, new { message = ex.Message });
#else
                return StatusCode(500, new { message = "Error interno del servidor." });
#endif
            }
        }
    }
}
