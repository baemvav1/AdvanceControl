using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Services.RelacionUsuarioArea;
using Advance_Control.Services.Areas;
using Advance_Control.Utilities;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar contactos
    /// </summary>
    public sealed partial class ContactosPage : Page
    {
        public ContactosViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly ILoggingService _loggingService;
        private readonly IRelacionUsuarioAreaService _relacionUsuarioAreaService;
        private readonly IAreasService _areasService;

        public ContactosPage()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<ContactosViewModel>();
            
            // Resolver servicios desde DI
            _notificacionService = AppServices.Get<INotificacionService>();
            _loggingService = AppServices.Get<ILoggingService>();
            _relacionUsuarioAreaService = AppServices.Get<IRelacionUsuarioAreaService>();
            _areasService = AppServices.Get<IAreasService>();
            
            this.InitializeComponent();
            ButtonClickLogger.Attach(this, _loggingService, nameof(ContactosPage));
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar los contactos cuando se navega a esta página
            await ViewModel.LoadContactosAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadContactosAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear los campos del formulario
            var nombreTextBox = new TextBox
            {
                PlaceholderText = "Nombre del contacto (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var apellidoTextBox = new TextBox
            {
                PlaceholderText = "Apellido del contacto",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var tratamientoTextBox = new TextBox
            {
                PlaceholderText = "Tratamiento (ej. Ing., Lic., Dr.)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var correoTextBox = new TextBox
            {
                PlaceholderText = "Correo electrónico",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var telefonoTextBox = new TextBox
            {
                PlaceholderText = "Teléfono",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var departamentoTextBox = new TextBox
            {
                PlaceholderText = "Departamento",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var cargoTextBox = new TextBox
            {
                PlaceholderText = "Cargo",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var codigoInternoTextBox = new TextBox
            {
                PlaceholderText = "Código interno",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var notasTextBox = new TextBox
            {
                PlaceholderText = "Notas adicionales (opcional)",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                MaxHeight = 150,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var idClienteNumberBox = new NumberBox
            {
                PlaceholderText = "ID del cliente asociado",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var idProveedorNumberBox = new NumberBox
            {
                PlaceholderText = "ID del proveedor asociado",
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var activoCheckBox = new CheckBox
            {
                Content = "Activo",
                IsChecked = true
            };

            var dialogContent = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Nombre:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        nombreTextBox,
                        new TextBlock { Text = "Apellido:" },
                        apellidoTextBox,
                        new TextBlock { Text = "Tratamiento:" },
                        tratamientoTextBox,
                        new TextBlock { Text = "Correo electrónico:" },
                        correoTextBox,
                        new TextBlock { Text = "Teléfono:" },
                        telefonoTextBox,
                        new TextBlock { Text = "Departamento:" },
                        departamentoTextBox,
                        new TextBlock { Text = "Cargo:" },
                        cargoTextBox,
                        new TextBlock { Text = "Código Interno:" },
                        codigoInternoTextBox,
                        new TextBlock { Text = "ID Cliente:" },
                        idClienteNumberBox,
                        new TextBlock { Text = "ID Proveedor:" },
                        idProveedorNumberBox,
                        new TextBlock { Text = "Notas:" },
                        notasTextBox,
                        activoCheckBox
                    }
                },
                MaxHeight = 500
            };

            var dialog = new ContentDialog
            {
                Title = "Nuevo Contacto",
                Content = dialogContent,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Validar campos requeridos
                if (string.IsNullOrWhiteSpace(nombreTextBox.Text))
                {
                    await _notificacionService.MostrarAsync("Validación", "El nombre es obligatorio");
                    return;
                }

                try
                {
                    // Convertir valores de NumberBox de manera segura
                    int? idCliente = null;
                    if (!double.IsNaN(idClienteNumberBox.Value))
                    {
                        idCliente = Convert.ToInt32(Math.Round(idClienteNumberBox.Value));
                    }

                    int? idProveedor = null;
                    if (!double.IsNaN(idProveedorNumberBox.Value))
                    {
                        idProveedor = Convert.ToInt32(Math.Round(idProveedorNumberBox.Value));
                    }

                    var success = await ViewModel.CreateContactoAsync(
                        nombre: nombreTextBox.Text.Trim(),
                        apellido: string.IsNullOrWhiteSpace(apellidoTextBox.Text) ? null : apellidoTextBox.Text.Trim(),
                        correo: string.IsNullOrWhiteSpace(correoTextBox.Text) ? null : correoTextBox.Text.Trim(),
                        telefono: string.IsNullOrWhiteSpace(telefonoTextBox.Text) ? null : telefonoTextBox.Text.Trim(),
                        departamento: string.IsNullOrWhiteSpace(departamentoTextBox.Text) ? null : departamentoTextBox.Text.Trim(),
                        cargo: string.IsNullOrWhiteSpace(cargoTextBox.Text) ? null : cargoTextBox.Text.Trim(),
                        codigoInterno: string.IsNullOrWhiteSpace(codigoInternoTextBox.Text) ? null : codigoInternoTextBox.Text.Trim(),
                        notas: string.IsNullOrWhiteSpace(notasTextBox.Text) ? null : notasTextBox.Text.Trim(),
                        idCliente: idCliente,
                        idProveedor: idProveedor,
                        activo: activoCheckBox.IsChecked ?? true,
                        tratamiento: string.IsNullOrWhiteSpace(tratamientoTextBox.Text) ? null : tratamientoTextBox.Text.Trim()
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Contacto creado", $"Contacto \"{nombreTextBox.Text.Trim()}\" creado correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear el contacto. Verifique los datos e intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al crear contacto desde la UI", ex, "ContactosPage", "NuevoButton_Click");
                    
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear el contacto. Por favor, intente nuevamente.");
                }
            }
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleContactoExpand(sender);
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleContactoExpand(sender);
        }

        private void ToggleContactoExpand(object sender)
        {
            if (sender is FrameworkElement element && element.Tag is Models.ContactoDto contacto)
            {
                contacto.Expand = !contacto.Expand;

                // Lazy-load áreas al expandir si tiene credencial y no se han cargado
                if (contacto.Expand && !contacto.AreasLoaded && contacto.TieneCredencial)
                {
                    _ = LoadAreasForContactoAsync(contacto);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadAreasForContactoAsync(Models.ContactoDto contacto)
        {
            if (contacto.CredencialId == null || contacto.CredencialId <= 0) return;

            try
            {
                contacto.IsLoadingAreas = true;
                var areas = await _relacionUsuarioAreaService.GetRelacionesPorUsuarioAsync(contacto.CredencialId.Value);
                contacto.AreasAsignadas = new ObservableCollection<Models.RelacionUsuarioAreaDto>(areas);
                contacto.AreasLoaded = true;
                contacto.NotifyNoAreasMessageChanged();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar áreas del contacto", ex, "ContactosPage", "LoadAreasForContactoAsync");
            }
            finally
            {
                contacto.IsLoadingAreas = false;
            }
        }

        private async void AgregarArea_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.ContactoDto contacto)
                return;

            if (!contacto.TieneCredencial)
            {
                await _notificacionService.MostrarAsync("Sin credencial", "Este contacto no tiene credencial asignada. No se pueden asignar áreas.");
                return;
            }

            try
            {
                // Obtener áreas disponibles
                var todasLasAreas = await _areasService.GetAreasAsync(activo: true);

                // Filtrar las que ya están asignadas
                var idsAsignados = contacto.AreasAsignadas.Select(a => a.IdArea).ToHashSet();
                var areasDisponibles = todasLasAreas.Where(a => !idsAsignados.Contains(a.IdArea)).ToList();

                if (areasDisponibles.Count == 0)
                {
                    await _notificacionService.MostrarAsync("Sin áreas disponibles", "Todas las áreas activas ya están asignadas a este contacto.");
                    return;
                }

                // Variable para guardar la selección
                Models.AreaDto? areaSeleccionada = null;

                // AutoSuggestBox con autocompletar
                var areaSuggestBox = new AutoSuggestBox
                {
                    PlaceholderText = "Buscar área por nombre...",
                    QueryIcon = new SymbolIcon(Symbol.Find),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                string FormatArea(Models.AreaDto a) => a.Nombre;

                areaSuggestBox.GotFocus += (s, _) =>
                {
                    if (s is AutoSuggestBox box)
                    {
                        box.ItemsSource = areasDisponibles.Take(10).Select(FormatArea).ToList();
                        box.IsSuggestionListOpen = true;
                    }
                };

                areaSuggestBox.TextChanged += (s, args) =>
                {
                    if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && s is AutoSuggestBox box)
                    {
                        var searchText = box.Text?.Trim().ToLowerInvariant() ?? string.Empty;
                        var filtered = string.IsNullOrWhiteSpace(searchText)
                            ? areasDisponibles.Take(10)
                            : areasDisponibles.Where(a =>
                                a.Nombre.ToLowerInvariant().Contains(searchText));
                        box.ItemsSource = filtered.Take(10).Select(FormatArea).ToList();
                    }
                };

                areaSuggestBox.SuggestionChosen += (s, args) =>
                {
                    if (args.SelectedItem is string selectedText)
                    {
                        areaSeleccionada = areasDisponibles.FirstOrDefault(a => FormatArea(a) == selectedText);
                    }
                };

                var notaTextBox = new TextBox
                {
                    PlaceholderText = "Nota (opcional)",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 60,
                    MaxHeight = 120
                };

                var dialogContent = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Área:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        areaSuggestBox,
                        new TextBlock { Text = "Nota:" },
                        notaTextBox
                    }
                };

                var dialog = new ContentDialog
                {
                    Title = "Asignar Área",
                    Content = dialogContent,
                    PrimaryButtonText = "Asignar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && areaSeleccionada != null)
                {
                    var nota = string.IsNullOrWhiteSpace(notaTextBox.Text) ? null : notaTextBox.Text.Trim();
                    var relacion = await _relacionUsuarioAreaService.CreateRelacionAsync(
                        contacto.CredencialId!.Value,
                        areaSeleccionada.IdArea,
                        nota);

                    if (relacion != null)
                    {
                        contacto.AreasAsignadas.Add(relacion);
                        contacto.NotifyNoAreasMessageChanged();
                        await _notificacionService.MostrarAsync("Área asignada", $"Área \"{areaSeleccionada.Nombre}\" asignada correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo asignar el área.");
                    }
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al asignar área", ex, "ContactosPage", "AgregarArea_Click");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al asignar el área.");
            }
        }

        private async void EliminarAreaAsignada_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.RelacionUsuarioAreaDto relacion)
                return;

            // Buscar el contacto padre recorriendo el árbol visual
            var contacto = ViewModel.Contactos?.FirstOrDefault(c =>
                c.AreasAsignadas.Any(a => a.Id == relacion.Id));

            if (contacto == null) return;

            var confirmDialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Desea quitar el área \"{relacion.NombreArea}\" de este contacto?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await _relacionUsuarioAreaService.DeleteRelacionAsync(relacion.Id);
                    if (success)
                    {
                        contacto.AreasAsignadas.Remove(relacion);
                        contacto.NotifyNoAreasMessageChanged();
                        await _notificacionService.MostrarAsync("Área removida", $"Área \"{relacion.NombreArea}\" removida correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar la asignación.");
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al eliminar área asignada", ex, "ContactosPage", "EliminarAreaAsignada_Click");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la asignación.");
                }
            }
        }
    }
}
