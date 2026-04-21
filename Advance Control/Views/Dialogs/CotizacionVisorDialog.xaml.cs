using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

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
    private bool _cargandoPdf;

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

        // Inicializar WebView2 cuando el diálogo ya esté abierto y anexado al árbol visual.
        this.Opened += async (_, _) => await CargarPdfAsync();

        // Habilitar/deshabilitar "Enviar por correo" según disponibilidad de correo
        IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(_contactoPrincipal?.Correo);
    }

    private async Task CargarPdfAsync()
    {
        if (_pdfCargado || _cargandoPdf)
            return;

        _cargandoPdf = true;
        try
        {
            EstadoTextBlock.Visibility = Visibility.Collapsed;
            PdfPagesPanel.Children.Clear();

            var pdfFile = await ObtenerArchivoPdfAsync(_pdfPath);
            var pdfDocument = await PdfDocument.LoadFromFileAsync(pdfFile);

            if (pdfDocument.PageCount == 0)
            {
                EstadoTextBlock.Text = "El PDF no contiene páginas para mostrar.";
                EstadoTextBlock.Visibility = Visibility.Visible;
                _pdfCargado = false;
                return;
            }

            for (uint i = 0; i < pdfDocument.PageCount; i++)
            {
                using var page = pdfDocument.GetPage(i);
                var pageView = await RenderizarPaginaAsync(page, i + 1, pdfDocument.PageCount);
                PdfPagesPanel.Children.Add(pageView);
            }

            _pdfCargado = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CotizacionVisorDialog::CargarPdfAsync: {ex.GetType().Name} - {ex.Message}");
            EstadoTextBlock.Text = "No se pudo cargar la vista previa del PDF.";
            EstadoTextBlock.Visibility = Visibility.Visible;
            _pdfCargado = false;
        }
        finally
        {
            _cargandoPdf = false;
            CargandoRing.IsActive = false;
            CargandoRing.Visibility = Visibility.Collapsed;
        }
    }

    private static async Task<StorageFile> ObtenerArchivoPdfAsync(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
            throw new ArgumentException("La ruta del PDF no puede estar vacía.", nameof(pdfPath));

        if (Uri.TryCreate(pdfPath, UriKind.Absolute, out var uriExistente))
        {
            if (uriExistente.IsFile)
                return await StorageFile.GetFileFromPathAsync(uriExistente.LocalPath);
        }

        var fullPath = Path.GetFullPath(pdfPath);
        return await StorageFile.GetFileFromPathAsync(fullPath);
    }

    private static async Task<FrameworkElement> RenderizarPaginaAsync(PdfPage page, uint numeroPagina, uint totalPaginas)
    {
        using var renderStream = new InMemoryRandomAccessStream();
        var renderOptions = new PdfPageRenderOptions
        {
            DestinationWidth = 1400
        };

        await page.RenderToStreamAsync(renderStream, renderOptions);
        renderStream.Seek(0);

        var imageSource = new BitmapImage();
        await imageSource.SetSourceAsync(renderStream);

        var pageTitle = new TextBlock
        {
            Text = $"Página {numeroPagina} de {totalPaginas}",
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };

        var pageImage = new Image
        {
            Source = imageSource,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var pageBorder = new Border
        {
            Padding = new Thickness(8),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = pageImage
        };

        var pagePanel = new StackPanel
        {
            Spacing = 6
        };
        pagePanel.Children.Add(pageTitle);
        pagePanel.Children.Add(pageBorder);

        return pagePanel;
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
