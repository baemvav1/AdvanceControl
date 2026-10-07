using System;
using Advance_Control.Services.DevOps;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;

namespace Advance_Control
{
    /// <summary>
    /// Insignia "MODO PRUEBAS" en la barra de título: se consulta al abrir y cada minuto
    /// (el estado es público en la API, así que también se ve en la pantalla de login).
    /// </summary>
    public sealed partial class MainWindow
    {
        private DispatcherTimer? _modoPruebasTimer;

        private void IniciarModoPruebas()
        {
            var servicio = AppServices.Get<IModoPruebasService>();
            servicio.EstadoCambiado += (_, _) => DispatcherQueue.TryEnqueue(() => ActualizarModoPruebas(servicio.Estado));

            _modoPruebasTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _modoPruebasTimer.Tick += async (_, _) => await ConsultarModoPruebasAsync(servicio);
            _modoPruebasTimer.Start();
            _ = ConsultarModoPruebasAsync(servicio);
        }

        private async System.Threading.Tasks.Task ConsultarModoPruebasAsync(IModoPruebasService servicio)
        {
            try
            {
                ActualizarModoPruebas(await servicio.ObtenerEstadoAsync());
            }
            catch (Exception ex)
            {
                // Sin conexión: se conserva lo último que se mostró.
                System.Diagnostics.Debug.WriteLine($"No se pudo consultar el modo pruebas: {ex.Message}");
            }
        }

        private void ActualizarModoPruebas(ModoPruebasEstado estado)
        {
            ModoPruebasBadge.Visibility = estado.Activo ? Visibility.Visible : Visibility.Collapsed;
            ModoPruebasText.Text = string.IsNullOrWhiteSpace(estado.ActivadoPor)
                ? "MODO PRUEBAS · solo usuarios nivel 1"
                : $"MODO PRUEBAS · solo usuarios nivel 1 · activado por {estado.ActivadoPor}";
        }
    }
}
