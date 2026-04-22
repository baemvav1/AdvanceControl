using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Advance_Control.Services.Notificacion
{
    /// <summary>
    /// Implementación del servicio de notificaciones usando Windows AppNotification.
    /// </summary>
    public class NotificacionService : INotificacionService
    {
        private readonly ILoggingService _logger;

        private static readonly string[] PalabrasClaveError = { "Error", "Validación" };

        public NotificacionService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Muestra una notificación de Windows. Las que contienen "Error" o "Validación"
        /// en el título son persistentes (Reminder); el resto se auto-descartan.
        /// </summary>
        public async Task MostrarNotificacionAsync(
            string titulo,
            string? nota = null,
            DateTime? fechaHoraInicio = null,
            DateTime? fechaHoraFinal = null,
            int? tiempoDeVidaSegundos = null,
            Dictionary<string, string>? argumentos = null)
        {
            if (string.IsNullOrWhiteSpace(titulo))
                throw new ArgumentException("El título es requerido.", nameof(titulo));

            bool esPersistente = PalabrasClaveError.Any(k =>
                titulo.Contains(k, StringComparison.OrdinalIgnoreCase));

            var textoCopiable = BuildClipboardText(titulo, nota, fechaHoraInicio, fechaHoraFinal);
            var textoCopiableBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(textoCopiable));

            var builder = new AppNotificationBuilder()
                .AddText(titulo);

            if (!string.IsNullOrWhiteSpace(nota))
                builder.AddText(nota);

            if (fechaHoraInicio.HasValue)
                builder.AddText($"Inicio: {fechaHoraInicio.Value:dd/MM/yyyy HH:mm}");

            if (fechaHoraFinal.HasValue)
                builder.AddText($"Final: {fechaHoraFinal.Value:dd/MM/yyyy HH:mm}");

            if (esPersistente)
                builder.SetScenario(AppNotificationScenario.Reminder);

            // Agregar argumentos de activación (ej: acción=abrirChat, credencialId=5)
            if (argumentos != null)
            {
                foreach (var kvp in argumentos)
                    builder.AddArgument(kvp.Key, kvp.Value);
            }

            builder.AddButton(
                new AppNotificationButton("Copiar")
                    .AddArgument("notificationCommand", "copy")
                    .AddArgument("clipboardTextBase64", textoCopiableBase64));

            try
            {
                AppNotificationManager.Default.Show(builder.BuildNotification());
            }
            catch (Exception ex)
            {
                await _logger.LogWarningAsync(
                    $"No se pudo mostrar la notificación del sistema: {ex.Message}",
                    "NotificacionService",
                    "MostrarNotificacionAsync");

                // Fallback: notificación in-app cuando el sistema toast no está disponible
                InAppNotificacionMessenger.Enviar(titulo, nota, textoCopiable);
            }

            await _logger.LogInformationAsync(
                $"Notificación mostrada: {titulo}",
                "NotificacionService",
                "MostrarNotificacionAsync");
        }

        private static string BuildClipboardText(
            string titulo,
            string? nota,
            DateTime? fechaHoraInicio,
            DateTime? fechaHoraFinal)
        {
            var partes = new List<string> { titulo };

            if (!string.IsNullOrWhiteSpace(nota))
                partes.Add(nota.Trim());

            if (fechaHoraInicio.HasValue)
                partes.Add($"Inicio: {fechaHoraInicio.Value:dd/MM/yyyy HH:mm}");

            if (fechaHoraFinal.HasValue)
                partes.Add($"Final: {fechaHoraFinal.Value:dd/MM/yyyy HH:mm}");

            return string.Join(Environment.NewLine, partes);
        }
    }
}
