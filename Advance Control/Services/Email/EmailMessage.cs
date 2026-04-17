using System;
using System.Collections.Generic;

namespace Advance_Control.Services.Email;

/// <summary>
/// Representa un mensaje de correo electrónico para envío o recepción.
/// </summary>
public class EmailMessage
{
    /// <summary>Dirección(es) de destino. Requerido para envío.</summary>
    public List<string> Para { get; set; } = [];

    /// <summary>Dirección(es) en copia. Opcional.</summary>
    public List<string> CC { get; set; } = [];

    /// <summary>Dirección(es) en copia oculta (BCC). Opcional.</summary>
    public List<string> CCO { get; set; } = [];

    /// <summary>Asunto del correo.</summary>
    public string Asunto { get; set; } = string.Empty;

    /// <summary>Cuerpo del correo en texto plano.</summary>
    public string? CuerpoTexto { get; set; }

    /// <summary>Cuerpo del correo en HTML. Si se especifica, tiene prioridad sobre CuerpoTexto.</summary>
    public string? CuerpoHtml { get; set; }

    /// <summary>
    /// Adjuntos: par (nombreArchivo, contenido en bytes).
    /// Ej: ("cotizacion.pdf", pdfBytes)
    /// </summary>
    public List<(string NombreArchivo, byte[] Contenido)> Adjuntos { get; set; } = [];

    /// <summary>
    /// Ruta local de la imagen de firma para incrustar inline en el HTML vía CID.
    /// Si se especifica, se adjunta como recurso vinculado con Content-ID "email-firma".
    /// </summary>
    public string? FirmaImagePath { get; set; }

    /// <summary>Dirección del remitente. Se rellena automáticamente al recibir correos.</summary>
    public string? De { get; set; }

    /// <summary>Fecha del mensaje. Se rellena al recibir correos.</summary>
    public DateTimeOffset? Fecha { get; set; }

    /// <summary>Indica si el mensaje fue leído (al recibir).</summary>
    public bool Leido { get; set; }

    /// <summary>Identificador único del mensaje en el servidor IMAP.</summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Nombre de la carpeta del cliente donde guardar una copia vía IMAP (ej: "ACME Corp").
    /// Se creará en Clientes/{CarpetaCliente}. Null para no crear carpeta de cliente.
    /// </summary>
    public string? CarpetaCliente { get; set; }
}
