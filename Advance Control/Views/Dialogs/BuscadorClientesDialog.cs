using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Buscador de clientes por razón social (también RFC y nombre comercial), con "Nuevo cliente".
    /// Devuelve el cliente elegido o creado, o null si se cancela.
    /// </summary>
    public static class BuscadorClientesDialog
    {
        /// <param name="excluir">Clientes que no deben aparecer (p. ej. los ya vinculados).</param>
        public static Task<CustomerDto?> MostrarAsync(XamlRoot xamlRoot, IEnumerable<int>? excluir = null)
        {
            var clienteService = AppServices.Get<IClienteService>();
            var excluidos = new HashSet<int>(excluir ?? Enumerable.Empty<int>());
            var tcs = new TaskCompletionSource<CustomerDto?>();
            Action? cerrar = null;

            var buscador = new BuscadorListaUserControl(
                titulo: "Buscador clientes...",
                placeholder: "Buscar por razón social, RFC o nombre comercial...",
                indicacion: "Escribe la razón social del cliente",
                textoNuevo: "Nuevo cliente",
                cargar: async () => (await clienteService.GetClientesAsync())
                    .Where(c => c.Estatus && !excluidos.Contains(c.IdCliente))
                    .OrderBy(c => c.RazonSocial, StringComparer.CurrentCultureIgnoreCase)
                    .Select(c => new BuscadorListaItem(c, c.RazonSocial,
                        string.Join(" · ", new[] { c.Rfc, c.NombreComercial }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        $"{c.RazonSocial} {c.Rfc} {c.NombreComercial}"))
                    .ToList());

            void Terminar(CustomerDto? cliente)
            {
                if (tcs.Task.IsCompleted) return;
                cerrar?.Invoke();
                tcs.TrySetResult(cliente);
            }

            buscador.Elegido += (_, item) => Terminar(item.Dato as CustomerDto);
            buscador.Cancelado += (_, _) => Terminar(null);
            buscador.NuevoSolicitado += async (_, _) =>
            {
                var creado = await NuevoClienteAsync(xamlRoot, clienteService);
                if (creado is not null)
                    Terminar(creado);
            };

            cerrar = ModalPopup.Abrir(xamlRoot, buscador, alEscape: () => Terminar(null));
            return tcs.Task;
        }

        /// <summary>Alta de cliente (formulario existente) encima del buscador. Devuelve el cliente creado.</summary>
        private static async Task<CustomerDto?> NuevoClienteAsync(XamlRoot xamlRoot, IClienteService clienteService)
        {
            var form = new NuevoClienteUserControl();
            var error = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };
            var contenido = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 8, Children = { form, error } };
            CustomerDto? creado = null;

            await ModalPopup.FormularioAsync(xamlRoot, "Nuevo cliente", contenido, async () =>
            {
                if (!form.IsValid)
                {
                    error.Text = "Completa los campos obligatorios: RFC, razón social y nombre comercial.";
                    error.Visibility = Visibility.Visible;
                    return false;
                }

                var dto = new ClienteEditDto
                {
                    Operacion = "create",
                    Rfc = form.Rfc,
                    RazonSocial = form.RazonSocial,
                    NombreComercial = form.NombreComercial,
                    RegimenFiscal = form.RegimenFiscal,
                    UsoCfdi = form.UsoCfdi,
                    DiasCredito = form.DiasCredito,
                    LimiteCredito = form.LimiteCredito,
                    Prioridad = form.Prioridad,
                    Notas = form.Notas,
                    Estatus = form.Estatus,
                    CodigoPostal = form.CodigoPostal
                };
                var respuesta = await clienteService.CreateClienteAsync(dto);
                if (!respuesta.Success || respuesta.IdCliente is not > 0)
                {
                    error.Text = respuesta.Message ?? "No se pudo crear el cliente.";
                    error.Visibility = Visibility.Visible;
                    return false;
                }

                creado = new CustomerDto
                {
                    IdCliente = respuesta.IdCliente.Value,
                    Rfc = dto.Rfc ?? string.Empty,
                    RazonSocial = dto.RazonSocial ?? string.Empty,
                    NombreComercial = dto.NombreComercial ?? string.Empty,
                    RegimenFiscal = dto.RegimenFiscal ?? string.Empty,
                    UsoCfdi = dto.UsoCfdi ?? string.Empty,
                    DiasCredito = dto.DiasCredito,
                    LimiteCredito = dto.LimiteCredito,
                    Prioridad = dto.Prioridad ?? 0,
                    Notas = dto.Notas ?? string.Empty,
                    Estatus = dto.Estatus,
                    CodigoPostal = dto.CodigoPostal
                };
                return true;
            });

            return creado;
        }
    }
}
