using Advance_Control.Models;
using Advance_Control.Services.Portal;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Advance_Control.Views.Portal
{
    /// <summary>
    /// Vista de solo lectura de una hoja de mantenimiento preventivo, con el
    /// botón "Aprobar/Firmar" (aprobación con un clic) cuando está Completada.
    /// </summary>
    public sealed partial class ClienteHojaMantenimientoPage : Page
    {
        private static readonly (string Clave, string Titulo)[] Secciones =
        {
            ("Cabina", "Cabina"),
            ("CuartoMaquinas", "Cuarto de Máquinas"),
            ("Foso", "Foso"),
            ("Pasillo", "Pasillo"),
            ("TechoCabina", "Techo de Cabina"),
        };

        private static readonly (string Clave, string Texto)[] Marcas =
        {
            ("NoAplica", "No aplica"),
            ("Verificacion", "Verificación"),
            ("Ajuste", "Ajuste"),
            ("Limpieza", "Limpieza"),
            ("Lubricacion", "Lubricación"),
            ("Recorrido", "Recorrido"),
        };

        private readonly IPortalClienteService _portalClienteService;
        private long _idHoja;

        public ClienteHojaMantenimientoPage()
        {
            _portalClienteService = AppServices.Get<IPortalClienteService>();
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not long idHoja)
                return;

            _idHoja = idHoja;
            await CargarAsync();
        }

        private async Task CargarAsync()
        {
            var hoja = await _portalClienteService.ObtenerHojaMantenimientoAsync(_idHoja);
            if (hoja == null)
            {
                MostrarMensaje("No fue posible cargar esta hoja de mantenimiento.", InfoBarSeverity.Error);
                return;
            }

            MostrarHoja(hoja);
        }

        private void MostrarHoja(MantenimientoPreventivoHojaDto hoja)
        {
            EstadoTextBlock.Text = hoja.Estado switch
            {
                "Firmada" => $"Firmada el {hoja.FirmadaEn:dd/MM/yyyy HH:mm}",
                "Completada" => "Completada por el técnico, pendiente de tu aprobación.",
                _ => "En progreso (borrador del técnico)."
            };

            var resumenPartes = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(hoja.TipoMaquina))
                resumenPartes.Add($"Tipo de máquina: {hoja.TipoMaquina}");
            if (!string.IsNullOrWhiteSpace(hoja.SituacionFinal))
                resumenPartes.Add($"Situación: {hoja.SituacionFinal}");
            if (hoja.HoraEntrada.HasValue)
                resumenPartes.Add($"Entrada: {hoja.HoraEntrada.Value:hh\\:mm}");
            if (hoja.HoraSalida.HasValue)
                resumenPartes.Add($"Salida: {hoja.HoraSalida.Value:hh\\:mm}");
            ResumenTextBlock.Text = string.Join("  •  ", resumenPartes);

            if (!string.IsNullOrWhiteSpace(hoja.Observaciones))
            {
                ObservacionesTextBlock.Text = $"Observaciones: {hoja.Observaciones}";
                ObservacionesTextBlock.Visibility = Visibility.Visible;
            }

            ConstruirSecciones(hoja.ChecklistJson);

            FirmarButton.Visibility = hoja.PuedeFirmarse ? Visibility.Visible : Visibility.Collapsed;
            if (hoja.YaFirmada)
                MostrarMensaje("Ya aprobaste/firmaste esta hoja.", InfoBarSeverity.Success);
        }

        private void ConstruirSecciones(string checklistJson)
        {
            SeccionesPanel.Children.Clear();

            JsonDocument? documento = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(checklistJson))
                    documento = JsonDocument.Parse(checklistJson);
            }
            catch (JsonException)
            {
                // Checklist vacío o inválido: se muestran las secciones sin datos.
            }

            using (documento)
            {
                foreach (var (clave, titulo) in Secciones)
                {
                    var filas = new StackPanel { Spacing = 4 };
                    var totalMarcados = 0;
                    var totalItems = 0;

                    if (documento != null && documento.RootElement.TryGetProperty(clave, out var arreglo) && arreglo.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elemento in arreglo.EnumerateArray())
                        {
                            totalItems++;
                            var texto = elemento.TryGetProperty("texto", out var t) ? t.GetString() : null;
                            var marca = elemento.TryGetProperty("marca", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                            if (!string.IsNullOrEmpty(marca))
                                totalMarcados++;

                            var marcaTexto = marca == null ? "Sin marcar" : ObtenerTextoMarca(marca);

                            filas.Children.Add(new TextBlock
                            {
                                TextWrapping = TextWrapping.Wrap,
                                Text = $"{texto} — {marcaTexto}"
                            });
                        }
                    }

                    var expander = new Expander
                    {
                        Header = $"{titulo} ({totalMarcados}/{totalItems})",
                        Content = filas,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch
                    };

                    SeccionesPanel.Children.Add(expander);
                }
            }
        }

        private static string ObtenerTextoMarca(string clave)
        {
            foreach (var (marcaClave, marcaTexto) in Marcas)
            {
                if (marcaClave == clave)
                    return marcaTexto;
            }
            return clave;
        }

        private async void FirmarButton_Click(object sender, RoutedEventArgs e)
        {
            FirmarButton.IsEnabled = false;
            try
            {
                var resultado = await _portalClienteService.FirmarHojaMantenimientoAsync(_idHoja);
                if (resultado == null)
                {
                    MostrarMensaje("No fue posible registrar la aprobación. Intenta de nuevo.", InfoBarSeverity.Error);
                    FirmarButton.IsEnabled = true;
                    return;
                }

                MostrarMensaje("¡Listo! Quedó registrada tu aprobación.", InfoBarSeverity.Success);
                FirmarButton.Visibility = Visibility.Collapsed;
                EstadoTextBlock.Text = $"Firmada el {resultado.FirmadaEn:dd/MM/yyyy HH:mm}";
            }
            catch (InvalidOperationException ex)
            {
                MostrarMensaje(ex.Message, InfoBarSeverity.Error);
                FirmarButton.IsEnabled = true;
            }
        }

        private void MostrarMensaje(string mensaje, InfoBarSeverity severity)
        {
            MensajeInfoBar.Message = mensaje;
            MensajeInfoBar.Severity = severity;
            MensajeInfoBar.IsOpen = true;
        }

        private void VolverButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
