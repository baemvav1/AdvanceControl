using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Logging;
using Advance_Control.ViewModels;
using Moq;
using Xunit;

namespace Advance_Control.Tests.ViewModels
{
    /// <summary>
    /// Pruebas unitarias para el CustomersViewModel
    /// </summary>
    public class CustomersViewModelTests
    {
        private readonly Mock<IClienteService> _mockClienteService;
        private readonly Mock<ILoggingService> _mockLogger;

        public CustomersViewModelTests()
        {
            _mockClienteService = new Mock<IClienteService>();
            _mockLogger = new Mock<ILoggingService>();
        }

        [Fact]
        public void Constructor_WithNullClienteService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new CustomersViewModel(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new CustomersViewModel(_mockClienteService.Object, null!));
        }

        [Fact]
        public void Constructor_InitializesCustomersCollection()
        {
            // Act
            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(viewModel.Customers);
            Assert.Empty(viewModel.Customers);
        }

        [Fact]
        public async Task LoadClientesAsync_WithValidData_PopulatesCustomers()
        {
            // Arrange
            var mockClientes = new List<CustomerDto>
            {
                new CustomerDto { Id = 1, Name = "Cliente 1", Email = "cliente1@test.com" },
                new CustomerDto { Id = 2, Name = "Cliente 2", Email = "cliente2@test.com" },
                new CustomerDto { Id = 3, Name = "Cliente 3", Email = "cliente3@test.com" }
            };

            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockClientes);

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            Assert.Equal(3, viewModel.Customers.Count);
            Assert.Equal("Cliente 1", viewModel.Customers[0].Name);
            Assert.Equal("Cliente 2", viewModel.Customers[1].Name);
            Assert.Equal("Cliente 3", viewModel.Customers[2].Name);
            Assert.False(viewModel.IsLoading);
            Assert.Null(viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadClientesAsync_WhenLoading_DoesNotLoadAgain()
        {
            // Arrange
            var mockClientes = new List<CustomerDto>
            {
                new CustomerDto { Id = 1, Name = "Cliente 1", Email = "cliente1@test.com" }
            };

            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockClientes);

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);
            
            // Set IsLoading to true manually
            viewModel.GetType().GetProperty("IsLoading")!.SetValue(viewModel, true);

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            _mockClienteService.Verify(
                x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task LoadClientesAsync_WithEmptyResult_ClearsCustomers()
        {
            // Arrange
            var emptyList = new List<CustomerDto>();

            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyList);

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            Assert.Empty(viewModel.Customers);
            Assert.False(viewModel.IsLoading);
            Assert.Null(viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadClientesAsync_WithNullResult_SetsErrorMessage()
        {
            // Arrange
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<CustomerDto>?)null);

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            Assert.Empty(viewModel.Customers);
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("no devolvió datos válidos", viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadClientesAsync_WithHttpException_SetsErrorMessage()
        {
            // Arrange
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error"));

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("Error de conexión", viewModel.ErrorMessage);
            Assert.True(viewModel.HasError);
        }

        [Fact]
        public async Task LoadClientesAsync_WithCancellation_SetsErrorMessage()
        {
            // Arrange
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("cancelada", viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadClientesAsync_WithGeneralException_SetsErrorMessage()
        {
            // Arrange
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("Error inesperado", viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadClientesAsync_WithFilters_PassesCorrectQuery()
        {
            // Arrange
            ClienteQueryDto? capturedQuery = null;
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .Callback<ClienteQueryDto?, CancellationToken>((query, _) => capturedQuery = query)
                .ReturnsAsync(new List<CustomerDto>());

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object)
            {
                SearchText = "test search",
                RfcFilter = "RFC123",
                CurpFilter = "CURP456",
                NotasFilter = "notas",
                PrioridadFilter = 5
            };

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            Assert.NotNull(capturedQuery);
            Assert.Equal("test search", capturedQuery.Search);
            Assert.Equal("RFC123", capturedQuery.Rfc);
            Assert.Equal("CURP456", capturedQuery.Curp);
            Assert.Equal("notas", capturedQuery.Notas);
            Assert.Equal(5, capturedQuery.Prioridad);
        }

        [Fact]
        public async Task ClearFiltersAsync_ResetsAllFiltersAndReloads()
        {
            // Arrange
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CustomerDto>());

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object)
            {
                SearchText = "test",
                RfcFilter = "RFC",
                CurpFilter = "CURP",
                NotasFilter = "notas",
                PrioridadFilter = 5
            };

            // Act
            await viewModel.ClearFiltersAsync();

            // Assert
            Assert.Null(viewModel.SearchText);
            Assert.Null(viewModel.RfcFilter);
            Assert.Null(viewModel.CurpFilter);
            Assert.Null(viewModel.NotasFilter);
            Assert.Null(viewModel.PrioridadFilter);
            Assert.Null(viewModel.ErrorMessage);
            _mockClienteService.Verify(
                x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task ClearFiltersAsync_WithException_SetsErrorMessage()
        {
            // Arrange
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test error"));

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

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
            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Act
            viewModel.GetType().GetProperty("ErrorMessage")!.SetValue(viewModel, "Test error");

            // Assert
            Assert.True(viewModel.HasError);
        }

        [Fact]
        public void HasError_WithoutErrorMessage_ReturnsFalse()
        {
            // Arrange
            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);

            // Assert
            Assert.False(viewModel.HasError);
        }

        [Fact]
        public async Task LoadClientesAsync_SetsIsLoadingCorrectly()
        {
            // Arrange
            var isLoadingStates = new List<bool>();
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CustomerDto>());

            var viewModel = new CustomersViewModel(_mockClienteService.Object, _mockLogger.Object);
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CustomersViewModel.IsLoading))
                    isLoadingStates.Add(viewModel.IsLoading);
            };

            // Act
            await viewModel.LoadClientesAsync();

            // Assert
            Assert.Contains(true, isLoadingStates); // Should have been true during loading
            Assert.False(viewModel.IsLoading); // Should be false after completion
        }

        [Fact]
        public void CustomerDto_ExpandProperty_IsInitializedToFalse()
        {
            // Arrange & Act
            var customer = new CustomerDto
            {
                IdCliente = 1,
                RazonSocial = "Test Cliente",
                Rfc = "TEST123456ABC"
            };

            // Assert
            Assert.False(customer.Expand);
        }
    }
}
