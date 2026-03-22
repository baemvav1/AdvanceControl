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
/// Diálogo para enviar una cotización por correo.
/// Permite configurar Para, CC (contactos del cliente), CCO, Asunto y Mensaje.
/// </summary>
public sealed partial class EnviarCotizacionDialog : ContentDialog
{
    private readonly string _pdfPath;
    private readonly IEmailService _emailService;
    private readonly ICorreoUsuarioService _correoUsuarioService;
    private readonly List<CheckBox> _ccCheckboxes = [];

    /// <summary>
    /// Crea el diálogo de envío de cotización o reporte.
    /// </summary>
    /// <param name="pdfPath">Ruta al archivo PDF.</param>
    /// <param name="contactoPrincipal">Contacto destinatario principal (puede ser null).</param>
    /// <param name="todosContactos">Lista de contactos del cliente para elegir CC.</param>
    /// <param name="razonSocial">Razón social del cliente (para el asunto).</param>
    /// <param name="xamlRoot">XamlRoot del padre.</param>
    /// <param name="tipo">Tipo de documento: "Cotización" o "Reporte".</param>
    /// <param name="idOperacion">ID de la operación (para el cuerpo del correo).</param>
    public EnviarCotizacionDialog(
        string pdfPath,
        ContactoDto? contactoPrincipal,
        List<ContactoDto> todosContactos,
        string razonSocial,
        XamlRoot xamlRoot,
        string tipo = "Cotización",
        int? idOperacion = null)
    {
        _pdfPath = pdfPath ?? throw new ArgumentNullException(nameof(pdfPath));
        _emailService = AppServices.Get<IEmailService>();
        _correoUsuarioService = AppServices.Get<ICorreoUsuarioService>();

        this.InitializeComponent();
        this.XamlRoot = xamlRoot;
        this.Title = $"Enviar {tipo.ToLowerInvariant()} por correo";

        // Pre-llenar campos
        ParaTextBox.Text = contactoPrincipal?.Correo ?? string.Empty;
        AsuntoTextBox.Text = $"{tipo} - {razonSocial}";

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

        // Poblar checkboxes de CC (todos los contactos excepto el principal)
        foreach (var contacto in todosContactos)
        {
            if (string.IsNullOrWhiteSpace(contacto.Correo)) continue;
            if (contacto.Correo.Equals(contactoPrincipal?.Correo, StringComparison.OrdinalIgnoreCase)) continue;

            var nombreMostrado = $"{contacto.NombreCompleto} <{contacto.Correo}>";
            var cb = new CheckBox { Content = nombreMostrado, Tag = contacto.Correo };
            CCPanel.Children.Add(cb);
            _ccCheckboxes.Add(cb);
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
                                  .Replace("\n", "<br/>");
            var firmaCidHtml = !string.IsNullOrEmpty(firmaPath)
                ? FirmaCorreoHelper.GetFirmaCidHtml()
                : string.Empty;
            var cuerpoHtml = $"<html><body><p>{textoHtml}</p>{firmaCidHtml}</body></html>";

            var mensaje = new EmailMessage
            {
                Para = [paraEmail],
                CC = ccEmails,
                CCO = ccoEmails,
                Asunto = AsuntoTextBox.Text.Trim(),
                CuerpoTexto = textoPlano,
                CuerpoHtml = cuerpoHtml,
                FirmaImagePath = firmaPath,
                Adjuntos = [(Path.GetFileName(_pdfPath), pdfBytes)]
            };

            if (string.IsNullOrWhiteSpace(mensaje.Asunto))
            {
                MostrarError("El asunto no puede estar vacío.");
                return;
            }

            // Enviar
            await _emailService.SendEmailAsync(mensaje);

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
