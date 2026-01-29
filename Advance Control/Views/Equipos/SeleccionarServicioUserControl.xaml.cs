using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// UserControl para seleccionar un servicio de la lista
    /// </summary>
    public sealed partial class SeleccionarServicioUserControl : UserControl
    {
        private readonly IServicioService _servicioService;
        private List<ServicioDto> _allServicios = new();
        private bool _isDataLoaded = false;

        public SeleccionarServicioUserControl()
        {
            this.InitializeComponent();
            
            // Resolver el servicio de servicios desde DI
            _servicioService = ((App)Application.Current).Host.Services.GetRequiredService<IServicioService>();
            
            // Cargar servicios al inicializar
            this.Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Solo cargar una vez
            if (!_isDataLoaded)
            {
                await LoadServiciosAsync();
                _isDataLoaded = true;
            }
        }

        /// <summary>
        /// Servicio seleccionado actualmente
        /// </summary>
        public ServicioDto? SelectedServicio { get; private set; }

        /// <summary>
        /// Indica si hay un servicio seleccionado
        /// </summary>
        public bool HasSelection => SelectedServicio != null;

        /// <summary>
        /// Carga la lista de servicios desde el servicio
        /// </summary>
        private async Task LoadServiciosAsync()
        {
            try
            {
                LoadingRing.IsActive = true;
                ServiciosListView.Visibility = Visibility.Collapsed;

                _allServicios = await _servicioService.GetServiciosAsync(null, CancellationToken.None);

                ServiciosListView.ItemsSource = _allServicios;
                ServiciosListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar lista vacía con mensaje informativo
                _allServicios = new List<ServicioDto>();
                ServiciosListView.ItemsSource = _allServicios;
                ServiciosListView.Visibility = Visibility.Visible;
                
                // Log del error para diagnóstico
                System.Diagnostics.Debug.WriteLine($"Error al cargar servicios: {ex.GetType().Name} - {ex.Message}");
                
                // Mostrar mensaje de error en el placeholder de búsqueda
                ConceptoTextBox.PlaceholderText = "Error al cargar servicios. Intente nuevamente.";
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
            var conceptoSearch = ConceptoTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var descripcionSearch = DescripcionTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(conceptoSearch) && string.IsNullOrWhiteSpace(descripcionSearch))
            {
                ServiciosListView.ItemsSource = _allServicios;
            }
            else
            {
                // Filtrar servicios localmente
                var filtered = _allServicios.Where(sv =>
                {
                    bool matchesConcepto = string.IsNullOrWhiteSpace(conceptoSearch) || 
                                          (sv.Concepto?.ToLowerInvariant().Contains(conceptoSearch) == true);
                    
                    bool matchesDescripcion = string.IsNullOrWhiteSpace(descripcionSearch) || 
                                             (sv.Descripcion?.ToLowerInvariant().Contains(descripcionSearch) == true);
                    
                    return matchesConcepto && matchesDescripcion;
                }).ToList();

                ServiciosListView.ItemsSource = filtered;
            }
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de servicios
        /// </summary>
        private void ServiciosListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedServicio = ServiciosListView.SelectedItem as ServicioDto;
        }
    }
}
