using System;
using System.Threading.Tasks;
using Advance_Control.Services.Auth;
using Advance_Control.Services.Logging;
using Advance_Control.ViewModels;
using Moq;
using Xunit;

namespace Advance_Control.Tests.ViewModels
{
    /// <summary>
    /// Pruebas unitarias para el LoginViewModel
    /// </summary>
    public class LoginViewModelTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILoggingService> _mockLogger;

        public LoginViewModelTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILoggingService>();
        }

        [Fact]
        public void Constructor_WithNullAuthService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new LoginViewModel(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new LoginViewModel(_mockAuthService.Object, null!));
        }

        [Fact]
        public void User_WhenSet_UpdatesCanLogin()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object);
            var canLoginChangedCount = 0;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LoginViewModel.CanLogin))
                    canLoginChangedCount++;
            };

            // Act
            viewModel.User = "testuser";

            // Assert
            Assert.Equal("testuser", viewModel.User);
            Assert.True(canLoginChangedCount > 0, "CanLogin should notify property changed");
        }

        [Fact]
        public void Password_WhenSet_UpdatesCanLogin()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object);
            var canLoginChangedCount = 0;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LoginViewModel.CanLogin))
                    canLoginChangedCount++;
            };

            // Act
            viewModel.Password = "password123";

            // Assert
            Assert.Equal("password123", viewModel.Password);
            Assert.True(canLoginChangedCount > 0, "CanLogin should notify property changed");
        }

        [Fact]
        public void CanLogin_WithValidCredentials_ReturnsTrue()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = "testuser",
                Password = "password123"
            };

            // Assert
            Assert.True(viewModel.CanLogin);
        }

        [Fact]
        public void CanLogin_WithEmptyUser_ReturnsFalse()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = "",
                Password = "password123"
            };

            // Assert
            Assert.False(viewModel.CanLogin);
        }

        [Fact]
        public void CanLogin_WithEmptyPassword_ReturnsFalse()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = "testuser",
                Password = ""
            };

            // Assert
            Assert.False(viewModel.CanLogin);
        }

        [Fact]
        public void CanLogin_WhenLoading_ReturnsFalse()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = "testuser",
                Password = "password123"
            };

            // Act
            viewModel.GetType().GetProperty("IsLoading")!.SetValue(viewModel, true);

            // Assert
            Assert.False(viewModel.CanLogin);
        }

        [Fact]
        public void HasError_WithErrorMessage_ReturnsTrue()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object);

            // Act
            viewModel.GetType().GetProperty("ErrorMessage")!.SetValue(viewModel, "Test error");

            // Assert
            Assert.True(viewModel.HasError);
        }

        [Fact]
        public void HasError_WithoutErrorMessage_ReturnsFalse()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object);

            // Assert
            Assert.False(viewModel.HasError);
        }

        [Fact]
        public void ClearForm_ResetsAllFields()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = "testuser",
                Password = "password123"
            };
            viewModel.GetType().GetProperty("ErrorMessage")!.SetValue(viewModel, "Test error");

            // Act
            viewModel.ClearForm();

            // Assert
            Assert.Empty(viewModel.User);
            Assert.Empty(viewModel.Password);
            Assert.Empty(viewModel.ErrorMessage);
        }

        [Fact]
        public void LoginCommand_IsNotNull()
        {
            // Arrange & Act
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(viewModel.LoginCommand);
        }

        [Theory]
        [InlineData("", "password123", "El nombre de usuario es requerido.")]
        [InlineData("ab", "password123", "El nombre de usuario debe tener al menos 3 caracteres.")]
        [InlineData("user", "", "La contraseña es requerida.")]
        [InlineData("user", "123", "La contraseña debe tener al menos 4 caracteres.")]
        public void ExecuteLogin_WithInvalidCredentials_SetsErrorMessage(string user, string password, string expectedError)
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = user,
                Password = password
            };

            // Act
            viewModel.LoginCommand.Execute(null);
            
            // Give a moment for async operation to start
            Task.Delay(100).Wait();

            // Assert
            Assert.Contains(expectedError, viewModel.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteLogin_WithSuccessfulAuth_SetsLoginSuccessful()
        {
            // Arrange
            _mockAuthService
                .Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(true);

            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = "validuser",
                Password = "validpassword"
            };

            // Act
            viewModel.LoginCommand.Execute(null);
            
            // Wait for async operation to complete
            await Task.Delay(500);

            // Assert
            Assert.True(viewModel.LoginSuccessful);
        }

        [Fact]
        public async Task ExecuteLogin_WithFailedAuth_SetsErrorMessage()
        {
            // Arrange
            _mockAuthService
                .Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(false);

            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = "validuser",
                Password = "validpassword"
            };

            // Act
            viewModel.LoginCommand.Execute(null);
            
            // Wait for async operation to complete
            await Task.Delay(500);

            // Assert
            Assert.False(viewModel.LoginSuccessful);
            Assert.Contains("Usuario o contraseña incorrectos", viewModel.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteLogin_WhenException_SetsErrorMessage()
        {
            // Arrange
            _mockAuthService
                .Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ThrowsAsync(new Exception("Network error"));

            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockLogger.Object)
            {
                User = "validuser",
                Password = "validpassword"
            };

            // Act
            viewModel.LoginCommand.Execute(null);
            
            // Wait for async operation to complete
            await Task.Delay(500);

            // Assert
            Assert.False(viewModel.LoginSuccessful);
            Assert.Contains("Error al iniciar sesión", viewModel.ErrorMessage);
        }
    }
}
