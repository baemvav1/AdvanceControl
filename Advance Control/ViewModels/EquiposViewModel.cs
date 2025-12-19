using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class EquiposViewModel : ViewModelBase
    {
        private readonly IEquipoService _equipoService;
        private readonly ILoggingService _logger;
        private ObservableCollection<EquipoDto> _equipos;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _marcaFilter;
        private string? _creadoFilterText;
        private string? _paradasFilterText;
        private string? _kilogramosFilterText;
        private string? _personasFilterText;
        private string? _descripcionFilter;
        private string? _identificadorFilter;

        public EquiposViewModel(IEquipoService equipoService, ILoggingService logger)
        {
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _equipos = new ObservableCollection<EquipoDto>();
        }

        public ObservableCollection<EquipoDto> Equipos
        {
            get => _equipos;
            set => SetProperty(ref _equipos, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
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

        /// <summary>
        /// Indica si hay un mensaje de error activo
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string? MarcaFilter
        {
            get => _marcaFilter;
            set => SetProperty(ref _marcaFilter, value);
        }

        public string? CreadoFilterText
        {
            get => _creadoFilterText;
            set => SetProperty(ref _creadoFilterText, value);
        }

        /// <summary>
        /// Gets the integer value of the CreadoFilterText, or null if empty/invalid.
        /// Values must be between 1900 and 2100 to be considered valid.
        /// </summary>
        private int? CreadoFilter
        {
            get
            {
                if (int.TryParse(CreadoFilterText, out var result) && result >= 1900 && result <= 2100)
                {
                    return result;
                }
                return null;
            }
        }

        public string? ParadasFilterText
        {
            get => _paradasFilterText;
            set => SetProperty(ref _paradasFilterText, value);
        }

        /// <summary>
        /// Gets the integer value of the ParadasFilterText, or null if empty/invalid
        /// </summary>
        private int? ParadasFilter
        {
            get
            {
                if (int.TryParse(ParadasFilterText, out var result) && result >= 0)
                {
                    return result;
                }
                return null;
            }
        }

        public string? KilogramosFilterText
        {
            get => _kilogramosFilterText;
            set => SetProperty(ref _kilogramosFilterText, value);
        }

        /// <summary>
        /// Gets the integer value of the KilogramosFilterText, or null if empty/invalid
        /// </summary>
        private int? KilogramosFilter
        {
            get
            {
                if (int.TryParse(KilogramosFilterText, out var result) && result >= 0)
                {
                    return result;
                }
                return null;
            }
        }

        public string? PersonasFilterText
        {
            get => _personasFilterText;
            set => SetProperty(ref _personasFilterText, value);
        }

        /// <summary>
        /// Gets the integer value of the PersonasFilterText, or null if empty/invalid
        /// </summary>
        private int? PersonasFilter
        {
            get
            {
                if (int.TryParse(PersonasFilterText, out var result) && result >= 0)
                {
                    return result;
                }
                return null;
            }
        }

        public string? DescripcionFilter
        {
            get => _descripcionFilter;
            set => SetProperty(ref _descripcionFilter, value);
        }

        public string? IdentificadorFilter
        {
            get => _identificadorFilter;
            set => SetProperty(ref _identificadorFilter, value);
        }

        /// <summary>
        /// Carga los equipos desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadEquiposAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null; // Limpiar errores anteriores
                await _logger.LogInformationAsync("Cargando equipos...", "EquiposViewModel", "LoadEquiposAsync");

                var query = new EquipoQueryDto
                {
                    Marca = MarcaFilter,
                    Creado = CreadoFilter,
                    Paradas = ParadasFilter,
                    Kilogramos = KilogramosFilter,
                    Personas = PersonasFilter,
                    Descripcion = DescripcionFilter,
                    Identificador = IdentificadorFilter
                };

                var equipos = await _equipoService.GetEquiposAsync(query, cancellationToken);

                Equipos.Clear();
                foreach (var equipo in equipos)
                {
                    Equipos.Add(equipo);
                }

                await _logger.LogInformationAsync($"Se cargaron {equipos.Count} equipos exitosamente", "EquiposViewModel", "LoadEquiposAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de equipos cancelada por el usuario", "EquiposViewModel", "LoadEquiposAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar equipos", ex, "EquiposViewModel", "LoadEquiposAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar equipos: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar equipos", ex, "EquiposViewModel", "LoadEquiposAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los equipos
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                MarcaFilter = null;
                CreadoFilterText = null;
                ParadasFilterText = null;
                KilogramosFilterText = null;
                PersonasFilterText = null;
                DescripcionFilter = null;
                IdentificadorFilter = null;
                ErrorMessage = null;
                await LoadEquiposAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar equipos.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "EquiposViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Elimina un equipo por su ID
        /// </summary>
        public async Task<bool> DeleteEquipoAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando equipo {id}...", "EquiposViewModel", "DeleteEquipoAsync");

                var result = await _equipoService.DeleteEquipoAsync(id, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Equipo {id} eliminado exitosamente", "EquiposViewModel", "DeleteEquipoAsync");
                    // Recargar la lista de equipos
                    await LoadEquiposAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo eliminar el equipo.";
                    await _logger.LogWarningAsync($"No se pudo eliminar el equipo {id}", "EquiposViewModel", "DeleteEquipoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar equipo: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar equipo {id}", ex, "EquiposViewModel", "DeleteEquipoAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza un equipo existente
        /// </summary>
        public async Task<bool> UpdateEquipoAsync(int id, EquipoQueryDto updateData, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando equipo {id}...", "EquiposViewModel", "UpdateEquipoAsync");

                var result = await _equipoService.UpdateEquipoAsync(id, updateData, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Equipo {id} actualizado exitosamente", "EquiposViewModel", "UpdateEquipoAsync");
                    // Recargar la lista de equipos
                    await LoadEquiposAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo actualizar el equipo.";
                    await _logger.LogWarningAsync($"No se pudo actualizar el equipo {id}", "EquiposViewModel", "UpdateEquipoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar equipo: {ex.Message}";
                await _logger.LogErrorAsync($"Error al actualizar equipo {id}", ex, "EquiposViewModel", "UpdateEquipoAsync");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo equipo
        /// </summary>
        public async Task<bool> CreateEquipoAsync(string marca, int creado = 0, int paradas = 0, int kilogramos = 0, int personas = 0, string? descripcion = null, string identificador = "", bool estatus = true, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Creando nuevo equipo...", "EquiposViewModel", "CreateEquipoAsync");

                var result = await _equipoService.CreateEquipoAsync(marca, creado, paradas, kilogramos, personas, descripcion, identificador, estatus, cancellationToken);

                if (result)
                {
                    await _logger.LogInformationAsync($"Equipo creado exitosamente", "EquiposViewModel", "CreateEquipoAsync");
                    // Recargar la lista de equipos
                    await LoadEquiposAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No se pudo crear el equipo.";
                    await _logger.LogWarningAsync($"No se pudo crear el equipo", "EquiposViewModel", "CreateEquipoAsync");
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al crear equipo: {ex.Message}";
                await _logger.LogErrorAsync($"Error al crear equipo", ex, "EquiposViewModel", "CreateEquipoAsync");
                return false;
            }
        }
    }
}
