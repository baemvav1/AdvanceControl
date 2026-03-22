using System;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class LevantamientoViewModel : ViewModelBase
    {
        private readonly ILoggingService _logger;
        private bool _isLoading;
        private string? _errorMessage;

        public LevantamientoViewModel(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public async Task InitializeAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                await _logger.LogInformationAsync("Levantamiento inicializado", nameof(LevantamientoViewModel), nameof(InitializeAsync));
            }
            catch (Exception ex)
            {
                ErrorMessage = "No se pudo inicializar el módulo de levantamiento.";
                await _logger.LogErrorAsync("Error al inicializar levantamiento", ex, nameof(LevantamientoViewModel), nameof(InitializeAsync));
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
