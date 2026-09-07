using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Inmuebles;
using Advance_Control.Services.Relaciones;
using Advance_Control.Services.RelacionesInmueble;
using Advance_Control.Services.TipoMantenimiento;
using Microsoft.Extensions.DependencyInjection;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// UserControl para crear una nueva orden de servicio con búsqueda de equipo/inmueble y
    /// selección de cliente relacionado
    /// </summary>
    public sealed partial class NuevaOrdenServicioUserControl : UserControl
    {
        private readonly IEquipoService _equipoService;
        private readonly IInmuebleService _inmuebleService;
        private readonly IRelacionService _relacionService;
        private readonly IRelacionInmuebleService _relacionInmuebleService;
        private readonly ITipoMantenimientoService _tipoMantenimientoService;
        private List<EquipoDto> _allEquipos = new();
        private List<InmuebleDto> _allInmuebles = new();
        private List<TipoMantenimientoDto> _tiposMantenimiento = new();
        private EquipoDto? _selectedEquipo;
        private InmuebleDto? _selectedInmueble;
        private RelacionClienteDto? _selectedCliente;

        /// <summary>
        /// true = buscando por Equipo, false = buscando por Inmueble
        /// </summary>
        private bool _buscarPorEquipo = true;

        public NuevaOrdenServicioUserControl()
        {
            this.InitializeComponent();

            // Resolve services from DI
            _equipoService = ((App)Application.Current).Host.Services.GetRequiredService<IEquipoService>();
            _inmuebleService = ((App)Application.Current).Host.Services.GetRequiredService<IInmuebleService>();
            _relacionService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionService>();
            _relacionInmuebleService = ((App)Application.Current).Host.Services.GetRequiredService<IRelacionInmuebleService>();
            _tipoMantenimientoService = ((App)Application.Current).Host.Services.GetRequiredService<ITipoMantenimientoService>();

            // Fijar el estado inicial del selector Equipo/Inmueble después de InitializeComponent
            // (no en XAML con IsChecked="True": el evento Checked dispararía durante
            // InitializeComponent, antes de que EquipoPanel/InmueblePanel estén asignados)
            BuscarPorEquipoRadio.IsChecked = true;

            // Load equipment, inmuebles and tipos when control is loaded
            this.Loaded += async (s, e) =>
            {
                await Task.WhenAll(LoadEquiposAsync(), LoadInmueblesAsync(), LoadTiposMantenimientoAsync());
            };
        }

        #region Public Properties

        /// <summary>
        /// Gets the selected maintenance type ID from the dynamically loaded combobox
        /// </summary>
        public int? IdTipoMantenimiento
        {
            get
            {
                if (TipoMantenimientoComboBox.SelectedItem is TipoMantenimientoDto selectedTipo)
                {
                    return selectedTipo.Id;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the selected equipment ID (null si se está buscando por inmueble)
        /// </summary>
        public int? IdEquipo => _buscarPorEquipo ? _selectedEquipo?.IdEquipo : null;

        /// <summary>
        /// Gets the selected inmueble ID (null si se está buscando por equipo)
        /// </summary>
        public int? IdInmueble => _buscarPorEquipo ? null : _selectedInmueble?.IdInmueble;

        /// <summary>
        /// Gets the selected client ID
        /// </summary>
        public int? IdCliente => _selectedCliente?.IdCliente;

        /// <summary>
        /// Gets the note text
        /// </summary>
        public string? Nota => string.IsNullOrWhiteSpace(NotaTextBox.Text) ? null : NotaTextBox.Text;

        /// <summary>
        /// Indicates if all required fields are filled
        /// </summary>
        public bool IsValid =>
            IdTipoMantenimiento.HasValue &&
            (IdEquipo.HasValue || IdInmueble.HasValue) &&
            IdCliente.HasValue;

        #endregion

        #region Tipos Mantenimiento Loading

        /// <summary>
        /// Carga los tipos de mantenimiento desde la API
        /// </summary>
        private async Task LoadTiposMantenimientoAsync()
        {
            try
            {
                _tiposMantenimiento = await _tipoMantenimientoService.GetTiposMantenimientoAsync();
                TipoMantenimientoComboBox.ItemsSource = _tiposMantenimiento;

                if (_tiposMantenimiento.Count > 0)
                    TipoMantenimientoComboBox.SelectedIndex = 0;
            }
            catch (Exception)
            {
                // Si falla la carga, el combobox queda vacío
            }
        }

        #endregion

        #region Equipo / Inmueble toggle

        private void BuscarPorEquipoRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (EquipoPanel == null || InmueblePanel == null)
                return;

            _buscarPorEquipo = true;
            EquipoPanel.Visibility = Visibility.Visible;
            InmueblePanel.Visibility = Visibility.Collapsed;

            // Limpiar la selección del lado inmueble y recalcular clientes según el equipo actual
            _selectedInmueble = null;
            SelectedInmuebleInfo.Visibility = Visibility.Collapsed;
            InmuebleAutoSuggestBox.Text = string.Empty;
            _ = LoadClientesRelacionadosAsync(_selectedEquipo?.Identificador);
        }

        private void BuscarPorInmuebleRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (EquipoPanel == null || InmueblePanel == null)
                return;

            _buscarPorEquipo = false;
            EquipoPanel.Visibility = Visibility.Collapsed;
            InmueblePanel.Visibility = Visibility.Visible;

            // Limpiar la selección del lado equipo y recalcular clientes según el inmueble actual
            _selectedEquipo = null;
            SelectedEquipoInfo.Visibility = Visibility.Collapsed;
            EquipoAutoSuggestBox.Text = string.Empty;
            _ = LoadClientesRelacionadosAsync(_selectedInmueble?.Identificador);
        }

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
            catch (Exception ex)
            {
                // En caso de error, mostrar lista vacía
                _allEquipos = new List<EquipoDto>();
                System.Diagnostics.Debug.WriteLine($"Error al cargar equipos: {ex.GetType().Name} - {ex.Message}");
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

        #region Inmueble Loading and Search

        /// <summary>
        /// Loads inmuebles from the service
        /// </summary>
        private async Task LoadInmueblesAsync()
        {
            try
            {
                InmuebleLoadingRing.IsActive = true;

                _allInmuebles = await _inmuebleService.GetInmueblesAsync(null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _allInmuebles = new List<InmuebleDto>();
                System.Diagnostics.Debug.WriteLine($"Error al cargar inmuebles: {ex.GetType().Name} - {ex.Message}");
            }
            finally
            {
                InmuebleLoadingRing.IsActive = false;
            }
        }

        /// <summary>
        /// Handles text changes in the inmueble search box
        /// </summary>
        private void InmuebleAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchText = sender.Text?.Trim().ToLowerInvariant() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    sender.ItemsSource = _allInmuebles.Take(10).Select(i => FormatInmuebleDisplay(i)).ToList();
                }
                else
                {
                    var filtered = _allInmuebles.Where(i =>
                        (i.Identificador?.ToLowerInvariant().Contains(searchText) == true) ||
                        (i.TipoInmueble?.ToLowerInvariant().Contains(searchText) == true) ||
                        (i.Descripcion?.ToLowerInvariant().Contains(searchText) == true) ||
                        i.IdInmueble.ToString().Contains(searchText)
                    ).Take(10).Select(i => FormatInmuebleDisplay(i)).ToList();

                    sender.ItemsSource = filtered;
                }
            }
        }

        /// <summary>
        /// Shows suggestions when the inmueble search box gets focus
        /// </summary>
        private void InmuebleAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox autoSuggestBox)
            {
                autoSuggestBox.ItemsSource = _allInmuebles.Take(10).Select(im => FormatInmuebleDisplay(im)).ToList();
                autoSuggestBox.IsSuggestionListOpen = true;
            }
        }

        /// <summary>
        /// Handles inmueble suggestion selection
        /// </summary>
        private async void InmuebleAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedText)
            {
                var selectedInmueble = _allInmuebles.FirstOrDefault(i => FormatInmuebleDisplay(i) == selectedText);

                if (selectedInmueble != null)
                {
                    await SelectInmuebleAsync(selectedInmueble);
                }
            }
        }

        /// <summary>
        /// Formats inmueble display text for the AutoSuggestBox
        /// </summary>
        private static string FormatInmuebleDisplay(InmuebleDto inmueble)
        {
            return $"{inmueble.Identificador} - {inmueble.TipoInmueble}";
        }

        /// <summary>
        /// Handles clearing the inmueble selection
        /// </summary>
        private void ClearInmuebleSelection_Click(object sender, RoutedEventArgs e)
        {
            _selectedInmueble = null;
            _selectedCliente = null;

            InmuebleAutoSuggestBox.Text = string.Empty;
            SelectedInmuebleInfo.Visibility = Visibility.Collapsed;

            ClientesListView.ItemsSource = null;
            ClientesListView.Visibility = Visibility.Collapsed;
            NoEquipoSelectedMessage.Visibility = Visibility.Visible;
            NoClientesMessage.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Selects an inmueble and loads related clients
        /// </summary>
        private async Task SelectInmuebleAsync(InmuebleDto inmueble)
        {
            _selectedInmueble = inmueble;
            _selectedCliente = null;

            SelectedInmuebleIdentificador.Text = inmueble.Identificador ?? "Sin identificador";
            SelectedInmuebleTipo.Text = inmueble.TipoInmueble ?? "Sin tipo";
            SelectedInmuebleInfo.Visibility = Visibility.Visible;

            NoEquipoSelectedMessage.Visibility = Visibility.Collapsed;

            await LoadClientesRelacionadosAsync(inmueble.Identificador);
        }

        #endregion

        #region Client Loading

        /// <summary>
        /// Loads clients related to the selected equipment or inmueble (según el toggle activo)
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

                var relaciones = _buscarPorEquipo
                    ? await _relacionService.GetRelacionesAsync(identificador, 0, CancellationToken.None)
                    : await _relacionInmuebleService.GetRelacionesAsync(identificador, 0, CancellationToken.None);

                if (relaciones.Count > 0)
                {
                    ClientesListView.ItemsSource = relaciones;
                    ClientesListView.Visibility = Visibility.Visible;
                    NoClientesMessage.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ClientesListView.Visibility = Visibility.Collapsed;
                    NoClientesMessage.Text = "No hay clientes relacionados";
                    NoClientesMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ClientesListView.Visibility = Visibility.Collapsed;
                NoClientesMessage.Text = "Error al cargar clientes relacionados";
                NoClientesMessage.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Error al cargar clientes relacionados: {ex.GetType().Name} - {ex.Message}");
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
