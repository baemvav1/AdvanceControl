using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// UserControl para seleccionar un equipo de la lista
    /// </summary>
    public sealed partial class SeleccionarEquipoUserControl : UserControl
    {
        private readonly IEquipoService _equipoService;
        private List<EquipoDto> _allEquipos = new();

        public SeleccionarEquipoUserControl()
        {
            this.InitializeComponent();
            
            // Resolver el servicio de equipos desde DI
            _equipoService = ((App)Application.Current).Host.Services.GetRequiredService<IEquipoService>();
            
            // Cargar equipos al inicializar
            this.Loaded += async (s, e) => await LoadEquiposAsync();
        }

        /// <summary>
        /// Equipo seleccionado actualmente
        /// </summary>
        public EquipoDto? SelectedEquipo { get; private set; }

        /// <summary>
        /// Indica si hay un equipo seleccionado
        /// </summary>
        public bool HasSelection => SelectedEquipo != null;

        /// <summary>
        /// Carga la lista de equipos desde el servicio
        /// </summary>
        private async Task LoadEquiposAsync()
        {
            try
            {
                LoadingRing.IsActive = true;
                EquiposListView.Visibility = Visibility.Collapsed;

                _allEquipos = await _equipoService.GetEquiposAsync(null, CancellationToken.None);

                EquiposListView.ItemsSource = _allEquipos;
                EquiposListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar lista vacía con mensaje informativo
                System.Diagnostics.Debug.WriteLine($"Error al cargar equipos: {ex.GetType().Name} - {ex.Message}");
                _allEquipos = new List<EquipoDto>();
                EquiposListView.ItemsSource = _allEquipos;
                EquiposListView.Visibility = Visibility.Visible;
                
                // Mostrar mensaje de error en el placeholder de búsqueda
                SearchTextBox.PlaceholderText = "Error al cargar equipos. Intente nuevamente.";
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Maneja el cambio de texto en el cuadro de búsqueda
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                EquiposListView.ItemsSource = _allEquipos;
            }
            else
            {
                // Filtrar equipos localmente
                var filtered = _allEquipos.Where(eq =>
                    (eq.Marca?.ToLowerInvariant().Contains(searchText) == true) ||
                    (eq.Identificador?.ToLowerInvariant().Contains(searchText) == true) ||
                    eq.IdEquipo.ToString().Contains(searchText)
                ).ToList();

                EquiposListView.ItemsSource = filtered;
            }
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de equipos
        /// </summary>
        private void EquiposListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedEquipo = EquiposListView.SelectedItem as EquipoDto;
        }
    }
}
