using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.ViewModels;
using Advance_Control.Services.Equipos;
using Advance_Control.Utilities;
using System;
using System.Threading.Tasks;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Vista para crear un nuevo equipo, o editar uno existente cuando se
    /// construye con el equipo a editar (ver <see cref="ViewModel"/>.IsEditMode).
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
        private readonly IEquipoService _equipoService;

        /// <summary>
        /// Constructor que recibe el ViewModel por inyección de dependencias
        /// </summary>
        public NuevoEquipoUserControl(NuevoEquipoViewModel viewModel) : this(viewModel, null)
        {
        }

        /// <summary>
        /// Constructor para editar un equipo existente: precarga el formulario
        /// con sus datos actuales y, al guardar, actualiza en vez de crear.
        /// </summary>
        public NuevoEquipoUserControl(NuevoEquipoViewModel viewModel, EquipoDto? equipoAEditar)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel),
                    "El NuevoEquipoViewModel no puede ser null. Asegúrese de que está registrado en el contenedor de DI.");
            }

            ViewModel = viewModel;
            _equiposViewModel = AppServices.Get<EquiposViewModel>();
            _equipoService = AppServices.Get<IEquipoService>();

            if (equipoAEditar != null)
                ViewModel.CargarDesde(equipoAEditar);

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
                bool success;

                if (ViewModel.IsEditMode)
                {
                    var updateData = new EquipoQueryDto
                    {
                        Marca = ViewModel.Marca,
                        Creado = ViewModel.Creado,
                        Paradas = ViewModel.Paradas,
                        Kilogramos = ViewModel.Kilogramos,
                        Personas = ViewModel.Personas,
                        Descripcion = string.IsNullOrWhiteSpace(ViewModel.Descripcion) ? null : ViewModel.Descripcion,
                        Identificador = string.IsNullOrWhiteSpace(ViewModel.Identificador) ? null : ViewModel.Identificador,
                        Controlador = string.IsNullOrWhiteSpace(ViewModel.Controlador) ? null : ViewModel.Controlador,
                        TipoPuerta = string.IsNullOrWhiteSpace(ViewModel.TipoPuerta) ? null : ViewModel.TipoPuerta,
                        Velocidad = string.IsNullOrWhiteSpace(ViewModel.Velocidad) ? null : ViewModel.Velocidad,
                        TipoMaquina = string.IsNullOrWhiteSpace(ViewModel.TipoMaquina) ? null : ViewModel.TipoMaquina,
                        Operador = string.IsNullOrWhiteSpace(ViewModel.Operador) ? null : ViewModel.Operador
                    };

                    success = await _equiposViewModel.UpdateEquipoAsync(ViewModel.IdEquipoEditando!.Value, updateData);

                    if (!success)
                    {
                        ViewModel.ErrorMessage = _equiposViewModel.ErrorMessage
                            ?? "No se pudo actualizar el equipo.";
                    }
                }
                else
                {
                    // Llamar al API directamente desde el diálogo
                    success = await _equiposViewModel.CreateEquipoAsync(
                        ViewModel.Marca,
                        ViewModel.Creado!.Value,
                        ViewModel.Paradas,
                        ViewModel.Kilogramos,
                        ViewModel.Personas,
                        string.IsNullOrWhiteSpace(ViewModel.Descripcion) ? null : ViewModel.Descripcion,
                        string.IsNullOrWhiteSpace(ViewModel.Identificador) ? "" : ViewModel.Identificador,
                        ViewModel.Estatus,
                        ViewModel.IdUbicacion,
                        string.IsNullOrWhiteSpace(ViewModel.Controlador) ? null : ViewModel.Controlador,
                        string.IsNullOrWhiteSpace(ViewModel.TipoPuerta) ? null : ViewModel.TipoPuerta,
                        string.IsNullOrWhiteSpace(ViewModel.Velocidad) ? null : ViewModel.Velocidad,
                        string.IsNullOrWhiteSpace(ViewModel.TipoMaquina) ? null : ViewModel.TipoMaquina,
                        string.IsNullOrWhiteSpace(ViewModel.Operador) ? null : ViewModel.Operador
                    );

                    if (!success)
                    {
                        ViewModel.ErrorMessage = _equiposViewModel.ErrorMessage
                            ?? "No se pudo crear el equipo. El identificador puede ya existir.";
                    }
                }

                if (success)
                {
                    SaveSuccessful = true;
                    CloseDialogAction?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = ViewModel.IsEditMode
                    ? $"Error al actualizar el equipo: {ex.Message}"
                    : $"Error al crear el equipo: {ex.Message}";
            }
        }

        private async void SugerirIdentificadorButton_Click(object sender, RoutedEventArgs e)
        {
            SugerirIdentificadorButton.IsEnabled = false;
            try
            {
                ViewModel.Identificador = await _equipoService.GetSiguienteIdentificadorAsync();
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
