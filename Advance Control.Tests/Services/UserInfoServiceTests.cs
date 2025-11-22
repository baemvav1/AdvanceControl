using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Services.UserInfo;
using Moq;
using Moq.Protected;
using Xunit;

namespace Advance_Control.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio de información de usuario
    /// </summary>
    public class UserInfoServiceTests
    {
        private readonly Mock<IApiEndpointProvider> _mockEndpointProvider;
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;

        public UserInfoServiceTests()
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
                .Setup(x => x.GetEndpoint("api", "UserInfo", "infoUsuario"))
                .Returns("https://test.api.com/api/UserInfo/infoUsuario");
        }

        [Fact]
        public async Task GetUserInfoAsync_WhenSuccessful_ReturnsUserInfo()
        {
            // Arrange
            var expectedUserInfo = new UserInfoDto
            {
                CredencialId = 1,
                NombreCompleto = "Braulio Emiliano Vazquez Valdez",
                Correo = "baemvav@gmail.com",
                Telefono = "5655139308",
                Nivel = 6,
                TipoUsuario = "Devs"
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expectedUserInfo)
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new UserInfoService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.GetUserInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUserInfo.CredencialId, result.CredencialId);
            Assert.Equal(expectedUserInfo.NombreCompleto, result.NombreCompleto);
            Assert.Equal(expectedUserInfo.Correo, result.Correo);
            Assert.Equal(expectedUserInfo.Telefono, result.Telefono);
            Assert.Equal(expectedUserInfo.Nivel, result.Nivel);
            Assert.Equal(expectedUserInfo.TipoUsuario, result.TipoUsuario);
        }

        [Fact]
        public async Task GetUserInfoAsync_WhenUnauthorized_ReturnsNull()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Unauthorized")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var service = new UserInfoService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.GetUserInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserInfoAsync_WhenServerError_ReturnsNull()
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

            var service = new UserInfoService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.GetUserInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserInfoAsync_WhenNetworkError_ReturnsNull()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var service = new UserInfoService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            var result = await service.GetUserInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserInfoAsync_CallsCorrectEndpoint()
        {
            // Arrange
            var expectedUserInfo = new UserInfoDto
            {
                CredencialId = 1,
                NombreCompleto = "Test User",
                TipoUsuario = "Admin"
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expectedUserInfo)
            };

            string? capturedUri = null;
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => 
                {
                    capturedUri = request.RequestUri?.ToString();
                })
                .ReturnsAsync(responseMessage);

            var service = new UserInfoService(_httpClient, _mockEndpointProvider.Object, _mockLogger.Object);

            // Act
            await service.GetUserInfoAsync();

            // Assert
            Assert.NotNull(capturedUri);
            Assert.Contains("UserInfo/infoUsuario", capturedUri);
        }
    }
}
