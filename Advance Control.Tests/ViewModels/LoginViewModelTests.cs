using System;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Auth;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Security;
using Advance_Control.Services.Session;
using Advance_Control.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Moq;
using Xunit;

namespace Advance_Control.Tests.ViewModels
{
    public class LoginViewModelTests
    {
        private readonly Mock<IAuthService> _mockAuthService = new();
        private readonly Mock<ILoggingService> _mockLogger = new();
        private readonly Mock<INotificacionService> _mockNotificationService = new();
        private readonly Mock<ISecureStorage> _mockSecureStorage = new();
        private readonly Mock<IUserSessionService> _mockUserSessionService = new();
        private readonly Mock<IActivityService> _mockActivityService = new();

        public LoginViewModelTests()
        {
            _mockSecureStorage.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
            _mockSecureStorage.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockSecureStorage.Setup(x => x.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockSecureStorage.Setup(x => x.ClearAsync()).Returns(Task.CompletedTask);
            _mockNotificationService
                .Setup(x => x.MostrarNotificacionAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int?>(),
                    It.IsAny<System.Collections.Generic.Dictionary<string, string>?>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public void Constructor_WithNullAuthService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new LoginViewModel(null!, _mockLogger.Object, _mockNotificationService.Object, _mockSecureStorage.Object, _mockUserSessionService.Object, _mockActivityService.Object));
        }

        [Fact]
        public void CanLogin_WithValidCredentials_ReturnsTrue()
        {
            var viewModel = CreateViewModel();
            viewModel.User = "demo";
            viewModel.Password = "1234";

            Assert.True(viewModel.CanLogin);
        }

        [Fact]
        public void CanLogin_WithMissingPassword_ReturnsFalse()
        {
            var viewModel = CreateViewModel();
            viewModel.User = "demo";
            viewModel.Password = string.Empty;

            Assert.False(viewModel.CanLogin);
        }

        [Fact]
        public void ClearForm_ResetsAllFields()
        {
            var viewModel = CreateViewModel();
            viewModel.User = "demo";
            viewModel.Password = "1234";
            viewModel.ErrorMessage = "error";

            viewModel.ClearForm();

            Assert.Equal(string.Empty, viewModel.User);
            Assert.Equal(string.Empty, viewModel.Password);
            Assert.Equal(string.Empty, viewModel.ErrorMessage);
        }

        [Fact]
        public void RefreshAuthenticationState_UpdatesIsAuthenticated()
        {
            _mockAuthService.SetupGet(x => x.IsAuthenticated).Returns(true);
            var viewModel = CreateViewModel();

            viewModel.RefreshAuthenticationState();

            Assert.True(viewModel.IsAuthenticated);
            Assert.False(viewModel.IsNotAuthenticated);
        }

        [Fact]
        public async Task LoginCommand_WithValidCredentialsAndSuccess_MarksLoginAsSuccessful()
        {
            _mockAuthService
                .Setup(x => x.AuthenticateAsync("demo", "1234", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var viewModel = CreateViewModel();
            viewModel.User = "demo";
            viewModel.Password = "1234";

            await ((IAsyncRelayCommand)viewModel.LoginCommand).ExecuteAsync(null);

            Assert.True(viewModel.LoginSuccessful);
            Assert.True(viewModel.IsAuthenticated);
            Assert.Equal(string.Empty, viewModel.ErrorMessage);
            _mockActivityService.Verify(x => x.CrearActividadAsync("Sesion", It.Is<string>(s => s.Contains("demo")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginCommand_WhenAuthenticationFails_SetsErrorMessage()
        {
            _mockAuthService
                .Setup(x => x.AuthenticateAsync("demo", "1234", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var viewModel = CreateViewModel();
            viewModel.User = "demo";
            viewModel.Password = "1234";

            await ((IAsyncRelayCommand)viewModel.LoginCommand).ExecuteAsync(null);

            Assert.False(viewModel.LoginSuccessful);
            Assert.Contains("incorrectos", viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LogoutCommand_WhenAuthenticated_ClearsSessionState()
        {
            _mockAuthService.SetupGet(x => x.IsAuthenticated).Returns(true);
            _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var viewModel = CreateViewModel();
            viewModel.RefreshAuthenticationState();
            viewModel.User = "demo";

            await ((IAsyncRelayCommand)viewModel.LogoutCommand).ExecuteAsync(null);

            Assert.False(viewModel.IsAuthenticated);
            Assert.False(viewModel.LoginSuccessful);
            Assert.Equal(string.Empty, viewModel.User);
            _mockUserSessionService.Verify(x => x.Clear(), Times.Once);
        }

        private LoginViewModel CreateViewModel()
            => new(
                _mockAuthService.Object,
                _mockLogger.Object,
                _mockNotificationService.Object,
                _mockSecureStorage.Object,
                _mockUserSessionService.Object,
                _mockActivityService.Object);
    }
}
