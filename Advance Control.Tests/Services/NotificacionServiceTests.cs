using System;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Moq;
using Xunit;

namespace Advance_Control.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio de notificaciones
    /// </summary>
    public class NotificacionServiceTests
    {
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly NotificacionService _service;

        // Constantes para pruebas de tiempo
        private const int TiempoDeVidaPorDefecto = 3;
        private const double BufferSegundos = 0.5;

        public NotificacionServiceTests()
        {
            _mockLogger = new Mock<ILoggingService>();
            _service = new NotificacionService(_mockLogger.Object);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTituloValido_CreaNotificacion()
        {
            // Arrange
            var titulo = "Test Notificación";
            var nota = "Esta es una nota de prueba";
            var fechaInicio = DateTime.Now;
            var fechaFinal = DateTime.Now.AddHours(1);

            // Act
            var resultado = await _service.MostrarNotificacionAsync(titulo, nota, fechaInicio, fechaFinal);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(titulo, resultado.Titulo);
            Assert.Equal(nota, resultado.Nota);
            Assert.Equal(fechaInicio, resultado.FechaHoraInicio);
            Assert.Equal(fechaFinal, resultado.FechaHoraFinal);
            Assert.NotEqual(Guid.Empty, resultado.Id);
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
        public async Task MostrarNotificacionAsync_ConParametrosOpcionales_CreaNotificacion()
        {
            // Arrange
            var titulo = "Solo Titulo";

            // Act
            var resultado = await _service.MostrarNotificacionAsync(titulo);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(titulo, resultado.Titulo);
            Assert.Null(resultado.Nota);
            Assert.Null(resultado.FechaHoraInicio);
            Assert.Null(resultado.FechaHoraFinal);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_AgregaNotificacionAColeccion()
        {
            // Arrange
            var titulo = "Notificación Test";

            // Act
            await _service.MostrarNotificacionAsync(titulo);
            var notificaciones = _service.ObtenerNotificaciones().ToList();

            // Assert
            Assert.Single(notificaciones);
            Assert.Equal(titulo, notificaciones[0].Titulo);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_DisparaEvento()
        {
            // Arrange
            var titulo = "Notificación con Evento";
            NotificacionDto? notificacionRecibida = null;

            _service.NotificacionAgregada += (sender, notif) =>
            {
                notificacionRecibida = notif;
            };

            // Act
            await _service.MostrarNotificacionAsync(titulo);

            // Assert
            Assert.NotNull(notificacionRecibida);
            Assert.Equal(titulo, notificacionRecibida.Titulo);
        }

        [Fact]
        public void ObtenerNotificaciones_RetornaListaVaciaPorDefecto()
        {
            // Act
            var notificaciones = _service.ObtenerNotificaciones().ToList();

            // Assert
            Assert.Empty(notificaciones);
        }

        [Fact]
        public async Task ObtenerNotificaciones_RetornaTodasLasNotificaciones()
        {
            // Arrange
            await _service.MostrarNotificacionAsync("Notificación 1");
            await _service.MostrarNotificacionAsync("Notificación 2");
            await _service.MostrarNotificacionAsync("Notificación 3");

            // Act
            var notificaciones = _service.ObtenerNotificaciones().ToList();

            // Assert
            Assert.Equal(3, notificaciones.Count);
        }

        [Fact]
        public async Task EliminarNotificacion_ConIdValido_RetornaTrue()
        {
            // Arrange
            var notificacion = await _service.MostrarNotificacionAsync("Test");
            var id = notificacion.Id;

            // Act
            var resultado = _service.EliminarNotificacion(id);

            // Assert
            Assert.True(resultado);
            Assert.Empty(_service.ObtenerNotificaciones());
        }

        [Fact]
        public void EliminarNotificacion_ConIdInvalido_RetornaFalse()
        {
            // Arrange
            var idInexistente = Guid.NewGuid();

            // Act
            var resultado = _service.EliminarNotificacion(idInexistente);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task LimpiarNotificaciones_EliminaTodasLasNotificaciones()
        {
            // Arrange
            await _service.MostrarNotificacionAsync("Notificación 1");
            await _service.MostrarNotificacionAsync("Notificación 2");
            await _service.MostrarNotificacionAsync("Notificación 3");

            // Act
            _service.LimpiarNotificaciones();

            // Assert
            Assert.Empty(_service.ObtenerNotificaciones());
        }

        [Fact]
        public async Task NotificacionesObservable_EsColeccionObservable()
        {
            // Arrange & Act
            var coleccion = _service.NotificacionesObservable;
            await _service.MostrarNotificacionAsync("Test");

            // Assert
            Assert.NotNull(coleccion);
            Assert.Single(coleccion);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_RegistraEnLogger()
        {
            // Arrange
            var titulo = "Test Logging";

            // Act
            await _service.MostrarNotificacionAsync(titulo);

            // Assert
            _mockLogger.Verify(
                x => x.LogInformationAsync(
                    It.Is<string>(msg => msg.Contains(titulo)),
                    "NotificacionService",
                    "MostrarNotificacionAsync"),
                Times.Once);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTiempoDeVida_CreaNotificacionConTiempoDeVida()
        {
            // Arrange
            var titulo = "Notificación temporal";
            var tiempoDeVida = 5;

            // Act
            var resultado = await _service.MostrarNotificacionAsync(titulo, tiempoDeVidaSegundos: tiempoDeVida);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(titulo, resultado.Titulo);
            Assert.Equal(tiempoDeVida, resultado.TiempoDeVidaSegundos);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_SinTiempoDeVida_AsignaTresSegundosPorDefecto()
        {
            // Arrange
            var titulo = "Notificación normal";

            // Act
            var resultado = await _service.MostrarNotificacionAsync(titulo);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(TiempoDeVidaPorDefecto, resultado.TiempoDeVidaSegundos);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTituloError_NoAsignaTiempoDeVida()
        {
            // Arrange
            var titulo = "Error al procesar";

            // Act
            var resultado = await _service.MostrarNotificacionAsync(titulo);

            // Assert
            Assert.NotNull(resultado);
            Assert.Null(resultado.TiempoDeVidaSegundos);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTituloValidacion_NoAsignaTiempoDeVida()
        {
            // Arrange
            var titulo = "Validación";

            // Act
            var resultado = await _service.MostrarNotificacionAsync(titulo);

            // Assert
            Assert.NotNull(resultado);
            Assert.Null(resultado.TiempoDeVidaSegundos);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTituloErrorMayuscula_NoAsignaTiempoDeVida()
        {
            // Arrange
            var titulo = "ERROR DE SISTEMA";

            // Act
            var resultado = await _service.MostrarNotificacionAsync(titulo);

            // Assert
            Assert.NotNull(resultado);
            Assert.Null(resultado.TiempoDeVidaSegundos);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTiempoDeVidaExplicito_UsaTiempoEspecificado()
        {
            // Arrange
            var titulo = "Notificación con tiempo específico";
            var tiempoDeVida = 10;

            // Act
            var resultado = await _service.MostrarNotificacionAsync(titulo, tiempoDeVidaSegundos: tiempoDeVida);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(tiempoDeVida, resultado.TiempoDeVidaSegundos);
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTiempoDeVida_SeEliminaAutomaticamente()
        {
            // Arrange
            var titulo = "Notificación auto-eliminable";
            var tiempoDeVida = 1; // 1 segundo

            // Act
            await _service.MostrarNotificacionAsync(titulo, tiempoDeVidaSegundos: tiempoDeVida);
            Assert.Single(_service.ObtenerNotificaciones());

            // Wait for auto-deletion
            await Task.Delay(TimeSpan.FromSeconds(tiempoDeVida + 0.5));

            // Assert
            Assert.Empty(_service.ObtenerNotificaciones());
        }

        [Fact]
        public async Task EliminarNotificacion_ConNotificacionTemporal_CancelaTimerYEliminaNotificacion()
        {
            // Arrange
            var titulo = "Notificación temporal a eliminar";
            var tiempoDeVida = 5; // 5 segundos
            var notificacion = await _service.MostrarNotificacionAsync(titulo, tiempoDeVidaSegundos: tiempoDeVida);

            // Act
            var resultado = _service.EliminarNotificacion(notificacion.Id);

            // Assert
            Assert.True(resultado);
            Assert.Empty(_service.ObtenerNotificaciones());

            // Verificar que el timer fue cancelado esperando más tiempo del configurado
            await Task.Delay(TimeSpan.FromSeconds(tiempoDeVida + 1));
            Assert.Empty(_service.ObtenerNotificaciones()); // Sigue vacío, no se agregó de nuevo
        }

        [Fact]
        public async Task MostrarNotificacionAsync_ConTiempoDeVidaCero_NoSeEliminaAutomaticamente()
        {
            // Arrange
            var titulo = "Notificación con tiempo cero";
            var tiempoDeVida = 0;

            // Act
            await _service.MostrarNotificacionAsync(titulo, tiempoDeVidaSegundos: tiempoDeVida);
            await Task.Delay(TimeSpan.FromSeconds(1));

            // Assert
            Assert.Single(_service.ObtenerNotificaciones());
        }

        [Fact]
        public async Task MostrarNotificacionAsync_NotificacionNormal_SeEliminaEn3Segundos()
        {
            // Arrange
            var titulo = "Notificación de éxito";

            // Act
            await _service.MostrarNotificacionAsync(titulo);
            Assert.Single(_service.ObtenerNotificaciones());

            // Wait for auto-deletion (default time + buffer)
            await Task.Delay(TimeSpan.FromSeconds(TiempoDeVidaPorDefecto + BufferSegundos));

            // Assert
            Assert.Empty(_service.ObtenerNotificaciones());
        }

        [Fact]
        public async Task MostrarNotificacionAsync_NotificacionError_NoCaducaNunca()
        {
            // Arrange
            var titulo = "Error al guardar";

            // Act
            await _service.MostrarNotificacionAsync(titulo);
            Assert.Single(_service.ObtenerNotificaciones());

            // Wait longer than default timeout to verify it doesn't auto-delete
            await Task.Delay(TimeSpan.FromSeconds(TiempoDeVidaPorDefecto + 1));

            // Assert
            Assert.Single(_service.ObtenerNotificaciones());
        }
    }
}
