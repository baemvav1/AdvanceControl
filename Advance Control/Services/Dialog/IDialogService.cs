using System;
using System.Threading.Tasks;

namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Servicio para mostrar diálogos en la aplicación.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Muestra el LoginView como un diálogo.
        /// </summary>
        /// <returns>Task que completa cuando el diálogo se cierra.</returns>
        Task ShowLoginDialogAsync();

        /// <summary>
        /// Muestra un diálogo de mensaje simple.
        /// </summary>
        /// <param name="title">Título del diálogo.</param>
        /// <param name="message">Mensaje a mostrar.</param>
        /// <returns>Task que completa cuando el diálogo se cierra.</returns>
        Task ShowMessageDialogAsync(string title, string message);

        /// <summary>
        /// Muestra un diálogo de confirmación.
        /// </summary>
        /// <param name="title">Título del diálogo.</param>
        /// <param name="message">Mensaje de confirmación.</param>
        /// <param name="primaryButtonText">Texto del botón principal (por defecto "Aceptar").</param>
        /// <param name="secondaryButtonText">Texto del botón secundario (por defecto "Cancelar").</param>
        /// <returns>True si el usuario selecciona el botón principal, false en caso contrario.</returns>
        Task<bool> ShowConfirmationDialogAsync(string title, string message, string primaryButtonText = "Aceptar", string secondaryButtonText = "Cancelar");
    }
}
