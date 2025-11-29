using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Dialog;
using Advance_Control.Views.Dialogs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Advance_Control.Views
{
    /// <summary>
    /// Página para visualizar y gestionar equipos
    /// </summary>
    public sealed partial class EquiposView : Page
    {
        public EquiposViewModel ViewModel { get; }
        private readonly IDialogService _dialogService;

        public EquiposView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<EquiposViewModel>();
            
            // Resolver el DialogService desde DI
            _dialogService = ((App)Application.Current).Host.Services.GetRequiredService<IDialogService>();
            
            this.InitializeComponent();
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los equipos cuando se navega a esta página
            await ViewModel.LoadEquiposAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadEquiposAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the EquipoDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.EquipoDto equipo)
            {
                equipo.Expand = !equipo.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the EquipoDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.EquipoDto equipo)
            {
                equipo.Expand = !equipo.Expand;
            }
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar el diálogo para crear un nuevo equipo usando el DialogService
            var result = await _dialogService.ShowDialogAsync<NuevoEquipoDialog, NuevoEquipoResult>(
                getResult: control => new NuevoEquipoResult
                {
                    IsValid = control.ValidateFields(),
                    Marca = control.Marca,
                    Creado = control.GetCreadoAsInt(),
                    Descripcion = control.Descripcion,
                    Identificador = control.Identificador
                },
                title: "Nuevo Equipo",
                primaryButtonText: "Crear",
                closeButtonText: "Cancelar"
            );

            // Si el usuario presionó "Crear" y los datos son válidos
            if (result != null && result.IsValid)
            {
                await ViewModel.CreateEquipoAsync(
                    result.Marca,
                    result.Creado,
                    result.Descripcion,
                    result.Identificador
                );
            }
        }
    }

    /// <summary>
    /// Clase para almacenar el resultado del diálogo de nuevo equipo
    /// </summary>
    public class NuevoEquipoResult
    {
        public bool IsValid { get; set; }
        public string Marca { get; set; } = string.Empty;
        public int Creado { get; set; }
        public string? Descripcion { get; set; }
        public string Identificador { get; set; } = string.Empty;
    }
}
