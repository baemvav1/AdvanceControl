using System;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Utilities;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Aprobación del cliente: cerrar la cotización (queda visible en el Portal de Clientes y se
    /// notifica al dirigido), registrar la aprobación a nombre del técnico y reenviar la
    /// notificación. Además: ver la refacción del cargo seleccionado y borrar fotos de cargos.
    /// </summary>
    public sealed partial class OperacionVisorPage
    {
        private const string PortalClientesUrl = "https://advance-elevadores.mx/portalclientes/";

        /// <summary>
        /// Abierta: "Cerrar cotización". Cerrada: "Registrar aprobación" (si el cliente no ha
        /// respondido) y "Reenviar notificación". Nada de esto en solo lectura.
        /// </summary>
        private void ActualizarBotonesCotizacion()
        {
            var op = _operacion;
            var puede = op is { CanMutateOperation: true };
            var cerrada = op?.CotFinalizada == true;

            CerrarCotizacionButton.Visibility = puede && !cerrada ? Visibility.Visible : Visibility.Collapsed;
            AprobarCotizacionButton.Visibility = puede && cerrada ? Visibility.Visible : Visibility.Collapsed;
            ReenviarNotificacionButton.Visibility = puede && cerrada ? Visibility.Visible : Visibility.Collapsed;

            AprobarCotizacionButton.IsEnabled = _aprobacion?.PorResponder == true;
            ToolTipService.SetToolTip(AprobarCotizacionButton, _aprobacion?.PorResponder == true
                ? "Registrar que el cliente aprobó la cotización (queda a tu nombre)"
                : "El cliente ya respondió la cotización");
            ReenviarNotificacionButton.IsEnabled = _aprobacion?.IdContactoDirigido.HasValue == true;
        }

        private async void CerrarCotizacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_operacion is not { CanMutateOperation: true } || _operacion.CotFinalizada) return;

            if (!FilasCargos.Any())
            {
                await _notificaciones.MostrarAsync("Sin cargos", "Agrega los cargos antes de cerrar la cotización.");
                return;
            }

            if (!await AsegurarDirigidoAsync()) return;

            if (!await ConfirmarAsync("Cerrar cotización",
                    $"Ya no podrás agregar, editar ni eliminar cargos hasta que la reabras, y {_aprobacion!.DirigidoNombre} podrá verla y aprobarla en el Portal de Clientes.\n\nLa operación sigue abierta.",
                    "Cerrar cotización"))
            {
                return;
            }

            try
            {
                _aprobacion = await _aprobaciones.CerrarCotizacionAsync(_idOperacion);
                _operacion.CotFinalizada = _aprobacion.CotFinalizada;
                _actividad.Registrar("Operaciones", "Cotización cerrada");
            }
            catch (InvalidOperationException ex)
            {
                await _notificaciones.MostrarAsync("Cerrar cotización", ex.Message);
                return;
            }

            await CargarOperacionAsync(primeraVez: false);
            await NotificarCotizacionAsync();
        }

        private async void AprobarCotizacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_operacion is not { CanMutateOperation: true }) return;

            var (ok, comentario) = await AprobacionDialogs.RegistrarAprobacionAsync(XamlRoot, "la cotización");
            if (!ok) return;

            try
            {
                _aprobacion = await _aprobaciones.AprobarCotizacionAsync(_idOperacion, comentario);
                _actividad.Registrar("Operaciones", "Aprobación de cotización registrada");
                await CargarOperacionAsync(primeraVez: false);
                await _notificaciones.MostrarAsync("Cotización aprobada", "Quedó registrada la aprobación del cliente a tu nombre.");
            }
            catch (InvalidOperationException ex)
            {
                await _notificaciones.MostrarAsync("Registrar aprobación", ex.Message);
            }
        }

        private async void ReenviarNotificacionButton_Click(object sender, RoutedEventArgs e) => await NotificarCotizacionAsync();

        /// <summary>Si la operación aún no tiene contacto dirigido, lo pide y lo guarda.</summary>
        private async Task<bool> AsegurarDirigidoAsync()
        {
            if (_aprobacion?.IdContactoDirigido.HasValue == true) return true;
            if (_operacion?.IdCliente is not int idCliente) return false;

            var contacto = await SeleccionarDirigidoDialog.ElegirAsync(XamlRoot, idCliente, "¿A quién va dirigida la operación?", actual: null);
            if (contacto == null) return false;

            try
            {
                _aprobacion = await _aprobaciones.SetDirigidoAsync(_idOperacion, contacto.ContactoId);
                _actividad.Registrar("Operaciones", "Contacto dirigido asignado");
                CotizacionEstadoText.Text = _aprobacion.EstadoTexto ?? string.Empty;
                await CargarContactosAsync();
                return true;
            }
            catch (InvalidOperationException ex)
            {
                await _notificaciones.MostrarAsync("Contacto dirigido", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Genera el PDF de la cotización y abre la vista previa del correo: el dirigido fijo en
        /// "Para", los demás contactos con login en CC y la liga al portal si el dirigido tiene login.
        /// </summary>
        private async Task NotificarCotizacionAsync()
        {
            var a = _aprobacion;
            var op = _operacion;
            if (a == null || op?.IdCliente is not int idCliente || a.IdContactoDirigido is not long idDirigido) return;

            try
            {
                var contactos = await ContactosClienteAsync();
                var dirigido = contactos.FirstOrDefault(c => c.ContactoId == idDirigido);
                if (string.IsNullOrWhiteSpace(dirigido?.Correo))
                {
                    await _notificaciones.MostrarAsync("Sin correo", $"{a.DirigidoNombre} no tiene correo registrado; agrégalo en el catálogo de clientes para notificarle.");
                    return;
                }

                if (!a.DirigidoTieneLogin)
                {
                    await _notificaciones.MostrarAsync("Sin acceso al portal",
                        $"{a.DirigidoNombre} no tiene login en el Portal de Clientes: solo podrá confirmar firmando y respondiendo el correo.");
                }

                if (!await VerificarFirmasAsync()) return;
                if (!string.IsNullOrEmpty(_operaciones.FindExistingPdf(_idOperacion, "Cotizacion")))
                {
                    _operaciones.DeleteOperacionPdfs(_idOperacion, "Cotizacion");
                }

                var dirigidoA = NombreDirigido(dirigido) ?? string.Empty;
                var ruta = await _operaciones.GenerateQuoteAsync(op, dirigidoA);
                if (string.IsNullOrEmpty(ruta))
                {
                    await _notificaciones.MostrarAsync("Error", "No se pudo generar la cotización.");
                    return;
                }

                op.CotizacionPdfPath = ruta;
                await _operaciones.UpdateCheckAsync(op, "cotizacion_generada");

                var portal = new NotificacionPortal
                {
                    Liga = a.DirigidoTieneLogin ? $"{PortalClientesUrl}operaciones/{_idOperacion}" : null,
                    DirigidoNombre = a.DirigidoNombre ?? dirigidoA,
                    DirigidoTieneLogin = a.DirigidoTieneLogin,
                    QueSeAprueba = "la cotización",
                    CorreosConLogin = await CorreosConLoginAsync(idCliente, idDirigido),
                    Nota = a.Version > 1 ? $"Esta es la versión {a.Version} de la cotización; reemplaza a la anterior." : null,
                };

                var correo = new EnviarCotizacionDialog(ruta, dirigido, contactos, op.RazonSocial ?? string.Empty, XamlRoot,
                    idOperacion: _idOperacion, portal: portal);
                if (await correo.ShowAsync() != ContentDialogResult.Primary)
                {
                    await RefrescarGaleriaAsync();
                    return;
                }

                await _operaciones.UpdateCheckAsync(op, "cotizacion_enviada");
                await _aprobaciones.MarcarCotizacionNotificadaAsync(_idOperacion, correo.Destinatarios);
                await CargarOperacionAsync(primeraVez: false);
                await _notificaciones.MostrarAsync("Cliente notificado", $"Se envió la cotización a {correo.Destinatarios}.");
            }
            catch (InvalidOperationException ex)
            {
                await _notificaciones.MostrarAsync("Notificar al cliente", ex.Message);
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Error", $"Ocurrió un error al notificar al cliente. {ex.Message}");
            }
        }

        // ---------------- Ver refacción del cargo seleccionado ----------------

        private static bool EsRefaccion(CargoFila? cargo) =>
            cargo is { IdRelacionCargo: not null } && cargo.Tipo.StartsWith("Refacc", StringComparison.OrdinalIgnoreCase);

        private async void VerRefaccionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cargoActual is not { IdRelacionCargo: int idRefaccion } || !EsRefaccion(_cargoActual)) return;

            try
            {
                var dialogo = new ContentDialog
                {
                    Title = "Detalles de la Refacción",
                    Content = new RefaccionesViewerUserControl(idRefaccion),
                    CloseButtonText = "Cerrar",
                    XamlRoot = XamlRoot
                };
                await dialogo.ShowAsync();
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Ver refacción", $"No se pudo cargar la refacción. {ex.Message}");
            }
        }

        // ---------------- Borrar fotos de un cargo (la X de la miniatura) ----------------

        private async Task EliminarFotoCargoAsync(CargoImageDto foto)
        {
            if (string.IsNullOrEmpty(foto.FileName) || _operacion is not { CanMutateOperation: true })
            {
                await _notificaciones.MostrarAsync("Operación finalizada", "La operación no permite eliminar fotos.");
                return;
            }

            if (!await ConfirmarAsync("Confirmar eliminación", $"¿Eliminar la imagen {foto.FileName}?", "Eliminar")) return;

            try
            {
                if (await _imagenesCargo.DeleteImageAsync(_idOperacion, foto.FileName))
                {
                    _actividad.Registrar("Operaciones", "Imagen eliminada");
                    await RefrescarGaleriaAsync();
                }
                else
                {
                    await _notificaciones.MostrarAsync("Error", "No se pudo eliminar la imagen.");
                }
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Error", $"Ocurrió un error al eliminar la imagen. {ex.Message}");
            }
        }
    }
}
