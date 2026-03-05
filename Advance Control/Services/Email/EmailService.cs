using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Advance_Control.Services.Security;

namespace Advance_Control.Services.Email;

/// <summary>
/// Implementación del servicio de correo usando Hostinger:
/// - SMTP: smtp.hostinger.com:587 (STARTTLS)
/// - IMAP: imap.hostinger.com:993 (SSL)
/// Las credenciales se leen de ISecureStorage (Windows PasswordVault).
/// </summary>
public class EmailService : IEmailService
{
    private const string SmtpHost = "smtp.hostinger.com";
    private const int SmtpPort = 587;

    private const string ImapHost = "imap.hostinger.com";
    private const int ImapPort = 993;

    private const string ClaveUsuario = "email_smtp_user";
    private const string ClavePassword = "email_smtp_password";

    private readonly ISecureStorage _storage;

    public EmailService(ISecureStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    // -------------------------------------------------------------------------
    // Credenciales
    // -------------------------------------------------------------------------

    private async Task<(string usuario, string password)> ObtenerCredencialesAsync()
    {
        var usuario = await _storage.GetAsync(ClaveUsuario);
        var password = await _storage.GetAsync(ClavePassword);

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("No hay credenciales de correo configuradas. Ingresa usuario y contraseña en la página de configuración.");

        return (usuario, password);
    }

    // -------------------------------------------------------------------------
    // Verificar conexión SMTP
    // -------------------------------------------------------------------------

    public async Task<string> VerifyConnectionAsync()
    {
        try
        {
            var (usuario, password) = await ObtenerCredencialesAsync();

            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(usuario, password);
            await client.DisconnectAsync(true);

            return "Conexión exitosa. Las credenciales son válidas.";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (MailKit.Security.AuthenticationException)
        {
            return "Error de autenticación: usuario o contraseña incorrectos.";
        }
        catch (Exception ex)
        {
            return $"No se pudo conectar al servidor: {ex.Message}";
        }
    }

    // -------------------------------------------------------------------------
    // Enviar correo
    // -------------------------------------------------------------------------

    public async Task SendEmailAsync(EmailMessage mensaje)
    {
        if (mensaje is null) throw new ArgumentNullException(nameof(mensaje));
        if (mensaje.Para.Count == 0) throw new ArgumentException("El correo debe tener al menos un destinatario.", nameof(mensaje));
        if (string.IsNullOrWhiteSpace(mensaje.Asunto)) throw new ArgumentException("El asunto no puede estar vacío.", nameof(mensaje));
        if (string.IsNullOrWhiteSpace(mensaje.CuerpoTexto) && string.IsNullOrWhiteSpace(mensaje.CuerpoHtml))
            throw new ArgumentException("El correo debe tener cuerpo de texto o HTML.", nameof(mensaje));

        var (usuario, password) = await ObtenerCredencialesAsync();

        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(usuario));

        foreach (var para in mensaje.Para)
            mime.To.Add(MailboxAddress.Parse(para));

        foreach (var cc in mensaje.CC)
            mime.Cc.Add(MailboxAddress.Parse(cc));

        foreach (var cco in mensaje.CCO)
            mime.Bcc.Add(MailboxAddress.Parse(cco));

        mime.Subject = mensaje.Asunto;

        // Construir cuerpo
        var builder = new BodyBuilder();
        if (!string.IsNullOrWhiteSpace(mensaje.CuerpoHtml))
            builder.HtmlBody = mensaje.CuerpoHtml;
        if (!string.IsNullOrWhiteSpace(mensaje.CuerpoTexto))
            builder.TextBody = mensaje.CuerpoTexto;

        // Adjuntos
        foreach (var (nombreArchivo, contenido) in mensaje.Adjuntos)
            builder.Attachments.Add(nombreArchivo, contenido);

        // Firma inline via CID (compatible con Gmail, Outlook, etc.)
        if (!string.IsNullOrWhiteSpace(mensaje.FirmaImagePath) && File.Exists(mensaje.FirmaImagePath))
        {
            var recurso = builder.LinkedResources.Add(mensaje.FirmaImagePath);
            recurso.ContentId = "email-firma";
            recurso.ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Inline);
        }

        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(usuario, password);
        await client.SendAsync(mime);
        await client.DisconnectAsync(true);
    }

    // -------------------------------------------------------------------------
    // Recibir correos (IMAP)
    // -------------------------------------------------------------------------

    public async Task<List<EmailMessage>> GetEmailsAsync(int cantidad = 50, bool soloNoLeidos = false)
    {
        var (usuario, password) = await ObtenerCredencialesAsync();
        var resultado = new List<EmailMessage>();

        using var client = new ImapClient();
        await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(usuario, password);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        // Obtener los índices de los mensajes más recientes
        var total = inbox.Count;
        var inicio = Math.Max(0, total - cantidad);

        for (int i = total - 1; i >= inicio; i--)
        {
            var summary = await inbox.FetchAsync(i, i, MessageSummaryItems.Flags | MessageSummaryItems.Envelope);
            if (summary.Count == 0) continue;

            var s = summary[0];

            // Filtrar no leídos si se solicita
            if (soloNoLeidos && s.Flags.HasValue && s.Flags.Value.HasFlag(MessageFlags.Seen))
                continue;

            var message = await inbox.GetMessageAsync(i);

            resultado.Add(new EmailMessage
            {
                MessageId = message.MessageId,
                De = message.From?.Mailboxes?.FirstOrDefault()?.Address,
                Para = message.To.Mailboxes.Select(m => m.Address).ToList(),
                CC = message.Cc.Mailboxes.Select(m => m.Address).ToList(),
                Asunto = message.Subject ?? string.Empty,
                CuerpoTexto = message.TextBody,
                CuerpoHtml = message.HtmlBody,
                Fecha = message.Date,
                Leido = s.Flags.HasValue && s.Flags.Value.HasFlag(MessageFlags.Seen)
            });
        }

        await client.DisconnectAsync(true);
        return resultado;
    }
}
