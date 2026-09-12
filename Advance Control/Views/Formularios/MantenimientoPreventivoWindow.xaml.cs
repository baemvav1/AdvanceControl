using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Inmuebles;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Quotes;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Utilities;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;

namespace Advance_Control.Views.Formularios
{
    /// <summary>
    /// Ventana con el formulario de mantenimiento preventivo de elevadores.
    /// Prellena lo que ya se conoce de la operación (proyecto, equipo, fechas,
    /// contacto del cliente y firma del técnico); el resto (checklists, fotos,
    /// observaciones, firmas nuevas) sigue siendo visual: no persiste información.
    /// </summary>
    public sealed partial class MantenimientoPreventivoWindow : Window
    {
        private readonly OperacionDto _operacion;
        private readonly ContactoDto? _contactoCliente;
        private readonly IFirmaService _firmaService;
        private readonly IEquipoService _equipoService;
        private readonly IInmuebleService _inmuebleService;
        private readonly IUbicacionService _ubicacionService;
        private readonly IOperacionImageService _operacionImageService;
        private readonly IMantenimientoPreventivoPdfService _pdfService;

        // ---- Mantenimiento preventivo: Cabina ----
        public ObservableCollection<ChecklistItem> CabinaChecklist { get; } = BuildChecklist(
            "Botonera e indicadores",
            "Puertas de cabina",
            "Sujeción de pasamanos",
            "Estado general de la cabina",
            "Confort",
            "Alarma y/o intercomunicador",
            "Iluminación de emergencia",
            "Limpieza de sardinel");

        // ---- Mantenimiento preventivo: Cuarto de máquinas ----
        public ObservableCollection<ChecklistItem> CuartoMaquinasChecklist { get; } = BuildChecklist(
            "Limpieza del cuarto de máquinas",
            "Máquina y cables de tracción",
            "Verificar nivel de aceite (si aplica)",
            "Conjunto de freno y contactos BK",
            "Regulador de velocidad",
            "Reapriete de líneas de VVF",
            "Protecciones y conexiones",
            "Cerradura de la puerta de control",
            "Verificar fugas de aceite",
            "Limpieza de control",
            "Nomenclaturas de señalización");

        // ---- Mantenimiento preventivo: Foso ----
        public ObservableCollection<ChecklistItem> FosoChecklist { get; } = BuildChecklist(
            "Limpieza fondo de foso",
            "Switches de seguridad",
            "Polea, cable o cadena",
            "Verificación de los buffers",
            "Conjunto de polea tensora",
            "Verificar fugas de aceite",
            "Verificar mangueras y tuberías",
            "Verificar largo de cables de tracción");

        // ---- Mantenimiento preventivo: Pasillo ----
        public ObservableCollection<ChecklistItem> PasilloChecklist { get; } = BuildChecklist(
            "Abertura y cierre de las puertas",
            "Aceleración, desaceleración y nivelación",
            "Verificación de patinillos",
            "Limpieza de puertas",
            "Ajuste de trincos",
            "Limpiar y lubricar rieles",
            "Verificar puerta",
            "Límites de reducción",
            "Cable de tracción",
            "Cable del regulador");

        // ---- Mantenimiento preventivo: Techo de cabina ----
        public ObservableCollection<ChecklistItem> TechoCabinaChecklist { get; } = BuildChecklist(
            "Operador de puerta y puerta de cabina",
            "Botonera de inspección",
            "Techo / Estructura",
            "Switches de seguridades",
            "Fijación de cables de tracción",
            "Corredizas o roller guides super",
            "Presencia de agua en cabina",
            "Presencia de puentes no autorizados",
            "Presencia de agua en cubo",
            "Iluminación en cubo",
            "Iluminación en cuarto de máquinas",
            "Extintor en cuarto de máquinas",
            "Presencia de agua en cuarto de máquinas");

        /// <param name="operacion">Operación desde la que se abre el formulario.</param>
        /// <param name="contactoCliente">
        /// Contacto del cliente ya elegido por el usuario (mismo diálogo "¿A quién va dirigido...?"
        /// que se usa al generar la cotización), o null si se omitió la selección.
        /// </param>
        public MantenimientoPreventivoWindow(OperacionDto operacion, ContactoDto? contactoCliente = null)
        {
            this.InitializeComponent();

            _operacion = operacion;
            _contactoCliente = contactoCliente;
            _firmaService = AppServices.Get<IFirmaService>();
            _equipoService = AppServices.Get<IEquipoService>();
            _inmuebleService = AppServices.Get<IInmuebleService>();
            _ubicacionService = AppServices.Get<IUbicacionService>();
            _operacionImageService = AppServices.Get<IOperacionImageService>();
            _pdfService = AppServices.Get<IMantenimientoPreventivoPdfService>();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(1200, 860));
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = true;
                presenter.IsResizable = true;
            }

            this.Title = "Mantenimiento Preventivo";

            PrellenarDatosBasicos();
            _ = CargarDatosRelacionadosAsync();
        }

        /// <summary>Datos que ya vienen en <see cref="OperacionDto"/>, sin llamadas a servicios.</summary>
        private void PrellenarDatosBasicos()
        {
            ProyectoTextBox.Text = _operacion.RazonSocial ?? string.Empty;
            NoEquipoTextBox.Text = _operacion.Identificador ?? string.Empty;
            FechaTextBox.Text = _operacion.FechaInicio?.ToString("dd/MM/yyyy") ?? string.Empty;

            // "Refe. Equipo" y "Ruta" no tienen una fuente de datos propia en OperacionDto/EquipoDto:
            // se dejan en blanco para que el técnico las complete a mano. Hora de entrada/salida
            // también quedan en blanco a propósito: las registra el técnico al llegar/salir de la visita.

            TecnicoNombreTextBlock.Text = !string.IsNullOrWhiteSpace(_operacion.Atiende)
                ? $"Atiende: {_operacion.Atiende}"
                : string.Empty;

            // El contacto ya viene elegido desde el diálogo "¿A quién va dirigido...?"
            // (mismo que al generar la cotización), mostrado antes de abrir esta ventana.
            if (_contactoCliente != null)
            {
                var nombre = _contactoCliente.NombreCompleto;
                var correo = _contactoCliente.Correo;
                DirigidoATextBlock.Text = !string.IsNullOrWhiteSpace(correo)
                    ? $"Dirigido a: {nombre} — {correo}"
                    : $"Dirigido a: {nombre}";
                DirigidoATextBlock.Visibility = Visibility.Visible;
            }
        }

        /// <summary>Dirección del equipo/inmueble y firma del técnico.</summary>
        private async System.Threading.Tasks.Task CargarDatosRelacionadosAsync()
        {
            await CargarDireccionAsync();
            CargarFirmaTecnico();
        }

        private async System.Threading.Tasks.Task CargarDireccionAsync()
        {
            if (string.IsNullOrWhiteSpace(_operacion.Identificador))
                return;

            try
            {
                int? idUbicacion = null;

                if (string.Equals(_operacion.TipoObjetivo, "equipo", StringComparison.OrdinalIgnoreCase))
                {
                    var equipos = await _equipoService.GetEquiposAsync(new EquipoQueryDto { Identificador = _operacion.Identificador });
                    idUbicacion = equipos?.FirstOrDefault()?.IdUbicacion;
                }
                else if (string.Equals(_operacion.TipoObjetivo, "inmueble", StringComparison.OrdinalIgnoreCase))
                {
                    var inmuebles = await _inmuebleService.GetInmueblesAsync(new InmuebleQueryDto { Identificador = _operacion.Identificador });
                    idUbicacion = inmuebles?.FirstOrDefault()?.IdUbicacion;
                }

                if (idUbicacion.HasValue && idUbicacion.Value > 0)
                {
                    var ubicacion = await _ubicacionService.GetUbicacionByIdAsync(idUbicacion.Value);
                    if (ubicacion != null)
                        DireccionTextBox.Text = ubicacion.DireccionCompleta ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar dirección para Mantenimiento Preventivo: {ex.Message}");
            }
        }

        /// <summary>
        /// Usa la misma fuente que las cotizaciones/reportes/notas (<see cref="IFirmaService"/>,
        /// archivo {idAtiende}_{nombre}.png en Documents\Advance Control\Firmas\).
        /// </summary>
        private void CargarFirmaTecnico()
        {
            if (!_operacion.IdAtiende.HasValue)
                return;

            try
            {
                var firmaPath = _firmaService.GetFirmaOperadorPath(_operacion.IdAtiende.Value);
                if (string.IsNullOrEmpty(firmaPath) || !File.Exists(firmaPath))
                    return;

                FirmaTecnicoImage.Source = new BitmapImage(new Uri(firmaPath));
                FirmaTecnicoImage.Visibility = Visibility.Visible;
                FirmaTecnicoPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar firma del técnico para Mantenimiento Preventivo: {ex.Message}");
            }
        }

        private static ObservableCollection<ChecklistItem> BuildChecklist(params string[] textos)
        {
            var lista = new ObservableCollection<ChecklistItem>();
            foreach (var texto in textos)
                lista.Add(new ChecklistItem { Texto = texto });
            return lista;
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Genera el PDF con el estado actual del formulario, lo sube al VPS (queda listado junto
        /// con el resto de documentos de la operación) y abre el mismo visor con envío por correo
        /// que se usa para cotización/reporte/nota.
        /// </summary>
        private async void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_operacion.IdOperacion.HasValue)
                return;

            FinalizarButton.IsEnabled = false;
            EstadoFinalizarTextBlock.Text = "Generando PDF...";
            try
            {
                var datos = BuildPdfData();
                var localPath = await _pdfService.GeneratePdfAsync(datos);

                EstadoFinalizarTextBlock.Text = "Subiendo al servidor...";
                await using var stream = File.OpenRead(localPath);
                var subido = await _operacionImageService.UploadMantenimientoPreventivoAsync(_operacion.IdOperacion.Value, stream);

                var rutaParaVisor = !string.IsNullOrWhiteSpace(subido?.Url) ? subido!.Url! : localPath;
                var contactosCliente = _contactoCliente != null ? new List<ContactoDto> { _contactoCliente } : new List<ContactoDto>();

                EstadoFinalizarTextBlock.Text = subido != null
                    ? "PDF generado y guardado en el servidor."
                    : "PDF generado localmente; no se pudo subir al servidor.";

                var visor = new CotizacionVisorDialog(rutaParaVisor, _contactoCliente, contactosCliente, _operacion.RazonSocial ?? string.Empty, this.Content.XamlRoot, tipo: "Mantenimiento Preventivo");
                visor.NotificarResultado(await visor.ShowAsync());

                if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
                {
                    var email = new EnviarCotizacionDialog(rutaParaVisor, _contactoCliente, contactosCliente, _operacion.RazonSocial ?? string.Empty, this.Content.XamlRoot, tipo: "Mantenimiento Preventivo", idOperacion: _operacion.IdOperacion);
                    await email.ShowAsync();
                }
                else if (visor.Resultado == CotizacionVisorResultado.AbrirExterno)
                {
                    var file = await global::Windows.Storage.StorageFile.GetFileFromPathAsync(rutaParaVisor);
                    await global::Windows.System.Launcher.LaunchFileAsync(file);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al finalizar mantenimiento preventivo: {ex.Message}");
                EstadoFinalizarTextBlock.Text = "Ocurrió un error al generar o subir el PDF.";
            }
            finally
            {
                FinalizarButton.IsEnabled = true;
            }
        }

        private MantenimientoPreventivoPdfData BuildPdfData()
        {
            var secciones = new List<MantenimientoPreventivoSeccion>
            {
                new("Cabina", CabinaChecklist.ToList()),
                new("Cuarto de Máquinas", CuartoMaquinasChecklist.ToList()),
                new("Foso", FosoChecklist.ToList()),
                new("Pasillo", PasilloChecklist.ToList()),
                new("Techo de Cabina", TechoCabinaChecklist.ToList()),
            };

            string? situacionFinal = SituacionOperativoRadioButton.IsChecked == true
                ? "Operativo"
                : SituacionDetenidoRadioButton.IsChecked == true
                    ? "Detenido"
                    : null;

            return new MantenimientoPreventivoPdfData
            {
                IdOperacion = _operacion.IdOperacion ?? 0,
                Proyecto = ProyectoTextBox.Text,
                Direccion = DireccionTextBox.Text,
                Ruta = RutaTextBox.Text,
                NoEquipo = NoEquipoTextBox.Text,
                RefeEquipo = RefeEquipoTextBox.Text,
                Fecha = FechaTextBox.Text,
                HoraEntrada = HoraEntradaTextBox.Text,
                HoraSalida = HoraSalidaTextBox.Text,
                DirigidoA = DirigidoATextBlock.Visibility == Visibility.Visible ? DirigidoATextBlock.Text : null,
                Secciones = secciones,
                Observaciones = ObservacionesTextBox.Text,
                SituacionFinal = situacionFinal,
                TecnicoNombre = _operacion.Atiende,
                IdAtiende = _operacion.IdAtiende,
                ClienteNombre = _contactoCliente?.NombreCompleto,
                ClienteCorreo = _contactoCliente?.Correo
            };
        }
    }
}
