using Microsoft.UI.Xaml.Controls;
using System;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Diálogo para crear un nuevo equipo.
    /// </summary>
    public sealed partial class NuevoEquipoDialog : UserControl
    {
        /// <summary>
        /// Marca del equipo (obligatorio)
        /// </summary>
        public string Marca { get; set; } = string.Empty;

        /// <summary>
        /// Identificador del equipo (obligatorio)
        /// </summary>
        public string Identificador { get; set; } = string.Empty;

        /// <summary>
        /// Año de creación del equipo (obligatorio)
        /// </summary>
        public double Creado { get; set; } = DateTime.Now.Year;

        /// <summary>
        /// Descripción del equipo (opcional)
        /// </summary>
        public string? Descripcion { get; set; }

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
