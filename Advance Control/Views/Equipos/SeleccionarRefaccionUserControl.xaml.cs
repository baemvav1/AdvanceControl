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
        private void RefaccionesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedRefaccion = RefaccionesListView.SelectedItem as RefaccionDto;
        }
    }
}
