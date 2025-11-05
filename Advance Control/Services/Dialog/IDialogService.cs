using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Advance_Control.Services.Dialog
{
    /// <summary>
    /// Servicio para mostrar diálogos con UserControls personalizados.
    /// Permite lanzar cualquier UserControl con o sin parámetros, y obtener resultados genéricos.
    /// Cuando no se configuran botones, el diálogo se cierra automáticamente al hacer clic fuera (light dismiss).
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Muestra un diálogo con un UserControl sin parámetros y sin resultado específico.
        /// Si no se especifican botones (primaryButtonText, secondaryButtonText, closeButtonText),
        /// el diálogo se mostrará con light dismiss habilitado (se cierra al hacer clic fuera).
        /// </summary>
        /// <typeparam name="TUserControl">El tipo de UserControl a mostrar.</typeparam>
        /// <param name="title">Título del diálogo (opcional).</param>
        /// <param name="primaryButtonText">Texto del botón primario (opcional).</param>
        /// <param name="secondaryButtonText">Texto del botón secundario (opcional).</param>
        /// <param name="closeButtonText">Texto del botón de cerrar (opcional).</param>
        /// <returns>True si el usuario presionó el botón primario, false en caso contrario.</returns>
        /// <example>
        /// <code>
        /// // Ejemplo: Mostrar un diálogo simple sin parámetros ni resultado específico
        /// var result = await _dialogService.ShowDialogAsync&lt;MyUserControl&gt;(
        ///     title: "Mi Diálogo",
        ///     primaryButtonText: "Aceptar",
        ///     closeButtonText: "Cancelar"
        /// );
        /// if (result)
        /// {
        ///     // Usuario presionó Aceptar
        /// }
        /// 
        /// // Ejemplo: Diálogo con light dismiss (sin botones)
        /// await _dialogService.ShowDialogAsync&lt;NotificationUserControl&gt;(
        ///     title: "Notificación"
        ///     // Sin botones - se cierra al hacer clic fuera
        /// );
        /// </code>
        /// </example>
        Task<bool> ShowDialogAsync<TUserControl>(
            string? title = null,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = null
        ) where TUserControl : UserControl, new();

        /// <summary>
        /// Muestra un diálogo con un UserControl que recibe parámetros y no devuelve resultado específico.
        /// </summary>
        /// <typeparam name="TUserControl">El tipo de UserControl a mostrar.</typeparam>
        /// <param name="configureControl">Acción para configurar el UserControl antes de mostrarlo.</param>
        /// <param name="title">Título del diálogo (opcional).</param>
        /// <param name="primaryButtonText">Texto del botón primario (opcional).</param>
        /// <param name="secondaryButtonText">Texto del botón secundario (opcional).</param>
        /// <param name="closeButtonText">Texto del botón de cerrar (opcional).</param>
        /// <returns>True si el usuario presionó el botón primario, false en caso contrario.</returns>
        /// <example>
        /// <code>
        /// // Ejemplo: Mostrar un diálogo con parámetros pero sin resultado específico
        /// var result = await _dialogService.ShowDialogAsync&lt;MyUserControl&gt;(
        ///     configureControl: control =&gt; 
        ///     {
        ///         control.Username = "admin";
        ///         control.IsEditMode = true;
        ///     },
        ///     title: "Editar Usuario",
        ///     primaryButtonText: "Guardar",
        ///     closeButtonText: "Cancelar"
        /// );
        /// </code>
        /// </example>
        Task<bool> ShowDialogAsync<TUserControl>(
            Action<TUserControl> configureControl,
            string? title = null,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = null
        ) where TUserControl : UserControl, new();

        /// <summary>
        /// Muestra un diálogo con un UserControl sin parámetros y obtiene un resultado genérico.
        /// </summary>
        /// <typeparam name="TUserControl">El tipo de UserControl a mostrar.</typeparam>
        /// <typeparam name="TResult">El tipo de resultado esperado.</typeparam>
        /// <param name="getResult">Función para obtener el resultado del UserControl.</param>
        /// <param name="title">Título del diálogo (opcional).</param>
        /// <param name="primaryButtonText">Texto del botón primario (opcional).</param>
        /// <param name="secondaryButtonText">Texto del botón secundario (opcional).</param>
        /// <param name="closeButtonText">Texto del botón de cerrar (opcional).</param>
        /// <returns>El resultado del tipo especificado si el usuario presionó el botón primario, default(TResult) en caso contrario.</returns>
        /// <example>
        /// <code>
        /// // Ejemplo: Obtener un resultado de tipo string del UserControl
        /// var username = await _dialogService.ShowDialogAsync&lt;LoginUserControl, string&gt;(
        ///     getResult: control =&gt; control.EnteredUsername,
        ///     title: "Iniciar Sesión",
        ///     primaryButtonText: "Entrar",
        ///     closeButtonText: "Cancelar"
        /// );
        /// if (!string.IsNullOrEmpty(username))
        /// {
        ///     // Procesar el nombre de usuario
        /// }
        /// </code>
        /// </example>
        Task<TResult?> ShowDialogAsync<TUserControl, TResult>(
            Func<TUserControl, TResult> getResult,
            string? title = null,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = null
        ) where TUserControl : UserControl, new();

        /// <summary>
        /// Muestra un diálogo con un UserControl que recibe parámetros y devuelve un resultado genérico.
        /// </summary>
        /// <typeparam name="TUserControl">El tipo de UserControl a mostrar.</typeparam>
        /// <typeparam name="TResult">El tipo de resultado esperado.</typeparam>
        /// <param name="configureControl">Acción para configurar el UserControl antes de mostrarlo.</param>
        /// <param name="getResult">Función para obtener el resultado del UserControl.</param>
        /// <param name="title">Título del diálogo (opcional).</param>
        /// <param name="primaryButtonText">Texto del botón primario (opcional).</param>
        /// <param name="secondaryButtonText">Texto del botón secundario (opcional).</param>
        /// <param name="closeButtonText">Texto del botón de cerrar (opcional).</param>
        /// <returns>El resultado del tipo especificado si el usuario presionó el botón primario, default(TResult) en caso contrario.</returns>
        /// <example>
        /// <code>
        /// // Ejemplo: UserControl con parámetros y resultado de tipo bool
        /// var confirmed = await _dialogService.ShowDialogAsync&lt;ConfirmDeleteUserControl, bool&gt;(
        ///     configureControl: control =&gt; 
        ///     {
        ///         control.ItemName = "Cliente ABC";
        ///         control.ItemId = 123;
        ///     },
        ///     getResult: control =&gt; control.IsConfirmed,
        ///     title: "Confirmar Eliminación",
        ///     primaryButtonText: "Eliminar",
        ///     closeButtonText: "Cancelar"
        /// );
        /// 
        /// // Ejemplo: Obtener una lista de items seleccionados
        /// var selectedItems = await _dialogService.ShowDialogAsync&lt;ItemSelectorUserControl, List&lt;int&gt;&gt;(
        ///     configureControl: control =&gt; 
        ///     {
        ///         control.AvailableItems = allItems;
        ///         control.MaxSelection = 5;
        ///     },
        ///     getResult: control =&gt; control.SelectedItemIds,
        ///     title: "Seleccionar Items",
        ///     primaryButtonText: "Confirmar",
        ///     closeButtonText: "Cancelar"
        /// );
        /// </code>
        /// </example>
        Task<TResult?> ShowDialogAsync<TUserControl, TResult>(
            Action<TUserControl> configureControl,
            Func<TUserControl, TResult> getResult,
            string? title = null,
            string? primaryButtonText = null,
            string? secondaryButtonText = null,
            string? closeButtonText = null
        ) where TUserControl : UserControl, new();
    }
}
