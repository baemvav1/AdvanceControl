using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Areas;
using Advance_Control.Services.Levantamiento;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class LevantamientosViewModel : ViewModelBase
    {
        private readonly ILevantamientoApiService _apiService;
        private readonly IAreasService _areasService;
        private readonly ILoggingService _logger;

        private bool _isLoading;
        private string? _errorMessage;
        private ObservableCollection<AreaDto> _areas;
        private AreaDto? _selectedAreaFilter;

        // Lista completa sin filtrar (fuente de verdad)
        private List<LevantamientoListItemResponse> _todos = new();

        public LevantamientosViewModel(
            ILevantamientoApiService apiService,
            IAreasService areasService,
            ILoggingService logger)
        {
            _apiService   = apiService   ?? throw new ArgumentNullException(nameof(apiService));
            _areasService = areasService ?? throw new ArgumentNullException(nameof(areasService));
            _logger       = logger       ?? throw new ArgumentNullException(nameof(logger));
            Levantamientos = new ObservableCollection<LevantamientoListItemResponse>();
            _areas         = new ObservableCollection<AreaDto>();
        }

        public ObservableCollection<LevantamientoListItemResponse> Levantamientos { get; }

        public ObservableCollection<AreaDto> Areas
        {
            get => _areas;
            set => SetProperty(ref _areas, value);
        }

        public AreaDto? SelectedAreaFilter
        {
            get => _selectedAreaFilter;
            set => SetProperty(ref _selectedAreaFilter, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                    OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public bool IsEmpty => !IsLoading && Levantamientos.Count == 0;

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// Carga las áreas disponibles para el filtro ComboBox
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var areas = await _areasService.GetAreasAsync(activo: true, cancellationToken: cancellationToken);
                Areas.Clear();
                foreach (var a in areas)
                    Areas.Add(a);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar áreas en LevantamientosViewModel", ex,
                    nameof(LevantamientosViewModel), nameof(InitializeAsync));
            }
        }

        public async Task LoadLevantamientosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                _todos = (await _apiService.ListarLevantamientosAsync(cancellationToken))
                    .OrderByDescending(l => l.FechaCreacion)
                    .ToList();

                IEnumerable<LevantamientoListItemResponse> filtrados = _todos;

                if (SelectedAreaFilter != null)
                {
                    var ids = await _areasService.GetIdentificadoresEnAreaAsync(SelectedAreaFilter.IdArea, cancellationToken);
                    var set = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
                    filtrados = _todos.Where(l => !string.IsNullOrEmpty(l.EquipoIdentificador) && set.Contains(l.EquipoIdentificador));
                }

                Levantamientos.Clear();
                foreach (var item in filtrados)
                    Levantamientos.Add(item);

                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar levantamientos: {ex.Message}";
                await _logger.LogErrorAsync("Error al cargar levantamientos", ex,
                    nameof(LevantamientosViewModel), nameof(LoadLevantamientosAsync));
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<bool> DeleteLevantamientoAsync(int idLevantamiento, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _apiService.EliminarLevantamientoAsync(idLevantamiento, cancellationToken);
                if (result)
                {
                    await _logger.LogInformationAsync(
                        $"Levantamiento {idLevantamiento} eliminado",
                        nameof(LevantamientosViewModel), nameof(DeleteLevantamientoAsync));
                    await LoadLevantamientosAsync(cancellationToken);
                }
                return result;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar: {ex.Message}";
                await _logger.LogErrorAsync($"Error al eliminar levantamiento {idLevantamiento}", ex,
                    nameof(LevantamientosViewModel), nameof(DeleteLevantamientoAsync));
                return false;
            }
        }

        /// <summary>
        /// Busca el PDF mas reciente generado para un levantamiento.
        /// </summary>
        public string? BuscarReportePdf(int idLevantamiento)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var folder = Path.Combine(docs, "Advance Control", "Levantamientos", $"Levantamiento{idLevantamiento}");

            if (!Directory.Exists(folder))
                return null;

            return Directory.GetFiles(folder, "Reporte_Levantamiento_*.pdf")
                .OrderByDescending(File.GetLastWriteTime)
                .FirstOrDefault();
        }
    }
}
