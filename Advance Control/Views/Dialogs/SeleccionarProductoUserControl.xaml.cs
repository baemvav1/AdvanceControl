using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Productos;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// UserControl para seleccionar un producto de la lista
    /// </summary>
    public sealed partial class SeleccionarProductoUserControl : UserControl
    {
        private readonly IProductoService _productoService;
        private List<ProductoDto> _allProductos = new();
        private bool _isDataLoaded = false;

        public SeleccionarProductoUserControl()
        {
            this.InitializeComponent();

            // Resolver el servicio de productos desde DI
            _productoService = ((App)Application.Current).Host.Services.GetRequiredService<IProductoService>();

            // Cargar productos al inicializar
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Solo cargar una vez
            if (!_isDataLoaded)
            {
                await LoadProductosAsync();
                _isDataLoaded = true;
            }
        }

        /// <summary>
        /// Producto seleccionado actualmente
        /// </summary>
        public ProductoDto? SelectedProducto { get; private set; }

        /// <summary>
        /// Indica si hay un producto seleccionado
        /// </summary>
        public bool HasSelection => SelectedProducto != null;

        /// <summary>
        /// Evento que se dispara cuando el costo final del producto debe ser usado para rellenar el monto
        /// </summary>
        public event EventHandler<double?>? CostoChanged;

        /// <summary>
        /// Carga la lista de productos desde el servicio
        /// </summary>
        private async Task LoadProductosAsync()
        {
            try
            {
                LoadingRing.Visibility = Visibility.Visible;
                LoadingRing.IsActive = true;
                ProductosListView.Visibility = Visibility.Collapsed;

                _allProductos = await _productoService.GetProductosAsync(null, CancellationToken.None);

                ProductosListView.ItemsSource = _allProductos;
                ProductosListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar lista vacía con mensaje informativo
                _allProductos = new List<ProductoDto>();
                ProductosListView.ItemsSource = _allProductos;
                ProductosListView.Visibility = Visibility.Visible;

                // Log del error para diagnóstico
                System.Diagnostics.Debug.WriteLine($"Error al cargar productos: {ex.GetType().Name} - {ex.Message}");

                // Mostrar mensaje de error en el placeholder de búsqueda
                ConceptoTextBox.PlaceholderText = "Error al cargar productos. Intente nuevamente.";
            }
            finally
            {
                LoadingRing.Visibility = Visibility.Collapsed;
                LoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Maneja el cambio de texto en los cuadros de búsqueda
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var conceptoSearch = ConceptoTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var descripcionSearch = DescripcionTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(conceptoSearch) && string.IsNullOrWhiteSpace(descripcionSearch))
            {
                ProductosListView.ItemsSource = _allProductos;
            }
            else
            {
                // Filtrar productos localmente
                var filtered = _allProductos.Where(pr =>
                {
                    bool matchesConcepto = string.IsNullOrWhiteSpace(conceptoSearch) ||
                                          (pr.Concepto?.ToLowerInvariant().Contains(conceptoSearch) == true);

                    bool matchesDescripcion = string.IsNullOrWhiteSpace(descripcionSearch) ||
                                             (pr.Descripcion?.ToLowerInvariant().Contains(descripcionSearch) == true);

                    return matchesConcepto && matchesDescripcion;
                }).ToList();

                ProductosListView.ItemsSource = filtered;
            }
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de productos
        /// </summary>
        private void ProductosListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedProducto = ProductosListView.SelectedItem as ProductoDto;

            // Notificar el costo final para prellenar el monto del cargo
            CostoChanged?.Invoke(this, SelectedProducto?.CostoFinal);
        }
    }
}
