using System;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Lienzo de prototipos del grupo Catálogos. Visible solo para nivel 1 (devs)
    /// vía el sistema de permisos UI. Sin funcionalidad fija: se construye libremente aquí.
    /// Prototipo actual: equipo → clientes → (área → ubicación), con el Visor según la tarjeta.
    /// </summary>
    public sealed partial class ProCatalogosPage : Page
    {
        public ProCatalogosViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;

        public ProCatalogosPage()
        {
            ViewModel = AppServices.Get<ProCatalogosViewModel>();
            _notificacionService = AppServices.Get<INotificacionService>();
            InitializeComponent();
        }

        // ── Equipo ─────────────────────────────────────────────────────────

        private async void AgregarEquipo_Click(object sender, RoutedEventArgs e)
        {
            var equipo = await BuscadorEquiposDialog.MostrarAsync(XamlRoot);
            if (equipo is not null)
                ViewModel.Equipo = equipo;
        }

        private void LimpiarEquipo_Click(object sender, RoutedEventArgs e) => ViewModel.LimpiarEquipo();

        private void TarjetaEquipo_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel.MostrarEquipoEnVisor();

        // ── Clientes ───────────────────────────────────────────────────────

        private async void AgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            var cliente = await BuscadorClientesDialog.MostrarAsync(XamlRoot, ViewModel.Clientes.Select(c => c.IdCliente));
            if (cliente is null) return;
            await Avisar(await ViewModel.VincularClienteAsync(cliente));
        }

        private async void DesvincularCliente_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ClienteSeleccionado is not { } cliente || ViewModel.Equipo is not { } equipo) return;
            if (!await ConfirmarAsync("Desvincular cliente",
                    $"¿Desvincular a {cliente.RazonSocial} del equipo {equipo.Identificador}? El cliente no se borra, solo deja de estar ligado a este equipo."))
                return;
            await Avisar(await ViewModel.DesvincularClienteSeleccionadoAsync());
        }

        // ── Contactos del cliente ─────────────────────────────────────────

        private async void AgregarContacto_Click(object sender, RoutedEventArgs e)
        {
            var contacto = await ContactosDialogs.BuscarAsync(XamlRoot, ViewModel.Contactos.Select(c => c.ContactoId));
            if (contacto is null) return;
            await Avisar(await ViewModel.VincularContactoAsync(contacto));
        }

        private async void DesvincularContacto_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ContactoSeleccionado is not { } contacto || ViewModel.ClienteSeleccionado is not { } cliente) return;
            if (!await ConfirmarAsync("Desvincular contacto",
                    $"¿Desvincular a {contacto.NombreCompleto} de {cliente.RazonSocial}? El contacto no se borra, solo deja de estar ligado a este cliente."))
                return;
            await Avisar(await ViewModel.DesvincularContactoSeleccionadoAsync());
        }

        /// <summary>Clic en un contacto: queda seleccionado y se abre su detalle (editable).</summary>
        private async void Contacto_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not ContactoDto contacto) return;
            ViewModel.ContactoSeleccionado = contacto;
            var actualizado = await ContactosDialogs.DetalleAsync(XamlRoot, contacto);
            if (actualizado is not null)
                ViewModel.ReemplazarContacto(actualizado);
        }

        // ── Utilidades ────────────────────────────────────────────────────

        private async Task<bool> ConfirmarAsync(string titulo, string mensaje)
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = new TextBlock { Text = mensaje, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = "Desvincular",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private Task Avisar(string? error) =>
            error is null ? Task.CompletedTask : _notificacionService.MostrarNotificacionAsync("Error", error);
    }
}
