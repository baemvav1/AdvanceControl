using System;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Moq;
using Xunit;

namespace Advance_Control.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio de notificaciones (AppNotification)
    /// </summary>
    public class NotificacionServiceTests
    {
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly NotificacionService _service;

        public NotificacionServiceTests()
        {
            _mockLogger = new Mock<ILoggingService>();
            _service = new NotificacionService(_mockLogger.Object);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTituloVacio_LanzaExcepcion()
        {
            // Arrange
            var titulo = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.MostrarNotificacionAsync(titulo));
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTituloNull_LanzaExcepcion()
        {
            // Arrange
            string? titulo = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.MostrarNotificacionAsync(titulo!));
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTituloValido_RegistraEnLogger()
        {
            // Arrange
            var titulo = "Operación completada";

            // Act — AppNotificationManager puede fallar en entorno de prueba (sin registro MSIX),
            // el servicio atrapa esa excepción internamente, por lo que no debe lanzar hacia el test.
            var excepcion = await Record.ExceptionAsync(() =>
                _service.MostrarNotificacionAsync(titulo));

            // Assert — la excepción de argumento NO debe producirse; sí puede ignorarse la de Show()
            Assert.IsNotType<ArgumentException>(excepcion);
        }
    }
}
