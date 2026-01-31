using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Refacciones;
using Advance_Control.Services.RelacionesProveedorRefaccion;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// UserControl para seleccionar una refacción de la lista
    /// </summary>
    public sealed partial class SeleccionarRefaccionUserControl : UserControl
    {
        private readonly IRefaccionService _refaccionService;
        private readonly IRelacionProveedorRefaccionService _relacionProveedorRefaccionService;
        private List<RefaccionDto> _allRefacciones = new();
        private List<ProveedorPorRefaccionDto> _proveedores = new();
        private bool _isDataLoaded = false;
        private bool _hasProveedores = false;
        private bool _isLoadingProveedores = false;
        private int? _idProveedor;

        public SeleccionarRefaccionUserControl(int? idProveedor)
        {
            this.InitializeComponent();
            
            // Resolve services from DI
            _refaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRefaccionService>();
            _relacionProveedorRefaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionProveedorRefaccionService>();
            _idProveedor = idProveedor;
            // Cargar refacciones al inicializar
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Solo cargar una vez
            if (!_isDataLoaded)
            {
                await LoadRefaccionesAsync();
                _isDataLoaded = true;
            }
        }

        /// <summary>
        /// Refacción seleccionada actualmente
        /// </summary>
        public RefaccionDto? SelectedRefaccion { get; private set; }

        /// <summary>
        /// Proveedor seleccionado actualmente
        /// </summary>
        public ProveedorPorRefaccionDto? SelectedProveedor { get; private set; }

        /// <summary>
        /// Indica si hay una refacción seleccionada
        /// </summary>
        public bool HasSelection => SelectedRefaccion != null;

        /// <summary>
        /// Evento que se dispara cuando el costo de la refacción debe ser usado para rellenar el monto
        /// </summary>
        public event EventHandler<double?>? CostoChanged;

        /// <summary>
        /// Carga la lista de refacciones desde el servicio
        /// </summary>
        private async Task LoadRefaccionesAsync()
        {
            try
            {
                LoadingRing.Visibility = Visibility.Visible;
                LoadingRing.IsActive = true;

                RefaccionesListView.Visibility = Visibility.Collapsed;

                _allRefacciones = await _refaccionService.GetRefaccionesAsync(null, CancellationToken.None);

                RefaccionesListView.ItemsSource = _allRefacciones;
                RefaccionesListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar lista vacía con mensaje informativo
                _allRefacciones = new List<RefaccionDto>();
                RefaccionesListView.ItemsSource = _allRefacciones;
                RefaccionesListView.Visibility = Visibility.Visible;
                
                // Log del error para diagnóstico
                System.Diagnostics.Debug.WriteLine($"Error al cargar refacciones: {ex.GetType().Name} - {ex.Message}");
                
                // Mostrar mensaje de error en el placeholder de búsqueda
                MarcaTextBox.PlaceholderText = "Error al cargar refacciones. Intente nuevamente.";
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
            var marcaSearch = MarcaTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var serieSearch = SerieTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(marcaSearch) && string.IsNullOrWhiteSpace(serieSearch))
            {
                RefaccionesListView.ItemsSource = _allRefacciones;
            }
            else
            {
                // Filtrar refacciones localmente
                var filtered = _allRefacciones.Where(rf =>
                {
                    bool matchesMarca = string.IsNullOrWhiteSpace(marcaSearch) || 
                                       (rf.Marca?.ToLowerInvariant().Contains(marcaSearch) == true);
                    
                    bool matchesSerie = string.IsNullOrWhiteSpace(serieSearch) || 
                                       (rf.Serie?.ToLowerInvariant().Contains(serieSearch) == true);
                    
                    return matchesMarca && matchesSerie;
                }).ToList();

                RefaccionesListView.ItemsSource = filtered;
            }
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de refacciones
        /// </summary>
        private async void RefaccionesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                SelectedRefaccion = RefaccionesListView.SelectedItem as RefaccionDto;
                
                if (SelectedRefaccion != null)
                {
                    // Hide search panel and list
                    SearchPanel.Visibility = Visibility.Collapsed;
                    RefaccionesListView.Visibility = Visibility.Collapsed;
                    
                    // Show selected refaccion panel
                    SelectedRefaccionPanel.Visibility = Visibility.Visible;
                    SelectedMarcaTextBlock.Text = SelectedRefaccion.Marca ?? string.Empty;
                    SelectedSerieTextBlock.Text = SelectedRefaccion.Serie ?? string.Empty;
                    SelectedCostoTextBlock.Text = $"Costo: ${SelectedRefaccion.Costo ?? 0}";
                    
                    // Notify costo change
                    CostoChanged?.Invoke(this, SelectedRefaccion.Costo);
                    
                    // Check if proveedor exists
                    try
                    {
                        _hasProveedores = await _refaccionService.CheckProveedorExistsAsync(SelectedRefaccion.IdRefaccion);
                        
                        if (_hasProveedores)
                        {
                            // If idProveedor is preselected (not 0), load and auto-select the provider
                            if (_idProveedor.HasValue && _idProveedor.Value != 0)
                            {
                                await LoadAndPreselectProveedorAsync(SelectedRefaccion.IdRefaccion, _idProveedor.Value);
                            }
                            else
                            {
                                // Show proveedores button for manual selection
                                ProveedoresPanel.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            ProveedoresPanel.Visibility = Visibility.Collapsed;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar proveedores: {ex.GetType().Name} - {ex.Message}");
                        ProveedoresPanel.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en RefaccionesListView_SelectionChanged: {ex.GetType().Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja el clic en el botón para mostrar la lista de refacciones nuevamente
        /// </summary>
        private void ShowListButton_Click(object sender, RoutedEventArgs e)
        {
            // Show search panel and list
            SearchPanel.Visibility = Visibility.Visible;
            RefaccionesListView.Visibility = Visibility.Visible;
            
            // Hide selected refaccion panel and proveedores
            SelectedRefaccionPanel.Visibility = Visibility.Collapsed;
            SelectedProveedorPanel.Visibility = Visibility.Collapsed;
            ProveedoresPanel.Visibility = Visibility.Collapsed;
            ProveedoresGrid.Visibility = Visibility.Collapsed;
            
            // Clear selection
            RefaccionesListView.SelectedItem = null;
            SelectedRefaccion = null;
            
            // Clear provider selection and data
            ProveedoresListView.ItemsSource = null;
            ProveedoresListView.SelectedItem = null;
            SelectedProveedor = null;
            _proveedores.Clear();
            _isLoadingProveedores = false;
            
            // Notify costo cleared
            CostoChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Maneja el clic en el botón de Proveedores
        /// </summary>
        private async void ProveedoresButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle proveedores grid visibility
            if (ProveedoresGrid.Visibility == Visibility.Collapsed)
            {
                // Show the grid and load providers if not already loaded
                ProveedoresGrid.Visibility = Visibility.Visible;
                
                // Prevent concurrent loads
                if (!_isLoadingProveedores && _proveedores.Count == 0 && SelectedRefaccion != null)
                {
                    await LoadProveedoresAsync(SelectedRefaccion.IdRefaccion);
                }
            }
            else
            {
                ProveedoresGrid.Visibility = Visibility.Collapsed;
                // Ensure loading ring is off when hiding grid
                ProveedoresLoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Carga la lista de proveedores para la refacción seleccionada
        /// </summary>
        private async Task LoadProveedoresAsync(int idRefaccion)
        {
            _isLoadingProveedores = true;
            try
            {
                ProveedoresLoadingRing.IsActive = true;
                ProveedoresListView.Visibility = Visibility.Collapsed;

                _proveedores = await _relacionProveedorRefaccionService.GetProveedoresByRefaccionAsync(idRefaccion, CancellationToken.None);

                ProveedoresListView.ItemsSource = _proveedores;
                ProveedoresListView.Visibility = Visibility.Visible;
            }
            catch (InvalidOperationException ex)
            {
                // Network or API error - show empty list gracefully
                _proveedores = new List<ProveedorPorRefaccionDto>();
                ProveedoresListView.ItemsSource = _proveedores;
                ProveedoresListView.Visibility = Visibility.Visible;
                
                System.Diagnostics.Debug.WriteLine($"Error al cargar proveedores: {ex.Message}");
            }
            finally
            {
                ProveedoresLoadingRing.IsActive = false;
                _isLoadingProveedores = false;
            }
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de proveedores
        /// </summary>
        private void ProveedoresListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedProveedor = ProveedoresListView.SelectedItem as ProveedorPorRefaccionDto;
            
            // When a proveedor is selected, show the selected proveedor panel
            if (SelectedProveedor != null)
            {
                // Hide the proveedores list and button
                ProveedoresPanel.Visibility = Visibility.Collapsed;
                ProveedoresGrid.Visibility = Visibility.Collapsed;
                
                // Show selected proveedor panel
                SelectedProveedorPanel.Visibility = Visibility.Visible;
                SelectedProveedorIdTextBlock.Text = $"ID: {SelectedProveedor.IdProveedor?.ToString() ?? "N/A"}";
                SelectedProveedorNombreTextBlock.Text = SelectedProveedor.NombreComercial ?? string.Empty;
                SelectedProveedorCostoTextBlock.Text = $"Costo: ${SelectedProveedor.Costo?.ToString() ?? "N/A"}";
                
                // Notify the cost changed
                CostoChanged?.Invoke(this, SelectedProveedor.Costo);
            }
            else if (SelectedRefaccion != null)
            {
                // When proveedor is deselected, revert to refaccion cost
                CostoChanged?.Invoke(this, SelectedRefaccion.Costo);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón para mostrar la lista de proveedores nuevamente
        /// </summary>
        private void ShowProveedoresListButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide selected proveedor panel
            SelectedProveedorPanel.Visibility = Visibility.Collapsed;
            
            // Show proveedores button and grid
            ProveedoresPanel.Visibility = Visibility.Visible;
            ProveedoresGrid.Visibility = Visibility.Visible;
            
            // Clear provider selection (but keep the list)
            ProveedoresListView.SelectedItem = null;
            SelectedProveedor = null;
            
            // Revert to refaccion cost
            if (SelectedRefaccion != null)
            {
                CostoChanged?.Invoke(this, SelectedRefaccion.Costo);
            }
        }

        /// <summary>
        /// Carga los proveedores y preselecciona el proveedor especificado si existe
        /// </summary>
        private async Task LoadAndPreselectProveedorAsync(int idRefaccion, int idProveedorPreselected)
        {
            try
            {
                // Load providers
                await LoadProveedoresAsync(idRefaccion);
                
                // Find the provider with the preselected ID
                var proveedorToSelect = _proveedores.FirstOrDefault(p => p.IdProveedor == idProveedorPreselected);
                
                if (proveedorToSelect != null)
                {
                    // Select the provider in the ListView (this will trigger SelectionChanged)
                    ProveedoresListView.SelectedItem = proveedorToSelect;
                    
                    // Since selection triggers the event handler, the UI will be updated automatically
                }
                else
                {
                    // Provider not found in the list, show manual selection UI
                    ProveedoresPanel.Visibility = Visibility.Visible;
                    ProveedoresGrid.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al preseleccionar proveedor: {ex.GetType().Name} - {ex.Message}");
                // On error, show manual selection UI
                ProveedoresPanel.Visibility = Visibility.Visible;
            }
        }
    }
}
