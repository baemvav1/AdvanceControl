using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Clientes;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// UserControl para seleccionar un cliente de la lista
    /// </summary>
    public sealed partial class SeleccionarClienteUserControl : UserControl
    {
        private readonly IClienteService _clienteService;
        private List<CustomerDto> _allClientes = new();

        public SeleccionarClienteUserControl()
        {
            this.InitializeComponent();
            
            // Resolver el servicio de clientes desde DI
            _clienteService = ((App)Application.Current).Host.Services.GetRequiredService<IClienteService>();
            
            // Cargar clientes al inicializar
            this.Loaded += async (s, e) => await LoadClientesAsync();
        }

        /// <summary>
        /// Cliente seleccionado actualmente
        /// </summary>
        public CustomerDto? SelectedCliente { get; private set; }

        /// <summary>
        /// Nota ingresada por el usuario
        /// </summary>
        public string? Nota => NotaTextBox.Text;

        /// <summary>
        /// Indica si hay un cliente seleccionado
        /// </summary>
        public bool HasSelection => SelectedCliente != null;

        /// <summary>
        /// Carga la lista de clientes desde el servicio
        /// </summary>
        private async Task LoadClientesAsync()
        {
            try
            {
                LoadingRing.IsActive = true;
                ClientesListView.Visibility = Visibility.Collapsed;

                _allClientes = await _clienteService.GetClientesAsync(null, CancellationToken.None);

                ClientesListView.ItemsSource = _allClientes;
                ClientesListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar lista vacía con mensaje informativo
                System.Diagnostics.Debug.WriteLine($"Error al cargar clientes: {ex.GetType().Name} - {ex.Message}");
                _allClientes = new List<CustomerDto>();
                ClientesListView.ItemsSource = _allClientes;
                ClientesListView.Visibility = Visibility.Visible;
                
                // Mostrar mensaje de error en el placeholder de búsqueda
                SearchTextBox.PlaceholderText = "Error al cargar clientes. Intente nuevamente.";
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
                ClientesListView.ItemsSource = _allClientes;
            }
            else
            {
                // Filtrar clientes localmente
                var filtered = _allClientes.Where(c =>
                    (c.NombreComercial?.ToLowerInvariant().Contains(searchText) == true) ||
                    (c.RazonSocial?.ToLowerInvariant().Contains(searchText) == true) ||
                    c.IdCliente.ToString().Contains(searchText)
                ).ToList();

                ClientesListView.ItemsSource = filtered;
            }
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de clientes
        /// </summary>
        private void ClientesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedCliente = ClientesListView.SelectedItem as CustomerDto;
            
            // Mostrar el panel de nota cuando hay un cliente seleccionado
            NotaPanel.Visibility = SelectedCliente != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
