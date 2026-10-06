using System;
using Advance_Control.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Contactos;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Elegir el contacto dirigido de una operación: el único que aprueba o rechaza
    /// la cotización y las hojas de mantenimiento en el Portal de Clientes.
    /// </summary>
    public static class SeleccionarDirigidoDialog
    {
        /// <returns>El contacto elegido, o null si se canceló o el cliente no tiene contactos activos.</returns>
        public static async Task<ContactoDto?> ElegirAsync(XamlRoot xamlRoot, int idCliente, string titulo, string? explicacion = null, long? actual = null)
        {
            var contactos = (await AppServices.Get<IContactoService>().GetContactosAsync(new ContactoQueryDto { IdCliente = idCliente }))
                .Where(c => c.Activo != false)
                .OrderBy(c => c.NombreCompleto)
                .ToList();

            if (contactos.Count == 0)
            {
                await new ContentDialog
                {
                    Title = titulo,
                    Content = "El cliente no tiene contactos activos. Agrega uno en el catálogo de clientes (pivot Contactos) para poder dirigirle la operación.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = xamlRoot
                }.ShowAsync();
                return null;
            }

            var lista = new ListView { SelectionMode = ListViewSelectionMode.Single, MaxHeight = 360 };
            foreach (var contacto in contactos)
            {
                var item = new ListViewItem
                {
                    Tag = contacto,
                    Content = new StackPanel
                    {
                        Spacing = 1,
                        Children =
                        {
                            new TextBlock { Text = contacto.NombreCompleto, FontWeight = FontWeights.SemiBold },
                            new TextBlock
                            {
                                Text = string.Join(" · ", new[] { contacto.Cargo, contacto.Correo }.Where(s => !string.IsNullOrWhiteSpace(s))),
                                FontSize = 12,
                                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                            }
                        }
                    }
                };
                lista.Items.Add(item);
                if (contacto.ContactoId == actual)
                    lista.SelectedItem = item;
            }

            var dialogo = new ContentDialog
            {
                Title = titulo,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = explicacion ?? "Solo este contacto podrá aprobar o rechazar la cotización y los mantenimientos de la operación en el Portal de Clientes. Los correos le llegarán siempre a él.",
                            TextWrapping = TextWrapping.Wrap
                        },
                        lista
                    }
                },
                PrimaryButtonText = "Seleccionar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };
            dialogo.IsPrimaryButtonEnabled = lista.SelectedItem != null;
            lista.SelectionChanged += (_, _) => dialogo.IsPrimaryButtonEnabled = lista.SelectedItem != null;

            return await dialogo.ShowAsync() == ContentDialogResult.Primary && lista.SelectedItem is ListViewItem { Tag: ContactoDto elegido }
                ? elegido
                : null;
        }
    }
}
