using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Suscripciones;
using Advance_Control.Services.Contratos;
using Advance_Control.Services.LocalStorage;
using Advance_Control.Services.Logging;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;
using Advance_Control.ViewModels;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>
    /// Formulario para generar un contrato de suscripción: llena los campos variables
    /// del machote, guarda el contrato en la base de datos, genera el PDF y lo sube al VPS.
    /// </summary>
    public sealed partial class GenerarContratoUserControl : UserControl
    {
        public GenerarContratoViewModel ViewModel { get; }
        public Action? CloseDialogAction { get; set; }
        public bool GeneradoExitosamente { get; private set; }

        private readonly int _idCliente;
        private readonly string _clienteRazonSocial;
        private readonly IEquipoService _equipoService;
        private readonly Services.Suscripciones.IContratoSuscripcionService _contratoService;
        private readonly IContratoPdfService _pdfService;
        private readonly IContratoDocumentoService _documentoService;
        private readonly ILoggingService _logger;
        private readonly INotificacionService _notificacionService;

        private bool _isLoadingEquipos;
        public bool IsLoadingEquipos
        {
            get => _isLoadingEquipos;
            set { _isLoadingEquipos = value; Bindings.Update(); }
        }

        private bool _isGuardando;
        public bool IsGuardando
        {
            get => _isGuardando;
            set { _isGuardando = value; Bindings.Update(); }
        }

        public ObservableCollection<EquipoSeleccionableDto> EquiposVista { get; } = new();

        private ObservableCollection<EquipoSeleccionableDto> _todosLosEquipos = new();

        public GenerarContratoUserControl(string nivel, int idCliente, string clienteRazonSocial, string? direccionSugerida)
        {
            ViewModel = new GenerarContratoViewModel(nivel);
            _idCliente = idCliente;
            _clienteRazonSocial = clienteRazonSocial;

            if (!string.IsNullOrWhiteSpace(direccionSugerida))
                ViewModel.DireccionInstalacion = direccionSugerida;

            _equipoService = AppServices.Get<IEquipoService>();
            _contratoService = AppServices.Get<Services.Suscripciones.IContratoSuscripcionService>();
            _pdfService = AppServices.Get<IContratoPdfService>();
            _documentoService = AppServices.Get<IContratoDocumentoService>();
            _logger = AppServices.Get<ILoggingService>();
            _notificacionService = AppServices.Get<INotificacionService>();

            this.InitializeComponent();
            this.DataContext = ViewModel;

            this.Loaded += GenerarContratoUserControl_Loaded;
        }

        private async void GenerarContratoUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= GenerarContratoUserControl_Loaded;
            await CargarEquiposAsync();
        }

        private async Task CargarEquiposAsync()
        {
            IsLoadingEquipos = true;
            try
            {
                var equipos = await _equipoService.GetEquiposAsync();
                _todosLosEquipos = new ObservableCollection<EquipoSeleccionableDto>(
                    equipos.OrderBy(eq => eq.Identificador).Select(eq => new EquipoSeleccionableDto(eq)));

                AplicarFiltro();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar equipos para el contrato", ex, nameof(GenerarContratoUserControl), nameof(CargarEquiposAsync));
                ViewModel.ErrorMessage = "No se pudieron cargar los equipos disponibles.";
            }
            finally
            {
                IsLoadingEquipos = false;
            }
        }

        private void AplicarFiltro()
        {
            EquiposVista.Clear();

            var filtro = ViewModel.FiltroEquipos?.Trim();
            var origen = string.IsNullOrWhiteSpace(filtro)
                ? _todosLosEquipos
                : new ObservableCollection<EquipoSeleccionableDto>(_todosLosEquipos.Where(e =>
                    (e.Equipo.Identificador?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.Equipo.Marca?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false)));

            foreach (var item in origen)
                EquiposVista.Add(item);
        }

        private void FiltroEquiposTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            AplicarFiltro();
        }

        private void EquipoCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ViewModel.RecalcularNumeroUnidades();
        }

        private async void FinalizarButton_Click(object sender, RoutedEventArgs e)
        {
            var equiposSeleccionados = _todosLosEquipos.Where(eq => eq.IsSelected).Select(eq => eq.Equipo).ToList();

            if (equiposSeleccionados.Count == 0)
            {
                ViewModel.ErrorMessage = "Debe seleccionar al menos un equipo.";
                return;
            }

            if (!ViewModel.MontoMensual.HasValue)
            {
                ViewModel.ErrorMessage = "El monto mensual es requerido y debe ser mayor a cero.";
                return;
            }

            IsGuardando = true;
            ViewModel.ErrorMessage = string.Empty;

            try
            {
                var idsEquipos = equiposSeleccionados.Select(eq => eq.IdEquipo).ToArray();

                var contrato = await _contratoService.CreateContratoAsync(
                    idCliente: _idCliente,
                    nivel: ViewModel.Nivel,
                    numeroContrato: string.IsNullOrWhiteSpace(ViewModel.NumeroContrato) ? null : ViewModel.NumeroContrato,
                    direccionInstalacion: string.IsNullOrWhiteSpace(ViewModel.DireccionInstalacion) ? null : ViewModel.DireccionInstalacion,
                    montoMensual: ViewModel.MontoMensual.Value,
                    numeroUnidades: ViewModel.NumeroUnidades,
                    vigenciaInicio: ViewModel.VigenciaInicio.DateTime,
                    vigenciaFin: ViewModel.VigenciaFin.DateTime,
                    nombreFirmante: string.IsNullOrWhiteSpace(ViewModel.NombreFirmante) ? null : ViewModel.NombreFirmante,
                    telefonoFirmante: string.IsNullOrWhiteSpace(ViewModel.TelefonoFirmante) ? null : ViewModel.TelefonoFirmante,
                    fechaFirma: ViewModel.FechaFirma.DateTime,
                    pdfGeneradoUrl: null,
                    idsEquipos: idsEquipos);

                if (contrato == null)
                {
                    ViewModel.ErrorMessage = "No se pudo guardar el contrato. Intente nuevamente.";
                    return;
                }

                var pdfPath = await _pdfService.GenerarContratoPdfAsync(contrato, _clienteRazonSocial, equiposSeleccionados);

                await using (var stream = File.OpenRead(pdfPath))
                {
                    var url = await _documentoService.SubirGeneradoAsync(contrato.Id, stream, "application/pdf");
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        await _contratoService.ActualizarPdfGeneradoAsync(contrato.Id, url);
                    }
                    else
                    {
                        await _logger.LogWarningAsync(
                            $"El contrato {contrato.Id} se creó pero el PDF no se pudo subir al VPS; queda solo local en {pdfPath}",
                            nameof(GenerarContratoUserControl), nameof(FinalizarButton_Click));
                        await _notificacionService.MostrarAsync("Advertencia", "El contrato se guardó pero el PDF no se pudo subir al servidor. Se guardó una copia local.");
                    }
                }

                GeneradoExitosamente = true;
                CloseDialogAction?.Invoke();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al generar el contrato de suscripción", ex, nameof(GenerarContratoUserControl), nameof(FinalizarButton_Click));
                ViewModel.ErrorMessage = $"Error al generar el contrato: {ex.Message}";
            }
            finally
            {
                IsGuardando = false;
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            GeneradoExitosamente = false;
            CloseDialogAction?.Invoke();
        }
    }
}
