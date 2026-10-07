using System;
using Advance_Control.Services.DevOps;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;

namespace Advance_Control
{
    /// <summary>
    /// Insignias de la barra de título: "ENTORNO DE PRUEBAS" (fija, según el entorno elegido en
    /// el login) y "MANTENIMIENTO", que se consulta al abrir y cada minuto (el estado es público
    /// en la API, así que también se ve en la pantalla de login).
    /// </summary>
    public sealed partial class MainWindow
    {
        private DispatcherTimer? _mantenimientoTimer;

        private void IniciarMantenimiento()
        {
            EntornoPruebasBadge.Visibility = EntornoApp.EsPruebas ? Visibility.Visible : Visibility.Collapsed;
            if (EntornoApp.EsPruebas)
                Title = "Advance Control · PRUEBAS";

            var servicio = AppServices.Get<IMantenimientoService>();
            servicio.EstadoCambiado += (_, _) => DispatcherQueue.TryEnqueue(() => ActualizarMantenimiento(servicio.Estado));

            _mantenimientoTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _mantenimientoTimer.Tick += async (_, _) => await ConsultarMantenimientoAsync(servicio);
            _mantenimientoTimer.Start();
            _ = ConsultarMantenimientoAsync(servicio);
        }

        private async System.Threading.Tasks.Task ConsultarMantenimientoAsync(IMantenimientoService servicio)
        {
            try
            {
                ActualizarMantenimiento(await servicio.ObtenerEstadoAsync());
            }
            catch (Exception ex)
            {
                // Sin conexión: se conserva lo último que se mostró.
                System.Diagnostics.Debug.WriteLine($"No se pudo consultar el modo mantenimiento: {ex.Message}");
            }
        }

        private void ActualizarMantenimiento(MantenimientoEstado estado)
        {
            MantenimientoBadge.Visibility = estado.Activo ? Visibility.Visible : Visibility.Collapsed;
            MantenimientoText.Text = string.IsNullOrWhiteSpace(estado.ActivadoPor)
                ? "MANTENIMIENTO · solo usuarios nivel 1"
                : $"MANTENIMIENTO · solo usuarios nivel 1 · activado por {estado.ActivadoPor}";
        }
    }
}
