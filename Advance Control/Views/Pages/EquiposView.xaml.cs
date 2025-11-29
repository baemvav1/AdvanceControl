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
using Advance_Control.Views.Equipos;

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

        public EquiposView()
        {
            // Resolver el ViewModel desde DI
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<EquiposViewModel>();
            
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

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Resolver el ViewModel desde DI
            var nuevoEquipoViewModel = ((App)Application.Current).Host.Services.GetRequiredService<NuevoEquipoViewModel>();
            var nuevoEquipoView = new NuevoEquipoView(nuevoEquipoViewModel);
            
            // Crear el diálogo similar a como lo hace el Login
            var dialog = new ContentDialog
            {
                Title = "Nuevo Equipo",
                Content = nuevoEquipoView,
                XamlRoot = this.XamlRoot
            };

            // Configurar el cierre del diálogo desde el NuevoEquipoView
            nuevoEquipoView.CloseDialogAction = () => 
            {
                try
                {
                    var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                    if (dispatcherQueue != null)
                    {
                        _ = dispatcherQueue.TryEnqueue(() =>
                        {
                            try
                            {
                                dialog.Hide();
                            }
                            catch
                            {
                                // El diálogo ya puede estar cerrado
                            }
                        });
                    }
                    else
                    {
                        dialog.Hide();
                    }
                }
                catch
                {
                    // El diálogo ya puede estar cerrado
                }
            };

            await dialog.ShowAsync();

            // Si se guardó exitosamente, crear el equipo en el servidor
            if (nuevoEquipoView.SaveSuccessful && nuevoEquipoViewModel.Creado.HasValue)
            {
                await ViewModel.CreateEquipoAsync(
                    nuevoEquipoViewModel.Marca,
                    nuevoEquipoViewModel.Creado.Value,
                    string.IsNullOrWhiteSpace(nuevoEquipoViewModel.Descripcion) ? null : nuevoEquipoViewModel.Descripcion,
                    nuevoEquipoViewModel.Identificador,
                    nuevoEquipoViewModel.Estatus
                );
            }
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
    }
}

