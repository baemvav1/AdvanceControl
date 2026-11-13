using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.PersonalCliente;
using Advance_Control.Utilities;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Pivot "Personal": técnicos, supervisores y administrativos (agentes) asignados al cliente.
    /// Se guarda en el momento; el API solo deja asignar/quitar a niveles 1 a 6.
    /// </summary>
    public sealed partial class ClientesPage
    {
        private IPersonalClienteService PersonalClienteService => AppServices.Get<IPersonalClienteService>();

        private async Task LoadPersonalForClienteAsync(CustomerDto cliente)
        {
            if (cliente.IsLoadingPersonal)
                return;

            try
            {
                cliente.IsLoadingPersonal = true;
                var personal = await PersonalClienteService.GetPersonalAsync(cliente.IdCliente);

                cliente.Tecnicos.Clear();
                cliente.Supervisores.Clear();
                cliente.Agentes.Clear();
                foreach (var p in personal)
                    cliente.PersonalDeRol(p.Rol)?.Add(p);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el personal asignado del cliente", ex, "ClientesPage", nameof(LoadPersonalForClienteAsync));
            }
            finally
            {
                cliente.PersonalLoaded = true;
                cliente.NotifyPersonalChanged();
                cliente.IsLoadingPersonal = false;
            }
        }

        /// <summary>+ de una lista: Tag = cliente, CommandParameter = rol.</summary>
        private async void AgregarPersonal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: CustomerDto cliente, CommandParameter: string rol })
                return;
            var lista = cliente.PersonalDeRol(rol);
            if (lista is null) return;
            var rolTexto = PersonalClienteDto.RolTexto(rol);

            List<PersonalClienteDto> candidatos;
            try
            {
                var asignados = lista.Select(p => p.CredencialId).ToHashSet();
                candidatos = (await PersonalClienteService.GetCandidatosAsync(rol))
                    .Where(c => !asignados.Contains(c.CredencialId)).ToList();
            }
            catch (Exception ex)
            {
                await _notificacionService.MostrarAsync("Error", ex.Message);
                return;
            }

            var elegido = await BuscadorListaDialog.ElegirAsync(XamlRoot, $"Asignar {rolTexto}", "Buscar por nombre o usuario",
                $"No hay más usuarios disponibles para {rolTexto}.",
                candidatos.Select(c => new BuscadorListaItem(c, c.NombreCompleto, c.Detalle, $"{c.Nombre} {c.Usuario} {c.Correo}")));
            if (elegido is not PersonalClienteDto usuario)
                return;

            var error = await PersonalClienteService.AsignarAsync(cliente.IdCliente, usuario.CredencialId, rol);
            if (error is not null)
            {
                await _notificacionService.MostrarAsync("Error", error);
                return;
            }

            usuario.Rol = rol;
            usuario.IdCliente = cliente.IdCliente;
            var i = 0;
            while (i < lista.Count && string.Compare(lista[i].Nombre, usuario.Nombre, StringComparison.CurrentCultureIgnoreCase) < 0) i++;
            lista.Insert(i, usuario);
            cliente.NotifyPersonalChanged();
        }

        /// <summary>Basura de un renglón: quita al usuario del rol en el cliente (con confirmación).</summary>
        private async void QuitarPersonal_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not PersonalClienteDto usuario)
                return;
            var cliente = ViewModel.Customers.FirstOrDefault(c => c.PersonalDeRol(usuario.Rol)?.Contains(usuario) == true);
            if (cliente is null || usuario.Rol is null)
                return;

            var dialog = new ContentDialog
            {
                Title = "Quitar personal",
                Content = new TextBlock
                {
                    Text = $"¿Quitar a {usuario.NombreCompleto} como {PersonalClienteDto.RolTexto(usuario.Rol)} de {cliente.RazonSocial}?",
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = "Quitar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var error = await PersonalClienteService.QuitarAsync(cliente.IdCliente, usuario.CredencialId, usuario.Rol);
            if (error is not null)
            {
                await _notificacionService.MostrarAsync("Error", error);
                return;
            }

            cliente.PersonalDeRol(usuario.Rol)?.Remove(usuario);
            cliente.NotifyPersonalChanged();
        }
    }
}
