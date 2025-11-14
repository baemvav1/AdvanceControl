using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Notificacion
{
    /// <summary>
    /// Implementación del servicio de notificaciones.
    /// Actualmente funciona como un mock, pero en el futuro se integrará con un endpoint.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILoggingService _logger;
        private readonly ObservableCollection<Models.Notificacion> _notificaciones;

        public NotificationService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificaciones = new ObservableCollection<Models.Notificacion>();
        }

        /// <summary>
        /// Colección observable de notificaciones actuales
        /// </summary>
        public ObservableCollection<Models.Notificacion> Notificaciones => _notificaciones;

        /// <summary>
        /// Agrega una nueva notificación al sistema
        /// </summary>
        /// <param name="titulo">Título de la notificación (requerido)</param>
        /// <param name="nota">Nota o mensaje de la notificación (opcional)</param>
        /// <param name="fechaHoraInicio">Fecha y hora de inicio (opcional)</param>
        /// <param name="fechaHoraFinal">Fecha y hora final (opcional)</param>
        /// <returns>Task completado cuando la notificación se agregó</returns>
        /// <exception cref="ArgumentException">Si el título está vacío o nulo</exception>
        public async Task AgregarNotificacionAsync(
            string titulo,
            string? nota = null,
            DateTime? fechaHoraInicio = null,
            DateTime? fechaHoraFinal = null)
        {
            try
            {
                // Validar que el título no esté vacío
                if (string.IsNullOrWhiteSpace(titulo))
                {
                    throw new ArgumentException("El título de la notificación es requerido", nameof(titulo));
                }

                // Crear la nueva notificación
                var notificacion = new Models.Notificacion
                {
                    Titulo = titulo,
                    Nota = nota,
                    FechaHoraInicio = fechaHoraInicio,
                    FechaHoraFinal = fechaHoraFinal,
                    FechaCreacion = DateTime.Now,
                    Leida = false
                };

                // Agregar a la colección en el hilo de UI si está disponible
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                if (dispatcherQueue != null)
                {
                    await dispatcherQueue.EnqueueAsync(() =>
                    {
                        _notificaciones.Add(notificacion);
                    });
                }
                else
                {
                    // Si no hay dispatcher (ej: en pruebas), agregar directamente
                    _notificaciones.Add(notificacion);
                }

                // Log de la notificación
                await _logger.LogInformationAsync(
                    $"Nueva notificación agregada: {titulo}",
                    "NotificationService",
                    "AgregarNotificacionAsync");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(
                    "Error al agregar notificación",
                    ex,
                    "NotificationService",
                    "AgregarNotificacionAsync");
                throw;
            }
        }

        /// <summary>
        /// Marca una notificación como leída
        /// </summary>
        /// <param name="notificacion">La notificación a marcar como leída</param>
        public void MarcarComoLeida(Models.Notificacion notificacion)
        {
            if (notificacion == null)
                throw new ArgumentNullException(nameof(notificacion));

            notificacion.Leida = true;
        }

        /// <summary>
        /// Elimina una notificación del sistema
        /// </summary>
        /// <param name="notificacion">La notificación a eliminar</param>
        public void EliminarNotificacion(Models.Notificacion notificacion)
        {
            if (notificacion == null)
                throw new ArgumentNullException(nameof(notificacion));

            _notificaciones.Remove(notificacion);
        }

        /// <summary>
        /// Limpia todas las notificaciones
        /// </summary>
        public void LimpiarNotificaciones()
        {
            _notificaciones.Clear();
        }
    }
}
