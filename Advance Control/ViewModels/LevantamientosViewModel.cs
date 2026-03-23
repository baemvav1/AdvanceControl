using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Levantamiento;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class LevantamientosViewModel : ViewModelBase
    {
        private readonly ILevantamientoApiService _apiService;
        private readonly ILoggingService _logger;

        private bool _isLoading;
        private string? _errorMessage;

        public LevantamientosViewModel(
            ILevantamientoApiService apiService,
            ILoggingService logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Levantamientos = new ObservableCollection<LevantamientoListItemResponse>();
        }

        public ObservableCollection<LevantamientoListItemResponse> Levantamientos { get; }

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

        public async Task LoadLevantamientosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var list = await _apiService.ListarLevantamientosAsync(cancellationToken);

                Levantamientos.Clear();
                foreach (var item in list.OrderByDescending(l => l.FechaCreacion))
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
