using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Diálogo para crear un nuevo equipo.
    /// </summary>
    public sealed partial class NuevoEquipoDialog : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _marca = string.Empty;
        private string _identificador = string.Empty;
        private double _creado = DateTime.Now.Year;
        private string? _descripcion;

        /// <summary>
        /// Marca del equipo (obligatorio)
        /// </summary>
        public string Marca
        {
            get => _marca;
            set
            {
                if (_marca != value)
                {
                    _marca = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Identificador del equipo (obligatorio)
        /// </summary>
        public string Identificador
        {
            get => _identificador;
            set
            {
                if (_identificador != value)
                {
                    _identificador = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Año de creación del equipo (obligatorio)
        /// </summary>
        public double Creado
        {
            get => _creado;
            set
            {
                if (_creado != value)
                {
                    _creado = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Descripción del equipo (opcional)
        /// </summary>
        public string? Descripcion
        {
            get => _descripcion;
            set
            {
                if (_descripcion != value)
                {
                    _descripcion = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Constructor del diálogo
        /// </summary>
        public NuevoEquipoDialog()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Valida que los campos obligatorios estén completos
        /// </summary>
        /// <returns>True si la validación es exitosa, false en caso contrario</returns>
        public bool ValidateFields()
        {
            return !string.IsNullOrWhiteSpace(Marca) &&
                   !string.IsNullOrWhiteSpace(Identificador) &&
                   Creado >= 1900 && Creado <= 2100;
        }

        /// <summary>
        /// Obtiene el año de creación como entero
        /// </summary>
        public int GetCreadoAsInt()
        {
            return (int)Creado;
        }
    }
}
