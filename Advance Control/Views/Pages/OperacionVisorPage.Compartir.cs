using System;
using System.Collections.Generic;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.Services.Mensajeria;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Compartir la operación por chat: modal con buscador y lista de contactos del chat (sin
    /// depender del que esté abierto en el panel; si hay uno, queda preseleccionado), descripción
    /// opcional y Enviar. Manda la misma referencia que el visor (tipo "Operacion").
    /// </summary>
    public sealed partial class OperacionVisorPage
    {
        private readonly IMensajeriaService _mensajeria = AppServices.Get<IMensajeriaService>();
        private readonly ChatPanelViewModel _chatPanel = AppServices.Get<ChatPanelViewModel>();

        private const string PlantillaContacto =
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            "<StackPanel Padding=\"0,4\" Spacing=\"1\">" +
            "<TextBlock Text=\"{Binding NombreVisible}\" />" +
            "<TextBlock Text=\"{Binding Usuario}\" FontSize=\"12\" Opacity=\"0.65\" />" +
            "</StackPanel></DataTemplate>";

        private async void CompartirButton_Click(object sender, RoutedEventArgs e)
        {
            if (_operacion == null)
            {
                return;
            }

            List<UsuarioChatDto> contactos;
            try
            {
                contactos = await _mensajeria.GetUsuariosChatAsync();
            }
            catch (Exception)
            {
                contactos = _chatPanel.Usuarios.ToList();
            }

            contactos = contactos
                .OrderBy(c => c.NombreVisible, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            if (contactos.Count == 0)
            {
                await _notificaciones.MostrarAsync("Compartir operación", "No hay contactos de chat disponibles.");
                return;
            }

            var buscar = new AutoSuggestBox { PlaceholderText = "Buscar contacto", QueryIcon = new SymbolIcon(Symbol.Find) };
            var lista = new ListView
            {
                ItemsSource = contactos,
                SelectionMode = ListViewSelectionMode.Single,
                ItemTemplate = (DataTemplate)XamlReader.Load(PlantillaContacto),
                Height = 240
            };
            var descripcion = new TextBox
            {
                Header = "Descripción",
                PlaceholderText = "Opcional: qué necesitas que revise",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80
            };

            var contenido = new StackPanel { Spacing = 8, Width = 380 };
            contenido.Children.Add(buscar);
            contenido.Children.Add(lista);
            contenido.Children.Add(descripcion);

            var dialogo = new ContentDialog
            {
                Title = $"Compartir operación #{_operacion.FolioTexto}",
                Content = contenido,
                PrimaryButtonText = "Enviar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false,
                XamlRoot = XamlRoot
            };

            lista.SelectionChanged += (_, _) => dialogo.IsPrimaryButtonEnabled = lista.SelectedItem is UsuarioChatDto;
            buscar.TextChanged += (_, _) =>
            {
                var seleccionado = lista.SelectedItem;
                var texto = buscar.Text.Trim();
                lista.ItemsSource = string.IsNullOrEmpty(texto)
                    ? contactos
                    : contactos.Where(c => c.NombreVisible.Contains(texto, StringComparison.CurrentCultureIgnoreCase)
                                        || c.Usuario.Contains(texto, StringComparison.CurrentCultureIgnoreCase)).ToList();
                if (seleccionado != null && ((IEnumerable<UsuarioChatDto>)lista.ItemsSource).Contains(seleccionado))
                {
                    lista.SelectedItem = seleccionado;
                }
            };

            // El contacto abierto en el panel de chat, si hay, queda preseleccionado.
            var abierto = contactos.FirstOrDefault(c => c.CredencialId == _chatPanel.UsuarioSeleccionado?.CredencialId);
            if (abierto != null)
            {
                lista.SelectedItem = abierto;
                lista.Loaded += (_, _) => lista.ScrollIntoView(abierto);
            }

            if (await dialogo.ShowAsync() != ContentDialogResult.Primary || lista.SelectedItem is not UsuarioChatDto destino)
            {
                return;
            }

            if (!_mensajeria.EstaConectado)
            {
                await _notificaciones.MostrarAsync("Chat sin conexión", "No se pudo enviar la operación porque el chat no está conectado.");
                return;
            }

            var mensaje = string.IsNullOrWhiteSpace(descripcion.Text)
                ? $"Referencia de operación {_operacion.Identificador ?? _idOperacion.ToString()}"
                : descripcion.Text.Trim();
            try
            {
                await _mensajeria.EnviarMensajeAsync(destino.CredencialId, mensaje, idReferencia: _idOperacion, tipoReferencia: "Operacion");
                _actividad.Registrar("Operaciones", "Operación compartida por chat");
                await _notificaciones.MostrarAsync("Operación compartida", $"Se envió al chat con {destino.NombreVisible}.");
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Compartir operación", $"No se pudo enviar. {ex.Message}");
            }
        }
    }
}
