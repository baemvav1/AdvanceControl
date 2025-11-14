using System;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Moq;
using Xunit;

namespace Advance_Control.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio de notificaciones
    /// </summary>
    public class NotificationServiceTests
    {
        private readonly Mock<ILoggingService> _mockLogger;

        public NotificationServiceTests()
        {
            _mockLogger = new Mock<ILoggingService>();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NotificationService(null!));
        }

        [Fact]
        public void Notificaciones_ReturnsEmptyCollection()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);

            // Act
            var notificaciones = service.Notificaciones;

            // Assert
            Assert.NotNull(notificaciones);
            Assert.Empty(notificaciones);
        }

        [Fact]
        public async Task AgregarNotificacionAsync_WithValidTitulo_AddsNotification()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);
            var titulo = "Test Notification";

            // Act
            await service.AgregarNotificacionAsync(titulo);

            // Assert
            Assert.Single(service.Notificaciones);
            Assert.Equal(titulo, service.Notificaciones[0].Titulo);
            Assert.False(service.Notificaciones[0].Leida);
        }

        [Fact]
        public async Task AgregarNotificacionAsync_WithAllParameters_AddsNotificationWithAllData()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);
            var titulo = "Test Notification";
            var nota = "Test Note";
            var fechaInicio = DateTime.Now;
            var fechaFinal = DateTime.Now.AddDays(1);

            // Act
            await service.AgregarNotificacionAsync(titulo, nota, fechaInicio, fechaFinal);

            // Assert
            Assert.Single(service.Notificaciones);
            var notificacion = service.Notificaciones[0];
            Assert.Equal(titulo, notificacion.Titulo);
            Assert.Equal(nota, notificacion.Nota);
            Assert.Equal(fechaInicio, notificacion.FechaHoraInicio);
            Assert.Equal(fechaFinal, notificacion.FechaHoraFinal);
        }

        [Fact]
        public async Task AgregarNotificacionAsync_WithEmptyTitulo_ThrowsArgumentException()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.AgregarNotificacionAsync(string.Empty));
        }

        [Fact]
        public async Task AgregarNotificacionAsync_WithNullTitulo_ThrowsArgumentException()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                service.AgregarNotificacionAsync(null!));
        }

        [Fact]
        public void MarcarComoLeida_WithValidNotification_MarksAsRead()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);
            var notificacion = new Models.Notificacion 
            { 
                Titulo = "Test",
                Leida = false 
            };

            // Act
            service.MarcarComoLeida(notificacion);

            // Assert
            Assert.True(notificacion.Leida);
        }

        [Fact]
        public void MarcarComoLeida_WithNullNotification_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.MarcarComoLeida(null!));
        }

        [Fact]
        public async Task EliminarNotificacion_WithValidNotification_RemovesNotification()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);
            await service.AgregarNotificacionAsync("Test 1");
            await service.AgregarNotificacionAsync("Test 2");
            var notificacionToRemove = service.Notificaciones[0];

            // Act
            service.EliminarNotificacion(notificacionToRemove);

            // Assert
            Assert.Single(service.Notificaciones);
            Assert.DoesNotContain(notificacionToRemove, service.Notificaciones);
        }

        [Fact]
        public void EliminarNotificacion_WithNullNotification_ThrowsArgumentNullException()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.EliminarNotificacion(null!));
        }

        [Fact]
        public async Task LimpiarNotificaciones_RemovesAllNotifications()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);
            await service.AgregarNotificacionAsync("Test 1");
            await service.AgregarNotificacionAsync("Test 2");
            await service.AgregarNotificacionAsync("Test 3");

            // Act
            service.LimpiarNotificaciones();

            // Assert
            Assert.Empty(service.Notificaciones);
        }

        [Fact]
        public async Task AgregarNotificacionAsync_MultipleNotifications_MaintainsOrder()
        {
            // Arrange
            var service = new NotificationService(_mockLogger.Object);

            // Act
            await service.AgregarNotificacionAsync("First");
            await service.AgregarNotificacionAsync("Second");
            await service.AgregarNotificacionAsync("Third");

            // Assert
            Assert.Equal(3, service.Notificaciones.Count);
            Assert.Equal("First", service.Notificaciones[0].Titulo);
            Assert.Equal("Second", service.Notificaciones[1].Titulo);
            Assert.Equal("Third", service.Notificaciones[2].Titulo);
        }
    }
}
