using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Conciliacion;
using Advance_Control.Services.EstadoCuenta;
using Advance_Control.Services.Facturas;
using Advance_Control.Services.Notificacion;
using Advance_Control.Utilities;

namespace Advance_Control.ViewModels
{
    public class ConciliacionAutomaticaWindowViewModel : ViewModelBase
    {
        private readonly IEstadoCuentaXmlService _estadoCuentaXmlService;
        private readonly IFacturaService _facturaService;
        private readonly INotificacionService _notificacionService;
        private readonly ConciliacionMatchingEngine _conciliacionMatchingEngine;
        private readonly List<ConciliacionMovimientoResumenDto> _movimientosPendientesBase = new();
        private readonly List<FacturaResumenDto> _facturasPendientesBase = new();
        private readonly HashSet<int> _facturasDescartadasAbonos = new();
        private readonly Dictionary<int, HashSet<int>> _movimientosVetadosPorFacturaAbonos = new();
        private bool _bitacoraConciliacionInicializada;
        private bool _aplicarReglaPueMismoMes = true;
        private bool _usarRfcComoRegla = false;

        public ConciliacionAutomaticaWindowViewModel(
            IEstadoCuentaXmlService estadoCuentaXmlService,
            IFacturaService facturaService,
            INotificacionService notificacionService,
            ConciliacionMatchingEngine conciliacionMatchingEngine)
        {
            _estadoCuentaXmlService = estadoCuentaXmlService ?? throw new ArgumentNullException(nameof(estadoCuentaXmlService));
            _facturaService = facturaService ?? throw new ArgumentNullException(nameof(facturaService));
            _notificacionService = notificacionService ?? throw new ArgumentNullException(nameof(notificacionService));
            _conciliacionMatchingEngine = conciliacionMatchingEngine ?? throw new ArgumentNullException(nameof(conciliacionMatchingEngine));
        }

        public async Task<IReadOnlyList<ConciliacionMatchPropuestaDto>> CargarPropuestasAsync(
            ConciliacionAutomaticaModo modo,
            bool aplicarReglaPueMismoMes,
            bool usarRfcComoRegla)
        {
            _aplicarReglaPueMismoMes = aplicarReglaPueMismoMes;
            _usarRfcComoRegla = usarRfcComoRegla;
            if (modo == ConciliacionAutomaticaModo.Abonos)
            {
                _facturasDescartadasAbonos.Clear();
                _movimientosVetadosPorFacturaAbonos.Clear();
            }

            await CargarDatosBaseAsync();

            return modo switch
            {
                ConciliacionAutomaticaModo.Automatica => await CrearPropuestasAutomaticasAsync(),
                ConciliacionAutomaticaModo.Combinacional => await CrearPropuestasCombinacionalesAsync(),
                ConciliacionAutomaticaModo.Abonos => await CrearPropuestasAbonosAsync(),
                _ => Array.Empty<ConciliacionMatchPropuestaDto>()
            };
        }

        public async Task<IReadOnlyList<ConciliacionMatchPropuestaDto>> DescartarYRecalcularPropuestasAbonosAsync(
            ConciliacionMatchPropuestaDto propuestaDescartada)
        {
            if (!string.Equals(propuestaDescartada.Tipo, "Abonos", StringComparison.OrdinalIgnoreCase))
            {
                return await CrearPropuestasAbonosAsync(mostrarMensajeSinResultados: false);
            }

            var factura = propuestaDescartada.Facturas.FirstOrDefault();
            if (factura != null)
            {
                _facturasDescartadasAbonos.Add(factura.IdFactura);
                _movimientosVetadosPorFacturaAbonos.Remove(factura.IdFactura);
            }

            return await CrearPropuestasAbonosAsync(mostrarMensajeSinResultados: false);
        }

        public async Task<IReadOnlyList<ConciliacionMatchPropuestaDto>> DescartarMovimientoYRecalcularFacturaAbonosAsync(
            int idFactura,
            int idMovimiento)
        {
            if (!_movimientosVetadosPorFacturaAbonos.TryGetValue(idFactura, out var movimientosVetados))
            {
                movimientosVetados = new HashSet<int>();
                _movimientosVetadosPorFacturaAbonos[idFactura] = movimientosVetados;
            }

            movimientosVetados.Add(idMovimiento);
            _facturasDescartadasAbonos.Remove(idFactura);

            return await CrearPropuestasAbonosAsync(
                mostrarMensajeSinResultados: false,
                prioridadFacturaId: idFactura);
        }

        public async Task<bool> AplicarPropuestasAprobadasAsync(
            ConciliacionAutomaticaModo modo,
            IReadOnlyList<ConciliacionMatchPropuestaDto> aprobadas)
        {
            if (aprobadas.Count == 0)
            {
                await MostrarErrorConciliacionAsync("Selecciona al menos una propuesta para continuar.");
                return false;
            }

            foreach (var propuesta in aprobadas)
            {
                if (string.Equals(propuesta.Tipo, "Abonos", StringComparison.OrdinalIgnoreCase))
                {
                    await AplicarMovimientosSobreFacturaAsync(
                        propuesta.Facturas[0],
                        propuesta.TodosLosMovimientos);
                }
                else
                {
                    await ConciliarMovimientoConFacturasAsync(
                        propuesta.Movimiento,
                        propuesta.Facturas,
                        propuesta.Observaciones);
                }
            }

            await MostrarResultadoSegunModoAsync(modo, aprobadas);
            return true;
        }

        private async Task<IReadOnlyList<ConciliacionMatchPropuestaDto>> CrearPropuestasAutomaticasAsync()
        {
            var facturasObjetivo = _facturasPendientesBase
                .Where(factura => _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura) > 0)
                .Where(factura => !_usarRfcComoRegla || !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();
            var facturasUnoAUno = facturasObjetivo
                .Where(_conciliacionMatchingEngine.EsFacturaElegibleParaConciliacionUnoAUno)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            if (facturasObjetivo.Count == 0)
            {
                await MostrarErrorConciliacionAsync("No hay facturas con saldo pendiente para conciliacion automatica.");
                return Array.Empty<ConciliacionMatchPropuestaDto>();
            }

            var movimientosDisponibles = _movimientosPendientesBase
                .Where(movimiento => !_usarRfcComoRegla || !string.IsNullOrWhiteSpace(movimiento.RfcEmisor))
                .OrderBy(movimiento => movimiento.Fecha)
                .ThenBy(movimiento => movimiento.IdMovimiento)
                .ToList();
            var facturasRemanentes = new List<FacturaResumenDto>(facturasObjetivo);

            var propuestasUnoAUno = RecolectarPropuestasUnoAUno(facturasUnoAUno, movimientosDisponibles, facturasRemanentes);
            var propuestasCombinacional = RecolectarPropuestasCombinacional(facturasRemanentes, movimientosDisponibles);
            var propuestas = propuestasUnoAUno.Concat(propuestasCombinacional).ToList();

            if (_usarRfcComoRegla)
            {
                propuestas = propuestas
                    .Where(p => p.Movimiento != null
                        && p.Facturas.Any()
                        && string.Equals(
                            p.Movimiento.RfcEmisor?.Trim(),
                            p.Facturas.First().ReceptorRfc?.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (propuestas.Count == 0)
            {
                await MostrarErrorConciliacionAsync("No se encontro ningun movimiento compatible para las facturas pendientes.");
            }

            return propuestas;
        }

        private async Task<IReadOnlyList<ConciliacionMatchPropuestaDto>> CrearPropuestasCombinacionalesAsync()
        {
            var facturasObjetivo = _facturasPendientesBase
                .Where(factura => _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura) > 0)
                .Where(factura => !_usarRfcComoRegla || !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();

            if (!_conciliacionMatchingEngine.CanRunCombinacional(facturasObjetivo, _movimientosPendientesBase))
            {
                await MostrarErrorConciliacionAsync("No hay grupos de facturas pendientes para conciliacion automatica convinacional.");
                return Array.Empty<ConciliacionMatchPropuestaDto>();
            }

            var movimientosDisponibles = _movimientosPendientesBase
                .Where(movimiento => !_usarRfcComoRegla || !string.IsNullOrWhiteSpace(movimiento.RfcEmisor))
                .OrderBy(movimiento => movimiento.Fecha)
                .ThenBy(movimiento => movimiento.IdMovimiento)
                .ToList();
            var facturasRemanentes = new List<FacturaResumenDto>(facturasObjetivo);
            var propuestas = RecolectarPropuestasCombinacional(facturasRemanentes, movimientosDisponibles);

            if (_usarRfcComoRegla)
            {
                propuestas = propuestas
                    .Where(p => p.Movimiento != null
                        && p.Facturas.Any()
                        && p.Facturas.All(f => string.Equals(
                            p.Movimiento.RfcEmisor?.Trim(),
                            f.ReceptorRfc?.Trim(),
                            StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (propuestas.Count == 0)
            {
                await MostrarErrorConciliacionAsync("No se encontraron combinaciones compatibles para las facturas pendientes.");
            }

            return propuestas;
        }

        private async Task<IReadOnlyList<ConciliacionMatchPropuestaDto>> CrearPropuestasAbonosAsync(
            bool mostrarMensajeSinResultados = true,
            int? prioridadFacturaId = null)
        {
            var facturasObjetivo = _facturasPendientesBase
                .Where(factura =>
                    _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura) > 0
                    && !_facturasDescartadasAbonos.Contains(factura.IdFactura))
                .Where(factura => !_usarRfcComoRegla || !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                .ToList();

            facturasObjetivo = prioridadFacturaId.HasValue
                ? facturasObjetivo
                    .OrderBy(factura => factura.IdFactura == prioridadFacturaId.Value ? 0 : 1)
                    .ThenBy(factura => factura.Fecha)
                    .ThenBy(factura => factura.IdFactura)
                    .ToList()
                : facturasObjetivo
                    .OrderBy(factura => factura.Fecha)
                    .ThenBy(factura => factura.IdFactura)
                    .ToList();

            if (facturasObjetivo.Count == 0)
            {
                await MostrarErrorConciliacionAsync("No hay facturas con saldo pendiente para conciliacion automatica de abonos.");
                return Array.Empty<ConciliacionMatchPropuestaDto>();
            }

            var movimientosDisponibles = _movimientosPendientesBase
                .Where(movimiento => decimal.Round(movimiento.Abono, 2) > 0)
                .OrderBy(movimiento => movimiento.Fecha)
                .ThenBy(movimiento => movimiento.IdMovimiento)
                .ToList();

            var propuestas = RecolectarPropuestasAbonos(facturasObjetivo, movimientosDisponibles, _usarRfcComoRegla)
                .OrderBy(propuesta => propuesta.FacturaPrincipal?.Fecha ?? DateTime.MaxValue)
                .ThenBy(propuesta => propuesta.FacturaPrincipal?.IdFactura ?? int.MaxValue)
                .ToList();

            if (propuestas.Count == 0 && mostrarMensajeSinResultados)
            {
                await MostrarErrorConciliacionAsync("No se encontraron combinaciones de movimientos para las facturas pendientes.");
            }

            return propuestas;
        }

        private async Task CargarDatosBaseAsync()
        {
            await InicializarBitacoraConciliacionSiEsNecesarioAsync();

            var estadosTask = _estadoCuentaXmlService.ObtenerEstadosCuentaAsync();
            var facturasTask = _facturaService.ObtenerFacturasAsync();

            await Task.WhenAll(estadosTask, facturasTask);

            var estados = await estadosTask;
            var facturas = await facturasTask;

            var detalleTasks = estados
                .OrderBy(estado => estado.FechaCorte)
                .ThenBy(estado => estado.IdEstadoCuenta)
                .Select(CargarDetalleEstadoSeguroAsync)
                .ToList();

            var resultadosDetalle = detalleTasks.Count == 0
                ? Array.Empty<(EstadoCuentaDetalleDto? Detalle, string? Error)>()
                : await Task.WhenAll(detalleTasks);

            var detallesValidos = resultadosDetalle
                .Where(resultado => resultado.Detalle?.EstadoCuenta != null)
                .Select(resultado => resultado.Detalle!)
                .ToList();

            var movimientosPendientes = detallesValidos
                .SelectMany(detalle => detalle.Grupos
                    .Where(grupo => !grupo.Conciliado && grupo.Abono > 0)
                    .Select(grupo => new ConciliacionMovimientoResumenDto
                    {
                        IdEstadoCuenta = detalle.EstadoCuenta!.IdEstadoCuenta,
                        IdMovimiento = grupo.IdMovimiento,
                        NumeroCuenta = detalle.EstadoCuenta.NumeroCuenta,
                        TipoCuenta = detalle.EstadoCuenta.TipoCuenta,
                        Banco = detalle.EstadoCuenta.NombreBanco,
                        Titular = detalle.EstadoCuenta.Titular,
                        GrupoId = grupo.GrupoId,
                        Fecha = grupo.Fecha,
                        TipoOperacion = grupo.TipoOperacion,
                        SubtipoOperacion = grupo.SubtipoOperacion,
                        Descripcion = grupo.Descripcion,
                        Referencia = grupo.Referencia,
                        Cargo = grupo.Cargo,
                        Abono = grupo.Abono,
                        Saldo = grupo.Saldo,
                        RelacionadosCount = grupo.MovimientosRelacionados.Count,
                        RfcEmisor = grupo.RfcEmisor
                            ?? grupo.MovimientosRelacionados
                            .Select(r => r.Rfc)
                            .FirstOrDefault(rfc => !string.IsNullOrWhiteSpace(rfc)),
                        PeriodoTexto = detalle.EstadoCuenta.PeriodoTexto,
                        MetadatosTexto = grupo.MetadatosTexto
                    }))
                .OrderBy(movimiento => movimiento.Fecha)
                .ThenBy(movimiento => movimiento.IdMovimiento)
                .ToList();

            _movimientosPendientesBase.Clear();
            _movimientosPendientesBase.AddRange(movimientosPendientes);
            _facturasPendientesBase.Clear();
            _facturasPendientesBase.AddRange(FiltrarFacturasConciliables(facturas));
        }

        private async Task ConciliarMovimientoConFacturasAsync(
            ConciliacionMovimientoResumenDto movimiento,
            IReadOnlyList<FacturaResumenDto> facturas,
            string observaciones)
        {
            var montoAplicado = decimal.Round(facturas.Sum(_conciliacionMatchingEngine.ObtenerMontoPendienteFactura), 2);
            var request = new ConciliacionAutomaticaRequestDto
            {
                IdFactura = facturas.Count == 1 ? facturas[0].IdFactura : 0,
                Facturas = facturas
                    .Select(factura => new ConciliacionAutomaticaFacturaDto
                    {
                        IdFactura = factura.IdFactura,
                        MontoAbono = _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(factura)
                    })
                    .ToList(),
                IdMovimiento = movimiento.IdMovimiento,
                FechaAbono = movimiento.Fecha,
                MontoAbono = montoAplicado,
                Referencia = movimiento.Referencia,
                Observaciones = observaciones,
                RegistrarEnBitacoraConciliacion = true,
                TipoOperacionBitacoraConciliacion = "automatica"
            };

            var response = await _estadoCuentaXmlService.ConciliarAutomaticamenteAsync(request);
            if (!response.Success)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Message)
                    ? "No fue posible completar la conciliacion automatica."
                    : response.Message);
            }
        }

        private async Task AplicarMovimientosSobreFacturaAsync(
            FacturaResumenDto factura,
            IReadOnlyList<ConciliacionMovimientoResumenDto> movimientos)
        {
            foreach (var movimiento in movimientos
                .OrderBy(item => item.Fecha)
                .ThenBy(item => item.IdMovimiento))
            {
                var resultado = await _facturaService.RegistrarAbonoAsync(new RegistrarAbonoFacturaRequestDto
                {
                    IdFactura = factura.IdFactura,
                    IdMovimiento = movimiento.IdMovimiento,
                    FechaAbono = movimiento.Fecha,
                    MontoAbono = movimiento.Abono,
                    Referencia = movimiento.Referencia,
                    Observaciones = $"Abono generado desde conciliacion automatica de abonos con movimiento {movimiento.GrupoId}.",
                    RegistrarEnBitacoraConciliacion = true,
                    TipoOperacionBitacoraConciliacion = "automatica_abonos"
                });

                if (!resultado.Success)
                {
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible registrar uno de los abonos combinados."
                        : resultado.Message);
                }
            }
        }

        private List<ConciliacionMatchPropuestaDto> RecolectarPropuestasAbonos(
            List<FacturaResumenDto> facturasObjetivo,
            List<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            bool usarRfcComoRegla = false)
        {
            var propuestas = new List<ConciliacionMatchPropuestaDto>();

            foreach (var facturaObjetivo in facturasObjetivo)
            {
                var saldoFactura = _conciliacionMatchingEngine.ObtenerMontoPendienteFactura(facturaObjetivo);
                var candidatos = _conciliacionMatchingEngine.ObtenerMovimientosCandidatosParaFactura(
                    movimientosDisponibles,
                    facturaObjetivo,
                    saldoFactura,
                    aplicarReglaPueMismoMes: _aplicarReglaPueMismoMes,
                    limitarCandidatos: false)
                    .Where(movimiento => !EsMovimientoVetadoParaFactura(facturaObjetivo.IdFactura, movimiento.IdMovimiento))
                    .Where(movimiento => !usarRfcComoRegla
                        || string.Equals(
                            movimiento.RfcEmisor?.Trim(),
                            facturaObjetivo.ReceptorRfc?.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (candidatos.Count < 2)
                {
                    continue;
                }

                var combinacion = _conciliacionMatchingEngine.BuscarCombinacionMovimientosParaFactura(
                    candidatos,
                    saldoFactura,
                    facturaObjetivo.Fecha);

                if (combinacion == null || combinacion.Count == 0)
                {
                    continue;
                }

                var movimientoPrincipal = combinacion
                    .OrderBy(movimiento => Math.Abs((movimiento.Fecha - facturaObjetivo.Fecha).Ticks))
                    .ThenBy(movimiento => movimiento.Fecha)
                    .ThenBy(movimiento => movimiento.IdMovimiento)
                    .First();
                var adicionales = combinacion
                    .Where(movimiento => movimiento.IdMovimiento != movimientoPrincipal.IdMovimiento)
                    .ToList();

                propuestas.Add(new ConciliacionMatchPropuestaDto
                {
                    Tipo = "Abonos",
                    Facturas = new List<FacturaResumenDto> { facturaObjetivo },
                    Movimiento = movimientoPrincipal,
                    MovimientosAdicionales = adicionales,
                    Observaciones = $"Conciliacion automatica de abonos para factura {facturaObjetivo.Folio}."
                });

                foreach (var movimiento in combinacion)
                {
                    movimientosDisponibles.RemoveAll(item => item.IdMovimiento == movimiento.IdMovimiento);
                }
            }

            return propuestas;
        }

        private bool EsMovimientoVetadoParaFactura(int idFactura, int idMovimiento)
        {
            return _movimientosVetadosPorFacturaAbonos.TryGetValue(idFactura, out var movimientosVetados)
                && movimientosVetados.Contains(idMovimiento);
        }

        private List<ConciliacionMatchPropuestaDto> RecolectarPropuestasUnoAUno(
            List<FacturaResumenDto> facturasUnoAUno,
            List<ConciliacionMovimientoResumenDto> movimientosDisponibles,
            List<FacturaResumenDto> facturasRemanentes)
        {
            var propuestas = new List<ConciliacionMatchPropuestaDto>();

            foreach (var facturaObjetivo in facturasUnoAUno)
            {
                var totalFactura = _conciliacionMatchingEngine.ObtenerTotalFactura(facturaObjetivo);
                var movimientoObjetivo = _conciliacionMatchingEngine.BuscarMovimientoCoincidente(
                    movimientosDisponibles,
                    totalFactura,
                    new[] { facturaObjetivo },
                    facturaObjetivo.Fecha,
                    _aplicarReglaPueMismoMes);

                if (movimientoObjetivo == null)
                {
                    continue;
                }

                propuestas.Add(new ConciliacionMatchPropuestaDto
                {
                    Tipo = "1 a 1",
                    Facturas = new List<FacturaResumenDto> { facturaObjetivo },
                    Movimiento = movimientoObjetivo,
                    Observaciones = $"Conciliacion automatica 1 a 1 con movimiento {movimientoObjetivo.GrupoId}."
                });

                movimientosDisponibles.Remove(movimientoObjetivo);
                facturasRemanentes.RemoveAll(factura => factura.IdFactura == facturaObjetivo.IdFactura);
            }

            return propuestas;
        }

        private List<ConciliacionMatchPropuestaDto> RecolectarPropuestasCombinacional(
            List<FacturaResumenDto> facturasRemanentes,
            List<ConciliacionMovimientoResumenDto> movimientosDisponibles)
        {
            var propuestas = new List<ConciliacionMatchPropuestaDto>();

            var gruposPorRfc = facturasRemanentes
                .Where(factura => !string.IsNullOrWhiteSpace(factura.ReceptorRfc))
                .GroupBy(factura => factura.ReceptorRfc!.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderBy(grupo => grupo.Min(factura => factura.Fecha))
                .ThenBy(grupo => grupo.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var grupoRfc in gruposPorRfc)
            {
                var facturasRfc = facturasRemanentes
                    .Where(factura => string.Equals(factura.ReceptorRfc?.Trim(), grupoRfc.Key, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(factura => factura.Fecha)
                    .ThenBy(factura => factura.IdFactura)
                    .ToList();

                if (facturasRfc.Count < 2)
                {
                    continue;
                }

                while (facturasRfc.Count >= 2 && movimientosDisponibles.Count > 0)
                {
                    var maximoAbonoDisponible = movimientosDisponibles.Max(movimiento => movimiento.Abono);
                    if (maximoAbonoDisponible <= 0)
                    {
                        break;
                    }

                    var combinacion = _conciliacionMatchingEngine.BuscarCombinacionFacturasCompatible(
                        facturasRfc,
                        movimientosDisponibles,
                        maximoAbonoDisponible,
                        out var movimientoObjetivo,
                        _aplicarReglaPueMismoMes);

                    if (combinacion == null || movimientoObjetivo == null)
                    {
                        break;
                    }

                    propuestas.Add(new ConciliacionMatchPropuestaDto
                    {
                        Tipo = "Combinacional",
                        Facturas = combinacion,
                        Movimiento = movimientoObjetivo,
                        Observaciones = $"Conciliacion automatica combinacional con movimiento {movimientoObjetivo.GrupoId} para RFC {grupoRfc.Key}."
                    });

                    var idsConciliados = combinacion.Select(factura => factura.IdFactura).ToHashSet();
                    facturasRemanentes.RemoveAll(factura => idsConciliados.Contains(factura.IdFactura));
                    facturasRfc.RemoveAll(factura => idsConciliados.Contains(factura.IdFactura));
                    movimientosDisponibles.RemoveAll(movimiento => movimiento.IdMovimiento == movimientoObjetivo.IdMovimiento);
                }
            }

            return propuestas;
        }

        private async Task InicializarBitacoraConciliacionSiEsNecesarioAsync()
        {
            if (_bitacoraConciliacionInicializada)
            {
                return;
            }

            var resultado = await _facturaService.InicializarBitacoraConciliacionAsync();
            if (!resultado.Success)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(resultado.Message)
                    ? "No fue posible inicializar la bitacora de conciliacion."
                    : resultado.Message);
            }

            _bitacoraConciliacionInicializada = true;
        }

        private async Task MostrarResultadoSegunModoAsync(
            ConciliacionAutomaticaModo modo,
            IReadOnlyList<ConciliacionMatchPropuestaDto> aprobadas)
        {
            switch (modo)
            {
                case ConciliacionAutomaticaModo.Automatica:
                {
                    var conciliacionesUnoAUno = aprobadas.Count(propuesta => propuesta.Tipo == "1 a 1");
                    var conciliacionesCombinacionales = aprobadas
                        .Where(propuesta => propuesta.Tipo == "Combinacional")
                        .Sum(propuesta => propuesta.Facturas.Count);
                    var gruposCombinacionales = aprobadas.Count(propuesta => propuesta.Tipo == "Combinacional");
                    var segmentos = new List<string>();

                    if (conciliacionesUnoAUno > 0)
                    {
                        segmentos.Add($"{conciliacionesUnoAUno} factura(s) por relacion 1 a 1");
                    }

                    if (conciliacionesCombinacionales > 0)
                    {
                        segmentos.Add($"{conciliacionesCombinacionales} factura(s) en {gruposCombinacionales} grupo(s) combinacional(es)");
                    }

                    await MostrarResultadoFinalConciliacionAsync("Conciliacion automatica", segmentos);
                    break;
                }

                case ConciliacionAutomaticaModo.Combinacional:
                {
                    var facturasConciliadas = aprobadas.Sum(propuesta => propuesta.Facturas.Count);
                    var gruposConciliados = aprobadas.Count;
                    await MostrarResultadoFinalConciliacionAsync(
                        "Conciliacion automatica convinacional",
                        new[] { $"{facturasConciliadas} factura(s) en {gruposConciliados} grupo(s) combinacional(es)" });
                    break;
                }

                case ConciliacionAutomaticaModo.Abonos:
                {
                    var facturasConciliadas = aprobadas.Count;
                    var movimientosAplicados = aprobadas.Sum(propuesta => propuesta.TodosLosMovimientos.Count);
                    await MostrarResultadoFinalConciliacionAsync(
                        "Conciliacion automatica de abonos",
                        new[] { $"{facturasConciliadas} factura(s) conciliada(s) con {movimientosAplicados} movimiento(s)" });
                    break;
                }
            }
        }

        private async Task MostrarErrorConciliacionAsync(string mensaje)
        {
            await _notificacionService.MostrarAsync("Error de conciliacion", mensaje);
        }

        private async Task MostrarResultadoFinalConciliacionAsync(
            string proceso,
            IReadOnlyCollection<string> segmentosResumen)
        {
            var mensaje = $"{proceso} completada. {string.Join("; ", segmentosResumen)}.";
            await _notificacionService.MostrarAsync("Conciliacion", mensaje);
        }

        private async Task<(EstadoCuentaDetalleDto? Detalle, string? Error)> CargarDetalleEstadoSeguroAsync(EstadoCuentaResumenDto estado)
        {
            try
            {
                var detalle = await _estadoCuentaXmlService.ObtenerDetalleEstadoCuentaAsync(estado.IdEstadoCuenta);
                if (detalle?.EstadoCuenta == null)
                {
                    return (null, $"No se pudo cargar el detalle del estado {estado.CuentaTitulo}.");
                }

                return (detalle, null);
            }
            catch (Exception ex)
            {
                return (null, $"Se omitio el estado {estado.CuentaTitulo}: {ex.Message}");
            }
        }

        private List<FacturaResumenDto> FiltrarFacturasConciliables(IEnumerable<FacturaResumenDto> facturas)
        {
            return facturas
                .Where(factura => factura.Finiquito != true)
                .Where(factura => _conciliacionMatchingEngine.ObtenerTotalFactura(factura) > 0)
                .OrderBy(factura => factura.Fecha)
                .ThenBy(factura => factura.IdFactura)
                .ToList();
        }
    }
}
