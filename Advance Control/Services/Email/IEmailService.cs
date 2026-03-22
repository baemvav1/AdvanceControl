using System.Collections.Generic;
using System.Threading.Tasks;

namespace Advance_Control.Services.Email;

/// <summary>
/// Servicio de correo electrónico: verificación de conexión, envío (SMTP) y recepción (IMAP).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Verifica que las credenciales SMTP sean válidas estableciendo y cerrando una conexión.
    /// </summary>
    /// <returns>Mensaje descriptivo del resultado.</returns>
    Task<string> VerifyConnectionAsync();

    /// <summary>
    /// Verifica explícitamente un correo y contraseña sin depender de la configuración persistida.
    /// </summary>
    Task<string> VerifyConnectionAsync(string email, string password);

    /// <summary>
    /// Envía un correo electrónico usando las credenciales configuradas.
    /// </summary>
    /// <param name="mensaje">Mensaje a enviar (Para, Asunto y al menos un cuerpo son obligatorios).</param>
    Task SendEmailAsync(EmailMessage mensaje);

    /// <summary>
    /// Recupera los últimos mensajes de la bandeja de entrada vía IMAP.
    /// </summary>
    /// <param name="cantidad">Número máximo de mensajes a recuperar (más recientes primero).</param>
    /// <param name="soloNoLeidos">Si es true, filtra únicamente mensajes no leídos.</param>
    Task<List<EmailMessage>> GetEmailsAsync(int cantidad = 50, bool soloNoLeidos = false);
}
