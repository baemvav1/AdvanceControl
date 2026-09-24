using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Productos;
using Advance_Control.Services.Logging;
using Advance_Control.ViewModels.Common;

namespace Advance_Control.ViewModels
{
    public class ProductosViewModel : ViewModelBase
    {
        public PaginadorViewModel<ProductoDto> Paginacion { get; } = new();

        private List<ProductoDto> _catalogoSugerencias = new();
        private bool HayFiltrosActivos =>
            !string.IsNullOrWhiteSpace(ConceptoFilter) || !string.IsNullOrWhiteSpace(DescripcionFilter) ||
            !string.IsNullOrWhiteSpace(CostoDirectoFilter);

        public IEnumerable<string?> ValoresConcepto => _catalogoSugerencias.Select(p => p.Concepto);
        public IEnumerable<string?> ValoresDescripcion => _catalogoSugerencias.Select(p => p.Descripcion);

        private readonly IProductoService _productoService;
        private readonly ILoggingService _logger;
        private ObservableCollection<ProductoDto> _productos;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _conceptoFilter;
        private string? _descripcionFilter;
        private string? _costoDirectoFilter;

        public ProductosViewModel(IProductoService productoService, ILoggingService logger)
        {
            _productoService = productoService ?? throw new ArgumentNullException(nameof(productoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _productos = new ObservableCollection<ProductoDto>();
        }

        public ObservableCollection<ProductoDto> Productos
        {
            get => _productos;
            set => SetProperty(ref _productos, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string? ConceptoFilter
        {
            get => _conceptoFilter;
            set => SetProperty(ref _conceptoFilter, value);
        }

        public string? DescripcionFilter
        {
            get => _descripcionFilter;
            set => SetProperty(ref _descripcionFilter, value);
        }

        public string? CostoDirectoFilter
        {
            get => _costoDirectoFilter;
            set => SetProperty(ref _costoDirectoFilter, value);
        }

        /// <summary>
        /// Carga los productos desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadProductosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                await _logger.LogInformationAsync("Cargando productos...", "ProductosViewModel", "LoadProductosAsync");

                double? costoValue = null;
                if (!string.IsNullOrWhiteSpace(CostoDirectoFilter))
                {
                    if (double.TryParse(CostoDirectoFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedCosto))
                    {
                        costoValue = parsedCosto;
                    }
                }

                var query = new ProductoQueryDto
                {
                    Concepto = ConceptoFilter,
                    Descripcion = DescripcionFilter,
                    CostoDirecto = costoValue
                };

                var productos = await _productoService.GetProductosAsync(query, cancellationToken);

                Productos.Clear();
                foreach (var producto in productos)
                {
                    Productos.Add(producto);
                }
                Paginacion.EstablecerElementos(productos);

                if (_catalogoSugerencias.Count == 0 && !HayFiltrosActivos)
                    _catalogoSugerencias = productos.ToList();

                await _logger.LogInformationAsync($"Se cargaron {productos.Count} productos exitosamente", "ProductosViewModel", "LoadProductosAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de productos cancelada por el usuario", "ProductosViewModel", "LoadProductosAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar productos", ex, "ProductosViewModel", "LoadProductosAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error inesperado al cargar productos. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync("Error inesperado al cargar productos", ex, "ProductosViewModel", "LoadProductosAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los productos
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ConceptoFilter = null;
                DescripcionFilter = null;
                CostoDirectoFilter = null;
                ErrorMessage = null;
                await LoadProductosAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar productos.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "ProductosViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina un producto por su ID
        /// </summary>
        public async Task<bool> DeleteProductoAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando producto {id}...", "ProductosViewModel", "DeleteProductoAsync");

                var result = await _productoService.DeleteProductoAsync(id, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Producto {id} eliminado exitosamente", "ProductosViewModel", "DeleteProductoAsync");
                    await LoadProductosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar el producto.";
                    await _logger.LogWarningAsync($"No se pudo eliminar el producto {id}", "ProductosViewModel", "DeleteProductoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al eliminar producto. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync($"Error al eliminar producto {id}", ex, "ProductosViewModel", "DeleteProductoAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza un producto existente
        /// </summary>
        public async Task<bool> UpdateProductoAsync(int id, ProductoQueryDto updateData, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando producto {id}...", "ProductosViewModel", "UpdateProductoAsync");

                var result = await _productoService.UpdateProductoAsync(id, updateData, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Producto {id} actualizado exitosamente", "ProductosViewModel", "UpdateProductoAsync");
                    await LoadProductosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo actualizar el producto.";
                    await _logger.LogWarningAsync($"No se pudo actualizar el producto {id}", "ProductosViewModel", "UpdateProductoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al actualizar producto. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync($"Error al actualizar producto {id}", ex, "ProductosViewModel", "UpdateProductoAsync");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo producto
        /// </summary>
        public async Task<bool> CreateProductoAsync(string concepto, string descripcion, double costoDirecto, double porcentajeUtilidad, bool estatus = true, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync("Creando nuevo producto...", "ProductosViewModel", "CreateProductoAsync");

                var result = await _productoService.CreateProductoAsync(concepto, descripcion, costoDirecto, porcentajeUtilidad, estatus, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync("Producto creado exitosamente", "ProductosViewModel", "CreateProductoAsync");
                    await LoadProductosAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear el producto.";
                    await _logger.LogWarningAsync("No se pudo crear el producto", "ProductosViewModel", "CreateProductoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al crear producto. Por favor, intente nuevamente.";
                await _logger.LogErrorAsync("Error al crear producto", ex, "ProductosViewModel", "CreateProductoAsync");
                return false;
            }
        }
    }
}
