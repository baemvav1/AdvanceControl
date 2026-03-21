using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Advance_Control.Models;
using Advance_Control.Services.EstadoCuenta;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Advance_Control.ViewModels
{
    public class EsCuentaViewModel : ViewModelBase
    {
        private static readonly Regex CurrencyRegex = new(@"(?<signoInicial>-)?\$\s*(?<valor>[0-9]{1,3}(?:,[0-9]{3})*(?:\.\d{2})|[0-9]+\.\d{2})(?<signoFinal>\s*-)?", RegexOptions.Compiled);
        private readonly IEstadoCuentaXmlService _estadoCuentaXmlService;
        private readonly List<EstadoCuentaResumenDto> _estadosCuentaExistentesBase = new();
        private EstadoCuentaBancario? _estadoCuentaBancario;
        private ObservableCollection<MovimientoBancario> _movimientos;
        private ObservableCollection<EstadoCuentaResumenDto> _estadosCuentaExistentes;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;
        private DateTimeOffset? _fechaFiltroDesde;
        private DateTimeOffset? _fechaFiltroHasta;
        private string? _abonoFiltro;
        private string? _retiroFiltro;
        private string? _referenciaFiltro;

        public EsCuentaViewModel(IEstadoCuentaXmlService estadoCuentaXmlService)
        {
            _estadoCuentaXmlService = estadoCuentaXmlService ?? throw new ArgumentNullException(nameof(estadoCuentaXmlService));
            _movimientos = new ObservableCollection<MovimientoBancario>();
            _estadosCuentaExistentes = new ObservableCollection<EstadoCuentaResumenDto>();
        }

        public EstadoCuentaBancario? EstadoCuentaBancario
        {
            get => _estadoCuentaBancario;
            set
            {
                if (SetProperty(ref _estadoCuentaBancario, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public ObservableCollection<MovimientoBancario> Movimientos
        {
            get => _movimientos;
            set => SetProperty(ref _movimientos, value);
        }

        public ObservableCollection<EstadoCuentaResumenDto> EstadosCuentaExistentes
        {
            get => _estadosCuentaExistentes;
            set => SetProperty(ref _estadosCuentaExistentes, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string? SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        public DateTimeOffset? FechaFiltroDesde
        {
            get => _fechaFiltroDesde;
            set
            {
                if (SetProperty(ref _fechaFiltroDesde, value))
                {
                    AplicarFiltrosEstadosExistentes();
                }
            }
        }

        public DateTimeOffset? FechaFiltroHasta
        {
            get => _fechaFiltroHasta;
            set
            {
                if (SetProperty(ref _fechaFiltroHasta, value))
                {
                    AplicarFiltrosEstadosExistentes();
                }
            }
        }

        public string? AbonoFiltro
        {
            get => _abonoFiltro;
            set
            {
                if (SetProperty(ref _abonoFiltro, value))
                {
                    AplicarFiltrosEstadosExistentes();
                }
            }
        }

        public string? RetiroFiltro
        {
            get => _retiroFiltro;
            set
            {
                if (SetProperty(ref _retiroFiltro, value))
                {
                    AplicarFiltrosEstadosExistentes();
                }
            }
        }

        public string? ReferenciaFiltro
        {
            get => _referenciaFiltro;
            set
            {
                if (SetProperty(ref _referenciaFiltro, value))
                {
                    AplicarFiltrosEstadosExistentes();
                }
            }
        }

        public bool CanSave => EstadoCuentaBancario != null && !IsLoading;

        public string ResumenEstadosExistentes => $"{EstadosCuentaExistentes.Count} estados mostrados";

        public async Task CargarArchivoXmlAsync(nint windowHandle)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".xml");

                WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var xmlContent = await FileIO.ReadTextAsync(file);
                    ParsearEstadoCuentaXml(xmlContent);
                    var request = CrearRequestDesdeEstadoActual();
                    var result = await ValidarYGuardarEstadoActualAsync(request);

                    if (!result.Success && !string.Equals(result.Accion, "existente", StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                            ? $"No se pudo guardar el estado de cuenta {file.Name}."
                            : $"{file.Name}: {result.Message}";
                        SuccessMessage = null;
                        return;
                    }

                    if (EstadoCuentaBancario != null)
                    {
                        EstadoCuentaBancario.IdEstadoCuentaBancario = result.IdEstadoCuenta;
                    }

                    await CargarEstadosCuentaExistentesAsync();
                    SuccessMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? $"Archivo {file.Name} cargado y guardado exitosamente."
                        : $"{file.Name}: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el archivo XML: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task GuardarEstadoCuentaAsync()
        {
            if (EstadoCuentaBancario == null)
            {
                ErrorMessage = "Primero debes cargar un archivo XML.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                var result = await ValidarYGuardarEstadoActualAsync(CrearRequestDesdeEstadoActual());
                if (!result.Success && !string.Equals(result.Accion, "existente", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? "No se pudo guardar el estado de cuenta."
                        : result.Message;
                    return;
                }

                EstadoCuentaBancario.IdEstadoCuentaBancario = result.IdEstadoCuenta;
                await CargarEstadosCuentaExistentesAsync();
                SuccessMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Estado de cuenta guardado correctamente."
                    : $"{result.Message} Movimientos procesados: {result.MovimientosProcesados}.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al guardar el estado de cuenta: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CargarEstadosCuentaExistentesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var estados = await _estadoCuentaXmlService.ObtenerEstadosCuentaAsync();
                _estadosCuentaExistentesBase.Clear();
                _estadosCuentaExistentesBase.AddRange(estados.OrderByDescending(e => e.FechaCorte).ThenByDescending(e => e.IdEstadoCuenta));
                AplicarFiltrosEstadosExistentes();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al consultar los estados de cuenta existentes: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LimpiarFiltrosEstados()
        {
            FechaFiltroDesde = null;
            FechaFiltroHasta = null;
            AbonoFiltro = null;
            RetiroFiltro = null;
            ReferenciaFiltro = null;
            AplicarFiltrosEstadosExistentes();
        }

        private void ParsearEstadoCuentaXml(string xmlContent)
        {
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var raiz = doc.Root ?? throw new InvalidDataException("El XML no contiene un nodo raiz valido.");
                var ns = ObtenerNamespaceEstadoCuenta(raiz);
                var estadoCuenta = new EstadoCuentaBancario
                {
                    VersionXml = raiz.Attribute("version")?.Value ?? "2.0"
                };

                var infoGeneral = raiz.Element(ns + "informacionGeneral");
                if (infoGeneral != null)
                {
                    var banco = infoGeneral.Element(ns + "banco");
                    if (banco != null)
                    {
                        estadoCuenta.NombreBanco = banco.Element(ns + "nombre")?.Value ?? string.Empty;
                        estadoCuenta.RfcBanco = banco.Element(ns + "rfc")?.Value ?? string.Empty;
                        estadoCuenta.NombreSucursal = banco.Element(ns + "sucursal")?.Value ?? string.Empty;
                        estadoCuenta.DireccionSucursal = banco.Element(ns + "direccion")?.Value ?? string.Empty;
                    }

                    var titular = infoGeneral.Element(ns + "titular");
                    if (titular != null)
                    {
                        estadoCuenta.Titular = titular.Element(ns + "razonSocial")?.Value ?? string.Empty;
                        estadoCuenta.RfcTitular = titular.Element(ns + "rfc")?.Value ?? string.Empty;
                        estadoCuenta.NumeroCliente = titular.Element(ns + "numeroCliente")?.Value ?? string.Empty;
                        estadoCuenta.DireccionTitular = titular.Element(ns + "direccion")?.Value ?? string.Empty;
                    }

                    var cuenta = infoGeneral.Element(ns + "cuenta");
                    if (cuenta != null)
                    {
                        estadoCuenta.NumeroCuenta = cuenta.Element(ns + "numero")?.Value ?? string.Empty;
                        estadoCuenta.Clabe = cuenta.Element(ns + "clabe")?.Value ?? string.Empty;
                        estadoCuenta.TipoCuenta = cuenta.Element(ns + "tipo")?.Value ?? string.Empty;
                        estadoCuenta.TipoMoneda = cuenta.Element(ns + "moneda")?.Value ?? string.Empty;
                    }

                    var periodo = infoGeneral.Element(ns + "periodo");
                    if (periodo != null)
                    {
                        var fechaInicioTexto = periodo.Element(ns + "fechaInicio")?.Value;
                        var fechaFinTexto = periodo.Element(ns + "fechaFin")?.Value;
                        var fechaCorteTexto = periodo.Element(ns + "fechaCorte")?.Value;

                        estadoCuenta.Periodo = $"{fechaInicioTexto} - {fechaFinTexto}";
                        estadoCuenta.PeriodoInicio = ParseFechaEstadoCuenta(fechaInicioTexto) ?? DateTime.Today;
                        estadoCuenta.PeriodoFin = ParseFechaEstadoCuenta(fechaFinTexto) ?? estadoCuenta.PeriodoInicio;
                        estadoCuenta.FechaCorte = ParseFechaEstadoCuenta(fechaCorteTexto) ?? estadoCuenta.PeriodoFin;
                    }

                    var resumen = infoGeneral.Element(ns + "resumen");
                    if (resumen != null)
                    {
                        estadoCuenta.SaldoInicial = ObtenerValorDecimal(ns, resumen, "saldoInicial");
                        estadoCuenta.TotalAbonos = ObtenerValorDecimal(ns, resumen, "totalDepositos");
                        estadoCuenta.TotalCargos = ObtenerValorDecimal(ns, resumen, "totalRetiros");
                        estadoCuenta.TotalComisiones = ObtenerValorDecimal(ns, resumen, "totalComisiones");
                        estadoCuenta.SaldoFinal = ObtenerValorDecimal(ns, resumen, "saldoFinal");
                        estadoCuenta.SaldoPromedio = ObtenerValorDecimal(ns, resumen, "saldoPromedio");
                    }
                }

                var cfdiInfo = raiz.Element(ns + "cfdiInformacion");
                if (cfdiInfo != null)
                {
                    estadoCuenta.FolioFiscal = cfdiInfo.Element(ns + "folioFiscal")?.Value ?? string.Empty;

                    var certificados = cfdiInfo.Element(ns + "certificados");
                    if (certificados != null)
                    {
                        var certEmisor = certificados.Element(ns + "certificadoEmisor");
                        if (certEmisor != null)
                        {
                            estadoCuenta.CertificadoEmisor = certEmisor.Element(ns + "numero")?.Value ?? string.Empty;
                            estadoCuenta.FechaEmisionCert = certEmisor.Element(ns + "fechaEmision")?.Value ?? string.Empty;
                        }

                        var certSat = certificados.Element(ns + "certificadoSAT");
                        if (certSat != null)
                        {
                            estadoCuenta.CertificadoSAT = certSat.Element(ns + "numero")?.Value ?? string.Empty;
                            estadoCuenta.FechaCertificacionSAT = certSat.Element(ns + "fechaCertificacion")?.Value ?? string.Empty;
                        }
                    }

                    var datosFiscales = cfdiInfo.Element(ns + "datosFiscales");
                    if (datosFiscales != null)
                    {
                        estadoCuenta.RegimenFiscal = datosFiscales.Element(ns + "regimenFiscal")?.Value ?? string.Empty;
                        estadoCuenta.MetodoPago = datosFiscales.Element(ns + "metodoPago")?.Value ?? string.Empty;
                        estadoCuenta.FormaPago = datosFiscales.Element(ns + "formaPago")?.Value ?? string.Empty;
                        estadoCuenta.UsoCFDI = datosFiscales.Element(ns + "usoCFDI")?.Value ?? string.Empty;
                        estadoCuenta.ClaveProdServ = datosFiscales.Element(ns + "claveProdServ")?.Value ?? string.Empty;
                        estadoCuenta.LugarExpedicion = datosFiscales.Element(ns + "lugarExpedicion")?.Value ?? string.Empty;
                    }
                }

                var transacciones = raiz.Element(ns + "transacciones");
                if (transacciones != null)
                {
                    estadoCuenta.TotalTransacciones = ObtenerValorEntero(transacciones.Attribute("totalIndividuales"));
                    if (estadoCuenta.TotalTransacciones == 0)
                    {
                        estadoCuenta.TotalTransacciones = ObtenerValorEntero(transacciones.Attribute("total"));
                    }

                    estadoCuenta.TotalGrupos = ObtenerValorEntero(transacciones.Attribute("totalGrupos"));
                    if (estadoCuenta.TotalGrupos == 0)
                    {
                        estadoCuenta.TotalGrupos = ObtenerValorEntero(transacciones.Attribute("totalAgrupadas"));
                    }

                    var saldoAnterior = estadoCuenta.SaldoInicial ?? 0m;
                    var orden = 0;
                    foreach (var grupo in transacciones.Elements(ns + "grupo"))
                    {
                        orden++;
                        var diaTexto = grupo.Attribute("dia")?.Value;
                        var fecha = ConstruirFechaMovimiento(diaTexto, estadoCuenta.FechaCorte);
                        var principal = grupo.Element(ns + "transaccionPrincipal");
                        if (principal == null)
                        {
                            continue;
                        }

                        var movimiento = new MovimientoBancario
                        {
                            GrupoId = grupo.Attribute("id")?.Value ?? $"g-{orden}",
                            Dia = int.TryParse(diaTexto, out var dia) ? dia : null,
                            FechaMovimiento = fecha,
                            TipoGrupo = grupo.Attribute("tipo")?.Value,
                            TipoMovimiento = principal.Element(ns + "tipo")?.Value ?? grupo.Attribute("tipo")?.Value ?? "MOVIMIENTO",
                            SubtipoMovimiento = principal.Element(ns + "subtipo")?.Value,
                            Descripcion = principal.Element(ns + "descripcion")?.Value ?? string.Empty,
                            Referencia = principal.Element(ns + "referencia")?.Value ?? grupo.Attribute("id")?.Value,
                            Metadatos = ExtraerMetadatos(principal.Element(ns + "metadatos"))
                        };

                        CompletarMontos(movimiento, principal.Element(ns + "montos"), saldoAnterior);
                        saldoAnterior = movimiento.SaldoResultante;

                        var relacionados = grupo.Element(ns + "movimientosRelacionados");
                        if (relacionados != null)
                        {
                            foreach (var relacionado in relacionados.Elements(ns + "movimiento"))
                            {
                                movimiento.MovimientosRelacionados.Add(new MovimientoRelacionadoBancario
                                {
                                    Tipo = relacionado.Attribute("tipo")?.Value ?? string.Empty,
                                    Orden = int.TryParse(relacionado.Attribute("orden")?.Value, out var ordenRelacionado) ? ordenRelacionado : null,
                                    Descripcion = relacionado.Element(ns + "descripcion")?.Value ?? string.Empty,
                                    Rfc = relacionado.Element(ns + "rfc")?.Value,
                                    Monto = ObtenerValorDecimalOpcional(ns, relacionado, "montoIva")
                                        ?? ObtenerValorDecimalOpcional(ns, relacionado, "monto_iva")
                                        ?? ObtenerValorDecimalOpcional(ns, relacionado, "monto"),
                                    Saldo = ObtenerValorDecimalOpcional(ns, relacionado, "saldo")
                                });
                            }
                        }

                        estadoCuenta.Movimientos.Add(movimiento);
                    }
                }

                var resumenComisiones = raiz.Element(ns + "resumenComisiones");
                if (resumenComisiones != null)
                {
                    estadoCuenta.TotalIVA = ObtenerValorDecimal(ns, resumenComisiones, "iva");
                    var total = ObtenerValorDecimalOpcional(ns, resumenComisiones, "total");
                    if (total.HasValue && total.Value > 0)
                    {
                        estadoCuenta.TotalComisiones = total.Value;
                    }
                }

                EstadoCuentaBancario = estadoCuenta;
                Movimientos.Clear();
                foreach (var movimiento in estadoCuenta.Movimientos.OrderBy(m => m.FechaMovimiento).ThenBy(m => m.GrupoId))
                {
                    Movimientos.Add(movimiento);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al parsear el XML: {ex.Message}", ex);
            }
        }

        private static DateTime? ParseFechaEstadoCuenta(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            var texto = valor.Trim().ToUpperInvariant();
            var formats = new[] { "dd MMM yyyy", "d MMM yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" };
            var cultures = new[] { new CultureInfo("es-MX"), CultureInfo.InvariantCulture };

            foreach (var culture in cultures)
            {
                if (DateTime.TryParseExact(texto, formats, culture, DateTimeStyles.AllowWhiteSpaces, out var fecha))
                {
                    return fecha;
                }

                if (DateTime.TryParse(texto, culture, DateTimeStyles.AllowWhiteSpaces, out fecha))
                {
                    return fecha;
                }
            }

            return null;
        }

        internal static XNamespace ObtenerNamespaceEstadoCuenta(XElement raiz)
        {
            var namespaceName = raiz.Name.Namespace;
            return namespaceName != XNamespace.None
                ? namespaceName
                : raiz.GetDefaultNamespace();
        }

        private static DateTime ConstruirFechaMovimiento(string? diaTexto, DateTime fechaReferencia)
        {
            if (int.TryParse(diaTexto?.Trim(), out var dia) && fechaReferencia != default)
            {
                var ultimoDiaMes = DateTime.DaysInMonth(fechaReferencia.Year, fechaReferencia.Month);
                if (dia >= 1 && dia <= ultimoDiaMes)
                {
                    return new DateTime(fechaReferencia.Year, fechaReferencia.Month, dia);
                }
            }

            return fechaReferencia == default ? DateTime.Today : fechaReferencia;
        }

        private decimal ObtenerValorDecimal(XNamespace ns, XElement elementoPadre, string nombreElemento)
        {
            return ObtenerValorDecimalOpcional(ns, elementoPadre, nombreElemento) ?? 0m;
        }

        private decimal? ObtenerValorDecimalOpcional(XNamespace ns, XElement? elementoPadre, string nombreElemento)
        {
            var valor = elementoPadre?.Element(ns + nombreElemento)?.Value;
            return ParseDecimal(valor);
        }

        private static decimal? ParseDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (decimal.TryParse(valor, NumberStyles.Any, new CultureInfo("es-MX"), out var resultado))
            {
                return resultado;
            }

            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado))
            {
                return resultado;
            }

            return null;
        }

        private static int ObtenerValorEntero(XAttribute? atributo)
        {
            if (atributo == null)
            {
                return 0;
            }

            return int.TryParse(atributo.Value, out var resultado) ? resultado : 0;
        }

        private static Dictionary<string, string?> ExtraerMetadatos(XElement? metadatos)
        {
            var resultado = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (metadatos == null)
            {
                return resultado;
            }

            foreach (var seccion in metadatos.Elements())
            {
                foreach (var nodo in seccion.Elements())
                {
                    resultado[nodo.Name.LocalName] = nodo.Value;
                }
            }

            return resultado;
        }

        private static void CompletarMontos(MovimientoBancario movimiento, XElement? montos, decimal saldoAnterior)
        {
            var montosExplicitos = LeerMontosExplicitos(montos);
            if (montosExplicitos.HasValue)
            {
                movimiento.MontoAbono = montosExplicitos.Value.Abono;
                movimiento.MontoCargo = montosExplicitos.Value.Cargo;
                movimiento.SaldoResultante = montosExplicitos.Value.Saldo ?? saldoAnterior
                    + (montosExplicitos.Value.Abono ?? 0m)
                    - (montosExplicitos.Value.Cargo ?? 0m);
                return;
            }

            var montosInferidos = InferirMontosDesdeDescripcion(
                movimiento.TipoMovimiento,
                movimiento.Descripcion,
                saldoAnterior);

            movimiento.MontoAbono = montosInferidos.MontoAbono;
            movimiento.MontoCargo = montosInferidos.MontoCargo;
            movimiento.SaldoResultante = montosInferidos.SaldoResultante;
        }

        internal static (decimal? MontoAbono, decimal? MontoCargo, decimal SaldoResultante) InferirMontosDesdeDescripcion(
            string? tipoOperacion,
            string? descripcion,
            decimal saldoAnterior)
        {
            var valores = ExtraerValoresMonetarios(descripcion);
            if (valores.Count == 0)
            {
                return (null, null, saldoAnterior);
            }

            var saldoInferido = valores[^1];
            var delta = saldoInferido - saldoAnterior;
            var montoPrincipal = valores.Count >= 2 ? valores[^2] : Math.Abs(delta);
            if (montoPrincipal == 0m && delta != 0m)
            {
                montoPrincipal = Math.Abs(delta);
            }

            var direccionEsperada = ObtenerDireccionEsperada(tipoOperacion);
            if (direccionEsperada == DireccionMovimiento.Abono
                && (delta <= 0m || !CoincideDeltaConMonto(delta, montoPrincipal)))
            {
                saldoInferido = saldoAnterior + montoPrincipal;
                delta = montoPrincipal;
            }

            if (direccionEsperada == DireccionMovimiento.Cargo
                && (delta >= 0m || !CoincideDeltaConMonto(delta, montoPrincipal)))
            {
                saldoInferido = saldoAnterior - montoPrincipal;
                delta = -montoPrincipal;
            }

            if (delta > 0m)
            {
                return (montoPrincipal, null, saldoInferido);
            }

            if (delta < 0m)
            {
                return (null, montoPrincipal, saldoInferido);
            }

            return (null, null, saldoInferido);
        }

        internal static (decimal? Abono, decimal? Cargo, decimal? Saldo)? LeerMontosExplicitos(XElement? montos)
        {
            if (montos == null)
            {
                return null;
            }

            var ns = montos.GetDefaultNamespace();
            var abono = ParseDecimal(montos.Element(ns + "depositos")?.Value)
                ?? ParseDecimal(montos.Element(ns + "deposito")?.Value);
            var cargo = ParseDecimal(montos.Element(ns + "retiros")?.Value)
                ?? ParseDecimal(montos.Element(ns + "retiro")?.Value);
            var saldo = ParseDecimal(montos.Element(ns + "saldo")?.Value);

            if (!abono.HasValue && !cargo.HasValue && !saldo.HasValue)
            {
                return null;
            }

            return (
                abono.GetValueOrDefault() == 0m ? null : abono,
                cargo.GetValueOrDefault() == 0m ? null : cargo,
                saldo);
        }

        internal static IReadOnlyList<decimal> ExtraerValoresMonetarios(string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                return Array.Empty<decimal>();
            }

            return CurrencyRegex.Matches(descripcion)
                .Select(match =>
                {
                    var valor = ParseCurrencyValue(match.Groups["valor"].Value);
                    var esNegativo = match.Groups["signoInicial"].Success
                        || match.Groups["signoFinal"].Success;
                    return esNegativo ? -valor : valor;
                })
                .ToList();
        }

        private static bool CoincideDeltaConMonto(decimal delta, decimal montoPrincipal)
        {
            return Math.Abs(Math.Abs(delta) - Math.Abs(montoPrincipal)) <= 0.01m;
        }

        private static DireccionMovimiento ObtenerDireccionEsperada(string? tipoOperacion)
        {
            if (string.IsNullOrWhiteSpace(tipoOperacion))
            {
                return DireccionMovimiento.Desconocida;
            }

            return tipoOperacion.Trim().ToUpperInvariant() switch
            {
                "SPEI_RECIBIDO" => DireccionMovimiento.Abono,
                "DEPOSITO" => DireccionMovimiento.Abono,
                "ENVIO_SPEI" => DireccionMovimiento.Cargo,
                "COMPENSACION_SPEI" => DireccionMovimiento.Cargo,
                "COMISION" => DireccionMovimiento.Cargo,
                _ => DireccionMovimiento.Desconocida
            };
        }

        private static decimal ParseCurrencyValue(string valor)
        {
            var limpio = valor.Replace(",", string.Empty).Trim();
            return decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultado)
                ? resultado
                : 0m;
        }

        private enum DireccionMovimiento
        {
            Desconocida = 0,
            Abono = 1,
            Cargo = 2
        }

        private GuardarEstadoCuentaRequestDto CrearRequestDesdeEstadoActual()
        {
            if (EstadoCuentaBancario == null)
            {
                throw new InvalidOperationException("No hay un estado de cuenta cargado.");
            }

            return new GuardarEstadoCuentaRequestDto
            {
                VersionXml = EstadoCuentaBancario.VersionXml,
                NumeroCuenta = EstadoCuentaBancario.NumeroCuenta,
                Clabe = EstadoCuentaBancario.Clabe,
                TipoCuenta = EstadoCuentaBancario.TipoCuenta,
                TipoMoneda = EstadoCuentaBancario.TipoMoneda,
                FechaInicio = EstadoCuentaBancario.PeriodoInicio,
                FechaFin = EstadoCuentaBancario.PeriodoFin,
                FechaCorte = EstadoCuentaBancario.FechaCorte,
                SaldoInicial = EstadoCuentaBancario.SaldoInicial ?? 0m,
                TotalCargos = EstadoCuentaBancario.TotalCargos ?? 0m,
                TotalAbonos = EstadoCuentaBancario.TotalAbonos ?? 0m,
                SaldoFinal = EstadoCuentaBancario.SaldoFinal ?? 0m,
                TotalComisiones = EstadoCuentaBancario.TotalComisiones ?? 0m,
                TotalISR = EstadoCuentaBancario.TotalISR ?? 0m,
                TotalIVA = EstadoCuentaBancario.TotalIVA ?? 0m,
                TotalTransaccionesIndividuales = EstadoCuentaBancario.TotalTransacciones ?? 0,
                TotalGrupos = EstadoCuentaBancario.TotalGrupos ?? Movimientos.Count,
                NombreBanco = EstadoCuentaBancario.NombreBanco,
                RfcBanco = EstadoCuentaBancario.RfcBanco,
                NombreSucursal = EstadoCuentaBancario.NombreSucursal,
                DireccionSucursal = EstadoCuentaBancario.DireccionSucursal,
                Titular = EstadoCuentaBancario.Titular,
                RfcTitular = EstadoCuentaBancario.RfcTitular,
                NumeroCliente = EstadoCuentaBancario.NumeroCliente,
                DireccionTitular = EstadoCuentaBancario.DireccionTitular,
                FolioFiscal = EstadoCuentaBancario.FolioFiscal,
                CertificadoEmisor = EstadoCuentaBancario.CertificadoEmisor,
                FechaEmisionCert = EstadoCuentaBancario.FechaEmisionCert,
                CertificadoSat = EstadoCuentaBancario.CertificadoSAT,
                FechaCertificacionSat = EstadoCuentaBancario.FechaCertificacionSAT,
                RegimenFiscal = EstadoCuentaBancario.RegimenFiscal,
                MetodoPago = EstadoCuentaBancario.MetodoPago,
                FormaPago = EstadoCuentaBancario.FormaPago,
                UsoCfdi = EstadoCuentaBancario.UsoCFDI,
                ClaveProdServ = EstadoCuentaBancario.ClaveProdServ,
                LugarExpedicion = EstadoCuentaBancario.LugarExpedicion,
                Grupos = Movimientos.Select((movimiento, indice) => new GuardarEstadoCuentaGrupoDto
                {
                    OrdenGrupo = indice + 1,
                    GrupoId = string.IsNullOrWhiteSpace(movimiento.GrupoId) ? $"g-{indice + 1}" : movimiento.GrupoId,
                    Dia = movimiento.Dia,
                    Tipo = movimiento.TipoGrupo ?? movimiento.TipoMovimiento,
                    TransaccionPrincipal = new GuardarEstadoCuentaMovimientoDto
                    {
                        Fecha = movimiento.FechaMovimiento,
                        Tipo = movimiento.TipoMovimiento,
                        Subtipo = movimiento.SubtipoMovimiento,
                        Descripcion = movimiento.Descripcion,
                        Referencia = movimiento.Referencia,
                        Cargo = movimiento.MontoCargo,
                        Abono = movimiento.MontoAbono,
                        Saldo = movimiento.SaldoResultante,
                        Conciliado = movimiento.Conciliado,
                        Metadatos = movimiento.Metadatos
                    },
                    MovimientosRelacionados = movimiento.MovimientosRelacionados.Select(relacionado => new GuardarEstadoCuentaMovimientoRelacionadoDto
                    {
                        Tipo = relacionado.Tipo,
                        Orden = relacionado.Orden,
                        Descripcion = relacionado.Descripcion,
                        Rfc = relacionado.Rfc,
                        Monto = relacionado.Monto,
                        Saldo = relacionado.Saldo
                    }).ToList()
                }).ToList()
            };
        }

        private void AplicarFiltrosEstadosExistentes()
        {
            var fechaDesde = FechaFiltroDesde?.Date;
            var fechaHasta = FechaFiltroHasta?.Date;
            var referenciaFiltro = ReferenciaFiltro?.Trim();
            var abonoFiltro = AbonoFiltro?.Trim();
            var retiroFiltro = RetiroFiltro?.Trim();
            var abonoBuscado = decimal.TryParse(abonoFiltro, NumberStyles.Any, new CultureInfo("es-MX"), out var abonoValor)
                ? abonoValor
                : decimal.TryParse(abonoFiltro, NumberStyles.Any, CultureInfo.InvariantCulture, out abonoValor)
                    ? abonoValor
                    : (decimal?)null;
            var retiroBuscado = decimal.TryParse(retiroFiltro, NumberStyles.Any, new CultureInfo("es-MX"), out var retiroValor)
                ? retiroValor
                : decimal.TryParse(retiroFiltro, NumberStyles.Any, CultureInfo.InvariantCulture, out retiroValor)
                    ? retiroValor
                    : (decimal?)null;

            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value > fechaHasta.Value)
            {
                (fechaDesde, fechaHasta) = (fechaHasta, fechaDesde);
            }

            var filtrados = _estadosCuentaExistentesBase
                .Where(estado => !fechaDesde.HasValue || estado.FechaCorte.Date >= fechaDesde.Value.Date)
                .Where(estado => !fechaHasta.HasValue || estado.FechaCorte.Date <= fechaHasta.Value.Date)
                .Where(estado => string.IsNullOrWhiteSpace(abonoFiltro)
                    || (abonoBuscado.HasValue && estado.TotalAbonos == abonoBuscado.Value)
                    || estado.TotalAbonosTexto.Contains(abonoFiltro, StringComparison.OrdinalIgnoreCase))
                .Where(estado => string.IsNullOrWhiteSpace(retiroFiltro)
                    || (retiroBuscado.HasValue && estado.TotalCargos == retiroBuscado.Value)
                    || estado.TotalCargosTexto.Contains(retiroFiltro, StringComparison.OrdinalIgnoreCase))
                .Where(estado => string.IsNullOrWhiteSpace(referenciaFiltro)
                    || (!string.IsNullOrWhiteSpace(estado.ReferenciasBusqueda) && estado.ReferenciasBusqueda.Contains(referenciaFiltro, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(estado => estado.FechaCorte)
                .ThenByDescending(estado => estado.IdEstadoCuenta)
                .ToList();

            EstadosCuentaExistentes.Clear();
            foreach (var estado in filtrados)
            {
                EstadosCuentaExistentes.Add(estado);
            }

            OnPropertyChanged(nameof(ResumenEstadosExistentes));
        }

        private static void ValidarEstadoCuentaParaGuardado(GuardarEstadoCuentaRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.NumeroCuenta))
            {
                throw new InvalidOperationException("El estado de cuenta debe incluir número de cuenta.");
            }

            if (string.IsNullOrWhiteSpace(request.Clabe))
            {
                throw new InvalidOperationException("El estado de cuenta debe incluir CLABE.");
            }

            if (request.FechaCorte == default)
            {
                throw new InvalidOperationException("El estado de cuenta debe incluir fecha de corte.");
            }

            if (request.Grupos.Count == 0)
            {
                throw new InvalidOperationException("El estado de cuenta debe incluir al menos un grupo de movimientos.");
            }
        }

        private async Task<GuardarEstadoCuentaResponseDto> ValidarYGuardarEstadoActualAsync(GuardarEstadoCuentaRequestDto request)
        {
            ValidarEstadoCuentaParaGuardado(request);
            return await _estadoCuentaXmlService.GuardarEstadoCuentaAsync(request);
        }
    }
}
