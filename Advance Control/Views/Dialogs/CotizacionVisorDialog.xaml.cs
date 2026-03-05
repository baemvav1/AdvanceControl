using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs;

/// <summary>
/// Diálogo para visualizar una cotización PDF con la opción de enviarla por correo.
/// </summary>
public sealed partial class CotizacionVisorDialog : ContentDialog
{
    private readonly string _pdfPath;
    private readonly ContactoDto? _contactoPrincipal;
    private readonly List<ContactoDto> _todosContactos;
    private readonly string _razonSocial;
    private bool _pdfCargado;

    /// <summary>
    /// Crea el visor de PDF para cotización o reporte.
    /// </summary>
    /// <param name="pdfPath">Ruta al PDF generado.</param>
    /// <param name="contactoPrincipal">Contacto destinatario del correo.</param>
    /// <param name="todosContactos">Todos los contactos del cliente para seleccionar CC.</param>
    /// <param name="razonSocial">Razón social del cliente.</param>
    /// <param name="xamlRoot">XamlRoot del padre.</param>
    /// <param name="tipo">Tipo de documento: "Cotización" o "Reporte" (afecta título del diálogo).</param>
    public CotizacionVisorDialog(
        string pdfPath,
        ContactoDto? contactoPrincipal,
        List<ContactoDto> todosContactos,
        string razonSocial,
        XamlRoot xamlRoot,
        string tipo = "Cotización")
    {
        _pdfPath = pdfPath ?? throw new ArgumentNullException(nameof(pdfPath));
        _contactoPrincipal = contactoPrincipal;
        _todosContactos = todosContactos ?? [];
        _razonSocial = razonSocial;

        this.InitializeComponent();
        this.XamlRoot = xamlRoot;

        // Ajustar título según tipo de documento
        this.Title = $"Visor de {tipo.ToLowerInvariant()}";

        RutaTextBlock.Text = pdfPath;

        // Inicializar WebView2 cuando cargue el panel
        PdfWebView.Loaded += async (s, e) => await CargarPdfAsync();

        // Habilitar/deshabilitar "Enviar por correo" según disponibilidad de correo
        IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(_contactoPrincipal?.Correo);
    }

    private async Task CargarPdfAsync()
    {
        try
        {
            await PdfWebView.EnsureCoreWebView2Async();
            PdfWebView.Source = new Uri(_pdfPath);
            _pdfCargado = true;
        }
        catch
        {
            // Si WebView2 no está disponible, solo mostramos la ruta
            _pdfCargado = false;
        }
        finally
        {
            CargandoRing.IsActive = false;
            CargandoRing.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Resultado extra: indica si el usuario quiso enviar por correo (Primary) o abrir externamente (Secondary).
    /// </summary>
    public CotizacionVisorResultado Resultado { get; private set; } = CotizacionVisorResultado.Cerrar;

    internal void NotificarResultado(ContentDialogResult result)
    {
        Resultado = result switch
        {
            ContentDialogResult.Primary => CotizacionVisorResultado.EnviarCorreo,
            ContentDialogResult.Secondary => CotizacionVisorResultado.AbrirExterno,
            _ => CotizacionVisorResultado.Cerrar
        };
    }
}

/// <summary>Posibles acciones tras cerrar el visor de cotización.</summary>
public enum CotizacionVisorResultado
{
    Cerrar,
    EnviarCorreo,
    AbrirExterno
}
