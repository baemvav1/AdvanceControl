using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Navigation;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Alertas;
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
    public class MainViewModelTests
    {
        private readonly Mock<INavigationService> _mockNavigationService = new();
        private readonly Mock<IOnlineCheck> _mockOnlineCheck = new();
        private readonly Mock<ILoggingService> _mockLogger = new();
        private readonly Mock<IAuthService> _mockAuthService = new();
        private readonly Mock<IDialogService> _mockDialogService = new();
        private readonly Mock<IServiceProvider> _mockServiceProvider = new();
        private readonly Mock<INotificacionService> _mockNotificacionService = new();
        private readonly Mock<IUserInfoService> _mockUserInfoService = new();
        private readonly Mock<INotificacionAlertaService> _mockAlertaService = new();
        private readonly Mock<IActivityService> _mockActivityService = new();

        [Fact]
        public void Constructor_WithNullUserInfoService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MainViewModel(
                    _mockNavigationService.Object,
                    _mockOnlineCheck.Object,
                    _mockLogger.Object,
                    _mockAuthService.Object,
                    _mockDialogService.Object,
                    _mockServiceProvider.Object,
                    _mockNotificacionService.Object,
                    null!,
                    _mockAlertaService.Object,
                    _mockActivityService.Object));
        }

        [Fact]
        public async Task LoadUserInfoAsync_WithValidUserInfo_SetsUserInitialsAndType()
        {
            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserInfoDto
                {
                    NombreCompleto = "Braulio Emiliano Vazquez",
                    TipoUsuario = "Devs"
                });

            var viewModel = CreateViewModel();

            await viewModel.LoadUserInfoAsync();

            Assert.Equal("BEV", viewModel.UserInitials);
            Assert.Equal("Devs", viewModel.UserType);
        }

        [Fact]
        public async Task LoadUserInfoAsync_WhenServiceReturnsNull_ClearsUserInfo()
        {
            _mockUserInfoService
                .Setup(x => x.GetUserInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserInfoDto?)null);

            var viewModel = CreateViewModel();
            viewModel.UserInitials = "TMP";
            viewModel.UserType = "TMP";

            await viewModel.LoadUserInfoAsync();

            Assert.Equal(string.Empty, viewModel.UserInitials);
            Assert.Equal(string.Empty, viewModel.UserType);
        }

        [Fact]
        public async Task CargarAlertasAsync_WithResults_PopulatesCollection()
        {
            _mockAlertaService
                .Setup(x => x.GenerarYObtenerAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<NotificacionAlerta>
                 {
                    new() { IdNotificacion = 1, Titulo = "Alerta 1" },
                    new() { IdNotificacion = 2, Titulo = "Alerta 2" }
                 });

            var viewModel = CreateViewModel();

            await viewModel.CargarAlertasAsync(10);

            Assert.True(viewModel.HasAlertasDb);
            Assert.Equal(2, viewModel.AlertasDb.Count);
            Assert.True(viewModel.HasUnseenNotifications);
        }

        [Fact]
        public async Task DescartarAlertasAsync_ClearsAlertCollection()
        {
             _mockAlertaService
                 .Setup(x => x.GenerarYObtenerAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificacionAlerta> { new() { IdNotificacion = 1, Titulo = "Alerta 1" } });

            var viewModel = CreateViewModel();
            await viewModel.CargarAlertasAsync(10);

            await viewModel.DescartarAlertasAsync(10);

            Assert.False(viewModel.HasAlertasDb);
            Assert.Empty(viewModel.AlertasDb);
            _mockAlertaService.Verify(x => x.MarcarVistasAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_ClearsAuthenticationState()
        {
            _mockAuthService.Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var viewModel = CreateViewModel();
            viewModel.IsAuthenticated = true;
            viewModel.UserInitials = "ABC";
            viewModel.UserType = "Admin";

            await viewModel.LogoutAsync();

            Assert.False(viewModel.IsAuthenticated);
            Assert.Equal(string.Empty, viewModel.UserInitials);
            Assert.Equal(string.Empty, viewModel.UserType);
        }

        private MainViewModel CreateViewModel()
            => new(
                _mockNavigationService.Object,
                _mockOnlineCheck.Object,
                _mockLogger.Object,
                _mockAuthService.Object,
                _mockDialogService.Object,
                _mockServiceProvider.Object,
                _mockNotificacionService.Object,
                _mockUserInfoService.Object,
                _mockAlertaService.Object,
                _mockActivityService.Object);
    }
}
