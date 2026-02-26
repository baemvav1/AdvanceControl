using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Logging;
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
            IActivityService activityService)
        {
            _userSessionService = userSessionService ?? throw new ArgumentNullException(nameof(userSessionService));
            _logger             = logger             ?? throw new ArgumentNullException(nameof(logger));
            _activityService    = activityService    ?? throw new ArgumentNullException(nameof(activityService));
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

                if (!_userSessionService.IsLoaded)
                    await _userSessionService.LoadAsync(cancellationToken);

                if (_userSessionService.CredencialId <= 0)
                {
                    _isActividadEmpty = true;
                    return;
                }

                var items = await _activityService.GetActividadAsync(
                    _userSessionService.CredencialId,
                    cancellationToken);

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
    }
}
