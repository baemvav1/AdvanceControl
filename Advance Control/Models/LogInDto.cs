using System.ComponentModel.DataAnnotations;

namespace Advance_Control.Models
{
    /// <summary>
    /// DTO (Data Transfer Object) para el inicio de sesión.
    /// Representa las credenciales del usuario para autenticación.
    /// </summary>
    public class LogInDto
    {
        /// <summary>
        /// Nombre de usuario para autenticación
        /// </summary>
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [MinLength(3, ErrorMessage = "El usuario debe tener al menos 3 caracteres")]
        [MaxLength(50, ErrorMessage = "El usuario no puede exceder 50 caracteres")]
        public string? User { get; set; }

        /// <summary>
        /// Contraseña del usuario
        /// </summary>
        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
        public string? Password { get; set; }

        /// <summary>
        /// Crea una instancia de LogInDto a partir de las propiedades del LoginViewModel
        /// </summary>
        /// <param name="user">Nombre de usuario</param>
        /// <param name="password">Contraseña</param>
        /// <returns>Nueva instancia de LogInDto</returns>
        public static LogInDto Create(string user, string password)
        {
            return new LogInDto
            {
                User = user,
                Password = password
            };
        }
    }
}
