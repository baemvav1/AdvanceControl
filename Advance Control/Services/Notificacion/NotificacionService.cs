using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;

namespace Advance_Control.Services.Notificacion
{
    /// <summary>
    /// Implementación mock del servicio de notificaciones.
    /// En el futuro se conectará a un endpoint real.
    /// </summary>
    public class NotificacionService : INotificacionService
    {
        private readonly ILoggingService _logger;
        private readonly ObservableCollection<NotificacionDto> _notificaciones;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _timers;

        /// <summary>
        /// Evento que se dispara cuando se agrega una nueva notificación.
        /// </summary>
        public event EventHandler<NotificacionDto>? NotificacionAgregada;

        public NotificacionService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificaciones = new ObservableCollection<NotificacionDto>();
            _timers = new ConcurrentDictionary<Guid, CancellationTokenSource>();
        }

        /// <summary>
        /// Muestra una notificación en el sistema.
        /// </summary>
        public async Task<NotificacionDto> MostrarNotificacionAsync(
            string titulo, 
            string? nota = null, 
            DateTime? fechaHoraInicio = null, 
            DateTime? fechaHoraFinal = null,
            int? tiempoDeVidaSegundos = null)
        {
            if (string.IsNullOrWhiteSpace(titulo))
            {
                throw new ArgumentException("El título es requerido.", nameof(titulo));
            }

            var notificacion = new NotificacionDto
            {
                Titulo = titulo,
                Nota = nota,
                FechaHoraInicio = fechaHoraInicio,
                FechaHoraFinal = fechaHoraFinal,
                FechaCreacion = DateTime.Now,
                TiempoDeVidaSegundos = tiempoDeVidaSegundos
            };

            // Agregar a la colección
            _notificaciones.Add(notificacion);

            // Registrar en el log
            await _logger.LogInformationAsync(
                $"Notificación creada: {titulo}", 
                "NotificacionService", 
                "MostrarNotificacionAsync");

            // Disparar evento
            NotificacionAgregada?.Invoke(this, notificacion);

            // Si tiene tiempo de vida, programar auto-eliminación
            if (tiempoDeVidaSegundos.HasValue && tiempoDeVidaSegundos.Value > 0)
            {
                var cts = new CancellationTokenSource();
                _timers[notificacion.Id] = cts;
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(tiempoDeVidaSegundos.Value), cts.Token);
                        if (!cts.Token.IsCancellationRequested)
                        {
                            EliminarNotificacion(notificacion.Id);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Timer cancelado, no hacer nada
                    }
                    catch (Exception ex)
                    {
                        // Loguear cualquier otro error inesperado
                        _ = _logger.LogErrorAsync($"Error inesperado en auto-eliminación de notificación: {notificacion.Titulo}", ex, "NotificacionService", "MostrarNotificacionAsync");
                    }
                });
            }

            return notificacion;
        }

        /// <summary>
        /// Obtiene todas las notificaciones.
        /// </summary>
        public IEnumerable<NotificacionDto> ObtenerNotificaciones()
        {
            return _notificaciones.ToList();
        }

        /// <summary>
        /// Limpia todas las notificaciones.
        /// </summary>
        public void LimpiarNotificaciones()
        {
            _notificaciones.Clear();
            _logger.LogInformationAsync(
                "Todas las notificaciones han sido limpiadas", 
                "NotificacionService", 
                "LimpiarNotificaciones");
        }

        /// <summary>
        /// Elimina una notificación específica por su ID.
        /// </summary>
        public bool EliminarNotificacion(Guid id)
        {
            var notificacion = _notificaciones.FirstOrDefault(n => n.Id == id);
            if (notificacion != null)
            {
                // Cancelar el timer si existe
                if (_timers.TryGetValue(id, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _timers.Remove(id);
                }

                _notificaciones.Remove(notificacion);
                _logger.LogInformationAsync(
                    $"Notificación eliminada: {notificacion.Titulo}", 
                    "NotificacionService", 
                    "EliminarNotificacion");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Obtiene la colección observable de notificaciones.
        /// Para uso interno en ViewModels que necesitan binding.
        /// </summary>
        public ObservableCollection<NotificacionDto> NotificacionesObservable => _notificaciones;
    }
}
