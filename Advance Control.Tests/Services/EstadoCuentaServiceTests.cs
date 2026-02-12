using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.EstadoCuenta;
using Advance_Control.Services.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Advance_Control.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio de estados de cuenta
    /// </summary>
    public class EstadoCuentaServiceTests
    {
        private readonly Mock<IApiEndpointProvider> _mockEndpointProvider;
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;

        public EstadoCuentaServiceTests()
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
                .Setup(x => x.GetEndpoint("api", "estadocuenta"))
                .Returns("https://test.api.com/api/estadocuenta");

            _mockEndpointProvider
                .Setup(x => x.GetEndpoint("api", "estadocuenta", It.IsAny<string>(), "depositos"))
                .Returns<string, string, string, string>((a, b, id, d) => $"https://test.api.com/api/estadocuenta/{id}/depositos");

            _mockEndpointProvider
                .Setup(x => x.GetEndpoint("api", "estadocuenta", It.IsAny<string>(), "resumen"))
                .Returns<string, string, string, string>((a, b, id, d) => $"https://test.api.com/api/estadocuenta/{id}/resumen");

            _mockEndpointProvider
                .Setup(x => x.GetEndpoint("api", "estadocuenta", It.IsAny<string>(), "verificar-deposito"))
                .Returns<string, string, string, string>((a, b, id, d) => $"https://test.api.com/api/estadocuenta/{id}/verificar-deposito");
        }

        #region GetEstadosCuentaAsync Tests

        [Fact]
        public async Task GetEstadosCuentaAsync_WhenSuccessful_ReturnsEstadosCuenta()
        {
            // Arrange
            var estados = new List<EstadoCuentaDto>
            {
                new EstadoCuentaDto
                {
                    EstadoCuentaID = 1,
                    FechaCorte = new DateTime(2026, 1, 31),
                    PeriodoDesde = new DateTime(2026, 1, 1),
                    PeriodoHasta = new DateTime(2026, 1, 31),
                    SaldoInicial = 1000.00m,
                    SaldoCorte = 1500.00m,
                    TotalDepositos = 800.00m,
                    TotalRetiros = 300.00m,
                    Comisiones = 0.00m,
                    NombreArchivo = "estado_enero_2026.pdf"
                }
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(estados))
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains("/api/estadocuenta")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.GetEstadosCuentaAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].EstadoCuentaID);
            Assert.Equal(1000.00m, result[0].SaldoInicial);
        }

        [Fact]
        public async Task GetEstadosCuentaAsync_WhenServerError_ReturnsEmptyList()
        {
            // Arrange
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

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.GetEstadosCuentaAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetEstadosCuentaAsync_WhenNetworkError_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetEstadosCuentaAsync());
        }

        #endregion

        #region CreateEstadoCuentaAsync Tests

        [Fact]
        public async Task CreateEstadoCuentaAsync_WhenSuccessful_ReturnsResponse()
        {
            // Arrange
            var response = new EstadoCuentaOperationResponse { Id = 5, Message = "Estado de cuenta creado exitosamente" };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains("/api/estadocuenta")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.CreateEstadoCuentaAsync(
                fechaCorte: new DateTime(2026, 1, 31),
                periodoDesde: new DateTime(2026, 1, 1),
                periodoHasta: new DateTime(2026, 1, 31),
                saldoInicial: 1000m,
                saldoCorte: 1500m,
                totalDepositos: 800m,
                totalRetiros: 300m,
                comisiones: 50m,
                nombreArchivo: "estado_enero.pdf");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task CreateEstadoCuentaAsync_CallsCorrectEndpointWithParameters()
        {
            // Arrange
            var response = new EstadoCuentaOperationResponse { Id = 1, Message = "Success" };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
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

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            await service.CreateEstadoCuentaAsync(
                fechaCorte: new DateTime(2026, 1, 31),
                periodoDesde: new DateTime(2026, 1, 1),
                periodoHasta: new DateTime(2026, 1, 31),
                saldoInicial: 1000m,
                saldoCorte: 1500m,
                totalDepositos: 800m,
                totalRetiros: 300m);

            // Assert
            Assert.NotNull(capturedUri);
            Assert.Contains("fechaCorte=2026-01-31", capturedUri);
            Assert.Contains("periodoDesde=2026-01-01", capturedUri);
            Assert.Contains("periodoHasta=2026-01-31", capturedUri);
            Assert.Contains("saldoInicial=1000", capturedUri);
            Assert.Contains("saldoCorte=1500", capturedUri);
            Assert.Contains("totalDepositos=800", capturedUri);
            Assert.Contains("totalRetiros=300", capturedUri);
            Assert.Equal(HttpMethod.Post, capturedMethod);
        }

        #endregion

        #region GetDepositosAsync Tests

        [Fact]
        public async Task GetDepositosAsync_WhenSuccessful_ReturnsDepositos()
        {
            // Arrange
            var depositos = new List<DepositoDto>
            {
                new DepositoDto
                {
                    DepositoID = 1,
                    EstadoCuentaID = 5,
                    FechaDeposito = new DateTime(2026, 1, 15),
                    Descripcion = "Depósito por transferencia",
                    Monto = 500.00m,
                    Tipo = "Transferencia"
                }
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(depositos))
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains("/5/depositos")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.GetDepositosAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].DepositoID);
            Assert.Equal(500.00m, result[0].Monto);
        }

        [Fact]
        public async Task GetDepositosAsync_WhenIdIsZero_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetDepositosAsync(0));
        }

        [Fact]
        public async Task GetDepositosAsync_WhenIdIsNegative_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetDepositosAsync(-1));
        }

        #endregion

        #region AddDepositoAsync Tests

        [Fact]
        public async Task AddDepositoAsync_WhenSuccessful_ReturnsResponse()
        {
            // Arrange
            var response = new EstadoCuentaOperationResponse { Id = 12, Message = "Depósito agregado exitosamente" };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains("/5/depositos")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.AddDepositoAsync(
                estadoCuentaId: 5,
                fechaDeposito: new DateTime(2026, 1, 25),
                descripcionDeposito: "Pago cliente",
                montoDeposito: 1000.50m,
                tipoDeposito: "Transferencia");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(12, result.Id);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddDepositoAsync_WhenIdIsZero_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddDepositoAsync(0, DateTime.Now, "Test", 100m, "Transferencia"));
        }

        [Fact]
        public async Task AddDepositoAsync_WhenDescripcionIsEmpty_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddDepositoAsync(1, DateTime.Now, "", 100m, "Transferencia"));
        }

        [Fact]
        public async Task AddDepositoAsync_WhenMontoIsZero_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddDepositoAsync(1, DateTime.Now, "Test", 0m, "Transferencia"));
        }

        [Fact]
        public async Task AddDepositoAsync_WhenTipoIsEmpty_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddDepositoAsync(1, DateTime.Now, "Test", 100m, ""));
        }

        #endregion

        #region GetResumenDepositosAsync Tests

        [Fact]
        public async Task GetResumenDepositosAsync_WhenSuccessful_ReturnsResumen()
        {
            // Arrange
            var resumen = new List<ResumenDepositoDto>
            {
                new ResumenDepositoDto
                {
                    Tipo = "Transferencia",
                    CantidadDepositos = 5,
                    TotalMonto = 2500.00m
                },
                new ResumenDepositoDto
                {
                    Tipo = "Efectivo",
                    CantidadDepositos = 3,
                    TotalMonto = 1200.00m
                }
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(resumen))
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains("/5/resumen")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.GetResumenDepositosAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Transferencia", result[0].Tipo);
            Assert.Equal(5, result[0].CantidadDepositos);
            Assert.Equal(2500.00m, result[0].TotalMonto);
        }

        [Fact]
        public async Task GetResumenDepositosAsync_WhenIdIsZero_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetResumenDepositosAsync(0));
        }

        #endregion

        #region VerificarDepositoAsync Tests

        [Fact]
        public async Task VerificarDepositoAsync_WhenDepositoExists_ReturnsExisteTrue()
        {
            // Arrange
            var verificacion = new DepositoVerificacionDto
            {
                Existe = true,
                DepositoID = 12,
                Mensaje = "Depósito encontrado"
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(verificacion))
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains("/5/verificar-deposito")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.VerificarDepositoAsync(
                estadoCuentaId: 5,
                fechaDeposito: new DateTime(2026, 1, 25),
                descripcionDeposito: "Pago cliente",
                montoDeposito: 1000.50m);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Existe);
            Assert.Equal(12, result.DepositoID);
        }

        [Fact]
        public async Task VerificarDepositoAsync_WhenDepositoNotExists_ReturnsExisteFalse()
        {
            // Arrange
            var verificacion = new DepositoVerificacionDto
            {
                Existe = false,
                DepositoID = null,
                Mensaje = "Depósito no encontrado"
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(verificacion))
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.VerificarDepositoAsync(
                estadoCuentaId: 5,
                fechaDeposito: new DateTime(2026, 1, 25),
                descripcionDeposito: "Pago inexistente",
                montoDeposito: 999.99m);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Existe);
            Assert.Null(result.DepositoID);
        }

        [Fact]
        public async Task VerificarDepositoAsync_WhenIdIsZero_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.VerificarDepositoAsync(0, DateTime.Now, "Test", 100m));
        }

        [Fact]
        public async Task VerificarDepositoAsync_WhenDescripcionIsEmpty_ThrowsArgumentException()
        {
            // Arrange
            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.VerificarDepositoAsync(1, DateTime.Now, "", 100m));
        }

        #endregion

        #region CancellationToken Tests

        [Fact]
        public async Task GetEstadosCuentaAsync_WithCancellationToken_PropagatesCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException());

            var service = new EstadoCuentaService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                service.GetEstadosCuentaAsync(cts.Token));
        }

        #endregion
    }
}
