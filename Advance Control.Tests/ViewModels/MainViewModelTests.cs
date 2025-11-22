using System;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Navigation;
using Advance_Control.Services.Auth;
using Advance_Control.Services.Dialog;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.OnlineCheck;
using Advance_Control.Services.UserInfo;
using Advance_Control.ViewModels;
using Moq;
using Xunit;

namespace Advance_Control.Tests.ViewModels
{
    /// <summary>
    /// Pruebas unitarias para el MainViewModel
    /// </summary>
    public class MainViewModelTests
    {
        private readonly Mock<INavigationService> _mockNavigationService;
        private readonly Mock<IOnlineCheck> _mockOnlineCheck;
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IDialogService> _mockDialogService;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<INotificacionService> _mockNotificacionService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;

        public MainViewModelTests()
        {
            _mockNavigationService = new Mock<INavigationService>();
            _mockOnlineCheck = new Mock<IOnlineCheck>();
            _mockLogger = new Mock<ILoggingService>();
            _mockAuthService = new Mock<IAuthService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockNotificacionService = new Mock<INotificacionService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Setup default auth service behavior
            _mockAuthService.Setup(x => x.IsAuthenticated).Returns(false);
        }

        [Fact]
        public void Constructor_WithAllServices_InitializesSuccessfully()
        {
            // Act
            var viewModel = CreateViewModel();

            // Assert
            Assert.NotNull(viewModel);
            Assert.False(viewModel.IsAuthenticated);
        }

        [Fact]
        public void Constructor_WithNullUserInfoService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new MainViewModel(
                    _mockNavigationService.Object,
                    _mockOnlineCheck.Object,
                    _mockLogger.Object,
                    _mockAuthService.Object,
                    _mockDialogService.Object,
                    _mockServiceProvider.Object,
                    _mockNotificacionService.Object,
                    null!));
        }

        [Fact]
        public async Task LoadUserInfoAsync_WithValidUserInfo_SetsUserInitialsAndType()
        {
            // Arrange
            var userInfo = new UserInfoDto
            {
                CredencialId = 1,
                NombreCompleto = "Braulio Emiliano Vazquez",
                Correo = "baemvav@gmail.com",
                Telefono = "5655139308",
                Nivel = 6,
                TipoUsuario = "Devs"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(default))
                .ReturnsAsync(userInfo);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.LoadUserInfoAsync();

            // Assert
            Assert.Equal("BEV", viewModel.UserInitials); // Braulio Emiliano Vazquez
            Assert.Equal("Devs", viewModel.UserType);
        }

        [Fact]
        public async Task LoadUserInfoAsync_WithFullName_ExtractsCorrectInitials()
        {
            // Arrange
            var userInfo = new UserInfoDto
            {
                NombreCompleto = "Juan Carlos Perez Lopez",
                TipoUsuario = "Admin"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(default))
                .ReturnsAsync(userInfo);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.LoadUserInfoAsync();

            // Assert
            Assert.Equal("JCP", viewModel.UserInitials); // Takes first 3 words
            Assert.Equal("Admin", viewModel.UserType);
        }

        [Fact]
        public async Task LoadUserInfoAsync_WithSingleName_ExtractsSingleInitial()
        {
            // Arrange
            var userInfo = new UserInfoDto
            {
                NombreCompleto = "John",
                TipoUsuario = "User"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(default))
                .ReturnsAsync(userInfo);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.LoadUserInfoAsync();

            // Assert
            Assert.Equal("J", viewModel.UserInitials);
            Assert.Equal("User", viewModel.UserType);
        }

        [Fact]
        public async Task LoadUserInfoAsync_WithTwoNames_ExtractsTwoInitials()
        {
            // Arrange
            var userInfo = new UserInfoDto
            {
                NombreCompleto = "Maria Garcia",
                TipoUsuario = "Support"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(default))
                .ReturnsAsync(userInfo);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.LoadUserInfoAsync();

            // Assert
            Assert.Equal("MG", viewModel.UserInitials);
            Assert.Equal("Support", viewModel.UserType);
        }

        [Fact]
        public async Task LoadUserInfoAsync_WhenServiceReturnsNull_ClearsUserInfo()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(default))
                .ReturnsAsync((UserInfoDto?)null);

            var viewModel = CreateViewModel();
            viewModel.UserInitials = "TEST";
            viewModel.UserType = "TestType";

            // Act
            await viewModel.LoadUserInfoAsync();

            // Assert
            Assert.Equal(string.Empty, viewModel.UserInitials);
            Assert.Equal(string.Empty, viewModel.UserType);
        }

        [Fact]
        public async Task LoadUserInfoAsync_WhenExceptionOccurs_ClearsUserInfo()
        {
            // Arrange
            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(default))
                .ThrowsAsync(new Exception("Test exception"));

            var viewModel = CreateViewModel();
            viewModel.UserInitials = "TEST";
            viewModel.UserType = "TestType";

            // Act
            await viewModel.LoadUserInfoAsync();

            // Assert
            Assert.Equal(string.Empty, viewModel.UserInitials);
            Assert.Equal(string.Empty, viewModel.UserType);
        }

        [Fact]
        public async Task LogoutAsync_ClearsUserInfo()
        {
            // Arrange
            _mockAuthService
                .Setup(x => x.LogoutAsync(default))
                .ReturnsAsync(true);

            var viewModel = CreateViewModel();
            viewModel.UserInitials = "TEST";
            viewModel.UserType = "TestType";

            // Act
            await viewModel.LogoutAsync();

            // Assert
            Assert.Equal(string.Empty, viewModel.UserInitials);
            Assert.Equal(string.Empty, viewModel.UserType);
            Assert.False(viewModel.IsAuthenticated);
        }

        [Fact]
        public async Task LoadUserInfoAsync_WithEmptyNombreCompleto_SetsEmptyInitials()
        {
            // Arrange
            var userInfo = new UserInfoDto
            {
                NombreCompleto = "",
                TipoUsuario = "User"
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(default))
                .ReturnsAsync(userInfo);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.LoadUserInfoAsync();

            // Assert
            Assert.Equal(string.Empty, viewModel.UserInitials);
            Assert.Equal("User", viewModel.UserType);
        }

        [Fact]
        public async Task LoadUserInfoAsync_WithNullTipoUsuario_SetsEmptyUserType()
        {
            // Arrange
            var userInfo = new UserInfoDto
            {
                NombreCompleto = "Test User",
                TipoUsuario = null
            };

            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(default))
                .ReturnsAsync(userInfo);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.LoadUserInfoAsync();

            // Assert
            Assert.Equal("TU", viewModel.UserInitials);
            Assert.Equal(string.Empty, viewModel.UserType);
        }

        private MainViewModel CreateViewModel()
        {
            return new MainViewModel(
                _mockNavigationService.Object,
                _mockOnlineCheck.Object,
                _mockLogger.Object,
                _mockAuthService.Object,
                _mockDialogService.Object,
                _mockServiceProvider.Object,
                _mockNotificacionService.Object,
                _mockUserInfoService.Object);
        }
    }
}
