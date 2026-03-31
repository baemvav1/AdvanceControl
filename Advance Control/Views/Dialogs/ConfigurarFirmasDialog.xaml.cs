using Advance_Control.Services.Quotes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using global::Windows.Storage;
using global::Windows.Storage.Pickers;
using WinRT.Interop;

namespace Advance_Control.Views.Dialogs;

/// <summary>
/// Dialog que permite al usuario cargar las imágenes de firma antes de generar un PDF.
/// Se abre automáticamente cuando alguna firma está ausente.
/// </summary>
public sealed partial class ConfigurarFirmasDialog : ContentDialog
{
    private static readonly IReadOnlyList<string> _imagenesPermitidas = [".png", ".jpg", ".jpeg"];

    private readonly IFirmaService _firmaService;
    private readonly int? _idAtiende;
    private readonly string _nombreAtiende;
    private readonly IntPtr _hwnd;

    private StorageFile? _firmaDireccionPendiente;
    private StorageFile? _firmaOperadorPendiente;

    public ConfigurarFirmasDialog(
        int? idAtiende,
        string nombreAtiende,
        IFirmaService firmaService,
        XamlRoot xamlRoot,
        IntPtr hwnd)
    {
        InitializeComponent();
        XamlRoot = xamlRoot;

        _firmaService = firmaService ?? throw new ArgumentNullException(nameof(firmaService));
        _idAtiende    = idAtiende;
        _nombreAtiende = string.IsNullOrWhiteSpace(nombreAtiende) ? "Operador" : nombreAtiende.Trim();
        _hwnd          = hwnd;

        InicializarEstados();
        PrimaryButtonClick += OnContinuar_Click;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Inicialización
    // ─────────────────────────────────────────────────────────────────────

    private void InicializarEstados()
    {
        // Firma de dirección
        ActualizarEstadoDireccion(_firmaService.ExisteFirmaDireccion());

        // Firma del operador
        if (_idAtiende.HasValue)
        {
            TituloFirmaOperadorTextBlock.Text = $"Firma de {_nombreAtiende}";
            ActualizarEstadoOperador(_firmaService.ExisteFirmaOperador(_idAtiende.Value));
        }
        else
        {
            FirmaOperadorSection.Visibility = Visibility.Collapsed;
        }
    }

    private void ActualizarEstadoDireccion(bool existe)
    {
        EstadoFirmaDireccionTextBlock.Text = existe ? "✅ Firma cargada" : "⚠️ Sin firma — se omitirá en el PDF";
        PreviewFirmaDireccionImage.Visibility = Visibility.Collapsed;

        if (existe)
            _ = CargarPreviewAsync(PreviewFirmaDireccionImage, _firmaService.GetFirmaDireccionPath());
    }

    private void ActualizarEstadoOperador(bool existe)
    {
        if (!_idAtiende.HasValue) return;
        EstadoFirmaOperadorTextBlock.Text = existe ? "✅ Firma cargada" : "⚠️ Sin firma — se omitirá en el PDF";
        PreviewFirmaOperadorImage.Visibility = Visibility.Collapsed;

        if (existe)
        {
            var path = _firmaService.GetFirmaOperadorPath(_idAtiende.Value);
            if (path != null)
                _ = CargarPreviewAsync(PreviewFirmaOperadorImage, path);
        }
    }

    private static async Task CargarPreviewAsync(Image imageControl, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenReadAsync();
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            imageControl.Source = bitmap;
            imageControl.Visibility = Visibility.Visible;
        }
        catch { /* No bloquear si la imagen no carga */ }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Carga de archivos
    // ─────────────────────────────────────────────────────────────────────

    private async void CargarFirmaDireccion_Click(object sender, RoutedEventArgs e)
    {
        var archivo = await AbrirPickerImagenAsync();
        if (archivo == null) return;

        _firmaDireccionPendiente = archivo;
        EstadoFirmaDireccionTextBlock.Text = $"✅ Seleccionada: {archivo.Name}";
        _ = CargarPreviewDesdeStorageAsync(PreviewFirmaDireccionImage, archivo);
        OcultarError();
    }

    private async void CargarFirmaOperador_Click(object sender, RoutedEventArgs e)
    {
        var archivo = await AbrirPickerImagenAsync();
        if (archivo == null) return;

        _firmaOperadorPendiente = archivo;
        EstadoFirmaOperadorTextBlock.Text = $"✅ Seleccionada: {archivo.Name}";
        _ = CargarPreviewDesdeStorageAsync(PreviewFirmaOperadorImage, archivo);
        OcultarError();
    }

    private async Task<StorageFile?> AbrirPickerImagenAsync()
    {
        try
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            InitializeWithWindow.Initialize(picker, _hwnd);
            return await picker.PickSingleFileAsync();
        }
        catch
        {
            return null;
        }
    }

    private static async Task CargarPreviewDesdeStorageAsync(Image imageControl, StorageFile archivo)
    {
        try
        {
            using var stream = await archivo.OpenReadAsync();
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            imageControl.Source = bitmap;
            imageControl.Visibility = Visibility.Visible;
        }
        catch { /* No bloquear si la imagen no carga */ }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Confirmar
    // ─────────────────────────────────────────────────────────────────────

    private async void OnContinuar_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Diferir cierre hasta guardar los archivos
        var deferral = args.GetDeferral();
        OcultarError();

        try
        {
            if (_firmaDireccionPendiente != null)
                await _firmaService.GuardarFirmaDireccionAsync(_firmaDireccionPendiente);

            if (_firmaOperadorPendiente != null && _idAtiende.HasValue)
                await _firmaService.GuardarFirmaOperadorAsync(_idAtiende.Value, _nombreAtiende, _firmaOperadorPendiente);
        }
        catch (Exception ex)
        {
            MostrarError($"Error al guardar la firma: {ex.Message}");
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void MostrarError(string mensaje)
    {
        ErrorInfoBar.Message = mensaje;
        ErrorInfoBar.IsOpen = true;
    }

    private void OcultarError()
    {
        ErrorInfoBar.IsOpen = false;
    }
}
