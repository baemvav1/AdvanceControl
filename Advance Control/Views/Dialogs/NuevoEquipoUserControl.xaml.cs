using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;
using Advance_Control.Services.Equipos;
using Advance_Control.Utilities;
using System;
using System.Threading.Tasks;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Vista para crear un nuevo equipo.
    /// </summary>
    public sealed partial class NuevoEquipoUserControl : UserControl
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

        private readonly EquiposViewModel _equiposViewModel;

        /// <summary>
        /// Constructor que recibe el ViewModel por inyección de dependencias
        /// </summary>
        public NuevoEquipoUserControl(NuevoEquipoViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel), 
                    "El NuevoEquipoViewModel no puede ser null. Asegúrese de que está registrado en el contenedor de DI.");
            }

            ViewModel = viewModel;
            _equiposViewModel = AppServices.Get<EquiposViewModel>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar el formulario
            if (!ViewModel.ValidateForm())
                return;

            try
            {
                // Llamar al API directamente desde el diálogo
                var success = await _equiposViewModel.CreateEquipoAsync(
                    ViewModel.Marca,
                    ViewModel.Creado!.Value,
                    ViewModel.Paradas,
                    ViewModel.Kilogramos,
                    ViewModel.Personas,
                    string.IsNullOrWhiteSpace(ViewModel.Descripcion) ? null : ViewModel.Descripcion,
                    string.IsNullOrWhiteSpace(ViewModel.Identificador) ? "" : ViewModel.Identificador,
                    ViewModel.Estatus,
                    ViewModel.IdUbicacion
                );

                if (success)
                {
                    SaveSuccessful = true;
                    CloseDialogAction?.Invoke();
                }
                else
                {
                    // Mostrar error sin cerrar el diálogo
                    ViewModel.ErrorMessage = _equiposViewModel.ErrorMessage 
                        ?? "No se pudo crear el equipo. El identificador puede ya existir.";
                }
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"Error al crear el equipo: {ex.Message}";
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
