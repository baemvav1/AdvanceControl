using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Inmuebles;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.MantenimientoPreventivo;
using Advance_Control.Services.Quotes;
using Advance_Control.Services.Ubicaciones;
using Advance_Control.Utilities;
using Advance_Control.Views.Dialogs;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        private readonly IHojaMantenimientoService _hojaMantenimientoService;

        /// <summary>"Hidraulico", "ConCuartoMaquinas" o "SinCuartoMaquinas"; null si no se eligió ninguna.</summary>
        private string? _tipoMaquinaSeleccionado;

        /// <summary>Hoja persistida más reciente para esta operación (Borrador/Completada/Firmada), o null si aún no existe.</summary>
        private MantenimientoPreventivoHojaDto? _hojaActual;

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
            _hojaMantenimientoService = AppServices.Get<IHojaMantenimientoService>();

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

            WireProgreso(CabinaChecklist, CabinaProgresoTextBlock);
            WireProgreso(CuartoMaquinasChecklist, CuartoMaquinasProgresoTextBlock);
            WireProgreso(FosoChecklist, FosoProgresoTextBlock);
            WireProgreso(PasilloChecklist, PasilloProgresoTextBlock);
            WireProgreso(TechoCabinaChecklist, TechoCabinaProgresoTextBlock);
        }

        /// <summary>Datos que ya vienen en <see cref="OperacionDto"/>, sin llamadas a servicios.</summary>
        private void PrellenarDatosBasicos()
        {
            NombreClienteTextBlock.Text = _operacion.RazonSocial ?? string.Empty;
            EquipoTextBlock.Text = _operacion.Identificador ?? string.Empty;
            FechaTextBlock.Text = _operacion.FechaInicio?.ToString("dd/MM/yyyy") ?? string.Empty;

            // Hora de entrada/salida quedan en blanco a propósito: las registra el técnico
            // al llegar/salir de la visita.

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

        /// <summary>Dirección del equipo/inmueble, firma del técnico y el Borrador persistido (si existe).</summary>
        private async System.Threading.Tasks.Task CargarDatosRelacionadosAsync()
        {
            await CargarDireccionAsync();
            CargarFirmaTecnico();
            await CargarHojaExistenteAsync();
        }

        /// <summary>
        /// Recupera la hoja más reciente de esta operación (si existe) y rehidrata
        /// el formulario, para que cerrar/reabrir la ventana no pierda lo capturado.
        /// </summary>
        private async System.Threading.Tasks.Task CargarHojaExistenteAsync()
        {
            if (!_operacion.IdOperacion.HasValue)
                return;

            var hojas = await _hojaMantenimientoService.ObtenerPorOperacionAsync(_operacion.IdOperacion.Value);
            var hoja = hojas?.FirstOrDefault();
            if (hoja == null)
                return;

            _hojaActual = hoja;

            _tipoMaquinaSeleccionado = hoja.TipoMaquina;
            TipoMaquinaFlipView.SelectedIndex = hoja.TipoMaquina switch
            {
                "Hidraulico" => 0,
                "ConCuartoMaquinas" => 1,
                "SinCuartoMaquinas" => 2,
                _ => TipoMaquinaFlipView.SelectedIndex
            };

            if (hoja.SituacionFinal == "Operativo")
                SituacionOperativoRadioButton.IsChecked = true;
            else if (hoja.SituacionFinal == "Detenido")
                SituacionDetenidoRadioButton.IsChecked = true;

            if (hoja.HoraEntrada.HasValue)
                HoraEntradaTimePicker.SelectedTime = hoja.HoraEntrada.Value;
            if (hoja.HoraSalida.HasValue)
                HoraSalidaTimePicker.SelectedTime = hoja.HoraSalida.Value;

            ObservacionesTextBox.Text = hoja.Observaciones ?? string.Empty;

            RehidratarChecklist(hoja.ChecklistJson);

            EstadoFinalizarTextBlock.Text = hoja.Estado switch
            {
                "Firmada" => "Esta hoja ya fue firmada por el cliente y no puede modificarse.",
                "Completada" => "Esta hoja ya fue completada. Puedes volver a generarla si es necesario.",
                _ => "Se recuperó un borrador guardado previamente."
            };

            if (hoja.Estado == "Firmada")
                FinalizarButton.IsEnabled = false;
        }

        /// <summary>"NoAplica"/"Verificacion"/"Ajuste"/"Limpieza"/"Lubricacion"/"Recorrido", o null si no está marcado.</summary>
        private static string? MarcaDe(ChecklistItem item) =>
            item.NoAplica ? "NoAplica" :
            item.Verificacion ? "Verificacion" :
            item.Ajuste ? "Ajuste" :
            item.Limpieza ? "Limpieza" :
            item.Lubricacion ? "Lubricacion" :
            item.Recorrido ? "Recorrido" : null;

        /// <summary>Serializa las 5 secciones a la forma JSON que espera la API (ver migración 115).</summary>
        private string SerializarChecklist()
        {
            var secciones = new Dictionary<string, List<object>>
            {
                ["Cabina"] = CabinaChecklist.Select(i => (object)new { texto = i.Texto, marca = MarcaDe(i) }).ToList(),
                ["CuartoMaquinas"] = CuartoMaquinasChecklist.Select(i => (object)new { texto = i.Texto, marca = MarcaDe(i) }).ToList(),
                ["Foso"] = FosoChecklist.Select(i => (object)new { texto = i.Texto, marca = MarcaDe(i) }).ToList(),
                ["Pasillo"] = PasilloChecklist.Select(i => (object)new { texto = i.Texto, marca = MarcaDe(i) }).ToList(),
                ["TechoCabina"] = TechoCabinaChecklist.Select(i => (object)new { texto = i.Texto, marca = MarcaDe(i) }).ToList(),
            };
            return JsonSerializer.Serialize(secciones);
        }

        /// <summary>Aplica un checklist guardado previamente sobre las listas actuales, emparejando por Texto.</summary>
        private void RehidratarChecklist(string checklistJson)
        {
            if (string.IsNullOrWhiteSpace(checklistJson))
                return;

            try
            {
                using var documento = JsonDocument.Parse(checklistJson);
                RehidratarSeccion(documento, "Cabina", CabinaChecklist);
                RehidratarSeccion(documento, "CuartoMaquinas", CuartoMaquinasChecklist);
                RehidratarSeccion(documento, "Foso", FosoChecklist);
                RehidratarSeccion(documento, "Pasillo", PasilloChecklist);
                RehidratarSeccion(documento, "TechoCabina", TechoCabinaChecklist);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al rehidratar checklist de mantenimiento preventivo: {ex.Message}");
            }
        }

        private static void RehidratarSeccion(JsonDocument documento, string seccion, ObservableCollection<ChecklistItem> lista)
        {
            if (!documento.RootElement.TryGetProperty(seccion, out var arreglo) || arreglo.ValueKind != JsonValueKind.Array)
                return;

            foreach (var elemento in arreglo.EnumerateArray())
            {
                var texto = elemento.TryGetProperty("texto", out var textoElemento) ? textoElemento.GetString() : null;
                var marca = elemento.TryGetProperty("marca", out var marcaElemento) && marcaElemento.ValueKind == JsonValueKind.String
                    ? marcaElemento.GetString()
                    : null;

                var item = lista.FirstOrDefault(i => i.Texto == texto);
                if (item == null)
                    continue;

                item.NoAplica = marca == "NoAplica";
                item.Verificacion = marca == "Verificacion";
                item.Ajuste = marca == "Ajuste";
                item.Limpieza = marca == "Limpieza";
                item.Lubricacion = marca == "Lubricacion";
                item.Recorrido = marca == "Recorrido";
            }
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
                        DireccionTextBlock.Text = ubicacion.DireccionCompleta ?? string.Empty;
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

        private static string FormatearHora(TimeSpan? hora) =>
            hora.HasValue ? DateTime.Today.Add(hora.Value).ToString("HH:mm") : string.Empty;

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Botones "Marcar toda la sección" del encabezado de cada tabla. El Tag trae
        /// "{Seccion}:{Columna}" (p.ej. "Cabina:NoAplica"); "Limpiar" no coincide con
        /// ninguna columna y por lo tanto desmarca todo el renglón.
        /// </summary>
        private void MarcarSeccionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement boton || boton.Tag is not string tag) return;

            var partes = tag.Split(':');
            if (partes.Length != 2) return;

            var lista = ObtenerChecklist(partes[0]);
            if (lista == null) return;

            AplicarColumna(lista, partes[1]);
        }

        /// <summary>
        /// "Tipo de Máquina" es un carrusel (FlipView): la página que queda a la vista
        /// (0=Hidráulico, 1=Con cuarto de máquinas, 2=Sin cuarto de máquinas) es la
        /// selección — no hace falta un control de selección aparte.
        /// </summary>
        private void TipoMaquinaFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _tipoMaquinaSeleccionado = TipoMaquinaFlipView.SelectedIndex switch
            {
                0 => "Hidraulico",
                1 => "ConCuartoMaquinas",
                2 => "SinCuartoMaquinas",
                _ => null
            };

            // Un elevador "Sin cuarto de máquinas" (MRL), por definición, no tiene esa sala:
            // toda la sección deja de aplicar automáticamente al elegir este tipo.
            if (_tipoMaquinaSeleccionado == "SinCuartoMaquinas")
                AplicarColumna(CuartoMaquinasChecklist, "NoAplica");
        }

        private static void AplicarColumna(ObservableCollection<ChecklistItem> lista, string columna)
        {
            foreach (var item in lista)
            {
                item.NoAplica = columna == "NoAplica";
                item.Verificacion = columna == "Verificacion";
                item.Ajuste = columna == "Ajuste";
                item.Limpieza = columna == "Limpieza";
                item.Lubricacion = columna == "Lubricacion";
                item.Recorrido = columna == "Recorrido";
            }
        }

        private ObservableCollection<ChecklistItem>? ObtenerChecklist(string seccion) => seccion switch
        {
            "Cabina" => CabinaChecklist,
            "CuartoMaquinas" => CuartoMaquinasChecklist,
            "Foso" => FosoChecklist,
            "Pasillo" => PasilloChecklist,
            "TechoCabina" => TechoCabinaChecklist,
            _ => null
        };

        private static bool EstaMarcado(ChecklistItem item) =>
            item.NoAplica || item.Verificacion || item.Ajuste || item.Limpieza || item.Lubricacion || item.Recorrido;

        /// <summary>
        /// Badge "X/N" en el header de cada Expander: se recalcula cada vez que cambia
        /// cualquier renglón de esa sección (por eso <see cref="ChecklistItem"/> necesita
        /// INotifyPropertyChanged, tanto para los RadioButton como para estos botones).
        /// </summary>
        private static void WireProgreso(ObservableCollection<ChecklistItem> lista, TextBlock badge)
        {
            void Actualizar() => badge.Text = $"{lista.Count(EstaMarcado)}/{lista.Count}";
            foreach (var item in lista)
                item.PropertyChanged += (_, _) => Actualizar();
            Actualizar();
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

            if (_hojaActual?.Estado == "Firmada")
            {
                EstadoFinalizarTextBlock.Text = "Esta hoja ya fue firmada por el cliente y no puede modificarse.";
                return;
            }

            var seccionesSinMarcar = new (string Nombre, ObservableCollection<ChecklistItem> Lista)[]
            {
                ("Cabina", CabinaChecklist),
                ("Cuarto de Máquinas", CuartoMaquinasChecklist),
                ("Foso", FosoChecklist),
                ("Pasillo", PasilloChecklist),
                ("Techo de Cabina", TechoCabinaChecklist),
            }.Where(s => s.Lista.Count > 0 && s.Lista.All(i => !EstaMarcado(i))).Select(s => s.Nombre).ToList();

            if (seccionesSinMarcar.Count > 0)
            {
                var confirmar = new ContentDialog
                {
                    Title = "Secciones sin marcar",
                    Content = $"No marcaste ningún renglón en: {string.Join(", ", seccionesSinMarcar)}. ¿Generar el PDF de todas formas?",
                    PrimaryButtonText = "Generar de todas formas",
                    SecondaryButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.Content.XamlRoot
                };
                if (await confirmar.ShowAsync() != ContentDialogResult.Primary)
                    return;
            }

            FinalizarButton.IsEnabled = false;
            EstadoFinalizarTextBlock.Text = "Guardando avance...";
            try
            {
                var guardado = await GuardarHojaAsync("Completada");
                if (guardado == null)
                {
                    EstadoFinalizarTextBlock.Text = "No fue posible guardar el mantenimiento en el servidor. Intenta de nuevo.";
                    return;
                }
                _hojaActual = guardado;

                EstadoFinalizarTextBlock.Text = "Generando PDF...";
                var datos = BuildPdfData();
                var localPath = await _pdfService.GeneratePdfAsync(datos);

                EstadoFinalizarTextBlock.Text = "Subiendo al servidor...";
                await using var stream = File.OpenRead(localPath);
                var subido = await _operacionImageService.UploadMantenimientoPreventivoAsync(_operacion.IdOperacion.Value, stream);

                var rutaParaVisor = !string.IsNullOrWhiteSpace(subido?.Url) ? subido!.Url! : localPath;
                var contactosCliente = _contactoCliente != null ? new List<ContactoDto> { _contactoCliente } : new List<ContactoDto>();

                if (!string.IsNullOrWhiteSpace(subido?.Url))
                {
                    // Best-effort: la hoja ya quedó Completada aunque esto falle.
                    var actualizado = await GuardarHojaAsync("Completada", pdfUrl: subido!.Url);
                    if (actualizado != null)
                        _hojaActual = actualizado;
                }

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

        /// <summary>
        /// Persiste el estado actual del formulario (crea la hoja si aún no existe, o
        /// actualiza la existente). Es la fuente de verdad; el PDF es un subproducto.
        /// </summary>
        private async Task<MantenimientoPreventivoHojaDto?> GuardarHojaAsync(string estado, string? pdfUrl = null)
        {
            var request = new MantenimientoPreventivoGuardarRequestDto
            {
                IdOperacion = _operacion.IdOperacion!.Value,
                TipoMaquina = _tipoMaquinaSeleccionado,
                SituacionFinal = SituacionOperativoRadioButton.IsChecked == true
                    ? "Operativo"
                    : SituacionDetenidoRadioButton.IsChecked == true
                        ? "Detenido"
                        : null,
                HoraEntrada = HoraEntradaTimePicker.SelectedTime,
                HoraSalida = HoraSalidaTimePicker.SelectedTime,
                Observaciones = ObservacionesTextBox.Text,
                ChecklistJson = SerializarChecklist(),
                IdAtiende = _operacion.IdAtiende,
                IdContactoDirigido = _contactoCliente?.ContactoId,
                Estado = estado,
                PdfUrl = pdfUrl
            };

            return _hojaActual == null
                ? await _hojaMantenimientoService.CrearAsync(request)
                : await _hojaMantenimientoService.ActualizarAsync(_hojaActual.Id, request);
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

            string? tipoMaquina = _tipoMaquinaSeleccionado switch
            {
                "Hidraulico" => "Hidráulico",
                "ConCuartoMaquinas" => "Con cuarto de máquinas",
                "SinCuartoMaquinas" => "Sin cuarto de máquinas (MRL)",
                _ => null
            };

            return new MantenimientoPreventivoPdfData
            {
                IdOperacion = _operacion.IdOperacion ?? 0,
                NombreCliente = NombreClienteTextBlock.Text,
                Direccion = DireccionTextBlock.Text,
                Equipo = EquipoTextBlock.Text,
                Fecha = FechaTextBlock.Text,
                HoraEntrada = FormatearHora(HoraEntradaTimePicker.SelectedTime),
                HoraSalida = FormatearHora(HoraSalidaTimePicker.SelectedTime),
                TipoMaquina = tipoMaquina,
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
