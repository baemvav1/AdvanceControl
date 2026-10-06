using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.Services.Aprobaciones;
using Advance_Control.Services.Portal;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Aprobaciones del cliente en el visor de operación: contacto dirigido,
    /// "Cerrar / Reabrir cotización", aprobación registrada por el técnico y
    /// notificación por correo (con la cuenta del técnico) con liga al portal.
    /// </summary>
    public sealed partial class OperacionVisorPage
    {
        private const string PortalClientesUrl = "https://advance-elevadores.mx/portalclientes/";

        private OperacionAprobacionDto? _aprobacion;

        private static IAprobacionService Aprobaciones => AppServices.Get<IAprobacionService>();

        private async Task RefreshAprobacionAsync()
        {
            if (!Operacion.IdOperacion.HasValue) return;
            try
            {
                _aprobacion = await Aprobaciones.ObtenerOperacionAsync(Operacion.IdOperacion.Value);
                Operacion.CotFinalizada = _aprobacion.CotFinalizada;
            }
            catch (Exception ex)
            {
                LogDebugError(nameof(RefreshAprobacionAsync), ex);
            }
            ActualizarAprobacionUi();
        }

        private void ActualizarAprobacionUi()
        {
            var a = _aprobacion;
            DirigidoText.Text = string.IsNullOrWhiteSpace(a?.DirigidoNombre) ? "Sin contacto dirigido" : $"Dirigida a {a.DirigidoNombre}";
            CotizacionEstadoText.Text = a?.EstadoTexto ?? string.Empty;
            CotizacionNotificacionText.Text = a is { CotFinalizada: true } ? a.NotificacionTexto : string.Empty;

            var cerrada = a?.CotFinalizada == true;
            CotizacionToggleText.Text = cerrada ? "Reabrir cotización" : "Cerrar cotización";
            CotizacionToggleIcon.Glyph = cerrada ? "" : "";   // candado abierto / cerrado

            var editable = !Operacion.IsSharedReadOnly;
            CambiarDirigidoItem.IsEnabled = editable;
            AprobarCotizacionItem.IsEnabled = editable && cerrada && a!.PorResponder;
            ReenviarCotizacionItem.IsEnabled = editable && cerrada && a!.IdContactoDirigido.HasValue;
        }

        /// <summary>Si la operación aún no tiene contacto dirigido, lo pide y lo guarda.</summary>
        private async Task<bool> EnsureDirigidoAsync()
        {
            if (_aprobacion?.IdContactoDirigido.HasValue == true)
                return true;
            return await ElegirDirigidoAsync("¿A quién va dirigida la operación?");
        }

        private async Task<bool> ElegirDirigidoAsync(string titulo)
        {
            if (!Operacion.IdOperacion.HasValue || !Operacion.IdCliente.HasValue)
                return false;

            var contacto = await SeleccionarDirigidoDialog.ElegirAsync(_xamlRoot!, Operacion.IdCliente.Value, titulo, actual: _aprobacion?.IdContactoDirigido);
            if (contacto == null)
                return false;

            try
            {
                _aprobacion = await Aprobaciones.SetDirigidoAsync(Operacion.IdOperacion.Value, contacto.ContactoId);
                _activityService.Registrar("Operaciones", "Contacto dirigido asignado");
                ActualizarAprobacionUi();
                return true;
            }
            catch (InvalidOperationException ex)
            {
                await MostrarErrorAsync("Contacto dirigido", ex.Message);
                return false;
            }
        }

        private async void CambiarDirigidoItem_Click(object sender, RoutedEventArgs e)
        {
            var habia = _aprobacion?.IdContactoDirigido.HasValue == true;
            if (await ElegirDirigidoAsync(habia ? "Cambiar contacto dirigido" : "¿A quién va dirigida la operación?") && habia)
                await _notificacionService.MostrarAsync("Contacto dirigido", "Lo que estaba pendiente de aprobar ahora le corresponde al nuevo contacto.");
        }

        private async void CotizacionToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            if (!await EnsureCanMutateAsync("cerrar o reabrir la cotización")) return;

            if (_aprobacion?.CotFinalizada == true)
                await ReabrirCotizacionAsync();
            else
                await CerrarCotizacionAsync();
        }

        private async Task CerrarCotizacionAsync()
        {
            if (Operacion.Cargos.Count == 0)
            {
                await MostrarErrorAsync("Sin cargos", "Agrega los cargos antes de cerrar la cotización.");
                return;
            }
            if (!await EnsureDirigidoAsync()) return;

            var confirmar = new ContentDialog
            {
                Title = "Cerrar cotización",
                Content = $"Ya no podrás agregar, editar ni eliminar cargos hasta que la reabras, y {_aprobacion!.DirigidoNombre} podrá verla y aprobarla en el Portal de Clientes.\n\nLa operación sigue abierta.",
                PrimaryButtonText = "Cerrar cotización",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = _xamlRoot
            };
            if (await confirmar.ShowAsync() != ContentDialogResult.Primary) return;

            try
            {
                _aprobacion = await Aprobaciones.CerrarCotizacionAsync(Operacion.IdOperacion!.Value);
                Operacion.CotFinalizada = _aprobacion.CotFinalizada;
                _activityService.Registrar("Operaciones", "Cotización cerrada");
                ActualizarAprobacionUi();
            }
            catch (InvalidOperationException ex)
            {
                await MostrarErrorAsync("Cerrar cotización", ex.Message);
                return;
            }

            await NotificarCotizacionAsync();
        }

        private async Task ReabrirCotizacionAsync()
        {
            var aprobada = _aprobacion?.Estado == "Aprobada";
            var confirmar = new ContentDialog
            {
                Title = "Reabrir cotización",
                Content = aprobada
                    ? "La cotización ya fue aprobada por el cliente. Al reabrirla, esa aprobación queda como reemplazada y, cuando la vuelvas a cerrar, el cliente tendrá que aprobarla de nuevo.\n\nMientras esté abierta, la operación no aparecerá en el Portal de Clientes."
                    : "Podrás modificar los cargos. Mientras esté abierta, la operación no aparecerá en el Portal de Clientes.",
                PrimaryButtonText = "Reabrir",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = _xamlRoot
            };
            if (await confirmar.ShowAsync() != ContentDialogResult.Primary) return;

            try
            {
                _aprobacion = await Aprobaciones.ReabrirCotizacionAsync(Operacion.IdOperacion!.Value);
                Operacion.CotFinalizada = _aprobacion.CotFinalizada;
                _activityService.Registrar("Operaciones", aprobada ? "Cotización aprobada reabierta" : "Cotización reabierta");
                ActualizarAprobacionUi();
            }
            catch (InvalidOperationException ex)
            {
                await MostrarErrorAsync("Reabrir cotización", ex.Message);
            }
        }

        private async void AprobarCotizacionItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Operacion.IdOperacion.HasValue) return;
            var (ok, comentario) = await AprobacionDialogs.RegistrarAprobacionAsync(_xamlRoot!, "la cotización");
            if (!ok) return;

            try
            {
                _aprobacion = await Aprobaciones.AprobarCotizacionAsync(Operacion.IdOperacion.Value, comentario);
                _activityService.Registrar("Operaciones", "Aprobación de cotización registrada");
                await _viewModel.LoadCheckAsync(Operacion);
                ActualizarAprobacionUi();
                await _notificacionService.MostrarAsync("Cotización aprobada", "Quedó registrada la aprobación del cliente a tu nombre.");
            }
            catch (InvalidOperationException ex)
            {
                await MostrarErrorAsync("Registrar aprobación", ex.Message);
            }
        }

        private async void ReenviarCotizacionItem_Click(object sender, RoutedEventArgs e) => await NotificarCotizacionAsync();

        /// <summary>
        /// Genera el PDF de la cotización y abre la vista previa del correo: el dirigido fijo en
        /// "Para", los demás contactos con login en CC y la liga al portal si el dirigido tiene login.
        /// </summary>
        private async Task NotificarCotizacionAsync()
        {
            var a = _aprobacion;
            if (a == null || !Operacion.IdOperacion.HasValue || !Operacion.IdCliente.HasValue || !a.IdContactoDirigido.HasValue)
                return;

            try
            {
                var contactos = await _contactoService.GetContactosAsync(new ContactoQueryDto { IdCliente = Operacion.IdCliente.Value });
                var dirigido = contactos.FirstOrDefault(c => c.ContactoId == a.IdContactoDirigido.Value);
                if (string.IsNullOrWhiteSpace(dirigido?.Correo))
                {
                    await MostrarErrorAsync("Sin correo", $"{a.DirigidoNombre} no tiene correo registrado; agrégalo en el catálogo de clientes para notificarle.");
                    return;
                }

                if (!a.DirigidoTieneLogin)
                    await _notificacionService.MostrarAsync("Sin acceso al portal",
                        $"{a.DirigidoNombre} no tiene login en el Portal de Clientes: solo podrá confirmar firmando y respondiendo el correo.");

                if (!await VerificarFirmasAntesDePdfAsync()) return;
                if (!string.IsNullOrEmpty(_viewModel.FindExistingPdf(Operacion.IdOperacion.Value, "Cotizacion")))
                    _viewModel.DeleteOperacionPdfs(Operacion.IdOperacion.Value, "Cotizacion");

                var dirigidoA = string.Join(" ", new[] { dirigido!.Tratamiento, dirigido.Nombre, dirigido.Apellido }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var filePath = await _viewModel.GenerateQuoteAsync(Operacion, dirigidoA);
                if (string.IsNullOrEmpty(filePath))
                {
                    await MostrarErrorAsync("Error", "No se pudo generar la cotización.");
                    return;
                }
                Operacion.CotizacionPdfPath = filePath;
                await _viewModel.UpdateCheckAsync(Operacion, "cotizacion_generada");

                var correosConLogin = await CorreosConLoginAsync(Operacion.IdCliente.Value, a.IdContactoDirigido.Value);
                var portal = new NotificacionPortal
                {
                    Liga = a.DirigidoTieneLogin ? $"{PortalClientesUrl}operaciones/{Operacion.IdOperacion.Value}" : null,
                    DirigidoNombre = a.DirigidoNombre ?? dirigidoA,
                    DirigidoTieneLogin = a.DirigidoTieneLogin,
                    QueSeAprueba = "la cotización",
                    CorreosConLogin = correosConLogin,
                    Nota = a.Version > 1 ? $"Esta es la versión {a.Version} de la cotización; reemplaza a la anterior." : null,
                };

                var email = new EnviarCotizacionDialog(filePath, dirigido, contactos, Operacion.RazonSocial ?? string.Empty, _xamlRoot!,
                    idOperacion: Operacion.IdOperacion, portal: portal);
                if (await email.ShowAsync() != ContentDialogResult.Primary)
                    return;

                await _viewModel.UpdateCheckAsync(Operacion, "cotizacion_enviada");
                await Aprobaciones.MarcarCotizacionNotificadaAsync(Operacion.IdOperacion.Value, email.Destinatarios);
                await RefreshAprobacionAsync();
                await _notificacionService.MostrarAsync("Cliente notificado", $"Se envió la cotización a {email.Destinatarios}.");
            }
            catch (InvalidOperationException ex)
            {
                await MostrarErrorAsync("Notificar al cliente", ex.Message);
            }
            catch (Exception ex)
            {
                LogDebugError(nameof(NotificarCotizacionAsync), ex);
                await MostrarErrorAsync("Error", "Ocurrió un error al notificar al cliente.");
            }
        }

        /// <summary>Correos de los contactos del cliente con login del portal (menos el dirigido), para CC.</summary>
        internal static async Task<List<string>> CorreosConLoginAsync(int idCliente, long idDirigido)
        {
            try
            {
                var logins = await AppServices.Get<IClienteLoginService>().ObtenerAsync(idCliente);
                return logins
                    .Where(l => l.ContactoId != idDirigido && !string.IsNullOrWhiteSpace(l.ContactoCorreo))
                    .Select(l => l.ContactoCorreo!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                // Sin permiso para ver logins o sin conexión: solo se sugiere al dirigido.
                return new List<string>();
            }
        }

        private Task<decimal?> PedirSubtotalOrdenCompraAsync()
            => AprobacionDialogs.SubtotalOrdenCompraAsync(_xamlRoot!, _aprobacion is { CotFinalizada: true } ? _aprobacion.Subtotal : null);
    }
}
