using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Logging;
using Advance_Control.Services.NotificationSettings;
using Advance_Control.Services.Session;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la página de inicio (Dashboard).
    /// Muestra bienvenida personalizada y actividad reciente del usuario.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IUserSessionService _userSessionService;
        private readonly ILoggingService _logger;
        private readonly IActivityService _activityService;
        private readonly INotificationSettingsService _notificationSettings;

        private string _saludo = "Bienvenido";
        private string _nombreUsuario = string.Empty;
        private string _tipoUsuario = string.Empty;
        private string _iniciales = string.Empty;
        private string _fechaHoy = string.Empty;
        private bool _isLoading;
        private bool _isActividadLoading;
        private bool _isActividadEmpty = true;

        public DashboardViewModel(
            IUserSessionService userSessionService,
            ILoggingService logger,
            IActivityService activityService,
            INotificationSettingsService notificationSettings)
        {
            _userSessionService = userSessionService ?? throw new ArgumentNullException(nameof(userSessionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
            _notificationSettings = notificationSettings ?? throw new ArgumentNullException(nameof(notificationSettings));
            ActividadReciente = new ObservableCollection<ActivityItem>();
        }

        public string Saludo
        {
            get => _saludo;
            set => SetProperty(ref _saludo, value);
        }

        public string NombreUsuario
        {
            get => _nombreUsuario;
            set => SetProperty(ref _nombreUsuario, value);
        }

        public string TipoUsuario
        {
            get => _tipoUsuario;
            set => SetProperty(ref _tipoUsuario, value);
        }

        public string Iniciales
        {
            get => _iniciales;
            set => SetProperty(ref _iniciales, value);
        }

        public string FechaHoy
        {
            get => _fechaHoy;
            set => SetProperty(ref _fechaHoy, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsActividadLoading
        {
            get => _isActividadLoading;
            set
            {
                SetProperty(ref _isActividadLoading, value);
                OnPropertyChanged(nameof(IsActividadEmpty));
            }
        }

        /// <summary>True cuando no hay actividad y no está cargando</summary>
        public bool IsActividadEmpty
        {
            get => !_isActividadLoading && _isActividadEmpty;
        }

        public ObservableCollection<ActivityItem> ActividadReciente { get; }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;

                if (!_userSessionService.IsLoaded)
                    await _userSessionService.LoadAsync(cancellationToken);

                var nombre = _userSessionService.NombreCompleto ?? string.Empty;
                NombreUsuario = nombre;
                TipoUsuario = _userSessionService.TipoUsuario ?? string.Empty;
                Iniciales = ObtenerIniciales(nombre);
                Saludo = ObtenerSaludo();
                FechaHoy = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy",
                    new System.Globalization.CultureInfo("es-MX"));
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar sesión en dashboard", ex,
                    "DashboardViewModel", "LoadAsync", "Sistema", "DashboardPage");
            }
            finally
            {
                IsLoading = false;
            }

            // Cargar actividad reciente de forma independiente
            await LoadActividadAsync(cancellationToken);
        }

        public async Task LoadActividadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsActividadLoading = true;
                ActividadReciente.Clear();
                _isActividadEmpty = true;

                var credId = _userSessionService.IsLoaded && _userSessionService.CredencialId > 0
                    ? _userSessionService.CredencialId
                    : (int?)null;

                var items = await _activityService.GetActividadRecienteAsync(
                    credencialId: credId,
                    top: 30,
                    cancellationToken: cancellationToken);

                foreach (var item in items)
                    ActividadReciente.Add(item);

                _isActividadEmpty = ActividadReciente.Count == 0;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar actividad reciente", ex,
                    "DashboardViewModel", "LoadActividadAsync", "Sistema", "DashboardPage");
            }
            finally
            {
                IsActividadLoading = false;
                OnPropertyChanged(nameof(IsActividadEmpty));
            }
        }

        private static string ObtenerIniciales(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return "?";
            var partes = nombreCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length switch
            {
                0 => "?",
                1 => partes[0][0].ToString().ToUpperInvariant(),
                _ => $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant()
            };
        }

        private static string ObtenerSaludo()
        {
            var hora = DateTime.Now.Hour;
            return hora switch
            {
                >= 6 and < 12 => "Buenos días",
                >= 12 and < 19 => "Buenas tardes",
                _ => "Buenas noches"
            };
        }

        /// <summary>
        /// Silencia las notificaciones de la categoría del item dado y lo elimina de la lista visible.
        /// </summary>
        public async Task SilenciarCategoriaAsync(ActivityItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Categoria)) return;
            try
            {
                await _notificationSettings.SetCategoryEnabledAsync(item.Categoria, false).ConfigureAwait(false);

                // Quitar todos los items de esa categoría de la lista visible
                var toRemove = ActividadReciente
                    .Where(i => string.Equals(i.Categoria, item.Categoria, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var r in toRemove)
                    ActividadReciente.Remove(r);

                _isActividadEmpty = ActividadReciente.Count == 0;
                OnPropertyChanged(nameof(IsActividadEmpty));
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error al silenciar categoría '{item.Categoria}'", ex,
                    "DashboardViewModel", "SilenciarCategoriaAsync");
            }
        }
    }
}
