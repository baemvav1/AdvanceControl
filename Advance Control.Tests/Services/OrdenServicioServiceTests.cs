using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Services.OrdenServicio;
using Moq;
using Moq.Protected;
using Xunit;

namespace Advance_Control.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio de órdenes de servicio
    /// </summary>
    public class OrdenServicioServiceTests
    {
        private readonly Mock<IApiEndpointProvider> _mockEndpointProvider;
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;

        public OrdenServicioServiceTests()
        {
            _mockEndpointProvider = new Mock<IApiEndpointProvider>();
            _mockLogger = new Mock<ILoggingService>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.api.com")
            };

            // Setup default endpoint provider behavior
            _mockEndpointProvider
                .Setup(x => x.GetEndpoint("api", "OrdenServicio"))
                .Returns("https://test.api.com/api/OrdenServicio");
        }

        [Fact]
        public async Task UpdateAtendidoAsync_WhenSuccessful_ReturnsTrue()
        {
            // Arrange
            int idOrdenServicio = 1;
            int idAtendio = 5;

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"message\": \"Estado actualizado exitosamente\"}")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Patch &&
                        req.RequestUri!.ToString().Contains("/atendido") &&
                        req.RequestUri.ToString().Contains($"idOrdenServicio={idOrdenServicio}") &&
                        req.RequestUri.ToString().Contains($"idAtendio={idAtendio}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new OrdenServicioService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.UpdateAtendidoAsync(idOrdenServicio, idAtendio);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAtendidoAsync_WhenServerError_ReturnsFalse()
        {
            // Arrange
            int idOrdenServicio = 1;
            int idAtendio = 5;

            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Internal Server Error")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new OrdenServicioService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.UpdateAtendidoAsync(idOrdenServicio, idAtendio);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAtendidoAsync_WhenBadRequest_ReturnsFalse()
        {
            // Arrange
            int idOrdenServicio = -1;  // Invalid ID (negative values are not allowed by API)
            int idAtendio = 5;

            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"message\": \"El campo 'idOrdenServicio' debe ser mayor que 0.\"}")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new OrdenServicioService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.UpdateAtendidoAsync(idOrdenServicio, idAtendio);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAtendidoAsync_WhenNetworkError_ThrowsInvalidOperationException()
        {
            // Arrange
            int idOrdenServicio = 1;
            int idAtendio = 5;

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var service = new OrdenServicioService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.UpdateAtendidoAsync(idOrdenServicio, idAtendio));
        }

        [Fact]
        public async Task UpdateAtendidoAsync_CallsCorrectEndpoint()
        {
            // Arrange
            int idOrdenServicio = 123;
            int idAtendio = 456;

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"message\": \"Success\"}")
            };

            string? capturedUri = null;
            HttpMethod? capturedMethod = null;

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => 
                {
                    capturedUri = request.RequestUri?.ToString();
                    capturedMethod = request.Method;
                })
                .ReturnsAsync(responseMessage);

            var service = new OrdenServicioService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            await service.UpdateAtendidoAsync(idOrdenServicio, idAtendio);

            // Assert
            Assert.NotNull(capturedUri);
            Assert.Contains("/atendido", capturedUri);
            Assert.Contains($"idOrdenServicio={idOrdenServicio}", capturedUri);
            Assert.Contains($"idAtendio={idAtendio}", capturedUri);
            Assert.Equal(HttpMethod.Patch, capturedMethod);
        }

        [Fact]
        public async Task UpdateAtendidoAsync_WithCancellationToken_PropagatesCancellation()
        {
            // Arrange
            int idOrdenServicio = 1;
            int idAtendio = 5;
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException());

            var service = new OrdenServicioService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => 
                service.UpdateAtendidoAsync(idOrdenServicio, idAtendio, cts.Token));
        }
    }
}
