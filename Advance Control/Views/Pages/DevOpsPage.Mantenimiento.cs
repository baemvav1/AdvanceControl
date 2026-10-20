using System;
using System.Threading.Tasks;
using Advance_Control.Services.DevOps;
using Advance_Control.Services.Session;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Botón de modo mantenimiento: solo nivel 1. Activo, la API solo deja trabajar a usuarios
    /// nivel 1, cierra el portal y no timbra. No se desactiva solo.
    /// </summary>
    public sealed partial class DevOpsPage
    {
        private static IMantenimientoService Mantenimiento => AppServices.Get<IMantenimientoService>();

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await RefrescarMantenimientoAsync();
        }

        private async Task RefrescarMantenimientoAsync()
        {
            try
            {
                MostrarMantenimiento(await Mantenimiento.ObtenerEstadoAsync());
            }
            catch (Exception ex)
            {
                MantenimientoEstadoText.Text = $"No se pudo consultar el modo mantenimiento: {ex.Message}";
            }
        }

        private void MostrarMantenimiento(MantenimientoEstado estado)
        {
            var esNivel1 = AppServices.Get<IUserSessionService>().Nivel == 1;
            MantenimientoEstadoText.Text = estado.Activo
                ? $"ACTIVO desde el {estado.ActivadoEn?.ToLocalTime():dd/MM/yyyy HH:mm}{(string.IsNullOrWhiteSpace(estado.ActivadoPor) ? "" : $", por {estado.ActivadoPor}")}. Los demás usuarios y el portal no pueden entrar."
                : "Inactivo: el sistema funciona normal para todos.";
            MantenimientoBotonText.Text = estado.Activo ? "Desactivar mantenimiento" : "Activar mantenimiento";
            MantenimientoIcon.Glyph = estado.Activo ? "" : "";
            BtnModoPruebas.IsEnabled = esNivel1;
            ToolTipService.SetToolTip(BtnModoPruebas, esNivel1 ? null : "Solo un usuario nivel 1 puede usar el modo mantenimiento.");
        }

        private async void OnMantenimientoClick(object sender, RoutedEventArgs e)
        {
            var activo = Mantenimiento.Estado.Activo;
            var dialogo = new ContentDialog
            {
                Title = activo ? "Desactivar modo mantenimiento" : "Activar modo mantenimiento",
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = activo
                        ? "El sistema se reabrirá para todos los usuarios y para el Portal de Clientes."
                        : "Desde este momento:\n" +
                          "• Solo usuarios nivel 1 podrán entrar y trabajar en Advance Control (a los demás se les rechaza todo con \"Sistema en mantenimiento\").\n" +
                          "• El Portal de Clientes queda cerrado para todos los clientes.\n" +
                          "• No se podrá timbrar ni cancelar CFDI ante el SAT.\n\n" +
                          "No se desactiva solo: hay que volver a presionar este botón."
                },
                PrimaryButtonText = activo ? "Desactivar" : "Activar mantenimiento",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
                return;

            BtnModoPruebas.IsEnabled = false;
            try
            {
                if (activo)
                    await Mantenimiento.DesactivarAsync();
                else
                    await Mantenimiento.ActivarAsync();
            }
            catch (Exception ex)
            {
                MantenimientoEstadoText.Text = ex.Message;
                BtnModoPruebas.IsEnabled = true;
                return;
            }
            await RefrescarMantenimientoAsync();
        }
    }
}
