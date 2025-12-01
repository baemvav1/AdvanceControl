using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Relaciones;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// UserControl para crear un nuevo mantenimiento con búsqueda de equipo y selección de cliente relacionado
    /// </summary>
    public sealed partial class NuevoMantenimientoUserControl : UserControl
    {
        private readonly IEquipoService _equipoService;
        private readonly IRelacionService _relacionService;
        private List<EquipoDto> _allEquipos = new();
        private EquipoDto? _selectedEquipo;
        private RelacionClienteDto? _selectedCliente;

        public NuevoMantenimientoUserControl()
        {
            this.InitializeComponent();

            // Resolve services from DI
            _equipoService = ((App)Application.Current).Host.Services.GetRequiredService<IEquipoService>();
            _relacionService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionService>();

            // Load equipment when control is loaded
            this.Loaded += async (s, e) => await LoadEquiposAsync();
        }

        #region Public Properties

        /// <summary>
        /// Gets the selected maintenance type ID (1: Correctivo, 2: Preventivo)
        /// </summary>
        public int? IdTipoMantenimiento
        {
            get
            {
                if (TipoMantenimientoComboBox.SelectedItem is ComboBoxItem selectedItem &&
                    selectedItem.Tag is string tagValue &&
                    int.TryParse(tagValue, out int result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the selected equipment ID
        /// </summary>
        public int? IdEquipo => _selectedEquipo?.IdEquipo;

        /// <summary>
        /// Gets the selected client ID
        /// </summary>
        public int? IdCliente => _selectedCliente?.IdCliente;

        /// <summary>
        /// Gets the cost value
        /// </summary>
        public double? Costo => double.IsNaN(CostoNumberBox.Value) ? null : CostoNumberBox.Value;

        /// <summary>
        /// Gets the note text
        /// </summary>
        public string? Nota => string.IsNullOrWhiteSpace(NotaTextBox.Text) ? null : NotaTextBox.Text;

        /// <summary>
        /// Indicates if all required fields are filled
        /// </summary>
        public bool IsValid =>
            IdTipoMantenimiento.HasValue &&
            IdEquipo.HasValue &&
            IdCliente.HasValue &&
            Costo.HasValue &&
            Costo.Value > 0;

        #endregion

        #region Equipment Loading and Search

        /// <summary>
        /// Loads equipment from the service
        /// </summary>
        private async Task LoadEquiposAsync()
        {
            try
            {
                EquipoLoadingRing.IsActive = true;

                _allEquipos = await _equipoService.GetEquiposAsync(null, CancellationToken.None);
            }
            catch (Exception)
            {
                _allEquipos = new List<EquipoDto>();
            }
            finally
            {
                EquipoLoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Handles text changes in the equipment search box
        /// </summary>
        private void EquipoAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchText = sender.Text?.Trim().ToLowerInvariant() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    sender.ItemsSource = _allEquipos.Take(10).Select(e => FormatEquipoDisplay(e)).ToList();
                }
                else
                {
                    var filtered = _allEquipos.Where(e =>
                        (e.Identificador?.ToLowerInvariant().Contains(searchText) == true) ||
                        (e.Marca?.ToLowerInvariant().Contains(searchText) == true) ||
                        (e.Descripcion?.ToLowerInvariant().Contains(searchText) == true) ||
                        e.IdEquipo.ToString().Contains(searchText)
                    ).Take(10).Select(e => FormatEquipoDisplay(e)).ToList();

                    sender.ItemsSource = filtered;
                }
            }
        }

        /// <summary>
        /// Shows suggestions when the equipment search box gets focus
        /// </summary>
        private void EquipoAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox autoSuggestBox)
            {
                autoSuggestBox.ItemsSource = _allEquipos.Take(10).Select(eq => FormatEquipoDisplay(eq)).ToList();
                autoSuggestBox.IsSuggestionListOpen = true;
            }
        }

        /// <summary>
        /// Handles equipment suggestion selection
        /// </summary>
        private async void EquipoAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedText)
            {
                // Find the equipment by matching the formatted display text
                var selectedEquipo = _allEquipos.FirstOrDefault(e => FormatEquipoDisplay(e) == selectedText);

                if (selectedEquipo != null)
                {
                    await SelectEquipoAsync(selectedEquipo);
                }
            }
        }

        /// <summary>
        /// Formats equipment display text for the AutoSuggestBox
        /// </summary>
        private static string FormatEquipoDisplay(EquipoDto equipo)
        {
            return $"{equipo.Identificador} - {equipo.Marca}";
        }

        /// <summary>
        /// Handles clearing the equipment selection
        /// </summary>
        private void ClearEquipoSelection_Click(object sender, RoutedEventArgs e)
        {
            _selectedEquipo = null;
            _selectedCliente = null;

            EquipoAutoSuggestBox.Text = string.Empty;
            SelectedEquipoInfo.Visibility = Visibility.Collapsed;

            // Reset client list
            ClientesListView.ItemsSource = null;
            ClientesListView.Visibility = Visibility.Collapsed;
            NoEquipoSelectedMessage.Visibility = Visibility.Visible;
            NoClientesMessage.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Selects an equipment and loads related clients
        /// </summary>
        private async Task SelectEquipoAsync(EquipoDto equipo)
        {
            _selectedEquipo = equipo;
            _selectedCliente = null;

            // Update equipment display
            SelectedEquipoIdentificador.Text = equipo.Identificador ?? "Sin identificador";
            SelectedEquipoMarca.Text = equipo.Marca ?? "Sin marca";
            SelectedEquipoInfo.Visibility = Visibility.Visible;

            // Hide the no equipment selected message
            NoEquipoSelectedMessage.Visibility = Visibility.Collapsed;

            // Load related clients
            await LoadClientesRelacionadosAsync(equipo.Identificador);
        }

        #endregion

        #region Client Loading

        /// <summary>
        /// Loads clients related to the selected equipment
        /// </summary>
        private async Task LoadClientesRelacionadosAsync(string? identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador))
            {
                ClientesListView.Visibility = Visibility.Collapsed;
                NoClientesMessage.Visibility = Visibility.Collapsed;
                NoEquipoSelectedMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                ClienteLoadingRing.IsActive = true;
                ClientesListView.Visibility = Visibility.Collapsed;
                NoClientesMessage.Visibility = Visibility.Collapsed;

                var relaciones = await _relacionService.GetRelacionesAsync(identificador, 0, CancellationToken.None);

                if (relaciones.Count > 0)
                {
                    ClientesListView.ItemsSource = relaciones;
                    ClientesListView.Visibility = Visibility.Visible;
                    NoClientesMessage.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ClientesListView.Visibility = Visibility.Collapsed;
                    NoClientesMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                ClientesListView.Visibility = Visibility.Collapsed;
                NoClientesMessage.Text = "Error al cargar clientes relacionados";
                NoClientesMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                ClienteLoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Handles client selection change
        /// </summary>
        private void ClientesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCliente = ClientesListView.SelectedItem as RelacionClienteDto;
        }

        #endregion
    }
}
