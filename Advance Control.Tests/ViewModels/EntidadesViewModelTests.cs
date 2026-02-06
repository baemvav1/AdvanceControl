using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Entidades;
using Advance_Control.Services.Logging;
using Advance_Control.ViewModels;
using Moq;
using Xunit;

namespace Advance_Control.Tests.ViewModels
{
    /// <summary>
    /// Pruebas unitarias para el EntidadesViewModel
    /// </summary>
    public class EntidadesViewModelTests
    {
        private readonly Mock<IEntidadService> _mockEntidadService;
        private readonly Mock<ILoggingService> _mockLogger;

        public EntidadesViewModelTests()
        {
            _mockEntidadService = new Mock<IEntidadService>();
            _mockLogger = new Mock<ILoggingService>();
        }

        [Fact]
        public void Constructor_WithNullEntidadService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new EntidadesViewModel(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new EntidadesViewModel(_mockEntidadService.Object, null!));
        }

        [Fact]
        public void Constructor_InitializesEntidadesCollection()
        {
            // Act
            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(viewModel.Entidades);
            Assert.Empty(viewModel.Entidades);
        }

        [Fact]
        public async Task LoadEntidadesAsync_WithValidData_PopulatesEntidades()
        {
            // Arrange
            var mockEntidades = new List<EntidadDto>
            {
                new EntidadDto { IdEntidad = 1, NombreComercial = "Entidad 1", RazonSocial = "Razon 1" },
                new EntidadDto { IdEntidad = 2, NombreComercial = "Entidad 2", RazonSocial = "Razon 2" },
                new EntidadDto { IdEntidad = 3, NombreComercial = "Entidad 3", RazonSocial = "Razon 3" }
            };

            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockEntidades);

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            Assert.Equal(3, viewModel.Entidades.Count);
            Assert.Equal("Entidad 1", viewModel.Entidades[0].NombreComercial);
            Assert.Equal("Entidad 2", viewModel.Entidades[1].NombreComercial);
            Assert.Equal("Entidad 3", viewModel.Entidades[2].NombreComercial);
            Assert.False(viewModel.IsLoading);
            Assert.Null(viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadEntidadesAsync_WhenLoading_DoesNotLoadAgain()
        {
            // Arrange
            var mockEntidades = new List<EntidadDto>
            {
                new EntidadDto { IdEntidad = 1, NombreComercial = "Entidad 1", RazonSocial = "Razon 1" }
            };

            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockEntidades);

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);
            
            // Set IsLoading to true manually
            viewModel.GetType().GetProperty("IsLoading")!.SetValue(viewModel, true);

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            _mockEntidadService.Verify(
                x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task LoadEntidadesAsync_WithEmptyResult_ClearsEntidades()
        {
            // Arrange
            var emptyList = new List<EntidadDto>();

            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyList);

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            Assert.Empty(viewModel.Entidades);
            Assert.False(viewModel.IsLoading);
            Assert.Null(viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadEntidadesAsync_WithNullResult_SetsErrorMessage()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<EntidadDto>?)null);

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            Assert.Empty(viewModel.Entidades);
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("no devolvió datos válidos", viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadEntidadesAsync_WithHttpException_SetsErrorMessage()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error"));

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("Error de conexión", viewModel.ErrorMessage);
            Assert.True(viewModel.HasError);
        }

        [Fact]
        public async Task LoadEntidadesAsync_WithCancellation_SetsErrorMessage()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("cancelada", viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadEntidadesAsync_WithGeneralException_SetsErrorMessage()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("Error inesperado", viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadEntidadesAsync_WithFilters_PassesCorrectQuery()
        {
            // Arrange
            EntidadQueryDto? capturedQuery = null;
            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .Callback<EntidadQueryDto?, CancellationToken>((query, _) => capturedQuery = query)
                .ReturnsAsync(new List<EntidadDto>());

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object)
            {
                NombreComercialFilter = "Test Nombre",
                RazonSocialFilter = "Test Razon",
                RfcFilter = "RFC123",
                EstadoFilter = "Estado X",
                CiudadFilter = "Ciudad Y"
            };

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            Assert.NotNull(capturedQuery);
            Assert.Equal("Test Nombre", capturedQuery.NombreComercial);
            Assert.Equal("Test Razon", capturedQuery.RazonSocial);
            Assert.Equal("RFC123", capturedQuery.RFC);
            Assert.Equal("Estado X", capturedQuery.Estado);
            Assert.Equal("Ciudad Y", capturedQuery.Ciudad);
        }

        [Fact]
        public async Task ClearFiltersAsync_ResetsAllFiltersAndReloads()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EntidadDto>());

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object)
            {
                NombreComercialFilter = "test",
                RazonSocialFilter = "razon",
                RfcFilter = "RFC",
                EstadoFilter = "estado",
                CiudadFilter = "ciudad"
            };

            // Act
            await viewModel.ClearFiltersAsync();

            // Assert
            Assert.Null(viewModel.NombreComercialFilter);
            Assert.Null(viewModel.RazonSocialFilter);
            Assert.Null(viewModel.RfcFilter);
            Assert.Null(viewModel.EstadoFilter);
            Assert.Null(viewModel.CiudadFilter);
            Assert.Null(viewModel.ErrorMessage);
            _mockEntidadService.Verify(
                x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task ClearFiltersAsync_WithException_SetsErrorMessage()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test error"));

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            await viewModel.ClearFiltersAsync();

            // Assert
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("Error al limpiar filtros", viewModel.ErrorMessage);
        }

        [Fact]
        public void HasError_WithErrorMessage_ReturnsTrue()
        {
            // Arrange
            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            viewModel.GetType().GetProperty("ErrorMessage")!.SetValue(viewModel, "Test error");

            // Assert
            Assert.True(viewModel.HasError);
        }

        [Fact]
        public void HasError_WithoutErrorMessage_ReturnsFalse()
        {
            // Arrange
            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Assert
            Assert.False(viewModel.HasError);
        }

        [Fact]
        public async Task LoadEntidadesAsync_SetsIsLoadingCorrectly()
        {
            // Arrange
            var isLoadingStates = new List<bool>();
            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EntidadDto>());

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(EntidadesViewModel.IsLoading))
                    isLoadingStates.Add(viewModel.IsLoading);
            };

            // Act
            await viewModel.LoadEntidadesAsync();

            // Assert
            Assert.Contains(true, isLoadingStates); // Should have been true during loading
            Assert.False(viewModel.IsLoading); // Should be false after completion
        }

        [Fact]
        public void EntidadDto_ExpandProperty_IsInitializedToFalse()
        {
            // Arrange & Act
            var entidad = new EntidadDto
            {
                IdEntidad = 1,
                NombreComercial = "Test Entidad",
                RazonSocial = "Test Razon Social"
            };

            // Assert
            Assert.False(entidad.Expand);
        }

        [Fact]
        public async Task CreateEntidadAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.CreateEntidadAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApiResponse { Success = true });

            _mockEntidadService
                .Setup(x => x.GetEntidadesAsync(It.IsAny<EntidadQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EntidadDto>());

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            var result = await viewModel.CreateEntidadAsync("Nombre Test", "Razon Test");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CreateEntidadAsync_WithFailedResponse_ReturnsFalse()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.CreateEntidadAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApiResponse { Success = false, Message = "Error" });

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            var result = await viewModel.CreateEntidadAsync("Nombre Test", "Razon Test");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CreateEntidadAsync_WithException_ReturnsFalse()
        {
            // Arrange
            _mockEntidadService
                .Setup(x => x.CreateEntidadAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test error"));

            var viewModel = new EntidadesViewModel(_mockEntidadService.Object, _mockLogger.Object);

            // Act
            var result = await viewModel.CreateEntidadAsync("Nombre Test", "Razon Test");

            // Assert
            Assert.False(result);
        }
    }
}
