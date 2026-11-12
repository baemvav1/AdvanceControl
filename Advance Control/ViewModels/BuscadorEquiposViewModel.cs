using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Equipos;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    /// <summary>
    /// Buscador de equipos (pensado para mostrarse como modal). Carga una sola vez
    /// GET api/equipo_crud/buscador y filtra en memoria:
    ///  - texto: solo el identificador (contiene, sin mayúsculas);
    ///  - filtros de área, ubicación y marca. Ubicación ofrece solo las del área elegida.
    /// Orden: identificador exacto, luego los que empiezan con el texto, luego el resto.
    /// </summary>
    public class BuscadorEquiposViewModel : ViewModelBase
    {
        public const string TodasAreas = "Todas las áreas";
        public const string TodasUbicaciones = "Todas las ubicaciones";
        public const string TodasMarcas = "Todas las marcas";
        public const string SinArea = "(Sin área)";
        public const string SinUbicacion = "(Sin ubicación)";

        private readonly IEquipoService _equipoService;
        private readonly ILoggingService _logger;
        private List<EquipoBusquedaDto> _todos = new();

        private string _texto = string.Empty;
        private string _areaFiltro = TodasAreas;
        private string _ubicacionFiltro = TodasUbicaciones;
        private string _marcaFiltro = TodasMarcas;
        private EquipoBusquedaDto? _seleccionado;
        private bool _isLoading;
        private string? _error;
        private bool _cargado;

        public BuscadorEquiposViewModel(IEquipoService equipoService, ILoggingService logger)
        {
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ObservableCollection<EquipoBusquedaDto> Resultados { get; } = new();
        public ObservableCollection<string> OpcionesArea { get; } = new() { TodasAreas };
        public ObservableCollection<string> OpcionesUbicacion { get; } = new() { TodasUbicaciones };
        public ObservableCollection<string> OpcionesMarca { get; } = new() { TodasMarcas };

        public string Texto
        {
            get => _texto;
            set { if (SetProperty(ref _texto, value ?? string.Empty)) Filtrar(); }
        }

        public string AreaFiltro
        {
            get => _areaFiltro;
            set
            {
                if (_llenandoOpciones || !SetProperty(ref _areaFiltro, value ?? TodasAreas)) return;
                LlenarUbicaciones();
                Filtrar();
                OnPropertyChanged(nameof(HayFiltros));
            }
        }

        public string UbicacionFiltro
        {
            get => _ubicacionFiltro;
            set
            {
                // Al rellenar las opciones el ComboBox suelta la selección (null): ignorarlo.
                if (_llenandoOpciones) return;
                if (SetProperty(ref _ubicacionFiltro, value ?? TodasUbicaciones)) { Filtrar(); OnPropertyChanged(nameof(HayFiltros)); }
            }
        }

        public string MarcaFiltro
        {
            get => _marcaFiltro;
            set { if (!_llenandoOpciones && SetProperty(ref _marcaFiltro, value ?? TodasMarcas)) { Filtrar(); OnPropertyChanged(nameof(HayFiltros)); } }
        }

        public EquipoBusquedaDto? Seleccionado
        {
            get => _seleccionado;
            set
            {
                if (SetProperty(ref _seleccionado, value))
                {
                    OnPropertyChanged(nameof(HaySeleccion));
                    OnPropertyChanged(nameof(SinSeleccion));
                }
            }
        }

        public bool HaySeleccion => _seleccionado is not null;
        public bool SinSeleccion => _seleccionado is null && !_isLoading && _error is null;

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(SinSeleccion));
                    OnPropertyChanged(nameof(SinResultados));
                }
            }
        }

        public string? Error
        {
            get => _error;
            private set
            {
                if (SetProperty(ref _error, value))
                {
                    OnPropertyChanged(nameof(HayError));
                    OnPropertyChanged(nameof(SinSeleccion));
                    OnPropertyChanged(nameof(SinResultados));
                }
            }
        }

        public bool HayError => _error is not null;
        public bool SinResultados => _cargado && !_isLoading && _error is null && Resultados.Count == 0;
        public bool HayFiltros => _areaFiltro != TodasAreas || _ubicacionFiltro != TodasUbicaciones || _marcaFiltro != TodasMarcas;

        public string ConteoTexto => !_cargado ? string.Empty
            : Resultados.Count == _todos.Count ? $"{_todos.Count} equipos"
            : $"{Resultados.Count} de {_todos.Count} equipos";

        public async Task CargarAsync(CancellationToken cancellationToken = default)
        {
            if (_isLoading) return;
            IsLoading = true;
            Error = null;
            try
            {
                _todos = await _equipoService.GetBuscadorAsync(cancellationToken);
                LlenarOpciones();
                _cargado = true;
                // Al vaciar las opciones los ComboBox sueltan su selección: volver a "Todos".
                LimpiarFiltros();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al cargar el buscador de equipos", ex, "BuscadorEquiposViewModel", "CargarAsync");
                Error = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LimpiarFiltros()
        {
            _areaFiltro = TodasAreas;
            _ubicacionFiltro = TodasUbicaciones;
            _marcaFiltro = TodasMarcas;
            LlenarUbicaciones();
            OnPropertyChanged(nameof(AreaFiltro));
            OnPropertyChanged(nameof(UbicacionFiltro));
            OnPropertyChanged(nameof(MarcaFiltro));
            OnPropertyChanged(nameof(HayFiltros));
            Filtrar();
        }

        private bool _llenandoOpciones;

        private static void Llenar(ObservableCollection<string> destino, string todos, IEnumerable<string> valores, string? vacio)
        {
            destino.Clear();
            destino.Add(todos);
            foreach (var v in valores.Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase))
                destino.Add(v);
            if (vacio is not null) destino.Add(vacio);
        }

        private void LlenarOpciones()
        {
            _llenandoOpciones = true;
            try
            {
                Llenar(OpcionesArea, TodasAreas, _todos.Where(e => !string.IsNullOrWhiteSpace(e.Area)).Select(e => e.Area!),
                    _todos.Any(e => string.IsNullOrWhiteSpace(e.Area)) ? SinArea : null);
                Llenar(OpcionesMarca, TodasMarcas, _todos.Where(e => !string.IsNullOrWhiteSpace(e.Marca)).Select(e => e.Marca!.Trim()), null);
            }
            finally { _llenandoOpciones = false; }
            LlenarUbicaciones();
        }

        /// <summary>Ubicaciones del área elegida (o todas). Si la ubicación elegida ya no aplica, vuelve a "Todas".</summary>
        private void LlenarUbicaciones()
        {
            var delArea = FiltrarArea(_todos).ToList();
            _llenandoOpciones = true;
            try
            {
                Llenar(OpcionesUbicacion, TodasUbicaciones,
                    delArea.Where(e => !string.IsNullOrWhiteSpace(e.Ubicacion)).Select(e => e.Ubicacion!),
                    delArea.Any(e => string.IsNullOrWhiteSpace(e.Ubicacion)) ? SinUbicacion : null);
            }
            finally { _llenandoOpciones = false; }

            if (!OpcionesUbicacion.Contains(_ubicacionFiltro))
                _ubicacionFiltro = TodasUbicaciones;
            OnPropertyChanged(nameof(UbicacionFiltro));
            OnPropertyChanged(nameof(HayFiltros));
        }

        private IEnumerable<EquipoBusquedaDto> FiltrarArea(IEnumerable<EquipoBusquedaDto> q) =>
            _areaFiltro == SinArea ? q.Where(e => string.IsNullOrWhiteSpace(e.Area))
            : _areaFiltro != TodasAreas ? q.Where(e => string.Equals(e.Area, _areaFiltro, StringComparison.CurrentCultureIgnoreCase))
            : q;

        private void Filtrar()
        {
            var q = FiltrarArea(_todos);

            if (_ubicacionFiltro == SinUbicacion)
                q = q.Where(e => string.IsNullOrWhiteSpace(e.Ubicacion));
            else if (_ubicacionFiltro != TodasUbicaciones)
                q = q.Where(e => string.Equals(e.Ubicacion, _ubicacionFiltro, StringComparison.CurrentCultureIgnoreCase));

            if (_marcaFiltro != TodasMarcas)
                q = q.Where(e => string.Equals(e.Marca?.Trim(), _marcaFiltro, StringComparison.CurrentCultureIgnoreCase));

            var texto = _texto.Trim();
            if (texto.Length > 0)
            {
                q = q.Where(e => e.Identificador.Contains(texto, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(e => Rango(e, texto))
                     .ThenBy(e => e.Identificador, StringComparer.OrdinalIgnoreCase);
            }

            var anterior = _seleccionado;
            Resultados.Clear();
            foreach (var e in q)
                Resultados.Add(e);

            // Conservar la selección si sigue en los resultados; si queda uno solo, seleccionarlo.
            Seleccionado = anterior is not null && Resultados.Contains(anterior) ? anterior
                : Resultados.Count == 1 ? Resultados[0]
                : null;

            OnPropertyChanged(nameof(ConteoTexto));
            OnPropertyChanged(nameof(SinResultados));
        }

        private static int Rango(EquipoBusquedaDto e, string texto)
        {
            if (string.Equals(e.Identificador, texto, StringComparison.OrdinalIgnoreCase)) return 0;
            if (e.Identificador.StartsWith(texto, StringComparison.OrdinalIgnoreCase)) return 1;
            return 2;
        }
    }
}
