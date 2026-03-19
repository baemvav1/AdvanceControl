using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Clientes;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Session;
using Advance_Control.ViewModels;
using Moq;
using Xunit;

namespace Advance_Control.Tests.ViewModels
{
    public class CustomersViewModelTests
    {
        private readonly Mock<IClienteService> _mockClienteService = new();
        private readonly Mock<ILoggingService> _mockLogger = new();
        private readonly Mock<IUserSessionService> _mockUserSessionService = new();
        private readonly Mock<IActivityService> _mockActivityService = new();

        [Fact]
        public void Constructor_WithNullClienteService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CustomersViewModel(null!, _mockLogger.Object, _mockUserSessionService.Object, _mockActivityService.Object));
        }

        [Fact]
        public void Constructor_InitializesCustomersCollection()
        {
            var viewModel = CreateViewModel();

            Assert.NotNull(viewModel.Customers);
            Assert.Empty(viewModel.Customers);
            Assert.True(viewModel.IsEmpty);
        }

        [Fact]
        public async Task LoadClientesAsync_WithValidData_PopulatesCustomers()
        {
            var clientes = new List<CustomerDto>
            {
                new() { IdCliente = 1, NombreComercial = "Cliente 1", RazonSocial = "Razon 1", Rfc = "RFC1", Notas = "Nota 1", Prioridad = 1 },
                new() { IdCliente = 2, NombreComercial = "Cliente 2", RazonSocial = "Razon 2", Rfc = "RFC2", Notas = "Nota 2", Prioridad = 2 }
            };

            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientes);

            var viewModel = CreateViewModel();

            await viewModel.LoadClientesAsync();

            Assert.Equal(2, viewModel.Customers.Count);
            Assert.Equal("Cliente 1", viewModel.Customers[0].NombreComercial);
            Assert.Equal("Cliente 2", viewModel.Customers[1].NombreComercial);
            Assert.Null(viewModel.ErrorMessage);
            Assert.False(viewModel.IsLoading);
            Assert.False(viewModel.IsEmpty);
        }

        [Fact]
        public async Task LoadClientesAsync_WithNullResult_SetsErrorMessage()
        {
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<CustomerDto>)null!);

            var viewModel = CreateViewModel();

            await viewModel.LoadClientesAsync();

            Assert.Empty(viewModel.Customers);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("no devolvió datos válidos", viewModel.ErrorMessage);
            Assert.True(viewModel.HasError);
        }

        [Fact]
        public async Task LoadClientesAsync_WithHttpException_SetsConnectionError()
        {
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network"));

            var viewModel = CreateViewModel();

            await viewModel.LoadClientesAsync();

            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("Error de conexión", viewModel.ErrorMessage);
            Assert.True(viewModel.HasError);
            Assert.False(viewModel.IsLoading);
        }

        [Fact]
        public async Task LoadClientesAsync_WhenAlreadyLoading_DoesNotCallService()
        {
            var viewModel = CreateViewModel
            ();
            viewModel.IsLoading = true;

            await viewModel.LoadClientesAsync();

            _mockClienteService.Verify(
                x => x.GetClientesAsync(It.IsAny<ClienteQueryDto?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task LoadClientesAsync_PassesCurrentFiltersInQuery()
        {
            ClienteQueryDto? capturedQuery = null;

            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto?>(), It.IsAny<CancellationToken>()))
                .Callback<ClienteQueryDto?, CancellationToken>((query, _) => capturedQuery = query)
                .ReturnsAsync(new List<CustomerDto>());

            var viewModel = CreateViewModel();
            viewModel.RfcFilter = "RFC-DEMO";
            viewModel.NotasFilter = "Notas demo";
            viewModel.PrioridadFilter = 3;
            viewModel.SearchText = "No usado por query";
            viewModel.CurpFilter = "Tampoco usado";

            await viewModel.LoadClientesAsync();

            Assert.NotNull(capturedQuery);
            Assert.Equal("RFC-DEMO", capturedQuery!.Rfc);
            Assert.Equal("Notas demo", capturedQuery.Notas);
            Assert.Equal(3, capturedQuery.Prioridad);
        }

        [Fact]
        public async Task ClearFiltersAsync_ClearsFiltersAndReloads()
        {
            _mockClienteService
                .Setup(x => x.GetClientesAsync(It.IsAny<ClienteQueryDto?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CustomerDto>());

            var viewModel = CreateViewModel();
            viewModel.SearchText = "Busqueda";
            viewModel.RfcFilter = "RFC";
            viewModel.CurpFilter = "CURP";
            viewModel.NotasFilter = "Notas";
            viewModel.PrioridadFilter = 2;

            await viewModel.ClearFiltersAsync();

            Assert.Null(viewModel.SearchText);
            Assert.Null(viewModel.RfcFilter);
            Assert.Null(viewModel.CurpFilter);
            Assert.Null(viewModel.NotasFilter);
            Assert.Null(viewModel.PrioridadFilter);
            _mockClienteService.Verify(
                x => x.GetClientesAsync(It.IsAny<ClienteQueryDto?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private CustomersViewModel CreateViewModel()
            => new(
                _mockClienteService.Object,
                _mockLogger.Object,
                _mockUserSessionService.Object,
                _mockActivityService.Object);
    }
}
