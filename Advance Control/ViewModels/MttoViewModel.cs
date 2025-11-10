using System;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Mantenimiento (Mtto).
    /// Gestiona la lógica de presentación para operaciones de mantenimiento.
    /// </summary>
    public class MttoViewModel : ViewModelBase
    {
        private readonly ILoggingService _logger;
        private bool _isLoading;
        private string? _errorMessage;

        public MttoViewModel(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Indica si hay una operación en curso
        /// </summary>
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

        /// <summary>
        /// Inicializa los datos de la vista
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                
                await _logger.LogInformationAsync("Vista de Mantenimiento inicializada", "MttoViewModel", "InitializeAsync");
                
                // TODO: Cargar datos iniciales si es necesario
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al inicializar la vista de Mantenimiento.";
                await _logger.LogErrorAsync("Error al inicializar MttoViewModel", ex, "MttoViewModel", "InitializeAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
