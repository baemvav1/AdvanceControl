using Advance_Control.Models;
using Advance_Control.Services.Portal;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace Advance_Control.Views.Portal
{
    /// <summary>Lista de operaciones del Portal de Cliente.</summary>
    public sealed partial class ClienteOperacionesPage : Page
    {
        private readonly IPortalClienteService _portalClienteService;

        public ObservableCollection<OperacionClientePortalDto> Operaciones { get; } = new();

        public ClienteOperacionesPage()
        {
            _portalClienteService = AppServices.Get<IPortalClienteService>();

            InitializeComponent();
            OperacionesListView.ItemsSource = Operaciones;

            Loaded += async (_, _) => await CargarOperacionesAsync();
        }

        private async System.Threading.Tasks.Task CargarOperacionesAsync()
        {
            CargandoProgressRing.IsActive = true;
            CargandoProgressRing.Visibility = Visibility.Visible;
            SinOperacionesTextBlock.Visibility = Visibility.Collapsed;

            try
            {
                var operaciones = await _portalClienteService.ObtenerOperacionesAsync();

                Operaciones.Clear();
                foreach (var operacion in operaciones.OrderByDescending(o => o.FechaInicio))
                {
                    Operaciones.Add(operacion);
                }

                SinOperacionesTextBlock.Visibility = Operaciones.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            finally
            {
                CargandoProgressRing.IsActive = false;
                CargandoProgressRing.Visibility = Visibility.Collapsed;
            }
        }

        private void OperacionesListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is OperacionClientePortalDto operacion)
            {
                Frame.Navigate(typeof(ClienteOperacionDetallePage), operacion.IdOperacion);
            }
        }
    }
}
