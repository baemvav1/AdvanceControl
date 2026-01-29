using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Refacciones;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// UserControl para seleccionar una refacción de la lista
    /// </summary>
    public sealed partial class SeleccionarRefaccionUserControl : UserControl
    {
        private readonly IRefaccionService _refaccionService;
        private List<RefaccionDto> _allRefacciones = new();
        private bool _isDataLoaded = false;
        private bool _hasProveedores = false;

        public SeleccionarRefaccionUserControl()
        {
            this.InitializeComponent();
            
            // Resolver el servicio de refacciones desde DI
            _refaccionService = ((App)Application.Current).Host.Services.GetRequiredService<IRefaccionService>();
            
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
                SelectedCostoTextBlock.Text = $"Costo: ${SelectedRefaccion.Costo}";
                
                // Notify costo change
                CostoChanged?.Invoke(this, SelectedRefaccion.Costo);
                
                // Check if proveedor exists
                try
                {
                    _hasProveedores = await _refaccionService.CheckProveedorExistsAsync(SelectedRefaccion.IdRefaccion);
                    
                    if (_hasProveedores)
                    {
                        ProveedoresPanel.Visibility = Visibility.Visible;
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
            ProveedoresPanel.Visibility = Visibility.Collapsed;
            ProveedoresGrid.Visibility = Visibility.Collapsed;
            
            // Clear selection
            RefaccionesListView.SelectedItem = null;
            SelectedRefaccion = null;
            
            // Notify costo cleared
            CostoChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Maneja el clic en el botón de Proveedores
        /// </summary>
        private void ProveedoresButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle proveedores grid visibility
            ProveedoresGrid.Visibility = ProveedoresGrid.Visibility == Visibility.Visible 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }
    }
}
