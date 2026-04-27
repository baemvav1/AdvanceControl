using System;
using Advance_Control.Models;
using Advance_Control.Navigation;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Advance_Control.Views.Pages
{
    public sealed partial class AccesoClientePage : Page
    {
        public AccesoClienteViewModel ViewModel { get; }
        private readonly INavigationService _navigationService;

        public AccesoClientePage()
        {
            ViewModel = AppServices.Get<AccesoClienteViewModel>();
            _navigationService = AppServices.Get<INavigationService>();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeAsync();
        }

        private void ClienteSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.ActualizarSugerencias(sender.Text);
            }
        }

        private async void ClienteSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var texto = args.SelectedItem?.ToString();
            sender.Text = texto ?? string.Empty;
            await ViewModel.SeleccionarClienteAsync(texto);
        }

        private async void ClienteSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var texto = args.ChosenSuggestion?.ToString() ?? args.QueryText;
            await ViewModel.SeleccionarClienteAsync(texto);
        }

        private void TodasCard_Click(object sender, RoutedEventArgs e) => NavegarConFiltro(AccesoClienteFiltro.Todas);
        private void FacturadasCard_Click(object sender, RoutedEventArgs e) => NavegarConFiltro(AccesoClienteFiltro.Facturadas);
        private void FinalizadasCard_Click(object sender, RoutedEventArgs e) => NavegarConFiltro(AccesoClienteFiltro.Finalizadas);
        private void SinFinalizarCard_Click(object sender, RoutedEventArgs e) => NavegarConFiltro(AccesoClienteFiltro.SinFinalizar);

        private void NavegarConFiltro(AccesoClienteFiltro filtro)
        {
            var ctx = ViewModel.BuildContext(filtro);
            if (ctx == null) return;
            _navigationService.Navigate("Operaciones", ctx);
        }
    }
}
