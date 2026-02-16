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
        private EstadoCuenta? _estadoCuenta;
        private ObservableCollection<Transaccion> _transacciones;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _successMessage;

        public EsCuentaViewModel()
        {
            _transacciones = new ObservableCollection<Transaccion>();
        }

        /// <summary>
        /// Estado de cuenta actual cargado
        /// </summary>
        public EstadoCuenta? EstadoCuenta
        {
            get => _estadoCuenta;
            set => SetProperty(ref _estadoCuenta, value);
        }

        /// <summary>
        /// Colección observable de transacciones para el binding en la UI
        /// </summary>
        public ObservableCollection<Transaccion> Transacciones
        {
            get => _transacciones;
            set => SetProperty(ref _transacciones, value);
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
                    
                    SuccessMessage = $"Archivo {file.Name} cargado exitosamente. Se encontraron {Transacciones.Count} transacciones.";
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
                
                // Crear nueva instancia de EstadoCuenta
                var estadoCuenta = new EstadoCuenta();

                // EJEMPLO DE PARSEO - Ajustar según la estructura real del XML
                // Buscar el nodo raíz del estado de cuenta
                var raiz = doc.Root;
                if (raiz != null)
                {
                    // Extraer información general del estado de cuenta
                    // Ejemplo: <NumeroCuenta>1234567890</NumeroCuenta>
                    estadoCuenta.NumeroCuenta = raiz.Element("NumeroCuenta")?.Value;
                    estadoCuenta.Titular = raiz.Element("Titular")?.Value;
                    estadoCuenta.Banco = raiz.Element("Banco")?.Value;
                    estadoCuenta.Sucursal = raiz.Element("Sucursal")?.Value;
                    estadoCuenta.Periodo = raiz.Element("Periodo")?.Value;
                    estadoCuenta.FechaInicio = raiz.Element("FechaInicio")?.Value;
                    estadoCuenta.FechaFin = raiz.Element("FechaFin")?.Value;

                    // Parsear valores decimales
                    if (decimal.TryParse(raiz.Element("SaldoInicial")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal saldoInicial))
                        estadoCuenta.SaldoInicial = saldoInicial;
                    
                    if (decimal.TryParse(raiz.Element("SaldoFinal")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal saldoFinal))
                        estadoCuenta.SaldoFinal = saldoFinal;
                    
                    if (decimal.TryParse(raiz.Element("TotalCargos")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal totalCargos))
                        estadoCuenta.TotalCargos = totalCargos;
                    
                    if (decimal.TryParse(raiz.Element("TotalAbonos")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal totalAbonos))
                        estadoCuenta.TotalAbonos = totalAbonos;

                    // Parsear transacciones
                    // Ejemplo: <Transacciones><Transaccion>...</Transaccion></Transacciones>
                    var transaccionesElement = raiz.Element("Transacciones");
                    if (transaccionesElement != null)
                    {
                        foreach (var transElement in transaccionesElement.Elements("Transaccion"))
                        {
                            var transaccion = new Transaccion
                            {
                                Fecha = transElement.Element("Fecha")?.Value,
                                Descripcion = transElement.Element("Descripcion")?.Value,
                                Tipo = transElement.Element("Tipo")?.Value,
                                Referencia = transElement.Element("Referencia")?.Value
                            };

                            // Parsear monto
                            if (decimal.TryParse(transElement.Element("Monto")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto))
                                transaccion.Monto = monto;

                            // Parsear saldo
                            if (decimal.TryParse(transElement.Element("Saldo")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal saldo))
                                transaccion.Saldo = saldo;

                            estadoCuenta.Transacciones.Add(transaccion);
                        }
                    }
                }

                // Actualizar el estado de cuenta y la colección de transacciones
                EstadoCuenta = estadoCuenta;
                Transacciones.Clear();
                foreach (var transaccion in estadoCuenta.Transacciones)
                {
                    Transacciones.Add(transaccion);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al parsear el XML: {ex.Message}", ex);
            }
        }
    }
}
