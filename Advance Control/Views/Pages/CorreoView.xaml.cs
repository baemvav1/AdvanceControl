using Advance_Control.Services.Email;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Pages;

public sealed partial class CorreoView : Page
{
    public CorreoViewModel ViewModel { get; }

    public CorreoView()
    {
        ViewModel = AppServices.Get<CorreoViewModel>();
        this.InitializeComponent();
        this.DataContext = ViewModel;

        // Observar cambios en FirmaPath para actualizar la vista previa
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CorreoViewModel.FirmaPath))
                ActualizarVistaPreviaFirma(ViewModel.FirmaPath);
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
        // Restaurar la contraseña guardada en el PasswordBox
        PasswordBoxControl.Password = ViewModel.Password;
        // Mostrar firma si existe
        ActualizarVistaPreviaFirma(ViewModel.FirmaPath);
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.Password = PasswordBoxControl.Password;
    }

    private async void CargarFirma_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Email))
        {
            var dlg = new ContentDialog
            {
                Title = "Correo requerido",
                Content = "Primero guarda el correo electrónico antes de cargar una firma.",
                CloseButtonText = "Aceptar",
                XamlRoot = this.XamlRoot
            };
            await dlg.ShowAsync();
            return;
        }

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".gif");

        // Inicializar el picker con el hwnd de la ventana
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        try
        {
            var destino = await FirmaCorreoHelper.GuardarFirmaAsync(ViewModel.Email, file.Path);
            ViewModel.ActualizarFirma(destino);
            ViewModel.Estado = "Firma de correo guardada correctamente.";
        }
        catch (Exception ex)
        {
            var dlg = new ContentDialog
            {
                Title = "Error al guardar firma",
                Content = ex.Message,
                CloseButtonText = "Aceptar",
                XamlRoot = this.XamlRoot
            };
            await dlg.ShowAsync();
        }
    }

    private void ActualizarVistaPreviaFirma(string firmaPath)
    {
        if (!string.IsNullOrEmpty(firmaPath))
        {
            FirmaPreviewImage.Source = new BitmapImage(new Uri(firmaPath));
            FirmaPreviewImage.Visibility = Visibility.Visible;
            SinFirmaText.Visibility = Visibility.Collapsed;
        }
        else
        {
            FirmaPreviewImage.Source = null;
            FirmaPreviewImage.Visibility = Visibility.Collapsed;
            SinFirmaText.Visibility = Visibility.Visible;
        }
    }
}
