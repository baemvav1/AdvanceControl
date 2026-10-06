using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Aprobaciones;
using Advance_Control.Services.Contactos;
using Advance_Control.Utilities;
using Advance_Control.Views.Dialogs;
using Advance_Control.Views.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Formularios
{
    /// <summary>
    /// Aprobación de la hoja por el cliente: notificación por correo (cuenta del técnico) con la
    /// liga al portal, aprobación registrada por el técnico y reenvío. Solo el contacto dirigido
    /// de la operación aprueba o rechaza.
    /// </summary>
    public sealed partial class MantenimientoPreventivoWindow
    {
        private const string PortalClientesUrl = "https://advance-elevadores.mx/portalclientes/";

        private HojaAprobacionDto? _hojaAprobacion;

        private static IAprobacionService Aprobaciones => AppServices.Get<IAprobacionService>();

        private async Task RefreshHojaAprobacionAsync()
        {
            if (_hojaActual == null)
                return;

            try
            {
                _hojaAprobacion = await Aprobaciones.ObtenerHojaAsync(_hojaActual.Id);
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al consultar la aprobación de la hoja: {ex.Message}");
                return;
            }

            var pendiente = _hojaAprobacion.Estado == "Completada";
            AprobarHojaButton.IsEnabled = pendiente;
            ReenviarHojaButton.IsEnabled = pendiente;
            ToolTipService.SetToolTip(ReenviarHojaButton, _hojaAprobacion.NotificacionTexto);

            if (_hojaAprobacion.Estado == "Firmada")
            {
                FinalizarButton.IsEnabled = false;
                EstadoFinalizarTextBlock.Text = _hojaAprobacion.AprobadaVia == "advance"
                    ? $"Aprobación registrada por {_hojaAprobacion.AprobadaPor} el {_hojaAprobacion.FirmadaEn?.ToLocalTime():dd/MM/yyyy HH:mm}."
                    : $"Aprobada por {_hojaAprobacion.AprobadaPor ?? "el cliente"} en el portal el {_hojaAprobacion.FirmadaEn?.ToLocalTime():dd/MM/yyyy HH:mm}.";
            }
            else if (pendiente)
            {
                EstadoFinalizarTextBlock.Text = $"Pendiente de aprobación de {_hojaAprobacion.DirigidoNombre ?? "el cliente"} · {_hojaAprobacion.NotificacionTexto}.";
            }
        }

        private async void AprobarHojaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hojaActual == null) return;

            var (ok, comentario) = await AprobacionDialogs.RegistrarAprobacionAsync(this.Content.XamlRoot, "esta hoja de mantenimiento");
            if (!ok) return;

            try
            {
                _hojaAprobacion = await Aprobaciones.AprobarHojaAsync(_hojaActual.Id, comentario);
                _hojaActual.Estado = _hojaAprobacion.Estado;
                await RefreshHojaAprobacionAsync();
            }
            catch (InvalidOperationException ex)
            {
                EstadoFinalizarTextBlock.Text = ex.Message;
            }
        }

        private async void ReenviarHojaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hojaActual == null || !_operacion.IdOperacion.HasValue) return;

            ReenviarHojaButton.IsEnabled = false;
            try
            {
                // El formulario ya está rehidratado con la hoja guardada: se regenera el mismo PDF.
                var pdf = await _pdfService.GeneratePdfAsync(BuildPdfData());
                await NotificarHojaAsync(pdf, correccion: false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al reenviar la hoja de mantenimiento: {ex.Message}");
                EstadoFinalizarTextBlock.Text = "No se pudo generar el PDF para reenviar la notificación.";
            }
            finally
            {
                ReenviarHojaButton.IsEnabled = _hojaAprobacion?.Estado == "Completada";
            }
        }

        /// <summary>
        /// Vista previa del correo con la hoja en PDF: el dirigido fijo en "Para", los demás contactos
        /// con login en CC y, si el dirigido tiene login, la liga a la hoja en el portal.
        /// </summary>
        private async Task NotificarHojaAsync(string pdfPath, bool correccion)
        {
            if (_hojaActual == null || !_operacion.IdOperacion.HasValue || !_operacion.IdCliente.HasValue)
                return;
            if (!File.Exists(pdfPath))
            {
                EstadoFinalizarTextBlock.Text = "No se encontró el PDF de la hoja en este equipo; usa \"Reenviar notificación\" para generarlo de nuevo.";
                return;
            }

            try
            {
                var aprobacion = _hojaAprobacion ?? await Aprobaciones.ObtenerHojaAsync(_hojaActual.Id);
                if (!aprobacion.IdContactoDirigido.HasValue)
                {
                    EstadoFinalizarTextBlock.Text = "La operación no tiene contacto dirigido: asígnalo desde el visor de la operación para notificar al cliente.";
                    return;
                }

                var contactos = await AppServices.Get<IContactoService>().GetContactosAsync(new ContactoQueryDto { IdCliente = _operacion.IdCliente.Value });
                var dirigido = contactos.FirstOrDefault(c => c.ContactoId == aprobacion.IdContactoDirigido.Value);
                if (string.IsNullOrWhiteSpace(dirigido?.Correo))
                {
                    EstadoFinalizarTextBlock.Text = $"{aprobacion.DirigidoNombre} no tiene correo registrado; agrégalo en el catálogo de clientes para notificarle.";
                    return;
                }

                if (!aprobacion.DirigidoTieneLogin)
                    EstadoFinalizarTextBlock.Text = $"{aprobacion.DirigidoNombre} no tiene login en el Portal de Clientes: solo podrá confirmar firmando y respondiendo el correo.";

                var portal = new NotificacionPortal
                {
                    Liga = aprobacion.DirigidoTieneLogin ? $"{PortalClientesUrl}hojas/{_hojaActual.Id}" : null,
                    DirigidoNombre = aprobacion.DirigidoNombre ?? dirigido!.NombreCompleto,
                    DirigidoTieneLogin = aprobacion.DirigidoTieneLogin,
                    QueSeAprueba = "la hoja de mantenimiento preventivo",
                    CorreosConLogin = await OperacionVisorPage.CorreosConLoginAsync(_operacion.IdCliente.Value, aprobacion.IdContactoDirigido.Value),
                    Nota = correccion ? "Corregimos la hoja de mantenimiento que nos rechazó; le pedimos revisarla de nuevo." : null,
                };

                var email = new EnviarCotizacionDialog(pdfPath, dirigido, contactos, _operacion.RazonSocial ?? string.Empty, this.Content.XamlRoot,
                    tipo: "Mantenimiento Preventivo", idOperacion: _operacion.IdOperacion, portal: portal);
                if (await email.ShowAsync() != ContentDialogResult.Primary)
                    return;

                await Aprobaciones.MarcarHojaNotificadaAsync(_hojaActual.Id, email.Destinatarios);
                _hojaAprobacion = null;
                await RefreshHojaAprobacionAsync();
            }
            catch (InvalidOperationException ex)
            {
                EstadoFinalizarTextBlock.Text = $"No se pudo notificar al cliente: {ex.Message}";
            }
        }
    }
}
