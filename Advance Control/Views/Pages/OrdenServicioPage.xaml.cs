using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using Advance_Control.Services.UserInfo;
using Advance_Control.Services.Activity;
using Advance_Control.Services.OrdenServicio;
using Advance_Control.Views.Dialogs;
using Advance_Control.Models;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar órdenes de servicio
    /// </summary>
    public sealed partial class OrdenServicioPage : Page
    {
        public OrdenServicioViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly IUserInfoService _userInfoService;
        private readonly IActivityService _activityService;
        private readonly IOrdenServicioService _ordenServicioService;

        public OrdenServicioPage()
        {
            ViewModel = AppServices.Get<OrdenServicioViewModel>();
            _notificacionService = AppServices.Get<INotificacionService>();
            _userInfoService = AppServices.Get<IUserInfoService>();
            _activityService = AppServices.Get<IActivityService>();
            _ordenServicioService = AppServices.Get<IOrdenServicioService>();

            this.InitializeComponent();
            ButtonClickLogger.Attach(this, AppServices.Get<ILoggingService>(), nameof(OrdenServicioPage));

            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await ViewModel.InitializeAsync();
            await ViewModel.LoadOrdenesServicioAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadOrdenesServicioAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            EquipoASB.Text = string.Empty;
            AreaASB.Text = string.Empty;
            await ViewModel.ClearFiltersAsync();
        }

        private void EquipoASB_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                ViewModel.ActualizarSugerenciasEquipo(sender.Text);
        }

        private async void EquipoASB_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var texto = args.ChosenSuggestion as string ?? args.QueryText;
            await ViewModel.AplicarFiltroEquipo(texto);
        }

        private void AreaASB_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                ViewModel.ActualizarSugerenciasArea(sender.Text);
        }

        private async void AreaASB_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            AreaDto? area = null;
            if (args.ChosenSuggestion is AreaDto chosen)
                area = chosen;
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
                area = ViewModel.Areas.FirstOrDefault(a => a.Nombre.Equals(args.QueryText, StringComparison.OrdinalIgnoreCase));
            await ViewModel.AplicarFiltroArea(area);
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            var nuevaOrdenControl = new NuevaOrdenServicioUserControl();

            var dialog = new ContentDialog
            {
                Title = "Nueva Orden de Servicio",
                Content = nuevaOrdenControl,
                PrimaryButtonText = "Crear",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (!nuevaOrdenControl.IdTipoMantenimiento.HasValue)
                {
                    await _notificacionService.MostrarAsync("Error de validación", "Debe seleccionar un tipo de mantenimiento.");
                    return;
                }

                if (!nuevaOrdenControl.IdEquipo.HasValue)
                {
                    await _notificacionService.MostrarAsync("Error de validación", "Debe seleccionar un equipo.");
                    return;
                }

                if (!nuevaOrdenControl.IdCliente.HasValue)
                {
                    await _notificacionService.MostrarAsync("Error de validación", "Debe seleccionar un cliente.");
                    return;
                }

                try
                {
                    var success = await ViewModel.CreateOrdenServicioAsync(
                        nuevaOrdenControl.IdTipoMantenimiento.Value,
                        nuevaOrdenControl.IdCliente.Value,
                        nuevaOrdenControl.IdEquipo.Value,
                        nuevaOrdenControl.Nota
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Orden de servicio creada", "La orden de servicio se ha creado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear la orden de servicio. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al crear orden de servicio: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear la orden de servicio. Por favor, intente nuevamente.");
                }
            }
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Models.OrdenServicioDto orden)
            {
                orden.Expand = !orden.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Models.OrdenServicioDto orden)
            {
                orden.Expand = !orden.Expand;
            }
        }

        private async void DeleteOrdenServicioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OrdenServicioDto orden)
                return;

            if (!orden.IdOrdenServicio.HasValue)
                return;

            var dialog = new ContentDialog
            {
                Title = "Confirmar eliminación",
                Content = $"¿Está seguro de que desea eliminar la orden de servicio #{orden.IdOrdenServicio} ({orden.TipoMantenimiento})?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var success = await ViewModel.DeleteOrdenServicioAsync(orden.IdOrdenServicio.Value);

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Orden de servicio eliminada", "La orden de servicio se ha eliminado correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo eliminar la orden de servicio. Por favor, intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar orden de servicio: {ex.GetType().Name} - {ex.Message}");
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al eliminar la orden de servicio. Por favor, intente nuevamente.");
                }
            }
        }

        private async void AtenderOrdenServicioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OrdenServicioDto orden)
                return;

            if (!orden.IdOrdenServicio.HasValue)
                return;

            try
            {
                var userInfo = await _userInfoService.GetUserInfoAsync();

                if (userInfo == null)
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo obtener la información del usuario autenticado.");
                    return;
                }

                var tipoMantenimiento = orden.TipoMantenimiento ?? "sin tipo especificado";
                var dialog = new ContentDialog
                {
                    Title = "Confirmar atención",
                    Content = $"¿Está seguro de que desea marcar como atendida la orden de servicio #{orden.IdOrdenServicio} ({tipoMantenimiento})?",
                    PrimaryButtonText = "Atender",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var success = await ViewModel.UpdateAtendidoAsync(orden.IdOrdenServicio.Value, userInfo.CredencialId);

                    if (success)
                    {
                        _activityService.Registrar("OrdenServicio", "Orden de servicio atendida");
                        await _notificacionService.MostrarAsync("Orden de servicio atendida", "La orden de servicio se ha marcado como atendida correctamente.");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo marcar la orden de servicio como atendida. Por favor, intente nuevamente.");
                    }
                }
            }
            catch (Exception)
            {
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al atender la orden de servicio. Por favor, intente nuevamente.");
            }
        }

        private async void AtenderComoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.OrdenServicioDto orden)
                return;

            if (!orden.IdOrdenServicio.HasValue)
                return;

            try
            {
                var identificador = orden.Identificador;
                if (string.IsNullOrWhiteSpace(identificador))
                {
                    await _notificacionService.MostrarAsync("Error", "La orden de servicio no tiene un equipo asociado.");
                    return;
                }

                var tecnicos = await _ordenServicioService.GetTecnicosDisponiblesAsync(identificador);

                if (tecnicos == null || tecnicos.Count == 0)
                {
                    await _notificacionService.MostrarAsync("Sin técnicos", "No hay técnicos disponibles para atender esta orden de servicio en el área del equipo.");
                    return;
                }

                var tecnicoListView = new ListView
                {
                    SelectionMode = ListViewSelectionMode.Single,
                    MaxHeight = 400
                };

                foreach (var tecnico in tecnicos)
                {
                    var itemContent = new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = tecnico.NombreCompleto,
                                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                            },
                            new TextBlock
                            {
                                Text = $"{tecnico.TipoUsuario} — {tecnico.Cargo ?? "Sin cargo"}",
                                FontSize = 12,
                                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                            },
                            new TextBlock
                            {
                                Text = !string.IsNullOrWhiteSpace(tecnico.Correo) ? tecnico.Correo :
                                       !string.IsNullOrWhiteSpace(tecnico.Telefono) ? tecnico.Telefono : "",
                                FontSize = 11,
                                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                                Visibility = (!string.IsNullOrWhiteSpace(tecnico.Correo) || !string.IsNullOrWhiteSpace(tecnico.Telefono))
                                    ? Visibility.Visible : Visibility.Collapsed
                            }
                        }
                    };

                    tecnicoListView.Items.Add(new ListViewItem { Content = itemContent, Tag = tecnico });
                }

                var dialogContent = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Seleccione un técnico para atender la orden de servicio #{orden.IdOrdenServicio}:",
                            TextWrapping = TextWrapping.Wrap,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        },
                        tecnicoListView
                    }
                };

                var dialog = new ContentDialog
                {
                    Title = "Atender Como Técnico",
                    Content = dialogContent,
                    PrimaryButtonText = "Atender",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && tecnicoListView.SelectedItem is ListViewItem selectedItem
                    && selectedItem.Tag is TecnicoDisponibleDto selectedTecnico)
                {
                    var tipoMantenimiento = orden.TipoMantenimiento ?? "sin tipo especificado";
                    var confirmDialog = new ContentDialog
                    {
                        Title = "Confirmar atención",
                        Content = $"¿Está seguro de que desea marcar como atendida la orden de servicio #{orden.IdOrdenServicio} ({tipoMantenimiento}) por \"{selectedTecnico.NombreCompleto}\"?",
                        PrimaryButtonText = "Atender",
                        CloseButtonText = "Cancelar",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot
                    };

                    var confirmResult = await confirmDialog.ShowAsync();

                    if (confirmResult == ContentDialogResult.Primary)
                    {
                        var success = await ViewModel.UpdateAtendidoAsync(
                            orden.IdOrdenServicio.Value,
                            (int)selectedTecnico.CredencialId
                        );

                        if (success)
                        {
                            _activityService.Registrar("OrdenServicio", "Atendida como técnico");
                            await _notificacionService.MostrarAsync("Orden de servicio atendida", $"La orden de servicio se ha marcado como atendida por {selectedTecnico.NombreCompleto}.");
                        }
                        else
                        {
                            await _notificacionService.MostrarAsync("Error", "No se pudo marcar la orden de servicio como atendida. Por favor, intente nuevamente.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al atender orden de servicio como técnico: {ex.GetType().Name} - {ex.Message}");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al atender la orden de servicio. Por favor, intente nuevamente.");
            }
        }
    }
}
