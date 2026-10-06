using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Advance_Control.Models;
using Advance_Control.Services.CorreoUsuario;
using Advance_Control.Services.Email;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs;

/// <summary>
/// Datos para que el correo sirva de notificación de aprobación del Portal de Clientes:
/// el contacto dirigido queda fijo en "Para", los demás logins de la empresa van en CC
/// y el cuerpo lleva la liga y a quién va dirigida (solo esa persona aprueba).
/// </summary>
public sealed class NotificacionPortal
{
    /// <summary>URL del portal (p. ej. …/portalclientes/operaciones/123). Null si el dirigido no tiene login.</summary>
    public string? Liga { get; init; }
    public string DirigidoNombre { get; init; } = string.Empty;
    public bool DirigidoTieneLogin { get; init; }

    /// <summary>"la cotización" / "la hoja de mantenimiento".</summary>
    public string QueSeAprueba { get; init; } = "la cotización";

    /// <summary>Correos de los demás contactos con login del portal: se marcan en CC.</summary>
    public List<string> CorreosConLogin { get; init; } = [];

    /// <summary>Texto extra al inicio del mensaje (p. ej. "Corregimos la hoja que rechazaste").</summary>
    public string? Nota { get; init; }
}

/// <summary>
/// Diálogo para enviar una cotización por correo.
/// Permite configurar Para, CC (contactos del cliente), CCO, Asunto y Mensaje.
/// </summary>
public sealed partial class EnviarCotizacionDialog : ContentDialog
{
    private readonly string _pdfPath;
    private readonly string _razonSocial;
    private readonly IEmailService _emailService;
    private readonly ICorreoUsuarioService _correoUsuarioService;
    private readonly List<CheckBox> _ccCheckboxes = [];
    private readonly List<(string NombreArchivo, byte[] Contenido)> _adjuntosAdicionales;
    private readonly NotificacionPortal? _portal;

    /// <summary>Para + CC efectivamente enviados (para registrar la notificación).</summary>
    public string Destinatarios { get; private set; } = string.Empty;

    /// <summary>
    /// Crea el diálogo de envío de cotización, reporte, nota o finiquito.
    /// </summary>
    /// <param name="pdfPath">Ruta al archivo PDF principal (se adjunta siempre).</param>
    /// <param name="contactoPrincipal">Contacto destinatario principal (puede ser null).</param>
    /// <param name="todosContactos">Lista de contactos del cliente para elegir CC.</param>
    /// <param name="razonSocial">Razón social del cliente (para el asunto).</param>
    /// <param name="xamlRoot">XamlRoot del padre.</param>
    /// <param name="tipo">Tipo de documento: "Cotización", "Reporte", "Nota" o "Finiquito".</param>
    /// <param name="idOperacion">ID de la operación (para el cuerpo del correo).</param>
    /// <param name="adjuntosAdicionales">
    /// Adjuntos extra ya leídos en memoria (nombre + bytes), usados por "Finiquito" para
    /// sumar reporte, factura PDF y factura XML junto al <paramref name="pdfPath"/> principal.
    /// </param>
    public EnviarCotizacionDialog(
        string pdfPath,
        ContactoDto? contactoPrincipal,
        List<ContactoDto> todosContactos,
        string razonSocial,
        XamlRoot xamlRoot,
        string tipo = "Cotización",
        int? idOperacion = null,
        bool tFinalizado = false,
        List<(string NombreArchivo, byte[] Contenido)>? adjuntosAdicionales = null,
        string? asuntoPersonalizado = null,
        string? mensajePersonalizado = null,
        NotificacionPortal? portal = null)
    {
        _pdfPath = pdfPath ?? throw new ArgumentNullException(nameof(pdfPath));
        _portal = portal;
        _razonSocial = razonSocial;
        _adjuntosAdicionales = adjuntosAdicionales ?? [];
        _emailService = AppServices.Get<IEmailService>();
        _correoUsuarioService = AppServices.Get<ICorreoUsuarioService>();

        this.InitializeComponent();
        this.XamlRoot = xamlRoot;
        this.Title = $"Enviar {tipo.ToLowerInvariant()} por correo";

        // Pre-llenar campos
        ParaTextBox.Text = contactoPrincipal?.Correo ?? string.Empty;

        if (tipo == "Finiquito")
        {
            AsuntoTextBox.Text = $"Operacion Completada {idOperacion}";

            var partesFiniquito = new[] { contactoPrincipal?.Tratamiento, contactoPrincipal?.Nombre }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var destinatarioFiniquito = string.Join(" ", partesFiniquito);
            if (string.IsNullOrWhiteSpace(destinatarioFiniquito)) destinatarioFiniquito = "cliente";

            MensajeTextBox.Text =
                $"Apreciable {destinatarioFiniquito}, Se adjuntan los documentos relacionados con la operacion {idOperacion}.\n" +
                "Saludos!";
        }
        else
        {
            AsuntoTextBox.Text = tipo switch
            {
                "Nota" => $"Trabajos Realizados {idOperacion}",
                "Reporte" when tFinalizado => $"Reporte Trabajos Finalizados {idOperacion}",
                _ => $"{tipo} {idOperacion}"
            };

            // Construir saludo con tratamiento + nombre + apellido
            var partesSaludo = new[] { contactoPrincipal?.Tratamiento, contactoPrincipal?.Nombre, contactoPrincipal?.Apellido }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var nombreDestinatario = string.Join(" ", partesSaludo);
            if (string.IsNullOrWhiteSpace(nombreDestinatario)) nombreDestinatario = "cliente";

            var idOpTexto = idOperacion.HasValue ? $" #{idOperacion}" : string.Empty;
            MensajeTextBox.Text =
                $"Estimado: {nombreDestinatario}.\n\n" +
                $"En el siguiente correo, adjuntamos la {tipo.ToLowerInvariant()}{idOpTexto}.\n\n" +
                "Saludos Cordiales";
        }

        // Overrides opcionales (ej. "Factura {folio}") para tipos que no encajan en el switch de
        // arriba, pensado para operaciones (idOperacion) -- no cambia nada para los llamadores
        // existentes, que nunca los pasan.
        if (!string.IsNullOrWhiteSpace(asuntoPersonalizado))
        {
            AsuntoTextBox.Text = asuntoPersonalizado;
        }

        if (!string.IsNullOrWhiteSpace(mensajePersonalizado))
        {
            MensajeTextBox.Text = mensajePersonalizado;
        }

        // Poblar checkboxes de CC (todos los contactos excepto el principal)
        foreach (var contacto in todosContactos)
        {
            if (string.IsNullOrWhiteSpace(contacto.Correo)) continue;
            if (contacto.Correo.Equals(contactoPrincipal?.Correo, StringComparison.OrdinalIgnoreCase)) continue;

            var nombreMostrado = $"{contacto.NombreCompleto} <{contacto.Correo}>";
            var cb = new CheckBox
            {
                Content = nombreMostrado,
                Tag = contacto.Correo,
                IsChecked = portal?.CorreosConLogin.Contains(contacto.Correo, StringComparer.OrdinalIgnoreCase) == true
            };
            CCPanel.Children.Add(cb);
            _ccCheckboxes.Add(cb);
        }

        if (portal != null)
        {
            // El dirigido siempre recibe la notificación: no se puede cambiar el "Para".
            ParaTextBox.IsReadOnly = true;
            ToolTipService.SetToolTip(ParaTextBox, $"Contacto dirigido: {portal.DirigidoNombre}. Solo esta persona aprueba o rechaza.");
            MensajeTextBox.Text = MensajePortal(contactoPrincipal, portal, tipo, idOperacion);
        }

        // Suscribir el botón Enviar con validación
        this.PrimaryButtonClick += OnEnviarClick;
    }

    private async void OnEnviarClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Diferir el cierre para procesar el envío
        var deferral = args.GetDeferral();
        args.Cancel = true; // Prevenir cierre automático

        try
        {
            EstadoInfoBar.IsOpen = false;
            IsPrimaryButtonEnabled = false;

            // Validar Para
            var paraEmail = ParaTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(paraEmail))
            {
                MostrarError("El campo \"Para\" es obligatorio.");
                return;
            }

            // Construir listas CC
            var ccEmails = _ccCheckboxes
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag?.ToString() ?? string.Empty)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            // CC manual
            var ccManual = ParseEmails(CCManualTextBox.Text);
            ccEmails.AddRange(ccManual);

            // CCO
            var ccoEmails = ParseEmails(CCOTextBox.Text);

            // Leer PDF
            byte[] pdfBytes;
            try
            {
                pdfBytes = await Task.Run(() => File.ReadAllBytes(_pdfPath));
            }
            catch (Exception ex)
            {
                MostrarError($"No se pudo leer el archivo PDF: {ex.Message}");
                return;
            }

            // Construir mensaje
            var textoPlano = MensajeTextBox.Text;

            // Obtener firma si existe (se adjunta vía CID para máxima compatibilidad)
            var configuracionCorreo = await _correoUsuarioService.GetCorreoActualAsync();
            var emailUsuario = configuracionCorreo?.Email;
            var firmaPath = string.Empty;
            if (!string.IsNullOrWhiteSpace(emailUsuario))
                firmaPath = FirmaCorreoHelper.GetFirmaPath(emailUsuario);

            // Construir cuerpo HTML con texto del mensaje + referencia CID de firma
            var textoHtml = System.Net.WebUtility.HtmlEncode(textoPlano)
                                  .Replace("\r\n", "<br/>")
                                  .Replace("\n", "<br/>")
                                  .Replace("\r", "<br/>");
            var firmaCidHtml = !string.IsNullOrEmpty(firmaPath)
                ? FirmaCorreoHelper.GetFirmaCidHtml()
                : string.Empty;
            var ligaHtml = string.IsNullOrWhiteSpace(_portal?.Liga)
                ? string.Empty
                : $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(_portal!.Liga)}\" style=\"display:inline-block;padding:10px 18px;background:#0067C0;color:#ffffff;text-decoration:none;border-radius:4px;\">Ver en el Portal de Clientes</a></p>";
            var cuerpoHtml = $"<html><body><p>{textoHtml}</p>{ligaHtml}{firmaCidHtml}</body></html>";

            var adjuntos = new List<(string NombreArchivo, byte[] Contenido)>
            {
                (Path.GetFileName(_pdfPath), pdfBytes)
            };
            adjuntos.AddRange(_adjuntosAdicionales);

            var mensaje = new EmailMessage
            {
                Para = [paraEmail],
                CC = ccEmails,
                CCO = ccoEmails,
                Asunto = AsuntoTextBox.Text.Trim(),
                CuerpoTexto = textoPlano,
                CuerpoHtml = cuerpoHtml,
                FirmaImagePath = firmaPath,
                Adjuntos = adjuntos,
                CarpetaCliente = string.IsNullOrWhiteSpace(_razonSocial) ? null : _razonSocial
            };

            if (string.IsNullOrWhiteSpace(mensaje.Asunto))
            {
                MostrarError("El asunto no puede estar vacío.");
                return;
            }

            // Enviar
            await _emailService.SendEmailAsync(mensaje);
            Destinatarios = string.Join(", ", new[] { paraEmail }.Concat(ccEmails));

            // Éxito — permitir cierre
            args.Cancel = false;
        }
        catch (Exception ex)
        {
            MostrarError($"Error al enviar: {ex.Message}");
        }
        finally
        {
            IsPrimaryButtonEnabled = true;
            deferral.Complete();
        }
    }

    /// <summary>Mensaje de la notificación: qué se envía, a quién va dirigida y cómo aprobar.</summary>
    private static string MensajePortal(ContactoDto? contacto, NotificacionPortal portal, string tipo, int? idOperacion)
    {
        var nombre = string.Join(" ", new[] { contacto?.Tratamiento, contacto?.Nombre, contacto?.Apellido }.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (string.IsNullOrWhiteSpace(nombre)) nombre = portal.DirigidoNombre;
        var idOpTexto = idOperacion.HasValue ? $" de la operación #{idOperacion}" : string.Empty;

        var texto = $"Estimado: {nombre}.\n\n";
        if (!string.IsNullOrWhiteSpace(portal.Nota))
            texto += portal.Nota.Trim() + "\n\n";
        texto += $"Adjuntamos {portal.QueSeAprueba}{idOpTexto} para su aprobación.\n\n";
        texto += $"Va dirigida a {portal.DirigidoNombre}; solo esa persona puede aprobarla o rechazarla. Los demás destinatarios la reciben como aviso.\n\n";
        texto += portal.DirigidoTieneLogin && !string.IsNullOrWhiteSpace(portal.Liga)
            ? $"Puede aprobarla o rechazarla en el Portal de Clientes con el botón de abajo, o firmarla y respondernos este correo con la foto o el PDF firmado.\n{portal.Liga}\n\n"
            : "Para aprobarla, fírmela y respóndanos este correo con la foto o el PDF firmado.\n\n";
        return texto + "Saludos Cordiales";
    }

    private void MostrarError(string mensaje)
    {
        EstadoInfoBar.Severity = InfoBarSeverity.Error;
        EstadoInfoBar.Message = mensaje;
        EstadoInfoBar.IsOpen = true;
    }

    private static List<string> ParseEmails(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return [];
        return input.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
    }
}
