using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Auth;
using Advance_Control.Services.EndPointProvider;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Security;
using Moq;
using Moq.Protected;
using Xunit;

namespace Advance_Control.Tests.Services
{
    /// <summary>
    /// Pruebas unitarias para el servicio de autenticación
    /// </summary>
    public class AuthServiceTests
    {
        private readonly Mock<IApiEndpointProvider> _mockEndpointProvider;
        private readonly Mock<ISecureStorage> _mockSecureStorage;
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;

        public AuthServiceTests()
        {
            _mockEndpointProvider = new Mock<IApiEndpointProvider>();
            _mockSecureStorage = new Mock<ISecureStorage>();
            _mockLogger = new Mock<ILoggingService>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.api.com")
            };

            // Setup default endpoint provider behavior
            _mockEndpointProvider
                .Setup(x => x.GetEndpoint(It.IsAny<string[]>()))
                .Returns("https://test.api.com/api/Auth/login");
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ReturnsTrue()
        {
            // Arrange
            var username = "testuser";
            var password = "testpass123";
            
            var loginResponse = new
            {
                accessToken = "test-access-token",
                refreshToken = "test-refresh-token",
                expiresIn = 3600,
                tokenType = "Bearer"
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(loginResponse)
                });

            _mockSecureStorage
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            // Act
            var result = await authService.AuthenticateAsync(username, password);

            // Assert
            Assert.True(result);
            Assert.True(authService.IsAuthenticated);
            _mockSecureStorage.Verify(x => x.SetAsync("auth.access_token", "test-access-token"), Times.Once);
            _mockSecureStorage.Verify(x => x.SetAsync("auth.refresh_token", "test-refresh-token"), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyUsername_ReturnsFalse()
        {
            // Arrange
            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            // Act
            var result = await authService.AuthenticateAsync("", "password");

            // Assert
            Assert.False(result);
            Assert.False(authService.IsAuthenticated);
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyPassword_ReturnsFalse()
        {
            // Arrange
            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            // Act
            var result = await authService.AuthenticateAsync("username", "");

            // Assert
            Assert.False(result);
            Assert.False(authService.IsAuthenticated);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidCredentials_ReturnsFalse()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized
                });

            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            // Act
            var result = await authService.AuthenticateAsync("baduser", "badpass");

            // Assert
            Assert.False(result);
            Assert.False(authService.IsAuthenticated);
        }

        [Fact]
        public async Task GetAccessTokenAsync_WithValidToken_ReturnsToken()
        {
            // Arrange
            var loginResponse = new
            {
                accessToken = "test-access-token",
                refreshToken = "test-refresh-token",
                expiresIn = 3600,
                tokenType = "Bearer"
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(loginResponse)
                });

            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            await authService.AuthenticateAsync("testuser", "testpass");

            // Act
            var token = await authService.GetAccessTokenAsync();

            // Assert
            Assert.NotNull(token);
            Assert.Equal("test-access-token", token);
        }

        [Fact]
        public async Task ClearTokenAsync_RemovesTokens()
        {
            // Arrange
            var loginResponse = new
            {
                accessToken = "test-access-token",
                refreshToken = "test-refresh-token",
                expiresIn = 3600,
                tokenType = "Bearer"
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(loginResponse)
                });

            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            _mockSecureStorage
                .Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            await authService.AuthenticateAsync("testuser", "testpass");

            // Act
            await authService.ClearTokenAsync();

            // Assert
            Assert.False(authService.IsAuthenticated);
            _mockSecureStorage.Verify(x => x.RemoveAsync("auth.access_token"), Times.Once);
            _mockSecureStorage.Verify(x => x.RemoveAsync("auth.refresh_token"), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidRefreshToken_ReturnsTrue()
        {
            // Arrange
            var loginResponse = new
            {
                accessToken = "initial-access-token",
                refreshToken = "test-refresh-token",
                expiresIn = 3600,
                tokenType = "Bearer"
            };

            var refreshResponse = new
            {
                accessToken = "new-access-token",
                refreshToken = "new-refresh-token",
                expiresIn = 3600
            };

            var sequence = 0;
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    sequence++;
                    if (sequence == 1)
                    {
                        // First call is login
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = JsonContent.Create(loginResponse)
                        };
                    }
                    else
                    {
                        // Second call is refresh
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = JsonContent.Create(refreshResponse)
                        };
                    }
                });

            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            _mockEndpointProvider
                .Setup(x => x.GetEndpoint("api", "Auth", "refresh"))
                .Returns("https://test.api.com/api/Auth/refresh");

            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            await authService.AuthenticateAsync("testuser", "testpass");

            // Act
            var result = await authService.RefreshTokenAsync();

            // Assert
            Assert.True(result);
            Assert.True(authService.IsAuthenticated);
        }

        [Fact]
        public async Task LogoutAsync_WithValidRefreshToken_ReturnsTrue()
        {
            // Arrange
            var loginResponse = new
            {
                accessToken = "test-access-token",
                refreshToken = "test-refresh-token",
                expiresIn = 3600,
                tokenType = "Bearer"
            };

            var sequence = 0;
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    sequence++;
                    if (sequence == 1)
                    {
                        // First call is login
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = JsonContent.Create(loginResponse)
                        };
                    }
                    else
                    {
                        // Second call is logout
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.NoContent
                        };
                    }
                });

            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            _mockSecureStorage
                .Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockEndpointProvider
                .Setup(x => x.GetEndpoint("api", "Auth", "logout"))
                .Returns("https://test.api.com/api/Auth/logout");

            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            await authService.AuthenticateAsync("testuser", "testpass");

            // Act
            var result = await authService.LogoutAsync();

            // Assert
            Assert.True(result);
            Assert.False(authService.IsAuthenticated);
            _mockSecureStorage.Verify(x => x.RemoveAsync("auth.access_token"), Times.Once);
            _mockSecureStorage.Verify(x => x.RemoveAsync("auth.refresh_token"), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_WithoutRefreshToken_ClearsTokensAndReturnsTrue()
        {
            // Arrange
            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            _mockSecureStorage
                .Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            // Act
            var result = await authService.LogoutAsync();

            // Assert
            Assert.True(result);
            Assert.False(authService.IsAuthenticated);
        }

        [Fact]
        public async Task LogoutAsync_WhenServerFails_StillClearsLocalTokens()
        {
            // Arrange
            var loginResponse = new
            {
                accessToken = "test-access-token",
                refreshToken = "test-refresh-token",
                expiresIn = 3600,
                tokenType = "Bearer"
            };

            var sequence = 0;
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    sequence++;
                    if (sequence == 1)
                    {
                        // First call is login
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = JsonContent.Create(loginResponse)
                        };
                    }
                    else
                    {
                        // Second call is logout - server error
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.InternalServerError
                        };
                    }
                });

            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            _mockSecureStorage
                .Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockEndpointProvider
                .Setup(x => x.GetEndpoint("api", "Auth", "logout"))
                .Returns("https://test.api.com/api/Auth/logout");

            var authService = new AuthService(_httpClient, _mockEndpointProvider.Object, 
                _mockSecureStorage.Object, _mockLogger.Object);

            await authService.AuthenticateAsync("testuser", "testpass");

            // Act
            var result = await authService.LogoutAsync();

            // Assert
            Assert.False(result); // Returns false because server failed
            Assert.False(authService.IsAuthenticated); // But still clears local tokens
            _mockSecureStorage.Verify(x => x.RemoveAsync("auth.access_token"), Times.Once);
            _mockSecureStorage.Verify(x => x.RemoveAsync("auth.refresh_token"), Times.Once);
        }
    }
}
