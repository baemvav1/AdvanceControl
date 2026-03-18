using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.EstadoCuenta;

namespace Advance_Control.ViewModels
{
    public class DetailEstadoCuentaViewModel : ViewModelBase
    {
        private readonly IEstadoCuentaXmlService _estadoCuentaService;
        private readonly List<EstadoCuentaGrupoDetalleDto> _gruposOriginales = new();
        private EstadoCuentaResumenDto? _estadoCuenta;
        private bool _isLoading;
        private string? _errorMessage;
        private string _filtroTexto = string.Empty;
        private double _filtroMonto = double.NaN;
        private int _filtroConciliacionIndex;
        private bool _isFiltrosExpandidos = true;

        public DetailEstadoCuentaViewModel(IEstadoCuentaXmlService estadoCuentaService)
        {
            _estadoCuentaService = estadoCuentaService ?? throw new ArgumentNullException(nameof(estadoCuentaService));
            GruposFiltrados = new ObservableCollection<EstadoCuentaGrupoDetalleDto>();
            FiltrosTipoMovimiento = new ObservableCollection<FiltroTipoMovimientoDto>();
        }

        public EstadoCuentaResumenDto? EstadoCuenta
        {
            get => _estadoCuenta;
            private set => SetProperty(ref _estadoCuenta, value);
        }

        public ObservableCollection<EstadoCuentaGrupoDetalleDto> GruposFiltrados { get; }
        public ObservableCollection<FiltroTipoMovimientoDto> FiltrosTipoMovimiento { get; }
        public IReadOnlyList<string> OpcionesConciliacion { get; } = new[] { "Todos", "Conciliado", "No conciliado" };

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public string FiltroTexto
        {
            get => _filtroTexto;
            set
            {
                if (SetProperty(ref _filtroTexto, value))
                {
                    AplicarFiltros();
                }
            }
        }

        public double FiltroMonto
        {
            get => _filtroMonto;
            set
            {
                if (SetProperty(ref _filtroMonto, value))
                {
                    AplicarFiltros();
                }
            }
        }

        public int FiltroConciliacionIndex
        {
            get => _filtroConciliacionIndex;
            set
            {
                if (SetProperty(ref _filtroConciliacionIndex, value))
                {
                    AplicarFiltros();
                }
            }
        }

        public string ResumenGrupos => $"Grupos ({GruposFiltrados.Count})";
        public string ResumenRelacionados => $"Movimientos relacionados ({GruposFiltrados.Sum(g => g.MovimientosRelacionados.Count)})";
        public string ResumenFiltros => ObtenerResumenFiltros();
        public bool IsFiltrosExpandidos
        {
            get => _isFiltrosExpandidos;
            set
            {
                if (SetProperty(ref _isFiltrosExpandidos, value))
                {
                    OnPropertyChanged(nameof(TextoBotonFiltros));
                }
            }
        }

        public string TextoBotonFiltros => IsFiltrosExpandidos ? "Ocultar filtros" : "Mostrar filtros";

        public async Task CargarDetalleAsync(int idEstadoCuenta)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var detalle = await _estadoCuentaService.ObtenerDetalleEstadoCuentaAsync(idEstadoCuenta);
                if (detalle?.EstadoCuenta == null)
                {
                    ErrorMessage = "No se encontro el detalle del estado de cuenta seleccionado.";
                    LimpiarDatos();
                    return;
                }

                EstadoCuenta = detalle.EstadoCuenta;
                _gruposOriginales.Clear();
                _gruposOriginales.AddRange((detalle.Grupos ?? new List<EstadoCuentaGrupoDetalleDto>())
                    .OrderByDescending(grupo => grupo.Fecha)
                    .ThenByDescending(grupo => grupo.IdMovimiento));

                ActualizarFiltrosTipoMovimiento();
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar el detalle del estado de cuenta: {ex.Message}";
                LimpiarDatos();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LimpiarFiltros()
        {
            _filtroTexto = string.Empty;
            _filtroMonto = double.NaN;
            _filtroConciliacionIndex = 0;
            OnPropertyChanged(nameof(FiltroTexto));
            OnPropertyChanged(nameof(FiltroMonto));
            OnPropertyChanged(nameof(FiltroConciliacionIndex));

            foreach (var filtro in FiltrosTipoMovimiento)
            {
                filtro.IsSelected = true;
            }

            AplicarFiltros();
        }

        public void AlternarFiltros()
        {
            IsFiltrosExpandidos = !IsFiltrosExpandidos;
        }

        private void LimpiarDatos()
        {
            EstadoCuenta = null;
            _gruposOriginales.Clear();
            GruposFiltrados.Clear();
            LimpiarFiltrosTipoMovimiento();
            ActualizarResumenes();
        }

        private void ActualizarFiltrosTipoMovimiento()
        {
            var tipos = _gruposOriginales
                .GroupBy(ObtenerClaveFiltro, StringComparer.OrdinalIgnoreCase)
                .Select(grupo => new FiltroTipoMovimientoDto
                {
                    Clave = grupo.Key,
                    Etiqueta = grupo.Key,
                    Cantidad = grupo.Count(),
                    IsSelected = true
                })
                .OrderBy(tipo => tipo.Etiqueta)
                .ToList();

            LimpiarFiltrosTipoMovimiento();
            foreach (var tipo in tipos)
            {
                tipo.PropertyChanged += FiltroTipoMovimiento_PropertyChanged;
                FiltrosTipoMovimiento.Add(tipo);
            }

            OnPropertyChanged(nameof(ResumenFiltros));
        }

        private void AplicarFiltros()
        {
            var texto = (FiltroTexto ?? string.Empty).Trim();
            var monto = double.IsNaN(FiltroMonto) ? (decimal?)null : Convert.ToDecimal(FiltroMonto);
            var clavesSeleccionadas = FiltrosTipoMovimiento
                .Where(filtro => filtro.IsSelected)
                .Select(filtro => filtro.Clave)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var grupos = _gruposOriginales
                .Where(grupo => clavesSeleccionadas.Contains(ObtenerClaveFiltro(grupo)))
                .Where(grupo => CoincideTexto(texto, grupo))
                .Where(grupo => CoincideMonto(monto, grupo))
                .Where(CoincideConciliacion)
                .OrderByDescending(grupo => grupo.Fecha)
                .ThenByDescending(grupo => grupo.IdMovimiento)
                .ToList();

            ReemplazarColeccion(GruposFiltrados, grupos);
            ActualizarResumenes();
        }

        private static bool CoincideTexto(string texto, EstadoCuentaGrupoDetalleDto grupo)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            if (CoincideTexto(texto, grupo.Descripcion, grupo.Referencia, grupo.TipoOperacion, grupo.SubtipoOperacion, grupo.TipoGrupo, grupo.MetadatosResumen, grupo.RelacionadosResumen))
            {
                return true;
            }

            return grupo.MovimientosRelacionados.Any(relacionado =>
                CoincideTexto(texto, relacionado.TipoRelacion, relacionado.Descripcion, relacionado.Rfc, relacionado.MontoTexto, relacionado.SaldoTexto));
        }

        private static bool CoincideMonto(decimal? montoBuscado, EstadoCuentaGrupoDetalleDto grupo)
        {
            if (!montoBuscado.HasValue)
            {
                return true;
            }

            var objetivo = decimal.Round(montoBuscado.Value, 2);

            if (CoincideMonto(objetivo, grupo.Cargo)
                || CoincideMonto(objetivo, grupo.Abono)
                || CoincideMonto(objetivo, grupo.Saldo))
            {
                return true;
            }

            return grupo.MovimientosRelacionados.Any(relacionado =>
                CoincideMonto(objetivo, relacionado.Monto)
                || (relacionado.Saldo.HasValue && CoincideMonto(objetivo, relacionado.Saldo.Value)));
        }

        private static bool CoincideMonto(decimal objetivo, decimal valor)
        {
            return decimal.Round(valor, 2) == objetivo;
        }

        private static bool CoincideTexto(string texto, params string?[] valores)
        {
            return valores.Any(valor => !string.IsNullOrWhiteSpace(valor) && valor.Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        private bool CoincideConciliacion(EstadoCuentaGrupoDetalleDto grupo)
        {
            return FiltroConciliacionIndex switch
            {
                1 => grupo.Conciliado,
                2 => !grupo.Conciliado,
                _ => true
            };
        }

        private static void ReemplazarColeccion<T>(ObservableCollection<T> destino, IReadOnlyCollection<T> origen)
        {
            destino.Clear();
            foreach (var item in origen)
            {
                destino.Add(item);
            }
        }

        private void ActualizarResumenes()
        {
            OnPropertyChanged(nameof(ResumenGrupos));
            OnPropertyChanged(nameof(ResumenRelacionados));
            OnPropertyChanged(nameof(ResumenFiltros));
        }

        private void LimpiarFiltrosTipoMovimiento()
        {
            foreach (var filtro in FiltrosTipoMovimiento)
            {
                filtro.PropertyChanged -= FiltroTipoMovimiento_PropertyChanged;
            }

            FiltrosTipoMovimiento.Clear();
        }

        private void FiltroTipoMovimiento_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FiltroTipoMovimientoDto.IsSelected))
            {
                AplicarFiltros();
            }
        }

        private static string ObtenerClaveFiltro(EstadoCuentaGrupoDetalleDto grupo)
        {
            return string.IsNullOrWhiteSpace(grupo.TipoOperacion)
                ? (string.IsNullOrWhiteSpace(grupo.TipoGrupo) ? "Sin tipo" : grupo.TipoGrupo!)
                : grupo.TipoOperacion!;
        }

        private string ObtenerResumenFiltros()
        {
            if (FiltrosTipoMovimiento.Count == 0)
            {
                return "Sin filtros disponibles";
            }

            var seleccionados = FiltrosTipoMovimiento.Count(filtro => filtro.IsSelected);
            var resumenTipos = seleccionados == FiltrosTipoMovimiento.Count
                ? "tipos: todos"
                : $"tipos: {seleccionados} de {FiltrosTipoMovimiento.Count}";

            var resumenConciliacion = FiltroConciliacionIndex switch
            {
                1 => "conciliación: conciliado",
                2 => "conciliación: no conciliado",
                _ => "conciliación: todos"
            };

            return $"Filtros activos: {resumenTipos} · {resumenConciliacion}";
        }
    }
}
