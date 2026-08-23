using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Advance_Control.Models;
using Advance_Control.ViewModels;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Logging;
using Advance_Control.Services.ConfiguracionEmisor;
using Advance_Control.Services.SatCatalogo;
using Advance_Control.Utilities;
using global::Windows.Storage;
using global::Windows.Storage.Pickers;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Página para visualizar y gestionar entidades
    /// </summary>
    public sealed partial class EntidadesPage : Page
    {
        public EntidadesViewModel ViewModel { get; }
        private readonly INotificacionService _notificacionService;
        private readonly ILoggingService _loggingService;
        private readonly IConfiguracionEmisorService _configuracionEmisorService;
        private readonly ISatCatalogoService _satCatalogoService;

        public EntidadesPage()
        {
            // Resolver el ViewModel desde DI
            ViewModel = AppServices.Get<EntidadesViewModel>();

            // Resolver el servicio de notificaciones desde DI
            _notificacionService = AppServices.Get<INotificacionService>();

            // Resolver el servicio de logging desde DI
            _loggingService = AppServices.Get<ILoggingService>();

            // Resolver el servicio de configuración del emisor/CSD desde DI
            _configuracionEmisorService = AppServices.Get<IConfiguracionEmisorService>();

            // Resolver el servicio de catálogos SAT (Régimen Fiscal, Uso de CFDI) desde DI
            _satCatalogoService = AppServices.Get<ISatCatalogoService>();

            this.InitializeComponent();
            ButtonClickLogger.Attach(this, _loggingService, nameof(EntidadesPage));
            
            // Establecer el DataContext para los bindings
            this.DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Cargar las entidades cuando se navega a esta página
            await ViewModel.LoadEntidadesAsync();
        }

        private async void FiltroTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != global::Windows.System.VirtualKey.Enter)
            {
                return;
            }

            e.Handled = true;
            await ViewModel.LoadEntidadesAsync();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearFiltersAsync();
        }

        private async void NuevoButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear los campos del formulario
            var nombreComercialTextBox = new TextBox
            {
                PlaceholderText = "Nombre comercial (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var razonSocialTextBox = new TextBox
            {
                PlaceholderText = "Razón social (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var rfcTextBox = new TextBox
            {
                PlaceholderText = "RFC (opcional)",
                MaxLength = 13,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var calleTextBox = new TextBox
            {
                PlaceholderText = "Calle (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var numExtTextBox = new TextBox
            {
                PlaceholderText = "Número exterior (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var numIntTextBox = new TextBox
            {
                PlaceholderText = "Número interior (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var coloniaTextBox = new TextBox
            {
                PlaceholderText = "Colonia (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var cpTextBox = new TextBox
            {
                PlaceholderText = "Código postal (opcional)",
                MaxLength = 5,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var ciudadTextBox = new TextBox
            {
                PlaceholderText = "Ciudad (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var estadoTextBox = new TextBox
            {
                PlaceholderText = "Estado (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var paisTextBox = new TextBox
            {
                PlaceholderText = "País (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var apoderadoTextBox = new TextBox
            {
                PlaceholderText = "Apoderado (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var regimenFiscalTextBox = new TextBox
            {
                PlaceholderText = "Régimen fiscal SAT, ej. 601 (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var dialogContent = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Nombre Comercial:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        nombreComercialTextBox,
                        new TextBlock { Text = "Razón Social:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        razonSocialTextBox,
                        new TextBlock { Text = "RFC:" },
                        rfcTextBox,
                        new TextBlock { Text = "Régimen Fiscal:" },
                        regimenFiscalTextBox,
                        new TextBlock { Text = "Calle:" },
                        calleTextBox,
                        new TextBlock { Text = "Número Exterior:" },
                        numExtTextBox,
                        new TextBlock { Text = "Número Interior:" },
                        numIntTextBox,
                        new TextBlock { Text = "Colonia:" },
                        coloniaTextBox,
                        new TextBlock { Text = "Código Postal:" },
                        cpTextBox,
                        new TextBlock { Text = "Ciudad:" },
                        ciudadTextBox,
                        new TextBlock { Text = "Estado:" },
                        estadoTextBox,
                        new TextBlock { Text = "País:" },
                        paisTextBox,
                        new TextBlock { Text = "Apoderado:" },
                        apoderadoTextBox
                    }
                },
                MaxHeight = 500
            };

            var dialog = new ContentDialog
            {
                Title = "Nueva Entidad",
                Content = dialogContent,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Validar campos requeridos
                if (string.IsNullOrWhiteSpace(nombreComercialTextBox.Text))
                {
                    await _notificacionService.MostrarAsync("Validación", "El nombre comercial es obligatorio");
                    return;
                }

                if (string.IsNullOrWhiteSpace(razonSocialTextBox.Text))
                {
                    await _notificacionService.MostrarAsync("Validación", "La razón social es obligatoria");
                    return;
                }

                try
                {
                    var success = await ViewModel.CreateEntidadAsync(
                        nombreComercial: nombreComercialTextBox.Text.Trim(),
                        razonSocial: razonSocialTextBox.Text.Trim(),
                        rfc: string.IsNullOrWhiteSpace(rfcTextBox.Text) ? null : rfcTextBox.Text.Trim(),
                        cp: string.IsNullOrWhiteSpace(cpTextBox.Text) ? null : cpTextBox.Text.Trim(),
                        estado: string.IsNullOrWhiteSpace(estadoTextBox.Text) ? null : estadoTextBox.Text.Trim(),
                        ciudad: string.IsNullOrWhiteSpace(ciudadTextBox.Text) ? null : ciudadTextBox.Text.Trim(),
                        pais: string.IsNullOrWhiteSpace(paisTextBox.Text) ? null : paisTextBox.Text.Trim(),
                        calle: string.IsNullOrWhiteSpace(calleTextBox.Text) ? null : calleTextBox.Text.Trim(),
                        numExt: string.IsNullOrWhiteSpace(numExtTextBox.Text) ? null : numExtTextBox.Text.Trim(),
                        numInt: string.IsNullOrWhiteSpace(numIntTextBox.Text) ? null : numIntTextBox.Text.Trim(),
                        colonia: string.IsNullOrWhiteSpace(coloniaTextBox.Text) ? null : coloniaTextBox.Text.Trim(),
                        apoderado: string.IsNullOrWhiteSpace(apoderadoTextBox.Text) ? null : apoderadoTextBox.Text.Trim(),
                        regimenFiscal: string.IsNullOrWhiteSpace(regimenFiscalTextBox.Text) ? null : regimenFiscalTextBox.Text.Trim()
                    );

                    if (success)
                    {
                        await _notificacionService.MostrarAsync("Entidad creada", $"Entidad \"{nombreComercialTextBox.Text.Trim()}\" creada correctamente");
                    }
                    else
                    {
                        await _notificacionService.MostrarAsync("Error", "No se pudo crear la entidad. Verifique los datos e intente nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync("Error al crear entidad desde la UI", ex, "EntidadesPage", "NuevoButton_Click");
                    
                    await _notificacionService.MostrarAsync("Error", "Ocurrió un error al crear la entidad. Por favor, intente nuevamente.");
                }
            }
        }

        private void HeadGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Get the EntidadDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.EntidadDto entidad)
            {
                entidad.Expand = !entidad.Expand;
            }
        }

        private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the EntidadDto from the sender's Tag property
            if (sender is FrameworkElement element && element.Tag is Models.EntidadDto entidad)
            {
                entidad.Expand = !entidad.Expand;
            }
        }

        private async void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Models.EntidadDto entidad)
                return;

            var nombreComercialTextBox = new TextBox
            {
                Text = entidad.NombreComercial ?? "",
                PlaceholderText = "Nombre comercial (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var razonSocialTextBox = new TextBox
            {
                Text = entidad.RazonSocial ?? "",
                PlaceholderText = "Razón social (requerido)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var rfcTextBox = new TextBox
            {
                Text = entidad.RFC ?? "",
                PlaceholderText = "RFC (opcional)",
                MaxLength = 13,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var regimenFiscalTextBox = new TextBox
            {
                Text = entidad.RegimenFiscal ?? "",
                PlaceholderText = "Régimen fiscal SAT, ej. 601 (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var calleTextBox = new TextBox
            {
                Text = entidad.Calle ?? "",
                PlaceholderText = "Calle (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var numExtTextBox = new TextBox
            {
                Text = entidad.NumExt ?? "",
                PlaceholderText = "Número exterior (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var numIntTextBox = new TextBox
            {
                Text = entidad.NumInt ?? "",
                PlaceholderText = "Número interior (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var coloniaTextBox = new TextBox
            {
                Text = entidad.Colonia ?? "",
                PlaceholderText = "Colonia (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var cpTextBox = new TextBox
            {
                Text = entidad.CP ?? "",
                PlaceholderText = "Código postal (opcional)",
                MaxLength = 5,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var ciudadTextBox = new TextBox
            {
                Text = entidad.Ciudad ?? "",
                PlaceholderText = "Ciudad (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var estadoTextBox = new TextBox
            {
                Text = entidad.Estado ?? "",
                PlaceholderText = "Estado (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var paisTextBox = new TextBox
            {
                Text = entidad.Pais ?? "",
                PlaceholderText = "País (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var apoderadoTextBox = new TextBox
            {
                Text = entidad.Apoderado ?? "",
                PlaceholderText = "Apoderado (opcional)",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var dialogContent = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Nombre Comercial:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        nombreComercialTextBox,
                        new TextBlock { Text = "Razón Social:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                        razonSocialTextBox,
                        new TextBlock { Text = "RFC:" },
                        rfcTextBox,
                        new TextBlock { Text = "Régimen Fiscal:" },
                        regimenFiscalTextBox,
                        new TextBlock { Text = "Calle:" },
                        calleTextBox,
                        new TextBlock { Text = "Número Exterior:" },
                        numExtTextBox,
                        new TextBlock { Text = "Número Interior:" },
                        numIntTextBox,
                        new TextBlock { Text = "Colonia:" },
                        coloniaTextBox,
                        new TextBlock { Text = "Código Postal:" },
                        cpTextBox,
                        new TextBlock { Text = "Ciudad:" },
                        ciudadTextBox,
                        new TextBlock { Text = "Estado:" },
                        estadoTextBox,
                        new TextBlock { Text = "País:" },
                        paisTextBox,
                        new TextBlock { Text = "Apoderado:" },
                        apoderadoTextBox
                    }
                },
                MaxHeight = 500
            };

            var dialog = new ContentDialog
            {
                Title = "Editar Entidad",
                Content = dialogContent,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            if (string.IsNullOrWhiteSpace(nombreComercialTextBox.Text))
            {
                await _notificacionService.MostrarAsync("Validación", "El nombre comercial es obligatorio");
                return;
            }

            if (string.IsNullOrWhiteSpace(razonSocialTextBox.Text))
            {
                await _notificacionService.MostrarAsync("Validación", "La razón social es obligatoria");
                return;
            }

            try
            {
                var success = await ViewModel.UpdateEntidadAsync(
                    idEntidad: entidad.IdEntidad,
                    nombreComercial: nombreComercialTextBox.Text.Trim(),
                    razonSocial: razonSocialTextBox.Text.Trim(),
                    rfc: string.IsNullOrWhiteSpace(rfcTextBox.Text) ? null : rfcTextBox.Text.Trim(),
                    cp: string.IsNullOrWhiteSpace(cpTextBox.Text) ? null : cpTextBox.Text.Trim(),
                    estado: string.IsNullOrWhiteSpace(estadoTextBox.Text) ? null : estadoTextBox.Text.Trim(),
                    ciudad: string.IsNullOrWhiteSpace(ciudadTextBox.Text) ? null : ciudadTextBox.Text.Trim(),
                    pais: string.IsNullOrWhiteSpace(paisTextBox.Text) ? null : paisTextBox.Text.Trim(),
                    calle: string.IsNullOrWhiteSpace(calleTextBox.Text) ? null : calleTextBox.Text.Trim(),
                    numExt: string.IsNullOrWhiteSpace(numExtTextBox.Text) ? null : numExtTextBox.Text.Trim(),
                    numInt: string.IsNullOrWhiteSpace(numIntTextBox.Text) ? null : numIntTextBox.Text.Trim(),
                    colonia: string.IsNullOrWhiteSpace(coloniaTextBox.Text) ? null : coloniaTextBox.Text.Trim(),
                    apoderado: string.IsNullOrWhiteSpace(apoderadoTextBox.Text) ? null : apoderadoTextBox.Text.Trim(),
                    regimenFiscal: string.IsNullOrWhiteSpace(regimenFiscalTextBox.Text) ? null : regimenFiscalTextBox.Text.Trim()
                );

                if (success)
                {
                    await _notificacionService.MostrarAsync("Entidad actualizada", $"Entidad \"{nombreComercialTextBox.Text.Trim()}\" actualizada correctamente");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo actualizar la entidad. Verifique los datos e intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al editar entidad desde la UI", ex, "EntidadesPage", "EditarButton_Click");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al editar la entidad. Por favor, intente nuevamente.");
            }
        }

        private async void CertificadoButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFile? archivoCer = null;
            StorageFile? archivoKey = null;

            var estadoTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };

            async Task ActualizarEstadoAsync()
            {
                var estado = await _configuracionEmisorService.ObtenerEstadoCsdAsync();
                if (!estado.Cargado)
                {
                    estadoTextBlock.Text = "⚠️ No hay CSD cargado.";
                }
                else
                {
                    var vigenciaTexto = estado.FechaVigenciaHasta.HasValue ? estado.FechaVigenciaHasta.Value.ToString("dd/MM/yyyy") : "?";
                    estadoTextBlock.Text = estado.Vigente
                        ? $"✅ CSD vigente (No. {estado.NumeroCertificado}, vence {vigenciaTexto})"
                        : $"⚠️ CSD vencido (No. {estado.NumeroCertificado}, venció {vigenciaTexto})";
                }
            }

            var certTextBlock = new TextBlock { Text = "Sin archivo seleccionado", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) };
            var keyTextBlock = new TextBlock { Text = "Sin archivo seleccionado", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) };

            var btnCert = new Button { Content = "Elegir .cer" };
            btnCert.Click += async (_, _) =>
            {
                var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Downloads };
                picker.FileTypeFilter.Add(".cer");
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                var archivo = await picker.PickSingleFileAsync();
                if (archivo == null) return;
                archivoCer = archivo;
                certTextBlock.Text = $"✅ {archivo.Name}";
            };

            var btnKey = new Button { Content = "Elegir .key" };
            btnKey.Click += async (_, _) =>
            {
                var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.Downloads };
                picker.FileTypeFilter.Add(".key");
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                var archivo = await picker.PickSingleFileAsync();
                if (archivo == null) return;
                archivoKey = archivo;
                keyTextBlock.Text = $"✅ {archivo.Name}";
            };

            var passwordBox = new PasswordBox { PlaceholderText = "Contraseña del CSD" };

            var dialogContent = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    estadoTextBlock,
                    new TextBlock { Text = "Certificado (.cer):", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) },
                    btnCert,
                    certTextBlock,
                    new TextBlock { Text = "Llave privada (.key):", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) },
                    btnKey,
                    keyTextBlock,
                    new TextBlock { Text = "Contraseña:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) },
                    passwordBox
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Certificado de Sello Digital (CSD)",
                Content = dialogContent,
                PrimaryButtonText = "Cargar",
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            await ActualizarEstadoAsync();
            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            if (archivoCer == null || archivoKey == null)
            {
                await _notificacionService.MostrarAsync("Validación", "Selecciona el archivo .cer y el archivo .key antes de cargar.");
                return;
            }

            if (string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                await _notificacionService.MostrarAsync("Validación", "La contraseña del CSD es obligatoria.");
                return;
            }

            try
            {
                var certBytes = await ReadAllBytesAsync(archivoCer);
                var keyBytes = await ReadAllBytesAsync(archivoKey);

                var resultado = await _configuracionEmisorService.GuardarCsdAsync(certBytes, keyBytes, passwordBox.Password);

                if (resultado.Success)
                {
                    await _notificacionService.MostrarAsync("CSD cargado", "El certificado de sello digital se cargó correctamente.");
                }
                else
                {
                    await _notificacionService.MostrarAsync("Error", resultado.Message ?? "No se pudo cargar el CSD.");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar el CSD desde la UI", ex, "EntidadesPage", "CertificadoButton_Click");
                await _notificacionService.MostrarAsync("Error", "Ocurrió un error al cargar el CSD. Por favor, intente nuevamente.");
            }
        }

        private static async Task<byte[]> ReadAllBytesAsync(StorageFile file)
        {
            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Pivot "Configs CFDI": catálogos SAT c_RegimenFiscal y c_UsoCFDI (CFDI 4.0) editables
        /// desde aquí, en vez de TextBox libres en FacturarDirectoWindow -- ahí solo se eligen
        /// de este catálogo. El SAT los actualiza vía el Anexo 20, así que quedan editables.
        /// </summary>
        private void ConfigsCfdiPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var panel = (StackPanel)sender;
            if (panel.Children.Count > 0) return; // ya construido -- Loaded puede repetirse si el ItemsRepeater recicla el contenedor

            panel.Children.Add(ConstruirSeccionCatalogoCfdi(
                "Régimen Fiscal (c_RegimenFiscal)",
                claveMaxLength: 3,
                listar: () => _satCatalogoService.ListarRegimenFiscalAsync(incluirInactivos: true),
                guardar: item => _satCatalogoService.GuardarRegimenFiscalAsync(item),
                inactivar: (clave, estatus) => _satCatalogoService.InactivarRegimenFiscalAsync(clave, estatus)));

            panel.Children.Add(ConstruirSeccionCatalogoCfdi(
                "Uso de CFDI (c_UsoCFDI)",
                claveMaxLength: 4,
                listar: () => _satCatalogoService.ListarUsoCfdiAsync(incluirInactivos: true),
                guardar: item => _satCatalogoService.GuardarUsoCfdiAsync(item),
                inactivar: (clave, estatus) => _satCatalogoService.InactivarUsoCfdiAsync(clave, estatus)));
        }

        private UIElement ConstruirSeccionCatalogoCfdi(
            string titulo,
            int claveMaxLength,
            Func<Task<List<SatCatalogoItemDto>>> listar,
            Func<SatCatalogoItemDto, Task<SatCatalogoItemDto?>> guardar,
            Func<string, bool, Task<bool>> inactivar)
        {
            var listPanel = new StackPanel { Spacing = 6 };
            var progressRing = new ProgressRing { Width = 16, Height = 16, IsActive = true };

            async Task RecargarAsync()
            {
                progressRing.IsActive = true;
                listPanel.Children.Clear();
                try
                {
                    var items = await listar();
                    foreach (var item in items.OrderBy(i => i.Clave))
                    {
                        listPanel.Children.Add(ConstruirFilaCatalogoCfdi(item, titulo, claveMaxLength, guardar, inactivar, RecargarAsync));
                    }
                }
                finally
                {
                    progressRing.IsActive = false;
                }
            }

            var btnAgregar = new Button { Content = "+ Agregar clave" };
            btnAgregar.Click += async (_, _) =>
            {
                var nuevo = await MostrarDialogoClaveCfdiAsync(titulo, claveMaxLength, existente: null);
                if (nuevo == null) return;

                var guardado = await guardar(nuevo);
                if (guardado == null)
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo guardar la clave. Verifica el formato e intenta de nuevo.");
                    return;
                }
                await RecargarAsync();
            };

            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            header.Children.Add(new TextBlock { Text = titulo, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            header.Children.Add(progressRing);

            var container = new StackPanel { Spacing = 8 };
            container.Children.Add(header);
            container.Children.Add(btnAgregar);
            container.Children.Add(listPanel);

            _ = RecargarAsync();

            return new Border
            {
                Padding = new Thickness(12),
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Child = container
            };
        }

        private UIElement ConstruirFilaCatalogoCfdi(
            SatCatalogoItemDto item,
            string titulo,
            int claveMaxLength,
            Func<SatCatalogoItemDto, Task<SatCatalogoItemDto?>> guardar,
            Func<string, bool, Task<bool>> inactivar,
            Func<Task> recargar)
        {
            var opacidad = item.Estatus ? 1.0 : 0.5;

            var grid = new Grid { ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtClave = new TextBlock { Text = item.Clave, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, MinWidth = 40, Opacity = opacidad, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtClave, 0);

            var txtDescripcion = new TextBlock { Text = item.Descripcion, TextWrapping = TextWrapping.Wrap, Opacity = opacidad, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtDescripcion, 1);

            var aplicaTexto = (item.AplicaFisica, item.AplicaMoral) switch
            {
                (true, true) => "Física y Moral",
                (true, false) => "Física",
                (false, true) => "Moral",
                _ => "—"
            };
            var txtAplica = new TextBlock
            {
                Text = aplicaTexto,
                Opacity = opacidad,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            Grid.SetColumn(txtAplica, 2);

            var btnEditar = new Button { Content = "Editar" };
            btnEditar.Click += async (_, _) =>
            {
                var editado = await MostrarDialogoClaveCfdiAsync(titulo, claveMaxLength, existente: item);
                if (editado == null) return;

                var guardado = await guardar(editado);
                if (guardado == null)
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo guardar la clave. Verifica el formato e intenta de nuevo.");
                    return;
                }
                await recargar();
            };
            Grid.SetColumn(btnEditar, 3);

            var btnEstatus = new Button { Content = item.Estatus ? "Desactivar" : "Activar" };
            btnEstatus.Click += async (_, _) =>
            {
                var ok = await inactivar(item.Clave, !item.Estatus);
                if (!ok)
                {
                    await _notificacionService.MostrarAsync("Error", "No se pudo cambiar el estatus de la clave.");
                    return;
                }
                await recargar();
            };
            Grid.SetColumn(btnEstatus, 4);

            grid.Children.Add(txtClave);
            grid.Children.Add(txtDescripcion);
            grid.Children.Add(txtAplica);
            grid.Children.Add(btnEditar);
            grid.Children.Add(btnEstatus);

            return grid;
        }

        /// <summary>La clave es la llave primaria del catálogo -- no se edita una vez creada, solo se da de alta o se inactiva.</summary>
        private async Task<SatCatalogoItemDto?> MostrarDialogoClaveCfdiAsync(string titulo, int claveMaxLength, SatCatalogoItemDto? existente)
        {
            var claveTextBox = new TextBox
            {
                Text = existente?.Clave ?? string.Empty,
                PlaceholderText = $"Clave (máx. {claveMaxLength} caracteres)",
                MaxLength = claveMaxLength,
                IsEnabled = existente == null
            };
            var descripcionTextBox = new TextBox { Text = existente?.Descripcion ?? string.Empty, PlaceholderText = "Descripción" };
            var chkFisica = new CheckBox { Content = "Aplica a persona física", IsChecked = existente?.AplicaFisica ?? false };
            var chkMoral = new CheckBox { Content = "Aplica a persona moral", IsChecked = existente?.AplicaMoral ?? false };

            var dialog = new ContentDialog
            {
                Title = existente == null ? $"Agregar clave — {titulo}" : $"Editar clave — {titulo}",
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Verifica la clave y descripción contra el Anexo 20 vigente del SAT antes de guardar.",
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                        },
                        new TextBlock { Text = "Clave:" },
                        claveTextBox,
                        new TextBlock { Text = "Descripción:" },
                        descripcionTextBox,
                        chkFisica,
                        chkMoral
                    }
                },
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return null;

            if (string.IsNullOrWhiteSpace(claveTextBox.Text) || string.IsNullOrWhiteSpace(descripcionTextBox.Text))
            {
                await _notificacionService.MostrarAsync("Validación", "La clave y la descripción son obligatorias.");
                return null;
            }

            return new SatCatalogoItemDto
            {
                Clave = claveTextBox.Text.Trim(),
                Descripcion = descripcionTextBox.Text.Trim(),
                AplicaFisica = chkFisica.IsChecked == true,
                AplicaMoral = chkMoral.IsChecked == true,
                Estatus = true
            };
        }
    }
}

