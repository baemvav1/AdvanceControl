using Advance_Control.Models;
using Advance_Control.Services.Portal;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Linq;

namespace Advance_Control.Views.Portal
{
    /// <summary>Detalle de una operación del Portal de Cliente y sus hojas de mantenimiento.</summary>
    public sealed partial class ClienteOperacionDetallePage : Page
    {
        private readonly IPortalClienteService _portalClienteService;
        private int _idOperacion;

        public ObservableCollection<MantenimientoPreventivoHojaDto> Hojas { get; } = new();

        public ClienteOperacionDetallePage()
        {
            _portalClienteService = AppServices.Get<IPortalClienteService>();

            InitializeComponent();
            HojasListView.ItemsSource = Hojas;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not int idOperacion)
                return;

            _idOperacion = idOperacion;
            await CargarAsync();
        }

        private async System.Threading.Tasks.Task CargarAsync()
        {
            var operacion = (await _portalClienteService.ObtenerOperacionesAsync())
                .FirstOrDefault(o => o.IdOperacion == _idOperacion);

            IdentificadorTextBlock.Text = operacion?.Identificador ?? $"Operación #{_idOperacion}";
            NotaTextBlock.Text = operacion?.Nota ?? string.Empty;
            NotaTextBlock.Visibility = string.IsNullOrWhiteSpace(operacion?.Nota) ? Visibility.Collapsed : Visibility.Visible;
            FechaTextBlock.Text = operacion?.FechaInicio.HasValue == true
                ? $"Iniciada el {operacion.FechaInicio.Value:dd/MM/yyyy}"
                : string.Empty;

            var hojas = await _portalClienteService.ObtenerHojasMantenimientoAsync(_idOperacion);

            Hojas.Clear();
            foreach (var hoja in (hojas ?? new System.Collections.Generic.List<MantenimientoPreventivoHojaDto>()).OrderByDescending(h => h.CreadoEn))
            {
                Hojas.Add(hoja);
            }

            SinHojasTextBlock.Visibility = Hojas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HojasListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MantenimientoPreventivoHojaDto hoja)
            {
                Frame.Navigate(typeof(ClienteHojaMantenimientoPage), hoja.Id);
            }
        }

        private void VolverButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
