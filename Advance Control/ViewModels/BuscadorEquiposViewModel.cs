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
    /// Buscador de equipos. Carga una sola vez GET api/equipo_crud/buscador (para tener las
    /// sugerencias de marca/área/ubicación), pero la lista de equipos queda vacía hasta que hay
    /// un primer criterio: texto de identificador o una opción elegida en Marca, Área o Ubicación.
    ///  - Texto: solo identificador (contiene, sin mayúsculas). Exacto primero, luego "empieza con".
    ///  - Marca/Área/Ubicación: filtran solo cuando se elige una sugerencia (o se escribe un valor
    ///    exacto y se da Enter). Borrar el texto quita el filtro.
    ///  - Las sugerencias de cada campo respetan los otros filtros ya elegidos.
    /// </summary>
    public class BuscadorEquiposViewModel : ViewModelBase
    {
        public enum Campo { Marca, Area, Ubicacion }

        private readonly IEquipoService _equipoService;
        private readonly ILoggingService _logger;
        private List<EquipoBusquedaDto> _todos = new();

        private string _texto = string.Empty;
        private string? _marca;
        private string? _area;
        private string? _ubicacion;
        private EquipoBusquedaDto? _seleccionado;
        private bool _isLoading;
        private string? _error;

        public BuscadorEquiposViewModel(IEquipoService equipoService, ILoggingService logger)
        {
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ObservableCollection<EquipoBusquedaDto> Resultados { get; } = new();
        public ObservableCollection<string> SugerenciasMarca { get; } = new();
        public ObservableCollection<string> SugerenciasArea { get; } = new();
        public ObservableCollection<string> SugerenciasUbicacion { get; } = new();

        /// <summary>Texto del buscador (solo identificador).</summary>
        public string Texto
        {
            get => _texto;
            set { if (SetProperty(ref _texto, value ?? string.Empty)) Filtrar(); }
        }

        public EquipoBusquedaDto? Seleccionado
        {
            get => _seleccionado;
            set { if (SetProperty(ref _seleccionado, value)) OnPropertyChanged(nameof(HaySeleccion)); }
        }

        public bool HaySeleccion => _seleccionado is not null;

        public bool IsLoading
        {
            get => _isLoading;
            private set { if (SetProperty(ref _isLoading, value)) NotificarEstados(); }
        }

        public string? Error
        {
            get => _error;
            private set { if (SetProperty(ref _error, value)) NotificarEstados(); }
        }

        public bool HayError => _error is not null;

        /// <summary>Hay al menos un criterio: texto o una opción elegida.</summary>
        public bool HayCriterio => _texto.Trim().Length > 0 || _marca is not null || _area is not null || _ubicacion is not null;

        /// <summary>Mensaje inicial: todavía no se ha buscado nada.</summary>
        public bool MostrarIndicacion => !HayCriterio && !_isLoading && _error is null;

        public bool SinResultados => HayCriterio && !_isLoading && _error is null && Resultados.Count == 0;

        public string ConteoTexto => HayCriterio && Resultados.Count > 0
            ? (Resultados.Count == 1 ? "1 equipo" : $"{Resultados.Count} equipos")
            : string.Empty;

        public async Task CargarAsync(CancellationToken cancellationToken = default)
        {
            if (_isLoading) return;
            IsLoading = true;
            Error = null;
            try
            {
                _todos = await _equipoService.GetBuscadorAsync(cancellationToken);
                Filtrar();
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

        /// <summary>Recalcula las sugerencias de un campo para lo que se lleva escrito.</summary>
        public void ActualizarSugerencias(Campo campo, string? escrito)
        {
            var texto = (escrito ?? string.Empty).Trim();
            var valores = AplicarFiltros(_todos, excepto: campo)
                .Select(e => Valor(e, campo))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .Where(v => texto.Length == 0 || v.Contains(texto, StringComparison.CurrentCultureIgnoreCase))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase);

            var destino = Sugerencias(campo);
            destino.Clear();
            foreach (var v in valores)
                destino.Add(v);
        }

        /// <summary>
        /// Fija el filtro de un campo. Acepta solo valores existentes (sugerencia elegida o texto
        /// exacto); devuelve el valor tal como quedó, o null si se quitó el filtro.
        /// </summary>
        public string? ElegirOpcion(Campo campo, string? valor)
        {
            var texto = (valor ?? string.Empty).Trim();
            string? elegido = null;
            if (texto.Length > 0)
            {
                elegido = _todos.Select(e => Valor(e, campo)?.Trim())
                    .FirstOrDefault(v => string.Equals(v, texto, StringComparison.CurrentCultureIgnoreCase));
                if (elegido is null)
                    return FiltroActual(campo); // no existe: no cambia el filtro
            }

            switch (campo)
            {
                case Campo.Marca: _marca = elegido; break;
                case Campo.Area: _area = elegido; break;
                case Campo.Ubicacion: _ubicacion = elegido; break;
            }
            Filtrar();
            return elegido;
        }

        /// <summary>Quita el filtro de un campo (cuando se borra su texto).</summary>
        public void QuitarOpcion(Campo campo)
        {
            if (FiltroActual(campo) is null) return;
            ElegirOpcion(campo, null);
        }

        /// <summary>Cancelar: vuelve al estado inicial (sin criterios ni lista).</summary>
        public void Reiniciar()
        {
            _texto = string.Empty;
            _marca = _area = _ubicacion = null;
            OnPropertyChanged(nameof(Texto));
            Filtrar();
        }

        public string? FiltroActual(Campo campo) => campo switch
        {
            Campo.Marca => _marca,
            Campo.Area => _area,
            _ => _ubicacion
        };

        private ObservableCollection<string> Sugerencias(Campo campo) => campo switch
        {
            Campo.Marca => SugerenciasMarca,
            Campo.Area => SugerenciasArea,
            _ => SugerenciasUbicacion
        };

        private static string? Valor(EquipoBusquedaDto e, Campo campo) => campo switch
        {
            Campo.Marca => e.Marca,
            Campo.Area => e.Area,
            _ => e.Ubicacion
        };

        private IEnumerable<EquipoBusquedaDto> AplicarFiltros(IEnumerable<EquipoBusquedaDto> q, Campo? excepto = null)
        {
            bool Igual(string? a, string b) => string.Equals(a?.Trim(), b, StringComparison.CurrentCultureIgnoreCase);
            if (_marca is not null && excepto != Campo.Marca) q = q.Where(e => Igual(e.Marca, _marca));
            if (_area is not null && excepto != Campo.Area) q = q.Where(e => Igual(e.Area, _area));
            if (_ubicacion is not null && excepto != Campo.Ubicacion) q = q.Where(e => Igual(e.Ubicacion, _ubicacion));
            return q;
        }

        private void Filtrar()
        {
            var anterior = _seleccionado;
            Resultados.Clear();

            if (HayCriterio)
            {
                var q = AplicarFiltros(_todos);
                var texto = _texto.Trim();
                if (texto.Length > 0)
                    q = q.Where(e => e.Identificador.Contains(texto, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(e => string.Equals(e.Identificador, texto, StringComparison.OrdinalIgnoreCase) ? 0
                                     : e.Identificador.StartsWith(texto, StringComparison.OrdinalIgnoreCase) ? 1 : 2)
                         .ThenBy(e => e.Identificador, StringComparer.OrdinalIgnoreCase);
                else
                    q = q.OrderBy(e => e.Identificador, StringComparer.OrdinalIgnoreCase);

                foreach (var e in q)
                    Resultados.Add(e);
            }

            Seleccionado = anterior is not null && Resultados.Contains(anterior) ? anterior
                : Resultados.Count == 1 ? Resultados[0]
                : null;

            NotificarEstados();
        }

        private void NotificarEstados()
        {
            OnPropertyChanged(nameof(HayError));
            OnPropertyChanged(nameof(HayCriterio));
            OnPropertyChanged(nameof(MostrarIndicacion));
            OnPropertyChanged(nameof(SinResultados));
            OnPropertyChanged(nameof(ConteoTexto));
        }
    }
}
