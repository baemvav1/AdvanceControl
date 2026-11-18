using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.Models;
using Advance_Control.ViewModels;
using Advance_Control.Navigation;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página de inicio del sistema.
    /// Muestra bienvenida personalizada al usuario autenticado y el grid "Ops"
    /// con las operaciones/facturas pendientes de resolver.
    /// </summary>
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }
        private readonly INavigationService _navigationService;
        private readonly ChatPanelViewModel _chat;

        /// <summary>Chats con mensajes sin leer (tab Notificaciones), el más reciente primero.</summary>
        public ObservableCollection<UsuarioChatDto> ChatsNoLeidos { get; } = new();

        public DashboardPage()
        {
            ViewModel = AppServices.Get<DashboardViewModel>();
            _navigationService = AppServices.Get<INavigationService>();
            _chat = AppServices.Get<ChatPanelViewModel>();
            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(DashboardPage));
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _chat.Chats.CollectionChanged += Chats_CollectionChanged;
            _chat.PropertyChanged += Chat_PropertyChanged;
            SincronizarNotificaciones();
            _ = _chat.CargarChatsAsync();
            await ViewModel.LoadAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _chat.Chats.CollectionChanged -= Chats_CollectionChanged;
            _chat.PropertyChanged -= Chat_PropertyChanged;
        }

        // ---------------- Notificaciones ----------------

        private void Chats_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => SincronizarNotificaciones();

        /// <summary>TotalNoLeidos cambia cuando llega un mensaje o se lee un chat.</summary>
        private void Chat_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatPanelViewModel.TotalNoLeidos))
                SincronizarNotificaciones();
        }

        /// <summary>Deja en el tab solo los chats con no leídos, en el orden de la lista de chats.</summary>
        private void SincronizarNotificaciones()
        {
            var actuales = _chat.Chats.Where(c => c.TieneNoLeidos).ToList();
            if (!actuales.SequenceEqual(ChatsNoLeidos))
            {
                foreach (var quitar in ChatsNoLeidos.Except(actuales).ToList())
                    ChatsNoLeidos.Remove(quitar);
                for (var i = 0; i < actuales.Count; i++)
                {
                    var idx = ChatsNoLeidos.IndexOf(actuales[i]);
                    if (idx < 0) ChatsNoLeidos.Insert(i, actuales[i]);
                    else if (idx != i) ChatsNoLeidos.Move(idx, i);
                }
            }

            TxtSinNotificaciones.Visibility = ChatsNoLeidos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>Abre el chat en el panel derecho; al cargarlo se marca leído y la card se va.</summary>
        private void GridNotificaciones_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not UsuarioChatDto usuario || App.MainWindow is not MainWindow ventana)
                return;

            // Si ese chat quedó seleccionado con el panel oculto, reabrirlo para que se recargue y marque leído.
            if (_chat.UsuarioSeleccionado?.CredencialId == usuario.CredencialId)
                _chat.UsuarioSeleccionado = null;

            ventana.MostrarChatPanel();
            ventana.AbrirChatConUsuario(usuario.CredencialId);
        }

        private void OpsAbiertasPendientes_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
            => _navigationService.Navigate("Operaciones");

        private void OpsTFinalizadoPendientes_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
            => _navigationService.Navigate("Operaciones");

        private void OpsFinalizadasPendientes_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
            => _navigationService.Navigate("Facturacion");
    }
}

