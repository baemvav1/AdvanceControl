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
    /// Botón de modo pruebas: solo nivel 1. Activo, la API solo deja trabajar a nivel 1 y al
    /// cliente de prueba del portal; al desactivar se borra el cliente de prueba y se reabre.
    /// </summary>
    public sealed partial class DevOpsPage
    {
        private static IModoPruebasService ModoPruebas => AppServices.Get<IModoPruebasService>();

        protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await RefrescarModoPruebasAsync();
        }

        private async Task RefrescarModoPruebasAsync()
        {
            try
            {
                MostrarModoPruebas(await ModoPruebas.ObtenerEstadoAsync());
            }
            catch (Exception ex)
            {
                ModoPruebasEstadoText.Text = $"No se pudo consultar el modo pruebas: {ex.Message}";
            }
        }

        private void MostrarModoPruebas(ModoPruebasEstado estado)
        {
            var esNivel1 = AppServices.Get<IUserSessionService>().Nivel == 1;
            ModoPruebasEstadoText.Text = estado.Activo
                ? $"ACTIVO desde el {estado.ActivadoEn?.ToLocalTime():dd/MM/yyyy HH:mm}{(string.IsNullOrWhiteSpace(estado.ActivadoPor) ? "" : $", por {estado.ActivadoPor}")}. Los demás usuarios no pueden entrar."
                : "Inactivo: el sistema funciona normal para todos.";
            ModoPruebasBotonText.Text = estado.Activo ? "Desactivar modo pruebas" : "Activar modo pruebas";
            ModoPruebasIcon.Glyph = estado.Activo ? "" : "";
            BtnModoPruebas.IsEnabled = esNivel1;
            ToolTipService.SetToolTip(BtnModoPruebas, esNivel1 ? null : "Solo un usuario nivel 1 puede usar el modo pruebas.");
        }

        private async void OnModoPruebasClick(object sender, RoutedEventArgs e)
        {
            var activo = ModoPruebas.Estado.Activo;
            var dialogo = new ContentDialog
            {
                Title = activo ? "Desactivar modo pruebas" : "Activar modo pruebas",
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = activo
                        ? "Se borrará el cliente de prueba con TODO lo relacionado (operaciones, cotizaciones, hojas, factura ficticia, logins, archivos), " +
                          "se reiniciarán las semillas y el sistema se reabrirá para todos los usuarios y clientes.\n\n" +
                          "Lo que hayas modificado de clientes reales se queda como lo dejaste."
                        : "Desde este momento:\n" +
                          "• Solo usuarios nivel 1 podrán entrar y trabajar en Advance Control (a los demás se les rechaza todo con \"Sistema en mantenimiento\").\n" +
                          "• En el Portal de Clientes solo podrá entrar el cliente de prueba, que se genera si no existe.\n" +
                          "• No se podrá timbrar ni cancelar CFDI ante el SAT.\n" +
                          "• Los correos solo podrán enviarse a contactos del cliente de prueba.\n\n" +
                          "No se desactiva solo: hay que volver a presionar este botón."
                },
                PrimaryButtonText = activo ? "Desactivar y limpiar" : "Activar modo pruebas",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
                return;

            BtnModoPruebas.IsEnabled = false;
            if (activo)
                await ViewModel.DesactivarModoPruebasAsync();
            else
                await ViewModel.ActivarModoPruebasAsync();
            await RefrescarModoPruebasAsync();
        }
    }
}
