using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Contactos;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Buscador de contactos (con "Nuevo contacto") y detalle/edición de un contacto, como modales.
    /// </summary>
    public static class ContactosDialogs
    {
        /// <summary>
        /// Buscador de contactos por nombre, apellido, cargo o correo. No lista a los usuarios del
        /// sistema (con credencial) ni a los contactos de proveedores, ni a los de <paramref name="excluir"/>.
        /// Devuelve el contacto elegido o creado, o null si se cancela.
        /// </summary>
        public static Task<ContactoDto?> BuscarAsync(XamlRoot xamlRoot, IEnumerable<long>? excluir = null)
        {
            var contactoService = AppServices.Get<IContactoService>();
            var excluidos = new HashSet<long>(excluir ?? Enumerable.Empty<long>());
            var tcs = new TaskCompletionSource<ContactoDto?>();
            Action? cerrar = null;

            var buscador = new BuscadorListaUserControl(
                titulo: "Buscador contactos...",
                placeholder: "Buscar por nombre, apellido, cargo o correo...",
                indicacion: "Escribe el nombre del contacto",
                textoNuevo: "Nuevo contacto",
                cargar: async () => (await contactoService.GetContactosAsync())
                    .Where(c => c.Activo != false && !c.TieneCredencial && c.IdProveedor is null && !excluidos.Contains(c.ContactoId))
                    .OrderBy(c => c.NombreCompleto, StringComparer.CurrentCultureIgnoreCase)
                    .Select(c => new BuscadorListaItem(c, c.NombreCompleto, c.DetalleCorto,
                        $"{c.NombreCompleto} {c.Cargo} {c.Correo} {c.Telefono}"))
                    .ToList());

            void Terminar(ContactoDto? contacto)
            {
                if (tcs.Task.IsCompleted) return;
                cerrar?.Invoke();
                tcs.TrySetResult(contacto);
            }

            buscador.Elegido += (_, item) => Terminar(item.Dato as ContactoDto);
            buscador.Cancelado += (_, _) => Terminar(null);
            buscador.NuevoSolicitado += async (_, _) =>
            {
                var creado = await NuevoAsync(xamlRoot);
                if (creado is not null)
                    Terminar(creado);
            };

            cerrar = ModalPopup.Abrir(xamlRoot, buscador, alEscape: () => Terminar(null));
            return tcs.Task;
        }

        /// <summary>Alta de contacto. Devuelve el contacto creado (con su id) o null.</summary>
        public static async Task<ContactoDto?> NuevoAsync(XamlRoot xamlRoot)
        {
            var contactoService = AppServices.Get<IContactoService>();
            var form = new ContactoFormUserControl();
            ContactoDto? creado = null;

            await ModalPopup.FormularioAsync(xamlRoot, "Nuevo contacto", form, async () =>
            {
                if (!form.Validar()) return false;
                var respuesta = await contactoService.CreateContactoAsync(form.ADto());
                if (!respuesta.Success || respuesta.ContactoId is not > 0)
                {
                    form.MostrarError(respuesta.Message ?? "No se pudo crear el contacto.");
                    return false;
                }
                creado = form.AContacto(respuesta.ContactoId.Value);
                return true;
            });

            return creado;
        }

        /// <summary>Detalle del contacto con opción de editarlo. Devuelve el contacto actualizado, o null si no hubo cambios.</summary>
        public static async Task<ContactoDto?> DetalleAsync(XamlRoot xamlRoot, ContactoDto contacto)
        {
            var contactoService = AppServices.Get<IContactoService>();
            var form = new ContactoFormUserControl(contacto);
            ContactoDto? actualizado = null;

            await ModalPopup.FormularioAsync(xamlRoot, contacto.NombreCompleto, form, async () =>
            {
                if (!form.Validar()) return false;
                var respuesta = await contactoService.UpdateContactoAsync(form.ADto());
                if (!respuesta.Success)
                {
                    form.MostrarError(respuesta.Message ?? "No se pudo guardar el contacto.");
                    return false;
                }
                actualizado = form.AContacto(contacto.ContactoId);
                return true;
            }, textoGuardar: "Guardar cambios", textoCancelar: "Cerrar");

            return actualizado;
        }
    }
}
