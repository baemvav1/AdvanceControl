using System;
using System.IO;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;

namespace Advance_Control.Views.Pages
{
    public sealed partial class ComplementosPagoAuditoriaPage : Page
    {
        public ComplementosPagoAuditoriaViewModel ViewModel { get; }

        public ComplementosPagoAuditoriaPage()
        {
            InitializeComponent();
            ViewModel = AppServices.Get<ComplementosPagoAuditoriaViewModel>();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.CargarAsync();
        }

        private async void BtnDescargarPdf_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not ComplementoPagoResumenDto renglon)
            {
                return;
            }

            var rutaPdf = await ViewModel.GenerarPdfAsync(renglon.IdFacturaComplemento);
            if (rutaPdf == null)
            {
                return;
            }

            var picker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.SuggestedFileName = $"ComplementoPago_{renglon.ComplementoFolioTitulo}".Replace(" ", "_");
            picker.FileTypeChoices.Add("Documento PDF", new System.Collections.Generic.List<string> { ".pdf" });

            var archivo = await picker.PickSaveFileAsync();
            if (archivo == null)
            {
                return;
            }

            File.Copy(rutaPdf, archivo.Path, overwrite: true);
        }
    }
}
