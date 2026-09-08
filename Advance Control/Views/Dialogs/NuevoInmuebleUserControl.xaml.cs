using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.ViewModels;
using Advance_Control.Services.Inmuebles;
using Advance_Control.Utilities;
using System;
using System.Threading.Tasks;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Vista para crear un nuevo inmueble.
    /// </summary>
    public sealed partial class NuevoInmuebleUserControl : UserControl
    {
        /// <summary>
        /// ViewModel para el formulario de nuevo inmueble
        /// </summary>
        public NuevoInmuebleViewModel ViewModel { get; }

        /// <summary>
        /// Acción para cerrar el diálogo
        /// </summary>
        public Action? CloseDialogAction { get; set; }

        /// <summary>
        /// Indica si se guardó exitosamente
        /// </summary>
        public bool SaveSuccessful { get; private set; }

        private readonly InmueblesViewModel _inmueblesViewModel;
        private readonly IInmuebleService _inmuebleService;

        /// <summary>
        /// Constructor que recibe el ViewModel por inyección de dependencias
        /// </summary>
        public NuevoInmuebleUserControl(NuevoInmuebleViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel),
                    "El NuevoInmuebleViewModel no puede ser null. Asegúrese de que está registrado en el contenedor de DI.");
            }

            ViewModel = viewModel;
            _inmueblesViewModel = AppServices.Get<InmueblesViewModel>();
            _inmuebleService = AppServices.Get<IInmuebleService>();

            this.InitializeComponent();

            this.DataContext = ViewModel;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.ValidateForm())
                return;

            try
            {
                var success = await _inmueblesViewModel.CreateInmuebleAsync(
                    string.IsNullOrWhiteSpace(ViewModel.Descripcion) ? null : ViewModel.Descripcion,
                    string.IsNullOrWhiteSpace(ViewModel.Identificador) ? "" : ViewModel.Identificador,
                    ViewModel.IdTipoInmueble,
                    ViewModel.SuperficieM2,
                    string.IsNullOrWhiteSpace(ViewModel.CodigoPostal) ? null : ViewModel.CodigoPostal,
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
                    ViewModel.ErrorMessage = _inmueblesViewModel.ErrorMessage
                        ?? "No se pudo crear el inmueble. El identificador puede ya existir.";
                }
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"Error al crear el inmueble: {ex.Message}";
            }
        }

        private async void SugerirIdentificadorButton_Click(object sender, RoutedEventArgs e)
        {
            SugerirIdentificadorButton.IsEnabled = false;
            try
            {
                ViewModel.Identificador = await _inmuebleService.GetSiguienteIdentificadorAsync();
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"No se pudo sugerir un identificador: {ex.Message}";
            }
            finally
            {
                SugerirIdentificadorButton.IsEnabled = true;
            }
        }

        private void TipoInmuebleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TipoInmuebleComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                ViewModel.IdTipoInmueble = Convert.ToInt32(item.Tag);
            }
            else
            {
                ViewModel.IdTipoInmueble = null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearForm();
            SaveSuccessful = false;

            CloseDialogAction?.Invoke();
        }
    }
}
