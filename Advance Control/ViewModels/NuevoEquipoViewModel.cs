using System;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para el formulario de nuevo equipo.
    /// Gestiona los datos del equipo y la validación del formulario.
    /// </summary>
    public class NuevoEquipoViewModel : ViewModelBase
    {
        private readonly ILoggingService _logger;
        
        private string _marca = string.Empty;
        private string _creadoText = string.Empty;
        private string _paradasText = string.Empty;
        private string _kilogramosText = string.Empty;
        private string _personasText = string.Empty;
        private string _descripcion = string.Empty;
        private string _identificador = string.Empty;
        private bool _estatus = true;
        private string _errorMessage = string.Empty;

        public NuevoEquipoViewModel(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Marca del equipo
        /// </summary>
        public string Marca
        {
            get => _marca;
            set
            {
                if (SetProperty(ref _marca, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Año de creación (como texto para el TextBox)
        /// </summary>
        public string CreadoText
        {
            get => _creadoText;
            set
            {
                if (SetProperty(ref _creadoText, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Obtiene el año de creación como entero, o null si no es válido
        /// </summary>
        public int? Creado
        {
            get
            {
                if (int.TryParse(CreadoText, out var result) && result >= 1900 && result <= 2100)
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Número de paradas (como texto para el TextBox)
        /// </summary>
        public string ParadasText
        {
            get => _paradasText;
            set => SetProperty(ref _paradasText, value);
        }

        /// <summary>
        /// Obtiene el número de paradas como entero, o 0 si no es válido
        /// </summary>
        public int Paradas
        {
            get
            {
                if (int.TryParse(ParadasText, out var result) && result >= 0)
                {
                    return result;
                }
                return 0;
            }
        }

        /// <summary>
        /// Capacidad en kilogramos (como texto para el TextBox)
        /// </summary>
        public string KilogramosText
        {
            get => _kilogramosText;
            set => SetProperty(ref _kilogramosText, value);
        }

        /// <summary>
        /// Obtiene la capacidad en kilogramos como entero, o 0 si no es válido
        /// </summary>
        public int Kilogramos
        {
            get
            {
                if (int.TryParse(KilogramosText, out var result) && result >= 0)
                {
                    return result;
                }
                return 0;
            }
        }

        /// <summary>
        /// Capacidad de personas (como texto para el TextBox)
        /// </summary>
        public string PersonasText
        {
            get => _personasText;
            set => SetProperty(ref _personasText, value);
        }

        /// <summary>
        /// Obtiene la capacidad de personas como entero, o 0 si no es válido
        /// </summary>
        public int Personas
        {
            get
            {
                if (int.TryParse(PersonasText, out var result) && result >= 0)
                {
                    return result;
                }
                return 0;
            }
        }

        /// <summary>
        /// Descripción del equipo
        /// </summary>
        public string Descripcion
        {
            get => _descripcion;
            set => SetProperty(ref _descripcion, value);
        }

        /// <summary>
        /// Identificador único del equipo
        /// </summary>
        public string Identificador
        {
            get => _identificador;
            set
            {
                if (SetProperty(ref _identificador, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        /// <summary>
        /// Estatus del equipo
        /// </summary>
        public bool Estatus
        {
            get => _estatus;
            set => SetProperty(ref _estatus, value);
        }

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        public string ErrorMessage
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
        /// Indica si se puede guardar (validación básica)
        /// </summary>
        public bool CanSave => !string.IsNullOrWhiteSpace(Marca) && 
                               Creado.HasValue && 
                               !string.IsNullOrWhiteSpace(Identificador);

        /// <summary>
        /// Valida los datos del formulario
        /// </summary>
        /// <returns>True si los datos son válidos, false en caso contrario</returns>
        public bool ValidateForm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Marca))
            {
                ErrorMessage = "La marca es obligatoria.";
                return false;
            }

            if (Marca.Length > 100)
            {
                ErrorMessage = "La marca no puede tener más de 100 caracteres.";
                return false;
            }

            if (!Creado.HasValue)
            {
                ErrorMessage = "El año de creación es obligatorio y debe estar entre 1900 y 2100.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Identificador))
            {
                ErrorMessage = "El identificador es obligatorio.";
                return false;
            }

            if (Identificador.Length > 50)
            {
                ErrorMessage = "El identificador no puede tener más de 50 caracteres.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Descripcion) && Descripcion.Length > 500)
            {
                ErrorMessage = "La descripción no puede tener más de 500 caracteres.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Limpia los datos del formulario
        /// </summary>
        public void ClearForm()
        {
            Marca = string.Empty;
            CreadoText = string.Empty;
            ParadasText = string.Empty;
            KilogramosText = string.Empty;
            PersonasText = string.Empty;
            Descripcion = string.Empty;
            Identificador = string.Empty;
            Estatus = true;
            ErrorMessage = string.Empty;
        }
    }
}
