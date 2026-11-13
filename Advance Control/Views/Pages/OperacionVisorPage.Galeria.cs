using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Converters;
using Advance_Control.Models;
using Advance_Control.Services.Facturas;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Quotes;
using Advance_Control.Utilities;
using Advance_Control.Views.Dialogs;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using global::Windows.Storage.Pickers;
using WinStorage = global::Windows.Storage;
using WinSystem = global::Windows.System;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Galería del visor: siempre cargada en Info con todos los documentos y
    /// fotos de la operación (al seleccionar un cargo se desplaza a sus fotos), con los botones de la pestaña Documentos del visor
    /// (Generar, Cargar, Mtto Prev.). La lógica de cada acción es la del visor.
    /// </summary>
    public sealed partial class OperacionVisorPage
    {
        private readonly IOperacionImageService _imagenesOperacion = AppServices.Get<IOperacionImageService>();
        private readonly IFirmaService _firmas = AppServices.Get<IFirmaService>();
        private readonly IFacturaService _facturas = AppServices.Get<IFacturaService>();
        private readonly IFacturaPdfService _facturaPdf = AppServices.Get<IFacturaPdfService>();

        /// <summary>Sección de fotos de cada cargo, para desplazarse a ella y resaltarla.</summary>
        private readonly Dictionary<int, Border> _seccionesCargo = new();

        private const double LadoMiniatura = 120;
        private static readonly SolidColorBrush BrochaPdf = new(ColorHelper.FromArgb(0xFF, 0xE0, 0x52, 0x52));
        private static readonly SolidColorBrush BrochaSinResalte = new(Colors.Transparent);

        /// <summary>Después de generar, cargar o eliminar.</summary>
        private Task RefrescarGaleriaAsync() => MostrarGaleriaAsync();

        /// <summary>Desplaza la galería para que la sección del cargo quede arriba.</summary>
        private void DesplazarACargo(CargoFila cargo)
        {
            if (!_seccionesCargo.TryGetValue(cargo.IdCargo, out var seccion))
            {
                return;
            }

            var posicion = seccion.TransformToVisual(GaleriaContenido).TransformPoint(new global::Windows.Foundation.Point(0, 0));
            GaleriaScroll.ChangeView(null, posicion.Y, null);
        }

        private void ResaltarCargosSeleccionados()
        {
            var acento = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            foreach (var (idCargo, seccion) in _seccionesCargo)
            {
                seccion.BorderBrush = idCargo == _cargoActual?.IdCargo ? acento : BrochaSinResalte;
            }
        }

        // ---------------- Agregar / Eliminar según el cargo seleccionado ----------------

        /// <summary>
        /// Sin cargo seleccionado: "Agregar" crea un cargo, Eliminar no aparece y se ven los botones
        /// de la operación (Generar, Cargar, Mtto Prev.). Con un cargo: solo "Agregar fotos" (a ese
        /// cargo) y Eliminar (lo borra).
        /// </summary>
        private void ActualizarBotonesCargo()
        {
            if (_cargoActual != null)
            {
                AgregarButton.Label = "Agregar fotos";
                AgregarButton.Icon = new SymbolIcon(Symbol.Camera);
                ToolTipService.SetToolTip(AgregarButton, $"Agregar fotos a {_cargoActual.TituloGaleria}");
            }
            else
            {
                AgregarButton.Label = "Agregar";
                AgregarButton.Icon = new SymbolIcon(Symbol.Add);
                ToolTipService.SetToolTip(AgregarButton, "Agregar un cargo a la operación");
            }

            var conCargo = _cargoActual != null;
            var soloLectura = _operacion?.IsSharedReadOnly == true;
            static Visibility Ver(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;
            AgregarButton.Visibility = Ver(!soloLectura);
            EliminarButton.Visibility = Ver(conCargo && !soloLectura);
            VerRefaccionButton.Visibility = Ver(EsRefaccion(_cargoActual));
            GenerarButton.Visibility = Ver(!conCargo && !soloLectura);
            CargarButton.Visibility = Ver(!conCargo && !soloLectura);
            MttoPrevButton.Visibility = Ver(!conCargo && !soloLectura);
        }

        private async void AgregarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cargoActual is { } cargo)
            {
                await CargarFotosCargoAsync(cargo);
            }
            else
            {
                await AgregarCargoAsync();
            }
        }

        /// <summary>
        /// Varias imágenes de una vez al cargo. Igual que el visor: se bloquea con la operación
        /// finalizada pero no con la cotización cerrada (las fotos del trabajo van después).
        /// </summary>
        private async Task CargarFotosCargoAsync(CargoFila cargo)
        {
            if (_operacion is not { CanMutateOperation: true })
            {
                await _notificaciones.MostrarAsync("Operación finalizada", "La operación está finalizada; reábrela para cargar fotos.");
                return;
            }

            var picker = new FileOpenPicker { ViewMode = PickerViewMode.Thumbnail, SuggestedStartLocation = PickerLocationId.PicturesLibrary };
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(HostWindow ?? App.MainWindow));
            foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".bmp" }) picker.FileTypeFilter.Add(ext);

            var archivos = await picker.PickMultipleFilesAsync();
            if (archivos == null || archivos.Count == 0) return;

            InfoCargando.IsActive = true;
            var fallidas = new List<string>();
            foreach (var archivo in archivos)
            {
                try
                {
                    using var stream = await archivo.OpenStreamForReadAsync();
                    var subida = await _imagenesCargo.UploadImageAsync(_idOperacion, cargo.IdCargo, stream,
                        ImageContentTypeHelper.GetContentTypeFromExtension(archivo.FileType));
                    if (subida == null) fallidas.Add(archivo.Name);
                }
                catch (Exception)
                {
                    fallidas.Add(archivo.Name);
                }
            }

            if (fallidas.Count < archivos.Count)
            {
                _actividad.Registrar("Operaciones", "Imagen de cargo cargada");
            }

            // Recarga y vuelve a las fotos del cargo (sigue seleccionado).
            await RefrescarGaleriaAsync();
            if (fallidas.Count > 0)
            {
                await _notificaciones.MostrarAsync("Cargar fotos", $"No se pudieron guardar: {string.Join(", ", fallidas)}");
            }
        }

        private async Task MostrarGaleriaAsync()
        {
            var version = ++_versionFotos;
            var desplazamiento = GaleriaScroll.VerticalOffset;
            GaleriaContenido.Children.Clear();
            _seccionesCargo.Clear();
            MostrarMensajeInfo(null);

            if (_operacion == null)
            {
                MostrarMensajeInfo("La operación no está cargada.");
                return;
            }

            InfoCargando.IsActive = true;
            try
            {
                // Cada sección por separado: si una falla (p. ej. un archivo que no se pudo
                // descargar), las demás se muestran y la fallida aparece con su error.
                var id = _idOperacion;
                var errores = new List<string>();
                async Task<List<OperacionImageDto>> Seguro(string seccion, Task<List<OperacionImageDto>> carga)
                {
                    try
                    {
                        return await carga ?? new List<OperacionImageDto>();
                    }
                    catch (Exception ex)
                    {
                        errores.Add($"{seccion}: {ex.Message}");
                        return new List<OperacionImageDto>();
                    }
                }

                var hojas = Seguro("Hojas de servicio", _imagenesOperacion.GetHojasServicioAsync(id, _mensajeReferenciaId));
                var prefacturas = Seguro("Prefacturas", _imagenesOperacion.GetPrefacturasAsync(id, _mensajeReferenciaId));
                var ordenes = Seguro("Órdenes de compra", _imagenesOperacion.GetOrdenComprasAsync(id, _mensajeReferenciaId));
                var levantamientos = Seguro("Levantamientos", _imagenesOperacion.GetLevantamientosAsync(id, _mensajeReferenciaId));
                var mttos = Seguro("Mantenimiento preventivo", _imagenesOperacion.GetMantenimientoPreventivosAsync(id, _mensajeReferenciaId));
                var facturaSubida = Seguro("Factura", Task.Run(async () =>
                {
                    var factura = await _imagenesOperacion.GetFacturaAsync(id, _mensajeReferenciaId);
                    return factura == null ? new List<OperacionImageDto>() : new List<OperacionImageDto> { factura };
                }));
                var cargos = FilasCargos.ToList();
                var fotosCargos = Task.WhenAll(cargos.Select(async c =>
                {
                    try
                    {
                        return (Cargo: c, Fotos: await _imagenesCargo.GetImagesAsync(id, c.IdCargo, _mensajeReferenciaId));
                    }
                    catch (Exception ex)
                    {
                        errores.Add($"Fotos de {(string.IsNullOrWhiteSpace(c.Detalle) ? c.Tipo : c.Detalle)}: {ex.Message}");
                        return (Cargo: c, Fotos: new List<CargoImageDto>());
                    }
                }));
                await Task.WhenAll(hojas, prefacturas, ordenes, levantamientos, mttos, facturaSubida, fotosCargos, CargarFacturaVinculadaAsync());
                if (version != _versionFotos)
                {
                    return;
                }

                _operaciones.RefreshPdfPaths(_operacion);
                var generados = new List<(string Nombre, string Ruta)>();
                if (!string.IsNullOrEmpty(_operacion.CotizacionPdfPath)) generados.Add(("Cotización", _operacion.CotizacionPdfPath));
                if (!string.IsNullOrEmpty(_operacion.ReportePdfPath)) generados.Add(("Reporte", _operacion.ReportePdfPath));
                if (!string.IsNullOrEmpty(_operacion.NotaPdfPath)) generados.Add(("Nota", _operacion.NotaPdfPath));

                var secciones = 0;
                if (generados.Count > 0)
                {
                    secciones++;
                    AgregarSeccion("Documentos generados", generados.Select(g => CrearMiniaturaPdf(g.Nombre, async () => await AbrirArchivoAsync(g.Ruta))));
                }

                secciones += AgregarSeccionDocumentos("Hojas de servicio", hojas.Result);
                secciones += AgregarSeccionDocumentos("Prefacturas", prefacturas.Result);
                secciones += AgregarSeccionDocumentos("Órdenes de compra", ordenes.Result);
                secciones += AgregarSeccionDocumentos("Levantamientos", levantamientos.Result);

                // Factura: la CFDI vinculada (su PDF se genera al abrirla) y la subida como documento.
                var miniaturasFactura = new List<UIElement>();
                if (_facturaVinculada is { } vinculada)
                {
                    var folio = string.IsNullOrWhiteSpace(vinculada.Folio) ? $"#{vinculada.IdFactura}" : vinculada.Folio;
                    miniaturasFactura.Add(CrearMiniaturaPdf($"Factura {folio}", async () => await AbrirFacturaVinculadaAsync(vinculada)));
                }

                foreach (var documento in facturaSubida.Result)
                {
                    miniaturasFactura.Add(CrearMiniaturaPdf(documento.FileName ?? "Factura", async () => await VerDocumentoAsync(documento)));
                }

                if (miniaturasFactura.Count > 0)
                {
                    secciones++;
                    AgregarSeccion("Factura", miniaturasFactura);
                }

                var mttoLista = mttos.Result ?? new List<OperacionImageDto>();
                if (mttoLista.Count > 0)
                {
                    secciones++;
                    AgregarSeccion("Mantenimiento preventivo", mttoLista.OrderBy(d => d.ImageNumber)
                        .Select(d => CrearMiniaturaPdf($"Mtto Prev. #{d.ImageNumber}", async () => await AbrirMantenimientoExistenteAsync(d))));
                }

                // Todos los cargos tienen sección (aunque no tengan fotos) para que seleccionar
                // cualquiera lleve a algún lugar.
                var borrarFotos = _operacion?.CanMutateOperation == true;
                foreach (var (cargo, fotos) in fotosCargos.Result)
                {
                    secciones++;
                    var seccion = AgregarSeccion(cargo.TituloGaleria,
                        fotos.Select(f => CrearMiniaturaImagen(f.Url, f.FileName, async () => await _visorImagenes.ShowImageAsync(f.Url, f.FileName),
                            borrarFotos ? async () => await EliminarFotoCargoAsync(f) : null)),
                        fotos.Count == 0 ? "Sin fotos" : null);
                    _seccionesCargo[cargo.IdCargo] = seccion;
                }

                if (errores.Count > 0)
                {
                    GaleriaContenido.Children.Insert(0, new InfoBar
                    {
                        Severity = InfoBarSeverity.Warning,
                        IsOpen = true,
                        IsClosable = true,
                        Title = "Algunos archivos no se pudieron cargar",
                        Message = string.Join("\n", errores)
                    });
                }

                MostrarMensajeInfo(secciones == 0 && errores.Count == 0 ? "La operación no tiene documentos ni fotos." : null);

                // Tras recargar: mismo resalte y, si hay un cargo seleccionado, a sus fotos; si no,
                // donde estaba el usuario.
                ResaltarCargosSeleccionados();
                GaleriaContenido.UpdateLayout();
                if (_cargoActual != null)
                {
                    DesplazarACargo(_cargoActual);
                }
                else
                {
                    GaleriaScroll.ChangeView(null, desplazamiento, null, true);
                }
            }
            catch (Exception ex)
            {
                if (version == _versionFotos)
                {
                    MostrarMensajeInfo($"No se pudo cargar la galería. {ex.Message}");
                }
            }
            finally
            {
                if (version == _versionFotos)
                {
                    InfoCargando.IsActive = false;
                }
            }
        }

        private int AgregarSeccionDocumentos(string titulo, IList<OperacionImageDto>? documentos)
        {
            if (documentos == null || documentos.Count == 0)
            {
                return 0;
            }

            var eliminar = _operacion?.CanMutateOperation == true;
            AgregarSeccion(titulo, documentos.Select(d => d.IsPdf
                ? CrearMiniaturaPdf(d.FileName ?? "PDF", async () => await VerDocumentoAsync(d), eliminar ? async () => await EliminarDocumentoAsync(d) : null)
                : CrearMiniaturaImagen(d.Url, d.FileName, async () => await VerDocumentoAsync(d), eliminar ? async () => await EliminarDocumentoAsync(d) : null)));
            return 1;
        }

        /// <summary>
        /// Título y miniaturas dentro de un marco; el marco se pinta con el color de acento cuando
        /// la sección es de un cargo seleccionado.
        /// </summary>
        private Border AgregarSeccion(string titulo, IEnumerable<UIElement> miniaturas, string? vacio = null)
        {
            var panel = new StackPanel { Spacing = 6 };
            panel.Children.Add(new TextBlock { Text = titulo, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            if (vacio != null)
            {
                panel.Children.Add(new TextBlock { Text = vacio, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            }

            var rejilla = new VariableSizedWrapGrid
            {
                Orientation = Orientation.Horizontal,
                ItemWidth = LadoMiniatura + 6,
                ItemHeight = LadoMiniatura + 6
            };
            foreach (var miniatura in miniaturas)
            {
                rejilla.Children.Add(miniatura);
            }

            panel.Children.Add(rejilla);
            var marco = new Border
            {
                Child = panel,
                Padding = new Thickness(6),
                BorderThickness = new Thickness(2),
                BorderBrush = BrochaSinResalte,
                CornerRadius = new CornerRadius(6)
            };
            GaleriaContenido.Children.Add(marco);
            return marco;
        }

        private UIElement CrearMiniaturaImagen(string? url, string? nombre, Func<Task> abrir, Func<Task>? eliminar)
        {
            var boton = CrearBotonMiniatura(abrir, "Ver imagen");
            boton.Padding = new Thickness(0);
            boton.Content = new Image { Source = FilePathToImageConverter.ToImage(url), Stretch = Stretch.UniformToFill };
            return EnvolverConEliminar(boton, eliminar, nombre);
        }

        private UIElement CrearMiniaturaPdf(string nombre, Func<Task> abrir, Func<Task>? eliminar = null)
        {
            var boton = CrearBotonMiniatura(abrir, "Abrir PDF");
            boton.Padding = new Thickness(4);
            var contenido = new StackPanel { Spacing = 3, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            contenido.Children.Add(new FontIcon { Glyph = "", FontSize = 32, Foreground = BrochaPdf, HorizontalAlignment = HorizontalAlignment.Center });
            contenido.Children.Add(new TextBlock
            {
                Text = nombre,
                FontSize = 10,
                MaxLines = 2,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.WrapWholeWords,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            boton.Content = contenido;
            return EnvolverConEliminar(boton, eliminar, nombre);
        }

        private Button CrearBotonMiniatura(Func<Task> abrir, string tooltip)
        {
            var boton = new Button
            {
                Width = LadoMiniatura,
                Height = LadoMiniatura,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"]
            };
            ToolTipService.SetToolTip(boton, tooltip);
            boton.Click += async (_, _) => await abrir();
            return boton;
        }

        private static UIElement EnvolverConEliminar(Button miniatura, Func<Task>? eliminar, string? nombre)
        {
            if (eliminar == null)
            {
                return miniatura;
            }

            var contenedor = new Grid { Width = LadoMiniatura, Height = LadoMiniatura };
            contenedor.Children.Add(miniatura);
            var quitar = new Button
            {
                MinWidth = 18,
                MinHeight = 18,
                Padding = new Thickness(3),
                Margin = new Thickness(0, 2, 2, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush(ColorHelper.FromArgb(0xCC, 0, 0, 0)),
                Content = new SymbolIcon { Symbol = Symbol.Cancel, Foreground = new SolidColorBrush(Colors.White) }
            };
            ToolTipService.SetToolTip(quitar, string.IsNullOrEmpty(nombre) ? "Eliminar archivo" : $"Eliminar {nombre}");
            quitar.Click += async (_, _) => await eliminar();
            contenedor.Children.Add(quitar);
            return contenedor;
        }

        // ---------------- Abrir / eliminar documentos (igual que el visor) ----------------

        private static async Task AbrirArchivoAsync(string ruta)
        {
            try
            {
                var archivo = await WinStorage.StorageFile.GetFileFromPathAsync(ruta);
                await WinSystem.Launcher.LaunchFileAsync(archivo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProOperaciones] AbrirArchivo: {ex.Message}");
            }
        }

        private async Task VerDocumentoAsync(OperacionImageDto doc)
        {
            if (string.IsNullOrEmpty(doc.Url) || _operacion == null)
            {
                return;
            }

            try
            {
                if (!doc.IsPdf)
                {
                    await _visorImagenes.ShowImageAsync(doc.Url, doc.Tipo);
                    return;
                }

                var contactos = await ContactosClienteAsync();
                var principal = contactos.FirstOrDefault();
                var tipo = doc.Tipo switch
                {
                    "HojaServicio" => "Hoja de Servicio",
                    "OrdenCompra" => "Orden de Compra",
                    _ => doc.Tipo ?? "Documento"
                };
                await MostrarVisorPdfAsync(doc.Url, principal, contactos, tipo);
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Error", $"No se pudo abrir el archivo. {ex.Message}");
            }
        }

        private async Task EliminarDocumentoAsync(OperacionImageDto doc)
        {
            if (string.IsNullOrEmpty(doc.FileName) || _operacion is not { CanMutateOperation: true })
            {
                await _notificaciones.MostrarAsync("Operación finalizada", "La operación no permite eliminar archivos.");
                return;
            }

            if (!await ConfirmarAsync("Confirmar eliminación", $"¿Eliminar el archivo {doc.FileName}?", "Eliminar"))
            {
                return;
            }

            try
            {
                if (await _imagenesOperacion.DeleteImageAsync(doc.IdOperacion, doc.FileName))
                {
                    _actividad.Registrar("Operaciones", "Doc. op. eliminado");
                    await RefrescarGaleriaAsync();
                }
                else
                {
                    await _notificaciones.MostrarAsync("Error", "No se pudo eliminar el archivo.");
                }
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Error", $"Ocurrió un error al eliminar el archivo. {ex.Message}");
            }
        }

        // ---------------- Generar ----------------

        private void GenerarFlyout_Opening(object sender, object e)
        {
            if (_operacion == null)
            {
                return;
            }

            _operaciones.RefreshPdfPaths(_operacion);
            AbrirCotizacionItem.Visibility = string.IsNullOrEmpty(_operacion.CotizacionPdfPath) ? Visibility.Collapsed : Visibility.Visible;
            AbrirReporteItem.Visibility = string.IsNullOrEmpty(_operacion.ReportePdfPath) ? Visibility.Collapsed : Visibility.Visible;
            AbrirNotaItem.Visibility = string.IsNullOrEmpty(_operacion.NotaPdfPath) ? Visibility.Collapsed : Visibility.Visible;
            GenerarNotaItem.IsEnabled = _operacion.IsTrabajoFinalizado;
            AbrirNotaItem.IsEnabled = _operacion.IsTrabajoFinalizado;
            GenerarFiniquitoItem.IsEnabled = _operacion.EstaFacturada;
        }

        private async void AbrirCotizacionPdf_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_operacion?.CotizacionPdfPath)) await AbrirArchivoAsync(_operacion.CotizacionPdfPath);
        }

        private async void AbrirReportePdf_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_operacion?.ReportePdfPath)) await AbrirArchivoAsync(_operacion.ReportePdfPath);
        }

        private async void AbrirNotaPdf_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_operacion?.NotaPdfPath)) await AbrirArchivoAsync(_operacion.NotaPdfPath);
        }

        private async void GenerarCotizacion_Click(object sender, RoutedEventArgs e)
        {
            var op = _operacion;
            if (op == null) return;
            if (op.Cargos.Count == 0) { await _notificaciones.MostrarAsync("Sin cargos", "No hay cargos para generar la cotización."); return; }
            try
            {
                if (!string.IsNullOrEmpty(_operaciones.FindExistingPdf(_idOperacion, "Cotizacion")))
                    _operaciones.DeleteOperacionPdfs(_idOperacion, "Cotizacion");

                // Va dirigida siempre al contacto dirigido de la operación; solo sin dirigido se pregunta.
                var contactos = await ContactosClienteAsync();
                var contacto = _aprobacion?.IdContactoDirigido is long idDirigido ? contactos.FirstOrDefault(c => c.ContactoId == idDirigido) : null;
                contacto ??= await ElegirContactoAsync(contactos, "¿A quién va dirigida la cotización?");

                if (!await VerificarFirmasAsync()) return;

                var ruta = await _operaciones.GenerateQuoteAsync(op, NombreDirigido(contacto));
                if (string.IsNullOrEmpty(ruta)) { await _notificaciones.MostrarAsync("Error", "No se pudo generar la cotización."); return; }

                _actividad.Registrar("Operaciones", "Cotización generada");
                op.CotizacionPdfPath = ruta;
                await _operaciones.UpdateCheckAsync(op, "cotizacion_generada");
                await DespuesDeCambiarCheckAsync();
                if (await MostrarVisorPdfAsync(ruta, contacto, contactos, "Cotización"))
                {
                    await _operaciones.UpdateCheckAsync(op, "cotizacion_enviada");
                    await DespuesDeCambiarCheckAsync();
                    await _notificaciones.MostrarAsync("Correo enviado", "La cotización fue enviada correctamente.");
                }
            }
            catch (Exception ex) { await _notificaciones.MostrarAsync("Error", $"Ocurrió un error al generar la cotización. {ex.Message}"); }
        }

        private async void GenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            var op = _operacion;
            if (op == null) return;
            if (op.Cargos.Count == 0) { await _notificaciones.MostrarAsync("Sin cargos", "No hay cargos para generar el reporte."); return; }
            try
            {
                if (!string.IsNullOrEmpty(_operaciones.FindExistingPdf(_idOperacion, "Reporte")))
                    _operaciones.DeleteOperacionPdfs(_idOperacion, "Reporte");

                var contactos = await ContactosClienteAsync();
                var contacto = await ElegirContactoAsync(contactos, "¿A quién va dirigido el reporte?");

                if (!await VerificarFirmasAsync()) return;

                var ruta = await _operaciones.GenerateReporteAsync(op, NombreDirigido(contacto));
                if (string.IsNullOrEmpty(ruta))
                {
                    var msg = !string.IsNullOrWhiteSpace(_operaciones.ErrorMessage) ? _operaciones.ErrorMessage : "No se pudo generar el reporte.";
                    await _notificaciones.MostrarAsync("Error al generar reporte", msg);
                    return;
                }

                _actividad.Registrar("Operaciones", "Reporte generado");
                op.ReportePdfPath = ruta;
                await _operaciones.UpdateCheckAsync(op, "reporte_generado");
                await DespuesDeCambiarCheckAsync();
                if (await MostrarVisorPdfAsync(ruta, contacto, contactos, "Reporte", tFinalizado: op.IsTrabajoFinalizado))
                {
                    await _operaciones.UpdateCheckAsync(op, "reporte_enviado");
                    await DespuesDeCambiarCheckAsync();
                    await _notificaciones.MostrarAsync("Correo enviado", "El reporte fue enviado correctamente.");
                }
            }
            catch (Exception ex) { await _notificaciones.MostrarAsync("Error", $"Ocurrió un error al generar el reporte. {ex.Message}"); }
        }

        private async void GenerarNota_Click(object sender, RoutedEventArgs e)
        {
            var op = _operacion;
            if (op == null) return;
            if (!op.IsTrabajoFinalizado) { await _notificaciones.MostrarAsync("Trabajo no finalizado", "El trabajo debe estar finalizado para generar una nota."); return; }
            if (op.Cargos.Count == 0) { await _notificaciones.MostrarAsync("Sin cargos", "No hay cargos para generar la nota."); return; }
            try
            {
                if (!string.IsNullOrEmpty(_operaciones.FindExistingPdf(_idOperacion, "Nota")))
                    _operaciones.DeleteOperacionPdfs(_idOperacion, "Nota");

                var contactos = await ContactosClienteAsync();
                var contacto = await ElegirContactoAsync(contactos, "¿A quién va dirigida la nota?");

                if (!await VerificarFirmasAsync()) return;

                var ruta = await _operaciones.GenerateNotaAsync(op, NombreDirigido(contacto));
                if (string.IsNullOrEmpty(ruta))
                {
                    var msg = !string.IsNullOrWhiteSpace(_operaciones.ErrorMessage) ? _operaciones.ErrorMessage : "No se pudo generar la nota.";
                    await _notificaciones.MostrarAsync("Error al generar nota", msg);
                    return;
                }

                _actividad.Registrar("Operaciones", "Nota generada");
                op.NotaPdfPath = ruta;
                await RefrescarGaleriaAsync();
                if (await MostrarVisorPdfAsync(ruta, contacto, contactos, "Nota"))
                {
                    await _notificaciones.MostrarAsync("Correo enviado", "La nota fue enviada correctamente.");
                }
            }
            catch (Exception ex) { await _notificaciones.MostrarAsync("Error", $"Ocurrió un error al generar la nota. {ex.Message}"); }
        }

        /// <summary>Igual que el visor: cotización y reporte (reutilizados si existen), PDF y XML de la factura, todo por correo.</summary>
        private async void GenerarFiniquito_Click(object sender, RoutedEventArgs e)
        {
            var op = _operacion;
            if (op == null) return;
            if (!op.EstaFacturada || !op.IdFacturaVinculada.HasValue)
            {
                await _notificaciones.MostrarAsync("Operación no facturada", "El finiquito solo se puede generar cuando la operación ya tiene una factura vinculada.");
                return;
            }
            if (op.Cargos.Count == 0) { await _notificaciones.MostrarAsync("Sin cargos", "No hay cargos para generar el finiquito."); return; }

            try
            {
                var contactos = await ContactosClienteAsync();
                var contacto = await ElegirContactoAsync(contactos, "¿A quién va dirigido el finiquito?");
                var dirigidoA = NombreDirigido(contacto);

                if (!await VerificarFirmasAsync()) return;

                _operaciones.RefreshPdfPaths(op);
                var cotizacion = op.CotizacionPdfPath;
                if (string.IsNullOrEmpty(cotizacion)) cotizacion = await _operaciones.GenerateQuoteAsync(op, dirigidoA);
                if (string.IsNullOrEmpty(cotizacion)) { await _notificaciones.MostrarAsync("Error", "No se pudo generar la cotización para el finiquito."); return; }
                op.CotizacionPdfPath = cotizacion;

                var reporte = op.ReportePdfPath;
                if (string.IsNullOrEmpty(reporte)) reporte = await _operaciones.GenerateReporteAsync(op, dirigidoA);
                if (string.IsNullOrEmpty(reporte)) { await _notificaciones.MostrarAsync("Error", "No se pudo generar el reporte para el finiquito."); return; }
                op.ReportePdfPath = reporte;

                var idFactura = op.IdFacturaVinculada.Value;
                var detalle = await _facturas.ObtenerDetalleFacturaAsync(idFactura);
                if (detalle?.Factura == null) { await _notificaciones.MostrarAsync("Error", "No se pudo obtener el detalle de la factura para el finiquito."); return; }
                var facturaPdf = await _facturaPdf.GenerarFacturaPdfAsync(detalle);
                var xml = await _facturas.ObtenerXmlFacturaAsync(idFactura);
                if (string.IsNullOrEmpty(xml)) { await _notificaciones.MostrarAsync("Error", "No se pudo obtener el XML de la factura para el finiquito."); return; }

                var nombreBase = !string.IsNullOrWhiteSpace(detalle.Factura.Uuid) ? detalle.Factura.Uuid : detalle.Factura.FolioTitulo;
                var adjuntos = new List<(string NombreArchivo, byte[] Contenido)>
                {
                    (Path.GetFileName(reporte), await File.ReadAllBytesAsync(reporte)),
                    (Path.GetFileName(facturaPdf), await File.ReadAllBytesAsync(facturaPdf)),
                    ($"{nombreBase}.xml", System.Text.Encoding.UTF8.GetBytes(xml))
                };

                var correo = new EnviarCotizacionDialog(cotizacion, contacto, contactos, op.RazonSocial ?? string.Empty, XamlRoot,
                    tipo: "Finiquito", idOperacion: _idOperacion, adjuntosAdicionales: adjuntos, idCliente: op.IdCliente);
                if (await correo.ShowAsync() == ContentDialogResult.Primary)
                {
                    _actividad.Registrar("Operaciones", "Finiquito enviado");
                    await _notificaciones.MostrarAsync("Correo enviado", "El finiquito fue enviado correctamente.");
                }

                await RefrescarGaleriaAsync();
            }
            catch (Exception ex) { await _notificaciones.MostrarAsync("Error", $"Ocurrió un error al generar el finiquito. {ex.Message}"); }
        }

        // ---------------- Cargar ----------------

        private void CargarFlyout_Opening(object sender, object e)
        {
            var puede = _operacion?.CanMutateOperation == true;
            CargarPrefacturaItem.IsEnabled = puede;
            CargarHojaServicioItem.IsEnabled = puede;
            CargarOrdenCompraItem.IsEnabled = puede;
            CargarLevantamientoItem.IsEnabled = puede;
        }

        private async void CargarPrefactura_Click(object sender, RoutedEventArgs e) => await CargarDocumentoAsync("Prefactura");
        private async void CargarHojaServicio_Click(object sender, RoutedEventArgs e) => await CargarDocumentoAsync("HojaServicio");
        private async void CargarOrdenCompra_Click(object sender, RoutedEventArgs e) => await CargarDocumentoAsync("OrdenCompra");
        private async void CargarLevantamiento_Click(object sender, RoutedEventArgs e) => await CargarDocumentoAsync("Levantamiento");

        private async Task CargarDocumentoAsync(string tipo)
        {
            var op = _operacion;
            if (op is not { CanMutateOperation: true })
            {
                await _notificaciones.MostrarAsync("Operación finalizada", "La operación no permite cargar documentos.");
                return;
            }

            try
            {
                // La orden de compra aprueba la cotización: primero el monto que cubre (antes de IVA).
                decimal? subtotalOc = null;
                if (tipo == "OrdenCompra")
                {
                    subtotalOc = await AprobacionDialogs.SubtotalOrdenCompraAsync(XamlRoot, _aprobacion is { CotFinalizada: true } ? _aprobacion.Subtotal : null);
                    if (subtotalOc == null) return;
                }

                var tipoDialogo = new ContentDialog
                {
                    Title = "Tipo de archivo",
                    Content = "¿Qué tipo de archivo desea cargar?",
                    PrimaryButtonText = "Imagen",
                    SecondaryButtonText = "PDF",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };
                var resultado = await tipoDialogo.ShowAsync();
                if (resultado == ContentDialogResult.None) return;
                var esPdf = resultado == ContentDialogResult.Secondary;

                var picker = new FileOpenPicker { ViewMode = PickerViewMode.Thumbnail };
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(HostWindow ?? App.MainWindow));
                if (esPdf)
                {
                    picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    picker.FileTypeFilter.Add(".pdf");
                }
                else
                {
                    picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                    foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" }) picker.FileTypeFilter.Add(ext);
                }

                var archivo = await picker.PickSingleFileAsync();
                if (archivo == null) return;

                using var stream = await archivo.OpenStreamForReadAsync();
                var contentType = esPdf ? "application/pdf" : ImageContentTypeHelper.GetContentTypeFromExtension(archivo.FileType);
                var id = _idOperacion;
                OperacionImageDto? subido = tipo switch
                {
                    "Prefactura" => await _imagenesOperacion.UploadPrefacturaAsync(id, stream, contentType),
                    "HojaServicio" => await _imagenesOperacion.UploadHojaServicioAsync(id, stream, contentType),
                    "OrdenCompra" => await _imagenesOperacion.UploadOrdenCompraConMontoAsync(id, stream, contentType, subtotalOc!.Value),
                    "Levantamiento" => await _imagenesOperacion.UploadLevantamientoAsync(id, stream, contentType),
                    _ => null
                };
                if (subido == null)
                {
                    await _notificaciones.MostrarAsync("Error", $"No se pudo guardar la {tipo.ToLower()}.");
                    return;
                }

                _actividad.Registrar("Operaciones", $"{tipo} cargada");
                var campo = tipo switch { "Prefactura" => "prefactura_cargada", "HojaServicio" => "hoja_servicio_cargada", "OrdenCompra" => "orden_compra_cargada", _ => null };
                if (campo != null)
                {
                    await _operaciones.UpdateCheckAsync(op, campo);
                }

                if (tipo == "OrdenCompra")
                {
                    // Aprueba la cotización: refresca estado y la línea de tiempo.
                    _aprobacion = await _aprobaciones.ObtenerOperacionAsync(id);
                    op.CotFinalizada = _aprobacion.CotFinalizada;
                    CotizacionEstadoText.Text = _aprobacion.EstadoTexto ?? string.Empty;
                    await _operaciones.LoadCheckAsync(op);
                }

                await DespuesDeCambiarCheckAsync();
                await _notificaciones.MostrarAsync($"{tipo} cargada", $"{subido.FileName} guardada correctamente.");
            }
            catch (InvalidOperationException ex) when (tipo == "OrdenCompra")
            {
                // Monto que no coincide, cotización sin cerrar o rechazada: el mensaje de la API explica qué hacer.
                await _notificaciones.MostrarAsync("Orden de compra", ex.Message);
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Error", $"Ocurrió un error al cargar la {tipo.ToLower()}. {ex.Message}");
            }
        }

        // ---------------- Mtto Prev. ----------------

        private async void MttoPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_operacion == null) return;
            try
            {
                List<OperacionImageDto> existentes = [];
                try { existentes = await _imagenesOperacion.GetMantenimientoPreventivosAsync(_idOperacion) ?? []; }
                catch (Exception) { /* sin lista: se abre uno nuevo */ }

                if (existentes.Count == 0)
                {
                    await AbrirMantenimientoNuevoAsync();
                    return;
                }

                var flyout = new MenuFlyout();
                foreach (var doc in existentes.OrderBy(d => d.ImageNumber))
                {
                    var fecha = !string.IsNullOrEmpty(doc.Url) && File.Exists(doc.Url) ? File.GetLastWriteTime(doc.Url).ToString("dd/MM/yyyy HH:mm") : null;
                    var item = new MenuFlyoutItem { Text = fecha != null ? $"Mtto Prev. #{doc.ImageNumber} — {fecha}" : $"Mtto Prev. #{doc.ImageNumber}" };
                    item.Click += async (_, _) => await AbrirMantenimientoExistenteAsync(doc);
                    flyout.Items.Add(item);
                }

                flyout.Items.Add(new MenuFlyoutSeparator());
                var nuevo = new MenuFlyoutItem { Text = "Nuevo" };
                nuevo.Click += async (_, _) => await AbrirMantenimientoNuevoAsync();
                flyout.Items.Add(nuevo);
                flyout.ShowAt((FrameworkElement)sender);
            }
            catch (Exception ex) { await _notificaciones.MostrarAsync("Mtto Prev.", ex.Message); }
        }

        private async Task AbrirMantenimientoExistenteAsync(OperacionImageDto doc)
        {
            if (string.IsNullOrEmpty(doc.Url)) return;
            try { await MostrarVisorPdfAsync(doc.Url, null, [], "Mantenimiento Preventivo"); }
            catch (Exception ex) { await _notificaciones.MostrarAsync("Mtto Prev.", ex.Message); }
        }

        /// <summary>La hoja va dirigida al contacto dirigido de la operación (el único que la aprueba).</summary>
        private async Task AbrirMantenimientoNuevoAsync()
        {
            if (_operacion?.IdCliente is not int idCliente) return;
            try
            {
                if (_aprobacion?.IdContactoDirigido is not long)
                {
                    var elegido = await SeleccionarDirigidoDialog.ElegirAsync(XamlRoot, idCliente, "¿A quién va dirigida la operación?", actual: null);
                    if (elegido == null) return;
                    _aprobacion = await _aprobaciones.SetDirigidoAsync(_idOperacion, elegido.ContactoId);
                    _actividad.Registrar("Operaciones", "Contacto dirigido asignado");
                    CotizacionEstadoText.Text = _aprobacion.EstadoTexto ?? string.Empty;
                    await CargarContactosAsync();
                }

                var contactos = await ContactosClienteAsync();
                var contacto = contactos.FirstOrDefault(c => c.ContactoId == _aprobacion!.IdContactoDirigido);
                new Formularios.MantenimientoPreventivoWindow(_operacion, contacto).Activate();
            }
            catch (InvalidOperationException ex) { await _notificaciones.MostrarAsync("Contacto dirigido", ex.Message); }
            catch (Exception ex) { await _notificaciones.MostrarAsync("Mtto Prev.", ex.Message); }
        }

        // ---------------- Apoyo ----------------

        private async Task<List<ContactoDto>> ContactosClienteAsync()
        {
            if (_operacion?.IdCliente is not int idCliente || idCliente <= 0) return [];
            try { return await _contactos.GetContactosAsync(new ContactoQueryDto { IdCliente = idCliente }) ?? []; }
            catch (Exception) { return []; }
        }

        private async Task<ContactoDto?> ElegirContactoAsync(List<ContactoDto> contactos, string titulo)
        {
            if (contactos.Count == 0) return null;
            var lista = new ListView { ItemsSource = contactos, DisplayMemberPath = "NombreCompleto", SelectionMode = ListViewSelectionMode.Single, MaxHeight = 300 };
            var dialogo = new ContentDialog
            {
                Title = titulo,
                Content = new ScrollViewer { Content = lista, MaxHeight = 320 },
                PrimaryButtonText = "Seleccionar",
                SecondaryButtonText = "Omitir",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            return await dialogo.ShowAsync() == ContentDialogResult.Primary ? lista.SelectedItem as ContactoDto : null;
        }

        private static string? NombreDirigido(ContactoDto? c) =>
            c == null ? null : string.Join(" ", new[] { c.Tratamiento, c.Nombre, c.Apellido }.Where(s => !string.IsNullOrWhiteSpace(s)));

        /// <summary>Si falta la firma de dirección o la de quien atiende, pide cargarlas antes del PDF.</summary>
        private async Task<bool> VerificarFirmasAsync()
        {
            var idAtiende = _operacion?.IdAtiende;
            var faltaDireccion = !_firmas.ExisteFirmaDireccion();
            var faltaOperador = idAtiende.HasValue && !_firmas.ExisteFirmaOperador(idAtiende.Value);
            if (!faltaDireccion && !faltaOperador) return true;

            var dialogo = new ConfigurarFirmasDialog(idAtiende, _operacion?.Atiende ?? string.Empty, _firmas, XamlRoot,
                WinRT.Interop.WindowNative.GetWindowHandle(HostWindow ?? App.MainWindow));
            return await dialogo.ShowAsync() == ContentDialogResult.Primary;
        }

        /// <summary>Visor de PDF del visor de operaciones; devuelve true si se envió por correo.</summary>
        private async Task<bool> MostrarVisorPdfAsync(string ruta, ContactoDto? contacto, List<ContactoDto> contactos, string tipo, bool tFinalizado = false)
        {
            var razon = _operacion?.RazonSocial ?? string.Empty;
            var visor = tipo == "Cotización"
                ? new CotizacionVisorDialog(ruta, contacto, contactos, razon, XamlRoot)
                : new CotizacionVisorDialog(ruta, contacto, contactos, razon, XamlRoot, tipo: tipo);
            visor.NotificarResultado(await visor.ShowAsync());

            if (visor.Resultado == CotizacionVisorResultado.EnviarCorreo)
            {
                var correo = tipo == "Cotización"
                    ? new EnviarCotizacionDialog(ruta, contacto, contactos, razon, XamlRoot, idOperacion: _idOperacion, idCliente: _operacion?.IdCliente)
                    : new EnviarCotizacionDialog(ruta, contacto, contactos, razon, XamlRoot, tipo: tipo, idOperacion: _idOperacion, tFinalizado: tFinalizado, idCliente: _operacion?.IdCliente);
                return await correo.ShowAsync() == ContentDialogResult.Primary;
            }

            if (visor.Resultado == CotizacionVisorResultado.AbrirExterno)
            {
                await AbrirArchivoAsync(ruta);
            }

            return false;
        }

        /// <summary>Factura CFDI vinculada a la operación (null si no está facturada).</summary>
        private OperacionFacturadaDto? _facturaVinculada;

        /// <summary>Genera el PDF de la factura CFDI vinculada y lo abre en el visor (con opción de correo).</summary>
        private async Task AbrirFacturaVinculadaAsync(OperacionFacturadaDto factura)
        {
            InfoCargando.IsActive = true;
            try
            {
                var detalle = await _facturas.ObtenerDetalleFacturaAsync(factura.IdFactura);
                if (detalle?.Factura == null)
                {
                    await _notificaciones.MostrarAsync("Factura", "No se pudo obtener el detalle de la factura.");
                    return;
                }

                var ruta = await _facturaPdf.GenerarFacturaPdfAsync(detalle);
                InfoCargando.IsActive = false;
                var contactos = await ContactosClienteAsync();
                await MostrarVisorPdfAsync(ruta, contactos.FirstOrDefault(), contactos, "Factura");
            }
            catch (Exception ex)
            {
                await _notificaciones.MostrarAsync("Factura", $"No se pudo abrir la factura. {ex.Message}");
            }
            finally
            {
                InfoCargando.IsActive = false;
            }
        }

        /// <summary>Factura CFDI vinculada a la operación (habilita "Generar Finiquito").</summary>
        private async Task CargarFacturaVinculadaAsync()
        {
            if (_operacion == null) return;
            try
            {
                var facturadas = await _facturas.ObtenerOperacionesFacturadasAsync();
                _facturaVinculada = facturadas.FirstOrDefault(f => f.IdOperacion == _idOperacion);
                _operacion.IdFacturaVinculada = _facturaVinculada?.IdFactura;
            }
            catch (Exception)
            {
                // Sin la factura solo queda deshabilitado el finiquito.
            }
        }

        /// <summary>El check cambió: línea de tiempo (con historial) y galería al día.</summary>
        private async Task DespuesDeCambiarCheckAsync()
        {
            await CargarHistorialChecksAsync();
            ConstruirLineaTiempo();
            ActualizarBotones();
            await RefrescarGaleriaAsync();
        }
    }
}
