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
using Advance_Control.Services.CorreoUsuario;

namespace Advance_Control.Services.Email;

/// <summary>
/// Implementación del servicio de correo usando Hostinger:
/// - SMTP: smtp.hostinger.com:587 (STARTTLS)
/// - IMAP: imap.hostinger.com:993 (SSL)
/// Las credenciales se leen desde la configuración de correo del usuario en la API.
/// </summary>
public class EmailService : IEmailService
{
    private const string SmtpHost = "smtp.hostinger.com";
    private const int SmtpPort = 587;

    private const string ImapHost = "imap.hostinger.com";
    private const int ImapPort = 993;

    private readonly ICorreoUsuarioService _correoUsuarioService;

    public EmailService(ICorreoUsuarioService correoUsuarioService)
    {
        _correoUsuarioService = correoUsuarioService ?? throw new ArgumentNullException(nameof(correoUsuarioService));
    }

    // -------------------------------------------------------------------------
    // Credenciales
    // -------------------------------------------------------------------------

    private async Task<(string usuario, string password)> ObtenerCredencialesAsync()
    {
        var configuracion = await _correoUsuarioService.GetCorreoActualAsync();

        if (configuracion == null
            || string.IsNullOrWhiteSpace(configuracion.Email)
            || string.IsNullOrWhiteSpace(configuracion.Password))
        {
            throw new InvalidOperationException("No hay credenciales de correo configuradas para el usuario actual.");
        }

        return (configuracion.Email, configuracion.Password);
    }

    // -------------------------------------------------------------------------
    // Verificar conexión SMTP
    // -------------------------------------------------------------------------

    public async Task<string> VerifyConnectionAsync()
    {
        try
        {
            var (usuario, password) = await ObtenerCredencialesAsync();
            return await VerifyConnectionAsync(usuario, password);
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

    public async Task<string> VerifyConnectionAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return "El correo y la contraseña son obligatorios.";

            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(email, password);
            await client.DisconnectAsync(true);

            return "Conexión exitosa. Las credenciales son válidas.";
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

        // Guardar copia en carpetas IMAP (fire-and-forget con timeout para no bloquear)
        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var imap = new ImapClient();
                imap.Timeout = 10000; // 10 segundos máximo por operación

                await imap.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect, cts.Token);
                await imap.AuthenticateAsync(usuario, password, cts.Token);

                // Guardar en Enviados
                var sentFolder = imap.GetFolder(SpecialFolder.Sent);
                await sentFolder.AppendAsync(mime, MessageFlags.Seen, cts.Token);

                // Guardar copia en carpeta del cliente: Clientes/{CarpetaCliente}
                if (!string.IsNullOrWhiteSpace(mensaje.CarpetaCliente))
                {
                    var separador = imap.PersonalNamespaces[0].DirectorySeparator;
                    var charsInvalidos = Path.GetInvalidFileNameChars();
                    var safeNombre = mensaje.CarpetaCliente;
                    if (separador != '\0')
                        safeNombre = safeNombre.Replace(separador, '-');
                    safeNombre = string.Concat(safeNombre.Split(charsInvalidos));

                    var personal = imap.GetFolder(imap.PersonalNamespaces[0]);

                    // Buscar o crear carpeta "Clientes" (isMessageFolder: true para compatibilidad)
                    var subcarpetas = await personal.GetSubfoldersAsync(false, cts.Token);
                    var clientesFolder = subcarpetas.FirstOrDefault(f => f.Name == "Clientes");
                    clientesFolder ??= await personal.CreateAsync("Clientes", true, cts.Token);

                    // Buscar o crear subcarpeta del cliente
                    var subcarpetasCliente = await clientesFolder.GetSubfoldersAsync(false, cts.Token);
                    var clienteFolder = subcarpetasCliente.FirstOrDefault(f => f.Name == safeNombre);
                    clienteFolder ??= await clientesFolder.CreateAsync(safeNombre, true, cts.Token);

                    await clienteFolder.AppendAsync(mime, MessageFlags.Seen, cts.Token);
                }

                await imap.DisconnectAsync(true);
            }
            catch { /* No fallar — el correo ya fue enviado por SMTP */ }
        });
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
