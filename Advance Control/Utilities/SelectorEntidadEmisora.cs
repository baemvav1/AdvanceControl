using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.ConfiguracionEmisor;
using Advance_Control.Services.Entidades;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Utilities
{
    /// <summary>
    /// ComboBox de "Emitir como" de las ventanas de timbrado. Propone la entidad con la que ya se
    /// le facturó al cliente (la API la guarda al timbrar) o, si nunca se le ha facturado, la
    /// predeterminada; y revisa que la entidad elegida tenga CSD vigente y cuenta de Bilkon.
    /// </summary>
    public sealed class SelectorEntidadEmisora
    {
        private readonly ComboBox _combo;
        private readonly IEntidadService _entidadService;
        private readonly IConfiguracionEmisorService _configuracionEmisorService;
        private bool _seleccionandoPorCodigo;

        /// <summary>Se dispara cuando el usuario (o la sugerencia por cliente) cambia la entidad.</summary>
        public event Func<Task>? EntidadCambiada;

        public SelectorEntidadEmisora(ComboBox combo)
        {
            _combo = combo ?? throw new ArgumentNullException(nameof(combo));
            _entidadService = AppServices.Get<IEntidadService>();
            _configuracionEmisorService = AppServices.Get<IConfiguracionEmisorService>();
            _combo.DisplayMemberPath = nameof(EntidadDto.Descripcion);
            _combo.SelectionChanged += async (_, _) =>
            {
                if (!_seleccionandoPorCodigo && EntidadCambiada != null)
                    await EntidadCambiada.Invoke();
            };
        }

        public EntidadDto? Seleccionada => _combo.SelectedItem as EntidadDto;

        public int? IdEntidadSeleccionada => Seleccionada?.IdEntidad;

        /// <summary>Carga las entidades activas y selecciona la sugerida para el cliente.</summary>
        public async Task InicializarAsync(int? idCliente = null, int? idOperacion = null, int? idContrato = null, string? rfc = null)
        {
            List<EntidadDto> entidades;
            try
            {
                entidades = await _entidadService.GetEntidadesAsync(new EntidadQueryDto { Estatus = true });
            }
            catch
            {
                entidades = new List<EntidadDto>();
            }

            _seleccionandoPorCodigo = true;
            try
            {
                _combo.ItemsSource = entidades;
                // Con una sola razón social no hay nada que elegir.
                _combo.IsEnabled = entidades.Count > 1;
                _combo.SelectedItem = entidades.FirstOrDefault(e => e.EsPredeterminada) ?? entidades.FirstOrDefault();
            }
            finally
            {
                _seleccionandoPorCodigo = false;
            }

            await SugerirParaClienteAsync(idCliente, idOperacion, idContrato, rfc, notificar: false);
        }

        /// <summary>Selecciona la entidad con la que se le facturó antes al cliente (o la predeterminada).</summary>
        public async Task SugerirParaClienteAsync(int? idCliente = null, int? idOperacion = null, int? idContrato = null, string? rfc = null, bool notificar = true)
        {
            if (idCliente == null && idOperacion == null && idContrato == null && string.IsNullOrWhiteSpace(rfc))
                return;

            var sugerida = await _entidadService.GetEntidadFacturacionClienteAsync(idCliente, idOperacion, idContrato, rfc);
            var entidad = (_combo.ItemsSource as IEnumerable<EntidadDto>)?.FirstOrDefault(e => e.IdEntidad == sugerida?.IdEntidad);
            if (entidad == null || ReferenceEquals(entidad, _combo.SelectedItem))
                return;

            _seleccionandoPorCodigo = true;
            try
            {
                _combo.SelectedItem = entidad;
            }
            finally
            {
                _seleccionandoPorCodigo = false;
            }

            if (notificar && EntidadCambiada != null)
                await EntidadCambiada.Invoke();
        }

        /// <summary>
        /// Datos fiscales de la entidad elegida y si se puede timbrar con ella (CSD vigente y cuenta
        /// de Bilkon). Aviso = null cuando todo está listo.
        /// </summary>
        public async Task<EvaluacionEmisor> EvaluarAsync()
        {
            var entidad = Seleccionada;
            if (entidad == null)
                return new EvaluacionEmisor(null, false, InfoBarSeverity.Error, "No hay entidades dadas de alta. Registra la razón social en Entidades antes de timbrar.");

            var configuracion = await _configuracionEmisorService.ObtenerConfiguracionAsync(entidad.IdEntidad);
            if (configuracion == null)
                return new EvaluacionEmisor(null, false, InfoBarSeverity.Error, $"No se pudieron leer los datos fiscales de {entidad.Descripcion}.");

            var estadoCsd = await _configuracionEmisorService.ObtenerEstadoCsdAsync(entidad.IdEntidad);
            if (!estadoCsd.Vigente)
            {
                return new EvaluacionEmisor(configuracion, false, InfoBarSeverity.Warning, estadoCsd.Cargado
                    ? $"El CSD de {entidad.Descripcion} ya no está vigente. No podrás timbrar hasta actualizarlo en Entidades."
                    : $"{entidad.Descripcion} no tiene CSD cargado. No podrás timbrar hasta cargarlo en Entidades.");
            }

            if (!entidad.FelLista)
            {
                return new EvaluacionEmisor(configuracion, false, InfoBarSeverity.Warning,
                    $"{entidad.Descripcion} no tiene cuenta de timbrado Bilkon. Captúrala en Entidades antes de timbrar.");
            }

            return new EvaluacionEmisor(configuracion, true, InfoBarSeverity.Success, null);
        }
    }

    public sealed record EvaluacionEmisor(ConfiguracionEmisorDto? Configuracion, bool Listo, InfoBarSeverity Severidad, string? Aviso);
}
