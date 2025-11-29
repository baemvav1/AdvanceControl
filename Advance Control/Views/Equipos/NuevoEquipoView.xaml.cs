using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;
using System;

namespace Advance_Control.Views.Equipos
{
    /// <summary>
    /// Vista para crear un nuevo equipo.
    /// </summary>
    public sealed partial class NuevoEquipoView : UserControl
    {
        /// <summary>
        /// ViewModel para el formulario de nuevo equipo
        /// </summary>
        public NuevoEquipoViewModel ViewModel { get; }

        /// <summary>
        /// Acción para cerrar el diálogo
        /// </summary>
        public Action? CloseDialogAction { get; set; }

        /// <summary>
        /// Indica si se guardó exitosamente
        /// </summary>
        public bool SaveSuccessful { get; private set; }

        /// <summary>
        /// Constructor que recibe el ViewModel por inyección de dependencias
        /// </summary>
        /// <param name="viewModel">ViewModel de nuevo equipo</param>
        /// <exception cref="ArgumentNullException">Si viewModel es null</exception>
        public NuevoEquipoView(NuevoEquipoViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel), 
                    "El NuevoEquipoViewModel no puede ser null. Asegúrese de que está registrado en el contenedor de DI.");
            }

            ViewModel = viewModel;
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar el formulario
            if (ViewModel.ValidateForm())
            {
                SaveSuccessful = true;
                CloseDialogAction?.Invoke();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar el formulario antes de cerrar
            ViewModel.ClearForm();
            SaveSuccessful = false;
            
            // Cerrar el diálogo cuando se cancela
            CloseDialogAction?.Invoke();
        }
    }
}
