using System;
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
    /// <summary>
    /// ViewModel para la vista de Estado de Cuenta.
    /// </summary>
    public class EsCuentaViewModel : ViewModelBase
    {
        private static readonly Regex CurrencyRegex = new(@"\$\s*([0-9]{1,3}(?:,[0-9]{3})*(?:\.\d{2})|[0-9]+\.\d{2})", RegexOptions.Compiled);
        private readonly IEstadoCuentaXmlService _estadoCuentaXmlService;
        private EstadoCuentaBancario? _estadoCuentaBancario;
        private ObservableCollection<MovimientoBancario> _movimientos;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;

        public EsCuentaViewModel(IEstadoCuentaXmlService estadoCuentaXmlService)
        {
            _estadoCuentaXmlService = estadoCuentaXmlService ?? throw new ArgumentNullException(nameof(estadoCuentaXmlService));
            _movimientos = new ObservableCollection<MovimientoBancario>();
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

        public bool CanSave => EstadoCuentaBancario != null && !IsLoading;

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
                    SuccessMessage = $"Archivo {file.Name} cargado exitosamente. Se encontraron {Movimientos.Count} movimientos.";
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

                var result = await _estadoCuentaXmlService.GuardarEstadoCuentaAsync(CrearRequestDesdeEstadoActual());
                if (!result.Success)
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                        ? "No se pudo guardar el estado de cuenta."
                        : result.Message;
                    return;
                }

                EstadoCuentaBancario.IdEstadoCuentaBancario = result.IdEstadoCuenta;
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

        private void ParsearEstadoCuentaXml(string xmlContent)
        {
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
                var raiz = doc.Root ?? throw new InvalidDataException("El XML no contiene un nodo raiz valido.");
                var estadoCuenta = new EstadoCuentaBancario();

                var infoGeneral = raiz.Element(ns + "informacionGeneral");
                if (infoGeneral != null)
                {
                    var banco = infoGeneral.Element(ns + "banco");
                    if (banco != null)
                    {
                        estadoCuenta.NombreBanco = banco.Element(ns + "nombre")?.Value;
                        estadoCuenta.RfcBanco = banco.Element(ns + "rfc")?.Value;
                        estadoCuenta.NombreSucursal = banco.Element(ns + "sucursal")?.Value;
                        estadoCuenta.DireccionSucursal = banco.Element(ns + "direccion")?.Value;
                    }

                    var titular = infoGeneral.Element(ns + "titular");
                    if (titular != null)
                    {
                        estadoCuenta.Titular = titular.Element(ns + "razonSocial")?.Value;
                        estadoCuenta.RfcTitular = titular.Element(ns + "rfc")?.Value;
                        estadoCuenta.NumeroCliente = titular.Element(ns + "numeroCliente")?.Value;
                        estadoCuenta.DireccionTitular = titular.Element(ns + "direccion")?.Value;
                    }

                    var cuenta = infoGeneral.Element(ns + "cuenta");
                    if (cuenta != null)
                    {
                        estadoCuenta.NumeroCuenta = cuenta.Element(ns + "numero")?.Value;
                        estadoCuenta.Clabe = cuenta.Element(ns + "clabe")?.Value;
                        estadoCuenta.TipoCuenta = cuenta.Element(ns + "tipo")?.Value;
                        estadoCuenta.TipoMoneda = cuenta.Element(ns + "moneda")?.Value;
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

                var movimientosElement = raiz.Element(ns + "transacciones");
                if (movimientosElement != null)
                {
                    estadoCuenta.TotalTransacciones = ObtenerValorEntero(movimientosElement.Attribute("total"));
                    var saldoAnterior = estadoCuenta.SaldoInicial ?? 0m;

                    foreach (var transElement in movimientosElement.Elements(ns + "transaccion"))
                    {
                        var movimiento = new MovimientoBancario
                        {
                            FechaMovimiento = ConstruirFechaMovimiento(transElement.Element(ns + "dia")?.Value, estadoCuenta.FechaCorte),
                            Descripcion = transElement.Element(ns + "descripcion")?.Value ?? string.Empty,
                            Referencia = transElement.Element(ns + "referencia")?.Value ?? transElement.Attribute("id")?.Value,
                            TipoMovimiento = DeterminarTipoTransaccion(transElement.Element(ns + "descripcion")?.Value),
                            FechaRegistro = DateTime.Now
                        };

                        CompletarMontosDesdeDescripcion(movimiento, saldoAnterior);
                        estadoCuenta.Movimientos.Add(movimiento);
                        saldoAnterior = movimiento.SaldoResultante;
                    }
                }

                EstadoCuentaBancario = estadoCuenta;
                Movimientos.Clear();
                foreach (var movimiento in estadoCuenta.Movimientos.OrderBy(m => m.FechaMovimiento).ThenBy(m => m.Referencia))
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
            var valor = elementoPadre?.Element(ns + nombreElemento)?.Value;
            if (string.IsNullOrEmpty(valor))
            {
                return 0;
            }

            if (decimal.TryParse(valor, NumberStyles.Any, new CultureInfo("es-MX"), out var resultado))
            {
                return resultado;
            }

            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado))
            {
                return resultado;
            }

            return 0;
        }

        private static int ObtenerValorEntero(XAttribute? atributo)
        {
            if (atributo == null)
            {
                return 0;
            }

            return int.TryParse(atributo.Value, out var resultado) ? resultado : 0;
        }

        private static string DeterminarTipoTransaccion(string? descripcion)
        {
            var texto = (descripcion ?? string.Empty).ToUpperInvariant();
            if (texto.Contains("SPEI RECIBIDO") || texto.Contains("DEPOSITO") || texto.Contains("COMPENSACION"))
            {
                return "ABONO";
            }

            if (texto.Contains("ENVIO SPEI") || texto.Contains("I.V.A.") || texto.Contains("COMISION") || texto.Contains("PAGO"))
            {
                return "CARGO";
            }

            return "MOVIMIENTO";
        }

        private static void CompletarMontosDesdeDescripcion(MovimientoBancario movimiento, decimal saldoAnterior)
        {
            var valores = CurrencyRegex.Matches(movimiento.Descripcion ?? string.Empty)
                .Select(match => ParseCurrencyValue(match.Groups[1].Value))
                .ToList();

            if (valores.Count == 0)
            {
                movimiento.MontoAbono = 0;
                movimiento.MontoCargo = 0;
                movimiento.SaldoResultante = saldoAnterior;
                return;
            }

            movimiento.SaldoResultante = valores[^1];
            var delta = movimiento.SaldoResultante - saldoAnterior;
            var montoPrincipal = valores.Count >= 2 ? valores[^2] : Math.Abs(delta);
            if (montoPrincipal == 0 && delta != 0)
            {
                montoPrincipal = Math.Abs(delta);
            }

            if (delta > 0)
            {
                movimiento.MontoAbono = montoPrincipal;
                movimiento.MontoCargo = 0;
                movimiento.TipoMovimiento = "ABONO";
            }
            else if (delta < 0)
            {
                movimiento.MontoCargo = montoPrincipal;
                movimiento.MontoAbono = 0;
                movimiento.TipoMovimiento = "CARGO";
            }
            else if (string.Equals(movimiento.TipoMovimiento, "ABONO", StringComparison.OrdinalIgnoreCase))
            {
                movimiento.MontoAbono = montoPrincipal;
                movimiento.MontoCargo = 0;
            }
            else if (string.Equals(movimiento.TipoMovimiento, "CARGO", StringComparison.OrdinalIgnoreCase))
            {
                movimiento.MontoCargo = montoPrincipal;
                movimiento.MontoAbono = 0;
            }
            else
            {
                movimiento.MontoAbono = 0;
                movimiento.MontoCargo = 0;
            }
        }

        private static decimal ParseCurrencyValue(string valor)
        {
            var limpio = valor.Replace(",", string.Empty).Trim();
            return decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultado)
                ? resultado
                : 0m;
        }

        private GuardarEstadoCuentaRequestDto CrearRequestDesdeEstadoActual()
        {
            if (EstadoCuentaBancario == null)
            {
                throw new InvalidOperationException("No hay un estado de cuenta cargado.");
            }

            return new GuardarEstadoCuentaRequestDto
            {
                NumeroCuenta = EstadoCuentaBancario.NumeroCuenta ?? string.Empty,
                Clabe = EstadoCuentaBancario.Clabe ?? string.Empty,
                TipoCuenta = EstadoCuentaBancario.TipoCuenta,
                TipoMoneda = EstadoCuentaBancario.TipoMoneda,
                FechaInicio = EstadoCuentaBancario.PeriodoInicio,
                FechaFin = EstadoCuentaBancario.PeriodoFin,
                FechaCorte = EstadoCuentaBancario.FechaCorte,
                SaldoInicial = EstadoCuentaBancario.SaldoInicial ?? 0,
                TotalCargos = EstadoCuentaBancario.TotalCargos ?? 0,
                TotalAbonos = EstadoCuentaBancario.TotalAbonos ?? 0,
                SaldoFinal = EstadoCuentaBancario.SaldoFinal ?? 0,
                TotalComisiones = EstadoCuentaBancario.TotalComisiones ?? 0,
                TotalISR = EstadoCuentaBancario.TotalISR ?? 0,
                TotalIVA = EstadoCuentaBancario.TotalIVA ?? 0,
                Movimientos = Movimientos.Select(m => new GuardarEstadoCuentaMovimientoDto
                {
                    Fecha = m.FechaMovimiento,
                    Descripcion = m.Descripcion ?? string.Empty,
                    Referencia = m.Referencia,
                    Cargo = m.MontoCargo,
                    Abono = m.MontoAbono,
                    Saldo = m.SaldoResultante,
                    TipoOperacion = m.TipoMovimiento
                }).ToList()
            };
        }
    }
}
