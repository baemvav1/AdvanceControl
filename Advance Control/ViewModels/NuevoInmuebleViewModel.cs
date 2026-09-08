using System;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para el formulario de nuevo inmueble.
    /// Gestiona los datos del inmueble y la validación del formulario.
    /// </summary>
    public class NuevoInmuebleViewModel : ViewModelBase
    {
        private readonly ILoggingService _logger;

        private string _identificador = string.Empty;
        private string _descripcion = string.Empty;
        private int? _idTipoInmueble = null;
        private string _superficieText = string.Empty;
        private string _codigoPostal = string.Empty;
        private bool _estatus = true;
        private int? _idUbicacion = null;
        private string _errorMessage = string.Empty;

        public NuevoInmuebleViewModel(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Identificador único del inmueble
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
        /// Descripción del inmueble
        /// </summary>
        public string Descripcion
        {
            get => _descripcion;
            set => SetProperty(ref _descripcion, value);
        }

        /// <summary>
        /// ID del tipo de inmueble seleccionado (ver tabla tipos_inmueble)
        /// </summary>
        public int? IdTipoInmueble
        {
            get => _idTipoInmueble;
            set => SetProperty(ref _idTipoInmueble, value);
        }

        /// <summary>
        /// Superficie en metros cuadrados (como texto para el TextBox, opcional)
        /// </summary>
        public string SuperficieText
        {
            get => _superficieText;
            set => SetProperty(ref _superficieText, value);
        }

        /// <summary>
        /// Obtiene la superficie como decimal, o null si está vacía o no es válida
        /// </summary>
        public double? SuperficieM2
        {
            get
            {
                if (double.TryParse(SuperficieText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) && result >= 0)
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Código postal del inmueble
        /// </summary>
        public string CodigoPostal
        {
            get => _codigoPostal;
            set => SetProperty(ref _codigoPostal, value);
        }

        /// <summary>
        /// Estatus del inmueble
        /// </summary>
        public bool Estatus
        {
            get => _estatus;
            set => SetProperty(ref _estatus, value);
        }

        /// <summary>
        /// ID de ubicación del inmueble (opcional)
        /// </summary>
        public int? IdUbicacion
        {
            get => _idUbicacion;
            set => SetProperty(ref _idUbicacion, value);
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
        /// Indica si se puede guardar. Ningún campo es estrictamente obligatorio
        /// (igual de permisivo que Equipos): el identificador se auto-genera si se deja vacío.
        /// </summary>
        public bool CanSave => true;

        /// <summary>
        /// Valida los datos del formulario
        /// </summary>
        public bool ValidateForm()
        {
            ErrorMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(Identificador) && Identificador.Length > 50)
            {
                ErrorMessage = "El identificador no puede tener más de 50 caracteres.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Descripcion) && Descripcion.Length > 500)
            {
                ErrorMessage = "La descripción no puede tener más de 500 caracteres.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(SuperficieText) && !SuperficieM2.HasValue)
            {
                ErrorMessage = "La superficie debe ser un número válido mayor o igual a 0.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CodigoPostal) && CodigoPostal.Length > 10)
            {
                ErrorMessage = "El código postal no puede tener más de 10 caracteres.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Limpia los datos del formulario
        /// </summary>
        public void ClearForm()
        {
            Identificador = string.Empty;
            Descripcion = string.Empty;
            IdTipoInmueble = null;
            SuperficieText = string.Empty;
            CodigoPostal = string.Empty;
            Estatus = true;
            IdUbicacion = null;
            ErrorMessage = string.Empty;
        }
    }
}
