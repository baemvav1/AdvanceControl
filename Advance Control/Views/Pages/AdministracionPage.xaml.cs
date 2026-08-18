using System;
using System.Linq;
using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.PermisosUi;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;
using Advance_Control.Views.Items.Administracion;
using Advance_Control.Views.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Advance_Control.Views.Pages;

public sealed partial class AdministracionPage : Page
{
    public UsuariosAdminViewModel ViewModel { get; }

    private readonly INotificacionService _notificacionService;
    private readonly ILoggingService _loggingService;
    private readonly IPermisoUiRuntimeService _permisoUiRuntimeService;
    private bool _permisosLoaded;

    public AdministracionPage()
    {
        ViewModel = AppServices.Get<UsuariosAdminViewModel>();
        _notificacionService = AppServices.Get<INotificacionService>();
        _loggingService = AppServices.Get<ILoggingService>();
        _permisoUiRuntimeService = AppServices.Get<IPermisoUiRuntimeService>();

        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadUsuariosAsync();
    }

    private async void RecargarUsuariosButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await CanAccessActionAsync(RecargarUsuariosButton))
            return;

        await ViewModel.LoadUsuariosAsync();
    }

    private async void NuevoUsuarioButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await CanAccessActionAsync(NuevoUsuarioButton))
            return;

        AbrirEditorUsuario();
    }

    private async void UsuarioItemView_EditRequested(object sender, UsuarioAdminActionEventArgs e)
    {
        if (!await CanAccessAdministracionModuleAsync())
            return;

        AbrirEditorUsuario(e.Usuario);
    }

    private async void UsuarioItemView_DeleteRequested(object sender, UsuarioAdminActionEventArgs e)
    {
        if (!await CanAccessAdministracionModuleAsync())
            return;

        await DesactivarUsuarioAsync(e.Usuario);
    }

    private void AbrirEditorUsuario(UsuarioAdminDto? usuario = null)
    {
        var window = new UsuarioEditorWindow(usuario);
        window.Closed += async (_, _) =>
        {
            if (window.SavedChanges)
            {
                await ViewModel.LoadUsuariosAsync();
            }
        };
        window.Activate();
    }

    private async Task DesactivarUsuarioAsync(UsuarioAdminDto usuario)
    {
        var confirmDialog = new ContentDialog
        {
            Title = "Desactivar usuario",
            Content = $"Se desactivará el login \"{usuario.Usuario}\". El contacto vinculado no se eliminará físicamente.",
            PrimaryButtonText = "Desactivar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        try
        {
            var result = await ViewModel.DeleteUsuarioAsync(usuario.CredencialId);
            await _notificacionService.MostrarAsync(
                result.Success ? "Usuario desactivado" : "Error",
                result.Message);
        }
        catch (Exception ex)
        {
            await _loggingService.LogErrorAsync("Error al desactivar usuario desde Administración", ex, nameof(AdministracionPage), nameof(DesactivarUsuarioAsync));
            await _notificacionService.MostrarAsync("Error", "No fue posible desactivar el usuario.");
        }
    }

    private async void RecargarPermisosButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await CanAccessActionAsync(RecargarPermisosButton))
            return;

        await ViewModel.LoadPermisosAsync();
        _permisosLoaded = !ViewModel.HasPermisosError;
    }

    private async void SincronizarPermisosButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await CanAccessActionAsync(SincronizarPermisosButton))
            return;

        await ViewModel.SyncPermisosAsync();
        await _permisoUiRuntimeService.InitializeAsync(_permisoUiRuntimeService.NivelUsuario, forceSync: false);
        _permisosLoaded = !ViewModel.HasPermisosError;
        await _notificacionService.MostrarAsync("Permisos", "Catálogo de permisos sincronizado correctamente.");
    }

    private async void CambiarNivelButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not PermisoTreeNode nodo)
            return;

        if (!await CanAccessAdministracionModuleAsync())
            return;

        if (nodo.Tipo == PermisoNodoTipo.Modulo && nodo.Modulo != null)
        {
            var nivelSeleccionado = await ShowNivelDialogAsync(
                $"Cambiar nivel de {nodo.Modulo.NombreModulo}",
                nodo.Modulo.NivelRequerido);

            if (!nivelSeleccionado.HasValue || nivelSeleccionado.Value == nodo.Modulo.NivelRequerido)
                return;

            await ViewModel.UpdateNivelModuloAsync(nodo.Modulo.IdPermisoModulo, nivelSeleccionado.Value);
            await _permisoUiRuntimeService.InitializeAsync(_permisoUiRuntimeService.NivelUsuario, forceSync: false);
        }
        else if (nodo.Tipo == PermisoNodoTipo.Accion && nodo.Accion != null)
        {
            var nivelSeleccionado = await ShowNivelDialogAsync(
                $"Cambiar nivel de {nodo.Accion.NombreAccion}",
                nodo.Accion.NivelRequerido);

            if (!nivelSeleccionado.HasValue || nivelSeleccionado.Value == nodo.Accion.NivelRequerido)
                return;

            await ViewModel.UpdateNivelAccionAsync(nodo.Accion.IdPermisoAccionModulo, nivelSeleccionado.Value);
            await _permisoUiRuntimeService.InitializeAsync(_permisoUiRuntimeService.NivelUsuario, forceSync: false);
        }
    }

    private void RolFiltroComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.RolFiltro = RolFiltroComboBox.SelectedItem as TipoUsuarioDto;
    }

    private void LimpiarRolFiltroButton_Click(object sender, RoutedEventArgs e)
    {
        RolFiltroComboBox.SelectedIndex = -1;
        ViewModel.RolFiltro = null;
    }

    private async Task<bool> CanAccessAdministracionModuleAsync()
    {
        var canAccess = _permisoUiRuntimeService.CanAccessModule(_permisoUiRuntimeService.BuildModuleKey(typeof(AdministracionPage)));
        if (!canAccess)
        {
            await ShowAccessDeniedDialogAsync();
        }

        return canAccess;
    }

    private async Task<bool> CanAccessActionAsync(FrameworkElement element)
    {
        if (!await CanAccessAdministracionModuleAsync())
            return false;

        var controlKey = PermisoUiKeyBuilder.ResolveRuntimeControlKey(element);
        if (string.IsNullOrWhiteSpace(controlKey))
            return true;

        var actionKey = _permisoUiRuntimeService.BuildActionKey(
            _permisoUiRuntimeService.BuildModuleKey(typeof(AdministracionPage)),
            element.GetType().Name,
            controlKey);

        var canAccess = _permisoUiRuntimeService.CanAccessAction(actionKey);
        if (!canAccess)
        {
            await ShowAccessDeniedDialogAsync();
        }

        return canAccess;
    }

    private async void AdministracionTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_permisosLoaded || !ReferenceEquals(AdministracionTabView.SelectedItem, PermisosTabViewItem))
            return;

        await ViewModel.LoadPermisosAsync();
        _permisosLoaded = !ViewModel.HasPermisosError;
    }

    private async Task<int?> ShowNivelDialogAsync(string title, int nivelActual)
    {
        if (ViewModel.TiposUsuarioPermisos.Count == 0)
            return null;

        var comboBox = new ComboBox
        {
            ItemsSource = ViewModel.TiposUsuarioPermisos,
            DisplayMemberPath = nameof(TipoUsuarioDto.TipoUsuario),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            PlaceholderText = "Selecciona un nivel"
        };

        comboBox.SelectedItem = ViewModel.TiposUsuarioPermisos
            .FirstOrDefault(tipo => tipo.IdTipoUsuario == nivelActual);

        var stackPanel = new StackPanel { Spacing = 8 };
        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Nivel actual: {nivelActual}",
            TextWrapping = TextWrapping.Wrap
        });
        stackPanel.Children.Add(comboBox);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = stackPanel,
            PrimaryButtonText = "Guardar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return null;

        return (comboBox.SelectedItem as TipoUsuarioDto)?.IdTipoUsuario;
    }

    private async Task ShowAccessDeniedDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Acceso denegado",
            Content = "No tienes acceso a este modulo",
            CloseButtonText = "Cerrar",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

}
