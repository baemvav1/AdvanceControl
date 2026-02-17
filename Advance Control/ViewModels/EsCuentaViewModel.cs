using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Advance_Control.Models;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de Estado de Cuenta
    /// </summary>
    public class EsCuentaViewModel : ViewModelBase
    {
        private EstadoCuentaBancario? _estadoCuentaBancario;
        private ObservableCollection<MovimientoBancario> _movimientos;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;

        // Inicializar el campo ns para evitar CS8618
        private XNamespace ns = XNamespace.None;

        public EsCuentaViewModel()
        {
            _movimientos = new ObservableCollection<MovimientoBancario>();
        }

        /// <summary>
        /// Estado de cuenta actual cargado
        /// </summary>
        public EstadoCuentaBancario? EstadoCuentaBancario
        {
            get => _estadoCuentaBancario;
            set => SetProperty(ref _estadoCuentaBancario, value);
        }

        /// <summary>
        /// Colección observable de Movimientos para el binding en la UI
        /// </summary>
        public ObservableCollection<MovimientoBancario> Movimientos
        {
            get => _movimientos;
            set => SetProperty(ref _movimientos, value);
        }

        /// <summary>
        /// Indica si se está cargando un archivo
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Mensaje de éxito para mostrar al usuario
        /// </summary>
        public string? SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        /// <summary>
        /// Carga un archivo XML y parsea el estado de cuenta
        /// </summary>
        public async Task CargarArchivoXmlAsync(nint windowHandle)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;

                // Configurar el FileOpenPicker
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".xml");

                // Inicializar el picker con el window handle
                WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);

                // Mostrar el picker y obtener el archivo
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    // Leer el contenido del archivo
                    string xmlContent = await FileIO.ReadTextAsync(file);
                    
                    // Parsear el XML
                    ParsearEstadoCuentaXml(xmlContent);
                    
                    SuccessMessage = $"Archivo {file.Name} cargado exitosamente. Se encontraron {Movimientos.Count} Movimientos.";
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



        /// <summary>
        /// Parsea el contenido XML y extrae los datos del estado de cuenta
        /// Este es un modelo de función para capturar los datos entre llaves
        /// </summary>
        private void ParsearEstadoCuentaXml(string xmlContent)
        {
            try
            {
                // Cargar el documento XML
                XDocument doc = XDocument.Parse(xmlContent);

                // Obtener el namespace del documento
                XNamespace ns = doc.Root.GetDefaultNamespace();

                // Obtener el elemento raíz (estadoCuenta)
                XElement raiz = doc.Root;

                // Crear nueva instancia de EstadoCuenta
                var estadoCuenta = new EstadoCuentaBancario();

                // Procesar información general
                var infoGeneral = raiz.Element(ns + "informacionGeneral");
                if (infoGeneral != null)
                {
                    // Procesar banco
                    var banco = infoGeneral.Element(ns + "banco");
                    if (banco != null)
                    {
                        estadoCuenta.NombreBanco = banco.Element(ns + "nombre")?.Value;
                        estadoCuenta.RfcBanco = banco.Element(ns + "rfc")?.Value;
                        estadoCuenta.NombreSucursal = banco.Element(ns + "sucursal")?.Value;
                        estadoCuenta.DireccionSucursal = banco.Element(ns + "direccion")?.Value;
                    }

                    // Procesar titular
                    var titular = infoGeneral.Element(ns + "titular");
                    if (titular != null)
                    {
                        estadoCuenta.Titular = titular.Element(ns + "razonSocial")?.Value;
                        estadoCuenta.RfcTitular = titular.Element(ns + "rfc")?.Value;
                        estadoCuenta.NumeroCliente = titular.Element(ns + "numeroCliente")?.Value;
                        estadoCuenta.DireccionTitular = titular.Element(ns + "direccion")?.Value;
                    }

                    // Procesar cuenta
                    var cuenta = infoGeneral.Element(ns + "cuenta");
                    if (cuenta != null)
                    {
                        estadoCuenta.NumeroCuenta = cuenta.Element(ns + "numero")?.Value;
                        estadoCuenta.Clabe = cuenta.Element(ns + "clabe")?.Value;
                        estadoCuenta.TipoCuenta = cuenta.Element(ns + "tipo")?.Value;
                        estadoCuenta.TipoMoneda = cuenta.Element(ns + "moneda")?.Value;
                    }

                    // Procesar período
                    var periodo = infoGeneral.Element(ns + "periodo");
                    if (periodo != null)
                    {
                        estadoCuenta.Periodo = $"{periodo.Element(ns + "fechaInicio")?.Value} - {periodo.Element(ns + "fechaFin")?.Value}";
                        // Conversión segura de string a DateTime
                        var fechaInicioStr = periodo.Element(ns + "fechaInicio")?.Value;
                        var fechaFinStr = periodo.Element(ns + "fechaFin")?.Value;
                        var fechaCorteStr = periodo.Element(ns + "fechaCorte")?.Value;

                        if (DateTime.TryParse(fechaInicioStr, out var fechaInicio))
                            estadoCuenta.PeriodoInicio = fechaInicio;
                        if (DateTime.TryParse(fechaFinStr, out var fechaFin))
                            estadoCuenta.PeriodoFin = fechaFin;
                        if (DateTime.TryParse(fechaCorteStr, out var fechaCorte))
                            estadoCuenta.FechaCorte = fechaCorte;
                    }

                    // Procesar resumen
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
                
                // Procesar CFDI información
                var cfdiInfo = raiz.Element(ns + "cfdiInformacion");
                if (cfdiInfo != null)
                {
                    estadoCuenta.FolioFiscal = cfdiInfo.Element(ns + "folioFiscal")?.Value;

                    // Procesar certificados
                    var certificados = cfdiInfo.Element(ns + "certificados");
                    if (certificados != null)
                    {
                        var certEmisor = certificados.Element(ns + "certificadoEmisor");
                        if (certEmisor != null)
                        {
                            estadoCuenta.CertificadoEmisor = certEmisor.Element(ns + "numero")?.Value;
                            estadoCuenta.FechaEmisionCert = certEmisor.Element(ns + "fechaEmision")?.Value;
                        }

                        var certSAT = certificados.Element(ns + "certificadoSAT");
                        if (certSAT != null)
                        {
                            estadoCuenta.CertificadoSAT = certSAT.Element(ns + "numero")?.Value;
                            estadoCuenta.FechaCertificacionSAT = certSAT.Element(ns + "fechaCertificacion")?.Value;
                        }
                    }

                    // Procesar datos fiscales
                    var datosFiscales = cfdiInfo.Element(ns + "datosFiscales");
                    if (datosFiscales != null)
                    {
                        estadoCuenta.RegimenFiscal = datosFiscales.Element(ns + "regimenFiscal")?.Value;
                        estadoCuenta.MetodoPago = datosFiscales.Element(ns + "metodoPago")?.Value;
                        estadoCuenta.FormaPago = datosFiscales.Element(ns + "formaPago")?.Value;
                        estadoCuenta.UsoCFDI = datosFiscales.Element(ns + "usoCFDI")?.Value;
                        estadoCuenta.ClaveProdServ = datosFiscales.Element(ns + "claveProdServ")?.Value;
                        estadoCuenta.LugarExpedicion = datosFiscales.Element(ns + "lugarExpedicion")?.Value;
                    }
                }
                
                // Procesar Movimientos
                var movimientosElement = raiz.Element(ns + "transacciones");
                if (movimientosElement != null)
                {
                    estadoCuenta.TotalTransacciones = ObtenerValorEntero(movimientosElement.Attribute("total"));

                    foreach (var transElement in movimientosElement.Elements(ns + "transaccion"))
                    {
                        var movimiento = new MovimientoBancario
                        {
                            // Asignar propiedades según los datos del XML y la definición de MovimientoBancario
                            FechaMovimiento = DateTime.TryParse(transElement.Element(ns + "dia")?.Value, out var fechaMov) ? fechaMov : default,
                            Descripcion = transElement.Element(ns + "descripcion")?.Value,
                            Referencia = transElement.Element(ns + "referencia")?.Value,
                            // Determinar si es cargo o abono
                            MontoAbono = ObtenerValorDecimal(ns, transElement, "depositos"),
                            MontoCargo = ObtenerValorDecimal(ns, transElement, "retiros"),
                            SaldoResultante = ObtenerValorDecimal(ns, transElement, "saldo"),
                            TipoMovimiento = tipoTransaccion(transElement),
                            FechaRegistro = DateTime.Now // O asignar según corresponda
                        };
                        estadoCuenta.Movimientos.Add(movimiento);



                        // Procesar metadatos si existen
                        /*var metadatos = transElement.Element(ns + "metadatos");
                        if (metadatos != null)
                        {
                            transaccion.BancoCodigo = metadatos.Element(ns + "banco_codigo")?.Value;
                            transaccion.BancoNombre = metadatos.Element(ns + "banco_nombre")?.Value;
                            transaccion.CuentaTransferencia = metadatos.Element(ns + "cuenta")?.Value;
                            transaccion.RfcTransferencia = metadatos.Element(ns + "rfc")?.Value;
                            transaccion.ClaveRastreo = metadatos.Element(ns + "cve_rastreo")?.Value;
                            transaccion.Concepto = metadatos.Element(ns + "concepto")?.Value;
                            transaccion.Hora = metadatos.Element(ns + "hora")?.Value;
                            transaccion.Emisor = metadatos.Element(ns + "emisor")?.Value;
                            transaccion.Destinatario = metadatos.Element(ns + "destinatario")?.Value;
                        }
                        */


                    }
                }

                // Procesar resumen de comisiones
                var resumenComisiones = raiz.Element(ns + "resumenComisiones");
                if (resumenComisiones != null)
                {
                   // estadoCuenta.OtrasComisiones = ObtenerValorDecimal(resumenComisiones, "otrasComisiones");
                    //estadoCuenta.IvaComisiones = ObtenerValorDecimal(resumenComisiones, "iva");
                    //estadoCuenta.TotalComisionesCobradas = ObtenerValorDecimal(resumenComisiones, "total");
                }

                // Actualizar el estado de cuenta y la colección de Movimientos
                EstadoCuentaBancario = estadoCuenta;
                Movimientos.Clear();
                /*foreach (var transaccion in estadoCuenta.Transacciones)
                {
                    Movimientos.Add(transaccion);
                }*/
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al parsear el XML: {ex.Message}", ex);
            }

        }

        private decimal ObtenerValorDecimal(XNamespace ns,XElement elementoPadre, string nombreElemento)
        {
            var valor = elementoPadre?.Element(ns + nombreElemento)?.Value;
            if (string.IsNullOrEmpty(valor)) return 0;

            // Usar cultura mexicana para decimales (punto como separador decimal)
            if (decimal.TryParse(valor, NumberStyles.Any, new CultureInfo("es-MX"), out decimal resultado))
                return resultado;

            // Intento alternativo con cultura invariante
            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out resultado))
                return resultado;

            return 0;
        }
        private int ObtenerValorEntero(XAttribute atributo)
        {
            if (atributo == null) return 0;

            if (int.TryParse(atributo.Value, out int resultado))
                return resultado;

            return 0;
        }

        private string tipoTransaccion(XElement transaccion)
        {
            return "";
        }
    }



}
