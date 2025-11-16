using System;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Asesoría.
    /// Gestiona la lógica de presentación para servicios de asesoría.
    /// </summary>
    public class AcesoriaViewModel : ViewModelBase
    {
        private readonly ILoggingService _logger;
        private bool _isLoading;
        private string? _errorMessage;
        private bool _isInitialized;

        public AcesoriaViewModel(ILoggingService logger)
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
        /// Inicializa los datos de la vista (solo se ejecuta en la primera carga)
        /// Para forzar una recarga, use InitializeAsync(forceReload: true)
        /// </summary>
        public async Task InitializeAsync(bool forceReload = false, CancellationToken cancellationToken = default)
        {
            // Prevenir inicialización redundante ya que ahora es Singleton
            if (_isInitialized && !forceReload) return;
            
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                
                await _logger.LogInformationAsync("Vista de Asesoría inicializada", "AcesoriaViewModel", "InitializeAsync");
                
                // TODO: Cargar datos iniciales si es necesario
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al inicializar la vista de Asesoría.";
                await _logger.LogErrorAsync("Error al inicializar AcesoriaViewModel", ex, "AcesoriaViewModel", "InitializeAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
